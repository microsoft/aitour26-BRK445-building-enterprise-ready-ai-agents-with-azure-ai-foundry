using Microsoft.AspNetCore.Components.Forms;
using SharedEntities;
using System.ComponentModel.DataAnnotations;

namespace Store.Models;

public class MultiAgentRequestModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ProductQuery { get; set; } = string.Empty;

    public Location? Location { get; set; }
    
    /// <summary>
    /// The type of orchestration to use for this request. Defaults to Sequential.
    /// </summary>
    public OrchestrationType OrchestationType { get; set; } = OrchestrationType.Sequential;

    public IBrowserFile? Image { get; set; }

    // Method to convert to shared entity
    public async Task<MultiAgentRequest> ToSharedEntityAsync()
    {
        var request = new MultiAgentRequest
        {
            UserId = UserId,
            ProductQuery = ProductQuery,
            Location = Location,
            Orchestration = OrchestationType
        };

        if (Image != null)
        {
            using var stream = Image.OpenReadStream(5 * 1024 * 1024); // 5MB limit
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            request.ImageData = memoryStream.ToArray();
            request.ImageContentType = Image.ContentType;
            request.ImageFileName = Image.Name;
        }

        return request;
    }
}