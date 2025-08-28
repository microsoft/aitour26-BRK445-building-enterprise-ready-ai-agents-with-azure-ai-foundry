namespace SharedEntities;

public class MultiAgentRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ProductQuery { get; set; } = string.Empty;
    public Location? Location { get; set; }
    
    /// <summary>
    /// The type of orchestration to use for this request. Defaults to Sequential.
    /// </summary>
    public OrchestationType OrchestationType { get; set; } = OrchestationType.Sequential;
    
    // Image handling properties similar to SingleAgentAnalysisRequest
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
    public string? ImageFileName { get; set; }
}