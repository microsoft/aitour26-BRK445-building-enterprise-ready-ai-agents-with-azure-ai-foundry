using SharedEntities;
using System.Text.Json;

namespace Store.Services;

public class AgentCatalogService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AgentCatalogService> _logger;

    public AgentCatalogService(HttpClient httpClient, ILogger<AgentCatalogService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<AgentListResponse> GetAvailableAgentsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching available agents");
            
            var response = await _httpClient.GetAsync("/api/agents");
            
            if (response.IsSuccessStatusCode)
            {
                var responseText = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AgentListResponse>(responseText, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return result ?? CreateFallbackAgentList();
            }
            else
            {
                _logger.LogWarning("Agent list service returned non-success status: {StatusCode}", response.StatusCode);
                return CreateFallbackAgentList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available agents");
            return CreateFallbackAgentList();
        }
    }

    public async Task<AgentTesterResponse> TestAgentAsync(AgentTesterRequest request)
    {
        try
        {
            _logger.LogInformation("Testing agent {AgentId} with question: {Question}", request.AgentId, request.Question);
            
            var response = await _httpClient.PostAsJsonAsync("/api/test", request);
            var responseText = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Agent tester service response - Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, responseText);
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<AgentTesterResponse>(responseText, new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
                return result ?? CreateFallbackResponse(request);
            }
            else
            {
                _logger.LogWarning("Agent tester service returned non-success status: {StatusCode}", response.StatusCode);
                return CreateFallbackResponse(request);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing agent {AgentId}", request.AgentId);
            return CreateFallbackResponse(request, ex.Message);
        }
    }

    private AgentListResponse CreateFallbackAgentList()
    {
        return new AgentListResponse
        {
            Agents = new[]
            {
                new AvailableAgent 
                { 
                    AgentId = "toolreasoningagent", 
                    AgentName = "Tool Reasoning Agent", 
                    Description = "Provides reasoning for tool recommendations based on DIY projects" 
                },
                new AvailableAgent 
                { 
                    AgentId = "photoanalysisagent", 
                    AgentName = "Photo Analysis Agent", 
                    Description = "Analyzes photos and identifies materials and project requirements" 
                },
                new AvailableAgent 
                { 
                    AgentId = "inventoryagent", 
                    AgentName = "Inventory Agent", 
                    Description = "Searches product inventory and provides availability information" 
                }
            }
        };
    }

    private AgentTesterResponse CreateFallbackResponse(AgentTesterRequest request, string? errorMessage = null)
    {
        var agentList = CreateFallbackAgentList();
        var agent = agentList.Agents.FirstOrDefault(a => a.AgentId == request.AgentId);
        
        return new AgentTesterResponse
        {
            AgentId = request.AgentId,
            AgentName = agent?.AgentName ?? "Unknown Agent",
            Question = request.Question,
            Response = errorMessage == null 
                ? $"Hello! I'm the {agent?.AgentName ?? "test agent"}. You asked: '{request.Question}'. This is a demo response since the agent service is not available."
                : $"Error occurred while testing the agent: {errorMessage}",
            Timestamp = DateTime.UtcNow,
            IsSuccessful = errorMessage == null,
            ErrorMessage = errorMessage
        };
    }
}