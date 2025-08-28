namespace SharedEntities;

public class NavigationInstructions
{
    public string StartLocation { get; set; } = string.Empty;
    public NavigationStep[] Steps { get; set; } = Array.Empty<NavigationStep>();
    public string EstimatedTime { get; set; } = string.Empty;
}