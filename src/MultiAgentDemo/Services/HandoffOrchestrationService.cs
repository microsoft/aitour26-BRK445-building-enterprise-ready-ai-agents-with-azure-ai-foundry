using MultiAgentDemo.Controllers;
using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Handoff orchestration - dynamically passes control between agents based on context or rules.
/// Use Case: Dynamic workflows, escalation, fallback, or expert handoff scenarios.
/// </summary>
public class HandoffOrchestrationService : IAgentOrchestrationService
{
    private readonly ILogger<HandoffOrchestrationService> _logger;
    private readonly InventoryAgentService _inventoryAgentService;
    private readonly MatchmakingAgentService _matchmakingAgentService;
    private readonly LocationAgentService _locationAgentService;
    private readonly NavigationAgentService _navigationAgentService;

    public HandoffOrchestrationService(
        ILogger<HandoffOrchestrationService> logger,
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
        _logger.LogInformation("Starting handoff orchestration {OrchestrationId}", orchestrationId);

        var steps = new List<AgentStep>();
        var context = new HandoffContext
        {
            ProductQuery = request.ProductQuery,
            UserId = request.UserId,
            Location = request.Location
        };

        // Start with inventory agent
        var inventoryStep = await ExecuteInventoryAgentAsync(context);
        steps.Add(inventoryStep);
        context.InventoryResult = inventoryStep.Result;

        // Process dynamic handoffs
        var nextAgent = DetermineNextAgent(inventoryStep, context);
        _logger.LogInformation("Handoff decision: Next agent is {NextAgent}", nextAgent);

        const int maxSteps = 10; // Safety limit
        while (nextAgent != "Complete" && steps.Count < maxSteps)
        {
            nextAgent = await ProcessHandoffAsync(nextAgent, context, steps, request);
        }

        NavigationInstructions? navigation = null;
        if (request.Location != null && !string.IsNullOrEmpty(context.NavigationResult))
        {
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestrationType.Handoff,
            OrchestrationDescription = "Agents executed using dynamic handoff logic, with each agent determining the next agent based on analysis results.",
            Steps = steps.ToArray(),
            Alternatives = StepsProcessor.GenerateDefaultProductAlternatives(),
            NavigationInstructions = navigation
        };
    }

    private async Task<string> ProcessHandoffAsync(string agentName, HandoffContext context, List<AgentStep> steps, MultiAgentRequest request)
    {
        switch (agentName)
        {
            case "MatchmakingAgent":
                var matchmakingStep = await ExecuteMatchmakingAgentAsync(context);
                steps.Add(matchmakingStep);
                context.MatchmakingResult = matchmakingStep.Result;
                return DetermineNextAgent(matchmakingStep, context);

            case "LocationAgent":
                var locationStep = await ExecuteLocationAgentAsync(context);
                steps.Add(locationStep);
                context.LocationResult = locationStep.Result;
                return DetermineNextAgent(locationStep, context);

            case "NavigationAgent":
                if (request.Location != null)
                {
                    var navigationStep = await ExecuteNavigationAgentAsync(context);
                    steps.Add(navigationStep);
                    context.NavigationResult = navigationStep.Result;
                }
                return "Complete";

            default:
                return "Complete";
        }
    }

    private string DetermineNextAgent(AgentStep lastStep, HandoffContext context)
    {
        return lastStep.Agent switch
        {
            "InventoryAgent" => lastStep.Result.Contains("0 products") || lastStep.Result.Contains("not found")
                ? "MatchmakingAgent"
                : "LocationAgent",

            "MatchmakingAgent" => string.IsNullOrEmpty(context.LocationResult)
                ? "LocationAgent"
                : context.Location != null ? "NavigationAgent" : "Complete",

            "LocationAgent" => lastStep.Result.Contains("not found") && string.IsNullOrEmpty(context.MatchmakingResult)
                ? "MatchmakingAgent"
                : context.Location != null ? "NavigationAgent" : "Complete",

            _ => "Complete"
        };
    }

    private async Task<AgentStep> ExecuteInventoryAgentAsync(HandoffContext context)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(context.ProductQuery);
            var productNames = result?.ProductsFound?.Select(p => p.Name) ?? [];
            var description = $"Handoff inventory check: {result?.TotalCount ?? 0} products found: {string.Join(", ", productNames)}";
            
            return CreateStep("InventoryAgent", $"Handoff search {context.ProductQuery}", description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory agent failed in handoff");
            return CreateStep("InventoryAgent", $"Handoff search {context.ProductQuery}", "Handoff inventory failed - escalating");
        }
    }

    private async Task<AgentStep> ExecuteMatchmakingAgentAsync(HandoffContext context)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(context.ProductQuery, context.UserId);
            var count = result?.Alternatives?.Length ?? 0;
            var description = $"Handoff alternatives: {count} options found after inventory analysis";
            
            return CreateStep("MatchmakingAgent", $"Handoff alternatives {context.ProductQuery}", description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking agent failed in handoff");
            return CreateStep("MatchmakingAgent", $"Handoff alternatives {context.ProductQuery}", "Handoff alternatives failed");
        }
    }

    private async Task<AgentStep> ExecuteLocationAgentAsync(HandoffContext context)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(context.ProductQuery);
            var location = result?.StoreLocations?.FirstOrDefault();
            var description = location != null
                ? $"Handoff location: {location.Section} Aisle {location.Aisle}"
                : "Handoff location not found - may need alternatives";
            
            return CreateStep("LocationAgent", $"Handoff locate {context.ProductQuery}", description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location agent failed in handoff");
            return CreateStep("LocationAgent", $"Handoff locate {context.ProductQuery}", "Handoff location failed");
        }
    }

    private async Task<AgentStep> ExecuteNavigationAgentAsync(HandoffContext context)
    {
        try
        {
            if (context.Location == null)
            {
                return CreateStep("NavigationAgent", "Handoff navigate", "No start location for handoff");
            }

            var destination = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(context.Location, destination);
            var stepCount = nav?.Steps?.Length ?? 0;
            var description = $"Handoff navigation: {stepCount} steps based on context analysis";
            
            return CreateStep("NavigationAgent", "Handoff navigate to product", description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation agent failed in handoff");
            return CreateStep("NavigationAgent", "Handoff navigate", "Handoff navigation failed");
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

    /// <summary>
    /// Context for tracking handoff state between agents.
    /// </summary>
    private sealed class HandoffContext
    {
        public string ProductQuery { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public Location? Location { get; init; }
        public string? InventoryResult { get; set; }
        public string? MatchmakingResult { get; set; }
        public string? LocationResult { get; set; }
        public string? NavigationResult { get; set; }
    }
}