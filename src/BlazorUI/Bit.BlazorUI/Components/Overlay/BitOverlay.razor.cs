
namespace Bit.BlazorUI;

public partial class BitOverlay
{
    private bool IsVisibleHasBeenSet;
    private bool isVisible;

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// When true, the Overlay will be closed by clicking on it.
    /// </summary>
    [Parameter] public bool AutoClose { get; set; } = true;

    /// <summary>
    /// When true, the scroll behavior of the Scroller element behind the overlay will be disabled.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; } = true;

    /// <summary>
    /// When true, the Overlay will be positioned absolute instead of fixed.
    /// </summary>
    [Parameter] public bool AbsolutePosition { get; set; }

    /// <summary>
    /// The content of the Overlay.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// When true, the Overlay and its content will be shown.
    /// </summary>
    [Parameter]
    public bool IsVisible
    {
        get => isVisible;
        set
        {
            if (isVisible == value) return;
            isVisible = value;
            _ = IsVisibleChanged.InvokeAsync(value);

            ClassBuilder.Reset();
            
            if (AutoToggleScroll)
            {
                _js.ToggleScroll(ScrollerSelector, value);
            }
        }
    }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }

    /// <summary>
    /// Set the selector of the Selector element for the Overlay to disable its scroll if applicable.
    /// </summary>
    [Parameter] public string ScrollerSelector { get; set; } = "body";


    protected override string RootElementClass => "bit-ovl";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsVisible ? "visible" : "");

        ClassBuilder.Register(() => AbsolutePosition ? "absolute" : "");
    }

    private void CloseOverlay()
    {
        if (IsEnabled is false) return;
        if (AutoClose is false) return;
        if (IsVisibleHasBeenSet && IsVisibleChanged.HasDelegate is false) return;

        IsVisible = false;
    }
}
