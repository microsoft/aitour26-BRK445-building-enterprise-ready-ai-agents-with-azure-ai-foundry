using Microsoft.AspNetCore.Http;

namespace SharedEntities;

public class PhotoAnalysisRequest
{
    public IFormFile Image { get; set; } = null!;
    public string Prompt { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
}
