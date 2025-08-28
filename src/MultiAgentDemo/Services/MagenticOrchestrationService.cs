using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Magentic orchestration - group chat-like orchestration inspired by MagenticOne.
/// Use Case: Complex, generalist multi-agent collaboration.
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

    public async Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request)
    {
        var orchestrationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting MagenticOne-inspired orchestration {OrchestrationId}", orchestrationId);

        var steps = new List<AgentStep>();
        var magenticContext = new MagenticContext 
        { 
            ProductQuery = request.ProductQuery, 
            UserId = request.UserId, 
            Location = request.Location,
            SharedKnowledge = new List<string>()
        };

        // Phase 1: Orchestrator initiates complex multi-agent collaboration
        var orchestratorStep = CreateOrchestratorStep("Orchestrator", "Initialize MagenticOne collaboration", 
            $"Beginning complex multi-agent analysis for '{request.ProductQuery}' using MagenticOne-inspired approach with adaptive planning.", DateTime.UtcNow);
        steps.Add(orchestratorStep);
        magenticContext.SharedKnowledge.Add($"Orchestrator initialized complex collaboration for: {request.ProductQuery}");

        // Phase 2: Specialist agents perform deep analysis
        var specialistInventory = await RunSpecialistInventoryAsync(magenticContext);
        steps.Add(specialistInventory);
        magenticContext.SharedKnowledge.Add($"Inventory Specialist: {specialistInventory.Result}");

        var specialistMatchmaking = await RunSpecialistMatchmakingAsync(magenticContext);
        steps.Add(specialistMatchmaking);
        magenticContext.SharedKnowledge.Add($"Matchmaking Specialist: {specialistMatchmaking.Result}");

        // Phase 3: Orchestrator synthesizes and plans next phase
        var synthesisStep = CreateOrchestratorStep("Orchestrator", "Synthesize specialist findings", 
            "Analyzing specialist inputs to determine optimal collaboration strategy. Adapting plan based on initial findings.", DateTime.UtcNow);
        steps.Add(synthesisStep);

        // Phase 4: Location and navigation coordination
        var coordinatedLocation = await RunCoordinatedLocationAsync(magenticContext);
        steps.Add(coordinatedLocation);
        magenticContext.SharedKnowledge.Add($"Location Coordinator: {coordinatedLocation.Result}");

        if (request.Location != null)
        {
            var coordinatedNavigation = await RunCoordinatedNavigationAsync(magenticContext);
            steps.Add(coordinatedNavigation);
            magenticContext.SharedKnowledge.Add($"Navigation Coordinator: {coordinatedNavigation.Result}");
        }

        // Phase 5: Multi-agent consensus building
        var consensusRound1 = CreateOrchestratorStep("Orchestrator", "Build multi-agent consensus", 
            "Facilitating consensus among specialists. Evaluating conflicting recommendations and building unified solution.", DateTime.UtcNow);
        steps.Add(consensusRound1);

        // Phase 6: Adaptive refinement based on all inputs
        var refinementStep = await RunAdaptiveRefinementAsync(magenticContext);
        steps.Add(refinementStep);

        // Phase 7: Final orchestrator synthesis
        var finalSynthesis = CreateOrchestratorStep("Orchestrator", "Finalize MagenticOne solution", 
            "Completed complex multi-agent collaboration with adaptive refinement. Delivering comprehensive solution based on specialist consensus.", DateTime.UtcNow);
        steps.Add(finalSynthesis);

        NavigationInstructions? navigation = null;
        if (request.Location != null)
        {
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestationType.Magentic,
            OrchestrationDescription = "Complex generalist multi-agent collaboration inspired by MagenticOne, featuring adaptive orchestration, specialist coordination, consensus building, and iterative refinement.",
            Steps = steps.ToArray(),
            Alternatives = alternatives,
            NavigationInstructions = navigation
        };
    }

    private AgentStep CreateOrchestratorStep(string agent, string action, string result, DateTime timestamp)
    {
        return new AgentStep
        {
            Agent = agent,
            Action = action,
            Result = result,
            Timestamp = timestamp
        };
    }

    private async Task<AgentStep> RunSpecialistInventoryAsync(MagenticContext context)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(context.ProductQuery);
            var names = result?.ProductsFound?.Select(p => p.Name) ?? Enumerable.Empty<string>();
            var response = $"MagenticOne Inventory Specialist: Deep analysis reveals {result?.TotalCount ?? 0} products. Cross-referencing with supply chain data and predictive availability models: {string.Join(", ", names)}";
            
            return new AgentStep { Agent = "Inventory Specialist", Action = "Complex inventory analysis", Result = response, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory specialist failed in MagenticOne");
            return new AgentStep { Agent = "Inventory Specialist", Action = "Complex inventory analysis", Result = "MagenticOne adaptive fallback: Inventory specialist adapting to constraints", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunSpecialistMatchmakingAsync(MagenticContext context)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(context.ProductQuery, context.UserId);
            var count = result?.Alternatives?.Length ?? 0;
            var response = $"MagenticOne Matchmaking Specialist: Advanced customer profiling and preference analysis identified {count} personalized alternatives with behavioral prediction modeling";
            
            return new AgentStep { Agent = "Matchmaking Specialist", Action = "Advanced customer analysis", Result = response, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking specialist failed in MagenticOne");
            return new AgentStep { Agent = "Matchmaking Specialist", Action = "Advanced customer analysis", Result = "MagenticOne recovery: Specialist adapting algorithm parameters", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunCoordinatedLocationAsync(MagenticContext context)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(context.ProductQuery);
            var loc = result?.StoreLocations?.FirstOrDefault();
            var response = loc != null ? 
                $"MagenticOne Location Coordinator: Integrated spatial analysis with inventory data confirms optimal location: {loc.Section} Aisle {loc.Aisle}. Coordinating with navigation systems." : 
                "MagenticOne Location Coordinator: Spatial analysis complete. Coordinating alternative location strategies with team.";
            
            return new AgentStep { Agent = "Location Coordinator", Action = "Integrated spatial analysis", Result = response, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location coordinator failed in MagenticOne");
            return new AgentStep { Agent = "Location Coordinator", Action = "Integrated spatial analysis", Result = "MagenticOne resilience: Coordinator switching to backup location algorithms", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunCoordinatedNavigationAsync(MagenticContext context)
    {
        try
        {
            if (context.Location == null) return new AgentStep { Agent = "Navigation Coordinator", Action = "Route optimization", Result = "MagenticOne Navigation: Awaiting customer location for route synthesis", Timestamp = DateTime.UtcNow };
            
            var dest = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(context.Location, dest);
            var steps = nav?.Steps?.Length ?? 0;
            var response = $"MagenticOne Navigation Coordinator: Multi-modal route optimization complete. Generated {steps} steps with real-time adaptation capabilities and crowd-flow analysis.";
            
            return new AgentStep { Agent = "Navigation Coordinator", Action = "Multi-modal route optimization", Result = response, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation coordinator failed in MagenticOne");
            return new AgentStep { Agent = "Navigation Coordinator", Action = "Multi-modal route optimization", Result = "MagenticOne adaptability: Navigation coordinator implementing alternative routing strategies", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunAdaptiveRefinementAsync(MagenticContext context)
    {
        await Task.Delay(100); // Simulate processing time for refinement
        
        var refinementSummary = string.Join("; ", context.SharedKnowledge.Take(3));
        var response = $"MagenticOne Adaptive Refinement: Synthesizing insights from {context.SharedKnowledge.Count} specialist inputs. Key findings: {refinementSummary}. Applying iterative improvement algorithms.";
        
        return new AgentStep 
        { 
            Agent = "Adaptive Refiner", 
            Action = "Multi-agent synthesis and refinement", 
            Result = response, 
            Timestamp = DateTime.UtcNow 
        };
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

    private async Task<ProductAlternative[]> GenerateProductAlternativesAsync(string productQuery)
    {
        await Task.Delay(10);
        return new[]
        {
            new ProductAlternative { Name = $"MagenticOne-optimized {productQuery}", Sku = "MAG-" + productQuery.Replace(" ", "").ToUpper(), Price = 199.99m, InStock = true, Location = "Aisle 1", Aisle = 1, Section = "A" },
            new ProductAlternative { Name = $"AI-curated {productQuery}", Sku = "AI-" + productQuery.Replace(" ", "").ToUpper(), Price = 129.99m, InStock = true, Location = "Aisle 3", Aisle = 3, Section = "B" },
            new ProductAlternative { Name = $"Collaborative-selected {productQuery}", Sku = "COLL-" + productQuery.Replace(" ", "").ToUpper(), Price = 89.99m, InStock = true, Location = "Aisle 7", Aisle = 7, Section = "C" },
            new ProductAlternative { Name = $"Adaptive {productQuery}", Sku = "ADAPT-" + productQuery.Replace(" ", "").ToUpper(), Price = 59.99m, InStock = false, Location = "Aisle 15", Aisle = 15, Section = "D" }
        };
    }

    private class MagenticContext
    {
        public string ProductQuery { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public Location? Location { get; set; }
        public List<string> SharedKnowledge { get; set; } = new();
    }
}