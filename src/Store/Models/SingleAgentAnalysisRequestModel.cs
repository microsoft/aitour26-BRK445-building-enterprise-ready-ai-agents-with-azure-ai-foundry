using Microsoft.AspNetCore.Components.Forms;
using SharedEntities;
using System.ComponentModel.DataAnnotations;

namespace Store.Models;

public class SingleAgentAnalysisRequestModel
{
    public IBrowserFile Image { get; set; } = null!;

    [Required]
    public string Prompt { get; set; } = string.Empty;

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    // Method to convert to shared entity
    public async Task<SingleAgentAnalysisRequest> ToSharedEntityAsync()
    {
        using var stream = Image.OpenReadStream(5 * 1024 * 1024); // 5MB limit
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        return new SingleAgentAnalysisRequest
        {
            Prompt = Prompt,
            CustomerId = CustomerId,
            ImageData = memoryStream.ToArray(),
            ImageContentType = Image.ContentType,
            ImageFileName = Image.Name
        };
    }
}