namespace Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

public class ProjectAssistantReply
{
    public string Reply { get; set; } = "";

    public string Summary { get; set; } = "";

    public ProjectOptions Options { get; set; } = new();
}
