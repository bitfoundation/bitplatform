namespace Bit.BlazorUI;

public partial class BitOverlay : BitComponentBase
{
    private bool IsVisibleHasBeenSet;



    private int _offsetTop;
    private bool _internalIsVisible;



    [Inject] private IJSRuntime _js { get; set; } = default!;



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
    [Parameter, ResetClassBuilder]
    public bool AbsolutePosition { get; set; }

    /// <summary>
    /// The content of the Overlay.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// When true, the Overlay and its content will be shown.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool IsVisible { get; set; }

    [Parameter] public EventCallback<bool> IsVisibleChanged { get; set; }

    /// <summary>
    /// Set the selector of the Selector element for the Overlay to disable its scroll if applicable.
    /// </summary>
    [Parameter] public string ScrollerSelector { get; set; } = "body";



    protected override string RootElementClass => "bit-ovl";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => _offsetTop > 0 ? FormattableString.Invariant($"top:{_offsetTop}px") : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IsVisible ? "bit-ovl-vis" : string.Empty);
        ClassBuilder.Register(() => AbsolutePosition ? "bit-ovl-abs" : string.Empty);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsVisible == IsVisible) return;

        _internalIsVisible = IsVisible;

        _offsetTop = 0;

        if (AutoToggleScroll is false) return;

        _offsetTop = await _js.BitOverlayToggleScroll(ScrollerSelector, IsVisible);

        if (AbsolutePosition is false) return;

        StyleBuilder.Reset();

        StateHasChanged();
    }



    private void CloseOverlay()
    {
        if (IsEnabled is false) return;
        if (AutoClose is false) return;
        if (IsVisibleHasBeenSet && IsVisibleChanged.HasDelegate is false) return;

        IsVisible = false;
        ClassBuilder.Reset();
        _ = IsVisibleChanged.InvokeAsync(IsVisible);
    }
}
