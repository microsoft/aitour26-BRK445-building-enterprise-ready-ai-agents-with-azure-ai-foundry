namespace SharedEntities;

public class MultiAgentResponse
{
    public string OrchestrationId { get; set; } = string.Empty;
    
    /// <summary>
    /// The type of orchestration that was used for this response.
    /// </summary>
    public OrchestrationType OrchestationType { get; set; } = OrchestrationType.Sequential;
    
    /// <summary>
    /// A description of how the orchestration was executed.
    /// </summary>
    public string OrchestrationDescription { get; set; } = string.Empty;
    
    public AgentStep[] Steps { get; set; } = Array.Empty<AgentStep>();
    public List<ProductAlternative> Alternatives { get; set; } = [];
    public NavigationInstructions? NavigationInstructions { get; set; }

    public string MermaidWorkflowRepresentation { get; set; } = string.Empty;
}