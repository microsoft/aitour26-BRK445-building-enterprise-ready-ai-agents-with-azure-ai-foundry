using Microsoft.Agents.AI;
using SharedEntities;

namespace MultiAgentDemo.Controllers;

public class StepsProcessor
{
    public static async Task<NavigationInstructions> GenerateNavigationInstructionsAsync(
        List<AgentStep> steps,
        AIAgent navigationAgent,
        Location? location,
        string productQuery,
        ILogger logger)
    {
        NavigationInstructions navigationInstructions = null;

        if (location == null)
        {
            location = new Location { Lat = 0, Lon = 0 };
        }

        try
        {
            // find the step where step.AgentId matches the _navigationAgent.Id
            var navigationStep = steps.FirstOrDefault(step => step.AgentId == navigationAgent.Id);
            var stepContent = navigationStep.Result;
            try
            {
                navigationInstructions = System.Text.Json.JsonSerializer.Deserialize<NavigationInstructions>(stepContent);
                if (navigationInstructions != null)
                {
                    logger.LogInformation("Navigation instructions found in step: {StepContent}", stepContent);
                    return navigationInstructions;
                }
            }
            catch
            {
                logger.LogWarning("Failed to deserialize navigation instructions from step: {StepContent}", stepContent);
            }

            // return default nav instructions
            return CreateDefaultNavigationInstructions(location, productQuery);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GenerateNavigationInstructions failed, returning fallback");
            return CreateDefaultNavigationInstructions(location, productQuery);
        }
    }

    public static NavigationInstructions CreateDefaultNavigationInstructions(Location location, string productQuery)
    {
        return new NavigationInstructions
        {
            Steps =[
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

    public static async Task<List<ProductAlternative>> GetProductAlternativesFromStepsAsync(
        List<AgentStep> steps,
        AIAgent productMatchmakingAgent,
        ILogger logger)
    {
        try
        {
            var alternatives = new List<ProductAlternative>();

            // find the step where step.AgentId matches the _navigationAgent.Id
            var alternativesStep = steps.FirstOrDefault(step => step.AgentId == productMatchmakingAgent.Id);
            var stepContent = alternativesStep.Result;
            try
            {
                alternatives = System.Text.Json.JsonSerializer.Deserialize<List<ProductAlternative>>(stepContent);
                if (alternatives != null)
                {
                    logger.LogInformation("Product Alternatives instructions found in step: {StepContent}", stepContent);
                    return alternatives;
                }
            }
            catch
            {
                logger.LogWarning("Failed to deserialize product alternatives instructions from step: {StepContent}", stepContent);
            }
            return GenerateDefaultProductAlternatives();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GenerateProductAlternatives failed, returning fallback alternatives");
            return GenerateDefaultProductAlternatives();
        }
    }

    public static List<ProductAlternative> GenerateDefaultProductAlternatives()
    {
        var alternatives = new List<ProductAlternative>
            {
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
            };
        return alternatives;
    }
}
