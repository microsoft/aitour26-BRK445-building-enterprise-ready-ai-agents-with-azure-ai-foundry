namespace SharedEntities;

public class ToolMatchRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string[] DetectedMaterials { get; set; } = Array.Empty<string>();
    public string Prompt { get; set; } = string.Empty;
}
