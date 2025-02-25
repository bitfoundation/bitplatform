namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.MediaQuery;

public partial class BitMediaQueryDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The content of the element.",
        },
        new()
        {
            Name = "Query",
            Type = "BitScreenQuery?",
            DefaultValue = "null",
            Description = "Specifies the media query to match for the child content.",
        }
    ];



    private string example1RazorCode = @"
<BitMediaQuery Query=""BitScreenQuery.Sm"">This is SM!</BitMediaQuery>";
}
