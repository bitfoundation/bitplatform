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

    private string GetPositionClass() => Position switch
    {
        BitTooltipPosition.TopLeft => $"tooltip-position-top-left",
        BitTooltipPosition.Top => $"tooltip-position-top",
        BitTooltipPosition.TopRight => $"tooltip-position-top-right",
        BitTooltipPosition.RightTop => $"tooltip-position-right-top",
        BitTooltipPosition.Right => $"tooltip-position-right",
        BitTooltipPosition.RightBottom => $"tooltip-position-right-bottom",
        BitTooltipPosition.BottomRight => $"tooltip-position-bottom-right",
        BitTooltipPosition.Bottom => $"tooltip-position-bottom",
        BitTooltipPosition.BottomLeft => $"tooltip-position-bottom-left",
        BitTooltipPosition.LeftBottom => $"tooltip-position-left-bottom",
        BitTooltipPosition.Left => $"tooltip-position-left",
        BitTooltipPosition.LeftTop => $"tooltip-position-left-top",

        _ => $"tooltip-position-top",
    };
}
