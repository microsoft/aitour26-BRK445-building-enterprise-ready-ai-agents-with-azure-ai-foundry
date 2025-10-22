#pragma warning disable SKEXP0110

using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models;
using SharedEntities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CustomerInformationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ILogger<CustomerController> _logger;
    private readonly AzureAIAgent _skAgent;
    private readonly AIAgent _agentFxAgent;

    private static readonly Dictionary<string, CustomerInformation> _customers = new()
    {
        { "1", new CustomerInformation { Id = "1", Name = "John Smith", OwnedTools = new[] { "hammer", "screwdriver", "measuring tape" }, Skills = new[] { "basic DIY", "painting" } } },
        { "2", new CustomerInformation { Id = "2", Name = "Sarah Johnson", OwnedTools = new[] { "drill", "saw", "level", "hammer" }, Skills = new[] { "intermediate DIY", "woodworking", "tiling" } } },
        { "3", new CustomerInformation { Id = "3", Name = "Mike Davis", OwnedTools = new[] { "basic toolkit" }, Skills = new[] { "beginner DIY" } } }
    };

    public CustomerController(
        ILogger<CustomerController> logger,
        AzureAIAgent skAgent,
        AIAgent agentFxAgent)
    {
        _logger = logger;
        _skAgent = skAgent;
        _agentFxAgent = agentFxAgent;
    }

    [HttpGet("{customerId}/sk")]
    public async Task<ActionResult<CustomerInformation>> GetCustomerSkAsync(string customerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SK] Getting customer information for ID: {CustomerId}", customerId);

        return await GetCustomerAsync(
            customerId,
            async (prompt, token) => await InvokeSemanticKernelAsync(prompt, token),
            "[SK]",
            cancellationToken);
    }

    [HttpGet("{customerId}/agentfx")]
    public async Task<ActionResult<CustomerInformation>> GetCustomerAgentFxAsync(string customerId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AgentFx] Getting customer information for ID: {CustomerId}", customerId);

        return await GetCustomerAsync(
            customerId,
            async (prompt, token) => await InvokeAgentFrameworkAsync(prompt, token),
            "[AgentFx]",
            cancellationToken);
    }

    [HttpPost("match-tools/sk")]
    public ActionResult<ToolMatchResult> MatchToolsSk([FromBody] ToolMatchRequest request)
    {
        _logger.LogInformation("[SK] Matching tools for customer {CustomerId}", request.CustomerId);
        return MatchToolsInternal(request);
    }

    [HttpPost("match-tools/agentfx")]
    public ActionResult<ToolMatchResult> MatchToolsAgentFx([FromBody] ToolMatchRequest request)
    {
        _logger.LogInformation("[AgentFx] Matching tools for customer {CustomerId}", request.CustomerId);
        return MatchToolsInternal(request);
    }

    private async Task<ActionResult<CustomerInformation>> GetCustomerAsync(
        string customerId,
        Func<string, CancellationToken, Task<string>> invokeAgent,
        string logPrefix,
        CancellationToken cancellationToken)
    {
        var prompt = BuildCustomerPrompt(customerId);

        try
        {
            var agentResponse = await invokeAgent(prompt, cancellationToken);
            _logger.LogInformation("{Prefix} Raw agent response length: {Length}", logPrefix, agentResponse.Length);

            if (TryParseCustomer(agentResponse, out var customer))
            {
                _logger.LogInformation("{Prefix} Returning customer {CustomerId} from agent response", logPrefix, customer.Id);
                return Ok(customer);
            }

            _logger.LogWarning("{Prefix} Parsing failed. Falling back to local store. Raw: {Raw}", logPrefix, TrimForLog(agentResponse));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Prefix} Agent invocation failed. Falling back to local store.", logPrefix);
        }

        return Ok(GetFallbackCustomer(customerId));
    }

    private ActionResult<ToolMatchResult> MatchToolsInternal(ToolMatchRequest request)
    {
        try
        {
            var customer = GetFallbackCustomer(request.CustomerId);
            var reusableTools = DetermineReusableTools(customer.OwnedTools, request.DetectedMaterials, request.Prompt);
            var missingTools = DetermineMissingTools(customer.OwnedTools, request.DetectedMaterials, request.Prompt);

            return Ok(new ToolMatchResult
            {
                ReusableTools = reusableTools,
                MissingTools = missingTools
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching tools for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, "An error occurred while matching tools");
        }
    }

    private async Task<string> InvokeSemanticKernelAsync(string prompt, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        AzureAIAgentThread agentThread = new(_skAgent.Client);
        await foreach (ChatMessageContent response in _skAgent.InvokeAsync(prompt, agentThread).WithCancellation(cancellationToken))
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

    private static CustomerInformation GetFallbackCustomer(string customerId)
    {
        if (_customers.TryGetValue(customerId, out var customer))
        {
            return customer;
        }

        return new CustomerInformation
        {
            Id = customerId,
            Name = $"Customer {customerId}",
            OwnedTools = new[] { "measuring tape", "basic hand tools" },
            Skills = new[] { "basic DIY" }
        };
    }

    private static string BuildCustomerPrompt(string customerId) =>
        $"Return a single JSON object (no surrounding text) that matches the shape of the CustomerInformation model. Find the customer information for the following customer ID: {customerId} and return the customer data as JSON.";

    private static string[] DetermineReusableTools(string[] ownedTools, string[] detectedMaterials, string prompt)
    {
        var reusable = new List<string>();
        var promptLower = prompt.ToLowerInvariant();

        foreach (var tool in ownedTools)
        {
            var toolLower = tool.ToLowerInvariant();

            if (toolLower.Contains("measuring tape") || toolLower.Contains("screwdriver") || toolLower.Contains("hammer"))
            {
                reusable.Add(tool);
            }

            if (promptLower.Contains("paint") && toolLower.Contains("brush"))
            {
                reusable.Add(tool);
            }

            if (promptLower.Contains("wood") && (toolLower.Contains("saw") || toolLower.Contains("drill")))
            {
                reusable.Add(tool);
            }
        }

        return reusable.ToArray();
    }

    private static ToolRecommendation[] DetermineMissingTools(string[] ownedTools, string[] detectedMaterials, string prompt)
    {
        var missing = new List<ToolRecommendation>();
        var promptLower = prompt.ToLowerInvariant();
        var ownedToolsLower = ownedTools.Select(t => t.ToLowerInvariant()).ToArray();

        if (promptLower.Contains("paint") || detectedMaterials.Any(m => m.Contains("paint", StringComparison.OrdinalIgnoreCase)))
        {
            if (!ownedToolsLower.Any(t => t.Contains("roller")))
            {
                missing.Add(new ToolRecommendation { Name = "Paint Roller", Sku = "PAINT-ROLLER-9IN", IsAvailable = true, Price = 12.99m, Description = "9-inch paint roller for smooth walls" });
            }

            if (!ownedToolsLower.Any(t => t.Contains("brush")))
            {
                missing.Add(new ToolRecommendation { Name = "Paint Brush Set", Sku = "BRUSH-SET-3PC", IsAvailable = true, Price = 24.99m, Description = "3-piece brush set for detail work" });
            }

            missing.Add(new ToolRecommendation { Name = "Drop Cloth", Sku = "DROP-CLOTH-9X12", IsAvailable = true, Price = 8.99m, Description = "Plastic drop cloth protection" });
        }

        if (promptLower.Contains("wood") || detectedMaterials.Any(m => m.Contains("wood", StringComparison.OrdinalIgnoreCase)))
        {
            if (!ownedToolsLower.Any(t => t.Contains("saw")))
            {
                missing.Add(new ToolRecommendation { Name = "Circular Saw", Sku = "SAW-CIRCULAR-7IN", IsAvailable = true, Price = 89.99m, Description = "7.25-inch circular saw for wood cutting" });
            }

            missing.Add(new ToolRecommendation { Name = "Wood Stain", Sku = "STAIN-WOOD-QT", IsAvailable = true, Price = 15.99m, Description = "1-quart wood stain in natural color" });
        }

        if (missing.Count == 0)
        {
            missing.Add(new ToolRecommendation { Name = "Safety Glasses", Sku = "SAFETY-GLASSES", IsAvailable = true, Price = 5.99m, Description = "Safety glasses for eye protection" });
            missing.Add(new ToolRecommendation { Name = "Work Gloves", Sku = "GLOVES-WORK-L", IsAvailable = true, Price = 7.99m, Description = "Heavy-duty work gloves" });
        }

        return missing.ToArray();
    }

    #region JSON & utility helpers

    private static bool TryParseCustomer(string agentResponse, out CustomerInformation customer)
    {
        customer = default!;
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
            var result = JsonSerializer.Deserialize<CustomerInformation>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result is null || string.IsNullOrWhiteSpace(result.Id))
            {
                return false;
            }

            customer = result;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? ExtractFirstJsonObject(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return null;
        }

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
