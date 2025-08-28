using SharedEntities;

namespace MultiAgentDemo.Services;

public class MatchmakingAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MatchmakingAgentService> _logger;

    public MatchmakingAgentService(HttpClient httpClient, ILogger<MatchmakingAgentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MatchmakingResult> FindAlternativesAsync(string productQuery, string userId)
    {
        try
        {
            var request = new { ProductQuery = productQuery, UserId = userId };
            var response = await _httpClient.PostAsJsonAsync("/api/matchmaking/alternatives", request);
            
            _logger.LogInformation($"MatchmakingAgentService HTTP status code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<MatchmakingResult>();
                return result ?? CreateFallbackMatchmakingResult(productQuery);
            }
            
            _logger.LogWarning("MatchmakingAgentService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling MatchmakingAgentService");
        }

        return CreateFallbackMatchmakingResult(productQuery);
    }

    private MatchmakingResult CreateFallbackMatchmakingResult(string productQuery)
    {
        return new MatchmakingResult
        {
            Alternatives = new[]
            {
                new ProductInfo { Name = $"Alternative for {productQuery}", Sku = "ALT-001", Price = 19.99m, IsAvailable = true }
            },
            SimilarProducts = new[]
            {
                new ProductInfo { Name = $"Similar to {productQuery}", Sku = "SIM-001", Price = 24.99m, IsAvailable = true }
            }
        };
    }
}
