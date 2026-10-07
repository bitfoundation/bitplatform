namespace Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

public class ProjectAssistantRequest
{
    [Required, MaxLength(4000)]
    public string? Message { get; set; }

    [MaxLength(8000)]
    public string? Summary { get; set; }

    [MaxLength(4000)]
    public string? LastReply { get; set; }

    public ProjectOptions Options { get; set; } = new();
}
