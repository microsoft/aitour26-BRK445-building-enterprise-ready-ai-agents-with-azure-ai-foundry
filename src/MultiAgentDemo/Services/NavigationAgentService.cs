using SharedEntities;

namespace MultiAgentDemo.Services;

public class NavigationAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NavigationAgentService> _logger;
    private string _framework = "sk"; // Default to Semantic Kernel

    public NavigationAgentService(HttpClient httpClient, ILogger<NavigationAgentService> logger)
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
        _logger.LogInformation($"[NavigationAgentService] Framework set to: {_framework}");
    }

    public async Task<NavigationInstructions> GenerateDirectionsAsync(Location fromLocation, Location toLocation)
    {
        try
        {
            var request = new { From = fromLocation, To = toLocation };
            var endpoint = $"/api/navigation/directions/{_framework}";
            _logger.LogInformation($"[NavigationAgentService] Calling endpoint: {endpoint}");
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            _logger.LogInformation($"NavigationAgentService HTTP status code: {response.StatusCode}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<NavigationInstructions>();
                return result ?? CreateFallbackNavigationInstructions(fromLocation, toLocation);
            }

            _logger.LogWarning("NavigationAgentService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling NavigationAgentService");
        }

        return CreateFallbackNavigationInstructions(fromLocation, toLocation);
    }

    private NavigationInstructions CreateFallbackNavigationInstructions(Location fromLocation, Location toLocation)
    {
        return new NavigationInstructions
        {
            Steps = new[]
            {
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
            }
        };
    }
}