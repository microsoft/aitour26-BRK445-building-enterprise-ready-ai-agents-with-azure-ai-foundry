//namespace Shared.Models;

//public class PhotoAnalysisRequest
//{
//    public IFormFile Image { get; set; } = null!;
//    public string Prompt { get; set; } = string.Empty;
//}

//public class PhotoAnalysisResult
//{
//    public string Description { get; set; } = string.Empty;
//    public string[] DetectedMaterials { get; set; } = Array.Empty<string>();
//}

//public class CustomerInformation
//{
//    public string Id { get; set; } = string.Empty;
//    public string Name { get; set; } = string.Empty;
//    public string[] OwnedTools { get; set; } = Array.Empty<string>();
//    public string[] Skills { get; set; } = Array.Empty<string>();
//}

//public class ToolMatchRequest
//{
//    public string CustomerId { get; set; } = string.Empty;
//    public string[] DetectedMaterials { get; set; } = Array.Empty<string>();
//    public string Prompt { get; set; } = string.Empty;
//}

//public class ReasoningRequest
//{
//    public PhotoAnalysisResult PhotoAnalysis { get; set; } = new();
//    public CustomerInformation Customer { get; set; } = new();
//    public string Prompt { get; set; } = string.Empty;
//}