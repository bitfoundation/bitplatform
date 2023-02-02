
namespace Bit.BlazorUI;

public partial class BitOverlay
{
    private bool IsVisibleHasBeenSet;
    private bool isVisible;

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// By default, it will be closed wherever the Overlay is clicked.
    /// </summary>
    [Parameter] public bool AutoClose { get; set; } = true;

    /// <summary>
    /// When the overlay is open, the element behind it cannot be scrolled, 
    /// and when the overlay is closed, it returns to its previous state.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; } = true;

    /// <summary>
    /// Set the Absolute Position style to Overlay when the Overlay is only for part of the page.
    /// </summary>
    [Parameter] public bool AbsolutePosition { get; set; }

    /// <summary>
    /// HTML content inside the Overlay.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Whether to display Overlay or not.
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
    /// Set specific element to toggle scroll on behind of Overlay.
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
