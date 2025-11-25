namespace SingleAgentDemo.Models;

public class PhotoAnalysisRequest
{
    public IFormFile Image { get; set; } = null!;
    public string Prompt { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
}

public class PhotoAnalysisResult
{
    public string Description { get; set; } = string.Empty;
    public string[] DetectedMaterials { get; set; } = Array.Empty<string>();
}

public class CustomerInformation
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string[] OwnedTools { get; set; } = Array.Empty<string>();
    public string[] Skills { get; set; } = Array.Empty<string>();
}

public class ToolMatchRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string[] DetectedMaterials { get; set; } = Array.Empty<string>();
    public string Prompt { get; set; } = string.Empty;
}

public class ToolMatchResult
{
    public string[] ReusableTools { get; set; } = Array.Empty<string>();
    public InternalToolRecommendation[] MissingTools { get; set; } = Array.Empty<InternalToolRecommendation>();
}

public class InternalToolRecommendation
{
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class InventorySearchRequest
{
    public string[] Skus { get; set; } = Array.Empty<string>();
}

public class ReasoningRequest
{
    public PhotoAnalysisResult PhotoAnalysis { get; set; } = new();
    public CustomerInformation Customer { get; set; } = new();
    public string Prompt { get; set; } = string.Empty;
}