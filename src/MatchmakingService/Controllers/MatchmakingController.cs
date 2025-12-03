using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using SharedEntities;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MatchmakingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchmakingController : ControllerBase
{
    private readonly ILogger<MatchmakingController> _logger;
    private readonly AIAgent _agentFxAgent;

    public MatchmakingController(
        ILogger<MatchmakingController> logger,
        AIAgent agentFxAgent)
    {
        _logger = logger;
        _agentFxAgent = agentFxAgent;
    }

    [HttpPost("alternatives/llm")]
    public async Task<ActionResult<MatchmakingResult>> FindAlternativesLlmAsync([FromBody] AlternativesRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[LLM] Finding alternatives for product: {ProductQuery}, User: {UserId}", request.ProductQuery, request.UserId);

        // LLM endpoint uses MAF under the hood since we removed SK
        return await FindAlternativesAsync(
            request,
            InvokeAgentFrameworkAsync,
            "[LLM]",
            cancellationToken);
    }

    [HttpPost("alternatives/maf")]
    public async Task<ActionResult<MatchmakingResult>> FindAlternativesMAFAsync([FromBody] AlternativesRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[MAF] Finding alternatives for product: {ProductQuery}, User: {UserId}", request.ProductQuery, request.UserId);

        return await FindAlternativesAsync(
            request,
            InvokeAgentFrameworkAsync,
            "[MAF]",
            cancellationToken);
    }

    private async Task<ActionResult<MatchmakingResult>> FindAlternativesAsync(
        AlternativesRequest request,
        Func<string, CancellationToken, Task<string>> invokeAgentAsync,
        string logPrefix,
        CancellationToken cancellationToken)
    {
        var prompt = BuildMatchmakingPrompt(request);

        try
        {
            var agentResponse = await invokeAgentAsync(prompt, cancellationToken);
            _logger.LogInformation("{Prefix} Raw agent response length: {Length}", logPrefix, agentResponse.Length);

            if (TryParseMatchmakingResult(agentResponse, out var result))
            {
                return Ok(result);
            }

            _logger.LogWarning("{Prefix} Unable to parse agent response. Using fallback recommendations. Raw: {Raw}", logPrefix, TrimForLog(agentResponse));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Prefix} Agent invocation failed. Falling back to heuristic recommendations.", logPrefix);
        }

        return Ok(BuildFallbackResult(request));
    }

    [HttpGet("health")]
    public IActionResult Health()
        => Ok(new { Status = "Healthy", Service = "MatchmakingService" });

    private async Task<string> InvokeAgentFrameworkAsync(string prompt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var thread = _agentFxAgent.GetNewThread();
        var response = await _agentFxAgent.RunAsync(prompt, thread);
        return response?.Text ?? string.Empty;
    }

    private MatchmakingResult BuildFallbackResult(AlternativesRequest request)
    {
        var normalizedQuery = request.ProductQuery.Trim();
        var baseName = string.IsNullOrEmpty(normalizedQuery) ? "your project" : normalizedQuery;

        var alternatives = new List<ProductInfo>
        {
            new()
            {
                Name = $"Alternative tool for {baseName}",
                Sku = "ALT-001",
                Price = 19.99m,
                IsAvailable = true,
                InStock = true,
                Section = "Hardware Tools",
                Location = "Aisle A1"
            },
            new()
            {
                Name = $"Premium alternative for {baseName}",
                Sku = "ALT-002",
                Price = 29.99m,
                IsAvailable = true,
                InStock = true,
                Section = "Hardware Tools",
                Location = "Aisle A1"
            }
        };

        var similarProducts = new List<ProductInfo>
        {
            new()
            {
                Name = $"Similar item related to {baseName}",
                Sku = "SIM-001",
                Price = 24.99m,
                IsAvailable = true,
                InStock = true,
                Section = "Hardware Tools",
                Location = "Aisle A2"
            }
        };

        return new MatchmakingResult
        {
            Alternatives = alternatives.ToArray(),
            SimilarProducts = similarProducts.ToArray()
        };
    }

    #region JSON & utility helpers

    private static string BuildMatchmakingPrompt(AlternativesRequest request) => @$"
You are an AI assistant that recommends alternative DIY tools and similar products for store customers.

Return a JSON object with the following structure:
{{
    ""alternatives"": [
        {{ ""name"": string, ""sku"": string, ""price"": number, ""isAvailable"": boolean, ""section""?: string, ""location""?: string }},
    ...
  ],
    ""similarProducts"": [
        {{ ""name"": string, ""sku"": string, ""price"": number, ""isAvailable"": boolean, ""section""?: string, ""location""?: string }},
    ...
  ]
}}

Ensure all SKUs are uppercase with dashes, prices use decimals, and include at least one item in each array.
Avoid extra commentary or markdown.

Product query: ""{request.ProductQuery}""
User identifier: ""{request.UserId}""
";

    private static bool TryParseMatchmakingResult(string agentResponse, out MatchmakingResult result)
    {
        result = default!;
        if (string.IsNullOrWhiteSpace(agentResponse))
        {
            return false;
        }

        var json = ExtractFirstJsonObject(agentResponse);
        if (json is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var alternatives = root.TryGetProperty("alternatives", out var alternativesProp)
                ? ParseProductArray(alternativesProp)
                : Array.Empty<ProductInfo>();

            var similarProducts = root.TryGetProperty("similarProducts", out var similarProp)
                ? ParseProductArray(similarProp)
                : Array.Empty<ProductInfo>();

            if (alternatives.Length == 0 && similarProducts.Length == 0)
            {
                return false;
            }

            result = new MatchmakingResult
            {
                Alternatives = alternatives,
                SimilarProducts = similarProducts
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static ProductInfo[] ParseProductArray(JsonElement arrayElement)
    {
        if (arrayElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<ProductInfo>();
        }

        var results = new List<ProductInfo>();
        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var hasAvailability = item.TryGetProperty("isAvailable", out var availableProp) && availableProp.ValueKind is JsonValueKind.True or JsonValueKind.False;
            var hasInStock = item.TryGetProperty("inStock", out var inStockProp) && inStockProp.ValueKind is JsonValueKind.True or JsonValueKind.False;

            var product = new ProductInfo
            {
                Name = item.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String
                    ? nameProp.GetString() ?? string.Empty
                    : string.Empty,
                Sku = item.TryGetProperty("sku", out var skuProp) && skuProp.ValueKind == JsonValueKind.String
                    ? skuProp.GetString() ?? string.Empty
                    : string.Empty,
                Price = item.TryGetProperty("price", out var priceProp) && priceProp.ValueKind is JsonValueKind.Number && priceProp.TryGetDecimal(out var price)
                    ? price
                    : 0m,
                IsAvailable = hasAvailability && availableProp.GetBoolean(),
                Section = item.TryGetProperty("section", out var sectionProp) && sectionProp.ValueKind == JsonValueKind.String
                    ? sectionProp.GetString() ?? string.Empty
                    : string.Empty,
                Location = item.TryGetProperty("location", out var locationProp) && locationProp.ValueKind == JsonValueKind.String
                    ? locationProp.GetString() ?? string.Empty
                    : string.Empty
            };

            if (hasInStock)
            {
                product.InStock = inStockProp.GetBoolean();
            }
            else
            {
                product.InStock = product.IsAvailable;
            }

            if (!string.IsNullOrWhiteSpace(product.Name) && !string.IsNullOrWhiteSpace(product.Sku))
            {
                results.Add(product);
            }
        }

        return results.ToArray();
    }

    private static string? ExtractFirstJsonObject(string input)
    {
        var start = input.IndexOf('{');
        if (start < 0)
        {
            return null;
        }

        var depth = 0;
        for (var i = start; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
            }

            if (depth == 0)
            {
                return input.Substring(start, i - start + 1).Trim();
            }
        }

        return null;
    }

    private static string TrimForLog(string value, int maxLength = 400)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";

    #endregion
}
