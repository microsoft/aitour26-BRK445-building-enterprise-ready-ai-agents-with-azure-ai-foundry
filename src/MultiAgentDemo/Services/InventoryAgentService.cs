using SharedEntities;

namespace MultiAgentDemo.Services;

public class InventoryAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryAgentService> _logger;
    private string _framework = "sk"; // Default to Semantic Kernel

    public InventoryAgentService(HttpClient httpClient, ILogger<InventoryAgentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Sets the agent framework to use for service calls
    /// </summary>
    /// <param name="framework">"sk" for Semantic Kernel or "agentfx" for Microsoft Agent Framework</param>
    public void SetFramework(string framework)
    {
        _framework = framework?.ToLowerInvariant() ?? "sk";
        _logger.LogInformation($"[InventoryAgentService] Framework set to: {_framework}");
    }

    public async Task<InventorySearchResult> SearchProductsAsync(string productQuery)
    {
        try
        {
            InventorySearchRequest request = new()
            {
                SearchQuery = productQuery
            };

            var httpContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var endpoint = $"/api/search/{_framework}";
            _logger.LogInformation($"[InventoryAgentService] Calling endpoint: {endpoint}");
            var response = await _httpClient.PostAsync(endpoint, httpContent);

            _logger.LogInformation($"InventoryAgentService HTTP status code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<InventorySearchResult>();
                return result ?? CreateFallbackInventoryResult(productQuery);
            }

            _logger.LogWarning("InventoryAgentService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling InventoryAgentService");
        }

        return CreateFallbackInventoryResult(productQuery);
    }

    private InventorySearchResult CreateFallbackInventoryResult(string productQuery)
    {
        return new InventorySearchResult
        {
            ProductsFound = new[]
            {
                new ProductInfo { Name = $"Demo Product for {productQuery}", Sku = "DEMO-001", Price = 29.99m, IsAvailable = true }
            },
            TotalCount = 1,
            SearchQuery = productQuery
        };
    }
}
