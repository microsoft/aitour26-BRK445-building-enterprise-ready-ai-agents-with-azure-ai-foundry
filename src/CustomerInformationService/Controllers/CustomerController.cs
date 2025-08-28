#pragma warning disable SKEXP0110

using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Shared.Models;
using SharedEntities;
using System.Text.Json;
using ZavaAIFoundrySKAgentsProvider;

namespace CustomerInformationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ILogger<CustomerController> _logger;
    private readonly AIFoundryAgentProvider _aIFoundryAgentProvider;
    private AzureAIAgent _agent;

    private static readonly Dictionary<string, CustomerInformation> _customers = new()
    {
        { "1", new CustomerInformation { Id = "1", Name = "John Smith", OwnedTools = new[] { "hammer", "screwdriver", "measuring tape" }, Skills = new[] { "basic DIY", "painting" } } },
        { "2", new CustomerInformation { Id = "2", Name = "Sarah Johnson", OwnedTools = new[] { "drill", "saw", "level", "hammer" }, Skills = new[] { "intermediate DIY", "woodworking", "tiling" } } },
        { "3", new CustomerInformation { Id = "3", Name = "Mike Davis", OwnedTools = new[] { "basic toolkit" }, Skills = new[] { "beginner DIY" } } }
    };

    public CustomerController(ILogger<CustomerController> logger,
        AIFoundryAgentProvider aIFoundryAgentProvider)
    {
        _logger = logger;
        _aIFoundryAgentProvider = aIFoundryAgentProvider;
    }

    // Extracts the first top-level JSON object from a string. Returns null if none found.
    private static string? ExtractFirstJsonObject(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;

        int start = input.IndexOf('{');
        if (start == -1) return null;

        int depth = 0;
        for (int i = start; i < input.Length; i++)
        {
            char c = input[i];
            if (c == '{') depth++;
            else if (c == '}') depth--;

            if (depth == 0)
            {
                // return substring from start to i (inclusive)
                return input.Substring(start, i - start + 1).Trim();
            }
        }

        return null;
    }

    [HttpGet("{customerId}")]
    public async Task<ActionResult<CustomerInformation>> GetCustomer(string customerId)
    {
        try
        {
            _logger.LogInformation("Getting customer information for ID: {CustomerId}", customerId);

            var aiPrompt = $@"Return a single JSON object (no surrounding text) that matches the shape of the CustomerInformation model. Find the customer information for the following customer ID: {customerId} and return the customer data as JSON.";

            // Create a Semantic Kernel agent based on the agent definition
            var agentResponse = string.Empty;
            _agent = await _aIFoundryAgentProvider.GetAzureAIAgent();
            AzureAIAgentThread agentThread = new(_agent.Client);
            await foreach (ChatMessageContent response in _agent.InvokeAsync(aiPrompt, agentThread))
            {
                _logger.LogInformation("Received response from agent: {Content}", response.Content);
                agentResponse += (response.Content);
            }

            // Try to deserialize the agent response into a CustomerInformation object.
            try
            {
                if (!string.IsNullOrWhiteSpace(agentResponse))
                {
                    // Attempt to extract a JSON object from the agent response in case the agent added surrounding text
                    string json = ExtractFirstJsonObject(agentResponse);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var customerFromAgent = JsonSerializer.Deserialize<CustomerInformation>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (customerFromAgent != null && !string.IsNullOrWhiteSpace(customerFromAgent.Id))
                        {
                            _logger.LogInformation("Successfully deserialized customer from agent for ID: {CustomerId}", customerFromAgent.Id);
                            return Ok(customerFromAgent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize agent response into CustomerInformation. Falling back to local data for ID: {CustomerId}", customerId);
            }

            if (_customers.TryGetValue(customerId, out var customer))
            {
                return Ok(customer);
            }

            // Return a fallback customer if not found
            var fallbackCustomer = new CustomerInformation
            {
                Id = customerId,
                Name = $"Customer {customerId}",
                OwnedTools = new[] { "measuring tape", "basic hand tools" },
                Skills = new[] { "basic DIY" }
            };

            _logger.LogInformation("Customer not found, returning fallback customer for ID: {CustomerId}", customerId);
            return Ok(fallbackCustomer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer information for ID: {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving customer information");
        }
    }

    [HttpPost("match-tools")]
    public ActionResult<ToolMatchResult> MatchTools([FromBody] ToolMatchRequest request)
    {
        try
        {
            _logger.LogInformation("Matching tools for customer {CustomerId}", request.CustomerId);

            if (!_customers.TryGetValue(request.CustomerId, out var customer))
            {
                customer = new CustomerInformation
                {
                    Id = request.CustomerId,
                    Name = $"Customer {request.CustomerId}",
                    OwnedTools = new[] { "measuring tape", "basic hand tools" },
                    Skills = new[] { "basic DIY" }
                };
            }

            var reusableTools = DetermineReusableTools(customer.OwnedTools, request.DetectedMaterials, request.Prompt);
            var missingTools = DetermineMissingTools(customer.OwnedTools, request.DetectedMaterials, request.Prompt);

            var result = new ToolMatchResult
            {
                ReusableTools = reusableTools,
                MissingTools = missingTools
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error matching tools for customer {CustomerId}", request.CustomerId);
            return StatusCode(500, "An error occurred while matching tools");
        }
    }

    private string[] DetermineReusableTools(string[] ownedTools, string[] detectedMaterials, string prompt)
    {
        var reusable = new List<string>();
        var promptLower = prompt.ToLower();

        foreach (var tool in ownedTools)
        {
            var toolLower = tool.ToLower();

            // Always useful tools
            if (toolLower.Contains("measuring tape") || toolLower.Contains("screwdriver") || toolLower.Contains("hammer"))
                reusable.Add(tool);

            // Context-specific tools
            if (promptLower.Contains("paint") && toolLower.Contains("brush"))
                reusable.Add(tool);

            if (promptLower.Contains("wood") && (toolLower.Contains("saw") || toolLower.Contains("drill")))
                reusable.Add(tool);
        }

        return reusable.ToArray();
    }

    private ToolRecommendation[] DetermineMissingTools(string[] ownedTools, string[] detectedMaterials, string prompt)
    {
        var missing = new List<ToolRecommendation>();
        var promptLower = prompt.ToLower();
        var ownedToolsLower = ownedTools.Select(t => t.ToLower()).ToArray();

        // Paint-related tools
        if (promptLower.Contains("paint") || detectedMaterials.Any(m => m.Contains("paint")))
        {
            if (!ownedToolsLower.Any(t => t.Contains("roller")))
                missing.Add(new ToolRecommendation { Name = "Paint Roller", Sku = "PAINT-ROLLER-9IN", IsAvailable = true, Price = 12.99m, Description = "9-inch paint roller for smooth walls" });

            if (!ownedToolsLower.Any(t => t.Contains("brush")))
                missing.Add(new ToolRecommendation { Name = "Paint Brush Set", Sku = "BRUSH-SET-3PC", IsAvailable = true, Price = 24.99m, Description = "3-piece brush set for detail work" });

            missing.Add(new ToolRecommendation { Name = "Drop Cloth", Sku = "DROP-CLOTH-9X12", IsAvailable = true, Price = 8.99m, Description = "Plastic drop cloth protection" });
        }

        // Wood-related tools
        if (promptLower.Contains("wood") || detectedMaterials.Any(m => m.Contains("wood")))
        {
            if (!ownedToolsLower.Any(t => t.Contains("saw")))
                missing.Add(new ToolRecommendation { Name = "Circular Saw", Sku = "SAW-CIRCULAR-7IN", IsAvailable = true, Price = 89.99m, Description = "7.25-inch circular saw for wood cutting" });

            missing.Add(new ToolRecommendation { Name = "Wood Stain", Sku = "STAIN-WOOD-QT", IsAvailable = true, Price = 15.99m, Description = "1-quart wood stain in natural color" });
        }

        // Default recommendations if none specific
        if (missing.Count == 0)
        {
            missing.Add(new ToolRecommendation { Name = "Safety Glasses", Sku = "SAFETY-GLASSES", IsAvailable = true, Price = 5.99m, Description = "Safety glasses for eye protection" });
            missing.Add(new ToolRecommendation { Name = "Work Gloves", Sku = "GLOVES-WORK-L", IsAvailable = true, Price = 7.99m, Description = "Heavy-duty work gloves" });
        }

        return missing.ToArray();
    }
}
