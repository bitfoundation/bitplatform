using System.ComponentModel;

namespace Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

public class ProjectAssistantReply
{
    [Description("Your reply in Markdown: a few short sentences, then at most two short questions.")]
    public string Reply { get; set; } = "";

    [Description("A short running summary of everything the user said that matters for the project, ending with the questions you just asked.")]
    public string Summary { get; set; } = "";

    [Description("Every option the conversation has decided so far. Leave an option null when nothing was said about it.")]
    public ProjectOptions Options { get; set; } = new();
}
