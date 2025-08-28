using SingleAgentDemo.Models;

namespace SingleAgentDemo.Services;

public class ToolReasoningService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ToolReasoningService> _logger;

    public ToolReasoningService(HttpClient httpClient, ILogger<ToolReasoningService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GenerateReasoningAsync(ReasoningRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/Reasoning/generate", request);
            
            _logger.LogInformation($"ToolReasoningService HTTP status code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var reasoning = await response.Content.ReadAsStringAsync();
                return reasoning;
            }
            
            _logger.LogWarning("ToolReasoningService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ToolReasoningService");
        }

        return GenerateFallbackReasoning(request);
    }

    private string GenerateFallbackReasoning(ReasoningRequest request)
    {
        return $"Based on the task '{request.Prompt}' and the detected materials ({string.Join(", ", request.PhotoAnalysis.DetectedMaterials)}), specific tools will be recommended to complement your existing tools: {string.Join(", ", request.Customer.OwnedTools)}.";
    }
}