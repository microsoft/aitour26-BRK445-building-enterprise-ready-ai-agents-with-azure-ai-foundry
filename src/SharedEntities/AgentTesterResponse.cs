namespace SharedEntities;

public class AgentTesterResponse
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsSuccessful { get; set; } = true;
    public string? ErrorMessage { get; set; }
}
