using System.Collections.Generic;
using Bit.BlazorUI.Playground.Web.Pages.Components.ComponentDemoBase;

namespace Bit.BlazorUI.Playground.Web.Pages.Components.Loading;

public partial class BitLoadingDemo
{
    public string OnClickValue { get; set; } = string.Empty;

    private readonly List<ComponentParameter> componentParameters = new()
    {
        new()
        {
            Name = "Size",
            Type = "int",
            DefaultValue = "64",
            Description = "The Size of the loading component in px."
        },
        new()
        {
            Name = "Color",
            Type = "string",
            DefaultValue = "#FFFFFF",
            Description = "The Color of the loading component compatible with colors in CSS."
        }
    };

    private readonly string example1HTMLCode = @"
<BitBarsLoading Color=""royalblue"" Size=""64"" />
<BitCircleLoading Color=""royalblue"" Size=""64"" />
<BitDotsRingLoading Color=""royalblue"" Size=""64"" />
<BitDualRingLoading Color=""royalblue"" Size=""64"" />
<BitEllipsisLoading Color=""royalblue"" Size=""64"" />
<BitGridLoading Color=""royalblue"" Size=""64"" />
<BitHeartLoading Color=""royalblue"" Size=""64"" />
<BitHourglassLoading Color=""royalblue"" Size=""64"" />
<BitRingLoading Color=""royalblue"" Size=""64"" />
<BitRippleLoading Color=""royalblue"" Size=""64"" />
<BitRollerLoading Color=""royalblue"" Size=""64"" />
<BitSpinnerLoading Color=""royalblue"" Size=""64"" />
";
}
