using SharedEntities;

namespace MultiAgentDemo.Services;

public class LocationAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocationAgentService> _logger;

    public LocationAgentService(HttpClient httpClient, ILogger<LocationAgentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LocationResult> FindProductLocationAsync(string productQuery)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/location/find?product={Uri.EscapeDataString(productQuery)}");
            
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
