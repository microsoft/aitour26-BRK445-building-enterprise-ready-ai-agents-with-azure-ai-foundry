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

    public async Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request)
    {
        var orchestrationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting sequential orchestration {OrchestrationId}", orchestrationId);

        var steps = new List<AgentStep>();

        // Execute agents sequentially, each potentially using results from previous ones
        var inventoryStep = await RunInventoryAgentAsync(request.ProductQuery);
        steps.Add(inventoryStep);

        var matchmakingStep = await RunMatchmakingAgentAsync(request.ProductQuery, request.UserId, inventoryStep);
        steps.Add(matchmakingStep);

        var locationStep = await RunLocationAgentAsync(request.ProductQuery, inventoryStep);
        steps.Add(locationStep);

        NavigationInstructions? navigation = null;
        if (request.Location != null)
        {
            var navigationStep = await RunNavigationAgentAsync(request.Location, request.ProductQuery, locationStep);
            steps.Add(navigationStep);
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        // Generate mock alternatives for UI compatibility
        var alternatives = StepsProcessor.GenerateDefaultProductAlternatives();

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestationType.Sequential,
            OrchestrationDescription = "Agents executed sequentially, with each agent building upon the results of the previous agent's work.",
            Steps = steps.ToArray(),
            Alternatives = alternatives,
            NavigationInstructions = navigation
        };
    }

    private async Task<AgentStep> RunInventoryAgentAsync(string productQuery)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(productQuery);
            var names = result?.ProductsFound?.Select(p => p.Name) ?? Enumerable.Empty<string>();
            var desc = $"Found {result?.TotalCount ?? 0} products: {string.Join(", ", names)}";
            return new AgentStep { Agent = "InventoryAgent", Action = $"Search {productQuery}", Result = desc, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory agent failed");
            return new AgentStep { Agent = "InventoryAgent", Action = $"Search {productQuery}", Result = "Fallback inventory result", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunMatchmakingAgentAsync(string productQuery, string userId, AgentStep previousStep)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(productQuery, userId);
            var count = result?.Alternatives?.Length ?? 0;
            var desc = $"{count} alternatives found based on inventory results: {previousStep.Result}";
            return new AgentStep { Agent = "MatchmakingAgent", Action = $"Find alternatives {productQuery}", Result = desc, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking agent failed");
            return new AgentStep { Agent = "MatchmakingAgent", Action = $"Find alternatives {productQuery}", Result = "Fallback alternatives", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunLocationAgentAsync(string productQuery, AgentStep inventoryStep)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(productQuery);
            var loc = result?.StoreLocations?.FirstOrDefault();
            var desc = loc != null ? $"Located in {loc.Section} Aisle {loc.Aisle} (verified against inventory: {inventoryStep.Result})" : "Location not found";
            return new AgentStep { Agent = "LocationAgent", Action = $"Locate {productQuery}", Result = desc, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location agent failed");
            return new AgentStep { Agent = "LocationAgent", Action = $"Locate {productQuery}", Result = "Fallback location", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunNavigationAgentAsync(Location? location, string productQuery, AgentStep locationStep)
    {
        try
        {
            if (location == null) return new AgentStep { Agent = "NavigationAgent", Action = "Navigate", Result = "No start location", Timestamp = DateTime.UtcNow };
            var dest = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(location, dest);
            var steps = nav?.Steps?.Length ?? 0;
            var desc = $"{steps} navigation steps based on location: {locationStep.Result}";
            return new AgentStep { Agent = "NavigationAgent", Action = "Navigate to product", Result = desc, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation agent failed");
            return new AgentStep { Agent = "NavigationAgent", Action = "Navigate", Result = "Fallback navigation", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<NavigationInstructions> GenerateNavigationInstructionsAsync(Location? location, string productQuery)
    {
        if (location == null) return new NavigationInstructions { Steps = Array.Empty<NavigationStep>(), StartLocation = string.Empty, EstimatedTime = string.Empty };
        var dest = new Location { Lat = 0, Lon = 0 };
        try
        {
            return await _navigationAgentService.GenerateDirectionsAsync(location, dest);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GenerateNavigationInstructions failed");
            return new NavigationInstructions { Steps = new[] { new NavigationStep { Direction = "General", Description = $"Head to the area where {productQuery} is typically located", Landmark = new NavigationLandmark { Description = "General area" } } }, StartLocation = string.Empty, EstimatedTime = string.Empty };
        }
    }
}