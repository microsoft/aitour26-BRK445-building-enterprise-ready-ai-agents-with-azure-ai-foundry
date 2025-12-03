using MultiAgentDemo.Controllers;
using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Group Chat orchestration - all agents participate in a group conversation, coordinated by a manager.
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

    /// <inheritdoc />
    public async Task<MultiAgentResponse> ExecuteAsync(MultiAgentRequest request)
    {
        var orchestrationId = Guid.NewGuid().ToString();
        _logger.LogInformation("Starting group chat orchestration {OrchestrationId}", orchestrationId);

        var steps = new List<AgentStep>();
        var conversationContext = new List<string>();

        // Initiate the group discussion
        var managerStep = CreateManagerStep("Initiate discussion",
            $"Welcome to the group discussion about '{request.ProductQuery}'. Let's collaborate to help the customer.");
        steps.Add(managerStep);
        conversationContext.Add($"Manager: {managerStep.Result}");

        // Round 1: Initial thoughts from all agents
        await ExecuteRoundOneAsync(request, steps, conversationContext);

        // Manager summarizes round 1
        var summary = CreateManagerStep("Summarize Round 1",
            "Great initial insights! Inventory found products, Matchmaking identified alternatives, Location provided coordinates. Let's build consensus.");
        steps.Add(summary);
        conversationContext.Add($"Manager: {summary.Result}");

        // Round 2: Agents respond to each other's insights
        await ExecuteRoundTwoAsync(request, steps, conversationContext);

        // Manager concludes
        var conclusion = CreateManagerStep("Conclude discussion",
            "Excellent collaboration! We've reached consensus on the best customer solution through group discussion.");
        steps.Add(conclusion);

        NavigationInstructions? navigation = null;
        if (request.Location != null)
        {
            navigation = await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery);
        }

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = OrchestrationType.GroupChat,
            OrchestrationDescription = "Agents participated in a collaborative group chat with multiple rounds of discussion to build consensus.",
            Steps = steps.ToArray(),
            Alternatives = StepsProcessor.GenerateDefaultProductAlternatives(),
            NavigationInstructions = navigation
        };
    }

    private async Task ExecuteRoundOneAsync(MultiAgentRequest request, List<AgentStep> steps, List<string> context)
    {
        var inventoryStep = await ExecuteInventoryAgentInGroupAsync(request.ProductQuery, 1);
        steps.Add(inventoryStep);
        context.Add($"Inventory: {inventoryStep.Result}");

        var matchmakingStep = await ExecuteMatchmakingAgentInGroupAsync(request.ProductQuery, request.UserId, 1);
        steps.Add(matchmakingStep);
        context.Add($"Matchmaking: {matchmakingStep.Result}");

        var locationStep = await ExecuteLocationAgentInGroupAsync(request.ProductQuery, 1);
        steps.Add(locationStep);
        context.Add($"Location: {locationStep.Result}");
    }

    private async Task ExecuteRoundTwoAsync(MultiAgentRequest request, List<AgentStep> steps, List<string> context)
    {
        var inventoryResponse = await ExecuteInventoryAgentInGroupAsync(request.ProductQuery, 2);
        steps.Add(inventoryResponse);
        context.Add($"Inventory: {inventoryResponse.Result}");

        var matchmakingResponse = await ExecuteMatchmakingAgentInGroupAsync(request.ProductQuery, request.UserId, 2);
        steps.Add(matchmakingResponse);
        context.Add($"Matchmaking: {matchmakingResponse.Result}");

        if (request.Location != null)
        {
            var navigationStep = await ExecuteNavigationAgentInGroupAsync(request.Location, request.ProductQuery);
            steps.Add(navigationStep);
            context.Add($"Navigation: {navigationStep.Result}");
        }
    }

    private static AgentStep CreateManagerStep(string action, string result) => new()
    {
        Agent = "Group Manager",
        Action = action,
        Result = result,
        Timestamp = DateTime.UtcNow
    };

    private async Task<AgentStep> ExecuteInventoryAgentInGroupAsync(string productQuery, int round)
    {
        try
        {
            var result = await _inventoryAgentService.SearchProductsAsync(productQuery);
            var productNames = result?.ProductsFound?.Select(p => p.Name) ?? [];

            var response = round == 1
                ? $"Group discussion: I found {result?.TotalCount ?? 0} products for '{productQuery}': {string.Join(", ", productNames)}. What do others think?"
                : "Following up: I can confirm stock levels and suggest cross-checking with the alternatives mentioned by Matchmaking.";

            return CreateStep("InventoryAgent", $"Group discussion Round {round}", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inventory agent failed in group chat");
            return CreateStep("InventoryAgent", $"Group discussion Round {round}", "Having technical issues, will follow up");
        }
    }

    private async Task<AgentStep> ExecuteMatchmakingAgentInGroupAsync(string productQuery, string userId, int round)
    {
        try
        {
            var result = await _matchmakingAgentService.FindAlternativesAsync(productQuery, userId);
            var count = result?.Alternatives?.Length ?? 0;

            var response = round == 1
                ? $"Group input: I've identified {count} alternatives for '{productQuery}'. These could complement what Inventory found."
                : "Building on Location's findings: I can match alternatives to specific aisles they mentioned. Great teamwork!";

            return CreateStep("MatchmakingAgent", $"Group discussion Round {round}", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Matchmaking agent failed in group chat");
            return CreateStep("MatchmakingAgent", $"Group discussion Round {round}", "Experiencing delays, will contribute next round");
        }
    }

    private async Task<AgentStep> ExecuteLocationAgentInGroupAsync(string productQuery, int round)
    {
        try
        {
            var result = await _locationAgentService.FindProductLocationAsync(productQuery);
            var location = result?.StoreLocations?.FirstOrDefault();

            var response = round == 1
                ? location != null
                    ? $"Group collaboration: Found '{productQuery}' in {location.Section} Aisle {location.Aisle}. This aligns with Inventory's findings!"
                    : "Group discussion: No specific location found, but Matchmaking's alternatives might help."
                : "Reflecting on our discussion: I can provide detailed aisle maps to support Navigation's route planning.";

            return CreateStep("LocationAgent", $"Group discussion Round {round}", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Location agent failed in group chat");
            return CreateStep("LocationAgent", $"Group discussion Round {round}", "Technical difficulties, deferring to group");
        }
    }

    private async Task<AgentStep> ExecuteNavigationAgentInGroupAsync(Location? location, string productQuery)
    {
        if (location == null)
        {
            return CreateStep("NavigationAgent", "Join group discussion", "Happy to help but need customer start location");
        }

        try
        {
            var destination = new Location { Lat = 0, Lon = 0 };
            var nav = await _navigationAgentService.GenerateDirectionsAsync(location, destination);
            var stepCount = nav?.Steps?.Length ?? 0;
            var response = $"Joining the discussion: Based on Location's coordinates and Inventory's findings, I can provide {stepCount} navigation steps!";

            return CreateStep("NavigationAgent", "Join group discussion", response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Navigation agent failed in group chat");
            return CreateStep("NavigationAgent", "Join group discussion", "Working on route calculation, great group effort so far!");
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