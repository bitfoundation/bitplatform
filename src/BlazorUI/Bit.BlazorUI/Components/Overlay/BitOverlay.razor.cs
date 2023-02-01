
namespace Bit.BlazorUI;

public partial class BitOverlay
{
    private bool IsVisibleHasBeenSet;
    private bool isVisible;

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public bool AbsolutePosition { get; set; }

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public bool AutoClose { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// 
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
    /// 
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
