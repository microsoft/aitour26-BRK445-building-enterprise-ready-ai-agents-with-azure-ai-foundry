#pragma warning disable SKEXP0110
using Microsoft.AspNetCore.Mvc;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using SharedEntities;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NavigationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NavigationController : ControllerBase
{
    private readonly ILogger<NavigationController> _logger;
    private readonly AzureAIAgent _skAgent;
    private readonly AIAgent _agentFxAgent;
    private readonly IChatClient _chatClient;

    public NavigationController(
        ILogger<NavigationController> logger,
        AzureAIAgent skAgent,
        AIAgent agentFxAgent,
        IChatClient chatClient)
    {
        _logger = logger;
        _skAgent = skAgent;
        _agentFxAgent = agentFxAgent;
        _chatClient = chatClient;
    }

    [HttpPost("directions/llm")]
    public async Task<ActionResult<NavigationInstructions>> GenerateDirectionsLlmAsync([FromBody] DirectionsRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[LLM] Generating directions from {From} to {To}", FormatLocation(request.From), FormatLocation(request.To));

        return await GenerateDirectionsAsync(
            request,
            InvokeLlmAsync,
            "[LLM]",
            cancellationToken);
    }

    [HttpPost("directions/sk")]
    public async Task<ActionResult<NavigationInstructions>> GenerateDirectionsSkAsync([FromBody] DirectionsRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[SK] Generating directions from {From} to {To}", FormatLocation(request.From), FormatLocation(request.To));

        return await GenerateDirectionsAsync(
            request,
            InvokeSemanticKernelAsync,
            "[SK]",
            cancellationToken);
    }

    [HttpPost("directions/maf")]
    public async Task<ActionResult<NavigationInstructions>> GenerateDirectionsMAFAsync([FromBody] DirectionsRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[MAF] Generating directions from {From} to {To}", FormatLocation(request.From), FormatLocation(request.To));

        return await GenerateDirectionsAsync(
            request,
            InvokeAgentFrameworkAsync,
            "[MAF]",
            cancellationToken);
    }

    private async Task<ActionResult<NavigationInstructions>> GenerateDirectionsAsync(
        DirectionsRequest request,
        Func<string, CancellationToken, Task<string>> invokeAgentAsync,
        string logPrefix,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request payload is required.");
        }

        if (request.From is null || request.To is null)
        {
            return BadRequest("Both origin and destination locations are required.");
        }

        var prompt = BuildNavigationPrompt(request);

        try
        {
            var agentResponse = await invokeAgentAsync(prompt, cancellationToken);
            _logger.LogInformation("{Prefix} Raw agent response length: {Length}", logPrefix, agentResponse.Length);

            if (TryParseNavigationInstructions(agentResponse, out var result))
            {
                return Ok(result);
            }

            _logger.LogWarning("{Prefix} Unable to parse navigation response. Using fallback instructions. Raw: {Raw}", logPrefix, TrimForLog(agentResponse));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Prefix} Agent invocation failed. Using fallback instructions.", logPrefix);
        }

        return Ok(BuildFallbackInstructions(request));
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "NavigationService" });
    }

    private async Task<string> InvokeLlmAsync(string prompt, CancellationToken cancellationToken)
    {
        var response = await _chatClient.GetResponseAsync(prompt, cancellationToken: cancellationToken);
        return response.Text ?? string.Empty;
    }

    private async Task<string> InvokeSemanticKernelAsync(string prompt, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        AzureAIAgentThread agentThread = new(_skAgent.Client);

        ChatMessageContent message = new(AuthorRole.User, prompt);
        await foreach (ChatMessageContent response in _skAgent.InvokeAsync(message, agentThread).WithCancellation(cancellationToken))
        {
            sb.Append(response.Content);
        }

        return sb.ToString();
    }

    private async Task<string> InvokeAgentFrameworkAsync(string prompt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var thread = _agentFxAgent.GetNewThread();
        var response = await _agentFxAgent.RunAsync(prompt, thread);
        return response?.Text ?? string.Empty;
    }

    private NavigationInstructions BuildFallbackInstructions(DirectionsRequest request) => new()
    {
        StartLocation = FormatLocation(request.From),
        EstimatedTime = "Approximately 2-3 minutes",
        Steps = GenerateNavigationSteps(request.From, request.To)
    };

    private NavigationStep[] GenerateNavigationSteps(Location from, Location to)
    {
        var steps = new List<NavigationStep>
        {
            new()
            {
                Direction = "Start",
                Description = $"Begin at {FormatLocation(from)}",
                Landmark = new NavigationLandmark { Location = CloneLocation(from) }
            }
        };

        if (!LocationsAreEqual(from, to))
        {
            steps.Add(new NavigationStep
            {
                Direction = "Walk Forward",
                Description = "Proceed down the main aisle for approximately 20 meters"
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
                Description = $"Continue straight until you reach the section near {FormatLocation(to)}"
            });
        }

        steps.Add(new NavigationStep
        {
            Direction = "Arrive",
            Description = $"You have arrived at {FormatLocation(to)}",
            Landmark = new NavigationLandmark { Location = CloneLocation(to) }
        });

        return steps.ToArray();
    }

    #region JSON & utility helpers

    private static string BuildNavigationPrompt(DirectionsRequest request) => @$"
You are an indoor navigation assistant for a hardware store. Provide concise step-by-step directions.

Return a JSON object with this shape:
{{
    ""startLocation"": string,
    ""estimatedTime"": string,
    ""steps"": [
        {{
            ""direction"": string,
            ""description"": string,
            ""landmark""?: {{
                ""description""?: string,
                ""location""?: {{ ""lat"": number, ""lon"": number }}
            }}
        }}
    ]
}}

Make sure at least three steps are included, ending with an arrival instruction.

Starting point coordinates: {request.From.Lat:F4}, {request.From.Lon:F4}
Destination coordinates: {request.To.Lat:F4}, {request.To.Lon:F4}
";

    private static bool TryParseNavigationInstructions(string agentResponse, out NavigationInstructions instructions)
    {
        instructions = default!;
        if (string.IsNullOrWhiteSpace(agentResponse))
        {
            return false;
        }

        var json = ExtractFirstJsonObject(agentResponse);
        if (json is null)
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var steps = root.TryGetProperty("steps", out var stepsElement)
                ? ParseSteps(stepsElement)
                : Array.Empty<NavigationStep>();

            if (steps.Length == 0)
            {
                return false;
            }

            var startLocation = root.TryGetProperty("startLocation", out var startProp) && startProp.ValueKind == JsonValueKind.String
                ? startProp.GetString() ?? string.Empty
                : string.Empty;

            var estimatedTime = root.TryGetProperty("estimatedTime", out var timeProp) && timeProp.ValueKind == JsonValueKind.String
                ? timeProp.GetString() ?? string.Empty
                : string.Empty;

            instructions = new NavigationInstructions
            {
                StartLocation = string.IsNullOrWhiteSpace(startLocation) ? "Store Entrance" : startLocation,
                EstimatedTime = estimatedTime,
                Steps = steps
            };

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static NavigationStep[] ParseSteps(JsonElement stepsElement)
    {
        if (stepsElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<NavigationStep>();
        }

        var steps = new List<NavigationStep>();

        foreach (var item in stepsElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var direction = item.TryGetProperty("direction", out var dirProp) && dirProp.ValueKind == JsonValueKind.String
                ? dirProp.GetString() ?? string.Empty
                : string.Empty;

            var description = item.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String
                ? descProp.GetString() ?? string.Empty
                : string.Empty;

            if (string.IsNullOrWhiteSpace(direction) && string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            var step = new NavigationStep
            {
                Direction = direction,
                Description = description
            };

            if (item.TryGetProperty("landmark", out var landmarkProp) && landmarkProp.ValueKind == JsonValueKind.Object)
            {
                var landmark = ParseLandmark(landmarkProp);
                if (landmark is not null)
                {
                    step.Landmark = landmark;
                }
            }

            steps.Add(step);
        }

        return steps.ToArray();
    }

    private static NavigationLandmark? ParseLandmark(JsonElement landmarkElement)
    {
        string? description = null;
        Location? location = null;

        if (landmarkElement.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String)
        {
            description = descProp.GetString();
        }

        if (landmarkElement.TryGetProperty("location", out var locProp) && locProp.ValueKind == JsonValueKind.Object)
        {
            if (TryGetDouble(locProp, "lat", out var lat) && TryGetDouble(locProp, "lon", out var lon))
            {
                location = new Location { Lat = lat, Lon = lon };
            }
        }

        if (description is null && location is null)
        {
            return null;
        }

        return new NavigationLandmark
        {
            Description = description,
            Location = location
        };
    }

    private static bool TryGetDouble(JsonElement parent, string propertyName, out double value)
    {
        value = default;
        if (!parent.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        return prop.TryGetDouble(out value);
    }

    private static string? ExtractFirstJsonObject(string input)
    {
        var start = input.IndexOf('{');
        if (start < 0)
        {
            return null;
        }

        var depth = 0;
        for (var i = start; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '{')
            {
                depth++;
            }
            else if (c == '}')
            {
                depth--;
            }

            if (depth == 0)
            {
                return input.Substring(start, i - start + 1).Trim();
            }
        }

        return null;
    }

    private static string TrimForLog(string value, int maxLength = 400)
        => value.Length <= maxLength ? value : value[..maxLength] + "...";

    private static bool LocationsAreEqual(Location left, Location right)
        => Math.Abs(left.Lat - right.Lat) < 0.0001 && Math.Abs(left.Lon - right.Lon) < 0.0001;

    private static Location CloneLocation(Location source) => new() { Lat = source.Lat, Lon = source.Lon };

    private static string FormatLocation(Location? location)
        => location is null ? "Unknown" : $"({location.Lat:F4}, {location.Lon:F4})";

    #endregion
}

public class DirectionsRequest
{
    public Location From { get; set; } = null!;
    public Location To { get; set; } = null!;
}