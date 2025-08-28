using SingleAgentDemo.Models;

namespace SingleAgentDemo.Services;

public class InventoryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(HttpClient httpClient, ILogger<InventoryService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<InternalToolRecommendation[]> EnrichWithInventoryAsync(InternalToolRecommendation[] tools)
    {
        try
        {
            var skus = tools.Select(t => t.Sku).ToArray();
            var searchRequest = new InventorySearchRequest { Skus = skus };
            
            var response = await _httpClient.PostAsJsonAsync("/api/search", searchRequest);
            
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