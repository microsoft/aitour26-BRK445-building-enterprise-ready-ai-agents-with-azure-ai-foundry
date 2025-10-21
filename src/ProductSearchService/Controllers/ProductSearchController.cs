#pragma warning disable SKEXP0110

using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Shared.Models;
using SharedEntities;
using ZavaAIFoundrySKAgentsProvider;
using ZavaAgentFxAgentsProvider;
using static Microsoft.SemanticKernel.Agents.AzureAI.AzureAIAgent;

namespace ProductSearchService.Controllers;

[ApiController]
[Route("api")]
public class ProductSearchController : ControllerBase
{
    private readonly ILogger<ProductSearchController> _logger;
    private readonly AIFoundryAgentProvider _aIFoundryAgentProvider;
    private readonly AgentFxAgentProvider _agentFxAgentProvider;
    private AzureAIAgent _agent;

    public ProductSearchController(
        ILogger<ProductSearchController> logger,
        AIFoundryAgentProvider aIFoundryAgentProvider,
        AgentFxAgentProvider agentFxAgentProvider)
    {
        _logger = logger;
        _aIFoundryAgentProvider = aIFoundryAgentProvider;
        _agentFxAgentProvider = agentFxAgentProvider;
    }

    [HttpPost("search/sk")]
    public async Task<ActionResult<ToolRecommendation[]>> SearchInventorySkAsync([FromBody] InventorySearchRequest request)
    {
        _logger.LogInformation("[SK] Searching inventory for search Query: {SearchQuery}", request.SearchQuery);
        return await ProductSearchInternalAsync(request, useSK: true);
    }

    [HttpPost("search/agentfx")]
    public async Task<ActionResult<ToolRecommendation[]>> SearchInventoryAgentFxAsync([FromBody] InventorySearchRequest request)
    {
        _logger.LogInformation("[AgentFx] Searching inventory for search Query: {SearchQuery}", request.SearchQuery);
        return await ProductSearchInternalAsync(request, useSK: false);
    }

    private async Task<ActionResult<ToolRecommendation[]>> ProductSearchInternalAsync(InventorySearchRequest request, bool useSK)
    {
        try
        {
            var aiPrompt = BuildProductSearchPrompt(request.SearchQuery);

            var agentResponse = string.Empty;

            if (useSK)
            {
                // Use Semantic Kernel agent
                _agent = await _aIFoundryAgentProvider.GetAzureAIAgent();
                AzureAIAgentThread agentThread = new(client: _agent.Client);
                await foreach (ChatMessageContent response in _agent.InvokeAsync(aiPrompt, agentThread))
                {
                    _logger.LogInformation("[SK] Received response from agent: {Content}", response.Content);
                    agentResponse += (response.Content);
                }
            }
            else
            {
                // Use AgentFx agent
                try
                {
                    _logger.LogInformation("[AgentFx] Using Microsoft Agent Framework for inventory search");
                    var agent = await _agentFxAgentProvider.GetAzureAIAgent();
                    var thread = agent.GetNewThread();

                    try
                    {
                        var response = await agent.RunAsync(aiPrompt, thread);
                        agentResponse = response?.Text ?? string.Empty;
                        _logger.LogInformation("[AgentFx] Received response from agent: {Content}", agentResponse);
                    }
                    finally
                    {
                        // Clean up the agent thread to avoid resource leaks if needed
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[AgentFx] Agent Framework invocation failed, using fallback");
                    agentResponse = await GetFallbackInventorySearch(request.SearchQuery);
                }
            }

            // If the agent returned exactly a single comma, treat that as "no results".
            var skusFromAgent = agentResponse?.Split(',')
                .Select(s => s.Trim())
                .Where(s => s != null)
                .ToArray() ?? Array.Empty<string>();

            var results = new List<ToolRecommendation>();

            // Handle the explicit "no products" signal from the agent: a single comma or empty/whitespace response
            var responseNormalized = agentResponse?.Trim();
            var noProductsSignal = string.Equals(responseNormalized, ",", StringComparison.Ordinal);

            if (noProductsSignal || skusFromAgent.Length == 0 || skusFromAgent.All(s => string.IsNullOrWhiteSpace(s)))
            {
                // Create a default recommendation list indicating nothing was found.
                results.Add(new ToolRecommendation
                {
                    Name = "No matching products found",
                    Sku = string.Empty,
                    IsAvailable = false,
                    Price = 0m,
                    Description = $"No products matched the query: '{request.SearchQuery}'"
                });
            }
            else
            {
                // Filter out any empty entries that may appear if the agent returned leading/trailing commas
                var filteredSkus = skusFromAgent
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();

                results = GetDefaultListOfRecommendations(filteredSkus);
            }

            return Ok(results.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching inventory");
            return StatusCode(500, "An error occurred while searching inventory");
        }
    }

    private string BuildProductSearchPrompt(string searchQuery)
    {
        return @$"
# Context
User Query: {searchQuery}

# Tasks
Search for products that may match the user query.
Analyze the user query and extract the product name or SKU that the user is referring to.

Return ONLY the product SKU identifiers, separated by commas, with no additional text, explanation, or formatting.  
If there are NO matching products, return a string that contains only a single comma: ','  
Example response: 'PAINT-ROLLER-9IN,BRUSH-SET-3PC,SAW-CIRCULAR-7IN'  
";
    }

    private async Task<string> GetFallbackInventorySearch(string searchQuery)
    {
        // Fallback logic for AgentFx - returns basic matching SKUs
        await Task.Delay(100); // Simulate processing

        var queryLower = searchQuery.ToLower();
        var matchedSkus = new List<string>();

        if (queryLower.Contains("paint") || queryLower.Contains("roller"))
            matchedSkus.Add("PAINT-ROLLER-9IN");
        if (queryLower.Contains("brush"))
            matchedSkus.Add("BRUSH-SET-3PC");
        if (queryLower.Contains("saw") || queryLower.Contains("cut"))
            matchedSkus.Add("SAW-CIRCULAR-7IN");
        if (queryLower.Contains("drill"))
            matchedSkus.Add("DRILL-CORDLESS");

        return matchedSkus.Count > 0 ? string.Join(",", matchedSkus) : ",";
    }

    public ToolRecommendation GetToolRecommendation(string sku)
    {

        ToolRecommendation toolRecommendation = new();

        if (_inventory.TryGetValue(sku, out var item))
        {
            // Simulate some dynamic pricing and availability
            toolRecommendation = new ToolRecommendation
            {
                Name = item.Name,
                Sku = item.Sku,
                IsAvailable = item.IsAvailable && Random.Shared.NextDouble() > 0.1, // 10% chance of being out of stock
                Price = item.Price * (decimal)(0.9 + Random.Shared.NextDouble() * 0.2), // Price variation ±10%
                Description = item.Description
            };
        }
        return toolRecommendation;
    }

    public List<ToolRecommendation> GetDefaultListOfRecommendations(string[] skus)
    {
        var results = new List<ToolRecommendation>();

        foreach (var sku in skus)
        {
            if (_inventory.TryGetValue(sku, out var item))
            {
                // Simulate some dynamic pricing and availability
                var enrichedItem = new ToolRecommendation
                {
                    Name = item.Name,
                    Sku = item.Sku,
                    IsAvailable = item.IsAvailable && Random.Shared.NextDouble() > 0.1, // 10% chance of being out of stock
                    Price = item.Price * (decimal)(0.9 + Random.Shared.NextDouble() * 0.2), // Price variation ±10%
                    Description = item.Description
                };
                results.Add(enrichedItem);
            }
            else
            {
                // Create a generic item for unknown SKUs
                results.Add(new ToolRecommendation
                {
                    Name = $"Tool for SKU {sku}",
                    Sku = sku,
                    IsAvailable = false,
                    Price = 29.99m,
                    Description = $"Product not found in current inventory"
                });
            }
        }

        return results;
    }

    [HttpGet("search/{sku}")]
    public ActionResult<ToolRecommendation> GetItem(string sku)
    {
        try
        {
            _logger.LogInformation("Getting inventory item for SKU: {Sku}", sku);

            if (_inventory.TryGetValue(sku, out var item))
            {
                return Ok(item);
            }

            return NotFound($"Item with SKU {sku} not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory item for SKU: {Sku}", sku);
            return StatusCode(500, "An error occurred while retrieving the item");
        }
    }

    [HttpGet("available")]
    public ActionResult<ToolRecommendation[]> GetAvailableItems()
    {
        try
        {
            _logger.LogInformation("Getting all available inventory items");

            var availableItems = _inventory.Values.Where(item => item.IsAvailable).ToArray();
            return Ok(availableItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available inventory items");
            return StatusCode(500, "An error occurred while retrieving available items");
        }
    }

    [HttpPost("check-availability")]
    public async Task<ActionResult<Dictionary<string, bool>>> CheckAvailabilityAsync([FromBody] string[] skus)
    {
        try
        {
            _logger.LogInformation("Checking availability for {Count} SKUs", skus.Length);

            // Simulate availability check delay
            await Task.Delay(300);

            var availability = new Dictionary<string, bool>();

            foreach (var sku in skus)
            {
                if (_inventory.TryGetValue(sku, out var item))
                {
                    availability[sku] = item.IsAvailable;
                }
                else
                {
                    availability[sku] = false;
                }
            }

            return Ok(availability);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability");
            return StatusCode(500, "An error occurred while checking availability");
        }
    }

    private static readonly Dictionary<string, ToolRecommendation> _inventory = new()
    {
        { "PAINT-ROLLER-9IN", new ToolRecommendation { Name = "Paint Roller", Sku = "PAINT-ROLLER-9IN", IsAvailable = true, Price = 12.99m, Description = "9-inch paint roller for smooth walls" } },
        { "BRUSH-SET-3PC", new ToolRecommendation { Name = "Paint Brush Set", Sku = "BRUSH-SET-3PC", IsAvailable = true, Price = 24.99m, Description = "3-piece brush set for detail work" } },
        { "DROP-CLOTH-9X12", new ToolRecommendation { Name = "Drop Cloth", Sku = "DROP-CLOTH-9X12", IsAvailable = true, Price = 8.99m, Description = "Plastic drop cloth protection" } },
        { "SAW-CIRCULAR-7IN", new ToolRecommendation { Name = "Circular Saw", Sku = "SAW-CIRCULAR-7IN", IsAvailable = true, Price = 89.99m, Description = "7.25-inch circular saw for wood cutting" } },
        { "STAIN-WOOD-QT", new ToolRecommendation { Name = "Wood Stain", Sku = "STAIN-WOOD-QT", IsAvailable = false, Price = 15.99m, Description = "1-quart wood stain in natural color" } },
        { "SAFETY-GLASSES", new ToolRecommendation { Name = "Safety Glasses", Sku = "SAFETY-GLASSES", IsAvailable = true, Price = 5.99m, Description = "Safety glasses for eye protection" } },
        { "GLOVES-WORK-L", new ToolRecommendation { Name = "Work Gloves", Sku = "GLOVES-WORK-L", IsAvailable = true, Price = 7.99m, Description = "Heavy-duty work gloves" } },
        { "DRILL-CORDLESS", new ToolRecommendation { Name = "Cordless Drill", Sku = "DRILL-CORDLESS", IsAvailable = true, Price = 79.99m, Description = "18V cordless drill with battery" } },
        { "LEVEL-2FT", new ToolRecommendation { Name = "Level", Sku = "LEVEL-2FT", IsAvailable = true, Price = 19.99m, Description = "2-foot aluminum level" } },
        { "TILE-CUTTER", new ToolRecommendation { Name = "Tile Cutter", Sku = "TILE-CUTTER", IsAvailable = false, Price = 45.99m, Description = "Manual tile cutting tool" } }
    };

}
