namespace Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

public class ProjectCreatedDto
{
    [Required, MaxLength(2000)]
    public string? Command { get; set; }

    [MaxLength(8000)]
    public string? Summary { get; set; }
}
