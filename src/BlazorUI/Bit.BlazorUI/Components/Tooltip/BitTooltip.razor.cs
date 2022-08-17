using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bit.BlazorUI;

public partial class BitTooltip
{
    private bool isVisible;

    /// <summary>
    /// The position of tooltip around the its anchor
    /// </summary>
    [Parameter] public BitTooltipPosition Position { get; set; }

    /// <summary>
    /// The text of tooltip to show
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The content of tooltip, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? TextFragment { get; set; }

    /// <summary>
    /// The anchor content, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override string RootElementClass => "bit-ttp";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled is false
            ? $"{RootElementClass} tooltip-disabled-{VisualClassRegistrar()}" : string.Empty);
    }

    private string GetPositionClass() => Position switch
    {
        BitTooltipPosition.Top => $"tooltip-position-top",
        BitTooltipPosition.Bottom => $"tooltip-position-bottom",

        _ => $"tooltip-position-top",
    };
}
