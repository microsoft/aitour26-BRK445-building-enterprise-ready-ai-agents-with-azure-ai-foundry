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

    public async Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request)
    {
        var orchestrationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting handoff orchestration {OrchestrationId}", orchestrationId);

        var steps = new List<AgentStep>();
        var currentContext = new HandoffContext { ProductQuery = request.ProductQuery, UserId = request.UserId, Location = request.Location };

        // Start with inventory agent
        var inventoryStep = await RunInventoryAgentAsync(currentContext);
        steps.Add(inventoryStep);
        currentContext.InventoryResult = inventoryStep.Result;

        // Decide next agent based on inventory result
        var nextAgent = DetermineNextAgent(inventoryStep, currentContext);
        _logger.LogInformation("Handoff decision: Next agent is {NextAgent}", nextAgent);

        while (nextAgent != "Complete" && steps.Count < 10) // Safety limit
        {
            switch (nextAgent)
            {
                case "MatchmakingAgent":
                    var matchmakingStep = await RunMatchmakingAgentAsync(currentContext);
                    steps.Add(matchmakingStep);
                    currentContext.MatchmakingResult = matchmakingStep.Result;
                    nextAgent = DetermineNextAgent(matchmakingStep, currentContext);
                    break;

                case "LocationAgent":
                    var locationStep = await RunLocationAgentAsync(currentContext);
                    steps.Add(locationStep);
                    currentContext.LocationResult = locationStep.Result;
                    nextAgent = DetermineNextAgent(locationStep, currentContext);
                    break;

                case "NavigationAgent":
                    if (request.Location != null)
                    {
                        var navigationStep = await RunNavigationAgentAsync(currentContext);
                        steps.Add(navigationStep);
                        currentContext.NavigationResult = navigationStep.Result;
                    }
                    nextAgent = "Complete";
                    break;

                default:
                    nextAgent = "Complete";
                    break;
            }
        }

        NavigationInstructions? navigation = null;
        if (request.Location != null && !string.IsNullOrEmpty(currentContext.NavigationResult))
        {
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        var alternatives = StepsProcessor.GenerateDefaultProductAlternatives();

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestationType.Handoff,
            OrchestrationDescription = "Agents executed using dynamic handoff logic, with each agent determining the next agent based on analysis results and business rules.",
            Steps = steps.ToArray(),
            Alternatives = alternatives,
            NavigationInstructions = navigation
        };
    }

    private string DetermineNextAgent(AgentStep lastStep, HandoffContext context)
    {
        // Dynamic routing logic based on step results and context
        switch (lastStep.Agent)
        {
            case "InventoryAgent":
                if (lastStep.Result.Contains("0 products") || lastStep.Result.Contains("not found"))
                {
                    // No inventory found, try alternatives first
                    return "MatchmakingAgent";
                }
                else
                {
                    // Products found, get location next
                    return "LocationAgent";
                }

            case "MatchmakingAgent":
                if (string.IsNullOrEmpty(context.LocationResult))
                {
                    // Need location information
                    return "LocationAgent";
                }
                else if (context.Location != null)
                {
                    // Have location info and user location, provide navigation
                    return "NavigationAgent";
                }
                else
                {
                    return "Complete";
                }

            case "LocationAgent":
                if (lastStep.Result.Contains("not found") && string.IsNullOrEmpty(context.MatchmakingResult))
                {
                    // Location not found, try alternatives
                    return "MatchmakingAgent";
                }
                else if (context.Location != null)
                {
                    // Have location info and user location, provide navigation
                    return "NavigationAgent";
                }
                else
                {
                    return "Complete";
                }

            case "NavigationAgent":
            default:
                return "Complete";
        }
    }

    private async Task<AgentStep> RunInventoryAgentAsync(HandoffContext context)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(context.ProductQuery);
            var names = result?.ProductsFound?.Select(p => p.Name) ?? Enumerable.Empty<string>();
            var desc = $"Handoff inventory check: {result?.TotalCount ?? 0} products found: {string.Join(", ", names)}";
            return new AgentStep { Agent = "InventoryAgent", Action = $"Handoff search {context.ProductQuery}", Result = desc, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory agent failed in handoff");
            return new AgentStep { Agent = "InventoryAgent", Action = $"Handoff search {context.ProductQuery}", Result = "Handoff inventory failed - escalating", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunMatchmakingAgentAsync(HandoffContext context)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(context.ProductQuery, context.UserId);
            var count = result?.Alternatives?.Length ?? 0;
            var desc = $"Handoff alternatives: {count} options found after inventory analysis";
            return new AgentStep { Agent = "MatchmakingAgent", Action = $"Handoff alternatives {context.ProductQuery}", Result = desc, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking agent failed in handoff");
            return new AgentStep { Agent = "MatchmakingAgent", Action = $"Handoff alternatives {context.ProductQuery}", Result = "Handoff alternatives failed", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunLocationAgentAsync(HandoffContext context)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(context.ProductQuery);
            var loc = result?.StoreLocations?.FirstOrDefault();
            var desc = loc != null ? $"Handoff location: {loc.Section} Aisle {loc.Aisle}" : "Handoff location not found - may need alternatives";
            return new AgentStep { Agent = "LocationAgent", Action = $"Handoff locate {context.ProductQuery}", Result = desc, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location agent failed in handoff");
            return new AgentStep { Agent = "LocationAgent", Action = $"Handoff locate {context.ProductQuery}", Result = "Handoff location failed", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunNavigationAgentAsync(HandoffContext context)
    {
        try
        {
            if (context.Location == null) return new AgentStep { Agent = "NavigationAgent", Action = "Handoff navigate", Result = "No start location for handoff", Timestamp = DateTime.UtcNow };
            var dest = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(context.Location, dest);
            var steps = nav?.Steps?.Length ?? 0;
            var desc = $"Handoff navigation: {steps} steps based on context analysis";
            return new AgentStep { Agent = "NavigationAgent", Action = "Handoff navigate to product", Result = desc, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation agent failed in handoff");
            return new AgentStep { Agent = "NavigationAgent", Action = "Handoff navigate", Result = "Handoff navigation failed", Timestamp = DateTime.UtcNow };
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

    private class HandoffContext
    {
        public string ProductQuery { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Location? Location { get; set; }
        public string? InventoryResult { get; set; }
        public string? MatchmakingResult { get; set; }
        public string? LocationResult { get; set; }
        public string? NavigationResult { get; set; }
    }
}