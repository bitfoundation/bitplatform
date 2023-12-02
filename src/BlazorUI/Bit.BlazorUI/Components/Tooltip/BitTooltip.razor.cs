namespace Bit.BlazorUI;

public partial class BitTooltip
{
    private BitTooltipPosition tooltipPosition = BitTooltipPosition.Top;


    /// <summary>
    /// The position of tooltip around its anchor
    /// </summary>
    [Parameter] 
    public BitTooltipPosition Position
    {
        get => tooltipPosition;
        set
        {
            if (tooltipPosition == value) return;

            tooltipPosition = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// The text of tooltip to show
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The content of tooltip, It can be any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? Template { get; set; }

    /// <summary>
    /// The content of inside of tooltip, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Anchor { get; set; }

    protected override string RootElementClass => "bit-ttp";


    private string GetTooltipPositionClasses() => Position switch
    {
        BitTooltipPosition.Top => "bit-ttp-top",
        BitTooltipPosition.TopLeft => "bit-ttp-tlf",
        BitTooltipPosition.TopRight => "bit-ttp-trg",
        BitTooltipPosition.RightTop => "bit-ttp-rtp",
        BitTooltipPosition.Right => "bit-ttp-rgt",
        BitTooltipPosition.RightBottom => "bit-ttp-rbm",
        BitTooltipPosition.BottomRight => "bit-ttp-brg",
        BitTooltipPosition.Bottom => "bit-ttp-btm",
        BitTooltipPosition.BottomLeft => "bit-ttp-blf",
        BitTooltipPosition.LeftBottom => "bit-ttp-lbm",
        BitTooltipPosition.Left => "bit-ttp-lft",
        BitTooltipPosition.LeftTop => "bit-ttp-ltp",
        _ => "bit-ttp-top",
    };
}
