namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Header;

public partial class BitHeaderDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Gets or sets the content to be rendered inside the BitHeader.",
        },
        new()
        {
            Name = "Height",
            Type = "int?",
            DefaultValue = "50",
            Description = "Gets or sets the height of the BitHeader (in pixels).",
        },
        new()
        {
            Name = "Fixed",
            Type = "bool",
            DefaultValue = "false",
            Description = "Renders the header with a fixed position at the top of the page.",
        }
    ];



    private readonly string example1RazorCode = @"
<BitHeader>I'm a Header</BitHeader>";

}
