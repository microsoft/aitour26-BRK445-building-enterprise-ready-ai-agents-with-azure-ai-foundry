using SingleAgentDemo.Models;

namespace SingleAgentDemo.Services;

public class CustomerInformationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomerInformationService> _logger;

    public CustomerInformationService(HttpClient httpClient, ILogger<CustomerInformationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CustomerInformation> GetCustomerInformationAsync(string customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/Customer/{customerId}");
            
            _logger.LogInformation($"CustomerInformationService HTTP status code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<CustomerInformation>();
                return result ?? CreateFallbackCustomer(customerId);
            }
            
            _logger.LogWarning("CustomerInformationService returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CustomerInformationService");
        }

        return CreateFallbackCustomer(customerId);
    }

    public async Task<ToolMatchResult> MatchToolsAsync(string customerId, string[] detectedMaterials, string prompt)
    {
        try
        {
            var matchRequest = new ToolMatchRequest
            {
                CustomerId = customerId,
                DetectedMaterials = detectedMaterials,
                Prompt = prompt
            };

            var response = await _httpClient.PostAsJsonAsync("/api/Customer/match-tools", matchRequest);
            
            _logger.LogInformation($"CustomerInformationService MatchTools HTTP status code: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ToolMatchResult>();
                return result ?? CreateFallbackToolMatch();
            }
            
            _logger.LogWarning("CustomerInformationService MatchTools returned non-success status: {StatusCode}", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling CustomerInformationService for tool matching");
        }

        return CreateFallbackToolMatch();
    }

    private CustomerInformation CreateFallbackCustomer(string customerId)
    {
        return new CustomerInformation
        {
            Id = customerId,
            Name = $"Customer {customerId}",
            OwnedTools = new[] { "hammer", "screwdriver", "measuring tape" },
            Skills = new[] { "basic DIY", "painting" }
        };
    }

    private ToolMatchResult CreateFallbackToolMatch()
    {
        return new ToolMatchResult
        {
            ReusableTools = new[] { "measuring tape", "screwdriver" },
            MissingTools = new[]
            {
                new InternalToolRecommendation { Name = "Paint Roller", Sku = "PAINT-ROLLER-9IN", IsAvailable = true, Price = 12.99m, Description = "9-inch paint roller for smooth walls" },
                new InternalToolRecommendation { Name = "Paint Brush Set", Sku = "BRUSH-SET-3PC", IsAvailable = true, Price = 24.99m, Description = "3-piece brush set for detail work" },
                new InternalToolRecommendation { Name = "Drop Cloth", Sku = "DROP-CLOTH-9X12", IsAvailable = true, Price = 8.99m, Description = "Plastic drop cloth protection" }
            }
        };
    }
}