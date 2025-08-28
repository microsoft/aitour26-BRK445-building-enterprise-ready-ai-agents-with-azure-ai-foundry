#pragma warning disable SKEXP0110

using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models;
using ZavaAIFoundrySKAgentsProvider;
using ZavaSemanticKernelProvider;

namespace ToolReasoningService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReasoningController : ControllerBase
{
    private readonly ILogger<ReasoningController> _logger;
    private readonly Kernel _kernel;
    private readonly AIFoundryAgentProvider _aIFoundryAgentProvider;
    private AzureAIAgent _agent;

    public ReasoningController(
        ILogger<ReasoningController> logger, 
        SemanticKernelProvider semanticKernelProvider,
        AIFoundryAgentProvider aIFoundryAgentProvider)
    {
        _logger = logger;
        _kernel = semanticKernelProvider.GetKernel();
        _aIFoundryAgentProvider = aIFoundryAgentProvider;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<string>> GenerateReasoningAsync([FromBody] ReasoningRequest request)
    {
        try
        {
            _logger.LogInformation("Generating reasoning for prompt: {Prompt}", request.Prompt);

            // Use Semantic Kernel for AI reasoning
            var reasoning = await GenerateDetailedReasoningWithAI(request);

            return Ok(reasoning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating reasoning");
            
            // Fallback to rule-based reasoning
            var fallbackReasoning = GenerateDetailedReasoning(request);
            return Ok(fallbackReasoning);
        }
    }

    private async Task<string> GenerateDetailedReasoningWithAI(ReasoningRequest request)
    {
        var reasoningPrompt = $@"
You are an expert DIY consultant. Based on the following information, provide detailed reasoning for tool recommendations:

**Project Task:** {request.Prompt}
**Image Analysis:** {request.PhotoAnalysis.Description}
**Detected Materials:** {string.Join(", ", request.PhotoAnalysis.DetectedMaterials)}
**Customer's Existing Tools:** {string.Join(", ", request.Customer.OwnedTools)}
**Customer's Skills:** {string.Join(", ", request.Customer.Skills)}

Please provide:
1. A brief analysis of the project requirements
2. Assessment of the customer's current capabilities
3. Specific reasoning for each recommended tool
4. Safety considerations
5. Tips for success based on their skill level

Format your response with clear sections and be encouraging while being practical about safety and skill requirements.
";

        try
        {
            // Create a Semantic Kernel agent based on the agent definition
            var agentResponse = string.Empty;
            _agent = await _aIFoundryAgentProvider.GetAzureAIAgent();
            AzureAIAgentThread agentThread = new(_agent.Client);
            try
            {
                ChatMessageContent message = new(AuthorRole.User, reasoningPrompt);
                await foreach (ChatMessageContent response in _agent.InvokeAsync(message, agentThread))
                {
                    _logger.LogInformation("Received response from agent: {Content}", response.Content);    
                    agentResponse += (response.Content);
                }
            }
            finally
            {
                // Clean up the agent thread to avoid resource leaks
                // await agentThread.DeleteAsync();
            }
            return agentResponse ?? GenerateDetailedReasoning(request);
        }
        catch
        {
            return GenerateDetailedReasoning(request);
        }
    }

    private string GenerateDetailedReasoning(ReasoningRequest request)
    {
        var promptLower = request.Prompt.ToLower();
        var materials = request.PhotoAnalysis.DetectedMaterials;
        var ownedTools = request.Customer.OwnedTools;
        var skills = request.Customer.Skills;

        var reasoning = $"Based on your project '{request.Prompt}' and analysis of the uploaded image, here's my reasoning for tool recommendations:\n\n";

        // Analyze the task
        reasoning += "**Task Analysis:**\n";
        reasoning += $"- The image analysis reveals: {request.PhotoAnalysis.Description}\n";
        reasoning += $"- Detected materials that need attention: {string.Join(", ", materials)}\n\n";

        // Analyze customer profile
        reasoning += "**Your Profile:**\n";
        reasoning += $"- Available tools: {string.Join(", ", ownedTools)}\n";
        reasoning += $"- Skill level: {string.Join(", ", skills)}\n\n";

        // Provide specific reasoning
        reasoning += "**Recommendations:**\n";

        if (promptLower.Contains("paint"))
        {
            reasoning += "- For painting projects, you'll need specialized application tools. Your existing tools like measuring tape and screwdriver will be useful for preparation work.\n";
            reasoning += "- A paint roller will provide even coverage on large surfaces, while brushes are essential for detail work and edges.\n";
            reasoning += "- Drop cloths are crucial to protect surrounding areas from paint splatter.\n";
        }
        else if (promptLower.Contains("wood"))
        {
            reasoning += "- Woodworking requires precision cutting tools. If you don't have a saw, this will be essential for accurate cuts.\n";
            reasoning += "- Wood stain or finish will protect and enhance the appearance of your wood project.\n";
            reasoning += "- Your measuring tools will be critical for accurate measurements.\n";
        }
        else if (promptLower.Contains("tile"))
        {
            reasoning += "- Tile work requires specialized tools for cutting and setting tiles properly.\n";
            reasoning += "- Proper adhesive and grout are essential for a durable installation.\n";
            reasoning += "- Spacers and levels ensure professional-looking results.\n";
        }
        else
        {
            reasoning += "- Based on the general nature of your project, I'm recommending safety equipment as a priority.\n";
            reasoning += "- Safety glasses and work gloves are essential for any DIY project.\n";
            reasoning += "- Your existing tools will handle most basic tasks, so we're focusing on safety and project-specific needs.\n";
        }

        reasoning += "\n**Safety Note:** Always prioritize safety equipment for any DIY project, regardless of your skill level.";

        return reasoning;
    }
}
