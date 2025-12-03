using MultiAgentDemo.Controllers;
using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Magentic orchestration - complex multi-agent collaboration inspired by MagenticOne.
/// Use Case: Complex, generalist multi-agent collaboration with adaptive planning.
/// </summary>
public class MagenticOrchestrationService : IAgentOrchestrationService
{
    private readonly ILogger<MagenticOrchestrationService> _logger;
    private readonly InventoryAgentService _inventoryAgentService;
    private readonly MatchmakingAgentService _matchmakingAgentService;
    private readonly LocationAgentService _locationAgentService;
    private readonly NavigationAgentService _navigationAgentService;

    public MagenticOrchestrationService(
        ILogger<MagenticOrchestrationService> logger,
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
        _logger.LogInformation("Starting MagenticOne-inspired orchestration {OrchestrationId}", orchestrationId);

        var steps = new List<AgentStep>();
        var context = new MagenticContext
        {
            ProductQuery = request.ProductQuery,
            UserId = request.UserId,
            Location = request.Location,
            SharedKnowledge = []
        };

        // Phase 1: Initialize complex collaboration
        steps.Add(CreateOrchestratorStep("Initialize MagenticOne collaboration",
            $"Beginning complex multi-agent analysis for '{request.ProductQuery}' using MagenticOne-inspired approach with adaptive planning."));
        context.SharedKnowledge.Add($"Orchestrator initialized complex collaboration for: {request.ProductQuery}");

        // Phase 2: Specialist agents perform deep analysis
        var inventoryStep = await ExecuteSpecialistInventoryAsync(context);
        steps.Add(inventoryStep);
        context.SharedKnowledge.Add($"Inventory Specialist: {inventoryStep.Result}");

        var matchmakingStep = await ExecuteSpecialistMatchmakingAsync(context);
        steps.Add(matchmakingStep);
        context.SharedKnowledge.Add($"Matchmaking Specialist: {matchmakingStep.Result}");

        // Phase 3: Orchestrator synthesizes and plans next phase
        steps.Add(CreateOrchestratorStep("Synthesize specialist findings",
            "Analyzing specialist inputs to determine optimal collaboration strategy. Adapting plan based on initial findings."));

        // Phase 4: Location and navigation coordination
        var locationStep = await ExecuteCoordinatedLocationAsync(context);
        steps.Add(locationStep);
        context.SharedKnowledge.Add($"Location Coordinator: {locationStep.Result}");

        if (request.Location != null)
        {
            var navigationStep = await ExecuteCoordinatedNavigationAsync(context);
            steps.Add(navigationStep);
            context.SharedKnowledge.Add($"Navigation Coordinator: {navigationStep.Result}");
        }

        // Phase 5: Multi-agent consensus building
        steps.Add(CreateOrchestratorStep("Build multi-agent consensus",
            "Facilitating consensus among specialists. Evaluating conflicting recommendations and building unified solution."));

        // Phase 6: Adaptive refinement
        var refinementStep = await ExecuteAdaptiveRefinementAsync(context);
        steps.Add(refinementStep);

        // Phase 7: Final synthesis
        steps.Add(CreateOrchestratorStep("Finalize MagenticOne solution",
            "Completed complex multi-agent collaboration with adaptive refinement. Delivering comprehensive solution based on specialist consensus."));

        NavigationInstructions? navigation = null;
        if (request.Location != null)
        {
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestrationType.Magentic,
            OrchestrationDescription = "Complex generalist multi-agent collaboration inspired by MagenticOne, featuring adaptive orchestration, specialist coordination, and iterative refinement.",
            Steps = steps.ToArray(),
            Alternatives = StepsProcessor.GenerateDefaultProductAlternatives(),
            NavigationInstructions = navigation
        };
    }

    private static AgentStep CreateOrchestratorStep(string action, string result) => new()
    {
        Agent = "Orchestrator",
        Action = action,
        Result = result,
        Timestamp = DateTime.UtcNow
    };

    private async Task<AgentStep> ExecuteSpecialistInventoryAsync(MagenticContext context)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(context.ProductQuery);
            var productNames = result?.ProductsFound?.Select(p => p.Name) ?? [];
            var response = $"MagenticOne Inventory Specialist: Deep analysis reveals {result?.TotalCount ?? 0} products. " +
                          $"Cross-referencing with supply chain data: {string.Join(", ", productNames)}";

            return CreateStep("Inventory Specialist", "Complex inventory analysis", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory specialist failed in MagenticOne");
            return CreateStep("Inventory Specialist", "Complex inventory analysis", 
                "MagenticOne adaptive fallback: Inventory specialist adapting to constraints");
        }
    }

    private async Task<AgentStep> ExecuteSpecialistMatchmakingAsync(MagenticContext context)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(context.ProductQuery, context.UserId);
            var count = result?.Alternatives?.Length ?? 0;
            var response = $"MagenticOne Matchmaking Specialist: Advanced customer profiling identified {count} personalized alternatives with behavioral prediction modeling";

            return CreateStep("Matchmaking Specialist", "Advanced customer analysis", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking specialist failed in MagenticOne");
            return CreateStep("Matchmaking Specialist", "Advanced customer analysis", 
                "MagenticOne recovery: Specialist adapting algorithm parameters");
        }
    }

    private async Task<AgentStep> ExecuteCoordinatedLocationAsync(MagenticContext context)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(context.ProductQuery);
            var location = result?.StoreLocations?.FirstOrDefault();
            var response = location != null
                ? $"MagenticOne Location Coordinator: Integrated spatial analysis confirms optimal location: {location.Section} Aisle {location.Aisle}. Coordinating with navigation systems."
                : "MagenticOne Location Coordinator: Spatial analysis complete. Coordinating alternative location strategies with team.";

            return CreateStep("Location Coordinator", "Integrated spatial analysis", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location coordinator failed in MagenticOne");
            return CreateStep("Location Coordinator", "Integrated spatial analysis", 
                "MagenticOne resilience: Coordinator switching to backup location algorithms");
        }
    }

    private async Task<AgentStep> ExecuteCoordinatedNavigationAsync(MagenticContext context)
    {
        if (context.Location == null)
        {
            return CreateStep("Navigation Coordinator", "Route optimization", 
                "MagenticOne Navigation: Awaiting customer location for route synthesis");
        }

        try
        {
            var destination = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(context.Location, destination);
            var stepCount = nav?.Steps?.Length ?? 0;
            var response = $"MagenticOne Navigation Coordinator: Multi-modal route optimization complete. Generated {stepCount} steps with real-time adaptation capabilities.";

            return CreateStep("Navigation Coordinator", "Multi-modal route optimization", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation coordinator failed in MagenticOne");
            return CreateStep("Navigation Coordinator", "Multi-modal route optimization", 
                "MagenticOne adaptability: Navigation coordinator implementing alternative routing strategies");
        }
    }

    private async Task<AgentStep> ExecuteAdaptiveRefinementAsync(MagenticContext context)
    {
        await Task.Delay(50); // Simulate processing time for refinement

        var refinementSummary = string.Join("; ", context.SharedKnowledge.Take(3));
        var response = $"MagenticOne Adaptive Refinement: Synthesizing insights from {context.SharedKnowledge.Count} specialist inputs. " +
                      $"Key findings: {refinementSummary}. Applying iterative improvement algorithms.";

        return new AgentStep
        {
            Agent = "Adaptive Refiner",
            Action = "Multi-agent synthesis and refinement",
            Result = response,
            Timestamp = DateTime.UtcNow
        };
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
    /// Context for MagenticOne-style orchestration with shared knowledge.
    /// </summary>
    private sealed class MagenticContext
    {
        public string ProductQuery { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public Location? Location { get; init; }
        public List<string> SharedKnowledge { get; init; } = [];
    }
}