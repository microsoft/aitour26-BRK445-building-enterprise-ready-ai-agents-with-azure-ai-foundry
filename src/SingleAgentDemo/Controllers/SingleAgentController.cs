using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using SingleAgentDemo.Models;
using SingleAgentDemo.Services;
using ZavaSemanticKernelProvider;
namespace SingleAgentDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SingleAgentController : ControllerBase
{
    private readonly ILogger<SingleAgentController> _logger;
    private readonly Kernel _kernel;
    private readonly AnalyzePhotoService _analyzePhotoService;
    private readonly CustomerInformationService _customerInformationService;
    private readonly ToolReasoningService _toolReasoningService;
    private readonly InventoryService _inventoryService;

    public SingleAgentController(
        ILogger<SingleAgentController> logger,        
        AnalyzePhotoService analyzePhotoService,
        CustomerInformationService customerInformationService,
        ToolReasoningService toolReasoningService,
        InventoryService inventoryService,
        SemanticKernelProvider semanticKernelProvider)
    {
        _logger = logger;
        _kernel = semanticKernelProvider.GetKernel();
        _analyzePhotoService = analyzePhotoService;
        _customerInformationService = customerInformationService;
        _toolReasoningService = toolReasoningService;
        _inventoryService = inventoryService;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<SharedEntities.SingleAgentAnalysisResponse>> AnalyzeAsync(
        [FromForm] IFormFile image,
        [FromForm] string prompt,
        [FromForm] string customerId)
    {
        try
        {
            _logger.LogInformation("Starting analysis for customer {CustomerId}", customerId);

            // Create a single agent that orchestrates the entire process
            var agent = CreateZavaAgentAssistant();
            
            // Step 1: Analyze the image
            var photoAnalysis = await _analyzePhotoService.AnalyzePhotoAsync(image, prompt);
            
            // Step 2: Get customer information
            var customerInfo = await _customerInformationService.GetCustomerInformationAsync(customerId);
            
            // Step 3: Use Single Semantic Kernel Agent to reason about tools needed
            var reasoning = await GenerateToolReasoningWithAgentAsync(agent, photoAnalysis, customerInfo, prompt);
            
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
            _logger.LogError(ex, "Error analyzing request for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    private ChatCompletionAgent CreateZavaAgentAssistant()
    {
        return new ChatCompletionAgent()
        {
            Name = "ZavaAssistant",
            Instructions = @"
You are Zava, an expert DIY and home improvement assistant. Your role is to:

1. Analyze customer projects and provide detailed tool recommendations
2. Consider the customer's existing tools and skill level
3. Provide clear, practical reasoning for each recommendation
4. Prioritize safety in all recommendations
5. Be encouraging while being realistic about project complexity
6. Offer specific tips based on the customer's skill level

Always format your responses in a clear, structured way with sections for:
- Project Analysis
- Customer Assessment
- Tool Recommendations
- Safety Considerations
- Success Tips

Be concise but thorough, and always prioritize the customer's safety and success.",
            Kernel = _kernel
        };
    }

    private async Task<string> GenerateToolReasoningWithAgentAsync(ChatCompletionAgent agent, PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
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
                return reasoning;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to call ToolReasoningService, using agent fallback");
            }

            // Fallback to using the kernel directly
            var agentPrompt = $@"
You are Zava, an expert DIY and home improvement assistant. Analyze this DIY project and provide tool recommendations:

Project: {prompt}
Image Analysis: {photoAnalysis.Description}
Detected Materials: {string.Join(", ", photoAnalysis.DetectedMaterials)}
Customer Tools: {string.Join(", ", customer.OwnedTools)}
Customer Skills: {string.Join(", ", customer.Skills)}

Provide detailed reasoning for tool recommendations considering their existing tools and skill level. Be encouraging but prioritize safety.";

            try
            {
                var result = await _kernel.InvokePromptAsync(agentPrompt);
                return result.GetValue<string>() ?? GenerateFallbackReasoning(photoAnalysis, customer, prompt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kernel invocation failed, using fallback");
                return GenerateFallbackReasoning(photoAnalysis, customer, prompt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate AI reasoning, using fallback");
            return GenerateFallbackReasoning(photoAnalysis, customer, prompt);
        }
    }

    private string GenerateFallbackReasoning(PhotoAnalysisResult photoAnalysis, CustomerInformation customer, string prompt)
    {
        return $"Based on the task '{prompt}' and the detected materials ({string.Join(", ", photoAnalysis.DetectedMaterials)}), specific tools will be recommended to complement your existing tools: {string.Join(", ", customer.OwnedTools)}.";
    }
}