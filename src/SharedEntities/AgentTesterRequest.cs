using System.ComponentModel.DataAnnotations;

namespace SharedEntities;

public class AgentTesterRequest
{
    [Required(ErrorMessage = "Please select an agent")]
    public string AgentId { get; set; } = string.Empty;

    public string AgentCnnStringId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a question")]
    [MinLength(3, ErrorMessage = "Question must be at least 3 characters long")]
    public string Question { get; set; } = string.Empty;
    
    public string UserId { get; set; } = "1"; // Default user ID
}
