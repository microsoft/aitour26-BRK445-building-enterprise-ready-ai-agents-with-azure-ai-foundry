using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Service for interacting with the inventory/product search external service.
/// </summary>
public class InventoryAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InventoryAgentService> _logger;
    private string _framework = "llm";

    public InventoryAgentService(HttpClient httpClient, ILogger<InventoryAgentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Sets the agent framework to use for service calls.
    /// </summary>
    /// <param name="framework">"llm" for LLM Direct Call or "maf" for Microsoft Agent Framework.</param>
    public void SetFramework(string framework)
    {
        _framework = framework?.ToLowerInvariant() ?? "llm";
        _logger.LogDebug("InventoryAgentService framework set to: {Framework}", _framework);
    }

    /// <summary>
    /// Searches for products matching the given query.
    /// </summary>
    public async Task<InventorySearchResult> SearchProductsAsync(string productQuery)
    {
        try
        {
            var request = new InventorySearchRequest { SearchQuery = productQuery };
            var httpContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            var endpoint = $"/api/search/{_framework}";
            _logger.LogDebug("Calling InventoryService endpoint: {Endpoint}", endpoint);
            
            var response = await _httpClient.PostAsync(endpoint, httpContent);
            _logger.LogDebug("InventoryService response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<InventorySearchResult>();
                return result ?? CreateFallbackResult(productQuery);
            }

            _logger.LogWarning("InventoryService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling InventoryService");
        }

        return CreateFallbackResult(productQuery);
    }

    private static InventorySearchResult CreateFallbackResult(string productQuery) => new()
    {
        ProductsFound =
        [
            new ProductInfo { Name = $"Demo Product for {productQuery}", Sku = "DEMO-001", Price = 29.99m, IsAvailable = true }
        ],
        TotalCount = 1,
        SearchQuery = productQuery
    };
}
