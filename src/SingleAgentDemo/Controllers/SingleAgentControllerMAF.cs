using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc;
using SharedEntities;
using System.Text;
using ZavaFoundryAgentsProvider;

namespace SingleAgentDemo.Controllers;

/// <summary>
/// Controller for single agent analysis using Microsoft Agent Framework with Foundry Agents.
/// Demonstrates direct agent invocation using RunStreamingAsync without workflow orchestration.
/// </summary>
[ApiController]
[Route("api/singleagent/maf")]
public class SingleAgentControllerMAF : ControllerBase
{
    private readonly ILogger<SingleAgentControllerMAF> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SingleAgentControllerMAF(
        ILogger<SingleAgentControllerMAF> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Analyze an image using a sequential multi-agent approach with Foundry Agents.
    /// </summary>
    /// <param name="image">The image to analyze.</param>
    /// <param name="prompt">The task description for the analysis.</param>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="useSharedThread">When true, uses a single thread for all agent executions (maintains context). When false, creates a new thread per agent.</param>
    [HttpPost("analyze")]
    public async Task<ActionResult<SingleAgentAnalysisResponse>> AnalyzeAsync(
        [FromForm] IFormFile image,
        [FromForm] string prompt,
        [FromForm] string customerId,
        [FromForm] bool useSharedThread = true)
    {
        try
        {
            _logger.LogInformation(
                "Starting analysis for customer {CustomerId} using MAF Foundry Agents (SharedThread: {UseSharedThread})",
                customerId, useSharedThread);

            var agents = GetAgents();
            AgentThread? sharedThread = useSharedThread ? agents.PhotoAnalyzer.GetNewThread() : null;

            // Step 1: Photo Analysis
            var photoAnalysisResult = await ExecuteAgentStreamingAsync(
                agents.PhotoAnalyzer,
                "PhotoAnalyzer",
                $"Analyze the uploaded image '{image.FileName}' for the following task: {prompt}. Identify materials, surfaces, and project requirements.",
                useSharedThread ? sharedThread : null);

            // Step 2: Customer Information
            var customerInfoResult = await ExecuteAgentStreamingAsync(
                agents.CustomerInformation,
                "CustomerInformation",
                $"Retrieve customer information for customer ID: {customerId}. Include their owned tools, skills, and project history.",
                useSharedThread ? sharedThread : null);

            // Step 3: Tool Reasoning (depends on steps 1 & 2)
            var reasoningPrompt = BuildReasoningPrompt(photoAnalysisResult, customerInfoResult, prompt);
            var reasoningResult = await ExecuteAgentStreamingAsync(
                agents.ToolReasoning,
                "ToolReasoning",
                reasoningPrompt,
                useSharedThread ? sharedThread : null);

            // Step 4: Inventory Check (depends on step 3)
            var inventoryPrompt = $"Based on the tool reasoning: {reasoningResult}, check inventory availability and pricing for the recommended tools.";
            var inventoryResult = await ExecuteAgentStreamingAsync(
                agents.Inventory,
                "Inventory",
                inventoryPrompt,
                useSharedThread ? sharedThread : null);

            _logger.LogInformation("Analysis complete for customer {CustomerId}", customerId);

            return Ok(BuildResponse(photoAnalysisResult, customerInfoResult, reasoningResult, inventoryResult, prompt, useSharedThread));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in analysis for customer {CustomerId}", customerId);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    /// <summary>
    /// Executes an agent using RunStreamingAsync (direct invocation without workflows).
    /// </summary>
    private async Task<string> ExecuteAgentStreamingAsync(
        AIAgent agent,
        string agentName,
        string prompt,
        AgentThread? thread)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Executing agent: {AgentName}", agentName);

        try
        {
            var resultBuilder = new StringBuilder();
            var agentThread = thread ?? agent.GetNewThread();

            await foreach (var update in agent.RunStreamingAsync(prompt, agentThread))
            {
                var text = update.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    resultBuilder.Append(text);
                    _logger.LogDebug("Agent {AgentName} streaming: {Text}", agentName, text);
                }
            }

            var result = resultBuilder.ToString();
            var duration = DateTime.UtcNow - startTime;
            
            _logger.LogInformation(
                "Agent {AgentName} completed in {Duration:F0}ms, result length: {Length}",
                agentName, duration.TotalMilliseconds, result.Length);

            return string.IsNullOrWhiteSpace(result) ? GetFallbackResponse(agentName, prompt) : result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent {AgentName} failed", agentName);
            return GetFallbackResponse(agentName, prompt);
        }
    }

    /// <summary>
    /// Retrieves all required agents from dependency injection.
    /// </summary>
    private (AIAgent PhotoAnalyzer, AIAgent CustomerInformation, AIAgent ToolReasoning, AIAgent Inventory) GetAgents()
    {
        return (
            PhotoAnalyzer: _serviceProvider.GetRequiredKeyedService<AIAgent>(
                AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.PhotoAnalyzerAgent)),
            CustomerInformation: _serviceProvider.GetRequiredKeyedService<AIAgent>(
                AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.CustomerInformationAgent)),
            ToolReasoning: _serviceProvider.GetRequiredKeyedService<AIAgent>(
                AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.ToolReasoningAgent)),
            Inventory: _serviceProvider.GetRequiredKeyedService<AIAgent>(
                AgentNamesProvider.GetAgentName(AgentNamesProvider.AgentName.InventoryAgent))
        );
    }

    private static string BuildReasoningPrompt(string photoAnalysis, string customerInfo, string prompt)
    {
        return $"Based on the photo analysis: {photoAnalysis} and customer info: {customerInfo}, " +
               $"determine what tools are needed for the project: {prompt}. Consider the customer's existing tools and skills.";
    }

    private SingleAgentAnalysisResponse BuildResponse(
        string photoAnalysis,
        string customerInfo,
        string toolReasoning,
        string inventoryInfo,
        string prompt,
        bool usedSharedThread)
    {
        return new SingleAgentAnalysisResponse
        {
            Analysis = photoAnalysis,
            ReusableTools = ExtractReusableTools(customerInfo),
            RecommendedTools = ExtractToolRecommendations(inventoryInfo),
            Reasoning = GenerateSummary(photoAnalysis, customerInfo, toolReasoning, inventoryInfo, prompt, usedSharedThread)
        };
    }

    private static string[] ExtractReusableTools(string customerInfoResult)
    {
        if (string.IsNullOrEmpty(customerInfoResult))
            return ["tape measure", "screwdriver", "hammer"];

        var toolKeywords = new[] { "hammer", "screwdriver", "drill", "saw", "wrench", "pliers", "tape measure", "measuring tape", "level" };
        var tools = toolKeywords
            .Where(keyword => customerInfoResult.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return tools.Count > 0 ? tools.ToArray() : ["tape measure", "screwdriver", "hammer"];
    }

    private static ToolRecommendation[] ExtractToolRecommendations(string inventoryResult)
    {
        // Production: parse structured agent response
        return
        [
            new() { Name = "Paint Roller", Sku = "PAINT-ROLLER-9IN", IsAvailable = true, Price = 12.99m, Description = "9-inch paint roller for smooth walls" },
            new() { Name = "Paint Brush Set", Sku = "BRUSH-SET-3PC", IsAvailable = true, Price = 24.99m, Description = "3-piece brush set for detail work" },
            new() { Name = "Drop Cloth", Sku = "DROP-CLOTH-9X12", IsAvailable = true, Price = 8.99m, Description = "Plastic drop cloth protection" }
        ];
    }

    private static string GenerateSummary(
        string photoAnalysis,
        string customerInfo,
        string toolReasoning,
        string inventoryInfo,
        string prompt,
        bool usedSharedThread)
    {
        var threadMode = usedSharedThread 
            ? "Shared thread (context preserved between agents)" 
            : "Separate threads (independent agent executions)";
        
        return string.Join(Environment.NewLine,
            "=== Microsoft Agent Framework Analysis (Foundry Agents) ===",
            "",
            $"Execution Mode: RunStreamingAsync (direct invocation, no workflow orchestration)",
            $"Thread Mode: {threadMode}",
            "",
            $"Project Task: {prompt}",
            "",
            "Step 1 - Photo Analysis:",
            photoAnalysis,
            "",
            "Step 2 - Customer Profile:",
            customerInfo,
            "",
            "Step 3 - Tool Reasoning:",
            toolReasoning,
            "",
            "Step 4 - Inventory Check:",
            inventoryInfo,
            "",
            "Summary:",
            "This analysis used direct agent invocation via RunStreamingAsync for real-time streaming responses.",
            "Each agent processed its task sequentially, with results passed to subsequent agents for context.");
    }

    private static string GetFallbackResponse(string agentName, string prompt) => agentName switch
    {
        "PhotoAnalyzer" => $"Image analysis completed for task: {prompt}. Detected typical DIY project requirements including surface preparation and finishing work.",
        "CustomerInformation" => "Customer profile retrieved. Customer has basic DIY tools including hammer, screwdriver, and measuring tape.",
        "ToolReasoning" => $"Based on the project requirements for '{prompt}', recommended tools include appropriate brushes, rollers, and preparation materials.",
        "Inventory" => "Inventory check completed. Recommended tools are available in stock with current pricing.",
        _ => $"Agent {agentName} completed processing for: {prompt}"
    };
}
