namespace Bit.BlazorUI;

/// <summary>
/// The Overlay component is used to provide emphasis on a particular element or parts of it. It signals to the user of a state change within the application and can be used for creating loaders, dialogs and more.
/// </summary>
public partial class BitOverlay : BitComponentBase
{
    private int _offsetTop;
    private bool _internalIsOpen;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// When true, the scroll behavior of the Scroller element behind the overlay will be disabled.
    /// </summary>
    [Parameter] public bool AutoToggleScroll { get; set; }

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
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    /// When true, the Overlay will be closed by clicking on it.
    /// </summary>
    [Parameter] public bool NoAutoClose { get; set; }

    /// <summary>
    /// Callback that is called when the overlay is clicked.
    /// </summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Set the selector of the Selector element for the Overlay to disable its scroll if applicable.
    /// </summary>
    [Parameter] public string? ScrollerSelector { get; set; }



    protected override string RootElementClass => "bit-ovl";

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => _offsetTop > 0 ? FormattableString.Invariant($"top:{_offsetTop}px") : string.Empty);
    }

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => IsOpen ? "bit-ovl-opn" : string.Empty);
        ClassBuilder.Register(() => AbsolutePosition ? "bit-ovl-abs" : string.Empty);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_internalIsOpen == IsOpen) return;

        _internalIsOpen = IsOpen;

        _offsetTop = 0;

        if (AutoToggleScroll is false) return;

        var scrollerSelector = ScrollerSelector.HasValue() ? ScrollerSelector! : "body";

        _offsetTop = await _js.BitOverlayToggleScroll(scrollerSelector, IsOpen);

        if (AbsolutePosition is false) return;

        StyleBuilder.Reset();

        StateHasChanged();
    }



    private async Task CloseOverlay(MouseEventArgs e)
    {
        if (IsEnabled is false) return;

        await OnClick.InvokeAsync(e);

        if (NoAutoClose) return;

        await AssignIsOpen(false);
    }
}
