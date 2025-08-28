namespace SharedEntities;

public class MultiAgentResponse
{
    public string OrchestrationId { get; set; } = string.Empty;
    
    /// <summary>
    /// The type of orchestration that was used for this response.
    /// </summary>
    public OrchestationType OrchestationType { get; set; } = OrchestationType.Sequential;
    
    /// <summary>
    /// A description of how the orchestration was executed.
    /// </summary>
    public string OrchestrationDescription { get; set; } = string.Empty;
    
    public AgentStep[] Steps { get; set; } = Array.Empty<AgentStep>();
    public ProductAlternative[] Alternatives { get; set; } = Array.Empty<ProductAlternative>();
    public NavigationInstructions? NavigationInstructions { get; set; }
}