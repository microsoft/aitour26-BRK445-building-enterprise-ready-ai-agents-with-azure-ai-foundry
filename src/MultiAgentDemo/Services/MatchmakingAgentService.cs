using SharedEntities;

namespace MultiAgentDemo.Services;

public class MatchmakingAgentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MatchmakingAgentService> _logger;
    private string _framework = "sk"; // Default to Semantic Kernel

    public MatchmakingAgentService(HttpClient httpClient, ILogger<MatchmakingAgentService> logger)
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
        _logger.LogInformation($"[MatchmakingAgentService] Framework set to: {_framework}");
    }

    public async Task<MatchmakingResult> FindAlternativesAsync(string productQuery, string userId)
    {
        try
        {
            var request = new { ProductQuery = productQuery, UserId = userId };
            var endpoint = $"/api/matchmaking/alternatives/{_framework}";
            _logger.LogInformation($"[MatchmakingAgentService] Calling endpoint: {endpoint}");
            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            
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
