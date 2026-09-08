using System.Globalization;
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

    /// <summary>Id of the root element.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Text direction of the chrome around the plot (title, legend, tooltip, data table).</summary>
    [Parameter] public BitDir? Dir { get; set; }

    /// <summary>
    /// Additional HTML attributes applied to the root element, following the same convention as the rest
    /// of the library: assign the dictionary explicitly rather than relying on unmatched-value capture.
    /// </summary>
    [Parameter] public Dictionary<string, object> HtmlAttributes { get; set; } = [];

    /// <summary>Accessible label for the chart. When null a summary is generated.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Render a visually-hidden data table for screen readers (default true).</summary>
    [Parameter] public bool GenerateTable { get; set; } = true;

    /// <summary>
    /// Upper bound on the rows the screen-reader table renders. A long series would otherwise put tens
    /// of thousands of hidden nodes in the DOM for no one's benefit; past the limit the table shows the
    /// first rows and its caption says how many were left out.
    /// </summary>
    [Parameter] public int MaxTableRows { get; set; } = 500;

    /// <summary>
    /// Upper bound on the columns the screen-reader table renders. A value series is one table row with
    /// a cell per category, so a long series is wide rather than tall and the row cap alone would not
    /// stop it; past this limit the table shows the first columns and its caption says how many were
    /// left out. Ignored for point (scatter/bubble) data, whose table is three fixed columns.
    /// </summary>
    [Parameter] public int MaxTableColumns { get; set; } = 100;

    /// <summary>
    /// A visually hidden sentence telling a screen-reader user how to walk the data, pointed at by the
    /// chart's <c>aria-describedby</c> alongside the data table. Without it the chart announces itself
    /// as a picture and nothing says the arrow keys do anything. Set it to null or an empty string to
    /// leave it out; it is only rendered when there is data to navigate.
    /// </summary>
    [Parameter] public string? NavigationHint { get; set; } =
        "Interactive chart. Use the left and right arrow keys to move through a series, "
        + "the up and down arrow keys to move between series, Home and End for the first and last value, "
        + "Enter to select, and Escape to leave.";

    /// <summary>Message shown in place of the plot when there is nothing to draw.</summary>
    [Parameter] public string NoDataText { get; set; } = "No data to display";

    /// <summary>Custom content shown in place of the plot when there is nothing to draw.</summary>
    [Parameter] public RenderFragment? NoDataTemplate { get; set; }

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

    /// <summary>Raised when the active (hovered or keyboard-focused) element set changes. The context
    /// is null when nothing is active any more.</summary>
    [Parameter] public EventCallback<BitChartTooltipContext?> OnElementHover { get; set; }

    /// <summary>Raised when a legend item is clicked, before the default visibility toggle runs.</summary>
    [Parameter] public EventCallback<BitChartLegendItemModel> OnLegendItemClick { get; set; }

    /// <summary>Raised after zoom or pan changes the visible axis ranges.</summary>
    [Parameter] public EventCallback OnZoomChange { get; set; }

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
    private readonly HashSet<BitChartDataElement> _active = new();
    private BitChartTooltipInfo? _activeTooltip;
    private BitChartTooltipContext? _tooltipContext;
    private readonly List<BitChartSvgNode> _hoverNodes = new();

    // Keyboard navigation.
    private int _focusIndex = -1;
    private string? _liveMessage;

    // What the current hover/focus points at, in data coordinates rather than by element identity.
    // A rebuild replaces every element object, so this is what lets an active tooltip - or the
    // keyboard position - survive a re-render driven by the parent, a resize, or a zoom.
    private (int Ds, int Di)? _hoverAnchor;
    private bool _hoverForceIndex;
    private (int Ds, int Di)? _focusKey;

    // Increments to (re)play entry animations: on data change and after the first size measurement.
    private int _animKey;
    private long _lastSig = long.MinValue;
    private bool _initialized;

    // Zoom/pan interop.
    [Inject] private IJSRuntime JS { get; set; } = default!;
    private ElementReference _plotEl;
    private IJSObjectReference? _zoomHandle;
    private DotNetObjectReference<BitChart>? _dotRef;
    /// <summary>What the gesture bridge is currently registered for; null until the first attempt.</summary>
    private string? _zoomSignature;

    // Drag-zoom selection box in viewBox coordinates (x, y, w, h).
    private (double X, double Y, double W, double H)? _dragBox;

    // Unique id for this instance's SVG defs (clip paths, gradients, patterns) and the a11y table.
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

        // The pointer/keyboard bridge is attached for every chart - it is what stops the arrow keys
        // scrolling the page while the chart has focus - and additionally observes the container size
        // when the chart is responsive, so it can render at real device pixels.
        if (!_sizeRegistered)
        {
            _sizeRegistered = true;
            try
            {
                _dotRef ??= DotNetObjectReference.Create(this);
                _sizeHandle = await JS.BitChartObserve(_plotEl, _dotRef, _config.Options.Responsive);
            }
            catch
            {
                // Interop unavailable (e.g. during prerender) - stay at the fixed virtual size.
                _sizeRegistered = false;
            }
        }

        // The gesture bridge is keyed by what it was asked to listen for, so turning zoom off (or
        // switching between panning and drag-to-zoom) at runtime tears the old listeners down instead
        // of leaving the chart reacting to gestures it no longer offers.
        var z = _config.Options.Zoom;
        bool wantZoom = z.Enabled && !_scene.IsRadialOrCircular;
        bool pan = z.Pan && !z.DragZoom;
        string signature = wantZoom ? $"{z.Wheel}|{pan}|{z.DragZoom}" : "";
        if (_zoomSignature == signature) return;

        if (_zoomHandle is not null)
        {
            try { await _zoomHandle.InvokeVoidAsync("dispose"); await _zoomHandle.DisposeAsync(); }
            catch (JSDisconnectedException) { }
            catch (Exception) { }
            _zoomHandle = null;
        }
        _zoomSignature = signature;
        if (!wantZoom) return;

        try
        {
            _dotRef ??= DotNetObjectReference.Create(this);
            // A concrete class is required here (not an anonymous type): the trimmer strips
            // anonymous type members in release builds, which breaks System.Text.Json
            // serialization during the JS interop call.
            _zoomHandle = await JS.BitChartRegister(_plotEl, _dotRef,
                new BitChartZoomPayload { Wheel = z.Wheel, Pan = pan, Drag = z.DragZoom });
        }
        catch
        {
            // Interop unavailable (e.g. during prerender) - zoom stays inert until the next render.
            _zoomSignature = null;
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
            ApplyRange(id, nMin, nMax);
        }
        AfterZoom();
    }

    /// <summary>
    /// Invoked while two fingers pinch the chart. Unlike the wheel, which arrives in notches and steps by
    /// a fixed fraction, a pinch reports how far apart the fingers have moved, so the zoom follows it
    /// continuously: spreading them (a scale above 1) zooms in around the point between them.
    /// </summary>
    [JSInvokable]
    public void OnPinchZoom(double fracX, double fracY, double scale)
    {
        if (!double.IsFinite(scale) || scale <= 0) return;
        double factor = 1 / scale;
        foreach (var id in AxesForMode())
        {
            double t = AxisFraction(id, fracX, fracY);
            var (min, max) = CurrentRange(id);
            double cursor = min + t * (max - min);
            ApplyRange(id, cursor - (cursor - min) * factor, cursor + (max - cursor) * factor);
        }
        AfterZoom();
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
            ApplyRange(id, min + lo * span, min + hi * span);
        }
        AfterZoom();
    }

    /// <summary>
    /// How an axis is laid out. Which axis runs across the plot follows the chart's index axis, not the
    /// axis' name: the value axes of a horizontal-bar chart are the horizontal ones. The fallback covers
    /// a scene with no cartesian layout, where nothing will be zoomed anyway.
    /// </summary>
    private (bool Horizontal, bool MinAtFar) Orientation(string id)
        => _scene.AxisOrientations.TryGetValue(id, out var o) ? o : (id == "x", id != "x");

    /// <summary>
    /// Converts an element fraction (0..1) to a 0..1 position along an axis, via the plot area. A
    /// reversed axis runs the other way, so the fraction is flipped with it - otherwise wheel zoom and
    /// pan would move away from the pointer.
    /// </summary>
    private double AxisFraction(string id, double fracX, double fracY)
    {
        var (horizontal, minAtFar) = Orientation(id);
        double t;
        if (_scene.PlotArea is not { } p)
        {
            t = horizontal ? fracX : minAtFar ? 1 - fracY : fracY;
        }
        else if (horizontal)
        {
            double x = fracX * _vw;
            t = p.Width <= 0 ? 0 : Math.Clamp((x - p.Left) / p.Width, 0, 1);
        }
        else
        {
            double y = fracY * _vh;
            double along = p.Height <= 0 ? 0 : Math.Clamp((y - p.Top) / p.Height, 0, 1);
            t = minAtFar ? 1 - along : along;
        }
        return _scene.ReversedAxes.Contains(id) ? 1 - t : t;
    }

    [JSInvokable]
    public void OnPan(double dx, double dy)
    {
        foreach (var id in AxesForMode())
        {
            var (min, max) = CurrentRange(id);
            double span = max - min;
            var (horizontal, minAtFar) = Orientation(id);
            // Dragging moves the data with the pointer, so the range moves against it.
            double delta = horizontal ? -dx * span : (minAtFar ? dy : -dy) * span;
            if (_scene.ReversedAxes.Contains(id)) delta = -delta;
            ApplyRange(id, min + delta, max + delta);
        }
        AfterZoom();
    }

    [JSInvokable]
    public void OnResetZoom() => ResetZoom();

    /// <summary>Clears every zoom/pan override and returns the chart to the full data range.</summary>
    public void ResetZoom()
    {
        if (_state.AxisRanges.Count == 0) return;
        _state.AxisRanges.Clear();
        AfterZoom();
    }

    /// <summary>Zooms an axis to an explicit value range. Pass null to clear that axis's override.</summary>
    public void ZoomTo(string axisId, double? min, double? max)
    {
        if (min is { } lo && max is { } hi && hi > lo) ApplyRange(axisId, lo, hi);
        else _state.AxisRanges.Remove(axisId);
        AfterZoom();
    }

    /// <summary>The visible range of an axis (its zoomed range when zoomed, else the full data range).</summary>
    public (double Min, double Max)? GetAxisRange(string axisId)
        => _scene.AxisRanges.TryGetValue(axisId, out var r) ? r : null;

    // ---- imperative API ----

    /// <summary>
    /// Rebuilds and redraws the chart from its current data and options. Blazor only re-renders a
    /// component when a parameter it can compare changes, so mutating the <see cref="Data"/> object in
    /// place - appending a point to a live series, editing a value - leaves the chart showing the old
    /// scene until this is called. Mirrors Chart.js's <c>chart.update()</c>.
    /// </summary>
    public void Refresh()
    {
        Recompute();
        StateHasChanged();
    }

    /// <summary>Whether a dataset is currently drawn (neither hidden through the legend nor by
    /// <see cref="BitChartDataset.Hidden"/>).</summary>
    public bool IsDatasetVisible(int datasetIndex)
    {
        if (datasetIndex < 0 || datasetIndex >= _config.Data.Datasets.Count) return false;
        return !_config.Data.Datasets[datasetIndex].Hidden && !_state.IsDatasetHidden(datasetIndex);
    }

    /// <summary>
    /// Shows or hides a dataset, exactly as clicking its legend entry would. A dataset whose
    /// <see cref="BitChartDataset.Hidden"/> is set stays hidden: that is the data's own answer, and
    /// this only drives the chart's own visibility state.
    /// </summary>
    public void SetDatasetVisible(int datasetIndex, bool visible)
    {
        if (datasetIndex < 0 || datasetIndex >= _config.Data.Datasets.Count) return;
        bool changed = visible ? _state.HiddenDatasets.Remove(datasetIndex) : _state.HiddenDatasets.Add(datasetIndex);
        if (!changed) return;
        Refresh();
    }

    /// <summary>Flips a dataset between shown and hidden. Mirrors Chart.js's <c>hide</c>/<c>show</c> pair.</summary>
    public void ToggleDataset(int datasetIndex) => SetDatasetVisible(datasetIndex, !IsDatasetVisible(datasetIndex));

    /// <summary>Whether a data index (a pie/doughnut/polar-area slice) is currently drawn.</summary>
    public bool IsDataIndexVisible(int dataIndex) => !_state.IsIndexHidden(dataIndex);

    /// <summary>
    /// Shows or hides one data index across the chart - the slice-level counterpart of
    /// <see cref="SetDatasetVisible"/>, used by the pie/doughnut/polar-area legend. Mirrors Chart.js's
    /// <c>toggleDataVisibility</c>.
    /// </summary>
    public void SetDataIndexVisible(int dataIndex, bool visible)
    {
        if (dataIndex < 0) return;
        bool changed = visible ? _state.HiddenIndices.Remove(dataIndex) : _state.HiddenIndices.Add(dataIndex);
        if (!changed) return;
        Refresh();
    }

    /// <summary>Flips one data index between shown and hidden.</summary>
    public void ToggleDataIndex(int dataIndex) => SetDataIndexVisible(dataIndex, IsDataIndexVisible(dataIndex) is false);

    /// <summary>Brings back every dataset and data index hidden through the legend or the API.</summary>
    public void ResetVisibility()
    {
        if (_state.HiddenDatasets.Count == 0 && _state.HiddenIndices.Count == 0) return;
        _state.HiddenDatasets.Clear();
        _state.HiddenIndices.Clear();
        Refresh();
    }

    /// <summary>
    /// Stores a new range for an axis, honoring the configured zoom limits so the chart can neither be
    /// zoomed in past <see cref="BitChartZoomOptions.MinRangeFraction"/> nor dragged outside the data.
    /// </summary>
    private void ApplyRange(string id, double min, double max)
    {
        if (double.IsNaN(min) || double.IsNaN(max) || max - min <= 1e-9) return;
        var z = _config.Options.Zoom;
        if (_scene.DataRanges.TryGetValue(id, out var full))
        {
            double fullSpan = full.Max - full.Min;
            if (fullSpan > 0)
            {
                double minSpan = fullSpan * Math.Clamp(z.MinRangeFraction, 0, 1);
                if (minSpan > 0 && max - min < minSpan)
                {
                    double c = (min + max) / 2;
                    min = c - minSpan / 2;
                    max = c + minSpan / 2;
                }
                if (z.LimitToData)
                {
                    double span = Math.Min(max - min, fullSpan);
                    if (min < full.Min) { min = full.Min; max = min + span; }
                    if (max > full.Max) { max = full.Max; min = max - span; }
                    min = Math.Max(min, full.Min);
                    max = Math.Min(max, full.Max);
                    if (max - min <= 1e-9) return;
                }
            }
        }
        _state.AxisRanges[id] = (min, max);
    }

    private void AfterZoom()
    {
        _suppressTransition = true;
        Recompute();
        StateHasChanged();
        if (OnZoomChange.HasDelegate) _ = OnZoomChange.InvokeAsync();
    }

    /// <summary>
    /// The axes a gesture moves. The mode names a direction on screen, not an axis id: X is whatever
    /// runs across the plot, which on a horizontal-bar chart is the value axis and not the one called
    /// "x", and which includes a secondary x axis rather than only the primary one.
    /// </summary>
    private IEnumerable<string> AxesForMode()
    {
        var mode = _config.Options.Zoom.Mode;
        foreach (var id in _scene.ZoomableAxes)
        {
            bool horizontal = Orientation(id).Horizontal;
            if (mode == BitChartZoomMode.X && !horizontal) continue;
            if (mode == BitChartZoomMode.Y && horizontal) continue;
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

        _scene = new BitChartRenderer(_config, _state, _vw, _vh, _instanceId).Render();
        RestoreInteraction();

        // Decide whether to (re)play the entry animation. We key off a signature of the data
        // values (not pixel positions), so data changes replay the animation while resize/zoom/pan
        // - which leave the values unchanged - do not.
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

    /// <summary>
    /// Re-points the hover and keyboard position at the freshly built scene. Every element object is
    /// new after a render, so an active tooltip would otherwise blink out on any re-render the reader
    /// did not ask for - a parent's <c>StateHasChanged</c>, a container resize, a zoom step. The
    /// position is re-found by (dataset, index); when the data it pointed at is gone (hidden through
    /// the legend, say) the interaction is simply dropped. Nothing is raised: no one interacted.
    /// </summary>
    private void RestoreInteraction()
    {
        var anchor = _hoverAnchor;
        bool forceIndex = _hoverForceIndex;
        var focusKey = _focusKey;

        ClearHover();
        _focusIndex = -1;

        if (focusKey is { } fk)
        {
            int at = _scene.Elements.FindIndex(e => e.DatasetIndex == fk.Ds && e.DataIndex == fk.Di);
            if (at >= 0)
            {
                _focusIndex = at;
                var el = _scene.Elements[at];
                BuildHover(el);
                _hoverNodes.Add(FocusOutline(el));
                _liveMessage = Describe(el);
                return;
            }
            _focusKey = null;
            _liveMessage = null;
            // The data being walked is gone - hidden through the legend, or removed. Anyone tracking the
            // active element has to be told, or they keep showing a reading the chart no longer has.
            NotifyHover();
            return;
        }

        if (anchor is not { } a) return;

        if (_scene.Elements.FirstOrDefault(e => e.DatasetIndex == a.Ds && e.DataIndex == a.Di) is { } hovered)
            BuildHover(hovered, forceIndex);
        else
            NotifyHover();
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
        BuildHover(e);
        NotifyHover();
    }

    /// <summary>
    /// Hovering the empty part of the plot activates the whole index under the pointer, which is what
    /// makes lines drawn without markers - and thin bars - reachable.
    /// </summary>
    private void OnEnterBand(BitChartHitBand band)
    {
        if (RepresentativeOf(band) is not { } rep) return;
        BuildHover(rep, forceIndexGroup: true);
        NotifyHover();
    }

    /// <summary>The element a band stands for: the first one at that index.</summary>
    private BitChartDataElement? RepresentativeOf(BitChartHitBand band)
        => _scene.Elements.FirstOrDefault(el => el.DataIndex == band.DataIndex);

    /// <summary>
    /// Clicking the plate between the elements reports the index under the pointer, matching what
    /// hovering there already does - with non-intersecting interaction the whole plot is the target,
    /// so a click that lands beside a thin line should not be silently dropped.
    /// </summary>
    private async Task OnClickBand(BitChartHitBand band)
    {
        if (RepresentativeOf(band) is { } rep) await OnClickElement(rep);
    }

    private void OnLeave()
    {
        ClearHover();
        NotifyHover();
    }

    /// <summary>
    /// A touch screen never hovers, so a tap has to do the work <c>mouseenter</c> does with a mouse.
    /// Only non-mouse pointers are handled: a mouse has already hovered by the time it presses, and
    /// re-running the hover there would rebuild the tooltip twice for one click.
    /// </summary>
    private void OnPointerDownElement(PointerEventArgs args, BitChartDataElement e)
    {
        if (IsMouse(args)) return;
        OnEnter(e);
    }

    private void OnPointerDownBand(PointerEventArgs args, BitChartHitBand band)
    {
        if (IsMouse(args)) return;
        OnEnterBand(band);
    }

    private static bool IsMouse(PointerEventArgs args)
        => string.IsNullOrEmpty(args.PointerType) || args.PointerType == "mouse";

    private void OnBlur()
    {
        if (_focusIndex < 0) return;
        _focusIndex = -1;
        _focusKey = null;
        ClearHover();
        _liveMessage = null;
        NotifyHover();
    }

    private void NotifyHover()
    {
        if (OnElementHover.HasDelegate) _ = OnElementHover.InvokeAsync(_tooltipContext);
    }

    private void ClearHover()
    {
        _active.Clear();
        _activeTooltip = null;
        _tooltipContext = null;
        _hoverNodes.Clear();
        _hoverAnchor = null;
    }

    private void BuildHover(BitChartDataElement e, bool forceIndexGroup = false)
    {
        _active.Clear();
        _hoverNodes.Clear();
        _hoverAnchor = (e.DatasetIndex, e.DataIndex);
        _hoverForceIndex = forceIndexGroup;
        var tip = _config.Options.Plugins.Tooltip;
        var mode = tip.Mode ?? _config.Options.Interaction.Mode;

        IEnumerable<BitChartDataElement> group =
            forceIndexGroup && !_scene.IsRadialOrCircular && mode != BitChartInteractionMode.Dataset
                ? _scene.Elements.Where(x => x.DataIndex == e.DataIndex)
                : mode switch
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

        // ---- Tooltip callbacks (title / body extras / footer / label color) + filter/sort ----
        var cb = tip.Callbacks;
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

        if (tip.Filter is { } filter)
        {
            for (int i = items.Count - 1; i >= 0; i--)
                if (!filter(items[i])) { items.RemoveAt(i); ordered.RemoveAt(i); }
        }
        if (tip.ItemSort is { } sort)
        {
            var pairs = items.Zip(ordered).ToList();
            pairs.Sort((a, b) => sort(a.First, b.First));
            items = pairs.Select(p => p.First).ToList();
            ordered = pairs.Select(p => p.Second).ToList();
        }

        foreach (var el in ordered)
            combined.Items.AddRange(el.Tooltip.Items);

        if (cb.Title?.Invoke(items) is { } titleText) combined.Title = titleText;
        if (cb.BeforeBody?.Invoke(items) is { } bb) combined.BeforeBody.AddRange(bb.Split('\n'));
        if (cb.AfterBody?.Invoke(items) is { } ab) combined.AfterBody.AddRange(ab.Split('\n'));
        if (cb.Footer?.Invoke(items) is { } ft) combined.Footer.AddRange(ft.Split('\n'));
        if (cb.LabelColor is not null)
            for (int i = 0; i < combined.Items.Count && i < items.Count; i++)
                if (cb.LabelColor(items[i]) is { } lc) combined.Items[i].Color = lc;

        // Positioner. Averaging runs along the axis the active items share - the index axis - so on a
        // horizontal-bar chart that is the vertical one, and the tooltip is put beside the group rather
        // than in the middle of it.
        if (tip.Position == BitChartTooltipPositioner.Average && _active.Count > 0)
        {
            if (_config.Options.IndexAxis == BitChartIndexAxis.Y && !_scene.IsRadialOrCircular)
            {
                combined.AnchorX = _active.Max(a => a.Tooltip.AnchorX);
                combined.AnchorY = _active.Average(a => a.CenterY);
            }
            else
            {
                combined.AnchorX = _active.Average(a => a.CenterX);
                combined.AnchorY = _active.Min(a => a.Tooltip.AnchorY);
            }
        }
        _activeTooltip = combined;

        // Context for a custom template.
        _tooltipContext = new BitChartTooltipContext
        {
            Title = combined.Title,
            Points = ordered.Select(a => new BitChartTooltipPoint
            {
                DatasetIndex = a.DatasetIndex,
                DataIndex = a.DataIndex,
                Label = a.SeriesLabel,
                Value = a.Value,
                Color = a.Tooltip.Items.FirstOrDefault()?.Color ?? "#000",
                FormattedValue = a.Tooltip.Items.FirstOrDefault()?.Text ?? ""
            }).ToList()
        };

        // Crosshair for index-style highlighting.
        var interaction = _config.Options.Interaction;
        bool indexMode = (forceIndexGroup || mode is BitChartInteractionMode.Index or BitChartInteractionMode.X)
            && !_scene.IsRadialOrCircular;
        if (indexMode && interaction.Crosshair && _scene.PlotArea is { } pa)
        {
            bool horizontalIndex = _config.Options.IndexAxis == BitChartIndexAxis.Y;
            _hoverNodes.Add(horizontalIndex
                ? new BitChartSvgLine
                {
                    X1 = pa.Left, Y1 = e.CenterY, X2 = pa.Right, Y2 = e.CenterY,
                    Stroke = interaction.CrosshairColor, StrokeWidth = 1, Dash = "4,3"
                }
                : new BitChartSvgLine
                {
                    X1 = e.CenterX, Y1 = pa.Top, X2 = e.CenterX, Y2 = pa.Bottom,
                    Stroke = interaction.CrosshairColor, StrokeWidth = 1, Dash = "4,3"
                });

            if (interaction.CrosshairLabel && e.Tooltip.Title is { Length: > 0 } indexLabel)
                AddCrosshairLabel(indexLabel, e, pa, horizontalIndex);
        }

        // The renderer precomputed each element's hover appearance, so highlighting costs no re-layout.
        foreach (var el in ordered)
            if (el.HoverShape is { } hs)
                _hoverNodes.Add(hs);
    }

    private const string FocusRingColor = "var(--bit-clr-pri, #0078d4)";

    /// <summary>
    /// Draws the active index in a chip where the crosshair meets the index axis. It reuses the tooltip
    /// colors so the two read as one piece of chrome, and is clamped into the plot so it cannot spill
    /// out at either end.
    /// </summary>
    private void AddCrosshairLabel(string text, BitChartDataElement e, BitChartArea pa, bool horizontalIndex)
    {
        var t = _config.Options.Plugins.Tooltip;
        double fontSize = t.BodyFont.Size;
        double w = BitChartTextMeasure.Width(text, fontSize) + 10;
        double h = fontSize + 6;

        double cx, cy;
        if (horizontalIndex)
        {
            // A chip wider or taller than the room it is clamped into would leave Math.Clamp with a
            // minimum above its maximum, so each upper bound is held at or above its lower one.
            cx = Math.Clamp(pa.Left - w / 2 - 4, w / 2, Math.Max(w / 2, _vw - w / 2));
            cy = Math.Clamp(e.CenterY, pa.Top + h / 2, Math.Max(pa.Top + h / 2, pa.Bottom - h / 2));
        }
        else
        {
            cx = Math.Clamp(e.CenterX, pa.Left + w / 2, Math.Max(pa.Left + w / 2, pa.Right - w / 2));
            cy = Math.Min(pa.Bottom + h / 2 + 3, _vh - h / 2);
        }

        _hoverNodes.Add(new BitChartSvgRect
        {
            X = cx - w / 2, Y = cy - h / 2, Width = w, Height = h,
            Rx = 3, Fill = t.BackgroundColor
        });
        _hoverNodes.Add(new BitChartSvgText
        {
            X = cx, Y = cy, Text = text, Fill = t.TitleColor,
            FontFamily = t.BodyFont.Family, FontSize = fontSize,
            Anchor = "middle", Baseline = "central"
        });
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
            // Left/right walk the series the reader is on; up/down step between the series at the same
            // category, which is how a multi-series chart is actually compared.
            case "ArrowRight":
                MoveWithinSeries(1);
                break;
            case "ArrowLeft":
                MoveWithinSeries(-1);
                break;
            case "ArrowDown":
                MoveAcrossSeries(1);
                break;
            case "ArrowUp":
                MoveAcrossSeries(-1);
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
                _focusKey = null;
                ClearHover();
                _liveMessage = null;
                NotifyHover();
                break;
        }
    }

    /// <summary>Steps to the next/previous element of the series the reader is currently on, wrapping
    /// within it. With nothing focused yet it enters the chart at the appropriate end.</summary>
    private void MoveWithinSeries(int dir)
    {
        int n = _scene.Elements.Count;
        if (_focusIndex < 0 || _focusIndex >= n) { SetFocus(dir > 0 ? 0 : n - 1); return; }

        int current = _scene.Elements[_focusIndex].DatasetIndex;
        var series = new List<int>();
        for (int i = 0; i < n; i++)
            if (_scene.Elements[i].DatasetIndex == current) series.Add(i);

        int at = series.IndexOf(_focusIndex);
        if (at < 0) { SetFocus((_focusIndex + dir + n) % n); return; }
        SetFocus(series[(at + dir + series.Count) % series.Count]);
    }

    /// <summary>
    /// Steps to the neighbouring series at the same category. When that series has nothing at this
    /// category - a null, or a shorter series - the nearest category it does have is taken, so the keys
    /// never dead-end. A chart of one series has nothing to step between, so the vertical keys keep
    /// walking the data instead of doing nothing.
    /// </summary>
    private void MoveAcrossSeries(int dir)
    {
        int n = _scene.Elements.Count;
        if (_focusIndex < 0 || _focusIndex >= n) { SetFocus(dir > 0 ? 0 : n - 1); return; }

        var current = _scene.Elements[_focusIndex];
        var datasets = _scene.Elements.Select(x => x.DatasetIndex).Distinct().OrderBy(i => i).ToList();
        if (datasets.Count < 2) { MoveWithinSeries(dir); return; }

        int at = datasets.IndexOf(current.DatasetIndex);
        int target = datasets[(at + dir + datasets.Count) % datasets.Count];

        int best = -1, bestDistance = int.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var el = _scene.Elements[i];
            if (el.DatasetIndex != target) continue;
            int distance = Math.Abs(el.DataIndex - current.DataIndex);
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = i;
        }
        if (best >= 0) SetFocus(best);
    }

    private void SetFocus(int i)
    {
        _focusIndex = i;
        var el = _scene.Elements[i];
        _focusKey = (el.DatasetIndex, el.DataIndex);
        BuildHover(el);
        _hoverNodes.Add(FocusOutline(el));
        _liveMessage = Describe(el);
        NotifyHover();
    }

    /// <summary>
    /// The ring drawn around the keyboard position. It traces the element's own outline rather than a
    /// stand-in shape: a rounded bar and an arc are both paths, and a circle around an arc's centroid
    /// would sit inside the ring it is supposed to be marking.
    /// </summary>
    private static BitChartSvgNode FocusOutline(BitChartDataElement el) => el.Shape switch
    {
        BitChartSvgRect r => new BitChartSvgRect { X = r.X - 2, Y = r.Y - 2, Width = r.Width + 4, Height = r.Height + 4, Fill = "none", Stroke = FocusRingColor, StrokeWidth = 2, CssClass = "bit-cht-focus-ring" },
        BitChartSvgCircle c => new BitChartSvgCircle { Cx = c.Cx, Cy = c.Cy, R = c.R + 5, Fill = "none", Stroke = FocusRingColor, StrokeWidth = 2, CssClass = "bit-cht-focus-ring" },
        BitChartSvgPath p => new BitChartSvgPath { D = p.D, Fill = "none", Stroke = FocusRingColor, StrokeWidth = 2, CssClass = "bit-cht-focus-ring" },
        BitChartSvgPolygon poly => new BitChartSvgPolygon { Points = [.. poly.Points], Closed = poly.Closed, Fill = "none", Stroke = FocusRingColor, StrokeWidth = 2, CssClass = "bit-cht-focus-ring" },
        _ => new BitChartSvgCircle { Cx = el.CenterX, Cy = el.CenterY, R = 8, Fill = "none", Stroke = FocusRingColor, StrokeWidth = 2, CssClass = "bit-cht-focus-ring" }
    };

    /// <summary>
    /// What the live region says about the focused element. The position is included because a reader
    /// stepping through the data has no other way to tell how far along they are, and it is counted
    /// within the series - the run the left/right keys actually walk. Which series they are on is
    /// announced only when there is more than one to be on.
    /// </summary>
    private string Describe(BitChartDataElement el)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(el.Tooltip.Title)) parts.Add(el.Tooltip.Title!);
        foreach (var item in el.Tooltip.Items) parts.Add(item.Text);

        var series = _scene.Elements.Where(x => x.DatasetIndex == el.DatasetIndex).ToList();
        int at = series.IndexOf(el);
        parts.Add($"{(at < 0 ? 1 : at + 1)} of {series.Count}");

        var datasets = _scene.Elements.Select(x => x.DatasetIndex).Distinct().OrderBy(i => i).ToList();
        if (datasets.Count > 1)
            parts.Add($"series {datasets.IndexOf(el.DatasetIndex) + 1} of {datasets.Count}");

        return string.Join(", ", parts);
    }

    private async Task ToggleLegend(BitChartLegendItemModel item)
    {
        if (OnLegendItemClick.HasDelegate) await OnLegendItemClick.InvokeAsync(item);
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

    private string TableId => $"{_instanceId}-table";

    private string HintId => $"{_instanceId}-hint";

    /// <summary>True when the keyboard hint is worth rendering: there is data to walk and text to say it with.</summary>
    private bool ShowNavigationHint => !string.IsNullOrWhiteSpace(NavigationHint) && _scene.Elements.Count > 0;

    /// <summary>
    /// What the chart points its <c>aria-describedby</c> at: the how-to-navigate sentence and the data
    /// table, in that order, and null when it has neither.
    /// </summary>
    private string? DescribedBy
    {
        get
        {
            if (ShowNavigationHint && GenerateTable) return $"{HintId} {TableId}";
            if (ShowNavigationHint) return HintId;
            return GenerateTable ? TableId : null;
        }
    }

    private string? ClipId => _scene.PlotArea is null ? null : $"{_instanceId}-clip";
    private string? ClipRef => ClipId is null ? null : $"url(#{ClipId})";

    private CultureInfo Culture => _config.Options.Culture ?? CultureInfo.InvariantCulture;

    private string Fmt(double v) => v.ToString(Culture);

    /// <summary>
    /// One cell of the screen-reader table. The tooltip names an error interval beside the value, so the
    /// table - which is what a reader has instead of the tooltip - has to name it too.
    /// </summary>
    private string CellText(BitChartDataset ds, int dataIndex)
    {
        if (dataIndex >= ds.Data.Count || ds.Data[dataIndex] is not { } value) return "";
        string text = Fmt(value);
        if (ds.ErrorData is not { } errors || dataIndex >= errors.Count || errors[dataIndex] is not { } e) return text;

        double minus = Math.Abs(e.Minus), plus = Math.Abs(e.Plus);
        if (minus <= 0 && plus <= 0) return text;
        return e.IsSymmetric ? $"{text} ±{Fmt(plus)}" : $"{text} +{Fmt(plus)}/-{Fmt(minus)}";
    }

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

    private string RootClass
    {
        get
        {
            string c = "bit-cht";
            if (RespectReducedMotion) c += " bit-cht-rm";
            if (!string.IsNullOrEmpty(Class)) c += " " + Class;
            return c;
        }
    }

    private string DataGroupClass
    {
        get
        {
            if (!CanAnimate) return "bit-cht-data";
            // Progressive draw: points reveal individually (per-element delay tied to x position),
            // so the group itself carries no animation.
            if (ProgressiveDraw) return "bit-cht-data";
            // When staggering, the individual elements animate (with per-element delays) instead of
            // the whole group, so the group itself carries no animation.
            if (Staggered) return "bit-cht-data";
            if (_scene.IsRadialOrCircular)
                return "bit-cht-data bit-cht-anim bit-cht-anim-grow";
            // Bars grow from the baseline as a size change, in the correct direction for the orientation.
            if (_scene.HasBars)
                return _scene.HorizontalBars ? "bit-cht-data bit-cht-anim bit-cht-anim-bars-h" : "bit-cht-data bit-cht-anim bit-cht-anim-bars-v";
            // Line/scatter points rise in.
            return "bit-cht-data bit-cht-anim bit-cht-anim-rise";
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

    /// <summary>Class for the series (line/area) group - animates with the same proven group mechanism.
    /// Radial/circular charts (e.g. radar) grow from the center so the web matches its joint points.</summary>
    private string SeriesGroupClass =>
        CanAnimate
            ? ProgressiveDraw
                // The stroke draws itself on (bit-cht-draw) and fills fade in, both at the path level,
                // so the group must not also rise.
                ? "bit-cht-series"
                : _scene.IsRadialOrCircular
                    ? "bit-cht-series bit-cht-anim bit-cht-anim-grow"
                    : "bit-cht-series bit-cht-anim bit-cht-anim-rise"
            : "bit-cht-series";

    private string ElementClass(BitChartDataElement el)
    {
        string c = "bit-cht-el";
        // A state hook only: the active look itself is the precomputed hover shape drawn over the
        // element, so the class carries no rule of its own and is there for consumers to style.
        if (IsActive(el)) c += " bit-cht-active";
        if (ProgressiveDraw)
        {
            // Each point pops in as the drawing stroke reaches it (delay set in ElementStyle).
            c += " bit-cht-el-anim bit-cht-el-rise";
        }
        else if (Staggered)
        {
            // Bars reuse the proven view-box scaling classes (with an explicit per-element pixel
            // transform-origin set in ElementStyle); points/markers rise in via the fill-box class.
            c += _scene.HasBars
                ? _scene.HorizontalBars ? " bit-cht-anim bit-cht-anim-bars-h" : " bit-cht-anim bit-cht-anim-bars-v"
                : " bit-cht-el-anim bit-cht-el-rise";
        }
        return c;
    }

    private string ElementStyle(int index)
    {
        string cursor = OnElementClick.HasDelegate ? "cursor:pointer" : "cursor:default";
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
            return $"{cursor};animation-delay:{BitChartSvg.N(pDelay)}ms;animation-duration:{BitChartSvg.N(elDur)}ms";
        }
        if (!Staggered) return cursor;
        double delay = index * _config.Options.Animation.DelayBetween;
        string s = $"{cursor};animation-delay:{BitChartSvg.N(delay)}ms";
        if (_scene.HasBars)
        {
            // bit-cht-anim-bars-* use transform-box: view-box, so the origin must be given in view-box
            // pixels pinned to the value-axis baseline (matching the non-staggered group behaviour).
            var el = _scene.Elements[index];
            s += _scene.HorizontalBars
                ? $";transform-origin:{BitChartSvg.N(_scene.BarBaseline)}px {BitChartSvg.N(el.CenterY)}px"
                : $";transform-origin:{BitChartSvg.N(el.CenterX)}px {BitChartSvg.N(_scene.BarBaseline)}px";
        }
        return s;
    }

    private string AnimStyle =>
        $"--bit-cht-dur:{_config.Options.Animation.Duration}ms;--bit-cht-ease:{_config.Options.Animation.Easing}";

    private bool IsActive(BitChartDataElement e) => _active.Count > 0 && _active.Contains(e);

    /// <summary>
    /// Places the tooltip above its anchor and keeps the whole box inside the plot: it flips below when
    /// there is no room above, and slides horizontally so it never spills out of (and gets clipped by)
    /// the chart container. The caret follows the anchor so it keeps pointing at the data.
    /// </summary>
    private (string Style, string CaretStyle) TooltipPlacement(BitChartTooltipInfo tt)
    {
        var t = _config.Options.Plugins.Tooltip;
        double caret = t.Caret ? t.CaretSize : 0;
        double gap = caret + 4;

        // Estimated box size: the tooltip is measured from its own text because the browser layout is
        // not available on the server, and this only needs to be good enough to pick a side.
        double fontW = t.BodyFont.Size;
        double width = 0;
        if (!string.IsNullOrEmpty(tt.Title))
            width = BitChartTextMeasure.Width(tt.Title, t.TitleFont.Size, t.TitleFont.Weight);
        foreach (var item in tt.Items)
            width = Math.Max(width, BitChartTextMeasure.Width(item.Text, fontW) + (t.DisplayColors ? 16 : 0));
        foreach (var line in tt.BeforeBody.Concat(tt.AfterBody).Concat(tt.Footer))
            width = Math.Max(width, BitChartTextMeasure.Width(line, fontW));
        width += t.Padding * 3;

        int lines = (string.IsNullOrEmpty(tt.Title) ? 0 : 1) + tt.Items.Count
            + tt.BeforeBody.Count + tt.AfterBody.Count + tt.Footer.Count;
        double height = Math.Max(1, lines) * (fontW * 1.5) + t.Padding * 2 + (tt.Footer.Count > 0 ? 8 : 0);

        // Anchor in view-box units, then clamp the box into the plot box.
        double ax = tt.AnchorX, ay = tt.AnchorY;
        bool below = ay - height - gap < 0;
        double top = below ? ay + gap : ay - height - gap;
        double left = ax - width / 2;
        left = Math.Clamp(left, 2, Math.Max(2, _vw - width - 2));
        top = Math.Clamp(top, 2, Math.Max(2, _vh - height - 2));

        // Percentages keep the tooltip aligned with the SVG, which scales with the container. An
        // explicit MaxWidth also lets the text wrap, which is what makes a prose tooltip readable.
        double maxWidth = t.MaxWidth ?? Math.Max(80, _vw - 8);
        string style =
            $"left:{BitChartSvg.N(Pct(left, _vw))}%;top:{BitChartSvg.N(Pct(top, _vh))}%;" +
            $"transform:translateZ(0);max-width:{BitChartSvg.N(maxWidth)}px" +
            (t.MaxWidth is null ? "" : ";white-space:normal");

        // The caret sits on the edge facing the anchor, at the anchor's horizontal position.
        double caretLeft = Math.Clamp(ax - left, caret + 2, Math.Max(caret + 2, width - caret - 2));
        string caretStyle = caret <= 0
            ? "display:none"
            : below
                ? $"left:{BitChartSvg.N(caretLeft - caret)}px;top:{BitChartSvg.N(-caret * 2)}px;border-width:{BitChartSvg.N(caret)}px;border-bottom-color:{t.BackgroundColor}"
                : $"left:{BitChartSvg.N(caretLeft - caret)}px;bottom:{BitChartSvg.N(-caret * 2)}px;border-width:{BitChartSvg.N(caret)}px;border-top-color:{t.BackgroundColor}";

        return (style, caretStyle);
    }

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

    private static double Pct(double v, double total) => total <= 0 ? 0 : v / total * 100;

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

    /// <summary>Total rows the data table would render without a cap.</summary>
    private int TableRowCount => HasPointData
        ? _config.Data.Datasets.Sum(d => d.Points?.Count ?? 0)
        : _config.Data.Datasets.Count;

    private bool TableTruncated => MaxTableRows > 0 && TableRowCount > MaxTableRows;

    /// <summary>Total columns the value table would render without a cap (one per category).</summary>
    private int TableColumnCount => HasPointData
        ? 3
        : Math.Max(_config.Data.Labels.Count, _config.Data.Datasets.Count == 0 ? 0 : _config.Data.Datasets.Max(d => d.Count));

    /// <summary>The number of category columns actually rendered.</summary>
    private int TableColumnLimit => HasPointData || MaxTableColumns <= 0
        ? int.MaxValue
        : MaxTableColumns;

    private bool TableColumnsTruncated => !HasPointData && MaxTableColumns > 0 && TableColumnCount > MaxTableColumns;

    /// <summary>Caption of the screen-reader table, which also carries the truncation notice.</summary>
    private string TableCaption
    {
        get
        {
            var caption = ChartAriaLabel;
            if (TableTruncated)
                caption += $" Showing the first {MaxTableRows.ToString("N0", Culture)} of {TableRowCount.ToString("N0", Culture)} rows.";
            if (TableColumnsTruncated)
                caption += $" Showing the first {MaxTableColumns.ToString("N0", Culture)} of {TableColumnCount.ToString("N0", Culture)} columns.";
            return caption;
        }
    }

    /// <summary>
    /// The side a title actually renders on. Left and right titles run down the side of the plot
    /// (rotated); anything that is not one of the four sides falls back to the top.
    /// </summary>
    private static BitChartPosition TitleSide(BitChartTitleModel title) => title.Position switch
    {
        BitChartPosition.Bottom => BitChartPosition.Bottom,
        BitChartPosition.Left => BitChartPosition.Left,
        BitChartPosition.Right => BitChartPosition.Right,
        _ => BitChartPosition.Top
    };

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
        GC.SuppressFinalize(this);
    }
}
