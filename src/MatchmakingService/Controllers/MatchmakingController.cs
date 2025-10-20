using Microsoft.AspNetCore.Mvc;
using SharedEntities;

namespace MatchmakingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchmakingController : ControllerBase
{
    private readonly ILogger<MatchmakingController> _logger;

    public MatchmakingController(ILogger<MatchmakingController> logger)
    {
        _logger = logger;
    }

    [HttpPost("alternatives/sk")]
    public async Task<ActionResult<MatchmakingResult>> FindAlternativesSkAsync([FromBody] AlternativesRequest request)
    {
        _logger.LogInformation("[SK] Finding alternatives for product: {ProductQuery}, User: {UserId}", 
            request.ProductQuery, request.UserId);
        return await FindAlternativesInternalAsync(request);
    }

    [HttpPost("alternatives/agentfx")]
    public async Task<ActionResult<MatchmakingResult>> FindAlternativesAgentFxAsync([FromBody] AlternativesRequest request)
    {
        _logger.LogInformation("[AgentFx] Finding alternatives for product: {ProductQuery}, User: {UserId}", 
            request.ProductQuery, request.UserId);
        return await FindAlternativesInternalAsync(request);
    }

    private async Task<ActionResult<MatchmakingResult>> FindAlternativesInternalAsync(AlternativesRequest request)
    {
        // AI-powered product matching logic would go here
        // For now, return demo data
        await Task.Delay(50); // Simulate processing
        
        var result = new MatchmakingResult
        {
            Alternatives = new[]
            {
                new ProductInfo 
                { 
                    Name = $"Alternative Tool for {request.ProductQuery}", 
                    Sku = "ALT-001", 
                    Price = 19.99m, 
                    IsAvailable = true 
                },
                new ProductInfo 
                { 
                    Name = $"Premium Alternative for {request.ProductQuery}", 
                    Sku = "ALT-002", 
                    Price = 29.99m, 
                    IsAvailable = true 
                }
            },
            SimilarProducts = new[]
            {
                new ProductInfo 
                { 
                    Name = $"Similar Tool to {request.ProductQuery}", 
                    Sku = "SIM-001", 
                    Price = 24.99m, 
                    IsAvailable = true 
                }
            }
        };

        return Ok(result);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "MatchmakingService" });
    }
}
