namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Spacer;

public partial class BitSpacerDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "Width",
            Type = "int?",
            DefaultValue = "null",
            Description = "Gets or sets the width of the spacer (in pixels).",
        },
    ];



    private readonly string example1RazorCode = @"
<BitSpacer>I'm a Spacer</BitSpacer>";

}
