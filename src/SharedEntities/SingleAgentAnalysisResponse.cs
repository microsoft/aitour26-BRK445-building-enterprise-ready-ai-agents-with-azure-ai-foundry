namespace SharedEntities;

public class SingleAgentAnalysisResponse
{
    public string Analysis { get; set; } = string.Empty;
    public string[] ReusableTools { get; set; } = Array.Empty<string>();
    public ToolRecommendation[] RecommendedTools { get; set; } = Array.Empty<ToolRecommendation>();
    public string Reasoning { get; set; } = string.Empty;
}