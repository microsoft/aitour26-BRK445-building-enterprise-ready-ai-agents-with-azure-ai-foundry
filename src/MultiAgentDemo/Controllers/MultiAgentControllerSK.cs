#pragma warning disable SKEXP0110
#pragma warning disable SKEXP0001

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using MultiAgentDemo.Services;
using SharedEntities;
using ZavaAIFoundrySKAgentsProvider;
using ZavaSemanticKernelProvider;

namespace MultiAgentDemo.Controllers
{
    [ApiController]
    [Route("api/multiagent/sk")]
    public class MultiAgentControllerSK : ControllerBase
    {
        private readonly ILogger<MultiAgentControllerSK> _logger;
        private readonly Kernel _kernel;
        private readonly InventoryAgentService _inventoryAgentService;
        private readonly MatchmakingAgentService _matchmakingAgentService;
        private readonly LocationAgentService _locationAgentService;
        private readonly NavigationAgentService _navigationAgentService;
        private readonly SequentialOrchestrationService _sequentialOrchestration;
        private readonly ConcurrentOrchestrationService _concurrentOrchestration;
        private readonly HandoffOrchestrationService _handoffOrchestration;
        private readonly GroupChatOrchestrationService _groupChatOrchestration;
        private readonly MagenticOrchestrationService _magenticOrchestration;
        private readonly AIFoundryAgentProvider _aIFoundryAgentProvider;
        private readonly IConfiguration _configuration;
        private AzureAIAgent _agent;

        public MultiAgentControllerSK(
            ILogger<MultiAgentControllerSK> logger,
            InventoryAgentService inventoryAgentService,
            MatchmakingAgentService matchmakingAgentService,
            LocationAgentService locationAgentService,
            NavigationAgentService navigationAgentService,
            SemanticKernelProvider semanticKernelProvider,
            SequentialOrchestrationService sequentialOrchestration,
            ConcurrentOrchestrationService concurrentOrchestration,
            HandoffOrchestrationService handoffOrchestration,
            GroupChatOrchestrationService groupChatOrchestration,
            MagenticOrchestrationService magenticOrchestration,
            AIFoundryAgentProvider aIFoundryAgentProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _kernel = semanticKernelProvider.GetKernel();
            _inventoryAgentService = inventoryAgentService;
            _matchmakingAgentService = matchmakingAgentService;
            _locationAgentService = locationAgentService;
            _navigationAgentService = navigationAgentService;
            _sequentialOrchestration = sequentialOrchestration;
            _concurrentOrchestration = concurrentOrchestration;
            _handoffOrchestration = handoffOrchestration;
            _groupChatOrchestration = groupChatOrchestration;
            _magenticOrchestration = magenticOrchestration;
            _aIFoundryAgentProvider = aIFoundryAgentProvider;
            _configuration = configuration;

            // Set framework to SK for all agent services
            _inventoryAgentService.SetFramework("sk");
            _matchmakingAgentService.SetFramework("sk");
            _locationAgentService.SetFramework("sk");
            _navigationAgentService.SetFramework("sk");


        }

        [HttpPost("assist")]
        public async Task<ActionResult<MultiAgentResponse>> AssistAsync([FromBody] MultiAgentRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
            {
                return BadRequest("Request body is required and must include a ProductQuery.");
            }

            _logger.LogInformation("Starting {OrchestrationTypeName} orchestration for query: {ProductQuery}", 
                request.OrchestationType, request.ProductQuery);

            try
            {
                var orchestrationService = GetOrchestrationService(request.OrchestationType);
                var response = await orchestrationService.ExecuteAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {OrchestrationTypeName} orchestration", request.OrchestationType);
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

            _logger.LogInformation("Starting sequential orchestration for query: {ProductQuery}", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Create specialized agents using ChatCompletionAgent
                var inventoryAgent = await GetInventoryAgentAsync();
                var matchmakingAgent = await GetMatchMakingAgentAsync();
                var locationAgent = await GetLocationAgentAsync(); 
                var navigationAgent = await GetNavigationAgentAsync();

                // Create a chat history for sequential conversation
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage($"I'm looking for: {request.ProductQuery}. Please help me find this product.");

                // Step 1: Inventory Agent
                var inventoryResults = new List<ChatMessageContent>();
                await foreach (var response in inventoryAgent.InvokeAsync(chatHistory))
                {
                    inventoryResults.Add(response);
                }
                var inventoryResponse = inventoryResults.LastOrDefault()?.Content ?? "No inventory response";
                chatHistory.AddAssistantMessage(inventoryResponse);
                steps.Add(new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = $"Search for {request.ProductQuery}",
                    Result = inventoryResponse,
                    Timestamp = DateTime.UtcNow
                });

                // Step 2: Matchmaking Agent (builds on inventory results)
                chatHistory.AddUserMessage("Based on the inventory findings above, what alternatives and similar products would you recommend?");
                var matchmakingResults = new List<ChatMessageContent>();
                await foreach (var response in matchmakingAgent.InvokeAsync(chatHistory))
                {
                    matchmakingResults.Add(response);
                }
                var matchmakingResponse = matchmakingResults.LastOrDefault()?.Content ?? "No matchmaking response";
                chatHistory.AddAssistantMessage(matchmakingResponse);
                steps.Add(new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = "Find alternatives based on inventory",
                    Result = matchmakingResponse,
                    Timestamp = DateTime.UtcNow
                });

                // Step 3: Location Agent (builds on previous findings)
                chatHistory.AddUserMessage("Based on the product information from inventory and matchmaking, where exactly can I find these products in the store?");
                var locationResults = new List<ChatMessageContent>();
                await foreach (var response in locationAgent.InvokeAsync(chatHistory))
                {
                    locationResults.Add(response);
                }
                var locationResponse = locationResults.LastOrDefault()?.Content ?? "No location response";
                chatHistory.AddAssistantMessage(locationResponse);
                steps.Add(new AgentStep
                {
                    Agent = "LocationAgent",
                    Action = "Locate products in store",
                    Result = locationResponse,
                    Timestamp = DateTime.UtcNow
                });

                // Step 4: Navigation Agent (builds on location findings)
                if (request.Location != null)
                {
                    chatHistory.AddUserMessage($"I'm currently at coordinates ({request.Location.Lat}, {request.Location.Lon}). Based on all the product locations identified, provide step-by-step navigation instructions.");
                    var navigationResults = new List<ChatMessageContent>();
                    await foreach (var response in navigationAgent.InvokeAsync(chatHistory))
                    {
                        navigationResults.Add(response);
                    }
                    var navigationResponse = navigationResults.LastOrDefault()?.Content ?? "No navigation response";
                    steps.Add(new AgentStep
                    {
                        Agent = "NavigationAgent",
                        Action = "Generate navigation instructions",
                        Result = navigationResponse,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Generate mock alternatives for UI compatibility
                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Sequential,
                    OrchestrationDescription = "Agents executed sequentially using Semantic Kernel ChatCompletionAgent, with each agent building upon the results of the previous agent's work.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sequential orchestration");
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

            _logger.LogInformation("Starting concurrent orchestration for query: {ProductQuery}", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();

                // Create specialized agents using ChatCompletionAgent
                var inventoryAgent = await GetInventoryAgentAsync();
                var matchmakingAgent = await GetMatchMakingAgentAsync();
                var locationAgent = await GetLocationAgentAsync();
                var navigationAgent = await GetNavigationAgentAsync();

                // Execute all agents concurrently with individual chat histories
                var concurrentTasks = new List<Task<AgentStep>>();

                // Each agent gets its own independent chat history
                concurrentTasks.Add(Task.Run(async () =>
                {
                    var chatHistory = new ChatHistory();
                    chatHistory.AddUserMessage($"Search inventory for: {request.ProductQuery}");
                    var results = new List<ChatMessageContent>();
                    await foreach (var response in inventoryAgent.InvokeAsync(chatHistory))
                    {
                        results.Add(response);
                    }
                    var responseContent = results.LastOrDefault()?.Content ?? "No inventory response";
                    return new AgentStep 
                    { 
                        Agent = "InventoryAgent", 
                        Action = $"Concurrent search for {request.ProductQuery}", 
                        Result = responseContent, 
                        Timestamp = DateTime.UtcNow 
                    };
                }));

                concurrentTasks.Add(Task.Run(async () =>
                {
                    var chatHistory = new ChatHistory();
                    chatHistory.AddUserMessage($"Find alternatives for: {request.ProductQuery}");
                    var results = new List<ChatMessageContent>();
                    await foreach (var response in matchmakingAgent.InvokeAsync(chatHistory))
                    {
                        results.Add(response);
                    }
                    var responseContent = results.LastOrDefault()?.Content ?? "No matchmaking response";
                    return new AgentStep 
                    { 
                        Agent = "MatchmakingAgent", 
                        Action = $"Concurrent alternatives for {request.ProductQuery}", 
                        Result = responseContent, 
                        Timestamp = DateTime.UtcNow 
                    };
                }));

                concurrentTasks.Add(Task.Run(async () =>
                {
                    var chatHistory = new ChatHistory();
                    chatHistory.AddUserMessage($"Locate in store: {request.ProductQuery}");
                    var results = new List<ChatMessageContent>();
                    await foreach (var response in locationAgent.InvokeAsync(chatHistory))
                    {
                        results.Add(response);
                    }
                    var responseContent = results.LastOrDefault()?.Content ?? "No location response";
                    return new AgentStep 
                    { 
                        Agent = "LocationAgent", 
                        Action = $"Concurrent location search for {request.ProductQuery}", 
                        Result = responseContent, 
                        Timestamp = DateTime.UtcNow 
                    };
                }));

                if (request.Location != null)
                {
                    concurrentTasks.Add(Task.Run(async () =>
                    {
                        var chatHistory = new ChatHistory();
                        chatHistory.AddUserMessage($"Provide navigation guidance for finding: {request.ProductQuery}");
                        var results = new List<ChatMessageContent>();
                        await foreach (var response in navigationAgent.InvokeAsync(chatHistory))
                        {
                            results.Add(response);
                        }
                        var responseContent = results.LastOrDefault()?.Content ?? "No navigation response";
                        return new AgentStep 
                        { 
                            Agent = "NavigationAgent", 
                            Action = $"Concurrent navigation for {request.ProductQuery}", 
                            Result = responseContent, 
                            Timestamp = DateTime.UtcNow 
                        };
                    }));
                }

                // Wait for all agents to complete concurrently
                var steps = await Task.WhenAll(concurrentTasks);

                // Generate mock alternatives for UI compatibility
                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Concurrent,
                    OrchestrationDescription = "All agents executed concurrently using Semantic Kernel ChatCompletionAgent, working independently in parallel without dependencies on each other's results.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in concurrent orchestration");
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

            _logger.LogInformation("Starting handoff orchestration for query: {ProductQuery}", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Create specialized agents using ChatCompletionAgent
                var routerAgent = new ChatCompletionAgent()
                {
                    Instructions = "You are a router agent. Analyze customer queries and decide which specialist should handle the request. Return only the agent name: InventoryAgent, MatchmakingAgent, LocationAgent, or NavigationAgent.",
                    Name = "RouterAgent",
                    Kernel = _kernel
                };

                var inventoryAgent = await GetInventoryAgentAsync();
                var matchmakingAgent = await GetMatchMakingAgentAsync();
                var locationAgent = await GetLocationAgentAsync();
                var navigationAgent = await GetNavigationAgentAsync();

                // Start with router to determine initial agent
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage($"Query to route: {request.ProductQuery}");
                
                var routerResults = new List<ChatMessageContent>();
                await foreach (var response in routerAgent.InvokeAsync(chatHistory))
                {
                    routerResults.Add(response);
                }
                var routerResponse = routerResults.LastOrDefault()?.Content ?? "InventoryAgent";
                
                steps.Add(new AgentStep 
                { 
                    Agent = "RouterAgent", 
                    Action = "Route initial query", 
                    Result = $"Routing to: {routerResponse}", 
                    Timestamp = DateTime.UtcNow 
                });

                // Initialize conversation history for handoff chain
                var handoffHistory = new ChatHistory();
                handoffHistory.AddUserMessage($"Customer query: {request.ProductQuery}");

                var currentAgent = routerResponse.Trim();
                var maxHandoffs = 4; // Prevent infinite loops
                var handoffCount = 0;

                while (handoffCount < maxHandoffs)
                {
                    Agent agent;
                    string actionDesc;

                    switch (currentAgent)
                    {
                        case "InventoryAgent":
                            agent = inventoryAgent;
                            actionDesc = "Handle inventory query";
                            break;
                        case "MatchmakingAgent":
                            agent = matchmakingAgent;
                            actionDesc = "Handle alternatives query";
                            break;
                        case "LocationAgent":
                            agent = locationAgent;
                            actionDesc = "Handle location query";
                            break;
                        case "NavigationAgent":
                            agent = navigationAgent;
                            actionDesc = "Handle navigation query";
                            break;
                        default:
                            agent = inventoryAgent;
                            actionDesc = "Handle default query";
                            currentAgent = "InventoryAgent";
                            break;
                    }

                    var results = new List<ChatMessageContent>();
                    await foreach (var responseItem in agent.InvokeAsync(handoffHistory))
                    {
                        results.Add(responseItem);
                    }
                    var response = results.LastOrDefault()?.Content ?? "No response";
                    handoffHistory.AddAssistantMessage(response);

                    steps.Add(new AgentStep 
                    { 
                        Agent = currentAgent, 
                        Action = actionDesc, 
                        Result = response, 
                        Timestamp = DateTime.UtcNow 
                    });

                    // Check for handoff instructions
                    if (response.Contains("HANDOFF_COMPLETE"))
                    {
                        break;
                    }
                    else if (response.Contains("HANDOFF_TO_LocationAgent"))
                    {
                        currentAgent = "LocationAgent";
                        handoffHistory.AddUserMessage("Please handle the location aspect of this query.");
                    }
                    else if (response.Contains("HANDOFF_TO_MatchmakingAgent"))
                    {
                        currentAgent = "MatchmakingAgent";
                        handoffHistory.AddUserMessage("Please handle finding alternatives for this query.");
                    }
                    else if (response.Contains("HANDOFF_TO_NavigationAgent"))
                    {
                        currentAgent = "NavigationAgent";
                        handoffHistory.AddUserMessage($"Please provide navigation instructions. Customer location: {request.Location?.Lat ?? 0}, {request.Location?.Lon ?? 0}");
                    }
                    else
                    {
                        // No handoff instruction found, complete the process
                        break;
                    }

                    handoffCount++;
                }

                // Generate mock alternatives for UI compatibility
                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Handoff,
                    OrchestrationDescription = "Dynamic handoff orchestration using Semantic Kernel ChatCompletionAgent, where agents pass control based on context and business logic rules.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handoff orchestration");
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

            _logger.LogInformation("Starting group chat orchestration for query: {ProductQuery}", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Create specialized agents using ChatCompletionAgent
                var inventoryAgent = await GetInventoryAgentAsync();
                var matchmakingAgent = await GetMatchMakingAgentAsync();
                var locationAgent = await GetLocationAgentAsync();
                var navigationAgent = await GetNavigationAgentAsync();

                // Create group chat with all agents
                var groupChat = new AgentGroupChat(inventoryAgent, matchmakingAgent, locationAgent, navigationAgent);

                // Add initial user message to start the group discussion
                groupChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, $"Hello team! I need help finding: {request.ProductQuery}. Let's work together to help this customer."));

                steps.Add(new AgentStep 
                { 
                    Agent = "GroupManager", 
                    Action = "Initialize group discussion", 
                    Result = $"Group chat started with 4 agents for query: {request.ProductQuery}", 
                    Timestamp = DateTime.UtcNow 
                });

                // Execute group chat for multiple rounds
                var maxTurns = 6; // Allow reasonable discussion
                var turnCount = 0;

                await foreach (var response in groupChat.InvokeAsync())
                {
                    if (turnCount >= maxTurns) break;

                    var agentName = response.AuthorName ?? "Unknown";
                    var content = response.Content ?? "No content";

                    steps.Add(new AgentStep 
                    { 
                        Agent = agentName, 
                        Action = $"Group discussion turn {turnCount + 1}", 
                        Result = content, 
                        Timestamp = DateTime.UtcNow 
                    });

                    turnCount++;
                }

                // Add a final summary step
                steps.Add(new AgentStep 
                { 
                    Agent = "GroupManager", 
                    Action = "Conclude group discussion", 
                    Result = "Group chat completed successfully with collaborative findings from all agents.", 
                    Timestamp = DateTime.UtcNow 
                });

                // Generate mock alternatives for UI compatibility
                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.GroupChat,
                    OrchestrationDescription = "Collaborative group chat orchestration using Semantic Kernel AgentGroupChat, where agents discuss and build consensus through multi-turn conversations.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in group chat orchestration");
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

            _logger.LogInformation("Starting Magentic orchestration for query: {ProductQuery}", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var steps = new List<AgentStep>();

                // Create coordinator agent (inspired by MagenticOne framework)
                var coordinatorAgent = new ChatCompletionAgent()
                {
                    Instructions = "You are the Magentic coordinator. Analyze queries, plan multi-agent workflows, assign tasks, and synthesize results. Provide orchestration decisions and final synthesis.",
                    Name = "MagenticCoordinator",
                    Kernel = _kernel
                };

                // Create specialized worker agents
                AzureAIAgent inventoryAgent = await GetInventoryAgentAsync();

                var matchmakingAgent = new ChatCompletionAgent()
                {
                    Instructions = "You are a product matchmaking specialist. Execute matching tasks assigned by the coordinator. Provide structured alternative recommendations.",
                    Name = "MatchmakingAgent", 
                    Kernel = _kernel
                };

                var locationAgent = new ChatCompletionAgent()
                {
                    Instructions = "You are a store location specialist. Execute location tasks assigned by the coordinator. Provide precise location data for synthesis.",
                    Name = "LocationAgent",
                    Kernel = _kernel
                };

                var navigationAgent = await GetNavigationAgentAsync();

                // Phase 1: Coordinator analyzes and plans
                var coordinatorHistory = new ChatHistory();
                coordinatorHistory.AddUserMessage($"Plan multi-agent workflow for customer query: {request.ProductQuery}. Consider what tasks each specialist should perform and in what order.");

                var planResults = new List<ChatMessageContent>();
                await foreach (var response in coordinatorAgent.InvokeAsync(coordinatorHistory))
                {
                    planResults.Add(response);
                }
                var planResponse = planResults.LastOrDefault()?.Content ?? "No plan response";
                coordinatorHistory.AddAssistantMessage(planResponse);

                steps.Add(new AgentStep 
                { 
                    Agent = "MagenticCoordinator", 
                    Action = "Analyze and plan workflow", 
                    Result = planResponse, 
                    Timestamp = DateTime.UtcNow 
                });

                // Phase 2: Execute coordinated tasks
                var sharedContext = new ChatHistory();
                sharedContext.AddUserMessage($"Customer query: {request.ProductQuery}");
                sharedContext.AddAssistantMessage($"Coordinator plan: {planResponse}");

                // Inventory task
                sharedContext.AddUserMessage("TASK for InventoryAgent: Search for the requested product and analyze availability.");
                var inventoryResults = new List<ChatMessageContent>();
                await foreach (var response in inventoryAgent.InvokeAsync(sharedContext))
                {
                    inventoryResults.Add(response);
                }
                var inventoryResponse = inventoryResults.LastOrDefault()?.Content ?? "No inventory response";
                sharedContext.AddAssistantMessage($"InventoryAgent result: {inventoryResponse}");

                steps.Add(new AgentStep 
                { 
                    Agent = "InventoryAgent", 
                    Action = "Execute coordinated inventory search", 
                    Result = inventoryResponse, 
                    Timestamp = DateTime.UtcNow 
                });

                // Matchmaking task
                sharedContext.AddUserMessage("TASK for MatchmakingAgent: Based on inventory findings, identify alternatives and similar products.");
                var matchmakingResults = new List<ChatMessageContent>();
                await foreach (var response in matchmakingAgent.InvokeAsync(sharedContext))
                {
                    matchmakingResults.Add(response);
                }
                var matchmakingResponse = matchmakingResults.LastOrDefault()?.Content ?? "No matchmaking response";
                sharedContext.AddAssistantMessage($"MatchmakingAgent result: {matchmakingResponse}");

                steps.Add(new AgentStep 
                { 
                    Agent = "MatchmakingAgent", 
                    Action = "Execute coordinated alternative search", 
                    Result = matchmakingResponse, 
                    Timestamp = DateTime.UtcNow 
                });

                // Location task
                sharedContext.AddUserMessage("TASK for LocationAgent: Based on all product findings, determine store locations and positioning.");
                var locationResults = new List<ChatMessageContent>();
                await foreach (var response in locationAgent.InvokeAsync(sharedContext))
                {
                    locationResults.Add(response);
                }
                var locationResponse = locationResults.LastOrDefault()?.Content ?? "No location response";
                sharedContext.AddAssistantMessage($"LocationAgent result: {locationResponse}");

                steps.Add(new AgentStep 
                { 
                    Agent = "LocationAgent", 
                    Action = "Execute coordinated location mapping", 
                    Result = locationResponse, 
                    Timestamp = DateTime.UtcNow 
                });

                // Navigation task (if applicable)
                if (request.Location != null)
                {
                    sharedContext.AddUserMessage($"TASK for NavigationAgent: Generate navigation from customer location ({request.Location.Lat}, {request.Location.Lon}) to product locations.");
                    var navigationResults = new List<ChatMessageContent>();
                    await foreach (var response in navigationAgent.InvokeAsync(sharedContext))
                    {
                        navigationResults.Add(response);
                    }
                    var navigationResponse = navigationResults.LastOrDefault()?.Content ?? "No navigation response";
                    sharedContext.AddAssistantMessage($"NavigationAgent result: {navigationResponse}");

                    steps.Add(new AgentStep 
                    { 
                        Agent = "NavigationAgent", 
                        Action = "Execute coordinated navigation planning", 
                        Result = navigationResponse, 
                        Timestamp = DateTime.UtcNow 
                    });
                }

                // Phase 3: Coordinator synthesizes results
                coordinatorHistory.AddUserMessage($"Synthesize results from all agents: Inventory: {inventoryResponse} | Alternatives: {matchmakingResponse} | Locations: {locationResponse} | Provide final comprehensive response.");
                var synthesisResults = new List<ChatMessageContent>();
                await foreach (var response in coordinatorAgent.InvokeAsync(coordinatorHistory))
                {
                    synthesisResults.Add(response);
                }
                var synthesisResponse = synthesisResults.LastOrDefault()?.Content ?? "No synthesis response";

                steps.Add(new AgentStep 
                { 
                    Agent = "MagenticCoordinator", 
                    Action = "Synthesize and coordinate final response", 
                    Result = synthesisResponse, 
                    Timestamp = DateTime.UtcNow 
                });

                // Generate mock alternatives for UI compatibility
                var alternatives = await GenerateProductAlternativesAsync(request.ProductQuery);

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestationType.Magentic,
                    OrchestrationDescription = "Magentic orchestration using Semantic Kernel ChatCompletionAgent, featuring coordinator-directed multi-agent collaboration with planning, execution, and synthesis phases.",
                    Steps = steps.ToArray(),
                    Alternatives = alternatives,
                    NavigationInstructions = request.Location != null ? await GenerateNavigationInstructionsAsync(request.Location, request.ProductQuery) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Magentic orchestration");
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
                _ => _sequentialOrchestration // Default fallback
            };
        }

        private async Task<AzureAIAgent> GetInventoryAgentAsync()
        {
            var inventoryAgentId = _configuration.GetConnectionString("inventoryagentid");
            var inventoryAgent = await _aIFoundryAgentProvider.CreateAzureAIAgentAsync(inventoryAgentId);
            return inventoryAgent;
        }

        private async Task<AzureAIAgent> GetNavigationAgentAsync()
        {
            var inventoryAgentId = _configuration.GetConnectionString("navigationagentid");
            var inventoryAgent = await _aIFoundryAgentProvider.CreateAzureAIAgentAsync(inventoryAgentId);
            return inventoryAgent;
        }

        private async Task<AzureAIAgent> GetMatchMakingAgentAsync()
        {
            var inventoryAgentId = _configuration.GetConnectionString("productmatchmakingagentid");
            var inventoryAgent = await _aIFoundryAgentProvider.CreateAzureAIAgentAsync(inventoryAgentId);
            return inventoryAgent;
        }

        private async Task<AzureAIAgent> GetLocationAgentAsync()
        {
            var inventoryAgentId = _configuration.GetConnectionString("locationserviceagentid");
            var inventoryAgent = await _aIFoundryAgentProvider.CreateAzureAIAgentAsync(inventoryAgentId);
            return inventoryAgent;
        }

        #region Legacy Methods (kept for compatibility)
        private async Task<AgentStep> RunInventoryAgentAsync(string productQuery)
        {
            try
            {
                var result = await _inventoryAgentService.SearchProductsAsync(productQuery);
                var names = result?.ProductsFound?.Select(p => p.Name) ?? Enumerable.Empty<string>();
                var desc = $"Found {result?.TotalCount ?? 0} products: {string.Join(", ", names)}";
                return new AgentStep { Agent = "InventoryAgent", Action = $"Search {productQuery}", Result = desc, Timestamp = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Inventory agent failed");
                return new AgentStep { Agent = "InventoryAgent", Action = $"Search {productQuery}", Result = "Fallback inventory result", Timestamp = DateTime.UtcNow };
            }
        }

        private async Task<AgentStep> RunMatchmakingAgentAsync(string productQuery, string userId)
        {
            try
            {
                var result = await _matchmakingAgentService.FindAlternativesAsync(productQuery, userId);
                var count = result?.Alternatives?.Length ?? 0;
                return new AgentStep { Agent = "MatchmakingAgent", Action = $"Find alternatives {productQuery}", Result = $"{count} alternatives found", Timestamp = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Matchmaking agent failed");
                return new AgentStep { Agent = "MatchmakingAgent", Action = $"Find alternatives {productQuery}", Result = "Fallback alternatives", Timestamp = DateTime.UtcNow };
            }
        }

        private async Task<AgentStep> RunLocationAgentAsync(string productQuery)
        {
            try
            {
                var result = await _locationAgentService.FindProductLocationAsync(productQuery);
                var loc = result?.StoreLocations?.FirstOrDefault();
                var desc = loc != null ? $"Located in {loc.Section} Aisle {loc.Aisle}" : "Location not found";
                return new AgentStep { Agent = "LocationAgent", Action = $"Locate {productQuery}", Result = desc, Timestamp = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Location agent failed");
                return new AgentStep { Agent = "LocationAgent", Action = $"Locate {productQuery}", Result = "Fallback location", Timestamp = DateTime.UtcNow };
            }
        }

        private async Task<AgentStep> RunNavigationAgentAsync(Location? location, string productQuery)
        {
            try
            {
                if (location == null) return new AgentStep { Agent = "NavigationAgent", Action = "Navigate", Result = "No start location", Timestamp = DateTime.UtcNow };
                var dest = new Location { Lat = 0, Lon = 0 };
                var nav = await _navigationAgentService.GenerateDirectionsAsync(location, dest);
                var steps = nav?.Steps?.Length ?? 0;
                return new AgentStep { Agent = "NavigationAgent", Action = "Navigate to product", Result = $"{steps} steps", Timestamp = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Navigation agent failed");
                return new AgentStep { Agent = "NavigationAgent", Action = "Navigate", Result = "Fallback navigation", Timestamp = DateTime.UtcNow };
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
        #endregion
    }
}
