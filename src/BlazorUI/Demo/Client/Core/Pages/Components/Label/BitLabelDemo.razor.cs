using Bit.BlazorUI.Demo.Client.Core.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Label;

public partial class BitLabelDemo
{
    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of label, It can be Any custom tag or a text.",
        },
        new()
        {
            Name = "For",
            Type = "string?",
            DefaultValue = "null",
            Description = "This attribute specifies which form element a label is bound to.",
        },
        new()
        {
            Name = "IsRequired",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the associated field is required or not, it shows a star above of it.",
        }
    };



    private readonly string example1HTMLCode = @"
<BitLabel>I'm a Label</BitLabel>
<BitLabel IsEnabled=""false"">I'm a disabled Label</BitLabel>
<BitLabel IsRequired=""true"">I'm a required Label</BitLabel>
<BitLabel For=""label-input"">A Label for An Input</BitLabel>
<input type=""text"" name=""label-input"" id=""label-input"" />
";
}
