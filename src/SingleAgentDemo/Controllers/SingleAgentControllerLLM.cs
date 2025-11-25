using Microsoft.AspNetCore.Mvc;
using SingleAgentDemo.Models;
using SingleAgentDemo.Services;

namespace SingleAgentDemo.Controllers;

[ApiController]
[Route("api/singleagent/llm")]
public class SingleAgentControllerLLM : ControllerBase
{
    private readonly ILogger<SingleAgentControllerLLM> _logger;
    private readonly AnalyzePhotoService _analyzePhotoService;
    private readonly CustomerInformationService _customerInformationService;
    private readonly ToolReasoningService _toolReasoningService;
    private readonly InventoryService _inventoryService;

    public SingleAgentControllerLLM(
        ILogger<SingleAgentControllerLLM> logger,        
        AnalyzePhotoService analyzePhotoService,
        CustomerInformationService customerInformationService,
        ToolReasoningService toolReasoningService,
        InventoryService inventoryService)
    {
        _logger = logger;
        _analyzePhotoService = analyzePhotoService;
        _customerInformationService = customerInformationService;
        _toolReasoningService = toolReasoningService;
        _inventoryService = inventoryService;

        // Set framework to LLM for all agent services
        _analyzePhotoService.SetFramework("llm");
        _customerInformationService.SetFramework("llm");
        _toolReasoningService.SetFramework("llm");
        _inventoryService.SetFramework("llm");
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<SharedEntities.SingleAgentAnalysisResponse>> AnalyzeAsync(
        [FromForm] IFormFile image,
        [FromForm] string prompt,
        [FromForm] string customerId)
    {
        try
        {
            _logger.LogInformation("Starting analysis workflow for customer {CustomerId} using LLM Direct Call", customerId);

            // Workflow Step 1: Photo Analysis
            _logger.LogInformation("LLM Workflow: Step 1 - Photo Analysis");
            var photoAnalysis = await _analyzePhotoService.AnalyzePhotoAsync(image, prompt);
            
            // Workflow Step 2: Customer Context Retrieval
            _logger.LogInformation("LLM Workflow: Step 2 - Customer Information Retrieval");
            var customerInfo = await _customerInformationService.GetCustomerInformationAsync(customerId);
            
            // Workflow Step 3: AI Reasoning
            _logger.LogInformation("LLM Workflow: Step 3 - AI-Powered Tool Reasoning");
            var reasoningRequest = new ReasoningRequest
            {
                PhotoAnalysis = photoAnalysis,
                Customer = customerInfo,
                Prompt = prompt
            };
            var reasoning = await _toolReasoningService.GenerateReasoningAsync(reasoningRequest);
            
            // Workflow Step 4: Tool Matching
            _logger.LogInformation("LLM Workflow: Step 4 - Tool Matching");
            var toolMatch = await _customerInformationService.MatchToolsAsync(
                customerId, 
                photoAnalysis.DetectedMaterials, 
                prompt);
            
            // Workflow Step 5: Inventory Enrichment
            _logger.LogInformation("LLM Workflow: Step 5 - Inventory Enrichment");
            var enrichedTools = await _inventoryService.EnrichWithInventoryAsync(toolMatch.MissingTools);

            // Workflow Complete: Synthesize results
            _logger.LogInformation("LLM Workflow: Complete - Synthesizing results");
            var response = new SharedEntities.SingleAgentAnalysisResponse
            {
                Analysis = photoAnalysis.Description,
                ReusableTools = toolMatch.ReusableTools,
                RecommendedTools = enrichedTools.Select(t => new SharedEntities.ToolRecommendation
                {
                    Name = t.Name,
                    Sku = t.Sku,
                    IsAvailable = t.IsAvailable,
                    Price = t.Price,
                    Description = t.Description
                }).ToArray(),
                Reasoning = reasoning
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in analysis workflow for customer {CustomerId} using LLM", customerId);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
}
