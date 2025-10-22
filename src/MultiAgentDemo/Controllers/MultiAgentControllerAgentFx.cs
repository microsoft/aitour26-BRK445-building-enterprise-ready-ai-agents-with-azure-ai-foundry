using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.Identity.Client;
using MultiAgentDemo.Services;
using SharedEntities;
using System.Net.Http;
using System.Net.Http.Json;
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

            var alternatives = await GenerateProductAlternativesAsync(steps, request.ProductQuery);
            var navigationInstructions = await GenerateNavigationInstructionsAsync(steps, request.Location, request.ProductQuery);

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

        #region Location Members
        private async Task<NavigationInstructions> GenerateNavigationInstructionsAsync(List<AgentStep> steps, Location? location, string productQuery)
        {
            NavigationInstructions navigationInstructions = null;

            if (location == null)
            {
                location = new Location { Lat = 0, Lon = 0 };
            }

            try
            {
                // Analyze the steps to extract location information
                foreach (var step in steps)
                {
                    var stepContent = step.Result;
                    try
                    {
                        navigationInstructions = System.Text.Json.JsonSerializer.Deserialize<NavigationInstructions>(stepContent);
                        if (navigationInstructions != null)
                        {
                            _logger.LogInformation("Navigation instructions found in step: {StepContent}", stepContent);
                            return navigationInstructions;
                        }
                    }
                    catch
                    {
                        _logger.LogWarning("Failed to deserialize navigation instructions from step: {StepContent}", stepContent);
                    }                    
                }


                // return default nav instructions
                return CreateDefaultNavigationInstructions(location, productQuery);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GenerateNavigationInstructions failed, returning fallback");
                // Return fake valid information if there is a problem during execution
                return CreateDefaultNavigationInstructions(location, productQuery);
            }
        }

        private static NavigationInstructions CreateDefaultNavigationInstructions(Location location, string productQuery)
        {
            return new NavigationInstructions
            {
                Steps = new[]
                                {
                        new NavigationStep
                        {
                            Direction = "Head straight",
                            Description = $"Walk towards the main area where {productQuery} is located",
                            Landmark = new NavigationLandmark { Description = "Main entrance area" }
                        },
                        new NavigationStep
                        {
                            Direction = "Turn left",
                            Description = "Continue to the product section",
                            Landmark = new NavigationLandmark { Description = "Product display section" }
                        }
                    },
                StartLocation = $"Current Location ({location.Lat:F4}, {location.Lon:F4})",
                EstimatedTime = "3-5 minutes"
            };
        }

        #endregion

        #region Product Alternative Members
        private async Task<ProductAlternative[]> GenerateProductAlternativesAsync(List<AgentStep> steps, string productQuery)
        {
            try
            {
                // Analyze the steps and choose the specific products for the alternatives
                // Extract product information from the workflow steps
                string stepsSummary = SummarizeStepsForMatchmaking(steps);

                // Build a comprehensive prompt for the product matchmaking agent
                var prompt = $@"Based on the workflow analysis:
{stepsSummary}

For the product query: '{productQuery}'

Please provide product alternatives with the following information:
1. Product name
2. SKU
3. Price estimate
4. Stock availability
5. Store location (aisle and section)

Focus on providing practical alternatives that match the customer's needs.";

                var analyzeAlternatives = await _productMatchmakingAgent.RunAsync(prompt);

                // Parse the response to get the alternatives
                // For now, return structured alternatives based on the product query
                var baseProductName = ExtractProductNameFromQuery(productQuery);
                var baseSku = GenerateSkuFromProductName(baseProductName);

                return new[]
                {
                    new ProductAlternative
                    {
                        Name = $"Premium {baseProductName}",
                        Sku = $"PREM-{baseSku}",
                        Price = 129.99m,
                        InStock = true,
                        Location = "Aisle 5",
                        Aisle = 5,
                        Section = "A"
                    },
                    new ProductAlternative
                    {
                        Name = $"Standard {baseProductName}",
                        Sku = $"STD-{baseSku}",
                        Price = 79.99m,
                        InStock = true,
                        Location = "Aisle 7",
                        Aisle = 7,
                        Section = "B"
                    },
                    new ProductAlternative
                    {
                        Name = $"Budget {baseProductName}",
                        Sku = $"BDG-{baseSku}",
                        Price = 39.99m,
                        InStock = false,
                        Location = "Aisle 12",
                        Aisle = 12,
                        Section = "C"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GenerateProductAlternatives failed, returning fallback alternatives");
                // Return fake valid information if there is a problem during execution
                return new[]
                {
                    new ProductAlternative
                    {
                        Name = "Alternative Product A",
                        Sku = "ALT-001",
                        Price = 89.99m,
                        InStock = true,
                        Location = "Aisle 5",
                        Aisle = 5,
                        Section = "B"
                    },
                    new ProductAlternative
                    {
                        Name = "Alternative Product B",
                        Sku = "ALT-002",
                        Price = 49.99m,
                        InStock = true,
                        Location = "Aisle 8",
                        Aisle = 8,
                        Section = "C"
                    }
                };
            }
        }

        private string SummarizeStepsForMatchmaking(List<AgentStep> steps)
        {
            // Summarize relevant steps for product matchmaking
            if (steps == null || !steps.Any())
            {
                return "No workflow steps available";
            }

            var summary = new StringBuilder();
            foreach (var step in steps)
            {
                summary.AppendLine($"- {step.Agent}: {step.Result}");
            }

            return summary.ToString();
        }

        private string ExtractProductNameFromQuery(string productQuery)
        {
            // Simple extraction - in production, this would be more sophisticated
            // Remove common words and clean up the query
            var words = productQuery.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            var stopWords = new[] { "I", "can't", "find", "the", "a", "an", "in", "this", "store", "help", "product", "can", "you" };

            var productWords = words.Where(w => !stopWords.Contains(w, StringComparer.OrdinalIgnoreCase))
                                   .Take(3);

            return string.Join(" ", productWords);
        }

        private string GenerateSkuFromProductName(string productName)
        {
            // Generate a simple SKU from product name
            return productName.Replace(" ", "").ToUpper().Substring(0, Math.Min(8, productName.Replace(" ", "").Length));
        } 
        #endregion
    }
}