using SharedEntities;
using System.Text.Json;

namespace Store.Services;

public class MultiAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MultiAgentService> _logger;
    private readonly AgentFrameworkService _frameworkService;

    public MultiAgentService(HttpClient httpClient, ILogger<MultiAgentService> logger, AgentFrameworkService frameworkService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _frameworkService = frameworkService;
    }

    public async Task<MultiAgentResponse?> AssistAsync(MultiAgentRequest request)
    {
        try
        {
            var framework = await _frameworkService.GetSelectedFrameworkAsync();
            
            _logger.LogInformation("Calling multi-agent service for user {UserId} with query {ProductQuery} using {OrchestationType} orchestration and {Framework} framework",
                request.UserId, request.ProductQuery, request.Orchestration, framework);

            // Route to specific orchestration endpoint if specified, otherwise use default
            var endpoint = GetOrchestrationEndpoint(request.Orchestration, framework);
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Multi-agent service response - Status: {StatusCode}, Content: {Content}",
                response.StatusCode, responseText);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<MultiAgentResponse>(responseText, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return result;
            }
            else
            {
                _logger.LogWarning("Multi-agent service returned non-success status: {StatusCode}", response.StatusCode);
                return CreateFallbackResponse(request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling multi-agent service for user {UserId}", request.UserId);
            return CreateFallbackResponse(request);
        }
    }

    private string GetOrchestrationEndpoint(OrchestrationType orchestrationType, string framework)
    {
        var frameworkPath = framework switch
        {
            "maf" => "maf",
            _ => "llm"
        };
        var baseRoute = $"/api/multiagent/{frameworkPath}";
        
        return orchestrationType switch
        {
            OrchestrationType.Default => $"{baseRoute}/assist",
            OrchestrationType.Sequential => $"{baseRoute}/assist/sequential",
            OrchestrationType.Concurrent => $"{baseRoute}/assist/concurrent",
            OrchestrationType.Handoff => $"{baseRoute}/assist/handoff",
            OrchestrationType.GroupChat => $"{baseRoute}/assist/groupchat",
            OrchestrationType.Magentic => $"{baseRoute}/assist/magentic",
            _ => $"{baseRoute}/assist" // Default endpoint
        };
    }

    private MultiAgentResponse CreateFallbackResponse(MultiAgentRequest request)
    {
        var orchestrationId = Guid.NewGuid().ToString("N")[..8];
        var baseTime = DateTime.UtcNow;
                
        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = request.Orchestration,
            OrchestrationDescription = GetFallbackOrchestrationDescription(request.Orchestration),
            Steps = GetFallbackSteps(request, baseTime),
            Alternatives = GetFallbackAlternatives(request.ProductQuery),
            NavigationInstructions = request.Location != null ? CreateFallbackNavigation(request) : null
        };
    }

    private string GetFallbackOrchestrationDescription(OrchestrationType orchestrationType)
    {
        return orchestrationType switch
        {
            OrchestrationType.Sequential => "Fallback sequential processing with step-by-step agent execution",
            OrchestrationType.Concurrent => "Fallback concurrent processing with parallel agent execution",
            OrchestrationType.Handoff => "Fallback handoff processing with dynamic agent routing",
            OrchestrationType.GroupChat => "Fallback group chat processing with collaborative agent discussion",
            OrchestrationType.Magentic => "Fallback MagenticOne processing with complex multi-agent collaboration",
            _ => "Fallback orchestration processing"
        };
    }

    private AgentStep[] GetFallbackSteps(MultiAgentRequest request, DateTime baseTime)
    {
        return request.Orchestration switch
        {
            OrchestrationType.Concurrent => new[]
            {
                new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = $"Concurrent search inventory for '{request.ProductQuery}'",
                    Result = $"Concurrent analysis: Found 5 products matching '{request.ProductQuery}' across multiple sections",
                    Timestamp = baseTime
                },
                new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = $"Concurrent find alternatives for '{request.ProductQuery}'",
                    Result = "Parallel processing: Identified 3 product alternatives with independent analysis",
                    Timestamp = baseTime.AddMilliseconds(50)
                },
                new AgentStep
                {
                    Agent = "LocationAgent",
                    Action = $"Concurrent locate '{request.ProductQuery}' in store",
                    Result = "Simultaneous location search: Products located in Aisles 5, 7, and 12",
                    Timestamp = baseTime.AddMilliseconds(100)
                },
                new AgentStep
                {
                    Agent = "NavigationAgent",
                    Action = $"Concurrent route generation for '{request.ProductQuery}'",
                    Result = "Parallel route calculation: Generated optimal paths for all product locations",
                    Timestamp = baseTime.AddMilliseconds(150)
                }
            },
            OrchestrationType.Handoff => new[]
            {
                new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = $"Initial handoff search for '{request.ProductQuery}'",
                    Result = $"Handoff: Found limited stock, escalating to alternatives specialist",
                    Timestamp = baseTime
                },
                new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = $"Handoff alternatives analysis for '{request.ProductQuery}'",
                    Result = "Taking handoff: Identified better alternatives, routing to location specialist",
                    Timestamp = baseTime.AddSeconds(1)
                },
                new AgentStep
                {
                    Agent = "LocationAgent",
                    Action = $"Expert handoff location for '{request.ProductQuery}'",
                    Result = "Final handoff: Located all alternatives in optimal sections",
                    Timestamp = baseTime.AddSeconds(2)
                }
            },
            OrchestrationType.GroupChat => new[]
            {
                new AgentStep
                {
                    Agent = "Group Manager",
                    Action = "Initiate group discussion",
                    Result = $"Welcome to group collaboration for '{request.ProductQuery}'. Let's work together!",
                    Timestamp = baseTime
                },
                new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = "Group discussion contribution",
                    Result = $"Group input: I found several options for '{request.ProductQuery}'. What do others think?",
                    Timestamp = baseTime.AddSeconds(1)
                },
                new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = "Group consensus building",
                    Result = "Building on Inventory's findings: I can add personalized alternatives to the discussion",
                    Timestamp = baseTime.AddSeconds(2)
                },
                new AgentStep
                {
                    Agent = "LocationAgent",
                    Action = "Group collaboration",
                    Result = "Great teamwork! I can provide precise locations for all the options we've discussed",
                    Timestamp = baseTime.AddSeconds(3)
                },
                new AgentStep
                {
                    Agent = "Group Manager",
                    Action = "Conclude group discussion",
                    Result = "Excellent collaboration! We've reached consensus on the best customer solution",
                    Timestamp = baseTime.AddSeconds(4)
                }
            },
            OrchestrationType.Magentic => new[]
            {
                new AgentStep
                {
                    Agent = "Orchestrator",
                    Action = "Initialize MagenticOne collaboration",
                    Result = $"Beginning complex multi-agent analysis for '{request.ProductQuery}' with adaptive planning",
                    Timestamp = baseTime
                },
                new AgentStep
                {
                    Agent = "Inventory Specialist",
                    Action = "Deep inventory analysis",
                    Result = $"MagenticOne specialist: Advanced analysis of '{request.ProductQuery}' with predictive modeling",
                    Timestamp = baseTime.AddSeconds(1)
                },
                new AgentStep
                {
                    Agent = "Matchmaking Specialist",
                    Action = "Advanced customer profiling",
                    Result = "MagenticOne personalization: Complex behavioral analysis for optimal recommendations",
                    Timestamp = baseTime.AddSeconds(2)
                },
                new AgentStep
                {
                    Agent = "Location Coordinator",
                    Action = "Spatial optimization",
                    Result = "MagenticOne coordination: Integrated spatial analysis with real-time inventory",
                    Timestamp = baseTime.AddSeconds(3)
                },
                new AgentStep
                {
                    Agent = "Orchestrator",
                    Action = "Synthesize complex collaboration",
                    Result = "MagenticOne synthesis: Completed adaptive multi-agent collaboration with iterative refinement",
                    Timestamp = baseTime.AddSeconds(4)
                }
            },
            _ => new[] // Sequential fallback
            {
                new AgentStep
                {
                    Agent = "InventoryAgent",
                    Action = $"Search inventory for '{request.ProductQuery}'",
                    Result = $"Found 5 products matching '{request.ProductQuery}' across hardware and tools sections",
                    Timestamp = baseTime
                },
                new AgentStep
                {
                    Agent = "MatchmakingAgent",
                    Action = $"Find alternatives for '{request.ProductQuery}'",
                    Result = "Identified 3 product alternatives: Premium, Standard, and Budget options with different feature sets",
                    Timestamp = baseTime.AddSeconds(1)
                },
                new AgentStep
                {
                    Agent = "LocationAgent",
                    Action = $"Locate '{request.ProductQuery}' in store",
                    Result = "Products located in Aisles 5, 7, and 12 with current stock levels verified",
                    Timestamp = baseTime.AddSeconds(2)
                },
                new AgentStep
                {
                    Agent = "NavigationAgent",
                    Action = $"Generate route to '{request.ProductQuery}'",
                    Result = "Calculated optimal path through store to visit all product locations efficiently",
                    Timestamp = baseTime.AddSeconds(3)
                }
            }
        };
    }

    private List<ProductAlternative> GetFallbackAlternatives(string productQuery)
    {
        return
        [
            new ProductAlternative
            {
                Name = $"Premium {productQuery}",
                Sku = "PREM-" + productQuery.Replace(" ", "").ToUpper(),
                Price = 189.99m,
                InStock = true,
                Location = "Aisle 5",
                Aisle = 5,
                Section = "A"
            },
            new ProductAlternative
            {
                Name = $"Standard {productQuery}",
                Sku = "STD-" + productQuery.Replace(" ", "").ToUpper(),
                Price = 89.99m,
                InStock = true,
                Location = "Aisle 7",
                Aisle = 7,
                Section = "B"
            },
            new ProductAlternative
            {
                Name = $"Budget {productQuery}",
                Sku = "BDG-" + productQuery.Replace(" ", "").ToUpper(),
                Price = 39.99m,
                InStock = false,
                Location = "Aisle 12",
                Aisle = 12,
                Section = "C"
            }
        ];
    }

    private NavigationInstructions CreateFallbackNavigation(MultiAgentRequest request)
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
                    Description = "Check budget alternatives (may be out of stock)",
                    Landmark = new NavigationLandmark { Description = "Garden center entrance" }
                }
            }
        };
    }
}