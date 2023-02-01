
namespace Bit.BlazorUI;

public partial class BitOverlay
{
    private bool IsVisibleHasBeenSet;
    private bool isVisible;
    private string zIndex = "999";
    private string color = "transparent";

    [Inject] public IJSRuntime _js { get; set; } = default!;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public bool AutoClose { get; set; } = true;

    /// <summary>
    /// 
    /// </summary>
    [Parameter] public string Color 
    { 
        get => color; 
        set
        {
            color = value;
            StyleBuilder.Reset();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Parameter]
    public string ZIndex
    {
        get => zIndex;
        set
        {
            zIndex = value;
            StyleBuilder.Reset();
        }
    }

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
            ToggleScroll();
            ClassBuilder.Reset();
        }
    }
    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }

    protected override string RootElementClass => "bit-ovl";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsVisible ? "visible" : "");
    }

    protected override void RegisterComponentStyles()
    {
        StyleBuilder.Register(() => $"background-color: {Color}");

        StyleBuilder.Register(() => $"z-index: {ZIndex}");
    }

    private async void ToggleScroll()
    {
        if (IsVisible)
        {
            await _js.InvokeVoidAsync("BitOverlay.hideScroll");
        }
        else
        {
            await _js.InvokeVoidAsync("BitOverlay.showScroll");
        }
    }

    private void CloseOverlay()
    {
        if (IsEnabled is false) return;
        if (AutoClose is false) return;
        if (IsVisibleHasBeenSet && IsVisibleChanged.HasDelegate is false) return;

        IsVisible = false;
    }
}
