#pragma warning disable SKEXP0110

using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Shared.Models;
using SharedEntities;
using System.Text.Json;
using ZavaAIFoundrySKAgentsProvider;
using ZavaAgentFxAgentsProvider;

namespace CustomerInformationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly ILogger<CustomerController> _logger;
    private readonly AIFoundryAgentProvider _aIFoundryAgentProvider;
    private readonly AgentFxAgentProvider _agentFxAgentProvider;
    private AzureAIAgent _agent;

    private static readonly Dictionary<string, CustomerInformation> _customers = new()
    {
        { "1", new CustomerInformation { Id = "1", Name = "John Smith", OwnedTools = new[] { "hammer", "screwdriver", "measuring tape" }, Skills = new[] { "basic DIY", "painting" } } },
        { "2", new CustomerInformation { Id = "2", Name = "Sarah Johnson", OwnedTools = new[] { "drill", "saw", "level", "hammer" }, Skills = new[] { "intermediate DIY", "woodworking", "tiling" } } },
        { "3", new CustomerInformation { Id = "3", Name = "Mike Davis", OwnedTools = new[] { "basic toolkit" }, Skills = new[] { "beginner DIY" } } }
    };

    public CustomerController(ILogger<CustomerController> logger,
        AIFoundryAgentProvider aIFoundryAgentProvider,
        AgentFxAgentProvider agentFxAgentProvider)
    {
        _logger = logger;
        _aIFoundryAgentProvider = aIFoundryAgentProvider;
        _agentFxAgentProvider = agentFxAgentProvider;
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

    private string BuildCustomerPrompt(string customerId)
    {
        return $@"Return a single JSON object (no surrounding text) that matches the shape of the CustomerInformation model. Find the customer information for the following customer ID: {customerId} and return the customer data as JSON.";
    }

    [HttpGet("{customerId}/sk")]
    public async Task<ActionResult<CustomerInformation>> GetCustomerSK(string customerId)
    {
        try
        {
            _logger.LogInformation("[SK] Getting customer information for ID: {CustomerId}", customerId);

            var aiPrompt = BuildCustomerPrompt(customerId);

            // Create a Semantic Kernel agent based on the agent definition
            var agentResponse = string.Empty;
            _agent = await _aIFoundryAgentProvider.GetAzureAIAgent();
            AzureAIAgentThread agentThread = new(_agent.Client);
            await foreach (ChatMessageContent response in _agent.InvokeAsync(aiPrompt, agentThread))
            {
                _logger.LogInformation("[SK] Received response from agent: {Content}", response.Content);
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
                            _logger.LogInformation("[SK] Successfully deserialized customer from agent for ID: {CustomerId}", customerFromAgent.Id);
                            return Ok(customerFromAgent);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[SK] Failed to deserialize agent response into CustomerInformation. Falling back to local data for ID: {CustomerId}", customerId);
            }

            return GetFallbackCustomer(customerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SK] Error getting customer information for ID: {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving customer information");
        }
    }

    [HttpGet("{customerId}/agentfx")]
    public async Task<ActionResult<CustomerInformation>> GetCustomerAgentFx(string customerId)
    {
        try
        {
            _logger.LogInformation("[AgentFx] Getting customer information for ID: {CustomerId}", customerId);

            var aiPrompt = BuildCustomerPrompt(customerId);

            try
            {
                // Use Microsoft Agent Framework for customer lookup
                _logger.LogInformation("[AgentFx] Using Microsoft Agent Framework for customer lookup");
                var agent = await _agentFxAgentProvider.GetAzureAIAgent();
                var thread = agent.GetNewThread();
                
                try
                {
                    var response = await agent.RunAsync(aiPrompt, thread);
                    var agentResponse = response?.Text ?? string.Empty;
                    _logger.LogInformation("[AgentFx] Received response from agent");

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
                                    _logger.LogInformation("[AgentFx] Successfully parsed customer information from agent response");
                                    return Ok(customerFromAgent);
                                }
                            }
                        }
                        _logger.LogWarning("[AgentFx] Could not parse customer JSON from agent, using fallback");
                    }
                    catch (JsonException jex)
                    {
                        _logger.LogWarning(jex, "[AgentFx] JSON parsing error, using fallback. Raw output: {Output}", agentResponse);
                    }
                }
                finally
                {
                    // Clean up the agent thread to avoid resource leaks if needed
                }
                
                return GetFallbackCustomer(customerId);
            }
            catch (Exception aiEx)
            {
                _logger.LogWarning(aiEx, "[AgentFx] Agent Framework invocation failed, using fallback");
                return GetFallbackCustomer(customerId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AgentFx] Error getting customer information for ID: {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while retrieving customer information");
        }
    }

    private ActionResult<CustomerInformation> GetFallbackCustomer(string customerId)
    {
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

    [HttpPost("match-tools/sk")]
    public ActionResult<ToolMatchResult> MatchToolsSK([FromBody] ToolMatchRequest request)
    {
        _logger.LogInformation("[SK] Matching tools for customer {CustomerId}", request.CustomerId);
        return MatchToolsImpl(request);
    }

    [HttpPost("match-tools/agentfx")]
    public ActionResult<ToolMatchResult> MatchToolsAgentFx([FromBody] ToolMatchRequest request)
    {
        _logger.LogInformation("[AgentFx] Matching tools for customer {CustomerId}", request.CustomerId);
        return MatchToolsImpl(request);
    }

    private ActionResult<ToolMatchResult> MatchToolsImpl(ToolMatchRequest request)
    {
        try
        {
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
