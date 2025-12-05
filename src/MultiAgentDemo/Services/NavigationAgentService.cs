using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Service for interacting with the navigation/directions external service.
/// </summary>
public class NavigationAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NavigationAgentService> _logger;
    private string _framework = "llm";

    public NavigationAgentService(HttpClient httpClient, ILogger<NavigationAgentService> logger)
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
        _logger.LogDebug("NavigationAgentService framework set to: {Framework}", _framework);
    }

    /// <summary>
    /// Generates navigation directions between two locations.
    /// </summary>
    public async Task<NavigationInstructions> GenerateDirectionsAsync(Location fromLocation, Location toLocation)
    {
        try
        {
            var request = new { From = fromLocation, To = toLocation };
            var endpoint = $"/api/navigation/directions/{_framework}";
            
            _logger.LogDebug("Calling NavigationService endpoint: {Endpoint}", endpoint);
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            _logger.LogDebug("NavigationService response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NavigationInstructions>();
                return result ?? CreateFallbackResult(fromLocation, toLocation);
            }

            _logger.LogWarning("NavigationService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling NavigationService");
        }

        return CreateFallbackResult(fromLocation, toLocation);
    }

    private static NavigationInstructions CreateFallbackResult(Location fromLocation, Location toLocation) => new()
    {
        Steps =
        [
            new NavigationStep
            {
                Direction = "Start",
                Description = $"Head towards {toLocation} from {fromLocation}",
                Landmark = new NavigationLandmark { Location = fromLocation }
            },
            new NavigationStep
            {
                Direction = "Continue",
                Description = "Follow the main pathway",
                Landmark = null
            },
            new NavigationStep
            {
                Direction = "Arrive",
                Description = $"You will find your destination at {toLocation}",
                Landmark = new NavigationLandmark { Location = toLocation }
            }
        ]
    };
}