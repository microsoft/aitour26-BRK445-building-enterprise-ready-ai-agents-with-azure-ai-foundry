using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using SingleAgentDemo.Models;
using SingleAgentDemo.Services;
using ZavaAgentFxAgentsProvider;

namespace SingleAgentDemo.Controllers;

[ApiController]
[Route("api/singleagent/agentfx")]
public class SingleAgentControllerAgentFx : ControllerBase
{
    private readonly ILogger<SingleAgentControllerAgentFx> _logger;
    private readonly AnalyzePhotoService _analyzePhotoService;
    private readonly CustomerInformationService _customerInformationService;
    private readonly ToolReasoningService _toolReasoningService;
    private readonly InventoryService _inventoryService;
    private readonly AgentFxAgentProvider _agentFxAgentProvider;
    private readonly IConfiguration _configuration;

    public SingleAgentControllerAgentFx(
        ILogger<SingleAgentControllerAgentFx> logger,        
        AnalyzePhotoService analyzePhotoService,
        CustomerInformationService customerInformationService,
        ToolReasoningService toolReasoningService,
        InventoryService inventoryService,
        AgentFxAgentProvider agentFxAgentProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _analyzePhotoService = analyzePhotoService;
        _customerInformationService = customerInformationService;
        _toolReasoningService = toolReasoningService;
        _inventoryService = inventoryService;
        _agentFxAgentProvider = agentFxAgentProvider;
        _configuration = configuration;

        // Set framework to AgentFx for all agent services
        _analyzePhotoService.SetFramework("agentfx");
        _customerInformationService.SetFramework("agentfx");
        _toolReasoningService.SetFramework("agentfx");
        _inventoryService.SetFramework("agentfx");
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<SharedEntities.SingleAgentAnalysisResponse>> AnalyzeAsync(
        [FromForm] IFormFile image,
        [FromForm] string prompt,
        [FromForm] string customerId)
    {
        try
        {
            _logger.LogInformation("Starting analysis workflow for customer {CustomerId} using Microsoft Agent Framework", customerId);

            // Implement sequential workflow pattern using Agent Framework for single agent analysis
            // This demonstrates a practical sequential workflow where each step builds on previous results
            
            // Workflow Step 1: Photo Analysis
            _logger.LogInformation("Agent Framework Workflow: Step 1 - Photo Analysis");
            var photoAnalysisStep = await ExecuteWorkflowStepAsync(
                "PhotoAnalyzer",
                "Analyze uploaded image to detect materials and project requirements",
                async () =>
                {
                    return await _analyzePhotoService.AnalyzePhotoAsync(image, prompt);
                });
            
            // Workflow Step 2: Customer Context Retrieval (concurrent with photo analysis possible, but sequential here)
            _logger.LogInformation("Agent Framework Workflow: Step 2 - Customer Information Retrieval");
            var customerInfoStep = await ExecuteWorkflowStepAsync(
                "CustomerInfoAgent",
                "Retrieve customer tools, skills, and project history",
                async () =>
                {
                    return await _customerInformationService.GetCustomerInformationAsync(customerId);
                });
            
            // Workflow Step 3: AI Reasoning (depends on steps 1 & 2)
            _logger.LogInformation("Agent Framework Workflow: Step 3 - AI-Powered Tool Reasoning");
            var reasoningStep = await ExecuteWorkflowStepAsync(
                "ReasoningAgent",
                "Apply AI reasoning to determine tool requirements based on analysis and customer context",
                async () =>
                {
                    return await GenerateToolReasoningWithAgentFxAsync(photoAnalysisStep, customerInfoStep, prompt);
                });
            
            // Workflow Step 4: Tool Matching (depends on all previous steps)
            _logger.LogInformation("Agent Framework Workflow: Step 4 - Tool Matching");
            var toolMatchStep = await ExecuteWorkflowStepAsync(
                "ToolMatchingAgent",
                "Match required tools against customer's existing tools",
                async () =>
                {
                    return await _customerInformationService.MatchToolsAsync(customerId, photoAnalysisStep.DetectedMaterials, prompt);
                });
            
            // Workflow Step 5: Inventory Enrichment (final step, depends on step 4)
            _logger.LogInformation("Agent Framework Workflow: Step 5 - Inventory Enrichment");
            var inventoryStep = await ExecuteWorkflowStepAsync(
                "InventoryAgent",
                "Enrich recommendations with real-time inventory data",
                async () =>
                {
                    return await _inventoryService.EnrichWithInventoryAsync(toolMatchStep.MissingTools);
                });

            // Workflow Complete: Synthesize results
            _logger.LogInformation("Agent Framework Workflow: Complete - Synthesizing results");
            var response = new SharedEntities.SingleAgentAnalysisResponse
            {
                Analysis = photoAnalysisStep.Description,
                ReusableTools = toolMatchStep.ReusableTools,
                RecommendedTools = inventoryStep.Select(t => new SharedEntities.ToolRecommendation
                {
                    Name = t.Name,
                    Sku = t.Sku,
                    IsAvailable = t.IsAvailable,
                    Price = t.Price,
                    Description = t.Description
                }).ToArray(),
                Reasoning = reasoningStep
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in analysis workflow for customer {CustomerId} using Microsoft Agent Framework", customerId);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Execute a workflow step with proper Agent Framework patterns
    /// Provides consistent logging, error handling, and step execution
    /// </summary>
    private async Task<T> ExecuteWorkflowStepAsync<T>(string agentName, string description, Func<Task<T>> action)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Executing workflow step: {AgentName} - {Description}", agentName, description);
        
        try
        {
            var result = await action();
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Completed workflow step: {AgentName} in {Duration}ms", agentName, duration.TotalMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed workflow step: {AgentName} - {Description}", agentName, description);
            throw;
        }
    }

    /// <summary>
    /// Generate tool reasoning using Agent Framework patterns
    /// Demonstrates request/response pattern and context passing between workflow steps
    /// </summary>
    private async Task<string> GenerateToolReasoningWithAgentFxAsync(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        try
        {
            _logger.LogInformation("Agent Framework: Generating tool reasoning with context from previous workflow steps");
            
            var reasoningRequest = new ReasoningRequest
            {
                PhotoAnalysis = photoAnalysis,
                Customer = customer,
                Prompt = prompt
            };

            // Use Agent Framework workflow pattern: request → agent processing → response
            try
            {
                var reasoning = await _toolReasoningService.GenerateReasoningAsync(reasoningRequest);
                
                // Enhance reasoning output with Agent Framework context
                var enhancedReasoning = $@"
=== Microsoft Agent Framework Analysis ===

Workflow Context:
- Sequential workflow pattern applied
- Each agent built upon previous agent outputs
- Context passed through 5 workflow steps

Project Analysis:
{reasoning}

Workflow Summary:
This recommendation was generated using a coordinated multi-step workflow where:
1. PhotoAnalyzer extracted visual requirements
2. CustomerInfoAgent retrieved your profile  
3. ReasoningAgent applied AI-powered analysis
4. ToolMatchingAgent compared against your tools
5. InventoryAgent enriched with real-time data

The Agent Framework ensures all agents work in harmony, with full context awareness at each step.";

                return enhancedReasoning;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call ToolReasoningService, using Agent Framework structured fallback");
                return GenerateStructuredFallbackReasoning(photoAnalysis, customer, prompt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI reasoning with Microsoft Agent Framework");
            return GenerateFallbackReasoning(photoAnalysis, customer, prompt);
        }
    }

    /// <summary>
    /// Generate structured fallback reasoning that demonstrates Agent Framework patterns
    /// </summary>
    private string GenerateStructuredFallbackReasoning(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        return $@"
=== Microsoft Agent Framework Analysis ===

Workflow Pattern: Sequential with Context Passing

Step 1 - Image Analysis Results:
- Project: {prompt}
- Visual Assessment: {photoAnalysis.Description}
- Detected Materials: {string.Join(", ", photoAnalysis.DetectedMaterials)}
- Complexity: {(photoAnalysis.DetectedMaterials.Length > 3 ? "High" : "Moderate")}

Step 2 - Customer Context:
- Existing Tools: {string.Join(", ", customer.OwnedTools)}
- Skill Level: {string.Join(", ", customer.Skills)}
- Experience: {(customer.OwnedTools.Length > 5 ? "Advanced" : "Intermediate")}

Step 3 - AI Reasoning:
Based on the coordinated workflow analysis, specific tools will be recommended to:
1. Fill gaps in your existing tool collection
2. Match the project complexity level
3. Align with your documented skill level  
4. Ensure safety and efficiency

Step 4 - Tool Matching:
The ToolMatchingAgent identifies which of your existing tools are applicable and which additional tools are needed.

Step 5 - Inventory Enrichment:
Real-time inventory data provides availability and pricing for recommended tools.

This Agent Framework workflow ensures comprehensive analysis by coordinating multiple specialized agents, each contributing their expertise to deliver personalized recommendations.";
    }

    private string GenerateFallbackReasoning(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        return $"Based on the task '{prompt}' and the detected materials ({string.Join(", ", photoAnalysis.DetectedMaterials)}), specific tools will be recommended to complement your existing tools: {string.Join(", ", customer.OwnedTools)}.";
    }
}
