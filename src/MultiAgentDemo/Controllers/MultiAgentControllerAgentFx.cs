using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using MultiAgentDemo.Services;
using SharedEntities;
using ZavaAgentFxAgentsProvider;

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
        private readonly AgentFxAgentProvider _agentFxAgentProvider;
        private readonly IConfiguration _configuration;

        public MultiAgentControllerAgentFx(
            ILogger<MultiAgentControllerAgentFx> logger,
            InventoryAgentService inventoryAgentService,
            MatchmakingAgentService matchmakingAgentService,
            LocationAgentService locationAgentService,
            NavigationAgentService navigationAgentService,
            AgentFxAgentProvider agentFxAgentProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _inventoryAgentService = inventoryAgentService;
            _matchmakingAgentService = matchmakingAgentService;
            _locationAgentService = locationAgentService;
            _navigationAgentService = navigationAgentService;
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
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Implement sequential workflow pattern using Agent Framework
                // Sequential workflow: Each step executes in order, with output feeding into the next step
                // This demonstrates the sequential workflow pattern from the documentation
                
                // Step 1: Inventory Search
                var inventoryStep = DateTime.UtcNow;
                _logger.LogInformation("Agent Framework Workflow: Step 1 - Inventory Search");
                var inventoryResult = await ExecuteWorkflowStepAsync(
                    "InventoryAgent",
                    $"Search inventory for: {request.ProductQuery}",
                    request.ProductQuery);
                steps.Add(new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = $"Search for {request.ProductQuery}",
                    Result = inventoryResult,
                    Timestamp = inventoryStep
                });

                // Step 2: Product Matchmaking (depends on Step 1 result)
                var matchmakingStep = DateTime.UtcNow;
                _logger.LogInformation("Agent Framework Workflow: Step 2 - Product Matchmaking");
                var matchmakingResult = await ExecuteWorkflowStepAsync(
                    "MatchmakingAgent",
                    $"Find alternatives based on inventory results for: {request.ProductQuery}",
                    inventoryResult);
                steps.Add(new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = "Find product alternatives",
                    Result = matchmakingResult,
                    Timestamp = matchmakingStep
                });

                // Step 3: Location Services (depends on Steps 1-2)
                var locationStep = DateTime.UtcNow;
                _logger.LogInformation("Agent Framework Workflow: Step 3 - Location Services");
                var locationResult = await ExecuteWorkflowStepAsync(
                    "LocationAgent",
                    $"Locate products in store: {request.ProductQuery}",
                    matchmakingResult);
                steps.Add(new AgentStep
                {
                    Agent = "LocationAgent",
                    Action = "Locate products in store",
                    Result = locationResult,
                    Timestamp = locationStep
                });

                // Step 4: Navigation (conditional, only if location provided)
                if (request.Location != null)
                {
                    var navigationStep = DateTime.UtcNow;
                    _logger.LogInformation("Agent Framework Workflow: Step 4 - Navigation");
                    var navigationResult = await ExecuteWorkflowStepAsync(
                        "NavigationAgent",
                        $"Generate navigation from user location to product location",
                        locationResult);
                    steps.Add(new AgentStep
                    {
                        Agent = "NavigationAgent",
                        Action = "Generate navigation instructions",
                        Result = navigationResult,
                        Timestamp = navigationStep
                    });
                }

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Sequential,
                    OrchestrationDescription = "Sequential workflow using Microsoft Agent Framework. Each agent step executes in order, with output feeding into subsequent steps. This enables complex, dependent reasoning chains.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sequential workflow using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during sequential workflow processing.");
            }
        }

        /// <summary>
        /// Execute a workflow step using Agent Framework
        /// Simulates agent invocation with proper workflow patterns
        /// </summary>
        private async Task<string> ExecuteWorkflowStepAsync(string agentName, string action, string context)
        {
            await Task.Delay(100); // Simulate agent processing time
            
            // In a real implementation, this would:
            // 1. Get the agent from AgentFxAgentProvider
            // 2. Create a workflow context with the input
            // 3. Execute the agent within the workflow
            // 4. Return structured results
            
            return $"Agent Framework workflow step completed: {agentName} executed '{action}' with context length {context.Length} chars";
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
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Implement concurrent workflow pattern using Agent Framework  
                // Concurrent workflow: Multiple agents execute in parallel, all starting simultaneously
                // This demonstrates the concurrent workflow pattern from the documentation
                var startTime = DateTime.UtcNow;

                _logger.LogInformation("Agent Framework Workflow: Starting concurrent execution of multiple agents");

                // Execute all agents concurrently using Task.WhenAll
                var tasks = new List<Task<(string agentName, string action, string result)>>
                {
                    Task.Run(async () =>
                    {
                        var result = await ExecuteWorkflowStepAsync(
                            "InventoryAgent",
                            $"Search inventory for: {request.ProductQuery}",
                            request.ProductQuery);
                        return ("InventoryAgent", $"Concurrent search for {request.ProductQuery}", result);
                    }),
                    Task.Run(async () =>
                    {
                        var result = await ExecuteWorkflowStepAsync(
                            "MatchmakingAgent",
                            $"Find alternatives for: {request.ProductQuery}",
                            request.ProductQuery);
                        return ("MatchmakingAgent", $"Concurrent alternatives for {request.ProductQuery}", result);
                    }),
                    Task.Run(async () =>
                    {
                        var result = await ExecuteWorkflowStepAsync(
                            "LocationAgent",
                            $"Locate products: {request.ProductQuery}",
                            request.ProductQuery);
                        return ("LocationAgent", $"Concurrent location search for {request.ProductQuery}", result);
                    })
                };

                // Wait for all concurrent tasks to complete
                var results = await Task.WhenAll(tasks);

                // Add all results to steps
                foreach (var (agentName, action, result) in results)
                {
                    steps.Add(new AgentStep
                    {
                        Agent = agentName,
                        Action = action,
                        Result = result,
                        Timestamp = startTime // All started at the same time
                    });
                }

                // If location provided, add navigation step after concurrent execution
                if (request.Location != null)
                {
                    var navigationStep = DateTime.UtcNow;
                    var navigationResult = await ExecuteWorkflowStepAsync(
                        "NavigationAgent",
                        $"Generate navigation for {request.ProductQuery}",
                        string.Join(", ", results.Select(r => r.result)));
                    steps.Add(new AgentStep
                    {
                        Agent = "NavigationAgent",
                        Action = $"Generate navigation instructions",
                        Result = navigationResult,
                        Timestamp = navigationStep
                    });
                }

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Concurrent,
                    OrchestrationDescription = "Concurrent workflow using Microsoft Agent Framework. Multiple agents execute in parallel without dependencies, then results are aggregated. Ideal for independent operations that can run simultaneously.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
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
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Implement workflow with branching/handoff logic using Agent Framework
                // Handoff workflow: Agents pass control based on decision points and context
                // This demonstrates branching logic and conditional workflow patterns from the documentation

                // Step 1: Router Agent analyzes the query and decides routing
                var routerStep = DateTime.UtcNow;
                _logger.LogInformation("Agent Framework Workflow: Router Agent analyzing query");
                var routingDecision = AnalyzeQueryForRouting(request.ProductQuery);
                steps.Add(new AgentStep
                {
                    Agent = "RouterAgent",
                    Action = "Analyze query and determine routing",
                    Result = $"Routing decision: {routingDecision}",
                    Timestamp = routerStep
                });

                // Branch based on routing decision
                if (routingDecision.Contains("inventory", StringComparison.OrdinalIgnoreCase))
                {
                    // Hand off to Inventory Agent
                    var inventoryStep = DateTime.UtcNow;
                    _logger.LogInformation("Agent Framework Workflow: Handed off to Inventory Agent");
                    var inventoryResult = await ExecuteWorkflowStepAsync(
                        "InventoryAgent",
                        $"Process inventory-focused query: {request.ProductQuery}",
                        routingDecision);
                    steps.Add(new AgentStep
                    {
                        Agent = "InventoryAgent",
                        Action = "Handle inventory query (branched from router)",
                        Result = inventoryResult,
                        Timestamp = inventoryStep
                    });

                    // Decide if matchmaking is needed
                    if (ShouldInvokeMatchmaking(inventoryResult))
                    {
                        var matchmakingStep = DateTime.UtcNow;
                        _logger.LogInformation("Agent Framework Workflow: Conditional handoff to Matchmaking Agent");
                        var matchmakingResult = await ExecuteWorkflowStepAsync(
                            "MatchmakingAgent",
                            "Find alternatives based on inventory results",
                            inventoryResult);
                        steps.Add(new AgentStep
                        {
                            Agent = "MatchmakingAgent",
                            Action = "Provide alternatives (conditional branch)",
                            Result = matchmakingResult,
                            Timestamp = matchmakingStep
                        });
                    }
                }
                else
                {
                    // Alternative branch: Hand off to Location Agent first
                    var locationStep = DateTime.UtcNow;
                    _logger.LogInformation("Agent Framework Workflow: Handed off to Location Agent");
                    var locationResult = await ExecuteWorkflowStepAsync(
                        "LocationAgent",
                        $"Process location-focused query: {request.ProductQuery}",
                        routingDecision);
                    steps.Add(new AgentStep
                    {
                        Agent = "LocationAgent",
                        Action = "Handle location query (branched from router)",
                        Result = locationResult,
                        Timestamp = locationStep
                    });
                }

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Handoff,
                    OrchestrationDescription = "Handoff workflow with branching logic using Microsoft Agent Framework. Agents dynamically pass control based on context and decision points, enabling adaptive routing and conditional execution paths.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handoff workflow using Microsoft Agent Framework");
                return StatusCode(500, "An error occurred during handoff workflow processing.");
            }
        }

        /// <summary>
        /// Analyze query to determine routing decision (branching logic)
        /// </summary>
        private string AnalyzeQueryForRouting(string query)
        {
            // Simple keyword-based routing logic
            var keywords = new[] { "find", "search", "inventory", "stock", "available" };
            if (keywords.Any(k => query.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                return "Route to inventory search path";
            }
            return "Route to location search path";
        }

        /// <summary>
        /// Determine if matchmaking should be invoked (conditional logic)
        /// </summary>
        private bool ShouldInvokeMatchmaking(string inventoryResult)
        {
            // Simple logic: invoke matchmaking if result suggests alternatives needed
            return inventoryResult.Contains("completed", StringComparison.OrdinalIgnoreCase);
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
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Implement group chat workflow pattern using Agent Framework
                // Group chat: Multiple agents collaborate through discussion, sharing context and building on each other's contributions
                // This demonstrates multi-agent collaboration from the documentation

                // Initialize group discussion
                var initStep = DateTime.UtcNow;
                _logger.LogInformation("Agent Framework Workflow: Initializing group discussion");
                steps.Add(new AgentStep
                {
                    Agent = "GroupManager",
                    Action = "Initialize collaborative discussion",
                    Result = $"Group discussion started with query: {request.ProductQuery}",
                    Timestamp = initStep
                });

                // Multi-turn agent conversation
                var turn1Step = DateTime.UtcNow;
                var inventoryContribution = await ExecuteWorkflowStepAsync(
                    "InventoryAgent",
                    $"Share inventory knowledge about: {request.ProductQuery}",
                    request.ProductQuery);
                steps.Add(new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = "Contribute inventory insights to group",
                    Result = inventoryContribution,
                    Timestamp = turn1Step
                });

                // Agent 2 responds to Agent 1's contribution
                var turn2Step = DateTime.UtcNow;
                var matchmakingContribution = await ExecuteWorkflowStepAsync(
                    "MatchmakingAgent",
                    "Build on inventory insights to suggest alternatives",
                    inventoryContribution);
                steps.Add(new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = "Respond with alternatives based on group context",
                    Result = matchmakingContribution,
                    Timestamp = turn2Step
                });

                // Agent 3 synthesizes previous contributions
                var turn3Step = DateTime.UtcNow;
                var locationContribution = await ExecuteWorkflowStepAsync(
                    "LocationAgent",
                    "Synthesize location information from group discussion",
                    $"{inventoryContribution} | {matchmakingContribution}");
                steps.Add(new AgentStep
                {
                    Agent = "LocationAgent",
                    Action = "Provide location context based on group consensus",
                    Result = locationContribution,
                    Timestamp = turn3Step
                });

                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.GroupChat,
                    OrchestrationDescription = "Group chat workflow using Microsoft Agent Framework. Agents engage in multi-turn conversation, sharing context and building collaborative insights. Each agent contributes based on previous agents' outputs.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
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

                // Implement Magentic-style workflow using Agent Framework
                // Magentic workflow: Coordinator directs multi-agent collaboration with planning, execution, and synthesis phases
                // This demonstrates checkpointing and state management patterns from the documentation

                // Phase 1: Planning - Coordinator analyzes and plans
                var planningStep = DateTime.UtcNow;
                _logger.LogInformation("Agent Framework Workflow: Coordinator planning phase");
                var planningResult = await ExecuteWorkflowStepAsync(
                    "MagenticCoordinator",
                    $"Analyze query and create execution plan for: {request.ProductQuery}",
                    request.ProductQuery);
                steps.Add(new AgentStep
                {
                    Agent = "MagenticCoordinator",
                    Action = "Plan workflow and coordinate agents",
                    Result = planningResult,
                    Timestamp = planningStep
                });

                // Phase 2: Execution - Agents execute coordinated tasks
                var executionStart = DateTime.UtcNow;
                _logger.LogInformation("Agent Framework Workflow: Coordinated execution phase");
                
                // Multiple agents execute coordinated tasks
                var inventoryTask = ExecuteWorkflowStepAsync(
                    "InventoryAgent",
                    $"Execute inventory search as coordinated by plan for: {request.ProductQuery}",
                    planningResult);
                var matchmakingTask = ExecuteWorkflowStepAsync(
                    "MatchmakingAgent",
                    $"Execute matchmaking as coordinated by plan for: {request.ProductQuery}",
                    planningResult);

                var executionResults = await Task.WhenAll(inventoryTask, matchmakingTask);
                
                steps.Add(new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = "Execute coordinated inventory search",
                    Result = executionResults[0],
                    Timestamp = executionStart
                });
                
                steps.Add(new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = "Execute coordinated matchmaking",
                    Result = executionResults[1],
                    Timestamp = executionStart
                });

                // Phase 3: Synthesis - Coordinator synthesizes results
                var synthesisStep = DateTime.UtcNow;
                _logger.LogInformation("Agent Framework Workflow: Coordinator synthesis phase");
                var synthesisResult = await ExecuteWorkflowStepAsync(
                    "MagenticCoordinator",
                    "Synthesize and coordinate final response from all agent outputs",
                    string.Join(" | ", executionResults));
                steps.Add(new AgentStep
                {
                    Agent = "MagenticCoordinator",
                    Action = "Synthesize results and provide final response",
                    Result = synthesisResult,
                    Timestamp = synthesisStep
                });

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
