namespace Bit.BlazorUI;

public partial class BitTooltip
{
    private string _positionClass = string.Empty;

    /// <summary>
    /// The position of tooltip around its anchor
    /// </summary>
    [Parameter] public BitTooltipPosition Position { get; set; }

    /// <summary>
    /// The text of tooltip to show
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The content of tooltip, It can be any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? TextFragment { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override string RootElementClass => "bit-ttp";

    protected override Task OnParametersSetAsync()
    {
        _positionClass = GetPositionClass();

        return base.OnParametersSetAsync();
    }

    private string GetPositionClass() => Position switch
    {
        BitTooltipPosition.TopLeft => $"top-left",
        BitTooltipPosition.Top => $"top",
        BitTooltipPosition.TopRight => $"top-right",
        BitTooltipPosition.RightTop => $"right-top",
        BitTooltipPosition.Right => $"right",
        BitTooltipPosition.RightBottom => $"right-bottom",
        BitTooltipPosition.BottomRight => $"bottom-right",
        BitTooltipPosition.Bottom => $"bottom",
        BitTooltipPosition.BottomLeft => $"bottom-left",
        BitTooltipPosition.LeftBottom => $"left-bottom",
        BitTooltipPosition.Left => $"left",
        BitTooltipPosition.LeftTop => $"left-top",

        _ => $"top",
    };
}
