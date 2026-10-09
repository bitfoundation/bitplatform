using System.ComponentModel;

namespace Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

public class ProjectAssistantAnswer
{
    [Description("Your reply in Markdown: a few short sentences, then at most two short questions.")]
    public string Reply { get; set; } = "";

    [Description("A short running summary of everything the user said that matters for the project, ending with the questions you just asked.")]
    public string Summary { get; set; } = "";

    [Description("Every option the conversation has decided so far, each one once. Leave out the options nothing was said about.")]
    public List<ProjectOptionSetting> Options { get; set; } = [];
}
