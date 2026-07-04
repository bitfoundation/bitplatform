using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

/// <summary>
/// A native Blazor chart component rendered entirely with SVG (no JavaScript or canvas).
/// Configure it either via a single <see cref="Config"/> object or the convenience
/// <see cref="Type"/>/<see cref="Data"/>/<see cref="Options"/> parameters, mirroring Chart.js.
/// </summary>
public partial class BitChart : ComponentBase, IAsyncDisposable
{
    /// <summary>Full configuration (type + data + options). Takes precedence when set.</summary>
    [Parameter] public BitChartConfig? Config { get; set; }

    [Parameter] public BitChartType Type { get; set; } = BitChartType.Line;
    [Parameter] public BitChartData? Data { get; set; }
    [Parameter] public BitChartOptions? Options { get; set; }

    /// <summary>CSS width of the chart container.</summary>
    [Parameter] public string Width { get; set; } = "100%";
    /// <summary>Optional CSS height. When null the height follows the aspect ratio.</summary>
    [Parameter] public string? Height { get; set; }

    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }

    /// <summary>Accessible label for the chart. When null a summary is generated.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Render a visually-hidden data table for screen readers (default true).</summary>
    [Parameter] public bool GenerateTable { get; set; } = true;

    /// <summary>
    /// When true (the default), entry/update animations are disabled for users who have requested
    /// reduced motion (the <c>prefers-reduced-motion: reduce</c> media query). Set to false to always
    /// animate regardless of the OS setting.
    /// </summary>
    [Parameter] public bool RespectReducedMotion { get; set; } = true;

    /// <summary>Optional custom tooltip template. When set it replaces the default tooltip body.</summary>
    [Parameter] public RenderFragment<BitChartTooltipContext>? TooltipTemplate { get; set; }

    /// <summary>Raised when a data element is clicked: (datasetIndex, dataIndex).</summary>
    [Parameter] public EventCallback<(int DatasetIndex, int DataIndex)> OnElementClick { get; set; }

    private readonly BitChartRenderState _state = new();
    private BitChartConfig _config = new();
    private BitChartScene _scene = new();

    // Virtual SVG coordinate space.
    private double _vw = 600;
    private double _vh = 300;

    // Measured container size (real device pixels) reported by the ResizeObserver.
    private double? _measuredWidth;
    private double? _measuredHeight;
    private bool _sizeRegistered;
    private IJSObjectReference? _sizeHandle;
    private bool _suppressTransition;

    // Interaction state (does not trigger a scene rebuild).
    private BitChartDataElement? _hovered;
    private readonly HashSet<BitChartDataElement> _active = new();
    private BitChartTooltipInfo? _activeTooltip;
    private BitChartTooltipContext? _tooltipContext;
    private readonly List<BitChartSvgNode> _hoverNodes = new();

    // Keyboard navigation.
    private int _focusIndex = -1;
    private string? _liveMessage;

    // Increments to (re)play entry animations: on data change and after the first size measurement.
    private int _animKey;
    private long _lastSig = long.MinValue;
    private bool _initialized;

    // Zoom/pan interop.
    [Inject] private IJSRuntime JS { get; set; } = default!;
    private ElementReference _plotEl;
    private IJSObjectReference? _zoomHandle;
    private DotNetObjectReference<BitChart>? _dotRef;
    private bool _zoomRegistered;

    // Drag-zoom selection box in viewBox coordinates (x, y, w, h).
    private (double X, double Y, double W, double H)? _dragBox;

    // Unique id for this instance's SVG defs (clip paths, gradients).
    private readonly string _instanceId = "bc" + Guid.NewGuid().ToString("N")[..8];

    protected override void OnParametersSet()
    {
        _config = Config ?? new BitChartConfig(Type, Data ?? new BitChartData(), Options ?? new BitChartOptions());
        Recompute();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // A resize-triggered render suppresses geometry transitions; re-enable for later updates.
        if (_suppressTransition) _suppressTransition = false;

        // Responsive sizing: observe the container so we can render at real device pixels.
        if (_config.Options.Responsive && !_sizeRegistered)
        {
            _sizeRegistered = true;
            try
            {
                _dotRef ??= DotNetObjectReference.Create(this);
                _sizeHandle = await JS.BitChartObserve(_plotEl, _dotRef);
            }
            catch
            {
                // Interop unavailable (e.g. during prerender) — stay at the fixed virtual size.
                _sizeRegistered = false;
            }
        }

        if (_zoomRegistered || !_config.Options.Zoom.Enabled || _scene.IsRadialOrCircular)
            return;
        _zoomRegistered = true;
        var z = _config.Options.Zoom;
        try
        {
            _dotRef ??= DotNetObjectReference.Create(this);
            _zoomHandle = await JS.BitChartRegister(_plotEl, _dotRef,
                new { wheel = z.Wheel, pan = z.Pan && !z.DragZoom, drag = z.DragZoom });
        }
        catch
        {
            // Interop unavailable (e.g. during prerender) — zoom stays inert.
            _zoomRegistered = false;
        }
    }

    /// <summary>Invoked by the ResizeObserver when the container's pixel size changes.</summary>
    [JSInvokable]
    public void OnResize(double width, double height)
    {
        if (width <= 0 || height <= 0) return;
        if (_measuredWidth is { } w && _measuredHeight is { } h
            && Math.Abs(w - width) < 1 && Math.Abs(h - height) < 1) return;

        _measuredWidth = width;
        _measuredHeight = height;
        _suppressTransition = true;   // resizing must not animate/transition element geometry
        Recompute();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnWheelZoom(double fracX, double fracY, double deltaY)
    {
        var z = _config.Options.Zoom;
        double factor = deltaY < 0 ? 1 - z.Speed : 1 + z.Speed;
        foreach (var id in AxesForMode())
        {
            double t = AxisFraction(id, fracX, fracY);
            var (min, max) = CurrentRange(id);
            double cursor = min + t * (max - min);
            double nMin = cursor - (cursor - min) * factor;
            double nMax = cursor + (max - cursor) * factor;
            if (nMax - nMin > 1e-9) _state.AxisRanges[id] = (nMin, nMax);
        }
        _suppressTransition = true;
        Recompute();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnDragMove(double x0, double y0, double x1, double y1)
    {
        _dragBox = (Math.Min(x0, x1) * _vw, Math.Min(y0, y1) * _vh,
                    Math.Abs(x1 - x0) * _vw, Math.Abs(y1 - y0) * _vh);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnDragEnd(double x0, double y0, double x1, double y1)
    {
        _dragBox = null;
        // Ignore tiny drags (treated as a click).
        if (Math.Abs(x1 - x0) < 0.01 && Math.Abs(y1 - y0) < 0.01) { StateHasChanged(); return; }

        foreach (var id in AxesForMode())
        {
            double ta = AxisFraction(id, x0, y0);
            double tb = AxisFraction(id, x1, y1);
            double lo = Math.Min(ta, tb), hi = Math.Max(ta, tb);
            var (min, max) = CurrentRange(id);
            double span = max - min;
            double nMin = min + lo * span, nMax = min + hi * span;
            if (nMax - nMin > 1e-9) _state.AxisRanges[id] = (nMin, nMax);
        }
        _suppressTransition = true;
        Recompute();
        StateHasChanged();
    }

    /// <summary>Converts an element fraction (0..1) to a 0..1 position along an axis, via the plot area.</summary>
    private double AxisFraction(string id, double fracX, double fracY)
    {
        if (_scene.PlotArea is not { } p) return id == "x" ? fracX : 1 - fracY;
        if (id == "x")
        {
            double x = fracX * _vw;
            return p.Width <= 0 ? 0 : Math.Clamp((x - p.Left) / p.Width, 0, 1);
        }
        double y = fracY * _vh;
        return p.Height <= 0 ? 0 : Math.Clamp(1 - (y - p.Top) / p.Height, 0, 1);
    }

    [JSInvokable]
    public void OnPan(double dx, double dy)
    {
        foreach (var id in AxesForMode())
        {
            var (min, max) = CurrentRange(id);
            double span = max - min;
            double delta = id == "x" ? -dx * span : dy * span;
            _state.AxisRanges[id] = (min + delta, max + delta);
        }
        _suppressTransition = true;
        Recompute();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnResetZoom()
    {
        _state.AxisRanges.Clear();
        _suppressTransition = true;
        Recompute();
        StateHasChanged();
    }

    private IEnumerable<string> AxesForMode()
    {
        var mode = _config.Options.Zoom.Mode;
        foreach (var id in _scene.ZoomableAxes)
        {
            bool isX = id == "x";
            if (mode == BitChartZoomMode.X && !isX) continue;
            if (mode == BitChartZoomMode.Y && isX) continue;
            yield return id;
        }
    }

    private (double Min, double Max) CurrentRange(string id)
    {
        if (_state.AxisRanges.TryGetValue(id, out var r)) return r;
        if (_scene.AxisRanges.TryGetValue(id, out var s)) return s;
        return (0, 1);
    }

    private void Recompute()
    {
        bool circular = _config.Type is BitChartType.Pie or BitChartType.Doughnut
            or BitChartType.PolarArea or BitChartType.Radar;
        double aspect = _config.Options.AspectRatio ?? (circular ? 1 : 2);
        if (aspect <= 0) aspect = 2;

        // Use the measured container width (real pixels → constant font sizes) when responsive,
        // otherwise fall back to a fixed 600-unit virtual space (e.g. during prerender).
        bool responsive = _config.Options.Responsive;
        double basis = responsive && _measuredWidth is { } mw && mw > 0 ? mw : 600;
        _vw = basis;
        if (_config.Options.MaintainAspectRatio)
            _vh = _vw / aspect;
        else
            _vh = responsive && _measuredHeight is { } mh && mh > 0 ? mh : basis / aspect;

        _scene = new BitChartRenderer(_config, _state, _vw, _vh).Render();
        ClearHover();
        _focusIndex = -1;

        // Decide whether to (re)play the entry animation. We key off a signature of the data
        // values (not pixel positions), so data changes replay the animation while resize/zoom/pan
        // — which leave the values unchanged — do not.
        long sig = ComputeSignature();
        bool dataChanged = _initialized && sig != _lastSig;
        _lastSig = sig;

        // Entry animation plays on first mount automatically (CSS). On later data changes we bump the
        // key to recreate the group so the animation replays. Resize/zoom/pan keep values unchanged
        // (same signature) so they don't replay.
        if (AnimationEnabled && dataChanged && !_suppressTransition)
            _animKey++;

        _initialized = true;
    }

    /// <summary>A cheap signature of the data values driving the chart (changes when data changes).
    /// Animation settings are folded in so that changing duration/easing/stagger replays the entry
    /// animation, giving immediate visual feedback when those options are tweaked.</summary>
    private long ComputeSignature()
    {
        unchecked
        {
            long h = 17;
            h = h * 31 + (int)_config.Type;
            var anim = _config.Options.Animation;
            h = h * 31 + anim.Duration;
            h = h * 31 + (anim.Easing?.GetHashCode() ?? 0);
            h = h * 31 + BitConverter.DoubleToInt64Bits(anim.DelayBetween);
            foreach (var el in _scene.Elements)
            {
                h = h * 31 + el.DatasetIndex;
                h = h * 31 + el.DataIndex;
                h = h * 31 + BitConverter.DoubleToInt64Bits(Math.Round(el.Value, 6));
            }
            return h;
        }
    }

    // ---- hover / interaction (no scene rebuild) ----

    private void OnEnter(BitChartDataElement e)
    {
        _hovered = e;
        BuildHover(e);
    }

    private void OnLeave() => ClearHover();

    private void ClearHover()
    {
        _hovered = null;
        _active.Clear();
        _activeTooltip = null;
        _tooltipContext = null;
        _hoverNodes.Clear();
    }

    private void BuildHover(BitChartDataElement e)
    {
        _active.Clear();
        _hoverNodes.Clear();
        var tip = _config.Options.Plugins.Tooltip;

        IEnumerable<BitChartDataElement> group = tip.Mode switch
        {
            BitChartInteractionMode.Index or BitChartInteractionMode.X or BitChartInteractionMode.Y when !_scene.IsRadialOrCircular
                => _scene.Elements.Where(x => x.DataIndex == e.DataIndex),
            BitChartInteractionMode.Dataset
                => _scene.Elements.Where(x => x.DatasetIndex == e.DatasetIndex),
            _ => new[] { e }
        };

        foreach (var el in group) _active.Add(el);

        // Combined tooltip.
        var combined = new BitChartTooltipInfo
        {
            Title = e.Tooltip.Title,
            AnchorX = e.Tooltip.AnchorX,
            AnchorY = e.Tooltip.AnchorY
        };
        var ordered = _active.OrderBy(a => a.DatasetIndex).ToList();
        foreach (var el in ordered)
            combined.Items.AddRange(el.Tooltip.Items);

        // ---- Tooltip callbacks (title / body extras / footer / label color) ----
        var cb = tip.Callbacks;
        if (cb.Title is not null || cb.BeforeBody is not null || cb.AfterBody is not null
            || cb.Footer is not null || cb.LabelColor is not null)
        {
            var items = ordered.Select(a => new BitChartTooltipItemContext
            {
                DatasetIndex = a.DatasetIndex,
                DataIndex = a.DataIndex,
                DatasetLabel = a.SeriesLabel,
                Label = a.Tooltip.Title,
                Value = a.Value,
                Color = a.Tooltip.Items.FirstOrDefault()?.Color ?? "#000",
                FormattedValue = a.Tooltip.Items.FirstOrDefault()?.Text ?? ""
            }).ToList();

            if (cb.Title?.Invoke(items) is { } titleText) combined.Title = titleText;
            if (cb.BeforeBody?.Invoke(items) is { } bb) combined.BeforeBody.AddRange(bb.Split('\n'));
            if (cb.AfterBody?.Invoke(items) is { } ab) combined.AfterBody.AddRange(ab.Split('\n'));
            if (cb.Footer?.Invoke(items) is { } ft) combined.Footer.AddRange(ft.Split('\n'));
            if (cb.LabelColor is not null)
                for (int i = 0; i < combined.Items.Count && i < items.Count; i++)
                    if (cb.LabelColor(items[i]) is { } lc) combined.Items[i].Color = lc;
        }

        // Positioner.
        if (tip.Position == BitChartTooltipPositioner.Average && _active.Count > 0)
        {
            combined.AnchorX = _active.Average(a => a.CenterX);
            combined.AnchorY = _active.Min(a => a.Tooltip.AnchorY);
        }
        _activeTooltip = combined;

        // Context for a custom template.
        _tooltipContext = new BitChartTooltipContext
        {
            Title = combined.Title,
            Points = _active.OrderBy(a => a.DatasetIndex).Select(a => new BitChartTooltipPoint
            {
                DatasetIndex = a.DatasetIndex,
                DataIndex = a.DataIndex,
                Label = a.SeriesLabel,
                Value = a.Value,
                Color = a.Tooltip.Items.FirstOrDefault()?.Color ?? "#000",
                FormattedValue = a.Tooltip.Items.FirstOrDefault()?.Text ?? ""
            }).ToList()
        };

        // Highlight overlay.
        bool indexMode = tip.Mode is BitChartInteractionMode.Index or BitChartInteractionMode.X && !_scene.IsRadialOrCircular;
        if (indexMode && _scene.PlotArea is { } pa)
            _hoverNodes.Add(new BitChartSvgLine
            {
                X1 = e.CenterX, Y1 = pa.Top, X2 = e.CenterX, Y2 = pa.Bottom,
                Stroke = "rgba(0,0,0,0.35)", StrokeWidth = 1, Dash = "4,3"
            });

        foreach (var el in _active)
        {
            if (el.Shape is BitChartSvgCircle c)
                _hoverNodes.Add(new BitChartSvgCircle
                {
                    Cx = c.Cx, Cy = c.Cy, R = c.R + 4,
                    Fill = "none", Stroke = c.Fill, StrokeWidth = 2, Opacity = 0.6
                });
        }
    }

    private async Task OnClickElement(BitChartDataElement e)
    {
        if (OnElementClick.HasDelegate)
            await OnElementClick.InvokeAsync((e.DatasetIndex, e.DataIndex));
    }

    // ---- keyboard navigation ----

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        int n = _scene.Elements.Count;
        if (n == 0) return;
        switch (e.Key)
        {
            case "ArrowRight":
            case "ArrowDown":
                Move(1);
                break;
            case "ArrowLeft":
            case "ArrowUp":
                Move(-1);
                break;
            case "Home":
                SetFocus(0);
                break;
            case "End":
                SetFocus(n - 1);
                break;
            case "Enter":
            case " ":
                if (_focusIndex >= 0) await OnClickElement(_scene.Elements[_focusIndex]);
                break;
            case "Escape":
                _focusIndex = -1;
                ClearHover();
                _liveMessage = null;
                break;
        }
    }

    private void Move(int dir)
    {
        int n = _scene.Elements.Count;
        if (_focusIndex < 0) SetFocus(dir > 0 ? 0 : n - 1);
        else SetFocus((_focusIndex + dir + n) % n);
    }

    private void SetFocus(int i)
    {
        _focusIndex = i;
        var el = _scene.Elements[i];
        _hovered = el;
        BuildHover(el);
        _hoverNodes.Add(FocusOutline(el));
        _liveMessage = Describe(el);
    }

    private static BitChartSvgNode FocusOutline(BitChartDataElement el) => el.Shape switch
    {
        BitChartSvgRect r => new BitChartSvgRect { X = r.X - 2, Y = r.Y - 2, Width = r.Width + 4, Height = r.Height + 4, Fill = "none", Stroke = "#1a1a1a", StrokeWidth = 2, CssClass = "bc-focus-ring" },
        BitChartSvgCircle c => new BitChartSvgCircle { Cx = c.Cx, Cy = c.Cy, R = c.R + 5, Fill = "none", Stroke = "#1a1a1a", StrokeWidth = 2, CssClass = "bc-focus-ring" },
        _ => new BitChartSvgCircle { Cx = el.CenterX, Cy = el.CenterY, R = 8, Fill = "none", Stroke = "#1a1a1a", StrokeWidth = 2, CssClass = "bc-focus-ring" }
    };

    private string Describe(BitChartDataElement el)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(el.Tooltip.Title)) parts.Add(el.Tooltip.Title!);
        foreach (var item in el.Tooltip.Items) parts.Add(item.Text);
        return string.Join(", ", parts);
    }

    private void ToggleLegend(BitChartLegendItemModel item)
    {
        if (_scene.Legend is null || !_scene.Legend.OnClickToggle) return;
        if (item.IsDataIndex)
        {
            if (!_state.HiddenIndices.Add(item.Index)) _state.HiddenIndices.Remove(item.Index);
        }
        else
        {
            if (!_state.HiddenDatasets.Add(item.Index)) _state.HiddenDatasets.Remove(item.Index);
        }
        Recompute();
        StateHasChanged();
    }

    // ---- view helpers ----

    private string ViewBox => $"0 0 {BitChartSvg.N(_vw)} {BitChartSvg.N(_vh)}";

    private string? ClipId => _scene.PlotArea is null ? null : $"{_instanceId}-clip";
    private string? ClipRef => ClipId is null ? null : $"url(#{ClipId})";

    private bool AnimationEnabled => _config.Options.Animation.Animate;

    /// <summary>
    /// Entry animations run on first mount (pure CSS, no JS dependency) and replay on data change.
    /// </summary>
    private bool CanAnimate => AnimationEnabled;

    /// <summary>
    /// True when a positive per-element delay is configured: each data element animates in sequence
    /// rather than the whole data group animating as one unit. Not used for radial/circular charts.
    /// </summary>
    private bool Staggered => CanAnimate && _config.Options.Animation.DelayBetween > 0 && !_scene.IsRadialOrCircular;

    /// <summary>
    /// True when the line/area series should draw on progressively (stroke reveal left to right)
    /// with points appearing in sequence. Driven by <see cref="BitChartAnimationOptions.Progressive"/>
    /// and only set by the renderer for line/area charts.
    /// </summary>
    private bool ProgressiveDraw => CanAnimate && _scene.ProgressiveDraw;

    /// <summary>
    /// Global (unscoped) animation rules emitted once per chart. Kept out of the component's
    /// isolated stylesheet so the rules reliably match the SVG shapes rendered by the child
    /// <c>SvgPrimitive</c> component (which carries a different CSS-isolation scope).
    /// </summary>
    private const string AnimationStyles = """
        <style>
        @keyframes bc-rise { from { opacity: 0; transform: translateY(12px) scaleY(0.9); } to { opacity: 1; transform: none; } }
        @keyframes bc-grow { from { opacity: 0; transform: scale(0.82); } to { opacity: 1; transform: none; } }
        @keyframes bc-draw { from { stroke-dashoffset: 1; } to { stroke-dashoffset: 0; } }
        @keyframes bc-fade { from { opacity: 0; } to { opacity: 1; } }
        @keyframes bc-scale-y { from { transform: scaleY(0); } to { transform: scaleY(1); } }
        @keyframes bc-scale-x { from { transform: scaleX(0); } to { transform: scaleX(1); } }
        @keyframes bc-pop { from { opacity: 0; transform: scale(0); } to { opacity: 1; transform: scale(1); } }
        .bc-animate { animation-duration: var(--bc-dur, 600ms); animation-timing-function: var(--bc-ease, ease-out); animation-fill-mode: both; }
        .bc-anim-rise { animation-name: bc-rise; transform-box: view-box; transform-origin: center bottom; }
        .bc-anim-grow { animation-name: bc-grow; transform-box: view-box; transform-origin: center; }
        .bc-anim-bars-v { animation-name: bc-scale-y; transform-box: view-box; transform-origin: center bottom; }
        .bc-anim-bars-h { animation-name: bc-scale-x; transform-box: view-box; transform-origin: left center; }
        .bc-el-anim { animation-duration: var(--bc-dur, 600ms); animation-timing-function: var(--bc-ease, ease-out); animation-fill-mode: both; transform-box: fill-box; }
        .bc-el-rise { animation-name: bc-rise; transform-origin: center bottom; }
        .bc-draw { animation: bc-draw var(--bc-dur, 600ms) var(--bc-ease, ease-out) both; stroke-dasharray: 1; }
        .bc-fade { animation: bc-fade var(--bc-dur, 600ms) var(--bc-ease, ease-out) both; }
        .bc-focus-ring { stroke-dasharray: 3 2; }
        .bc-el:hover :is(rect, path, polygon, circle) { filter: brightness(0.92); }
        .bc-active :is(rect, path, polygon) { filter: brightness(0.9); }
        .bc-transition :is(rect, circle, path, polygon) {
            transition: x var(--bc-dur,600ms) var(--bc-ease,ease-out), y var(--bc-dur,600ms) var(--bc-ease,ease-out),
                        width var(--bc-dur,600ms) var(--bc-ease,ease-out), height var(--bc-dur,600ms) var(--bc-ease,ease-out),
                        cx var(--bc-dur,600ms) var(--bc-ease,ease-out), cy var(--bc-dur,600ms) var(--bc-ease,ease-out),
                        r var(--bc-dur,600ms) var(--bc-ease,ease-out), d var(--bc-dur,600ms) var(--bc-ease,ease-out), fill .3s ease;
        }
        .bc-root { display: flex; flex-direction: column; box-sizing: border-box; font-family: Helvetica, Arial, sans-serif; }
        .bc-title, .bc-subtitle { display: flex; width: 100%; padding: 6px 0; text-align: center; }
        .bc-mid { display: flex; flex-direction: row; align-items: stretch; flex: 1 1 auto; min-height: 0; }
        .bc-plot { position: relative; flex: 1 1 auto; min-width: 0; }
        .bc-svg { display: block; width: 100%; height: 100%; overflow: visible; }
        .bc-svg:focus { outline: none; }
        .bc-svg:focus-visible { outline: 2px solid #36a2eb; outline-offset: 2px; border-radius: 4px; }
        .bc-el { transition: opacity .15s ease; }
        .bc-el:hover { opacity: 0.92; }
        .bc-hover { pointer-events: none; }
        .bc-legend { display: flex; gap: 4px 14px; padding: 6px 8px; flex-wrap: wrap; }
        .bc-legend-h { flex-direction: row; justify-content: center; }
        .bc-legend-v { flex-direction: column; align-content: center; justify-content: center; }
        .bc-legend-item { display: inline-flex; align-items: center; gap: 6px; user-select: none; line-height: 1.4; }
        .bc-legend-item.bc-hidden { text-decoration: line-through; opacity: 0.45; }
        .bc-legend-box { display: inline-block; border: 2px solid; border-radius: 2px; flex: 0 0 auto; }
        .bc-legend-dot { display: inline-block; border-radius: 50%; flex: 0 0 auto; }
        .bc-legend-marker { flex: 0 0 auto; overflow: visible; }
        .bc-legend-title { font-weight: bold; width: 100%; text-align: center; }
        .bc-tooltip { position: absolute; transform: translate(-50%, calc(-100% - 10px)); pointer-events: none; white-space: nowrap; z-index: 10; box-shadow: 0 2px 8px rgba(0,0,0,0.25); transition: left .08s linear, top .08s linear; }
        .bc-tt-title { margin-bottom: 3px; }
        .bc-tt-footer { margin-top: 4px; padding-top: 4px; border-top: 1px solid rgba(255,255,255,0.25); }
        .bc-tooltip-custom { background: #fff; color: #1f2733; border: 1px solid #e6e9ef; border-radius: 8px; padding: 8px 10px; font-size: 12px; }
        .bc-tt-item { display: flex; align-items: center; gap: 6px; line-height: 1.5; }
        .bc-tt-swatch { display: inline-block; width: 10px; height: 10px; border-radius: 2px; flex: 0 0 auto; }
        .bc-tt-swatch-svg { flex: 0 0 auto; overflow: visible; }
        .bc-sr-only { position: absolute; width: 1px; height: 1px; padding: 0; margin: -1px; overflow: hidden; clip: rect(0, 0, 0, 0); white-space: nowrap; border: 0; }
        </style>
        """;

    /// <summary>
    /// Opt-in reduced-motion overrides. Injected only when <see cref="RespectReducedMotion"/> is true,
    /// so animations/transitions are disabled for users who requested reduced motion.
    /// </summary>
    private const string ReducedMotionStyles = """
        <style>
        @media (prefers-reduced-motion: reduce) { .bc-animate, .bc-el-anim, .bc-draw, .bc-fade { animation: none; } .bc-transition :is(rect, circle, path, polygon) { transition: none; } }
        </style>
        """;

    private string DataGroupClass
    {
        get
        {
            if (!CanAnimate) return "bc-data";
            // Progressive draw: points reveal individually (per-element delay tied to x position),
            // so the group itself carries no animation.
            if (ProgressiveDraw) return "bc-data";
            // When staggering, the individual elements animate (with per-element delays) instead of
            // the whole group, so the group itself carries no animation.
            if (Staggered) return "bc-data";
            if (_scene.IsRadialOrCircular)
                return "bc-data bc-animate bc-anim-grow";
            // Bars grow from the baseline as a size change, in the correct direction for the orientation.
            if (_scene.HasBars)
                return _scene.HorizontalBars ? "bc-data bc-animate bc-anim-bars-h" : "bc-data bc-animate bc-anim-bars-v";
            // Line/scatter points rise in.
            return "bc-data bc-animate bc-anim-rise";
        }
    }

    /// <summary>
    /// Inline transform-origin for the data group's entry animation. For bars we pin the origin to the
    /// value-axis baseline (in view-box pixels) so the group scales out of the axis line instead of the
    /// bottom/left edge of the SVG. The animation itself stays on the keyed group, which is what makes
    /// it replay reliably on both reload and SPA navigation.
    /// </summary>
    private string? DataGroupStyle
    {
        get
        {
            if (!CanAnimate || Staggered || !_scene.HasBars) return null;
            // scaleY uses only the y-origin (vertical bars); scaleX only the x-origin (horizontal bars).
            return _scene.HorizontalBars
                ? $"transform-origin:{BitChartSvg.N(_scene.BarBaseline)}px 0px"
                : $"transform-origin:0px {BitChartSvg.N(_scene.BarBaseline)}px";
        }
    }

    /// <summary>Class for the series (line/area) group — animates with the same proven group mechanism.
    /// Radial/circular charts (e.g. radar) grow from the center so the web matches its joint points.</summary>
    private string SeriesGroupClass =>
        CanAnimate
            ? ProgressiveDraw
                // The stroke draws itself on (bc-draw) and fills fade in, both at the path level,
                // so the group must not also rise.
                ? "bc-series"
                : _scene.IsRadialOrCircular
                    ? "bc-series bc-animate bc-anim-grow"
                    : "bc-series bc-animate bc-anim-rise"
            : "bc-series";

    private string ElementClass(BitChartDataElement el)
    {
        string c = "bc-el";
        if (IsActive(el)) c += " bc-active";
        if (ProgressiveDraw)
        {
            // Each point pops in as the drawing stroke reaches it (delay set in ElementStyle).
            c += " bc-el-anim bc-el-rise";
        }
        else if (Staggered)
        {
            // Bars reuse the proven view-box scaling classes (with an explicit per-element pixel
            // transform-origin set in ElementStyle); points/markers rise in via the fill-box class.
            c += _scene.HasBars
                ? _scene.HorizontalBars ? " bc-animate bc-anim-bars-h" : " bc-animate bc-anim-bars-v"
                : " bc-el-anim bc-el-rise";
        }
        return c;
    }

    private string ElementStyle(int index)
    {
        if (ProgressiveDraw)
        {
            // Reveal each point in time with the stroke as it sweeps left to right. The delay is tied
            // to the point's horizontal position within the plot area so points and line stay in sync.
            double dur = _config.Options.Animation.Duration;
            double elDur = Math.Min(250, dur * 0.4);
            double frac = 0;
            if (_scene.PlotArea is { Width: > 0 } pa)
                frac = Math.Clamp((_scene.Elements[index].CenterX - pa.Left) / pa.Width, 0, 1);
            double pDelay = frac * Math.Max(0, dur - elDur);
            return $"cursor:pointer;animation-delay:{BitChartSvg.N(pDelay)}ms;animation-duration:{BitChartSvg.N(elDur)}ms";
        }
        if (!Staggered) return "cursor:pointer";
        double delay = index * _config.Options.Animation.DelayBetween;
        string s = $"cursor:pointer;animation-delay:{BitChartSvg.N(delay)}ms";
        if (_scene.HasBars)
        {
            // bc-anim-bars-* use transform-box: view-box, so the origin must be given in view-box
            // pixels pinned to the value-axis baseline (matching the non-staggered group behaviour).
            var el = _scene.Elements[index];
            s += _scene.HorizontalBars
                ? $";transform-origin:{BitChartSvg.N(_scene.BarBaseline)}px {BitChartSvg.N(el.CenterY)}px"
                : $";transform-origin:{BitChartSvg.N(el.CenterX)}px {BitChartSvg.N(_scene.BarBaseline)}px";
        }
        return s;
    }

    private string AnimStyle =>
        $"--bc-dur:{_config.Options.Animation.Duration}ms;--bc-ease:{_config.Options.Animation.Easing}";

    private bool IsActive(BitChartDataElement e) => _active.Count > 0 && _active.Contains(e);

    private string RootStyle
    {
        get
        {
            var s = $"width:{Width};";
            if (!string.IsNullOrEmpty(Height)) s += $"height:{Height};";
            if (!string.IsNullOrEmpty(Style)) s += Style;
            return s;
        }
    }

    private double Pct(double v, double total) => total <= 0 ? 0 : v / total * 100;

    private string ChartAriaLabel
    {
        get
        {
            if (!string.IsNullOrEmpty(AriaLabel)) return AriaLabel;
            if (_config.Options.Plugins.Title is { Display: true, Text.Length: > 0 } t) return t.Text;
            int series = _config.Data.Datasets.Count;
            return $"{_config.Type} chart with {series} data series.";
        }
    }

    private bool HasPointData => _config.Data.Datasets.Any(d => d.Points is { Count: > 0 });

    private static string AlignToFlex(BitChartAlign a) => a switch
    {
        BitChartAlign.Start => "flex-start",
        BitChartAlign.End => "flex-end",
        _ => "center"
    };

    private static string TextAlign(BitChartAlign a) => a switch
    {
        BitChartAlign.Start => "left",
        BitChartAlign.End => "right",
        _ => "center"
    };

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_sizeHandle is not null) { await _sizeHandle.InvokeVoidAsync("dispose"); await _sizeHandle.DisposeAsync(); }
            if (_zoomHandle is not null) { await _zoomHandle.InvokeVoidAsync("dispose"); await _zoomHandle.DisposeAsync(); }
        }
        catch (JSDisconnectedException) { }
        catch (Exception) { }
        _dotRef?.Dispose();
    }
}
