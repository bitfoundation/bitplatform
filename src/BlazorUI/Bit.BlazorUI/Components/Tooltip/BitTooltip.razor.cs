using System.Text;

namespace Bit.BlazorUI;

public partial class BitTooltip
{
    private bool IsShownHasBeenSet;

    private bool isShown;
    private BitTooltipPosition tooltipPosition = BitTooltipPosition.Top;



    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Anchor { get; set; }

    /// <summary>
    /// The content inside of tooltip tag, It can be Any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTooltip.
    /// </summary>
    [Parameter] public BitTooltipClassStyles? Classes { get; set; }

    /// <summary>
    /// The visible state of the Tooltip.
    /// </summary>
    [Parameter]
    public bool IsShown
    {
        get => isShown;
        set
        {
            if (value == isShown) return;

            isShown = value;
            _ = IsShownChanged.InvokeAsync(isShown);
        }
    }

    [Parameter] public EventCallback<bool> IsShownChanged { get; set; }

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
    /// The content of tooltip, It can be any custom tag or a text
    /// </summary>
    [Parameter] public RenderFragment? Template { get; set; }

    /// <summary>
    /// The text of tooltip to show
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Determines on which events the tooltip will act
    /// </summary>
    [Parameter] public bool ShowOnHover { get; set; } = true;

    /// <summary>
    /// Determines on which events the tooltip will act
    /// </summary>
    [Parameter] public bool ShowOnFocus { get; set; } = true;

    /// <summary>
    /// Determines on which events the tooltip will act
    /// </summary>
    [Parameter] public bool ShowOnClick { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitTooltip.
    /// </summary>
    [Parameter] public BitTooltipClassStyles? Styles { get; set; }


    protected override string RootElementClass => "bit-ttp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }



    private void HandleMouseEnter()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;

        if (ShowOnHover)
        {
            IsShown = true;
        }
    }

    private void HandleMouseLeave()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;
        if (ShowOnHover == false) return;

        IsShown = false;
    }

    private void HandleFocusIn()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;

        if (ShowOnFocus)
        {
            IsShown = true;
        }
    }

    private void HandleFocusOut()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;
        if (ShowOnFocus == false) return;

        IsShown = false;
    }

    private void HandleMouseUp()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;

        if (ShowOnClick)
        {
            IsShown = !IsShown;
        }
    }

    private string GetTooltipClasses()
    {
        StringBuilder className = new StringBuilder();

        className.Append(IsShown ? "bit-ttp-vis" : string.Empty);

        className.Append(' ').Append(Position switch
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
            _ => "bit-ttp-top"
        });

        return className.ToString();
    }
}
