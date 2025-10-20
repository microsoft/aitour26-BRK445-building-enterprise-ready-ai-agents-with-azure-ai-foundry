using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using MultiAgentDemo.Services;
using SharedEntities;
using ZavaAgentFxAgentsProvider;

namespace MultiAgentDemo.Controllers
{
    [ApiController]
    [Route("api/multiagent")]
    public class MultiAgentControllerAgentFx : ControllerBase
    {
        private readonly ILogger<MultiAgentControllerAgentFx> _logger;
        private readonly InventoryAgentService _inventoryAgentService;
        private readonly MatchmakingAgentService _matchmakingAgentService;
        private readonly LocationAgentService _locationAgentService;
        private readonly NavigationAgentService _navigationAgentService;
        private readonly SequentialOrchestrationService _sequentialOrchestration;
        private readonly ConcurrentOrchestrationService _concurrentOrchestration;
        private readonly HandoffOrchestrationService _handoffOrchestration;
        private readonly GroupChatOrchestrationService _groupChatOrchestration;
        private readonly MagenticOrchestrationService _magenticOrchestration;
        private readonly AgentFxAgentProvider _agentFxAgentProvider;
        private readonly IConfiguration _configuration;

        public MultiAgentControllerAgentFx(
            ILogger<MultiAgentControllerAgentFx> logger,
            InventoryAgentService inventoryAgentService,
            MatchmakingAgentService matchmakingAgentService,
            LocationAgentService locationAgentService,
            NavigationAgentService navigationAgentService,
            SequentialOrchestrationService sequentialOrchestration,
            ConcurrentOrchestrationService concurrentOrchestration,
            HandoffOrchestrationService handoffOrchestration,
            GroupChatOrchestrationService groupChatOrchestration,
            MagenticOrchestrationService magenticOrchestration,
            AgentFxAgentProvider agentFxAgentProvider,
            IConfiguration configuration)
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
            _agentFxAgentProvider = agentFxAgentProvider;
            _configuration = configuration;
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
                var orchestrationService = GetOrchestrationService(request.OrchestationType);
                var response = await orchestrationService.ExecuteAsync(request);
                return Ok(response);
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

            _logger.LogInformation("Starting sequential orchestration for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Get agents using AgentFx provider
                var inventoryAgentId = _configuration.GetConnectionString("inventoryagentid");
                var matchmakingAgentId = _configuration.GetConnectionString("productmatchmakingagentid");
                var locationAgentId = _configuration.GetConnectionString("locationserviceagentid");
                var navigationAgentId = _configuration.GetConnectionString("navigationagentid");

                // Invoke agents sequentially using Microsoft Agent Framework
                // Note: This is a simplified implementation demonstrating the AgentFx integration
                steps.Add(new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = $"Search for {request.ProductQuery} using Microsoft Agent Framework",
                    Result = $"Microsoft Agent Framework: Found products matching '{request.ProductQuery}'",
                    Timestamp = DateTime.UtcNow
                });

                steps.Add(new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = "Find alternatives using Microsoft Agent Framework",
                    Result = "Microsoft Agent Framework: Identified product alternatives",
                    Timestamp = DateTime.UtcNow
                });

                steps.Add(new AgentStep
                {
                    Agent = "LocationAgent",
                    Action = "Locate products using Microsoft Agent Framework",
                    Result = "Microsoft Agent Framework: Located products in store",
                    Timestamp = DateTime.UtcNow
                });

                if (request.Location != null)
                {
                    steps.Add(new AgentStep
                    {
                        Agent = "NavigationAgent",
                        Action = "Generate navigation using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework: Navigation instructions generated",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Sequential,
                    OrchestrationDescription = "Agents executed sequentially using Microsoft Agent Framework, with each agent building upon the results of the previous agent's work.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sequential orchestration using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during sequential orchestration processing.");
            }
        }

        [HttpPost("assist/concurrent")]
        public async Task<ActionResult<MultiAgentResponse>> AssistConcurrentAsync([FromBody] MultiAgentRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

            _logger.LogInformation("Starting concurrent orchestration for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>
                {
                    new AgentStep
                    {
                        Agent = "InventoryAgent",
                        Action = $"Concurrent search for {request.ProductQuery} using Microsoft Agent Framework",
                        Result = $"Microsoft Agent Framework Concurrent: Found inventory for '{request.ProductQuery}'",
                        Timestamp = DateTime.UtcNow
                    },
                    new AgentStep
                    {
                        Agent = "MatchmakingAgent",
                        Action = $"Concurrent alternatives for {request.ProductQuery} using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework Concurrent: Identified alternatives",
                        Timestamp = DateTime.UtcNow
                    },
                    new AgentStep
                    {
                        Agent = "LocationAgent",
                        Action = $"Concurrent location search for {request.ProductQuery} using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework Concurrent: Located products",
                        Timestamp = DateTime.UtcNow
                    }
                };

                if (request.Location != null)
                {
                    steps.Add(new AgentStep
                    {
                        Agent = "NavigationAgent",
                        Action = $"Concurrent navigation for {request.ProductQuery} using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework Concurrent: Navigation generated",
                        Timestamp = DateTime.UtcNow
                    });
                }

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Concurrent,
                    OrchestrationDescription = "All agents executed concurrently using Microsoft Agent Framework, working independently in parallel without dependencies on each other's results.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in concurrent orchestration using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during concurrent orchestration processing.");
            }
        }

        [HttpPost("assist/handoff")]
        public async Task<ActionResult<MultiAgentResponse>> AssistHandoffAsync([FromBody] MultiAgentRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

            _logger.LogInformation("Starting handoff orchestration for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>
                {
                    new AgentStep
                    {
                        Agent = "RouterAgent",
                        Action = "Route initial query using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework: Routing to InventoryAgent",
                        Timestamp = DateTime.UtcNow
                    },
                    new AgentStep
                    {
                        Agent = "InventoryAgent",
                        Action = "Handle inventory query using Microsoft Agent Framework",
                        Result = $"Microsoft Agent Framework Handoff: Processing '{request.ProductQuery}'",
                        Timestamp = DateTime.UtcNow
                    }
                };

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Handoff,
                    OrchestrationDescription = "Dynamic handoff orchestration using Microsoft Agent Framework, where agents pass control based on context and business logic rules.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handoff orchestration using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during handoff orchestration processing.");
            }
        }

        [HttpPost("assist/groupchat")]
        public async Task<ActionResult<MultiAgentResponse>> AssistGroupChatAsync([FromBody] MultiAgentRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

            _logger.LogInformation("Starting group chat orchestration for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>
                {
                    new AgentStep
                    {
                        Agent = "GroupManager",
                        Action = "Initialize group discussion using Microsoft Agent Framework",
                        Result = $"Microsoft Agent Framework: Group chat started for query: {request.ProductQuery}",
                        Timestamp = DateTime.UtcNow
                    },
                    new AgentStep
                    {
                        Agent = "InventoryAgent",
                        Action = "Group discussion turn 1 using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework: Sharing inventory findings with the group",
                        Timestamp = DateTime.UtcNow
                    },
                    new AgentStep
                    {
                        Agent = "MatchmakingAgent",
                        Action = "Group discussion turn 2 using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework: Building on inventory findings for alternatives",
                        Timestamp = DateTime.UtcNow
                    }
                };

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.GroupChat,
                    OrchestrationDescription = "Collaborative group chat orchestration using Microsoft Agent Framework, where agents discuss and build consensus through multi-turn conversations.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in group chat orchestration using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during group chat orchestration processing.");
            }
        }

        [HttpPost("assist/magentic")]
        public async Task<ActionResult<MultiAgentResponse>> AssistMagenticAsync([FromBody] MultiAgentRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

            _logger.LogInformation("Starting Magentic orchestration for query: {ProductQuery} using Microsoft Agent Framework", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>
                {
                    new AgentStep
                    {
                        Agent = "MagenticCoordinator",
                        Action = "Analyze and plan workflow using Microsoft Agent Framework",
                        Result = $"Microsoft Agent Framework Magentic: Analyzing '{request.ProductQuery}' and planning workflow",
                        Timestamp = DateTime.UtcNow
                    },
                    new AgentStep
                    {
                        Agent = "InventoryAgent",
                        Action = "Execute coordinated inventory search using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework Magentic: Inventory analysis complete",
                        Timestamp = DateTime.UtcNow
                    },
                    new AgentStep
                    {
                        Agent = "MagenticCoordinator",
                        Action = "Synthesize and coordinate final response using Microsoft Agent Framework",
                        Result = "Microsoft Agent Framework Magentic: Coordinated multi-agent collaboration complete",
                        Timestamp = DateTime.UtcNow
                    }
                };

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Magentic,
                    OrchestrationDescription = "Magentic orchestration using Microsoft Agent Framework, featuring coordinator-directed multi-agent collaboration with planning, execution, and synthesis phases.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Magentic orchestration using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during Magentic orchestration processing.");
            }
        }

        private IAgentOrchestrationService GetOrchestrationService(OrchestationType orchestrationType)
        {
            return orchestrationType switch
            {
                OrchestationType.Sequential => _sequentialOrchestration,
                OrchestationType.Concurrent => _concurrentOrchestration,
                OrchestationType.Handoff => _handoffOrchestration,
                OrchestationType.GroupChat => _groupChatOrchestration,
                OrchestationType.Magentic => _magenticOrchestration,
                _ => _sequentialOrchestration
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
                new ProductAlternative { Name = $"Standard {productQuery}", Sku = "STD-" + productQuery.Replace(" ", "").ToUpper(), Price = 49.99m, InStock = true, Location = "Aisle 5", Aisle = 5, Section = "B" },
                new ProductAlternative { Name = $"Budget {productQuery}", Sku = "BDG-" + productQuery.Replace(" ", "").ToUpper(), Price = 24.99m, InStock = false, Location = "Aisle 12", Aisle = 12, Section = "C" }
            };
        }
    }
}
