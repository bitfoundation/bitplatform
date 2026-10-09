using System.ComponentModel;

namespace Bit.Websites.Platform.Shared.Dtos.ProjectAssistant;

public class ProjectOptionSetting
{
    [Description("The option's name as the instructions spell it, e.g. database or offlineDb")]
    public string Option { get; set; } = "";

    [Description("One of the option's values; true or false for an option that is on or off; for platforms, the platforms separated by commas, or none")]
    public string Value { get; set; } = "";
}
