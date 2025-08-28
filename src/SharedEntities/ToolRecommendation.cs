namespace SharedEntities;

public class ToolRecommendation
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}