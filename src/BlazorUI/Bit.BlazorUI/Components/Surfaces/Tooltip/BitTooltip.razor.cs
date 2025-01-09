using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Tooltip component briefly describes unlabeled controls or provides a bit of additional information about labeled controls.
/// </summary>
public partial class BitTooltip : BitComponentBase
{
    private CancellationTokenSource? _showDelayTokenSource = new();
    private CancellationTokenSource? _hideDelayTokenSource = new();



    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Anchor { get; set; }

    /// <summary>
    /// The content inside of tooltip tag, It can be Any custom tag or a text.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitTooltip.
    /// </summary>
    [Parameter] public BitTooltipClassStyles? Classes { get; set; }

    /// <summary>
    /// Default value of the IsShown.
    /// </summary>
    [Parameter] public bool? DefaultIsShown { get; set; }

    /// <summary>
    /// Hides the arrow of tooltip.
    /// </summary>
    [Parameter] public bool HideArrow { get; set; }

    /// <summary>
    /// Delay (in milliseconds) before hiding the tooltip.
    /// </summary>
    [Parameter] public int HideDelay { get; set; } = 0;

    /// <summary>
    /// The visibility state of the tooltip.
    /// </summary>
    [Parameter, TwoWayBound]
    public bool IsShown { get; set; }

    /// <summary>
    /// The position of tooltip around its anchor.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitTooltipPosition Position { get; set; }

    /// <summary>
    /// The content you want inside the tooltip.
    /// </summary>
    [Parameter] public RenderFragment? Template { get; set; }

    /// <summary>
    /// The text of tooltip to show.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// Determines shows tooltip on click.
    /// </summary>
    [Parameter] public bool ShowOnClick { get; set; }

    /// <summary>
    /// Delay (in milliseconds) before showing the tooltip.
    /// </summary>
    [Parameter] public int ShowDelay { get; set; } = 0;

    /// <summary>
    /// Determines shows tooltip on focus.
    /// </summary>
    [Parameter] public bool ShowOnFocus { get; set; }

    /// <summary>
    /// Determines shows tooltip on hover.
    /// </summary>
    [Parameter] public bool ShowOnHover { get; set; } = true;

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

    protected override async Task OnInitializedAsync()
    {
        if (IsShownHasBeenSet is false && DefaultIsShown.HasValue)
        {
            await AssignIsShown(DefaultIsShown.Value);
        }

        await base.OnInitializedAsync();
    }



    private async Task Show()
    {
        _showDelayTokenSource?.Cancel();
        _hideDelayTokenSource?.Cancel();

        if (ShowDelay > 0)
        {
            _showDelayTokenSource = new CancellationTokenSource();

            await Task.Delay(ShowDelay, _showDelayTokenSource.Token);

            _showDelayTokenSource.Dispose();
            _showDelayTokenSource = null;
        }

        await AssignIsShown(true);
    }

    private async Task Hide()
    {
        _hideDelayTokenSource?.Cancel();
        _showDelayTokenSource?.Cancel();

        if (HideDelay > 0)
        {
            _hideDelayTokenSource = new CancellationTokenSource();

            await Task.Delay(HideDelay, _hideDelayTokenSource.Token);

            _hideDelayTokenSource.Dispose();
            _hideDelayTokenSource = null;
        }

        await AssignIsShown(false);
    }

    private async Task HandlePointerEnter()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;
        if (ShowOnHover is false) return;

        await Show();
    }

    private async Task HandlePointerLeave()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;
        if (ShowOnHover is false) return;

        await Hide();
    }

    private async Task HandleFocusIn()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;
        if (ShowOnFocus is false) return;

        await Show();
    }

    private async Task HandleFocusOut()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;
        if (ShowOnFocus is false) return;

        await Hide();
    }

    private async Task HandlePointerUp()
    {
        if (IsShownHasBeenSet && IsShownChanged.HasDelegate is false) return;

        if (ShowOnClick)
        {
            if (IsShown)
            {
                await Hide();
            }
            else
            {
                await Show();
            }
        }
    }

    private string GetTooltipClasses()
    {
        StringBuilder className = new StringBuilder();

        className.Append(IsShown ? "bit-ttp-vis " : string.Empty);

        className.Append(Position switch
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
