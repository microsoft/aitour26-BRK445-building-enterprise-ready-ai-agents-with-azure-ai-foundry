using SingleAgentDemo.Models;

namespace SingleAgentDemo.Services;

public class InventoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryService> _logger;
    private string _framework = "sk"; // Default to Semantic Kernel

    public InventoryService(HttpClient httpClient, ILogger<InventoryService> logger)
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
        _logger.LogInformation($"[InventoryService] Framework set to: {_framework}");
    }

    public async Task<InternalToolRecommendation[]> EnrichWithInventoryAsync(InternalToolRecommendation[] tools)
    {
        try
        {
            var skus = tools.Select(t => t.Sku).ToArray();
            var searchRequest = new InventorySearchRequest { Skus = skus };
            
            var endpoint = $"/api/search/{_framework}";
            _logger.LogInformation($"[InventoryService] Calling endpoint: {endpoint}");
            var response = await _httpClient.PostAsJsonAsync(endpoint, searchRequest);
            
            _logger.LogInformation($"InventoryService HTTP status code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var inventoryResults = await response.Content.ReadFromJsonAsync<InternalToolRecommendation[]>();
                return inventoryResults ?? tools;
            }
            
            _logger.LogWarning("InventoryService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling InventoryService");
        }

        return tools; // Return original tools if inventory service fails
    }
}