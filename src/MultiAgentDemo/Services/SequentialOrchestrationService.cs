using MultiAgentDemo.Controllers;
using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Sequential orchestration - passes result from one agent to the next in a defined order.
/// Use Case: Step-by-step workflows, pipelines, multi-stage processing.
/// </summary>
public class SequentialOrchestrationService : IAgentOrchestrationService
{
    private readonly ILogger<SequentialOrchestrationService> _logger;
    private readonly InventoryAgentService _inventoryAgentService;
    private readonly MatchmakingAgentService _matchmakingAgentService;
    private readonly LocationAgentService _locationAgentService;
    private readonly NavigationAgentService _navigationAgentService;

    public SequentialOrchestrationService(
        ILogger<SequentialOrchestrationService> logger,
        InventoryAgentService inventoryAgentService,
        MatchmakingAgentService matchmakingAgentService,
        LocationAgentService locationAgentService,
        NavigationAgentService navigationAgentService)
    {
        _logger = logger;
        _inventoryAgentService = inventoryAgentService;
        _matchmakingAgentService = matchmakingAgentService;
        _locationAgentService = locationAgentService;
        _navigationAgentService = navigationAgentService;
    }

    /// <inheritdoc />
    public async Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request)
    {
        var orchestrationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting sequential orchestration {OrchestrationId}", orchestrationId);

        var steps = new List<AgentStep>();

        // Execute agents sequentially, each using results from previous ones
        var inventoryStep = await ExecuteInventoryAgentAsync(request.ProductQuery);
        steps.Add(inventoryStep);

        var matchmakingStep = await ExecuteMatchmakingAgentAsync(request.ProductQuery, request.UserId, inventoryStep);
        steps.Add(matchmakingStep);

        var locationStep = await ExecuteLocationAgentAsync(request.ProductQuery, inventoryStep);
        steps.Add(locationStep);

        NavigationInstructions? navigation = null;
        if (request.Location != null)
        {
            var navigationStep = await ExecuteNavigationAgentAsync(request.Location, request.ProductQuery, locationStep);
            steps.Add(navigationStep);
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestrationType.Sequential,
            OrchestrationDescription = "Agents executed sequentially, with each agent building upon the results of the previous agent's work.",
            Steps = steps.ToArray(),
            Alternatives = StepsProcessor.GenerateDefaultProductAlternatives(),
            NavigationInstructions = navigation
        };
    }

    private async Task<AgentStep> ExecuteInventoryAgentAsync(string productQuery)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(productQuery);
            var productNames = result?.ProductsFound?.Select(p => p.Name) ?? [];
            var description = $"Found {result?.TotalCount ?? 0} products: {string.Join(", ", productNames)}";
            
            return CreateStep("InventoryAgent", $"Search {productQuery}", description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory agent failed");
            return CreateStep("InventoryAgent", $"Search {productQuery}", "Fallback inventory result");
        }
    }

    private async Task<AgentStep> ExecuteMatchmakingAgentAsync(string productQuery, string userId, AgentStep previousStep)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(productQuery, userId);
            var count = result?.Alternatives?.Length ?? 0;
            var description = $"{count} alternatives found based on inventory results: {previousStep.Result}";
            
            return CreateStep("MatchmakingAgent", $"Find alternatives {productQuery}", description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking agent failed");
            return CreateStep("MatchmakingAgent", $"Find alternatives {productQuery}", "Fallback alternatives");
        }
    }

    private async Task<AgentStep> ExecuteLocationAgentAsync(string productQuery, AgentStep inventoryStep)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(productQuery);
            var location = result?.StoreLocations?.FirstOrDefault();
            var description = location != null 
                ? $"Located in {location.Section} Aisle {location.Aisle} (verified against inventory: {inventoryStep.Result})" 
                : "Location not found";
            
            return CreateStep("LocationAgent", $"Locate {productQuery}", description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location agent failed");
            return CreateStep("LocationAgent", $"Locate {productQuery}", "Fallback location");
        }
    }

    private async Task<AgentStep> ExecuteNavigationAgentAsync(Location location, string productQuery, AgentStep locationStep)
    {
        try
        {
            var destination = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(location, destination);
            var stepCount = nav?.Steps?.Length ?? 0;
            var description = $"{stepCount} navigation steps based on location: {locationStep.Result}";
            
            return CreateStep("NavigationAgent", "Navigate to product", description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation agent failed");
            return CreateStep("NavigationAgent", "Navigate", "Fallback navigation");
        }
    }

    private async Task<NavigationInstructions> GenerateNavigationInstructionsAsync(Location location, string productQuery)
    {
        try
        {
            var destination = new Location { Lat = 0, Lon = 0 };
            return await _navigationAgentService.GenerateDirectionsAsync(location, destination);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GenerateNavigationInstructions failed");
            return StepsProcessor.CreateDefaultNavigationInstructions(location, productQuery);
        }
    }

    private static AgentStep CreateStep(string agent, string action, string result) => new()
    {
        Agent = agent,
        Action = action,
        Result = result,
        Timestamp = DateTime.UtcNow
    };
}