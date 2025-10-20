using Microsoft.AspNetCore.Mvc;
using SharedEntities;

namespace LocationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
    private readonly ILogger<LocationController> _logger;

    public LocationController(ILogger<LocationController> logger)
    {
        _logger = logger;
    }

    [HttpGet("find/sk")]
    public async Task<ActionResult<LocationResult>> FindProductLocationSkAsync([FromQuery] string product)
    {
        _logger.LogInformation("[SK] Finding location for product: {Product}", product);
        return await FindProductLocationInternalAsync(product);
    }

    [HttpGet("find/agentfx")]
    public async Task<ActionResult<LocationResult>> FindProductLocationAgentFxAsync([FromQuery] string product)
    {
        _logger.LogInformation("[AgentFx] Finding location for product: {Product}", product);
        return await FindProductLocationInternalAsync(product);
    }

    private async Task<ActionResult<LocationResult>> FindProductLocationInternalAsync(string product)
    {
        // AI-powered location finding logic would go here
        // For now, return demo data based on product type
        await Task.Delay(50); // Simulate processing
        
        var result = new LocationResult
        {
            StoreLocations = GenerateLocationsByProduct(product)
        };

        return Ok(result);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "LocationService" });
    }

    private StoreLocation[] GenerateLocationsByProduct(string product)
    {
        // Simple logic to generate different locations based on product type
        var productLower = product.ToLowerInvariant();
        
        if (productLower.Contains("tool") || productLower.Contains("drill") || productLower.Contains("hammer"))
        {
            return new[]
            {
                new StoreLocation 
                { 
                    Section = "Hardware Tools", 
                    Aisle = "A1", 
                    Shelf = "Middle", 
                    Description = $"Hand and power tools section - {product}" 
                }
            };
        }
        else if (productLower.Contains("paint") || productLower.Contains("brush"))
        {
            return new[]
            {
                new StoreLocation 
                { 
                    Section = "Paint & Supplies", 
                    Aisle = "B3", 
                    Shelf = "Top", 
                    Description = $"Paint and painting supplies - {product}" 
                }
            };
        }
        else if (productLower.Contains("garden") || productLower.Contains("plant"))
        {
            return new[]
            {
                new StoreLocation 
                { 
                    Section = "Garden Center", 
                    Aisle = "Outside", 
                    Shelf = "Ground Level", 
                    Description = $"Outdoor garden section - {product}" 
                }
            };
        }
        else
        {
            return new[]
            {
                new StoreLocation 
                { 
                    Section = "General Merchandise", 
                    Aisle = "C2", 
                    Shelf = "Middle", 
                    Description = $"General location for {product}" 
                }
            };
        }
    }
}