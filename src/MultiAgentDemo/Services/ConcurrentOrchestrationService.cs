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

    /// <inheritdoc />
    public async Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request)
    {
        var orchestrationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting concurrent orchestration {OrchestrationId}", orchestrationId);

        var startTime = DateTime.UtcNow;

        // Build list of concurrent agent tasks
        var tasks = new List<Task<AgentStep>>
        {
            ExecuteInventoryAgentAsync(request.ProductQuery, startTime),
            ExecuteMatchmakingAgentAsync(request.ProductQuery, request.UserId, startTime),
            ExecuteLocationAgentAsync(request.ProductQuery, startTime)
        };

        if (request.Location != null)
        {
            tasks.Add(ExecuteNavigationAgentAsync(request.Location, request.ProductQuery, startTime));
        }

        // Execute all agents concurrently
        var agentResults = await Task.WhenAll(tasks);
        var steps = agentResults.ToList();

        NavigationInstructions? navigation = null;
        if (request.Location != null)
        {
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestrationType.Concurrent,
            OrchestrationDescription = "All agents executed concurrently in parallel, providing independent analysis without dependencies.",
            Steps = steps.ToArray(),
            Alternatives = StepsProcessor.GenerateDefaultProductAlternatives(),
            NavigationInstructions = navigation
        };
    }

    private async Task<AgentStep> ExecuteInventoryAgentAsync(string productQuery, DateTime baseTime)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(productQuery);
            var productNames = result?.ProductsFound?.Select(p => p.Name) ?? [];
            var description = $"Concurrent search found {result?.TotalCount ?? 0} products: {string.Join(", ", productNames)}";
            
            return CreateStep("InventoryAgent", $"Concurrent search {productQuery}", description, baseTime);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory agent failed in concurrent execution");
            return CreateStep("InventoryAgent", $"Concurrent search {productQuery}", "Concurrent fallback inventory result", baseTime);
        }
    }

    private async Task<AgentStep> ExecuteMatchmakingAgentAsync(string productQuery, string userId, DateTime baseTime)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(productQuery, userId);
            var count = result?.Alternatives?.Length ?? 0;
            var description = $"Concurrent analysis found {count} independent alternatives";
            
            return CreateStep("MatchmakingAgent", $"Concurrent alternatives {productQuery}", description, baseTime.AddMilliseconds(100));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking agent failed in concurrent execution");
            return CreateStep("MatchmakingAgent", $"Concurrent alternatives {productQuery}", "Concurrent fallback alternatives", baseTime.AddMilliseconds(100));
        }
    }

    private async Task<AgentStep> ExecuteLocationAgentAsync(string productQuery, DateTime baseTime)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(productQuery);
            var location = result?.StoreLocations?.FirstOrDefault();
            var description = location != null 
                ? $"Concurrent location search: {location.Section} Aisle {location.Aisle}" 
                : "Concurrent location not found";
            
            return CreateStep("LocationAgent", $"Concurrent locate {productQuery}", description, baseTime.AddMilliseconds(200));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location agent failed in concurrent execution");
            return CreateStep("LocationAgent", $"Concurrent locate {productQuery}", "Concurrent fallback location", baseTime.AddMilliseconds(200));
        }
    }

    private async Task<AgentStep> ExecuteNavigationAgentAsync(Location location, string productQuery, DateTime baseTime)
    {
        try
        {
            var destination = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(location, destination);
            var stepCount = nav?.Steps?.Length ?? 0;
            var description = $"Concurrent navigation: {stepCount} independent route steps";
            
            return CreateStep("NavigationAgent", "Concurrent navigate to product", description, baseTime.AddMilliseconds(300));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation agent failed in concurrent execution");
            return CreateStep("NavigationAgent", "Concurrent navigate", "Concurrent fallback navigation", baseTime.AddMilliseconds(300));
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

    private static AgentStep CreateStep(string agent, string action, string result, DateTime timestamp) => new()
    {
        Agent = agent,
        Action = action,
        Result = result,
        Timestamp = timestamp
    };
}