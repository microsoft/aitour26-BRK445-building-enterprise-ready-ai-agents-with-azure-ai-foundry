using SharedEntities;
using System.Text.Json;

namespace Store.Services;

public class SingleAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SingleAgentService> _logger;
    private readonly AgentFrameworkService _frameworkService;

    public SingleAgentService(HttpClient httpClient, ILogger<SingleAgentService> logger, AgentFrameworkService frameworkService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _frameworkService = frameworkService;
    }

    public async Task<SingleAgentAnalysisResponse?> AnalyzeAsync(SingleAgentAnalysisRequest request)
    {
        try
        {
            var framework = await _frameworkService.GetSelectedFrameworkAsync();
            
            using var content = new MultipartFormDataContent();
            
            // Handle the image data from the shared entity
            if (request.ImageData != null)
            {
                var fileContent = new ByteArrayContent(request.ImageData);
                if (!string.IsNullOrEmpty(request.ImageContentType))
                {
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(request.ImageContentType);
                }
                content.Add(fileContent, "image", request.ImageFileName ?? "image");
            }
            
            content.Add(new StringContent(request.Prompt), "prompt");
            content.Add(new StringContent(request.CustomerId), "customerId");

            var frameworkPath = framework switch
            {
                "agentfx" => "agentfx",
                "llm" => "llm",
                _ => "sk"
            };
            var endpoint = $"/api/singleagent/{frameworkPath}/analyze";

            _logger.LogInformation("Calling single agent service for customer {CustomerId} using {Framework} framework", request.CustomerId, framework);
            
            var response = await _httpClient.PostAsync(endpoint, content);
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Single agent service response - Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, responseText);
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<SingleAgentAnalysisResponse>(responseText, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return result;
            }
            else
            {
                _logger.LogWarning("Single agent service returned non-success status: {StatusCode}", response.StatusCode);
                return CreateFallbackResponse(request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling single agent service for customer {CustomerId}", request.CustomerId);
            return CreateFallbackResponse(request);
        }
    }

    private SingleAgentAnalysisResponse CreateFallbackResponse(SingleAgentAnalysisRequest request)
    {
        return new SingleAgentAnalysisResponse
        {
            Analysis = $"Analysis of your project: {request.Prompt}. The image shows a room that requires surface preparation and painting work.",
            ReusableTools = new[] { "measuring tape", "screwdriver", "hammer" },
            RecommendedTools = new[]
            {
                new ToolRecommendation 
                { 
                    Name = "Paint Roller Set", 
                    Sku = "PAINT-ROLLER-9IN", 
                    IsAvailable = true, 
                    Price = 12.99m, 
                    Description = "9-inch paint roller with tray for smooth wall coverage" 
                },
                new ToolRecommendation 
                { 
                    Name = "Brush Set (3-piece)", 
                    Sku = "BRUSH-SET-3PC", 
                    IsAvailable = true, 
                    Price = 24.99m, 
                    Description = "Professional brush set for edges and detail work" 
                },
                new ToolRecommendation 
                { 
                    Name = "Drop Cloth", 
                    Sku = "DROP-CLOTH-9X12", 
                    IsAvailable = false, 
                    Price = 8.99m, 
                    Description = "Plastic drop cloth for floor protection" 
                }
            },
            Reasoning = $"Based on your project '{request.Prompt}' and the image analysis, you'll need painting tools to complement your existing basic tools. The recommended items will ensure professional results for your painting project."
        };
    }
}