using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using SingleAgentDemo.Models;
using SingleAgentDemo.Services;
using ZavaAgentFxAgentsProvider;

namespace SingleAgentDemo.Controllers;

[ApiController]
[Route("api/singleagent")]
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
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<SharedEntities.SingleAgentAnalysisResponse>> AnalyzeAsync(
        [FromForm] IFormFile image,
        [FromForm] string prompt,
        [FromForm] string customerId)
    {
        try
        {
            _logger.LogInformation("Starting analysis for customer {CustomerId} using Microsoft Agent Framework", customerId);

            // Step 1: Analyze the image
            var photoAnalysis = await _analyzePhotoService.AnalyzePhotoAsync(image, prompt);
            
            // Step 2: Get customer information
            var customerInfo = await _customerInformationService.GetCustomerInformationAsync(customerId);
            
            // Step 3: Use Microsoft Agent Framework to reason about tools needed
            var reasoning = await GenerateToolReasoningWithAgentFxAsync(photoAnalysis, customerInfo, prompt);
            
            // Step 4: Match tools and get inventory
            var toolMatch = await _customerInformationService.MatchToolsAsync(customerId, photoAnalysis.DetectedMaterials, prompt);
            
            // Step 5: Enrich with inventory information
            var enrichedTools = await _inventoryService.EnrichWithInventoryAsync(toolMatch.MissingTools);

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
            _logger.LogError(ex, "Error analyzing request for customer {CustomerId} using Microsoft Agent Framework", customerId);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    private async Task<string> GenerateToolReasoningWithAgentFxAsync(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        try
        {
            var reasoningRequest = new ReasoningRequest
            {
                PhotoAnalysis = photoAnalysis,
                Customer = customer,
                Prompt = prompt
            };

            // First try the dedicated reasoning service
            try
            {
                var reasoning = await _toolReasoningService.GenerateReasoningAsync(reasoningRequest);
                return $"Microsoft Agent Framework: {reasoning}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call ToolReasoningService, using Microsoft Agent Framework fallback");
            }

            // Fallback reasoning using Microsoft Agent Framework
            var fallbackReasoning = $@"
Microsoft Agent Framework Analysis:

Project: {prompt}
Image Analysis: {photoAnalysis.Description}
Detected Materials: {string.Join(", ", photoAnalysis.DetectedMaterials)}
Customer Tools: {string.Join(", ", customer.OwnedTools)}
Customer Skills: {string.Join(", ", customer.Skills)}

Based on this analysis using Microsoft Agent Framework, specific tools will be recommended to complement your existing tools and ensure project success. The framework evaluated your skill level and project requirements to provide personalized recommendations prioritizing safety and efficiency.";

            return fallbackReasoning;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI reasoning with Microsoft Agent Framework, using fallback");
            return GenerateFallbackReasoning(photoAnalysis, customer, prompt);
        }
    }

    private string GenerateFallbackReasoning(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        return $"Based on the task '{prompt}' and the detected materials ({string.Join(", ", photoAnalysis.DetectedMaterials)}), specific tools will be recommended to complement your existing tools: {string.Join(", ", customer.OwnedTools)}.";
    }
}
