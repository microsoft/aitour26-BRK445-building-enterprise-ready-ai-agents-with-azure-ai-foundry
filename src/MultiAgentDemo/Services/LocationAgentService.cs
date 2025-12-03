using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Service for interacting with the location/product finder external service.
/// </summary>
public class LocationAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationAgentService> _logger;
    private string _framework = "llm";

    public LocationAgentService(HttpClient httpClient, ILogger<LocationAgentService> logger)
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
        _logger.LogDebug("LocationAgentService framework set to: {Framework}", _framework);
    }

    /// <summary>
    /// Finds the store location for a product.
    /// </summary>
    public async Task<LocationResult> FindProductLocationAsync(string productQuery)
    {
        try
        {
            var endpoint = $"/api/location/find/{_framework}?product={Uri.EscapeDataString(productQuery)}";
            
            _logger.LogDebug("Calling LocationService endpoint: {Endpoint}", endpoint);
            var response = await _httpClient.GetAsync(endpoint);
            _logger.LogDebug("LocationService response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LocationResult>();
                return result ?? CreateFallbackResult(productQuery);
            }

            _logger.LogWarning("LocationService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LocationService");
        }

        return CreateFallbackResult(productQuery);
    }

    private static LocationResult CreateFallbackResult(string productQuery) => new()
    {
        StoreLocations =
        [
            new StoreLocation { Section = "Hardware", Aisle = "A1", Shelf = "Top", Description = $"Location for {productQuery}" }
        ]
    };
}
