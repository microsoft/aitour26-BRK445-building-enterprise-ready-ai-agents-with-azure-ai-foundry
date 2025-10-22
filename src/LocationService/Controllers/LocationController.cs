#pragma warning disable SKEXP0110
using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using SharedEntities;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LocationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly ILogger<LocationController> _logger;
    private readonly AzureAIAgent _skAgent;
    private readonly AIAgent _agentFxAgent;

    public LocationController(
        ILogger<LocationController> logger,
        AzureAIAgent skAgent,
        AIAgent agentFxAgent)
    {
        _logger = logger;
        _skAgent = skAgent;
        _agentFxAgent = agentFxAgent;
    }

    [HttpGet("find/sk")]
    public async Task<ActionResult<LocationResult>> FindProductLocationSkAsync([FromQuery] string product, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SK] Finding location for product: {Product}", product);

        return await FindProductLocationAsync(
            product,
            InvokeSemanticKernelAsync,
            "[SK]",
            cancellationToken);
    }

    [HttpGet("find/agentfx")]
    public async Task<ActionResult<LocationResult>> FindProductLocationAgentFxAsync([FromQuery] string product, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AgentFx] Finding location for product: {Product}", product);

        return await FindProductLocationAsync(
            product,
            InvokeAgentFrameworkAsync,
            "[AgentFx]",
            cancellationToken);
    }

    private async Task<ActionResult<LocationResult>> FindProductLocationAsync(
        string product,
        Func<string, CancellationToken, Task<string>> invokeAgentAsync,
        string logPrefix,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(product))
        {
            return BadRequest("Product query is required.");
        }

        var prompt = BuildLocationPrompt(product);

        try
        {
            var agentResponse = await invokeAgentAsync(prompt, cancellationToken);
            _logger.LogInformation("{Prefix} Raw agent response length: {Length}", logPrefix, agentResponse.Length);

            if (TryParseLocationResult(agentResponse, out var parsed))
            {
                return Ok(parsed);
            }

            _logger.LogWarning("{Prefix} Unable to parse agent response. Using fallback locations. Raw: {Raw}", logPrefix, TrimForLog(agentResponse));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Prefix} Agent invocation failed. Using fallback locations.", logPrefix);
        }

        return Ok(BuildFallbackResult(product));
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "LocationService" });
    }

    private async Task<string> InvokeSemanticKernelAsync(string prompt, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        AzureAIAgentThread agentThread = new(_skAgent.Client);

        ChatMessageContent message = new(AuthorRole.User, prompt);
        await foreach (ChatMessageContent response in _skAgent.InvokeAsync(message, agentThread).WithCancellation(cancellationToken))
        {
            sb.Append(response.Content);
        }

        return sb.ToString();
    }

    private async Task<string> InvokeAgentFrameworkAsync(string prompt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var thread = _agentFxAgent.GetNewThread();
        var response = await _agentFxAgent.RunAsync(prompt, thread);
        return response?.Text ?? string.Empty;
    }

    private LocationResult BuildFallbackResult(string product)
        => new()
        {
            StoreLocations = GenerateLocationsByProduct(product)
        };

    private StoreLocation[] GenerateLocationsByProduct(string product)
    {
        // Simple logic to generate different locations based on product type
        var productLower = product.ToLowerInvariant();

        if (productLower.Contains("tool") || productLower.Contains("drill") || productLower.Contains("hammer"))
        {
            return new[]
            {
                new StoreLocation
                {
                    Section = "Hardware Tools",
                    Aisle = "A1",
                    Shelf = "Middle",
                    Description = $"Hand and power tools section - {product}"
                }
            };
        }
        else if (productLower.Contains("paint") || productLower.Contains("brush"))
        {
            return new[]
            {
                new StoreLocation
                {
                    Section = "Paint & Supplies",
                    Aisle = "B3",
                    Shelf = "Top",
                    Description = $"Paint and painting supplies - {product}"
                }
            };
        }
        else if (productLower.Contains("garden") || productLower.Contains("plant"))
        {
            return new[]
            {
                new StoreLocation
                {
                    Section = "Garden Center",
                    Aisle = "Outside",
                    Shelf = "Ground Level",
                    Description = $"Outdoor garden section - {product}"
                }
            };
        }
        else
        {
            return new[]
            {
                new StoreLocation
                {
                    Section = "General Merchandise",
                    Aisle = "C2",
                    Shelf = "Middle",
                    Description = $"General location for {product}"
                }
            };
        }
    }

    #region JSON & utility helpers

    private static string BuildLocationPrompt(string product) => @$"
You are an assistant that helps customers locate DIY products within a hardware store.

Return a JSON object with the following structure:
{{
    ""storeLocations"": [
        {{ ""section"": string, ""aisle"": string, ""shelf"": string, ""description"": string }},
        ...
    ]
}}

Provide at least one location. Use concise descriptions and keep aisle identifiers short (e.g., 'A1').
If a specific detail is unknown, omit the property or leave it as an empty string.

Product query: ""{product}""
";

    private static bool TryParseLocationResult(string agentResponse, out LocationResult result)
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
            if (!document.RootElement.TryGetProperty("storeLocations", out var locationsElement))
            {
                return false;
            }

            var locations = ParseStoreLocations(locationsElement);
            if (locations.Length == 0)
            {
                return false;
            }

            result = new LocationResult
            {
                StoreLocations = locations
            };
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static StoreLocation[] ParseStoreLocations(JsonElement arrayElement)
    {
        if (arrayElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<StoreLocation>();
        }

        var results = new List<StoreLocation>();

        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var section = item.TryGetProperty("section", out var sectionProp) && sectionProp.ValueKind == JsonValueKind.String
                ? sectionProp.GetString() ?? string.Empty
                : string.Empty;

            var aisle = item.TryGetProperty("aisle", out var aisleProp) && aisleProp.ValueKind == JsonValueKind.String
                ? aisleProp.GetString() ?? string.Empty
                : string.Empty;

            var shelf = item.TryGetProperty("shelf", out var shelfProp) && shelfProp.ValueKind == JsonValueKind.String
                ? shelfProp.GetString() ?? string.Empty
                : string.Empty;

            var description = item.TryGetProperty("description", out var descriptionProp) && descriptionProp.ValueKind == JsonValueKind.String
                ? descriptionProp.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(section) && string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            results.Add(new StoreLocation
            {
                Section = section,
                Aisle = aisle,
                Shelf = shelf,
                Description = description
            });
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