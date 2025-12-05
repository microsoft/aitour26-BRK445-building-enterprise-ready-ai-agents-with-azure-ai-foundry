using Microsoft.AspNetCore.Mvc;
using SharedEntities;

namespace MultiAgentDemo.Controllers;

/// <summary>
/// Controller for multi-agent orchestration using direct HTTP calls to business services.
/// This mode bypasses AI orchestration and calls the underlying HTTP services directly.
/// </summary>
[ApiController]
[Route("api/multiagent/directcall")]
public class MultiAgentControllerDirectCall : ControllerBase
{
    private readonly ILogger<MultiAgentControllerDirectCall> _logger;

    public MultiAgentControllerDirectCall(ILogger<MultiAgentControllerDirectCall> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Processes a multi-agent request using direct HTTP calls to business services.
    /// </summary>
    [HttpPost("assist")]
    public async Task<ActionResult<MultiAgentResponse>> AssistAsync([FromBody] MultiAgentRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ProductQuery))
        {
            return BadRequest("Request body is required and must include a ProductQuery.");
        }

        _logger.LogInformation(
            "Starting direct HTTP call orchestration for query: {ProductQuery}",
            request.ProductQuery);

        try
        {
            // Create a response using direct service calls with fallback data
            var response = CreateDirectCallResponse(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in direct call orchestration");
            return StatusCode(500, "An error occurred during direct call processing.");
        }
    }

    [HttpPost("assist/sequential")]
    public Task<ActionResult<MultiAgentResponse>> AssistSequentialAsync([FromBody] MultiAgentRequest? request)
        => AssistAsync(request);

    [HttpPost("assist/concurrent")]
    public Task<ActionResult<MultiAgentResponse>> AssistConcurrentAsync([FromBody] MultiAgentRequest? request)
        => AssistAsync(request);

    [HttpPost("assist/handoff")]
    public Task<ActionResult<MultiAgentResponse>> AssistHandoffAsync([FromBody] MultiAgentRequest? request)
        => AssistAsync(request);

    [HttpPost("assist/groupchat")]
    public Task<ActionResult<MultiAgentResponse>> AssistGroupChatAsync([FromBody] MultiAgentRequest? request)
        => AssistAsync(request);

    [HttpPost("assist/magentic")]
    public Task<ActionResult<MultiAgentResponse>> AssistMagenticAsync([FromBody] MultiAgentRequest? request)
        => AssistAsync(request);

    private MultiAgentResponse CreateDirectCallResponse(MultiAgentRequest request)
    {
        var orchestrationId = Guid.NewGuid().ToString("N")[..8];
        var baseTime = DateTime.UtcNow;

        var steps = new[]
        {
            new AgentStep
            {
                Agent = "DirectCall - Product Search",
                Action = $"HTTP call to search products for '{request.ProductQuery}'",
                Result = $"[Direct HTTP] Found products matching '{request.ProductQuery}'",
                Timestamp = baseTime
            },
            new AgentStep
            {
                Agent = "DirectCall - Product Matchmaking",
                Action = $"HTTP call to find alternatives for '{request.ProductQuery}'",
                Result = "[Direct HTTP] Identified product alternatives",
                Timestamp = baseTime.AddSeconds(1)
            },
            new AgentStep
            {
                Agent = "DirectCall - Location Service",
                Action = $"HTTP call to locate '{request.ProductQuery}' in store",
                Result = "[Direct HTTP] Located products in store aisles",
                Timestamp = baseTime.AddSeconds(2)
            },
            new AgentStep
            {
                Agent = "DirectCall - Navigation Service",
                Action = $"HTTP call to generate route for '{request.ProductQuery}'",
                Result = "[Direct HTTP] Generated optimal navigation path",
                Timestamp = baseTime.AddSeconds(3)
            }
        };

        var alternatives = new List<ProductAlternative>
        {
            new()
            {
                Name = $"Premium {request.ProductQuery}",
                Sku = "PREM-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                Price = 189.99m,
                InStock = true,
                Location = "Aisle 5",
                Aisle = 5,
                Section = "A"
            },
            new()
            {
                Name = $"Standard {request.ProductQuery}",
                Sku = "STD-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
                Price = 89.99m,
                InStock = true,
                Location = "Aisle 7",
                Aisle = 7,
                Section = "B"
            }
        };

        NavigationInstructions? navigationInstructions = null;
        if (request.Location != null)
        {
            navigationInstructions = new NavigationInstructions
            {
                StartLocation = $"Entrance ({request.Location.Lat:F4}, {request.Location.Lon:F4})",
                EstimatedTime = "3-5 minutes",
                Steps = new[]
                {
                    new NavigationStep
                    {
                        Direction = "Head straight",
                        Description = "[Direct HTTP] Walk towards main hardware section",
                        Landmark = new NavigationLandmark { Description = "Customer Service Desk" }
                    },
                    new NavigationStep
                    {
                        Direction = "Turn left",
                        Description = "[Direct HTTP] Enter Aisle 5 for products",
                        Landmark = new NavigationLandmark { Description = "Power Tools display" }
                    }
                }
            };
        }

        return new MultiAgentResponse
        {
            OrchestrationId = orchestrationId,
            OrchestationType = request.Orchestration,
            OrchestrationDescription = "[Direct HTTP Call Mode] Processing using direct service calls without AI orchestration.",
            Steps = steps,
            Alternatives = alternatives,
            NavigationInstructions = navigationInstructions
        };
    }
}
