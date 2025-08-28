namespace Shared.Models;

public class ToolMatchResult
{
    public string[] ReusableTools { get; set; } = Array.Empty<string>();
    public ToolRecommendation[] MissingTools { get; set; } = Array.Empty<ToolRecommendation>();
}
