#pragma warning disable SKEXP0110
using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Models;
using System.Text.Json;
using ZavaAIFoundrySKAgentsProvider;
using ZavaSemanticKernelProvider;

namespace AnalyzePhotoService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PhotoAnalysisController : ControllerBase
{
    private readonly ILogger<PhotoAnalysisController> _logger;
    private readonly Kernel _kernel;
    private readonly AIFoundryAgentProvider _aIFoundryAgentProvider;
    private AzureAIAgent _agent;

    public PhotoAnalysisController(
        ILogger<PhotoAnalysisController> logger,
        SemanticKernelProvider semanticKernelProvider,
        AIFoundryAgentProvider aIFoundryAgentProvider)
    {
        _logger = logger;
        _kernel = semanticKernelProvider.GetKernel();
        _aIFoundryAgentProvider = aIFoundryAgentProvider;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<PhotoAnalysisResult>> AnalyzeAsync([FromForm] IFormFile image, [FromForm] string prompt)
    {
        try
        {
            _logger.LogInformation("Analyzing photo with prompt: {Prompt}", prompt);
            if (image == null)
            {
                return BadRequest("No image file was provided.");
            }

            // Build a prompt for the semantic kernel. Ask for a strict JSON object so we can parse it.
            var aiPrompt = $@"You are an AI assistant that analyzes photos of rooms for renovation and home-improvement projects.
Given the image filename and the user's short prompt, return a JSON object with exactly two fields:
  - description: a brief natural-language description of what the image shows and what renovation tasks are likely required
  - detectedMaterials: an array of short strings naming materials, finishes or items that appear relevant (e.g. 'paint', 'tile', 'wood', 'grout')

Return only valid JSON. Do not include any surrounding markdown or explanatory text.

ImageFileName: {image.FileName}
UserPrompt: {prompt}
";


            // Prepare a stable fallback result (same message used previously)
            var fallbackDescription = $"Photo analysis for prompt: '{prompt}'. Detected a room that needs renovation work. The image shows surfaces that require preparation and finishing.";

            //// If kernel isn't configured for some reason, use the fallback heuristic immediately
            //if (_kernel == null)
            //{
            //    _logger.LogWarning("Semantic Kernel is not available, using heuristic fallback");
            //    var fallback = new PhotoAnalysisResult
            //    {
            //        Description = fallbackDescription,
            //        DetectedMaterials = DetermineDetectedMaterials(prompt, image.FileName)
            //    };
            //    return Ok(fallback);
            //}


            try
            {
                //var aiResponse = await _kernel.InvokePromptAsync(aiPrompt);
                //var agentResponse = aiResponse.GetValue<string>() ?? string.Empty;

                // Create a Semantic Kernel agent based on the agent definition
                var agentResponse = string.Empty;
                _agent = await _aIFoundryAgentProvider.GetAzureAIAgent();
                AzureAIAgentThread agentThread = new(_agent.Client);
                await foreach (ChatMessageContent response in _agent.InvokeAsync(aiPrompt, agentThread))
                {
                    _logger.LogInformation("Received response from agent: {Content}", response.Content);
                    agentResponse += (response.Content);
                }

                // Try to extract a JSON object from the response in case the model returned extra text.
                var firstBrace = agentResponse.IndexOf('{');
                var lastBrace = agentResponse.LastIndexOf('}');
                string json = agentResponse;
                if (firstBrace >= 0 && lastBrace > firstBrace)
                {
                    json = agentResponse.Substring(firstBrace, lastBrace - firstBrace + 1);
                }

                try
                {
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    string description = string.Empty;
                    var materialsList = new List<string>();

                    if (root.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String)
                    {
                        description = descProp.GetString() ?? string.Empty;
                    }

                    if (root.TryGetProperty("detectedMaterials", out var materialsProp) && materialsProp.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in materialsProp.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                var s = item.GetString();
                                if (!string.IsNullOrWhiteSpace(s)) materialsList.Add(s!);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(description) && materialsList.Count > 0)
                    {
                        var result = new PhotoAnalysisResult
                        {
                            Description = description,
                            DetectedMaterials = materialsList.ToArray()
                        };

                        return Ok(result);
                    }
                    else
                    {
                        _logger.LogWarning("AI returned invalid or incomplete analysis, using heuristic fallback. Raw AI output: {Output}", agentResponse);
                        var fallback = new PhotoAnalysisResult
                        {
                            Description = fallbackDescription,
                            DetectedMaterials = DetermineDetectedMaterials(prompt, image.FileName)
                        };
                        return Ok(fallback);
                    }
                }
                catch (JsonException jex)
                {
                    _logger.LogWarning(jex, "Failed to parse AI JSON output, using heuristic fallback. Raw AI output: {Output}", agentResponse);
                    var fallback = new PhotoAnalysisResult
                    {
                        Description = fallbackDescription,
                        DetectedMaterials = DetermineDetectedMaterials(prompt, image.FileName)
                    };
                    return Ok(fallback);
                }
            }
            catch (Exception aiEx)
            {
                // If AI service fails for any reason, fall back to the simple rule-based detector and return success with that message
                _logger.LogWarning(aiEx, "Semantic Kernel invocation failed, using heuristic fallback");
                var fallback = new PhotoAnalysisResult
                {
                    Description = fallbackDescription,
                    DetectedMaterials = DetermineDetectedMaterials(prompt, image.FileName)
                };
                return Ok(fallback);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing photo");
            return StatusCode(500, "An error occurred while analyzing the photo");
        }
    }

    // Simple DTO used to deserialize the AI's JSON response.
    private record AiPhotoAnalysisResult(string Description, string[] DetectedMaterials);

    private string[] DetermineDetectedMaterials(string prompt, string? fileName)
    {
        var materials = new List<string>();

        // Simple keyword-based material detection
        var promptLower = prompt.ToLower();
        var fileNameLower = fileName?.ToLower() ?? "";

        if (promptLower.Contains("paint") || promptLower.Contains("wall"))
            materials.AddRange(new[] { "paint", "wall", "surface preparation" });

        if (promptLower.Contains("wood") || promptLower.Contains("deck"))
            materials.AddRange(new[] { "wood", "stain", "sanding" });

        if (promptLower.Contains("tile") || promptLower.Contains("bathroom"))
            materials.AddRange(new[] { "tile", "grout", "adhesive" });

        if (promptLower.Contains("garden") || promptLower.Contains("landscape"))
            materials.AddRange(new[] { "soil", "plants", "tools" });

        // Default materials if none detected
        if (materials.Count == 0)
            materials.AddRange(new[] { "general tools", "measuring", "safety equipment" });

        return materials.ToArray();
    }
}
