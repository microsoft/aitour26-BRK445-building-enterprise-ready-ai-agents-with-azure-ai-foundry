namespace SharedEntities;

public class AgentStep
{
    public string Agent { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}