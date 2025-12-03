using Microsoft.Agents.AI;
using SharedEntities;

namespace MultiAgentDemo.Controllers;

/// <summary>
/// Provides utility methods for processing agent workflow steps and extracting typed results.
/// </summary>
public static class StepsProcessor
{
    /// <summary>
    /// Extracts navigation instructions from workflow steps.
    /// </summary>
    /// <param name="steps">The list of agent steps from the workflow.</param>
    /// <param name="navigationAgent">The navigation agent to match against.</param>
    /// <param name="location">The user's current location.</param>
    /// <param name="productQuery">The product query string.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <returns>Navigation instructions or default fallback.</returns>
    public static async Task<NavigationInstructions> GenerateNavigationInstructionsAsync(
        List<AgentStep> steps,
        AIAgent navigationAgent,
        Location? location,
        string productQuery,
        ILogger logger)
    {
        location ??= new Location { Lat = 0, Lon = 0 };

        try
        {
            var navigationStep = steps.FirstOrDefault(step => step.AgentId == navigationAgent.Id);
            if (navigationStep?.Result == null)
            {
                return CreateDefaultNavigationInstructions(location, productQuery);
            }

            try
            {
                var instructions = System.Text.Json.JsonSerializer.Deserialize<NavigationInstructions>(navigationStep.Result);
                if (instructions != null)
                {
                    logger.LogInformation("Navigation instructions deserialized from step result");
                    return instructions;
                }
            }
            catch
            {
                logger.LogDebug("Failed to deserialize navigation instructions, using fallback");
            }

            return CreateDefaultNavigationInstructions(location, productQuery);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GenerateNavigationInstructions failed, returning fallback");
            return CreateDefaultNavigationInstructions(location, productQuery);
        }
    }

    /// <summary>
    /// Creates default navigation instructions when actual data is unavailable.
    /// </summary>
    public static NavigationInstructions CreateDefaultNavigationInstructions(Location location, string productQuery)
    {
        return new NavigationInstructions
        {
            Steps =
            [
                new NavigationStep
                {
                    Direction = "Head straight",
                    Description = $"Walk towards the main area where {productQuery} is located",
                    Landmark = new NavigationLandmark { Description = "Main entrance area" }
                },
                new NavigationStep
                {
                    Direction = "Turn left",
                    Description = "Continue to the product section",
                    Landmark = new NavigationLandmark { Description = "Product display section" }
                }
            ],
            StartLocation = $"Current Location ({location.Lat:F4}, {location.Lon:F4})",
            EstimatedTime = "3-5 minutes"
        };
    }

    /// <summary>
    /// Extracts product alternatives from workflow steps.
    /// </summary>
    /// <param name="steps">The list of agent steps from the workflow.</param>
    /// <param name="productMatchmakingAgent">The matchmaking agent to match against.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <returns>List of product alternatives or defaults.</returns>
    public static async Task<List<ProductAlternative>> GetProductAlternativesFromStepsAsync(
        List<AgentStep> steps,
        AIAgent productMatchmakingAgent,
        ILogger logger)
    {
        try
        {
            var alternativesStep = steps.FirstOrDefault(step => step.AgentId == productMatchmakingAgent.Id);
            if (alternativesStep?.Result == null)
            {
                return GenerateDefaultProductAlternatives();
            }

            try
            {
                var alternatives = System.Text.Json.JsonSerializer.Deserialize<List<ProductAlternative>>(alternativesStep.Result);
                if (alternatives != null && alternatives.Count > 0)
                {
                    logger.LogInformation("Product alternatives deserialized from step result");
                    return alternatives;
                }
            }
            catch
            {
                logger.LogDebug("Failed to deserialize product alternatives, using fallback");
            }

            return GenerateDefaultProductAlternatives();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GetProductAlternatives failed, returning fallback alternatives");
            return GenerateDefaultProductAlternatives();
        }
    }

    /// <summary>
    /// Generates default product alternatives when actual data is unavailable.
    /// </summary>
    public static List<ProductAlternative> GenerateDefaultProductAlternatives()
    {
        return
        [
            new ProductAlternative
            {
                Name = "Alternative Product A",
                Sku = "ALT-001",
                Price = 89.99m,
                InStock = true,
                Location = "Aisle 5",
                Aisle = 5,
                Section = "B"
            },
            new ProductAlternative
            {
                Name = "Alternative Product B",
                Sku = "ALT-002",
                Price = 49.99m,
                InStock = true,
                Location = "Aisle 8",
                Aisle = 8,
                Section = "C"
            }
        ];
    }
}
