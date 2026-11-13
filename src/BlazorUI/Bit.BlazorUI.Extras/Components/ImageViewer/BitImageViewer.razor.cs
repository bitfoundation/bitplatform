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
    /// The height of the frame the image is shown in, as a CSS value (e.g. "24rem"). Default: the
    /// --bit-ImageViewer-height variable, else 20rem.
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
    /// the page the viewer sits in; a viewer that fills the screen, or a dialog, has no page around it to scroll.
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
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
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

    private double ClampZoom(double zoom) => Math.Round(Math.Clamp(zoom, MinZoom, EffectiveMaxZoom), 4);

    private async Task SetZoom(double zoom)
    {
        if (Disabled) return;

        await AssignZoom(ClampZoom(zoom));
    }

    private BitImageViewerJsOptions GetScriptOptions() => new()
    {
        Min = MinZoom,
        Max = EffectiveMaxZoom,
        Factor = StepFactor,
        DoubleClick = Math.Clamp(DoubleClickZoom, MinZoom, EffectiveMaxZoom),
        Wheel = WheelZoom,
        Disabled = Disabled,
    };

    private string GetZoomText() => string.Format(CultureInfo.CurrentCulture, "{0:0}%", ClampZoom(Zoom) * 100);

    private string? GetViewportStyle() => JoinStyles(Height.HasValue() ? $"height:{Height}" : null, Styles?.Viewport);
}
