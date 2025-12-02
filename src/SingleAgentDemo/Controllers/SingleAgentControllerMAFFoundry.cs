using Microsoft.AspNetCore.Mvc;
using SingleAgentDemo.Models;
using SingleAgentDemo.Services;

namespace SingleAgentDemo.Controllers;

/// <summary>
/// Single Agent Controller using Microsoft Foundry Agents.
/// This controller provides a cloud-hosted agent experience using Azure AI Foundry.
/// </summary>
[ApiController]
[Route("api/singleagent/maffoundry")]
public class SingleAgentControllerMAFFoundry : ControllerBase
{
    private readonly ILogger<SingleAgentControllerMAFFoundry> _logger;
    private readonly AnalyzePhotoService _analyzePhotoService;
    private readonly CustomerInformationService _customerInformationService;
    private readonly ToolReasoningService _toolReasoningService;
    private readonly InventoryService _inventoryService;
    private readonly IConfiguration _configuration;

    public SingleAgentControllerMAFFoundry(
        ILogger<SingleAgentControllerMAFFoundry> logger,        
        AnalyzePhotoService analyzePhotoService,
        CustomerInformationService customerInformationService,
        ToolReasoningService toolReasoningService,
        InventoryService inventoryService,
        IConfiguration configuration)
    {
        _logger = logger;
        _analyzePhotoService = analyzePhotoService;
        _customerInformationService = customerInformationService;
        _toolReasoningService = toolReasoningService;
        _inventoryService = inventoryService;
        _configuration = configuration;

        // Set framework to MAF Foundry for all agent services
        _analyzePhotoService.SetFramework("maffoundry");
        _customerInformationService.SetFramework("maffoundry");
        _toolReasoningService.SetFramework("maffoundry");
        _inventoryService.SetFramework("maffoundry");
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<SharedEntities.SingleAgentAnalysisResponse>> AnalyzeAsync(
        [FromForm] IFormFile image,
        [FromForm] string prompt,
        [FromForm] string customerId)
    {
        try
        {
            _logger.LogInformation("Starting analysis workflow for customer {CustomerId} using Microsoft Foundry Agents", customerId);

            // Workflow Step 1: Photo Analysis
            _logger.LogInformation("Foundry Agents Workflow: Step 1 - Photo Analysis");
            var photoAnalysisStep = await ExecuteWorkflowStepAsync(
                "PhotoAnalyzer",
                "Analyze uploaded image to detect materials and project requirements",
                async () =>
                {
                    return await _analyzePhotoService.AnalyzePhotoAsync(image, prompt);
                });
            
            // Workflow Step 2: Customer Context Retrieval
            _logger.LogInformation("Foundry Agents Workflow: Step 2 - Customer Information Retrieval");
            var customerInfoStep = await ExecuteWorkflowStepAsync(
                "CustomerInfoAgent",
                "Retrieve customer tools, skills, and project history",
                async () =>
                {
                    return await _customerInformationService.GetCustomerInformationAsync(customerId);
                });
            
            // Workflow Step 3: AI Reasoning
            _logger.LogInformation("Foundry Agents Workflow: Step 3 - AI-Powered Tool Reasoning");
            var reasoningStep = await ExecuteWorkflowStepAsync(
                "ReasoningAgent",
                "Apply AI reasoning to determine tool requirements based on analysis and customer context",
                async () =>
                {
                    return await GenerateToolReasoningWithFoundryAgentsAsync(photoAnalysisStep, customerInfoStep, prompt);
                });
            
            // Workflow Step 4: Tool Matching
            _logger.LogInformation("Foundry Agents Workflow: Step 4 - Tool Matching");
            var toolMatchStep = await ExecuteWorkflowStepAsync(
                "ToolMatchingAgent",
                "Match required tools against customer's existing tools",
                async () =>
                {
                    return await _customerInformationService.MatchToolsAsync(customerId, photoAnalysisStep.DetectedMaterials, prompt);
                });
            
            // Workflow Step 5: Inventory Enrichment
            _logger.LogInformation("Foundry Agents Workflow: Step 5 - Inventory Enrichment");
            var inventoryStep = await ExecuteWorkflowStepAsync(
                "InventoryAgent",
                "Enrich recommendations with real-time inventory data",
                async () =>
                {
                    return await _inventoryService.EnrichWithInventoryAsync(toolMatchStep.MissingTools);
                });

            // Workflow Complete: Synthesize results
            _logger.LogInformation("Foundry Agents Workflow: Complete - Synthesizing results");
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
            _logger.LogError(ex, "Error in analysis workflow for customer {CustomerId} using Microsoft Foundry Agents", customerId);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

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

    private async Task<string> GenerateToolReasoningWithFoundryAgentsAsync(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        try
        {
            _logger.LogInformation("Foundry Agents: Generating tool reasoning with cloud-hosted agents");
            
            var reasoningRequest = new ReasoningRequest
            {
                PhotoAnalysis = photoAnalysis,
                Customer = customer,
                Prompt = prompt
            };

            try
            {
                var reasoning = await _toolReasoningService.GenerateReasoningAsync(reasoningRequest);
                
                var enhancedReasoning = $@"
=== Microsoft Foundry Agents Analysis ===

Cloud Infrastructure:
- Powered by Azure AI Foundry
- Agents managed via Azure.AI.Projects
- Scalable, enterprise-ready hosting

Project Analysis:
{reasoning}

Workflow Summary:
This recommendation was generated using Microsoft Foundry Agents:
1. PhotoAnalyzer Agent extracted visual requirements
2. CustomerInfoAgent retrieved your profile  
3. ReasoningAgent applied cloud-powered AI analysis
4. ToolMatchingAgent compared against your tools
5. InventoryAgent enriched with real-time data

Microsoft Foundry Agents provides enterprise-grade reliability with seamless Azure integration.";

                return enhancedReasoning;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call ToolReasoningService, using Foundry Agents fallback");
                return GenerateStructuredFallbackReasoning(photoAnalysis, customer, prompt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI reasoning with Microsoft Foundry Agents");
            return GenerateFallbackReasoning(photoAnalysis, customer, prompt);
        }
    }

    private string GenerateStructuredFallbackReasoning(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        return $@"
=== Microsoft Foundry Agents Analysis ===

Workflow Pattern: Cloud-Hosted Sequential Processing

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
Based on the cloud-hosted Foundry Agent analysis, specific tools will be recommended to:
1. Fill gaps in your existing tool collection
2. Match the project complexity level
3. Align with your documented skill level  
4. Ensure safety and efficiency

Step 4 - Tool Matching:
The ToolMatchingAgent identifies which of your existing tools are applicable and which additional tools are needed.

Step 5 - Inventory Enrichment:
Real-time inventory data provides availability and pricing for recommended tools.

This analysis is powered by Microsoft Foundry Agents, providing enterprise-grade cloud hosting and Azure integration.";
    }

    private string GenerateFallbackReasoning(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        return $"Based on the task '{prompt}' and the detected materials ({string.Join(", ", photoAnalysis.DetectedMaterials)}), specific tools will be recommended to complement your existing tools: {string.Join(", ", customer.OwnedTools)}.";
    }
}
