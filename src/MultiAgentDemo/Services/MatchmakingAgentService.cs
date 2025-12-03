using SharedEntities;

namespace MultiAgentDemo.Services;

/// <summary>
/// Service for interacting with the matchmaking/alternatives external service.
/// </summary>
public class MatchmakingAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MatchmakingAgentService> _logger;
    private string _framework = "llm";

    public MatchmakingAgentService(HttpClient httpClient, ILogger<MatchmakingAgentService> logger)
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
        _logger.LogDebug("MatchmakingAgentService framework set to: {Framework}", _framework);
    }

    /// <summary>
    /// Finds product alternatives for the given query and user.
    /// </summary>
    public async Task<MatchmakingResult> FindAlternativesAsync(string productQuery, string userId)
    {
        try
        {
            var request = new { ProductQuery = productQuery, UserId = userId };
            var endpoint = $"/api/matchmaking/alternatives/{_framework}";
            
            _logger.LogDebug("Calling MatchmakingService endpoint: {Endpoint}", endpoint);
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            _logger.LogDebug("MatchmakingService response status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MatchmakingResult>();
                return result ?? CreateFallbackResult(productQuery);
            }

            _logger.LogWarning("MatchmakingService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MatchmakingService");
        }

        return CreateFallbackResult(productQuery);
    }

    private static MatchmakingResult CreateFallbackResult(string productQuery) => new()
    {
        Alternatives =
        [
            new ProductInfo { Name = $"Alternative for {productQuery}", Sku = "ALT-001", Price = 19.99m, IsAvailable = true }
        ],
        SimilarProducts =
        [
            new ProductInfo { Name = $"Similar to {productQuery}", Sku = "SIM-001", Price = 24.99m, IsAvailable = true }
        ]
    };
}
