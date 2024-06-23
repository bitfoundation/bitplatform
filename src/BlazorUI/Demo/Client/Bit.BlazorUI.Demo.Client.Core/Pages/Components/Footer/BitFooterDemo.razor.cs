namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Footer;

public partial class BitFooterDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Gets or sets the content to be rendered inside the BitFooter.",
        },
        new()
        {
            Name = "Height",
            Type = "int?",
            DefaultValue = "50",
            Description = "Gets or sets the height of the BitFooter (in pixels).",
        },
        new()
        {
            Name = "Fixed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the footer with a fixed position at the bottom of the page.",
        }
    ];



    private readonly string example1RazorCode = @"
<BitFooter>I'm a Footer</BitFooter>";

}
