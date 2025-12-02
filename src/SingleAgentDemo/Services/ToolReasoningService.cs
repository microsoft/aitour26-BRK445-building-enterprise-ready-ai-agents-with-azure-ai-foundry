using SingleAgentDemo.Models;

namespace SingleAgentDemo.Services;

public class ToolReasoningService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ToolReasoningService> _logger;
    private string _framework = "sk"; // Default to Semantic Kernel

    public ToolReasoningService(HttpClient httpClient, ILogger<ToolReasoningService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Sets the agent framework to use for service calls
    /// </summary>
    /// <param name="framework">"llm" for LLM Direct Call, "sk" for Semantic Kernel, or "maf" for Microsoft Agent Framework</param>
    public void SetFramework(string framework)
    {
        _framework = framework?.ToLowerInvariant() ?? "sk";
        _logger.LogInformation($"[ToolReasoningService] Framework set to: {_framework}");
    }

    public async Task<string> GenerateReasoningAsync(ReasoningRequest request)
    {
        try
        {
            var endpoint = $"/api/Reasoning/generate/{_framework}";
            _logger.LogInformation($"[ToolReasoningService] Calling endpoint: {endpoint}");
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            
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