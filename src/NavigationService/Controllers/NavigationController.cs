using Microsoft.AspNetCore.Mvc;
using SharedEntities;

namespace NavigationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NavigationController : ControllerBase
{
    private readonly ILogger<NavigationController> _logger;

    public NavigationController(ILogger<NavigationController> logger)
    {
        _logger = logger;
    }

    [HttpPost("directions/sk")]
    public async Task<ActionResult<NavigationInstructions>> GenerateDirectionsSkAsync([FromBody] DirectionsRequest request)
    {
        _logger.LogInformation("[SK] Generating directions from {From} to {To}", request.From, request.To);
        return await GenerateDirectionsInternalAsync(request);
    }

    [HttpPost("directions/agentfx")]
    public async Task<ActionResult<NavigationInstructions>> GenerateDirectionsAgentFxAsync([FromBody] DirectionsRequest request)
    {
        _logger.LogInformation("[AgentFx] Generating directions from {From} to {To}", request.From, request.To);
        return await GenerateDirectionsInternalAsync(request);
    }

    private async Task<ActionResult<NavigationInstructions>> GenerateDirectionsInternalAsync(DirectionsRequest request)
    {
        // AI-powered navigation logic would go here
        // For now, return demo navigation steps
        await Task.Delay(50); // Simulate processing
        
        var result = new NavigationInstructions
        {
            Steps = GenerateNavigationSteps(request.From, request.To)
        };

        return Ok(result);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "NavigationService" });
    }

    private NavigationStep[] GenerateNavigationSteps(Location from, Location to)
    {
        var steps = new List<NavigationStep>();

        // Add starting step
        steps.Add(new NavigationStep
        {
            Direction = "Start",
            Description = $"Begin your journey from {from}",
            Landmark = new NavigationLandmark { Location = from }
        });

        // Add intermediate steps based on locations
        if (from.ToString() != to.ToString())
        {
            // Simple navigation logic - in real implementation this would use store layout
            steps.Add(new NavigationStep
            {
                Direction = "Walk Forward",
                Description = "Head towards the main aisle",
                Landmark = null
            });

            steps.Add(new NavigationStep
            {
                Direction = "Turn Right",
                Description = "Turn right at the customer service desk",
                Landmark = new NavigationLandmark { Description = "Customer Service Desk" }
            });

            steps.Add(new NavigationStep
            {
                Direction = "Continue",
                Description = $"Continue straight towards {to} section",
                Landmark = null
            });
        }

        // Add final step
        steps.Add(new NavigationStep
        {
            Direction = "Arrive",
            Description = $"You have arrived at {to}",
            Landmark = new NavigationLandmark { Location = to }
        });

        return steps.ToArray();
    }
}

public class DirectionsRequest
{
    public Location From { get; set; } = null!;
    public Location To { get; set; } = null!;
}