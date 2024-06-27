namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Label;

public partial class BitLabelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
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
            Name = "Required",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the associated field is required or not, it shows a star above of it.",
        }
    ];



    private readonly string example1RazorCode = @"
<BitLabel>I'm a Label</BitLabel>
<BitLabel IsEnabled=""false"">I'm a disabled Label</BitLabel>";

    private readonly string example2RazorCode = @"
Visible: [ <BitLabel Visibility=""BitVisibility.Visible"">Visible Label</BitLabel> ]
Hidden: [ <BitLabel Visibility=""BitVisibility.Hidden"">Hidden Label</BitLabel> ]
Collapsed: [ <BitLabel Visibility=""BitVisibility.Collapsed"">Collapsed Label</BitLabel> ]";

    private readonly string example3RazorCode = @"
<style>
    .custom-class {
        padding: 0.5rem;
        border: 1px solid red;
        max-width: max-content;
    }
</style>

<BitLabel Style=""color: dodgerblue; font-weight:bold"">I'm a Label with Style</BitLabel>
<BitLabel Class=""custom-class"">I'm a Label with Class</BitLabel>";

    private readonly string example4RazorCode = @"
<BitLabel Required>I'm a required Label</BitLabel>";

    private readonly string example5RazorCode = @"
<BitLabel For=""label-input"">A Label for An Input</BitLabel>
<input type=""text"" name=""label-input"" id=""label-input"" />";

    private readonly string example6RazorCode = @"
<BitLabel Dir=""BitDir.Rtl"">من یک برچسب هستم</BitLabel>";

}
