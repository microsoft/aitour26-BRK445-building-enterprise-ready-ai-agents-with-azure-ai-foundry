namespace SharedEntities;

public class ReasoningRequest
{
    public PhotoAnalysisResult PhotoAnalysis { get; set; } = new();
    public CustomerInformation Customer { get; set; } = new();
    public string Prompt { get; set; } = string.Empty;
}