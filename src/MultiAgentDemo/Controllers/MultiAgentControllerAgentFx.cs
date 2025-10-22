using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using MultiAgentDemo.Services;
using SharedEntities;
using System.Text;

namespace MultiAgentDemo.Controllers
{
    [ApiController]
    [Route("api/multiagent/agentfx")]
    public class MultiAgentControllerAgentFx : ControllerBase
    {
        private readonly ILogger<MultiAgentControllerAgentFx> _logger;
        private readonly InventoryAgentService _inventoryAgentService;
        private readonly MatchmakingAgentService _matchmakingAgentService;
        private readonly LocationAgentService _locationAgentService;
        private readonly NavigationAgentService _navigationAgentService;
        private readonly IConfiguration _configuration;
        private readonly AIAgent _customerInformationAgent; // Added field for keyed AIAgent
        private readonly AIAgent _inventoryAgent;
        private readonly AIAgent _locationServiceAgent;
        private readonly AIAgent _navigationAgent;
        private readonly AIAgent _photoAnalyzerAgent;
        private readonly AIAgent _productMatchmakingAgent;
        private readonly AIAgent _productSearchAgent;
        private readonly AIAgent _toolReasoningAgent;

        public MultiAgentControllerAgentFx(
            ILogger<MultiAgentControllerAgentFx> logger,
            InventoryAgentService inventoryAgentService,
            MatchmakingAgentService matchmakingAgentService,
            LocationAgentService locationAgentService,
            NavigationAgentService navigationAgentService,
            IConfiguration configuration,
            [FromKeyedServices("customerinformationagentid")] AIAgent customerInformationAgent,
            [FromKeyedServices("inventoryagentid")] AIAgent inventoryAgent,
            [FromKeyedServices("locationserviceagentid")] AIAgent locationServiceAgent,
            [FromKeyedServices("navigationagentid")] AIAgent navigationAgent,
            [FromKeyedServices("photoanalyzeragentid")] AIAgent photoAnalyzerAgent,
            [FromKeyedServices("productmatchmakingagentid")] AIAgent productMatchmakingAgent,
            [FromKeyedServices("productsearchagentid")] AIAgent productSearchAgent,
            [FromKeyedServices("toolreasoningagentid")] AIAgent toolReasoningAgent)
        {
            _logger = logger;
            _inventoryAgentService = inventoryAgentService;
            _matchmakingAgentService = matchmakingAgentService;
            _locationAgentService = locationAgentService;
            _navigationAgentService = navigationAgentService;
            _configuration = configuration;
            _customerInformationAgent = customerInformationAgent; // assign
            _inventoryAgent = inventoryAgent;
            _locationServiceAgent = locationServiceAgent;
            _navigationAgent = navigationAgent;
            _photoAnalyzerAgent = photoAnalyzerAgent;
            _productMatchmakingAgent = productMatchmakingAgent;
            _productSearchAgent = productSearchAgent;
            _toolReasoningAgent = toolReasoningAgent;

            // Set framework to AgentFx for all agent services
            _inventoryAgentService.SetFramework("agentfx");
            _matchmakingAgentService.SetFramework("agentfx");
            _locationAgentService.SetFramework("agentfx");
            _navigationAgentService.SetFramework("agentfx");
        }

        [HttpPost("assist")]
        public async Task<ActionResult<MultiAgentResponse>> AssistAsync([FromBody] MultiAgentRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

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
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

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
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

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
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

            _logger.LogInformation("Starting handoff workflow with branching logic for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

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
                var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(_productSearchAgent)
                    .WithHandoff(_productSearchAgent, _productMatchmakingAgent)
                    .WithHandoff(_productMatchmakingAgent, _locationServiceAgent)
                    .WithHandoff(_locationServiceAgent, _navigationAgent)
                    .Build();
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
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

            _logger.LogInformation("Starting Magentic workflow for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();
                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Magentic,
                    OrchestrationDescription = "Magentic-style workflow using Microsoft Agent Framework. Features coordinator-directed multi-agent collaboration with distinct planning, execution, and synthesis phases. Demonstrates state management and checkpointing patterns.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Magentic workflow using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during Magentic workflow processing.");
            }
        }

        private async Task<MultiAgentResponse> RunWorkFlow(
    MultiAgentRequest request,
    Workflow workflow)
        {
            var orchestrationId = Guid.NewGuid().ToString();
            var steps = new List<AgentStep>();

            // Run the workflow
            string? lastExecutorId = null;
            List<ChatMessage> result = [];

            // sync run
            // Run run = await InProcessExecution.RunAsync(workflow, request.ProductQuery);
            // foreach (WorkflowEvent evt in run.NewEvents)

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
                            _logger.LogInformation($"Message from {message.Role}: {message.Text}");
                            steps.Add(new AgentStep
                            {
                                Agent = message.AuthorName ?? outputEvent.SourceId,
                                Action = $"Search for {request.ProductQuery}",
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

            var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);
            var navigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null;

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
            return new[]
            {
                new ProductAlternative { Name = $"Standard {productQuery}", Sku = "STD-" + productQuery.Replace(" ", "").ToUpper(), Price = 49.99m, InStock = true, Location = "Aisle 5", Aisle = 5, Section = "B" },
                new ProductAlternative { Name = $"Budget {productQuery}", Sku = "BDG-" + productQuery.Replace(" ", "").ToUpper(), Price = 24.99m, InStock = false, Location = "Aisle 12", Aisle = 12, Section = "C" }
            };
        }
    }
}