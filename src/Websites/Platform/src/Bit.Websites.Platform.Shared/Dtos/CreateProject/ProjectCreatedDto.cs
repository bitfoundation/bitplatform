namespace Bit.Websites.Platform.Shared.Dtos.CreateProject;

public class ProjectCreatedDto
{
    [Required, MaxLength(2000)]
    public string? Command { get; set; }
}