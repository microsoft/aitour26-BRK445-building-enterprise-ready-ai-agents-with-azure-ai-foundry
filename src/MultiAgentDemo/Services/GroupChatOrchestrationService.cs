using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Group Chat orchestration - all agents participate in a group conversation, coordinated by a group manager.
/// Use Case: Brainstorming, collaborative problem solving, consensus building.
/// </summary>
public class GroupChatOrchestrationService : IAgentOrchestrationService
{
    private readonly ILogger<GroupChatOrchestrationService> _logger;
    private readonly InventoryAgentService _inventoryAgentService;
    private readonly MatchmakingAgentService _matchmakingAgentService;
    private readonly LocationAgentService _locationAgentService;
    private readonly NavigationAgentService _navigationAgentService;

    public GroupChatOrchestrationService(
        ILogger<GroupChatOrchestrationService> logger,
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
        _logger.LogInformation("Starting group chat orchestration {OrchestrationId}", orchestrationId);

        var steps = new List<AgentStep>();
        var conversationContext = new List<string>();

        // Group Manager initiates the conversation
        var managerStep = CreateManagerStep("Group Manager", "Initiate discussion", 
            $"Welcome to the group discussion about '{request.ProductQuery}'. Let's collaborate to help the customer.", DateTime.UtcNow);
        steps.Add(managerStep);
        conversationContext.Add($"Manager: {managerStep.Result}");

        // Round 1: Initial thoughts from all agents
        var inventoryStep = await RunInventoryAgentInGroupAsync(request.ProductQuery, conversationContext, 1);
        steps.Add(inventoryStep);
        conversationContext.Add($"Inventory: {inventoryStep.Result}");

        var matchmakingStep = await RunMatchmakingAgentInGroupAsync(request.ProductQuery, request.UserId, conversationContext, 1);
        steps.Add(matchmakingStep);
        conversationContext.Add($"Matchmaking: {matchmakingStep.Result}");

        var locationStep = await RunLocationAgentInGroupAsync(request.ProductQuery, conversationContext, 1);
        steps.Add(locationStep);
        conversationContext.Add($"Location: {locationStep.Result}");

        // Manager summarizes round 1
        var managerSummary1 = CreateManagerStep("Group Manager", "Summarize Round 1", 
            "Great initial insights! Inventory found products, Matchmaking identified alternatives, Location provided coordinates. Let's build consensus.", DateTime.UtcNow);
        steps.Add(managerSummary1);
        conversationContext.Add($"Manager: {managerSummary1.Result}");

        // Round 2: Agents respond to each other's insights
        var inventoryResponse = await RunInventoryAgentInGroupAsync(request.ProductQuery, conversationContext, 2);
        steps.Add(inventoryResponse);
        conversationContext.Add($"Inventory: {inventoryResponse.Result}");

        var matchmakingResponse = await RunMatchmakingAgentInGroupAsync(request.ProductQuery, request.UserId, conversationContext, 2);
        steps.Add(matchmakingResponse);
        conversationContext.Add($"Matchmaking: {matchmakingResponse.Result}");

        // Navigation agent joins if location is provided
        if (request.Location != null)
        {
            var navigationStep = await RunNavigationAgentInGroupAsync(request.Location, request.ProductQuery, conversationContext);
            steps.Add(navigationStep);
            conversationContext.Add($"Navigation: {navigationStep.Result}");
        }

        // Manager concludes the discussion
        var managerConclusion = CreateManagerStep("Group Manager", "Conclude discussion", 
            "Excellent collaboration! We've reached consensus on the best customer solution through group discussion.", DateTime.UtcNow);
        steps.Add(managerConclusion);

        NavigationInstructions? navigation = null;
        if (request.Location != null)
        {
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestationType.GroupChat,
            OrchestrationDescription = "Agents participated in a collaborative group chat coordinated by a group manager, with multiple rounds of discussion to build consensus and share insights.",
            Steps = steps.ToArray(),
            Alternatives = alternatives,
            NavigationInstructions = navigation
        };
    }

    private AgentStep CreateManagerStep(string agent, string action, string result, DateTime timestamp)
    {
        return new AgentStep
        {
            Agent = agent,
            Action = action,
            Result = result,
            Timestamp = timestamp
        };
    }

    private async Task<AgentStep> RunInventoryAgentInGroupAsync(string productQuery, List<string> context, int round)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(productQuery);
            var names = result?.ProductsFound?.Select(p => p.Name) ?? Enumerable.Empty<string>();
            
            string response;
            if (round == 1)
            {
                response = $"Group discussion: I found {result?.TotalCount ?? 0} products for '{productQuery}': {string.Join(", ", names)}. What do others think?";
            }
            else
            {
                response = $"Following up on the discussion: I can confirm stock levels and suggest cross-checking with the alternatives mentioned by Matchmaking.";
            }

            return new AgentStep { Agent = "InventoryAgent", Action = $"Group discussion Round {round}", Result = response, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory agent failed in group chat");
            return new AgentStep { Agent = "InventoryAgent", Action = $"Group discussion Round {round}", Result = "Having technical issues, will follow up", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunMatchmakingAgentInGroupAsync(string productQuery, string userId, List<string> context, int round)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(productQuery, userId);
            var count = result?.Alternatives?.Length ?? 0;
            
            string response;
            if (round == 1)
            {
                response = $"Group input: I've identified {count} alternatives for '{productQuery}'. These could complement what Inventory found.";
            }
            else
            {
                response = $"Building on Location's findings: I can match alternatives to specific aisles they mentioned. Great teamwork!";
            }

            return new AgentStep { Agent = "MatchmakingAgent", Action = $"Group discussion Round {round}", Result = response, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking agent failed in group chat");
            return new AgentStep { Agent = "MatchmakingAgent", Action = $"Group discussion Round {round}", Result = "Experiencing delays, will contribute next round", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunLocationAgentInGroupAsync(string productQuery, List<string> context, int round)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(productQuery);
            var loc = result?.StoreLocations?.FirstOrDefault();
            
            string response;
            if (round == 1)
            {
                response = loc != null ? 
                    $"Group collaboration: Found '{productQuery}' in {loc.Section} Aisle {loc.Aisle}. This aligns with Inventory's findings!" : 
                    "Group discussion: No specific location found, but Matchmaking's alternatives might help.";
            }
            else
            {
                response = "Reflecting on our discussion: I can provide detailed aisle maps to support Navigation's route planning.";
            }

            return new AgentStep { Agent = "LocationAgent", Action = $"Group discussion Round {round}", Result = response, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location agent failed in group chat");
            return new AgentStep { Agent = "LocationAgent", Action = $"Group discussion Round {round}", Result = "Technical difficulties, deferring to group", Timestamp = DateTime.UtcNow };
        }
    }

    private async Task<AgentStep> RunNavigationAgentInGroupAsync(Location? location, string productQuery, List<string> context)
    {
        try
        {
            if (location == null) return new AgentStep { Agent = "NavigationAgent", Action = "Join group discussion", Result = "Happy to help but need customer start location", Timestamp = DateTime.UtcNow };
            
            var dest = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(location, dest);
            var steps = nav?.Steps?.Length ?? 0;
            var response = $"Joining the discussion: Based on Location's coordinates and Inventory's findings, I can provide {steps} navigation steps!";
            
            return new AgentStep { Agent = "NavigationAgent", Action = "Join group discussion", Result = response, Timestamp = DateTime.UtcNow };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation agent failed in group chat");
            return new AgentStep { Agent = "NavigationAgent", Action = "Join group discussion", Result = "Working on route calculation, great group effort so far!", Timestamp = DateTime.UtcNow };
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

    private async Task<ProductAlternative[]> GenerateProductAlternativesAsync(string productQuery)
    {
        await Task.Delay(10);
        return new[]
        {
            new ProductAlternative { Name = $"Group-recommended {productQuery}", Sku = "GRP-" + productQuery.Replace(" ", "").ToUpper(), Price = 119.99m, InStock = true, Location = "Aisle 4", Aisle = 4, Section = "A" },
            new ProductAlternative { Name = $"Consensus {productQuery}", Sku = "CON-" + productQuery.Replace(" ", "").ToUpper(), Price = 69.99m, InStock = true, Location = "Aisle 6", Aisle = 6, Section = "B" },
            new ProductAlternative { Name = $"Team-selected {productQuery}", Sku = "TEAM-" + productQuery.Replace(" ", "").ToUpper(), Price = 39.99m, InStock = false, Location = "Aisle 10", Aisle = 10, Section = "C" }
        };
    }
}