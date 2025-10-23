using MultiAgentDemo.Controllers;
using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Concurrent orchestration - broadcasts a task to all agents, collects results independently.
/// Use Case: Parallel analysis, independent subtasks, ensemble decision making.
/// </summary>
public class ConcurrentOrchestrationService : IAgentOrchestrationService
{
    private readonly ILogger<ConcurrentOrchestrationService> _logger;
    private readonly InventoryAgentService _inventoryAgentService;
    private readonly MatchmakingAgentService _matchmakingAgentService;
    private readonly LocationAgentService _locationAgentService;
    private readonly NavigationAgentService _navigationAgentService;

    public ConcurrentOrchestrationService(
        ILogger<ConcurrentOrchestrationService> logger,
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
        _logger.LogInformation("Starting concurrent orchestration {OrchestrationId}", orchestrationId);

        var startTime = DateTime.UtcNow;

        // Execute all agents concurrently using Task.WhenAll
        var tasks = new List<Task<AgentStep>>
        {
            RunInventoryAgentAsync(request.ProductQuery, startTime),
            RunMatchmakingAgentAsync(request.ProductQuery, request.UserId, startTime),
            RunLocationAgentAsync(request.ProductQuery, startTime)
        };

        // Add navigation task if location is provided
        if (request.Location != null)
        {
            tasks.Add(RunNavigationAgentAsync(request.Location, request.ProductQuery, startTime));
        }

        // Wait for all agents to complete
        var agentResults = await Task.WhenAll(tasks);
        var steps = agentResults.ToList();

        // Generate navigation instructions if location provided
        NavigationInstructions? navigation = null;
        if (request.Location != null)
        {
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }
        var alternatives = StepsProcessor.GenerateDefaultProductAlternatives();


        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestrationType.Concurrent,
            OrchestrationDescription = "All agents executed concurrently in parallel, providing independent analysis without dependencies on each other's results.",
            Steps = steps.ToArray(),
            Alternatives = alternatives,
            NavigationInstructions = navigation
        };
    }

    private async Task<AgentStep> RunInventoryAgentAsync(string productQuery, DateTime baseTime)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(productQuery);
            var names = result?.ProductsFound?.Select(p => p.Name) ?? Enumerable.Empty<string>();
            var desc = $"Concurrent search found {result?.TotalCount ?? 0} products: {string.Join(", ", names)}";
            return new AgentStep { Agent = "InventoryAgent", Action = $"Concurrent search {productQuery}", Result = desc, Timestamp = baseTime };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory agent failed in concurrent execution");
            return new AgentStep { Agent = "InventoryAgent", Action = $"Concurrent search {productQuery}", Result = "Concurrent fallback inventory result", Timestamp = baseTime };
        }
    }

    private async Task<AgentStep> RunMatchmakingAgentAsync(string productQuery, string userId, DateTime baseTime)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(productQuery, userId);
            var count = result?.Alternatives?.Length ?? 0;
            var desc = $"Concurrent analysis found {count} independent alternatives";
            return new AgentStep { Agent = "MatchmakingAgent", Action = $"Concurrent alternatives {productQuery}", Result = desc, Timestamp = baseTime.AddMilliseconds(100) };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking agent failed in concurrent execution");
            return new AgentStep { Agent = "MatchmakingAgent", Action = $"Concurrent alternatives {productQuery}", Result = "Concurrent fallback alternatives", Timestamp = baseTime.AddMilliseconds(100) };
        }
    }

    private async Task<AgentStep> RunLocationAgentAsync(string productQuery, DateTime baseTime)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(productQuery);
            var loc = result?.StoreLocations?.FirstOrDefault();
            var desc = loc != null ? $"Concurrent location search: {loc.Section} Aisle {loc.Aisle}" : "Concurrent location not found";
            return new AgentStep { Agent = "LocationAgent", Action = $"Concurrent locate {productQuery}", Result = desc, Timestamp = baseTime.AddMilliseconds(200) };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location agent failed in concurrent execution");
            return new AgentStep { Agent = "LocationAgent", Action = $"Concurrent locate {productQuery}", Result = "Concurrent fallback location", Timestamp = baseTime.AddMilliseconds(200) };
        }
    }

    private async Task<AgentStep> RunNavigationAgentAsync(Location? location, string productQuery, DateTime baseTime)
    {
        try
        {
            if (location == null) return new AgentStep { Agent = "NavigationAgent", Action = "Concurrent navigate", Result = "No start location", Timestamp = baseTime.AddMilliseconds(300) };
            var dest = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(location, dest);
            var steps = nav?.Steps?.Length ?? 0;
            var desc = $"Concurrent navigation: {steps} independent route steps";
            return new AgentStep { Agent = "NavigationAgent", Action = "Concurrent navigate to product", Result = desc, Timestamp = baseTime.AddMilliseconds(300) };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation agent failed in concurrent execution");
            return new AgentStep { Agent = "NavigationAgent", Action = "Concurrent navigate", Result = "Concurrent fallback navigation", Timestamp = baseTime.AddMilliseconds(300) };
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