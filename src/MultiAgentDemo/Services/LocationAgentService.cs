using SharedEntities;

namespace MultiAgentDemo.Services;

public class LocationAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationAgentService> _logger;
    private string _framework = "sk"; // Default to Semantic Kernel

    public LocationAgentService(HttpClient httpClient, ILogger<LocationAgentService> logger)
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
        _logger.LogInformation($"[LocationAgentService] Framework set to: {_framework}");
    }

    public async Task<LocationResult> FindProductLocationAsync(string productQuery)
    {
        try
        {
            var endpoint = $"/api/location/find/{_framework}?product={Uri.EscapeDataString(productQuery)}";
            _logger.LogInformation($"[LocationAgentService] Calling endpoint: {endpoint}");
            var response = await _httpClient.GetAsync(endpoint);
            
            _logger.LogInformation($"LocationAgentService HTTP status code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LocationResult>();
                return result ?? CreateFallbackLocationResult(productQuery);
            }
            
            _logger.LogWarning("LocationAgentService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling LocationAgentService");
        }

        return CreateFallbackLocationResult(productQuery);
    }

    private LocationResult CreateFallbackLocationResult(string productQuery) => new LocationResult
    {
        StoreLocations =
            [
                new StoreLocation { Section = "Hardware", Aisle = "A1", Shelf = "Top", Description = $"Location for {productQuery}" }
            ]
    };
}
