using System.ComponentModel.DataAnnotations;

namespace SharedEntities;

public class SingleAgentAnalysisRequest
{
    [Required]
    public string Prompt { get; set; } = string.Empty;

    [Required]
    public string CustomerId { get; set; } = string.Empty;
    
    // For API calls, we'll use a different approach for file handling
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
    public string? ImageFileName { get; set; }
}