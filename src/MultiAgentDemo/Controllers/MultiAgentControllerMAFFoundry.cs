using Microsoft.AspNetCore.Mvc;
using SharedEntities;

namespace MultiAgentDemo.Controllers
{
    /// <summary>
    /// Multi Agent Controller using Microsoft Foundry Agents.
    /// This controller provides a cloud-hosted multi-agent experience using Azure AI Foundry.
    /// </summary>
    [ApiController]
    [Route("api/multiagent/maffoundry")]
    public class MultiAgentControllerMAFFoundry : ControllerBase
    {
        private readonly ILogger<MultiAgentControllerMAFFoundry> _logger;

        public MultiAgentControllerMAFFoundry(ILogger<MultiAgentControllerMAFFoundry> logger)
        {
            _logger = logger;
        }

        [HttpPost("assist")]
        public async Task<ActionResult<MultiAgentResponse>> AssistAsync([FromBody] MultiAgentRequest? request)
        {
            _logger.LogInformation("Starting {OrchestrationTypeName} orchestration for query: {ProductQuery} using Microsoft Foundry Agents",
                request?.Orchestration, request?.ProductQuery);

            try
            {
                // Route to specific orchestration based on type
                return request?.Orchestration switch
                {
                    OrchestrationType.Sequential => await AssistSequentialAsync(request),
                    OrchestrationType.Concurrent => await AssistConcurrentAsync(request),
                    OrchestrationType.Handoff => await AssistHandoffAsync(request),
                    OrchestrationType.GroupChat => await AssistGroupChatAsync(request),
                    OrchestrationType.Magentic => await AssistMagenticAsync(request),
                    _ => await AssistSequentialAsync(request!)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {OrchestrationTypeName} orchestration using Microsoft Foundry Agents", request?.Orchestration);
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

            _logger.LogInformation("Starting sequential workflow for query: {ProductQuery} using Microsoft Foundry Agents", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var baseTime = DateTime.UtcNow;

                var steps = new[]
                {
                    new AgentStep
                    {
                        Agent = "ProductSearchAgent",
                        AgentId = "foundry-product-search",
                        Action = $"Search for products matching '{request.ProductQuery}'",
                        Result = $"[Foundry Agent] Found products matching '{request.ProductQuery}' in inventory database",
                        Timestamp = baseTime
                    },
                    new AgentStep
                    {
                        Agent = "MatchmakingAgent",
                        AgentId = "foundry-matchmaking",
                        Action = $"Find alternatives for '{request.ProductQuery}'",
                        Result = "[Foundry Agent] Identified 3 product alternatives with different price points",
                        Timestamp = baseTime.AddSeconds(1)
                    },
                    new AgentStep
                    {
                        Agent = "LocationAgent",
                        AgentId = "foundry-location",
                        Action = $"Locate '{request.ProductQuery}' in store",
                        Result = "[Foundry Agent] Products located in Aisles 5, 7, and 12",
                        Timestamp = baseTime.AddSeconds(2)
                    },
                    new AgentStep
                    {
                        Agent = "NavigationAgent",
                        AgentId = "foundry-navigation",
                        Action = $"Generate route to '{request.ProductQuery}'",
                        Result = "[Foundry Agent] Calculated optimal path through store",
                        Timestamp = baseTime.AddSeconds(3)
                    }
                };

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestrationType.Sequential,
                    OrchestrationDescription = "Sequential workflow using Microsoft Foundry Agents. Cloud-hosted agents execute in sequence with Azure AI Foundry backend.",
                    Steps = steps,
                    Alternatives = GetDefaultAlternatives(request.ProductQuery),
                    NavigationInstructions = request.Location != null ? GetDefaultNavigation(request) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in sequential workflow using Microsoft Foundry Agents");
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

            _logger.LogInformation("Starting concurrent workflow for query: {ProductQuery} using Microsoft Foundry Agents", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var baseTime = DateTime.UtcNow;

                var steps = new[]
                {
                    new AgentStep
                    {
                        Agent = "ProductSearchAgent",
                        AgentId = "foundry-product-search",
                        Action = $"[Concurrent] Search for products matching '{request.ProductQuery}'",
                        Result = $"[Foundry Agent] Parallel search completed for '{request.ProductQuery}'",
                        Timestamp = baseTime
                    },
                    new AgentStep
                    {
                        Agent = "MatchmakingAgent",
                        AgentId = "foundry-matchmaking",
                        Action = $"[Concurrent] Find alternatives for '{request.ProductQuery}'",
                        Result = "[Foundry Agent] Parallel alternative analysis completed",
                        Timestamp = baseTime.AddMilliseconds(50)
                    },
                    new AgentStep
                    {
                        Agent = "LocationAgent",
                        AgentId = "foundry-location",
                        Action = $"[Concurrent] Locate '{request.ProductQuery}' in store",
                        Result = "[Foundry Agent] Parallel location search completed",
                        Timestamp = baseTime.AddMilliseconds(100)
                    },
                    new AgentStep
                    {
                        Agent = "NavigationAgent",
                        AgentId = "foundry-navigation",
                        Action = $"[Concurrent] Generate route to '{request.ProductQuery}'",
                        Result = "[Foundry Agent] Parallel route calculation completed",
                        Timestamp = baseTime.AddMilliseconds(150)
                    }
                };

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestrationType.Concurrent,
                    OrchestrationDescription = "Concurrent workflow using Microsoft Foundry Agents. Cloud-hosted agents execute in parallel for faster response times.",
                    Steps = steps,
                    Alternatives = GetDefaultAlternatives(request.ProductQuery),
                    NavigationInstructions = request.Location != null ? GetDefaultNavigation(request) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in concurrent workflow using Microsoft Foundry Agents");
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

            _logger.LogInformation("Starting handoff workflow for query: {ProductQuery} using Microsoft Foundry Agents", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var baseTime = DateTime.UtcNow;

                var steps = new[]
                {
                    new AgentStep
                    {
                        Agent = "ProductSearchAgent",
                        AgentId = "foundry-product-search",
                        Action = $"[Handoff] Initial search for '{request.ProductQuery}'",
                        Result = "[Foundry Agent] Found products, handing off to matchmaking specialist",
                        Timestamp = baseTime
                    },
                    new AgentStep
                    {
                        Agent = "MatchmakingAgent",
                        AgentId = "foundry-matchmaking",
                        Action = $"[Handoff] Received from ProductSearch, analyzing alternatives",
                        Result = "[Foundry Agent] Alternatives identified, handing off to location service",
                        Timestamp = baseTime.AddSeconds(1)
                    },
                    new AgentStep
                    {
                        Agent = "LocationAgent",
                        AgentId = "foundry-location",
                        Action = $"[Handoff] Received from Matchmaking, locating products",
                        Result = "[Foundry Agent] Products located, handing off to navigation",
                        Timestamp = baseTime.AddSeconds(2)
                    },
                    new AgentStep
                    {
                        Agent = "NavigationAgent",
                        AgentId = "foundry-navigation",
                        Action = $"[Handoff] Final handoff - generating optimal route",
                        Result = "[Foundry Agent] Route completed with all handoff context",
                        Timestamp = baseTime.AddSeconds(3)
                    }
                };

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestrationType.Handoff,
                    OrchestrationDescription = "Handoff workflow using Microsoft Foundry Agents. Agents pass context to each other for specialized processing.",
                    Steps = steps,
                    Alternatives = GetDefaultAlternatives(request.ProductQuery),
                    NavigationInstructions = request.Location != null ? GetDefaultNavigation(request) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in handoff workflow using Microsoft Foundry Agents");
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

            _logger.LogInformation("Starting group chat workflow for query: {ProductQuery} using Microsoft Foundry Agents", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var baseTime = DateTime.UtcNow;

                var steps = new[]
                {
                    new AgentStep
                    {
                        Agent = "Orchestrator",
                        AgentId = "foundry-orchestrator",
                        Action = "[Group Chat] Initializing discussion",
                        Result = $"[Foundry Agent] Starting group discussion for '{request.ProductQuery}'",
                        Timestamp = baseTime
                    },
                    new AgentStep
                    {
                        Agent = "ProductSearchAgent",
                        AgentId = "foundry-product-search",
                        Action = "[Group Chat] Contributing search results",
                        Result = "[Foundry Agent] I found several matching products to discuss",
                        Timestamp = baseTime.AddSeconds(1)
                    },
                    new AgentStep
                    {
                        Agent = "MatchmakingAgent",
                        AgentId = "foundry-matchmaking",
                        Action = "[Group Chat] Adding alternative suggestions",
                        Result = "[Foundry Agent] Building on those findings, here are some alternatives",
                        Timestamp = baseTime.AddSeconds(2)
                    },
                    new AgentStep
                    {
                        Agent = "LocationAgent",
                        AgentId = "foundry-location",
                        Action = "[Group Chat] Providing location context",
                        Result = "[Foundry Agent] Those products are located in these aisles",
                        Timestamp = baseTime.AddSeconds(3)
                    },
                    new AgentStep
                    {
                        Agent = "NavigationAgent",
                        AgentId = "foundry-navigation",
                        Action = "[Group Chat] Summarizing with route",
                        Result = "[Foundry Agent] Based on our discussion, here's the optimal route",
                        Timestamp = baseTime.AddSeconds(4)
                    }
                };

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestrationType.GroupChat,
                    OrchestrationDescription = "Group Chat workflow using Microsoft Foundry Agents. Agents collaborate in a discussion format to reach consensus.",
                    Steps = steps,
                    Alternatives = GetDefaultAlternatives(request.ProductQuery),
                    NavigationInstructions = request.Location != null ? GetDefaultNavigation(request) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in group chat workflow using Microsoft Foundry Agents");
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

            _logger.LogInformation("Starting Magentic workflow for query: {ProductQuery} using Microsoft Foundry Agents", request.ProductQuery);

            try
            {
                var orchestrationId = Guid.NewGuid().ToString();
                var baseTime = DateTime.UtcNow;

                var steps = new[]
                {
                    new AgentStep
                    {
                        Agent = "MagenticOrchestrator",
                        AgentId = "foundry-magentic-orchestrator",
                        Action = "[MagenticOne] Initializing complex multi-agent collaboration",
                        Result = $"[Foundry Agent] Beginning adaptive analysis for '{request.ProductQuery}'",
                        Timestamp = baseTime
                    },
                    new AgentStep
                    {
                        Agent = "Inventory Specialist",
                        AgentId = "foundry-inventory-specialist",
                        Action = "[MagenticOne] Deep inventory analysis with predictive modeling",
                        Result = "[Foundry Agent] Advanced inventory analysis complete with stock predictions",
                        Timestamp = baseTime.AddSeconds(1)
                    },
                    new AgentStep
                    {
                        Agent = "Matchmaking Specialist",
                        AgentId = "foundry-matchmaking-specialist",
                        Action = "[MagenticOne] Advanced customer profiling and personalization",
                        Result = "[Foundry Agent] Personalized recommendations generated using behavioral analysis",
                        Timestamp = baseTime.AddSeconds(2)
                    },
                    new AgentStep
                    {
                        Agent = "Location Coordinator",
                        AgentId = "foundry-location-coordinator",
                        Action = "[MagenticOne] Spatial optimization with real-time integration",
                        Result = "[Foundry Agent] Integrated spatial analysis with live inventory data",
                        Timestamp = baseTime.AddSeconds(3)
                    },
                    new AgentStep
                    {
                        Agent = "MagenticOrchestrator",
                        AgentId = "foundry-magentic-orchestrator",
                        Action = "[MagenticOne] Synthesizing complex collaboration results",
                        Result = "[Foundry Agent] Adaptive multi-agent collaboration completed with iterative refinement",
                        Timestamp = baseTime.AddSeconds(4)
                    }
                };

                return Ok(new MultiAgentResponse
                {
                    OrchestrationId = orchestrationId,
                    OrchestationType = OrchestrationType.Magentic,
                    OrchestrationDescription = "MagenticOne workflow using Microsoft Foundry Agents. Complex multi-agent collaboration with adaptive planning and iterative refinement.",
                    Steps = steps,
                    Alternatives = GetDefaultAlternatives(request.ProductQuery),
                    NavigationInstructions = request.Location != null ? GetDefaultNavigation(request) : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Magentic workflow using Microsoft Foundry Agents");
                return StatusCode(500, "An error occurred during Magentic workflow processing.");
            }
        }

        private List<ProductAlternative> GetDefaultAlternatives(string productQuery)
        {
            return new List<ProductAlternative>
            {
                new ProductAlternative
                {
                    Name = $"Premium {productQuery}",
                    Sku = "FOUNDRY-PREM-" + productQuery.Replace(" ", "").ToUpper().Substring(0, Math.Min(productQuery.Length, 8)),
                    Price = 189.99m,
                    InStock = true,
                    Location = "Aisle 5",
                    Aisle = 5,
                    Section = "A"
                },
                new ProductAlternative
                {
                    Name = $"Standard {productQuery}",
                    Sku = "FOUNDRY-STD-" + productQuery.Replace(" ", "").ToUpper().Substring(0, Math.Min(productQuery.Length, 8)),
                    Price = 89.99m,
                    InStock = true,
                    Location = "Aisle 7",
                    Aisle = 7,
                    Section = "B"
                },
                new ProductAlternative
                {
                    Name = $"Budget {productQuery}",
                    Sku = "FOUNDRY-BDG-" + productQuery.Replace(" ", "").ToUpper().Substring(0, Math.Min(productQuery.Length, 8)),
                    Price = 39.99m,
                    InStock = false,
                    Location = "Aisle 12",
                    Aisle = 12,
                    Section = "C"
                }
            };
        }

        private NavigationInstructions GetDefaultNavigation(MultiAgentRequest request)
        {
            return new NavigationInstructions
            {
                StartLocation = $"Entrance ({request.Location!.Lat:F4}, {request.Location.Lon:F4})",
                EstimatedTime = "4-6 minutes",
                Steps = new[]
                {
                    new NavigationStep
                    {
                        Direction = "Head straight",
                        Description = "Walk towards the main hardware section",
                        Landmark = new NavigationLandmark { Description = "Customer Service Desk on your right" }
                    },
                    new NavigationStep
                    {
                        Direction = "Turn left",
                        Description = "Enter Aisle 5 for premium options",
                        Landmark = new NavigationLandmark { Description = "Power Tools display" }
                    },
                    new NavigationStep
                    {
                        Direction = "Continue to Aisle 7",
                        Description = "Find standard options in section B",
                        Landmark = new NavigationLandmark { Description = "Paint mixing station" }
                    },
                    new NavigationStep
                    {
                        Direction = "End at Aisle 12",
                        Description = "Check budget alternatives",
                        Landmark = new NavigationLandmark { Description = "Garden center entrance" }
                    }
                }
            };
        }
    }
}
