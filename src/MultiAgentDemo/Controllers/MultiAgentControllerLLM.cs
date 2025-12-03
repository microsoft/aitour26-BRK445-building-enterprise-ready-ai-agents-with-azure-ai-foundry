using Microsoft.AspNetCore.Mvc;
using MultiAgentDemo.Services;
using SharedEntities;

namespace MultiAgentDemo.Controllers;

/// <summary>
/// Controller for multi-agent orchestration using LLM direct calls to external services.
/// Supports multiple orchestration patterns: Sequential, Concurrent, Handoff, GroupChat, and Magentic.
/// </summary>
[ApiController]
[Route("api/multiagent/llm")]
public class MultiAgentControllerLLM : ControllerBase
{
    private readonly ILogger<MultiAgentControllerLLM> _logger;
    private readonly InventoryAgentService _inventoryAgentService;
    private readonly MatchmakingAgentService _matchmakingAgentService;
    private readonly LocationAgentService _locationAgentService;
    private readonly NavigationAgentService _navigationAgentService;
    private readonly SequentialOrchestrationService _sequentialOrchestration;
    private readonly ConcurrentOrchestrationService _concurrentOrchestration;
    private readonly HandoffOrchestrationService _handoffOrchestration;
    private readonly GroupChatOrchestrationService _groupChatOrchestration;
    private readonly MagenticOrchestrationService _magenticOrchestration;

    public MultiAgentControllerLLM(
        ILogger<MultiAgentControllerLLM> logger,
        InventoryAgentService inventoryAgentService,
        MatchmakingAgentService matchmakingAgentService,
        LocationAgentService locationAgentService,
        NavigationAgentService navigationAgentService,
        SequentialOrchestrationService sequentialOrchestration,
        ConcurrentOrchestrationService concurrentOrchestration,
        HandoffOrchestrationService handoffOrchestration,
        GroupChatOrchestrationService groupChatOrchestration,
        MagenticOrchestrationService magenticOrchestration)
    {
        _logger = logger;
        _inventoryAgentService = inventoryAgentService;
        _matchmakingAgentService = matchmakingAgentService;
        _locationAgentService = locationAgentService;
        _navigationAgentService = navigationAgentService;
        _sequentialOrchestration = sequentialOrchestration;
        _concurrentOrchestration = concurrentOrchestration;
        _handoffOrchestration = handoffOrchestration;
        _groupChatOrchestration = groupChatOrchestration;
        _magenticOrchestration = magenticOrchestration;

        // Set framework to LLM for all agent services
        ConfigureFramework("llm");
    }

    /// <summary>
    /// Routes to the appropriate orchestration pattern based on request type.
    /// </summary>
    [HttpPost("assist")]
    public async Task<ActionResult<MultiAgentResponse>> AssistAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        _logger.LogInformation(
            "Starting {OrchestrationTypeName} orchestration for query: {ProductQuery} using LLM",
            request.Orchestration, request.ProductQuery);

        try
        {
            var orchestrationService = GetOrchestrationService(request.Orchestration);
            var response = await orchestrationService.ExecuteAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {OrchestrationTypeName} orchestration using LLM", request.Orchestration);
            return StatusCode(500, "An error occurred during orchestration processing.");
        }
    }

    /// <summary>
    /// Sequential orchestration - executes agents in order with result chaining.
    /// </summary>
    [HttpPost("assist/sequential")]
    public async Task<ActionResult<MultiAgentResponse>> AssistSequentialAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        request.Orchestration = OrchestrationType.Sequential;
        _logger.LogInformation("Starting sequential orchestration for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var response = await _sequentialOrchestration.ExecuteAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sequential orchestration using LLM");
            return StatusCode(500, "An error occurred during sequential processing.");
        }
    }

    /// <summary>
    /// Concurrent orchestration - executes agents in parallel.
    /// </summary>
    [HttpPost("assist/concurrent")]
    public async Task<ActionResult<MultiAgentResponse>> AssistConcurrentAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        request.Orchestration = OrchestrationType.Concurrent;
        _logger.LogInformation("Starting concurrent orchestration for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var response = await _concurrentOrchestration.ExecuteAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in concurrent orchestration using LLM");
            return StatusCode(500, "An error occurred during concurrent processing.");
        }
    }

    /// <summary>
    /// Handoff orchestration - dynamic agent routing based on context.
    /// </summary>
    [HttpPost("assist/handoff")]
    public async Task<ActionResult<MultiAgentResponse>> AssistHandoffAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        request.Orchestration = OrchestrationType.Handoff;
        _logger.LogInformation("Starting handoff orchestration for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var response = await _handoffOrchestration.ExecuteAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in handoff orchestration using LLM");
            return StatusCode(500, "An error occurred during handoff processing.");
        }
    }

    /// <summary>
    /// Group chat orchestration - collaborative multi-agent discussion.
    /// </summary>
    [HttpPost("assist/groupchat")]
    public async Task<ActionResult<MultiAgentResponse>> AssistGroupChatAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        request.Orchestration = OrchestrationType.GroupChat;
        _logger.LogInformation("Starting group chat orchestration for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var response = await _groupChatOrchestration.ExecuteAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in group chat orchestration using LLM");
            return StatusCode(500, "An error occurred during group chat processing.");
        }
    }

    /// <summary>
    /// MagenticOne orchestration - complex multi-agent collaboration.
    /// </summary>
    [HttpPost("assist/magentic")]
    public async Task<ActionResult<MultiAgentResponse>> AssistMagenticAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        request.Orchestration = OrchestrationType.Magentic;
        _logger.LogInformation("Starting MagenticOne orchestration for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var response = await _magenticOrchestration.ExecuteAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MagenticOne orchestration using LLM");
            return StatusCode(500, "An error occurred during MagenticOne processing.");
        }
    }

    private void ConfigureFramework(string framework)
    {
        _inventoryAgentService.SetFramework(framework);
        _matchmakingAgentService.SetFramework(framework);
        _locationAgentService.SetFramework(framework);
        _navigationAgentService.SetFramework(framework);
    }

    private IAgentOrchestrationService GetOrchestrationService(OrchestrationType orchestrationType) => orchestrationType switch
    {
        OrchestrationType.Sequential => _sequentialOrchestration,
        OrchestrationType.Concurrent => _concurrentOrchestration,
        OrchestrationType.Handoff => _handoffOrchestration,
        OrchestrationType.GroupChat => _groupChatOrchestration,
        OrchestrationType.Magentic => _magenticOrchestration,
        _ => _sequentialOrchestration
    };
}
