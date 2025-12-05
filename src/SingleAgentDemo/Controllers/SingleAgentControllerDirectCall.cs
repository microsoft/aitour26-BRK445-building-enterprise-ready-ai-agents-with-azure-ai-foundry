using Microsoft.AspNetCore.Mvc;
using SharedEntities;
using SingleAgentDemo.Services;

namespace SingleAgentDemo.Controllers;

/// <summary>
/// Controller for single agent analysis using direct HTTP calls to business services.
/// This mode bypasses AI orchestration and calls the underlying HTTP services directly.
/// </summary>
[ApiController]
[Route("api/singleagent/directcall")]
public class SingleAgentControllerDirectCall : ControllerBase
{
    private readonly ILogger<SingleAgentControllerDirectCall> _logger;
    private readonly AnalyzePhotoService _analyzePhotoService;
    private readonly CustomerInformationService _customerInformationService;
    private readonly ToolReasoningService _toolReasoningService;
    private readonly InventoryService _inventoryService;

    public SingleAgentControllerDirectCall(
        ILogger<SingleAgentControllerDirectCall> logger,
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

        // Set framework to directcall for all agent services
        _analyzePhotoService.SetFramework("directcall");
        _customerInformationService.SetFramework("directcall");
        _toolReasoningService.SetFramework("directcall");
        _inventoryService.SetFramework("directcall");
    }

    /// <summary>
    /// Analyze an image using direct HTTP calls to business services.
    /// </summary>
    [HttpPost("analyze")]
    public async Task<ActionResult<SingleAgentAnalysisResponse>> AnalyzeAsync(
        [FromForm] IFormFile image,
        [FromForm] string prompt,
        [FromForm] string customerId)
    {
        try
        {
            _logger.LogInformation("Starting analysis workflow for customer {CustomerId} using Direct HTTP Calls", customerId);

            // Workflow Step 1: Photo Analysis via HTTP service
            _logger.LogInformation("DirectCall Workflow: Step 1 - Photo Analysis");
            var photoAnalysis = await _analyzePhotoService.AnalyzePhotoAsync(image, prompt);
            
            // Workflow Step 2: Customer Context Retrieval via HTTP service
            _logger.LogInformation("DirectCall Workflow: Step 2 - Customer Information Retrieval");
            var customerInfo = await _customerInformationService.GetCustomerInformationAsync(customerId);
            
            // Workflow Step 3: Tool Reasoning via HTTP service
            _logger.LogInformation("DirectCall Workflow: Step 3 - Tool Reasoning");
            var reasoningRequest = new ReasoningRequest
            {
                PhotoAnalysis = photoAnalysis,
                Customer = customerInfo,
                Prompt = prompt
            };
            var reasoning = await _toolReasoningService.GenerateReasoningAsync(reasoningRequest);
            
            // Workflow Step 4: Tool Matching
            _logger.LogInformation("DirectCall Workflow: Step 4 - Tool Matching");
            var toolMatch = await _customerInformationService.MatchToolsAsync(
                customerId, 
                photoAnalysis.DetectedMaterials, 
                prompt);
            
            // Workflow Step 5: Inventory Enrichment via HTTP service
            _logger.LogInformation("DirectCall Workflow: Step 5 - Inventory Enrichment");
            var enrichedTools = await _inventoryService.EnrichWithInventoryAsync(toolMatch.MissingTools);

            // Workflow Complete: Synthesize results
            _logger.LogInformation("DirectCall Workflow: Complete - Synthesizing results");
            var response = new SingleAgentAnalysisResponse
            {
                Analysis = photoAnalysis.Description,
                ReusableTools = toolMatch.ReusableTools,
                RecommendedTools = enrichedTools.Select(t => new ToolRecommendation
                {
                    Name = t.Name,
                    Sku = t.Sku,
                    IsAvailable = t.IsAvailable,
                    Price = t.Price,
                    Description = t.Description
                }).ToArray(),
                Reasoning = $"[Direct HTTP Call Mode]\n{reasoning}"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in analysis workflow for customer {CustomerId} using DirectCall", customerId);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }
}
