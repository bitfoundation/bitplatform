using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// An image shown in a frame it can be zoomed and panned in: with the wheel (holding Ctrl, or Cmd), a pinch, a
/// double-click, the keyboard or the zoom buttons under it, and moved around by dragging it or with the arrow keys
/// while it is zoomed in.
/// </summary>
/// <remarks>
/// <see cref="Zoom"/> 1 is the image fitted in the frame - as large as fits, never larger than it is - and the zoom
/// runs from <see cref="MinZoom"/> to <see cref="MaxZoom"/> of that. The frame is a tab stop of its own: + and - zoom,
/// the arrow keys move a zoomed image, and 0 fits it again. A new <see cref="Src"/> starts fitted again.
/// <br />
/// <see cref="FullScreen"/> shows the viewer over the whole page, as a modal dialog a plain Escape or its close button
/// leaves - from <see cref="ShowFullScreenButton"/>'s button, from code, or, with <see cref="FullScreenOnly"/>, as a
/// lightbox the page opens from a thumbnail of its own.
/// </remarks>
public partial class BitImageViewer : BitComponentBase
{
    private static readonly BitImageViewerTexts _defaultTexts = new();



    [Inject] private IJSRuntime _js { get; set; } = default!;

    private ElementReference _viewportRef;
    private string _instructionsId = default!;
    private DotNetObjectReference<BitImageViewer>? _dotnetObj;
    private IJSObjectReference? _handle;

    // The zoom the script has, and the options it was last given, so that a render tells it only what changed - and a
    // zoom it reported itself is not sent back to it.
    private double _scriptZoom;
    private BitImageViewerJsOptions? _scriptOptions;

    // Whether the page is being held for a full screen viewer (the trap, the scroll lock and the focus to hand back),
    // which follows FullScreen a render behind: the hold is taken once the viewer is drawn over the page, and given back
    // once it is drawn in it again.
    private bool _heldFullScreen;

    // Set by the full screen button for the hold it starts: not every engine focuses a button it has just dispatched a
    // click for - WebKit leaves the focus where it was - and a focus that never reached the button is a focus that
    // cannot be handed back to it, so the button is focused first, which is what the other engines have already done.
    private ElementReference _fullScreenButtonRef;
    private bool _enteringFromButton;



    /// <summary>
    /// The default values of the parameters of the image viewer, cascaded through a <see cref="BitParams"/>. A value set
    /// on the image viewer itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitImageViewerParams.ParamName)]
    public BitImageViewerParams? CascadingParameters { get; set; }



    /// <summary>
    /// The alternative text of the image, which also names the viewer where it has no AriaLabel.
    /// </summary>
    [Parameter] public string? Alt { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the image viewer.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitImageViewerClassStyles? Classes { get; set; }

    /// <summary>
    /// The zoom a double-click (or a double tap) goes to, at the point it lands on, and back to fitted from any zoom
    /// above it. Default: 2.
    /// </summary>
    [Parameter] public double DoubleClickZoom { get; set; } = 2;

    /// <summary>
    /// Shows the viewer over the whole page, as a modal dialog: the focus is kept inside it, the page behind it holds
    /// still, the plain wheel zooms, and a plain Escape or the close button leaves it, handing the focus back to where
    /// it was. A zoom is kept across the change, of the frame it is fitted in each time.
    /// </summary>
    /// <remarks>
    /// The viewer is fixed to the viewport, so it covers the page only from where nothing around it makes a frame of its
    /// own for fixed elements (an ancestor with a transform, a filter or containment).
    /// </remarks>
    [Parameter, TwoWayBound, ResetClassBuilder, ResetStyleBuilder]
    public bool FullScreen { get; set; }

    /// <summary>
    /// Leaves the viewer out of the page until it is <see cref="FullScreen"/>, for a lightbox the page opens from a
    /// thumbnail of its own.
    /// </summary>
    [Parameter, ResetClassBuilder] public bool FullScreenOnly { get; set; }

    /// <summary>
    /// The height of the frame the image is shown in, as a CSS value (e.g. "24rem"). Default: the
    /// --bit-ImageViewer-height variable, else 20rem. A full screen viewer fills the screen instead.
    /// </summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>
    /// The largest zoom, as a multiple of the fitted image. Default: 8.
    /// </summary>
    [Parameter] public double MaxZoom { get; set; } = 8;

    /// <summary>
    /// The smallest zoom, as a multiple of the fitted image. Default: 1, the fitted image itself.
    /// </summary>
    [Parameter] public double MinZoom { get; set; } = 1;

    /// <summary>
    /// Leaves the zoom buttons out, for a viewer operated by the pointer and the keyboard alone.
    /// </summary>
    [Parameter] public bool NoToolbar { get; set; }

    /// <summary>
    /// Adds a button to the toolbar that shows the viewer <see cref="FullScreen"/>.
    /// </summary>
    [Parameter] public bool ShowFullScreenButton { get; set; }

    /// <summary>
    /// The URL of the image.
    /// </summary>
    [Parameter] public string? Src { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the image viewer.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitImageViewerClassStyles? Styles { get; set; }

    /// <summary>
    /// The words of the viewer: the names of its buttons and the instructions of its keyboard.
    /// </summary>
    [Parameter] public BitImageViewerTexts? Texts { get; set; }

    /// <summary>
    /// Zooms with the plain wheel too, not only with Ctrl (or Cmd) held. Off by default, since the wheel is what scrolls
    /// the page the viewer sits in; a viewer in a dialog has no page around it to scroll. A <see cref="FullScreen"/>
    /// viewer zooms with the plain wheel either way.
    /// </summary>
    [Parameter] public bool WheelZoom { get; set; }

    /// <summary>
    /// The zoom, as a multiple of the image fitted in the frame. Default: 1.
    /// </summary>
    [Parameter, TwoWayBound]
    public double Zoom { get; set; } = 1;

    /// <summary>
    /// The factor a step of the buttons and the + and - keys multiplies or divides the zoom by. Default: 1.25.
    /// </summary>
    [Parameter] public double ZoomFactor { get; set; } = 1.25;



    /// <summary>
    /// Zooms in by one step (<see cref="ZoomFactor"/>), around the center of the frame.
    /// </summary>
    public Task ZoomIn() => SetZoom(Zoom * StepFactor);

    /// <summary>
    /// Zooms out by one step (<see cref="ZoomFactor"/>), around the center of the frame.
    /// </summary>
    public Task ZoomOut() => SetZoom(Zoom / StepFactor);

    /// <summary>
    /// Fits the image in the frame again: zoom 1, centered.
    /// </summary>
    public Task Reset() => SetZoom(1);

    /// <summary>
    /// Shows the viewer over the whole page (<see cref="FullScreen"/>), unless it is disabled.
    /// </summary>
    public async Task EnterFullScreen()
    {
        if (Disabled) return;

        if (await AssignFullScreen(true))
        {
            await RenderAfterAssign();
        }
    }

    /// <summary>
    /// Shows the viewer back in the page, handing the focus back to where it was before it went full screen.
    /// </summary>
    public async Task ExitFullScreen()
    {
        if (await AssignFullScreen(false))
        {
            await RenderAfterAssign();
        }
    }

    private async Task ToggleFullScreen()
    {
        if (FullScreen)
        {
            await ExitFullScreen();
            return;
        }

        _enteringFromButton = true;
        await EnterFullScreen();
    }



    [JSInvokable("OnZoom")]
    public async Task _OnZoom(double zoom)
    {
        if (IsDisposed) return;

        _scriptZoom = zoom;

        // A Zoom the parent holds without a change callback stays where the parent holds it: the script is put back
        // there by the render that follows.
        await AssignZoom(zoom);

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-imv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => FullScreen ? $"bit-imv-fsc {Classes?.FullScreen}" : string.Empty);

        ClassBuilder.Register(() => FullScreenOnly ? "bit-imv-fso" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => FullScreen ? Styles?.FullScreen : null);
    }

    protected override void OnInitialized()
    {
        _instructionsId = $"{_Id}-ins";
        _scriptZoom = Zoom;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposed) return;

        var options = GetScriptOptions();

        try
        {
            await ApplyFullScreenHold();

            if (_handle is null)
            {
                _dotnetObj ??= DotNetObjectReference.Create(this);
                _scriptOptions = options;
                _scriptZoom = ClampZoom(Zoom);
                _handle = await _js.BitImageViewerSetup(_viewportRef, _dotnetObj, options, _scriptZoom);
                return;
            }

            if (options != _scriptOptions)
            {
                _scriptOptions = options;
                await _handle.InvokeVoidAsync("update", options);
            }

            var zoom = ClampZoom(Zoom);
            if (zoom != _scriptZoom)
            {
                _scriptZoom = zoom;
                await _handle.InvokeVoidAsync("setZoom", zoom);
            }
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // A viewer taken out of the page while full screen gives the page back: the lock is counted, and one never
        // released would hold the page still for good. The focus it would have handed back is left where the page
        // puts it, the viewer it would come back from being gone.
        if (_heldFullScreen)
        {
            _heldFullScreen = false;
            try
            {
                await _js.BitUtilsDisposeFocusTrap(_Id);
                await _js.BitUtilsUnlockScroll(_Id);
                await _js.BitUtilsForgetFocus(_Id);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        if (_handle is not null)
        {
            try
            {
                await _handle.InvokeVoidAsync("dispose");
                await _handle.DisposeAsync();
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
            catch (JSException) { } // nor a page that has already torn the script down
            _handle = null;
        }

        _dotnetObj?.Dispose();

        await base.DisposeAsync(disposing);
    }



    private BitImageViewerTexts ActiveTexts => Texts ?? _defaultTexts;

    private double EffectiveMaxZoom => Math.Max(MinZoom, MaxZoom);

    // A factor of one or less would zoom the wrong way, or not at all.
    private double StepFactor => ZoomFactor > 1 ? ZoomFactor : 1.25;

    private bool CannotZoomOut => Disabled || ClampZoom(Zoom) <= MinZoom;

    private bool CannotZoomIn => Disabled || ClampZoom(Zoom) >= EffectiveMaxZoom;

    private bool CannotReset => Disabled || ClampZoom(Zoom) == ClampZoom(1);

    // A disabled viewer goes full screen no more, but one that already is can always be left.
    private bool CannotToggleFullScreen => Disabled && FullScreen is false;

    private double ClampZoom(double zoom) => Math.Round(Math.Clamp(zoom, MinZoom, EffectiveMaxZoom), 4);

    private async Task SetZoom(double zoom)
    {
        if (Disabled) return;

        if (await AssignZoom(ClampZoom(zoom)))
        {
            await RenderAfterAssign();
        }
    }

    // A value assigned from a method the page calls - ZoomIn on a reference to the viewer, EnterFullScreen - re-renders
    // nothing on its own: the assignment only raises the change callback, and a viewer the page has not bound gets no
    // new parameters for the page's own render to pass on. Its render is what hands the zoom to the script and draws
    // the full screen, so it is asked for here; one from the viewer's own button comes after its event anyway.
    private Task RenderAfterAssign() => IsDisposed ? Task.CompletedTask : InvokeAsync(StateHasChanged);

    // A full screen that cannot be left - bound one way, which AssignFullScreen refuses to change - claims no Escape,
    // and leaves the key to whatever is around the viewer.
    private bool CanLeaveFullScreen => FullScreen && (FullScreenHasBeenSet is false || FullScreenChanged.HasDelegate);

    private string? EscapeClaim => CanLeaveFullScreen ? "claim" : null;

    private async Task HandleOnRootKeyDown(KeyboardEventArgs e)
    {
        if (CanLeaveFullScreen is false || e.IsPlainEscape() is false) return;

        await AssignFullScreen(false);
    }

    // Taken once the viewer is drawn over the page, given back once it is drawn in it again: the element the focus was
    // on is remembered and the focus moved into the frame, where the keyboard zooms and pans, Tab is kept inside the
    // viewer, and the page behind it is held still.
    private async Task ApplyFullScreenHold()
    {
        var fromButton = _enteringFromButton;
        _enteringFromButton = false;

        if (FullScreen == _heldFullScreen) return;

        _heldFullScreen = FullScreen;

        if (FullScreen)
        {
            if (fromButton && ShowFullScreenButton && NoToolbar is false)
            {
                await _fullScreenButtonRef.FocusAsync();
            }

            await _js.BitUtilsStoreFocus(_Id);
            await _js.BitUtilsLockScroll(_Id);
            await _js.BitUtilsSetupFocusTrap(_Id);
            await _viewportRef.FocusAsync();
        }
        else
        {
            await _js.BitUtilsDisposeFocusTrap(_Id);
            await _js.BitUtilsUnlockScroll(_Id);
            // The focus is still inside the viewer, which stays in the page, so it counts as lost (scopeId).
            await _js.BitUtilsRestoreFocus(_Id, onlyWhenLost: true, scopeId: _Id);
        }
    }

    private BitImageViewerJsOptions GetScriptOptions() => new()
    {
        Min = MinZoom,
        Max = EffectiveMaxZoom,
        Factor = StepFactor,
        DoubleClick = Math.Clamp(DoubleClickZoom, MinZoom, EffectiveMaxZoom),
        // A full screen viewer has no page around it for the wheel to scroll.
        Wheel = WheelZoom || FullScreen,
        Disabled = Disabled,
    };

    private string GetZoomText() => string.Format(CultureInfo.CurrentCulture, "{0:0}%", ClampZoom(Zoom) * 100);

    private string? GetViewportStyle() => JoinStyles(Height.HasValue() && FullScreen is false ? $"height:{Height}" : null, Styles?.Viewport);
}
