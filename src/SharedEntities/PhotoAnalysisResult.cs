namespace SharedEntities;

public class PhotoAnalysisResult
{
    public string Description { get; set; } = string.Empty;
    public string[] DetectedMaterials { get; set; } = Array.Empty<string>();
}
