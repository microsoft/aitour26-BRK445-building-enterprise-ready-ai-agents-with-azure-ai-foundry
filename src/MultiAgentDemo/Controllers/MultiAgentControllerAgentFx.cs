using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using SharedEntities;
using System.Text;

namespace MultiAgentDemo.Controllers
{
    [ApiController]
    [Route("api/multiagent/agentfx")]
    public class MultiAgentControllerAgentFx : ControllerBase
    {
        private readonly ILogger<MultiAgentControllerAgentFx> _logger;
        private readonly AIAgent _locationServiceAgent;
        private readonly AIAgent _navigationAgent;
        private readonly AIAgent _productMatchmakingAgent;
        private readonly AIAgent _productSearchAgent;

        public MultiAgentControllerAgentFx(
            ILogger<MultiAgentControllerAgentFx> logger,
            [FromKeyedServices("locationserviceagentid")] AIAgent locationServiceAgent,
            [FromKeyedServices("navigationagentid")] AIAgent navigationAgent,
            [FromKeyedServices("productmatchmakingagentid")] AIAgent productMatchmakingAgent,
            [FromKeyedServices("productsearchagentid")] AIAgent productSearchAgent)
        {
            _logger = logger;
            _locationServiceAgent = locationServiceAgent;
            _navigationAgent = navigationAgent;
            _productMatchmakingAgent = productMatchmakingAgent;
            _productSearchAgent = productSearchAgent;
        }

        [HttpPost("assist")]
        public async Task<ActionResult<MultiAgentResponse>> AssistAsync([FromBody] MultiAgentRequest? request)
        {
            _logger.LogInformation("Starting {OrchestrationTypeName} orchestration for query: {ProductQuery} using Microsoft Agent Framework",
                request.OrchestationType, request.ProductQuery);

            try
            {
                // Route to specific orchestration based on type
                return request.OrchestationType switch
                {
                    OrchestationType.Sequential => await AssistSequentialAsync(request),
                    OrchestationType.Concurrent => await AssistConcurrentAsync(request),
                    OrchestationType.Handoff => await AssistHandoffAsync(request),
                    OrchestationType.GroupChat => await AssistGroupChatAsync(request),
                    OrchestationType.Magentic => await AssistMagenticAsync(request),
                    _ => await AssistSequentialAsync(request)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {OrchestrationTypeName} orchestration using Microsoft Agent Framework", request.OrchestationType);
                return StatusCode(500, "An error occurred during orchestration processing.");
            }
        }

        [HttpPost("assist/sequential")]
        public async Task<ActionResult<MultiAgentResponse>> AssistSequentialAsync([FromBody] MultiAgentRequest? request)
        {
            _logger.LogInformation("Starting sequential workflow for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                // build the sequential workflow
                var agents = new List<AIAgent>
                {
                    _productSearchAgent,
                    _productMatchmakingAgent,
                    _locationServiceAgent,
                    _navigationAgent
                };
                var workflow = AgentWorkflowBuilder.BuildSequential(agents);

                // run the workflow
                var workflowResponse = await RunWorkFlow(request, workflow);
                return Ok(workflowResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sequential workflow using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during sequential workflow processing.");
            }
        }

        [HttpPost("assist/concurrent")]
        public async Task<ActionResult<MultiAgentResponse>> AssistConcurrentAsync([FromBody] MultiAgentRequest? request)
        {
            _logger.LogInformation("Starting concurrent workflow for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                // build the concurrent workflow
                var agents = new List<AIAgent>
                {
                    _productSearchAgent,
                    _productMatchmakingAgent,
                    _locationServiceAgent,
                    _navigationAgent
                };
                var workflow = AgentWorkflowBuilder.BuildConcurrent(agents);
                
                // run the workflow
                var workflowResponse = await RunWorkFlow(request, workflow);
                return Ok(workflowResponse);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in concurrent workflow using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during concurrent workflow processing.");
            }
        }

        [HttpPost("assist/handoff")]
        public async Task<ActionResult<MultiAgentResponse>> AssistHandoffAsync([FromBody] MultiAgentRequest? request)
        {
            _logger.LogInformation("Starting handoff workflow with branching logic for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                // build the product handoff workflow                
                var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(_productSearchAgent)
                    .WithHandoff(_productSearchAgent, _productMatchmakingAgent)
                    .WithHandoff(_productMatchmakingAgent, _locationServiceAgent)
                    .WithHandoff(_locationServiceAgent, _navigationAgent)
                    .Build();

                // run the workflow
                var workflowResponse = await RunWorkFlow(request, workflow);
                return Ok(workflowResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handoff workflow using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during handoff workflow processing.");
            }
        }

        [HttpPost("assist/groupchat")]
        public async Task<ActionResult<MultiAgentResponse>> AssistGroupChatAsync([FromBody] MultiAgentRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

            _logger.LogInformation("Starting group chat workflow for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                // build the product handoff workflow
                var agents = new List<AIAgent>
                {
                    _productSearchAgent,
                    _productMatchmakingAgent,
                    _locationServiceAgent,
                    _navigationAgent
                };
                var workflow = AgentWorkflowBuilder.CreateGroupChatBuilderWith(
                    agents => new RoundRobinGroupChatManager(agents)
                    { MaximumIterationCount = 5 })
                    .AddParticipants(agents)
                    .Build();

                // run the workflow
                var workflowResponse = await RunWorkFlow(request, workflow);
                return Ok(workflowResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in group chat workflow using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during group chat workflow processing.");
            }
        }

        [HttpPost("assist/magentic")]
        public async Task<ActionResult<MultiAgentResponse>> AssistMagenticAsync([FromBody] MultiAgentRequest? request)
        {
            _logger.LogInformation("Starting Magentic workflow for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                throw new NotImplementedException("The Magentic workflow is not implemented yet.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Magentic workflow using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during Magentic workflow processing.");
            }
        }

        private async Task<MultiAgentResponse> RunWorkFlow(MultiAgentRequest request, Workflow workflow)
        {
            var orchestrationId = Guid.NewGuid().ToString();
            var steps = new List<AgentStep>();
            string? lastExecutorId = null;
            List<ChatMessage> result = [];

            // async run
            StreamingRun run = await InProcessExecution.StreamAsync(workflow, request.ProductQuery);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
            await foreach (WorkflowEvent evt in run.WatchStreamAsync().ConfigureAwait(false))
            {
                switch (evt)
                {
                    case AgentRunUpdateEvent e:
                        if (e.ExecutorId != lastExecutorId)
                        {
                            lastExecutorId = e.ExecutorId;
                            _logger.LogInformation($"ExecutorId >> {e.ExecutorId}");
                        }
                        break;

                    case WorkflowOutputEvent outputEvent:
                        _logger.LogInformation($"WorkflowOutputEvent >> SourceId: {outputEvent.SourceId} - Data: {outputEvent.Data}");
                        var messages = outputEvent.As<List<ChatMessage>>() ?? new List<ChatMessage>();
                        foreach (var message in messages)
                        {
                            var agentName = GetAgentName(message.AuthorName);

                            steps.Add(new AgentStep
                            {
                                Agent = agentName,
                                AgentId = message.AuthorName,
                                Action = $"Processing - {request.ProductQuery}",
                                Result = message.Text,
                                Timestamp = message.CreatedAt.HasValue ? message.CreatedAt.Value.UtcDateTime : DateTime.UtcNow
                            });
                        }
                        break;

                    default:
                        break;
                }
            }

            // get the mermaid representation
            var mermaidWorkflowChart = workflow.ToMermaidString();

            // get typed responses from steps
            var alternatives = await StepsProcessor.GetProductAlternativesFromStepsAsync(steps, _productMatchmakingAgent, _logger);
            var navigationInstructions = await StepsProcessor.GenerateNavigationInstructionsAsync(steps, _navigationAgent, request.Location, request.ProductQuery, _logger);

            return new MultiAgentResponse
            {
                OrchestrationId = orchestrationId,
                OrchestationType = OrchestationType.Sequential,
                OrchestrationDescription = "Sequential workflow using Microsoft Agent Framework. Each agent step executes in order, with output feeding into subsequent steps. This enables complex, dependent reasoning chains.",
                Steps = steps.ToArray(),
                MermaidWorkflowRepresentation = mermaidWorkflowChart,
                Alternatives = alternatives,
                NavigationInstructions = navigationInstructions
            };
        }

        private string GetAgentName(string agentId)
        {
            var agentName = agentId;

            if (_locationServiceAgent.Id == agentId)
            {
                agentName = "Location Service Agent";
            }
            else if (_navigationAgent.Id == agentId)
            {
                agentName = "Navigation Agent";
            }
            else if (_productMatchmakingAgent.Id == agentId)
            {
                agentName = "Product Matchmaking Agent";
            }
            else if (_productSearchAgent.Id == agentId)
            {
                agentName = "Product Search Agent";
            }

            return agentName;
        }
   }
}