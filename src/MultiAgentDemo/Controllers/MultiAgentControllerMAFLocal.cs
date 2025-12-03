using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using SharedEntities;
using ZavaFoundryAgentsProvider;
using ZavaMAFLocalAgentsProvider;

namespace MultiAgentDemo.Controllers;

/// <summary>
/// Controller for multi-agent orchestration using Microsoft Agent Framework with locally created agents.
/// Agents are created with gpt-5-mini model and configured locally with instructions and tools.
/// </summary>
[ApiController]
[Route("api/multiagent/maf_local")]
public class MultiAgentControllerMAFLocal : ControllerBase
{
    private readonly ILogger<MultiAgentControllerMAFLocal> _logger;
    private readonly MAFLocalAgentProvider _localAgentProvider;

    public MultiAgentControllerMAFLocal(
        ILogger<MultiAgentControllerMAFLocal> logger,
        MAFLocalAgentProvider localAgentProvider)
    {
        _logger = logger;
        _localAgentProvider = localAgentProvider;
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
            "Starting {OrchestrationTypeName} orchestration for query: {ProductQuery} using MAF Local Agents",
            request.Orchestration, request.ProductQuery);

        try
        {
            return request.Orchestration switch
            {
                OrchestrationType.Sequential => await AssistSequentialAsync(request),
                OrchestrationType.Concurrent => await AssistConcurrentAsync(request),
                OrchestrationType.Handoff => await AssistHandoffAsync(request),
                OrchestrationType.GroupChat => await AssistGroupChatAsync(request),
                OrchestrationType.Magentic => await AssistMagenticAsync(request),
                _ => await AssistSequentialAsync(request)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {OrchestrationTypeName} orchestration using MAF Local Agents", request.Orchestration);
            return StatusCode(500, "An error occurred during orchestration processing.");
        }
    }

    /// <summary>
    /// Sequential workflow - executes agents in order, with output feeding into subsequent steps.
    /// </summary>
    [HttpPost("assist/sequential")]
    public async Task<ActionResult<MultiAgentResponse>> AssistSequentialAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        _logger.LogInformation("Starting sequential workflow with local agents for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var agents = GetLocalAgents();
            var workflow = AgentWorkflowBuilder.BuildSequential([
                agents.ProductSearch,
                agents.ProductMatchmaking,
                agents.LocationService,
                agents.Navigation
            ]);

            var workflowResponse = await RunWorkflowAsync(request, workflow, agents);
            return Ok(workflowResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in sequential workflow with local agents");
            return StatusCode(500, "An error occurred during sequential workflow processing.");
        }
    }

    /// <summary>
    /// Concurrent workflow - executes all agents in parallel for independent analysis.
    /// </summary>
    [HttpPost("assist/concurrent")]
    public async Task<ActionResult<MultiAgentResponse>> AssistConcurrentAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        _logger.LogInformation("Starting concurrent workflow with local agents for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var agents = GetLocalAgents();
            var workflow = AgentWorkflowBuilder.BuildConcurrent([
                agents.ProductSearch,
                agents.ProductMatchmaking,
                agents.LocationService,
                agents.Navigation
            ]);

            var workflowResponse = await RunWorkflowAsync(request, workflow, agents);
            return Ok(workflowResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in concurrent workflow with local agents");
            return StatusCode(500, "An error occurred during concurrent workflow processing.");
        }
    }

    /// <summary>
    /// Handoff workflow - dynamically passes control between agents based on branching logic.
    /// </summary>
    [HttpPost("assist/handoff")]
    public async Task<ActionResult<MultiAgentResponse>> AssistHandoffAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        _logger.LogInformation("Starting handoff workflow with local agents for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var agents = GetLocalAgents();
            var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(agents.ProductSearch)
                .WithHandoff(agents.ProductSearch, agents.ProductMatchmaking)
                .WithHandoff(agents.ProductMatchmaking, agents.LocationService)
                .WithHandoff(agents.LocationService, agents.Navigation)
                .Build();

            var workflowResponse = await RunWorkflowAsync(request, workflow, agents);
            return Ok(workflowResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in handoff workflow with local agents");
            return StatusCode(500, "An error occurred during handoff workflow processing.");
        }
    }

    /// <summary>
    /// Group chat workflow - agents collaborate in a round-robin conversation pattern.
    /// </summary>
    [HttpPost("assist/groupchat")]
    public async Task<ActionResult<MultiAgentResponse>> AssistGroupChatAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        _logger.LogInformation("Starting group chat workflow with local agents for query: {ProductQuery}", request.ProductQuery);

        try
        {
            var agents = GetLocalAgents();
            var agentList = new List<AIAgent>
            {
                agents.ProductSearch,
                agents.ProductMatchmaking,
                agents.LocationService,
                agents.Navigation
            };

            var workflow = AgentWorkflowBuilder.CreateGroupChatBuilderWith(
                _ => new RoundRobinGroupChatManager(agentList) { MaximumIterationCount = 5 })
                .AddParticipants(agentList)
                .Build();

            var workflowResponse = await RunWorkflowAsync(request, workflow, agents);
            return Ok(workflowResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in group chat workflow with local agents");
            return StatusCode(500, "An error occurred during group chat workflow processing.");
        }
    }

    /// <summary>
    /// Magentic workflow - complex multi-agent collaboration (not yet implemented).
    /// </summary>
    [HttpPost("assist/magentic")]
    public async Task<ActionResult<MultiAgentResponse>> AssistMagenticAsync([FromBody] MultiAgentRequest? request)
    {
        _logger.LogInformation("MagenticOne workflow requested for query: {ProductQuery}", request?.ProductQuery);

        return StatusCode(501, 
            "The MagenticOne workflow is not yet implemented in the MAF Local framework. " +
            "Please use another orchestration type.");
    }

    /// <summary>
    /// Executes a workflow and processes the streaming events.
    /// </summary>
    private async Task<MultiAgentResponse> RunWorkflowAsync(
        MultiAgentRequest request, 
        Workflow workflow,
        (AIAgent ProductSearch, AIAgent ProductMatchmaking, AIAgent LocationService, AIAgent Navigation) agents)
    {
        var orchestrationId = Guid.NewGuid().ToString();
        var steps = new List<AgentStep>();
        string? lastExecutorId = null;

        var run = await InProcessExecution.StreamAsync(workflow, request.ProductQuery);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (var evt in run.WatchStreamAsync().ConfigureAwait(false))
        {
            ProcessWorkflowEvent(evt, steps, request, ref lastExecutorId, agents);
        }

        var mermaidChart = workflow.ToMermaidString();
        
        var alternatives = await StepsProcessor.GetProductAlternativesFromStepsAsync(
            steps, agents.ProductMatchmaking, _logger);
        var navigationInstructions = await StepsProcessor.GenerateNavigationInstructionsAsync(
            steps, agents.Navigation, request.Location, request.ProductQuery, _logger);

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = request.Orchestration,
            OrchestrationDescription = GetOrchestrationDescription(request.Orchestration),
            Steps = steps.ToArray(),
            MermaidWorkflowRepresentation = mermaidChart,
            Alternatives = alternatives,
            NavigationInstructions = navigationInstructions
        };
    }

    /// <summary>
    /// Processes workflow events and extracts agent steps.
    /// </summary>
    private void ProcessWorkflowEvent(
        WorkflowEvent evt,
        List<AgentStep> steps,
        MultiAgentRequest request,
        ref string? lastExecutorId,
        (AIAgent ProductSearch, AIAgent ProductMatchmaking, AIAgent LocationService, AIAgent Navigation) agents)
    {
        switch (evt)
        {
            case AgentRunUpdateEvent updateEvent:
                if (updateEvent.ExecutorId != lastExecutorId)
                {
                    lastExecutorId = updateEvent.ExecutorId;
                    _logger.LogDebug("ExecutorId changed to: {ExecutorId}", updateEvent.ExecutorId);
                }
                break;

            case WorkflowOutputEvent outputEvent:
                _logger.LogDebug("WorkflowOutput - SourceId: {SourceId}", outputEvent.SourceId);
                var messages = outputEvent.As<List<ChatMessage>>() ?? [];
                
                foreach (var message in messages)
                {
                    steps.Add(new AgentStep
                    {
                        Agent = GetAgentDisplayName(message.AuthorName, agents),
                        AgentId = message.AuthorName ?? string.Empty,
                        Action = $"Processing - {request.ProductQuery}",
                        Result = message.Text,
                        Timestamp = message.CreatedAt?.UtcDateTime ?? DateTime.UtcNow
                    });
                }
                break;
        }
    }

    /// <summary>
    /// Retrieves locally created agents.
    /// </summary>
    private (AIAgent ProductSearch, AIAgent ProductMatchmaking, AIAgent LocationService, AIAgent Navigation) GetLocalAgents()
    {
        return (
            ProductSearch: _localAgentProvider.GetAgentByName(AgentNamesProvider.AgentName.ProductSearchAgent),
            ProductMatchmaking: _localAgentProvider.GetAgentByName(AgentNamesProvider.AgentName.ProductMatchmakingAgent),
            LocationService: _localAgentProvider.GetAgentByName(AgentNamesProvider.AgentName.LocationServiceAgent),
            Navigation: _localAgentProvider.GetAgentByName(AgentNamesProvider.AgentName.NavigationAgent)
        );
    }

    /// <summary>
    /// Converts agent ID to human-readable display name.
    /// </summary>
    private string GetAgentDisplayName(
        string? agentId,
        (AIAgent ProductSearch, AIAgent ProductMatchmaking, AIAgent LocationService, AIAgent Navigation) agents)
    {
        if (string.IsNullOrEmpty(agentId))
            return "Unknown Agent";
        
        return agentId switch
        {
            _ when agentId == agents.LocationService.Id => "Location Service Agent (Local)",
            _ when agentId == agents.Navigation.Id => "Navigation Agent (Local)",
            _ when agentId == agents.ProductMatchmaking.Id => "Product Matchmaking Agent (Local)",
            _ when agentId == agents.ProductSearch.Id => "Product Search Agent (Local)",
            _ => agentId
        };
    }

    /// <summary>
    /// Returns a description for the orchestration type.
    /// </summary>
    private static string GetOrchestrationDescription(OrchestrationType orchestration) => orchestration switch
    {
        OrchestrationType.Sequential => 
            "Sequential workflow using MAF Local Agents (gpt-5-mini). Each agent step executes in order, with output feeding into subsequent steps.",
        OrchestrationType.Concurrent => 
            "Concurrent workflow using MAF Local Agents (gpt-5-mini). All agents execute in parallel for independent analysis.",
        OrchestrationType.Handoff => 
            "Handoff workflow using MAF Local Agents (gpt-5-mini). Agents dynamically pass control based on context and branching logic.",
        OrchestrationType.GroupChat => 
            "Group chat workflow using MAF Local Agents (gpt-5-mini). Agents collaborate in a round-robin conversation pattern.",
        OrchestrationType.Magentic => 
            "MagenticOne-inspired workflow for complex multi-agent collaboration.",
        _ => 
            "Multi-agent workflow using MAF Local Agents (gpt-5-mini)."
    };
}
