using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Bit.BlazorUI;

/// <summary>
/// BitResourceTimeline is a high-performance resource timeline (planner / scheduler): a horizontal time axis, resource
/// rows down the vertical axis and allocation bars drawn in between. It renders wide time ranges with large numbers of
/// bars through a canvas, SVG or HTML renderer, and supports zooming, keyboard navigation, marquee and keyboard
/// selection, editing (move, resize, create, copy, paste and delete), nested resource groups, time-zone-aware axes,
/// streamed and on-demand (windowed) data loading, and templated resource rows and tooltips.
/// </summary>
public partial class BitResourceTimeline : BitComponentBase
{
    // The base keyboard shortcuts of the timeline, plus the editing ones while editing is on, advertised through
    // aria-keyshortcuts.
    private const string BaseKeyShortcuts = "ArrowLeft ArrowRight ArrowUp ArrowDown Home End PageUp PageDown Enter Escape";
    private const string EditingKeyShortcuts = " Alt+ArrowLeft Alt+ArrowRight Alt+ArrowUp Alt+ArrowDown Alt+Shift+ArrowLeft Alt+Shift+ArrowRight Alt+Shift+ArrowUp Alt+Shift+ArrowDown";
    private const string DeleteKeyShortcuts = " Delete Backspace";
    private const string CopyKeyShortcuts = " Control+c Control+v Meta+c Meta+v";
    private const string DisabledKeyShortcuts = "PageUp PageDown";

    private static readonly BitResourceTimelineTexts _defaultTexts = new();

    // The corner size the engine starts with, replaced with the layout it actually works with once it has started.
    private BitResourceTimelineLayout _layout = new(150, 60, 40);

    private ElementReference _viewportRef;
    private ElementReference _resourceOverlayInnerRef;

    // The visible rows the engine reports for the overlay of the ResourceTemplate, kept in step by OnResourceRowsChanged.
    private IReadOnlyList<BitResourceTimelineResourceRow> _resourceRows = [];
    private Dictionary<string, BitResourceTimelineResource> _resourcesById = [];

    // The instances of the loaded Config by id. The interop exchanges ids only - it keeps the payloads small (a large
    // marquee selection would otherwise pass the message size limit of a Blazor Server circuit) - and the ids are
    // resolved back to the app's own instances here, so they can be compared by reference against its own data.
    private Dictionary<string, BitResourceTimelineAllocation> _allocationsById = [];

    private DotNetObjectReference<BitResourceTimeline>? _dotnetObj;
    private bool _started;
    // Whether the engine was told to leave the resource labels to the overlay of the ResourceTemplate, and to report the
    // hovered bar for the TooltipTemplate - kept in step with the templates after every render (see SyncTemplatesAsync).
    private bool _resourceTemplateEnabled;
    private bool _tooltipTemplateEnabled;
    private bool _dataLoaded;
    private bool _isLoading;
    private bool _warnedMissingCreate;
    private bool _warnedMissingCopy;
    private BitResourceTimelineAllocation? _hoverAllocation;
    private double _hoverClientX;
    private double _hoverClientY;

    // Serializes the data loads, so a rapid swap of the Config cannot interleave two of them and race _isLoading and
    // _loadedConfig.
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    // The Config last pushed to the engine, compared by reference so a render of the parent that changes no data does not
    // marshal it again.
    private BitResourceTimelineConfig? _loadedConfig;

    // The Config currently being loaded. A render of the parent in that window (an OnViewChanged handler, a theme, the
    // overlay's layout) would otherwise queue a second load of the same instance and flash the loading overlay again.
    private BitResourceTimelineConfig? _loadingConfig;

    // What the engine was last configured with, compared the same way so a render changing none of them sends nothing.
    private BitResourceTimelineOptions? _loadedOptions;
    private BitResourceTimelineTexts? _loadedTexts;
    private bool _loadedDisabled;

    // Serializes the window fetches of the windowed mode, and tracks the newest request so a slow fetch a newer one has
    // superseded is dropped instead of being pushed to the engine.
    private readonly SemaphoreSlim _windowGate = new(1, 1);
    private long _latestWindowRequestId;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the resource timeline component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple resource timeline
    /// components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitResourceTimelineParams.ParamName)]
    public BitResourceTimelineParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the resource timeline.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitResourceTimelineClassStyles? Classes { get; set; }

    /// <summary>
    /// The data the timeline renders: the resource rows, the time range and the allocation bars. The timeline renders
    /// once this is set, and again whenever a new instance is assigned (it is compared by reference, so a Config changed
    /// in place needs a <see cref="ReloadAsync"/>).
    /// </summary>
    [Parameter] public BitResourceTimelineConfig? Config { get; set; }

    /// <summary>
    /// Enables the on-demand (windowed) loading of the bars. When set, the bars are not taken from the
    /// <see cref="BitResourceTimelineConfig.Allocations"/>: the timeline calls this with the time window it currently
    /// needs (the visible range plus a buffer) and expects every allocation overlapping it, across all the resources. It
    /// is called again as the user scrolls or zooms near the edge of the loaded window, which keeps the memory and the
    /// interop bounded for effectively unbounded data. The <see cref="Config"/> still supplies the resources and the
    /// overall time range. Leave it null to load all the allocations up front (the default).
    /// </summary>
    [Parameter]
    public Func<BitResourceTimelineWindow, Task<IReadOnlyList<BitResourceTimelineAllocation>>>? LoadAllocationsAsync { get; set; }

    /// <summary>
    /// The maximum number of allocations sent to the browser per interop call. A larger data set is streamed to the
    /// engine in batches of this size instead of one large call, which keeps every interop message bounded (a single
    /// payload of several megabytes would block the circuit of Blazor Server) and lets the circuit breathe between the
    /// batches. Zero or a negative value always sends everything in a single call.
    /// <br />
    /// The default value is 10000.
    /// </summary>
    [Parameter] public int LoadBatchSize { get; set; } = 10_000;

    /// <summary>
    /// The custom content of the overlay shown while the engine draws the bars. A default spinner is shown when it is not
    /// set.
    /// </summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>
    /// The minimum time in milliseconds the loading overlay stays visible. Rendering often completes within a frame or
    /// two, which is too brief to perceive; a small value (300, say) keeps the overlay on screen long enough to be seen.
    /// <br />
    /// The default value is 0 (no artificial delay).
    /// </summary>
    [Parameter] public int LoadingMinDurationMs { get; set; }

    /// <summary>
    /// Raised after an allocation is moved or resized (only while editing is enabled through
    /// <see cref="BitResourceTimelineOptions.Editable"/>), and after a bar drawn on empty content was created through
    /// <see cref="OnAllocationCreating"/>. The argument is the very instance supplied in the
    /// <see cref="Config"/>, already updated in place with its new <see cref="BitResourceTimelineAllocation.StartTime"/>,
    /// <see cref="BitResourceTimelineAllocation.EndTime"/> and <see cref="BitResourceTimelineAllocation.ResourceId"/>,
    /// so a handler only needs to persist the change. Raised only after a successful commit; an edit is refused without
    /// a reload by returning false from <see cref="OnAllocationChanging"/>.
    /// </summary>
    [Parameter] public EventCallback<BitResourceTimelineAllocation> OnAllocationChanged { get; set; }

    /// <summary>
    /// Called after a move or resize of a single bar is previewed onto the allocation instance and before it is
    /// committed. Returning false restores the previous resource and times (without a <see cref="ReloadAsync"/>). When it
    /// is not set the edit goes to <see cref="OnAllocationsChanging"/>, and is accepted when neither is set. Not an
    /// EventCallback, since those cannot return a value.
    /// </summary>
    [Parameter] public Func<BitResourceTimelineAllocationChange, Task<bool>>? OnAllocationChanging { get; set; }

    /// <summary>
    /// Called when the user finishes drawing a new bar (a drag on empty content with
    /// <see cref="BitResourceTimelineEmptyDragAction.Create"/>). Returns the complete allocation, with its
    /// <see cref="BitResourceTimelineAllocation.Id"/> set, or null to cancel; the timeline never invents ids. When it is
    /// not set the drawn bar is discarded.
    /// </summary>
    [Parameter] public Func<BitResourceTimelineCreateRequest, Task<BitResourceTimelineAllocation?>>? OnAllocationCreating { get; set; }

    /// <summary>
    /// Called for every commit that changes several bars at once (the selected bars dragged together), and for a
    /// single-bar commit while <see cref="OnAllocationChanging"/> is not set. Returning false restores every bar of the
    /// list. When it is not set a multi-bar edit is accepted.
    /// </summary>
    [Parameter] public Func<IReadOnlyList<BitResourceTimelineAllocationChange>, Task<bool>>? OnAllocationsChanging { get; set; }

    /// <summary>
    /// Called on paste (Ctrl/Cmd+V) with the bars of the clipboard and a suggested offset. Returns the new allocations
    /// (with new ids) to insert, or null or an empty list to cancel; the timeline never invents ids.
    /// </summary>
    [Parameter] public Func<BitResourceTimelineCopyRequest, Task<IReadOnlyList<BitResourceTimelineAllocation>?>>? OnAllocationsCopying { get; set; }

    /// <summary>
    /// Called before the selected (or focused) bars are removed through Delete/Backspace while
    /// <see cref="BitResourceTimelineOptions.AllowDelete"/> is set. Returning false keeps them. When it is not set the
    /// delete is accepted.
    /// </summary>
    [Parameter] public Func<IReadOnlyList<string>, Task<bool>>? OnAllocationsDeleting { get; set; }

    /// <summary>
    /// Raised on a click or a stationary tap that is not a drag, a marquee or a committed edit. The arguments say what was
    /// under the pointer - the bar (if any), the hidden bars when a +N label was hit, the resource row and the time -
    /// with the surface and viewport coordinates and the modifier keys. Raised for bars, empty content, both sticky axes
    /// and the corner. The selection is updated first (a click on a bar selects it, one on empty content clears it). A
    /// double-click raises it once per click, then <see cref="OnDoubleClick"/>.
    /// </summary>
    [Parameter] public EventCallback<BitResourceTimelinePointerArgs> OnClick { get; set; }

    /// <summary>
    /// Raised on a right-click of the timeline (the native menu of the browser is suppressed either way). The arguments
    /// say what was under the pointer - the bar (if any), the resource row and the time - with the surface and viewport
    /// coordinates, so a handler can render its own context menu there. Raised for bars, empty content and the rows of
    /// the resource axis; not for the time axis or the corner. A right-click does not change the selection.
    /// </summary>
    [Parameter] public EventCallback<BitResourceTimelineContextMenuArgs> OnContextMenu { get; set; }

    /// <summary>
    /// Raised on the second click of a double-click (or a double-tap), with the same arguments as
    /// <see cref="OnClick"/>, in addition to it. Typically opens an editor for the bar under the pointer, or creates an
    /// allocation at the clicked time and resource.
    /// </summary>
    [Parameter] public EventCallback<BitResourceTimelinePointerArgs> OnDoubleClick { get; set; }

    /// <summary>
    /// Raised after the user finishes resizing the resource column (a pointer release, or a keyboard step on the
    /// divider), with the new width in pixels, already clamped to
    /// <see cref="BitResourceTimelineOptions.ResourceAxisMinWidth"/> / <see cref="BitResourceTimelineOptions.ResourceAxisMaxWidth"/>
    /// and the viewport. Not raised for every pointer move. An app keeping the width stores it and passes it back as
    /// <see cref="BitResourceTimelineOptions.ResourceAxisWidth"/>.
    /// </summary>
    [Parameter] public EventCallback<int> OnResourceAxisWidthChanged { get; set; }

    /// <summary>
    /// Raised when the set of selected bars actually changes (selecting the same sole bar again, or clicking empty content
    /// with nothing selected, raises nothing). The array holds every selected bar in selection order, and is empty when
    /// nothing is selected. Its elements are the very instances supplied in the <see cref="Config"/>. The selection
    /// supports a click, Ctrl/Cmd+click to toggle a bar, Shift+click to select a range, and a drag on empty content to
    /// select a region.
    /// </summary>
    [Parameter] public EventCallback<BitResourceTimelineAllocation[]> OnSelectionChanged { get; set; }

    /// <summary>
    /// Raised at most once per frame, after a scroll, a zoom or a layout, when the visible time span or the effective
    /// scale (pixels per hour) changes. Meant for the chrome around the timeline (a day label, a "3 days" zoom
    /// indicator); not a per-pixel scroll stream.
    /// </summary>
    [Parameter] public EventCallback<BitResourceTimelineView> OnViewChanged { get; set; }

    /// <summary>
    /// The visual configuration of the timeline: its dimensions, fonts, colors, time zone, zoom limits, editing and the
    /// surface of the content pane. A new instance is applied as it is assigned (compared by reference, like the
    /// Config), which is how the renderer, the colors or the editing mode are switched at runtime. Applying merges: a
    /// value left null keeps the one in force - the default, a value an earlier instance set, or a width the user
    /// dragged the resource column to - and a color or a font never set follows the bit theme in force.
    /// </summary>
    [Parameter] public BitResourceTimelineOptions? Options { get; set; }

    /// <summary>
    /// The template of each row of the sticky resource column. When set, the engine stops drawing the resource labels and
    /// an overlay renders this template for each visible row instead, so a row can hold rich, interactive content
    /// (badges, links, avatars). Group rows get an expand/collapse button before the template. The overlay follows the
    /// vertical scroll, and the divider of the resource column stays above it, so the column remains resizable.
    /// </summary>
    [Parameter] public RenderFragment<BitResourceTimelineRowContext>? ResourceTemplate { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the resource timeline.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitResourceTimelineClassStyles? Styles { get; set; }

    /// <summary>
    /// The texts of the timeline - its accessible name and description, the loading text, the labels of the group
    /// buttons and of the column divider, and everything it announces to screen readers. All of them default to English;
    /// an instance with the ones to override localizes the timeline.
    /// </summary>
    [Parameter] public BitResourceTimelineTexts? Texts { get; set; }

    /// <summary>
    /// The template of the hover tooltip. When set, the hovered bar is rendered through this template in a positioned
    /// overlay (with every renderer, the canvas included) instead of the built-in text tooltip. Requires
    /// <see cref="BitResourceTimelineOptions.ShowTooltips"/> (on by default). A +N overflow marker has no allocation to
    /// pass here, so hovering one still shows the built-in tooltip listing the hidden bars.
    /// </summary>
    [Parameter] public RenderFragment<BitResourceTimelineAllocation>? TooltipTemplate { get; set; }

    /// <summary>
    /// The custom content of the top-start corner of the timeline, the otherwise blank cell where the time axis and the
    /// resource axis meet.
    /// </summary>
    [Parameter] public RenderFragment? TopStartContent { get; set; }



    /// <summary>
    /// Clears the selection of bars.
    /// </summary>
    public async Task ClearSelectionAsync()
    {
        if (IsReady() is false) return;

        await _js.BitResourceTimelineClearSelection(UniqueId);
    }

    /// <summary>
    /// Returns the current horizontal scale in pixels per hour (0 while the timeline is not ready).
    /// </summary>
    public async Task<double> GetPixelsPerHourAsync()
    {
        if (IsStarted() is false) return 0;

        return await _js.BitResourceTimelineGetPixelsPerHour(UniqueId);
    }

    /// <summary>
    /// Returns the selected bars in selection order; empty when nothing is selected.
    /// </summary>
    public async Task<IReadOnlyList<BitResourceTimelineAllocation>> GetSelectedBarsAsync()
    {
        if (IsReady() is false) return [];

        var ids = await _js.BitResourceTimelineGetSelectedBarIds(UniqueId);

        return ResolveAllocations(ids ?? []);
    }

    /// <summary>
    /// Scrolls the timeline so the current time is centered in view. Returns true when "now" falls within the time range
    /// of the timeline and it scrolled; otherwise false.
    /// </summary>
    public async Task<bool> GoToTodayAsync()
    {
        if (IsReady() is false) return false;

        return await _js.BitResourceTimelineGoToNow(UniqueId);
    }

    /// <summary>
    /// Pans the view by whole days without changing the zoom - a positive number forward in time, a negative one back.
    /// A step is exactly 24 hours by default, which keeps the time of day at the leading edge; with
    /// <see cref="BitResourceTimelineOptions.PanToDayStart"/> the leading edge lands on a local midnight instead (the
    /// start of the day <paramref name="days"/> calendar days away). 7 or -7 pans a week. A non-null
    /// <paramref name="panToDayStart"/> wins over that option for this call only. The pan is clamped to the time range,
    /// so this returns false when the view is already against that end (or the whole range fits on screen).
    /// </summary>
    public async Task<bool> PanByDaysAsync(int days, bool? panToDayStart = null)
    {
        if (IsReady() is false) return false;

        return await _js.BitResourceTimelinePanByDays(UniqueId, days, panToDayStart);
    }

    /// <summary>
    /// Sends the current data to the engine again even though the <see cref="Config"/> reference is unchanged, after the
    /// Config was changed in place.
    /// </summary>
    public Task ReloadAsync()
    {
        _loadedConfig = null;

        // Forced: a load of the same Config in flight when this is called sends what it read before the change.
        return LoadDataAsync(force: true);
    }

    /// <summary>
    /// Removes allocations by id from the current set, the selection and the keyboard focus. The other selected bars are
    /// left alone, and unknown ids are ignored. The <see cref="BitResourceTimelineConfig.Allocations"/> list is not
    /// changed.
    /// </summary>
    public async Task RemoveAllocationsAsync(IReadOnlyList<string> ids)
    {
        if (IsReady() is false || ids is null || ids.Count == 0) return;

        foreach (var id in ids)
        {
            _allocationsById.Remove(id);
        }

        await _js.BitResourceTimelineRemoveAllocations(UniqueId, ids);
    }

    /// <summary>
    /// Returns to the automatic (or configured) scale, and returns the new effective scale in pixels per hour.
    /// </summary>
    public async Task<double> ResetZoomAsync()
    {
        if (IsStarted() is false) return 0;

        return await _js.BitResourceTimelineResetZoom(UniqueId);
    }

    /// <summary>
    /// Scrolls so the start of the given allocation is in view and its resource row is on screen. Returns false when the
    /// id is unknown.
    /// </summary>
    public async Task<bool> ScrollToAllocationAsync(string id)
    {
        if (IsReady() is false || string.IsNullOrEmpty(id)) return false;

        return await _js.BitResourceTimelineScrollToAllocation(UniqueId, id);
    }

    /// <summary>
    /// Scrolls vertically so the given resource row is on screen, leaving the horizontal scroll as it is. Returns false
    /// when the id is unknown.
    /// </summary>
    public async Task<bool> ScrollToResourceAsync(string id)
    {
        if (IsReady() is false || string.IsNullOrEmpty(id)) return false;

        return await _js.BitResourceTimelineScrollToResource(UniqueId, id);
    }

    /// <summary>
    /// Scrolls the timeline so the given time is centered in view. Returns true when the time is within the time range of
    /// the timeline.
    /// </summary>
    public async Task<bool> ScrollToTimeAsync(DateTimeOffset time)
    {
        if (IsReady() is false) return false;

        return await _js.BitResourceTimelineScrollToTime(UniqueId, time.ToUnixTimeMilliseconds());
    }

    /// <summary>
    /// Sets the selection to the given allocation ids, or adds them to it when <paramref name="additive"/> is true.
    /// Unknown ids are ignored.
    /// </summary>
    public async Task SelectAsync(IReadOnlyList<string> ids, bool additive = false)
    {
        if (IsReady() is false || ids is null) return;

        await _js.BitResourceTimelineSelectBars(UniqueId, ids, additive);
    }

    /// <summary>
    /// Sets an explicit horizontal scale in pixels per hour (clamped to the configured bounds), or returns to the
    /// automatic (or configured) scale when null. Returns the new effective scale.
    /// </summary>
    public async Task<double> SetPixelsPerHourAsync(double? pixelsPerHour)
    {
        if (IsStarted() is false) return 0;

        return await _js.BitResourceTimelineSetPixelsPerHour(UniqueId, pixelsPerHour);
    }

    /// <summary>
    /// Merges the given allocations into the current set by id (the last one wins) without clearing the selection or the
    /// keyboard focus. Instances changed in place, or new instances replacing the previous ones of the same id, are both
    /// fine. The <see cref="BitResourceTimelineConfig.Allocations"/> list is not changed - a new <see cref="Config"/> (or
    /// a <see cref="ReloadAsync"/>) replaces everything. In the windowed mode only the current window is updated.
    /// </summary>
    public async Task UpsertAllocationsAsync(IReadOnlyList<BitResourceTimelineAllocation> allocations)
    {
        if (IsReady() is false || allocations is null || allocations.Count == 0) return;

        foreach (var allocation in allocations)
        {
            _allocationsById[allocation.Id] = allocation;
        }

        await _js.BitResourceTimelineUpsertAllocations(UniqueId, allocations);
    }

    /// <summary>
    /// Zooms in around the center of the viewport, and returns the new scale in pixels per hour (0 while the timeline is
    /// not ready).
    /// </summary>
    public async Task<double> ZoomInAsync()
    {
        if (IsStarted() is false) return 0;

        return await _js.BitResourceTimelineZoomIn(UniqueId);
    }

    /// <summary>
    /// Zooms out around the center of the viewport, and returns the new scale in pixels per hour (0 while the timeline is
    /// not ready).
    /// </summary>
    public async Task<double> ZoomOutAsync()
    {
        if (IsStarted() is false) return 0;

        return await _js.BitResourceTimelineZoomOut(UniqueId);
    }

    /// <summary>
    /// Zooms so exactly <paramref name="days"/> days fill the content area of the viewport, keeping the time under its
    /// center in place. The scale is clamped to <see cref="BitResourceTimelineOptions.MinPixelsPerHour"/> /
    /// <see cref="BitResourceTimelineOptions.MaxPixelsPerHour"/>. Returns the new scale in pixels per hour (0 while the
    /// timeline is not ready, and the current scale when <paramref name="days"/> is not a finite positive number).
    /// </summary>
    public async Task<double> ZoomToDaysAsync(double days)
    {
        if (IsStarted() is false) return 0;

        return await _js.BitResourceTimelineZoomToDays(UniqueId, days);
    }



    protected override string RootElementClass => "bit-rtm";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnParametersSet()
    {
        // The cascade is applied before anything reads the parameters it may fill in.
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    /// <summary>
    /// Applies the Options, the Texts and the disabled state again when one of them changed, and loads the Config when a
    /// new instance was assigned. Both are compared by reference, so changing one in place does not reach the engine.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (IsDisposing || _started is false) return;

        await ApplyConfigurationAsync();

        if (IsDisposing) return;

        if (Config is not null && DataChanged())
        {
            await LoadDataAsync();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsDisposing) return;

        try
        {
            if (firstRender)
            {
                await StartAsync();
            }
            else if (_started)
            {
                // A template assigned (or taken away) after the start: its overlay is in the markup only now.
                await SyncTemplatesAsync();
            }
        }
        catch (JSDisconnectedException) { } // the circuit is gone; there is no browser side left to talk to
    }



    // The texts in force: the ones given, or the English defaults.
    private BitResourceTimelineTexts ActiveTexts => Texts ?? _defaultTexts;

    private bool IsWindowed => LoadAllocationsAsync is not null;

    // A disabled timeline is view-only: the engine handles nothing but the paging keys there.
    private string KeyShortcuts => Disabled
                                   ? DisabledKeyShortcuts
                                   : BaseKeyShortcuts
                                     + (Options?.Editable is true ? EditingKeyShortcuts + CopyKeyShortcuts : string.Empty)
                                     + (Options?.Editable is true && Options?.AllowDelete is true ? DeleteKeyShortcuts : string.Empty);

    // Whether the engine is running, which is all the zoom calls need.
    private bool IsStarted() => IsDisposing is false && _started;

    // Whether the engine is running and holds data, which the calls about bars and the scroll position need.
    private bool IsReady() => IsStarted() && _dataLoaded;

    // Only the references are compared: an Options or Texts instance changed in place is not applied again (a new one, or
    // a ReloadAsync for the data, is).
    private bool ConfigurationChanged() => ReferenceEquals(_loadedOptions, Options) is false
                                           || ReferenceEquals(_loadedTexts, Texts) is false
                                           || _loadedDisabled != Disabled;

    // A load of this very instance in flight counts as already handled, so a render of the parent cannot replay it.
    private bool DataChanged() => ReferenceEquals(_loadedConfig, Config) is false && ReferenceEquals(_loadingConfig, Config) is false;

    private string GetCornerStyle() => FormattableString.Invariant($"width:{_layout.ResourceAxisWidth}px;height:{_layout.TimeAxisHeight}px");

    private string GetResourceOverlayStyle() => FormattableString.Invariant($"top:{_layout.TimeAxisHeight}px;width:{_layout.ResourceAxisWidth}px");

    private string GetResourceRowStyle(BitResourceTimelineResourceRow row)
    {
        var height = row.Height > 0 ? row.Height : _layout.ResourceHeight;

        // The engine lays its rows out left to right whatever the direction of the page, and the overlay sits on them.
        return FormattableString.Invariant($"height:{height}px;padding-left:{8 + row.Depth * _layout.ResourceIndent}px");
    }

    private string GetTooltipOverlayStyle() => FormattableString.Invariant($"left:{_hoverClientX}px;top:{_hoverClientY}px");

    // Starts the browser side: the engine against the viewport, the layout it works with, the overlays it reports to and
    // the initial data. Every await is a point the component may have been removed at (a route change, a re-keyed
    // instance, a parent that stops rendering it), so each one is followed by a disposal check.
    private async Task StartAsync()
    {
        _dotnetObj = DotNetObjectReference.Create(this);

        _loadedOptions = Options;
        _loadedTexts = Texts;
        _loadedDisabled = Disabled;

        var layout = await _js.BitResourceTimelineSetup(UniqueId, _viewportRef, _dotnetObj, Options, ActiveTexts, Disabled);

        // No layout is a runtime that cannot run scripts (a prerender), where there is nothing to start.
        if (IsDisposing || layout is not { } l) return;

        _started = true;
        _layout = l;
        StateHasChanged();

        // The Options, Texts or Disabled the parent changed while the engine was starting: the passes of
        // OnParametersSetAsync in that window had nothing to send them to.
        await ApplyConfigurationAsync();

        if (IsDisposing) return;

        // The overlay of the resource column is enabled before the data loads, so the renderer leaves the labels to it
        // and the engine reports its rows.
        await SyncTemplatesAsync();

        if (IsDisposing) return;

        if (Config is not null)
        {
            await LoadDataAsync();
        }
    }

    // Sends the Options, the Texts and the disabled state when one of them changed since they were last sent.
    private async Task ApplyConfigurationAsync()
    {
        if (ConfigurationChanged() is false) return;

        _loadedOptions = Options;
        _loadedTexts = Texts;
        _loadedDisabled = Disabled;

        var layout = await _js.BitResourceTimelineSetOptions(UniqueId, Options, ActiveTexts, Disabled);

        if (IsDisposing) return;

        if (layout is { } l)
        {
            _layout = l;
            StateHasChanged();
        }
    }

    // Tells the engine which templates there are, when that changed since it was last told: the overlay of the
    // ResourceTemplate takes the resource labels over (and gets the rows), the TooltipTemplate the hovered bar.
    private async Task SyncTemplatesAsync()
    {
        var resourceTemplate = ResourceTemplate is not null;

        if (resourceTemplate != _resourceTemplateEnabled)
        {
            _resourceTemplateEnabled = resourceTemplate;

            if (resourceTemplate)
            {
                await _js.BitResourceTimelineEnableResourceTemplate(UniqueId, _resourceOverlayInnerRef);
            }
            else
            {
                _resourceRows = [];
                await _js.BitResourceTimelineDisableResourceTemplate(UniqueId);
            }

            if (IsDisposing) return;
        }

        var tooltipTemplate = TooltipTemplate is not null;

        if (tooltipTemplate != _tooltipTemplateEnabled)
        {
            _tooltipTemplateEnabled = tooltipTemplate;

            if (tooltipTemplate)
            {
                await _js.BitResourceTimelineEnableTooltipTemplate(UniqueId);
            }
            else
            {
                _hoverAllocation = null;
                await _js.BitResourceTimelineDisableTooltipTemplate(UniqueId);
            }
        }
    }

    private async Task LoadDataAsync(bool force = false)
    {
        // The target is captured now: another load may be asked for while this one waits for the gate, and this older one
        // is then skipped below.
        var config = Config;

        if (IsStarted() is false || config is null) return;

        await _loadGate.WaitAsync();

        try
        {
            // A newer Config superseded this one while it was queued (or the component went away): that one loads instead.
            if (IsDisposing || ReferenceEquals(Config, config) is false) return;

            // The same Config is already in the engine, or a call ahead of this one on the gate is writing it. Sending it
            // again would flash the loading overlay and paint the bars twice on the first load - unless a ReloadAsync asked
            // for it, after the data was changed in place.
            if (force is false && (ReferenceEquals(_loadedConfig, config) || ReferenceEquals(_loadingConfig, config))) return;

            _loadingConfig = config;

            try
            {
                await LoadDataCoreAsync(config);
            }
            finally
            {
                if (ReferenceEquals(_loadingConfig, config))
                {
                    _loadingConfig = null;
                }
            }
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private async Task LoadDataCoreAsync(BitResourceTimelineConfig config)
    {
        _isLoading = true;
        StateHasChanged();

        // A short delay reliably lets the overlay paint before the synchronous work of the engine begins: a yield alone
        // flushes the render batch but does not guarantee a paint of the browser on WebAssembly.
        await Task.Delay(1);

        if (IsDisposing) return;

        var startedAt = DateTime.UtcNow;

        try
        {
            var allocations = config.Allocations;
            var start = config.StartDate.ToUnixTimeMilliseconds();
            var end = config.EndDate.ToUnixTimeMilliseconds();

            if (IsWindowed)
            {
                // Reset before the engine starts the windowed mode, so a window it asks for right away is neither wiped
                // from the map nor outranked by the initial one.
                _allocationsById = [];
                _latestWindowRequestId = 0;

                // The rows and the whole range first, then the visible window alone; the next windows arrive through
                // RequestAllocationWindow.
                await _js.BitResourceTimelineBeginWindowed(UniqueId, config.Resources, start, end);

                var window = await _js.BitResourceTimelineGetVisibleWindow(UniqueId);

                if (IsDisposing) return;

                if (window is { Length: 2 })
                {
                    await FetchAndApplyWindowAsync(0, window[0], window[1]);
                }

                if (IsDisposing) return;

                await _js.BitResourceTimelineWhenRendered(UniqueId);

                _loadedConfig = config;
                _dataLoaded = true;
                return;
            }

            if (LoadBatchSize > 0 && allocations.Count > LoadBatchSize)
            {
                // A large data set is streamed in bounded batches, so a single message of several megabytes neither blocks
                // the circuit nor stalls the main thread serializing and parsing it all at once.
                await _js.BitResourceTimelineBeginData(UniqueId, config.Resources, start, end, allocations.Count);

                for (var i = 0; i < allocations.Count; i += LoadBatchSize)
                {
                    // Streaming spans many awaits: stop as soon as there is nothing left to stream into.
                    if (IsDisposing) return;

                    var count = Math.Min(LoadBatchSize, allocations.Count - i);

                    await _js.BitResourceTimelineAppendAllocations(UniqueId, allocations.GetRange(i, count));
                }

                await _js.BitResourceTimelineEndData(UniqueId);
            }
            else
            {
                await _js.BitResourceTimelineSetData(UniqueId, config.Resources, start, end, allocations);
            }

            if (IsDisposing) return;

            // The overlay is hidden once the renderer has painted the bars.
            await _js.BitResourceTimelineWhenRendered(UniqueId);

            var byId = new Dictionary<string, BitResourceTimelineAllocation>(allocations.Count);

            foreach (var allocation in allocations)
            {
                byId[allocation.Id] = allocation;
            }

            _allocationsById = byId;
            _loadedConfig = config;
            _dataLoaded = true;
        }
        finally
        {
            // The overlay stays for the minimum duration, so it does not flash by imperceptibly on a fast render.
            var remaining = LoadingMinDurationMs - (int)(DateTime.UtcNow - startedAt).TotalMilliseconds;

            if (remaining > 0 && IsDisposing is false)
            {
                await Task.Delay(remaining);
            }

            _isLoading = false;

            if (IsDisposing is false)
            {
                StateHasChanged();
            }
        }
    }

    // Fetches the allocations of a window from the app and pushes them to the engine, unless a newer request superseded
    // this one in the meantime.
    private async Task FetchAndApplyWindowAsync(long requestId, long startMs, long endMs)
    {
        if (LoadAllocationsAsync is null) return;

        await _windowGate.WaitAsync();

        try
        {
            // Superseded while queued behind the gate (or nothing left to render into): the stale fetch is skipped.
            if (IsDisposing || requestId < _latestWindowRequestId) return;

            var window = new BitResourceTimelineWindow
            {
                Start = DateTimeOffset.FromUnixTimeMilliseconds(startMs),
                End = DateTimeOffset.FromUnixTimeMilliseconds(endMs),
            };

            var allocations = await LoadAllocationsAsync(window) ?? [];

            // A newer request arrived while the app was working, or the component was removed: the result is dropped.
            if (IsDisposing || requestId < _latestWindowRequestId) return;

            MergeWindowAllocations(allocations, startMs, endMs);

            await _js.BitResourceTimelineApplyAllocationWindow(UniqueId, requestId, allocations, startMs, endMs);
        }
        finally
        {
            _windowGate.Release();
        }
    }

    // A window fetch upserts the incoming ids and drops the ids that are neither in the payload nor overlapping the loaded
    // range any more. The survivors keep their instances, so an edit of one still resolves.
    private void MergeWindowAllocations(IReadOnlyList<BitResourceTimelineAllocation> incoming, long startMs, long endMs)
    {
        var incomingIds = new HashSet<string>(incoming.Count, StringComparer.Ordinal);

        foreach (var allocation in incoming)
        {
            incomingIds.Add(allocation.Id);
            _allocationsById[allocation.Id] = allocation;
        }

        var windowStart = DateTimeOffset.FromUnixTimeMilliseconds(startMs);
        var windowEnd = DateTimeOffset.FromUnixTimeMilliseconds(endMs);

        List<string>? drop = null;

        foreach (var pair in _allocationsById)
        {
            if (incomingIds.Contains(pair.Key)) continue;

            var allocation = pair.Value;

            if (allocation.StartTime < windowEnd && allocation.EndTime > windowStart) continue;

            drop ??= [];
            drop.Add(pair.Key);
        }

        if (drop is null) return;

        foreach (var id in drop)
        {
            _allocationsById.Remove(id);
        }
    }

    // Collapses or expands a group from the button of its row in the overlay.
    private async Task ToggleGroupAsync(string id)
    {
        if (Disabled || IsStarted() is false) return;

        await _js.BitResourceTimelineToggleGroup(UniqueId, id);
    }

    // The context of the ResourceTemplate for a row, with the full instance of its resource.
    private BitResourceTimelineRowContext ContextFor(BitResourceTimelineResourceRow row)
    {
        _resourcesById.TryGetValue(row.Id, out var resource);

        return new BitResourceTimelineRowContext
        {
            Resource = resource ?? new BitResourceTimelineResource { Id = row.Id, Name = row.Name },
            Depth = row.Depth,
            HasChildren = row.HasChildren,
            Collapsed = row.Collapsed,
        };
    }

    // The instances of the loaded Config for ids the engine reported, in their order. An unknown id (of a Config swapped
    // in the middle of a callback) is skipped.
    private BitResourceTimelineAllocation[] ResolveAllocations(string[] ids)
    {
        var result = new List<BitResourceTimelineAllocation>(ids.Length);

        foreach (var id in ids)
        {
            if (_allocationsById.TryGetValue(id, out var allocation))
            {
                result.Add(allocation);
            }
        }

        return [.. result];
    }

    private static BitResourceTimelineAllocationChangeKind ParseChangeKind(string? kind) =>
        Enum.TryParse<BitResourceTimelineAllocationChangeKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : BitResourceTimelineAllocationChangeKind.Move;

    private static BitResourceTimelineHitArea ParseHitArea(string? area) => area switch
    {
        "resourceAxis" => BitResourceTimelineHitArea.ResourceAxis,
        "timeAxis" => BitResourceTimelineHitArea.TimeAxis,
        "corner" => BitResourceTimelineHitArea.Corner,
        _ => BitResourceTimelineHitArea.Content,
    };

    private static void RestoreAllocation(BitResourceTimelineAllocation allocation, string previousResourceId, long previousStartTime, long previousEndTime)
    {
        allocation.ResourceId = previousResourceId;
        allocation.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(previousStartTime);
        allocation.EndTime = DateTimeOffset.FromUnixTimeMilliseconds(previousEndTime);
    }

    private T BuildPointerArgs<T>(string? allocationId, string[]? overflowIds, string? resourceId, long? time,
                                  string? area, double x, double y, double clientX, double clientY,
                                  bool ctrlKey, bool shiftKey, bool metaKey, bool altKey)
        where T : BitResourceTimelinePointerArgs, new()
    {
        BitResourceTimelineAllocation? allocation = null;

        if (allocationId is not null)
        {
            _allocationsById.TryGetValue(allocationId, out allocation);
        }

        BitResourceTimelineResource? resource = null;

        if (resourceId is not null && _resourcesById.TryGetValue(resourceId, out resource) is false)
        {
            resource = Config?.Resources.FirstOrDefault(r => r.Id == resourceId);
        }

        return new T
        {
            Allocation = allocation,
            OverflowAllocations = overflowIds is { Length: > 0 } ? ResolveAllocations(overflowIds) : [],
            Resource = resource,
            Time = time is { } t ? DateTimeOffset.FromUnixTimeMilliseconds(t) : null,
            Area = ParseHitArea(area),
            X = x,
            Y = y,
            ClientX = clientX,
            ClientY = clientY,
            CtrlKey = ctrlKey,
            ShiftKey = shiftKey,
            MetaKey = metaKey,
            AltKey = altKey,
        };
    }

    private async Task RaisePointerAsync(EventCallback<BitResourceTimelinePointerArgs> callback,
                                         string? allocationId, string[]? overflowIds, string? resourceId, long? time,
                                         string? area, double x, double y, double clientX, double clientY,
                                         bool ctrlKey, bool shiftKey, bool metaKey, bool altKey)
    {
        if (callback.HasDelegate is false) return;

        await callback.InvokeAsync(BuildPointerArgs<BitResourceTimelinePointerArgs>(allocationId, overflowIds, resourceId, time,
                                                                                     area, x, y, clientX, clientY,
                                                                                     ctrlKey, shiftKey, metaKey, altKey));
    }



    /// <summary>
    /// Called by the engine in the windowed mode when a scroll or a zoom needs a wider window than the one loaded. Public
    /// only because the interop requires it; not part of the API of the component.
    /// </summary>
    /// <param name="requestId">The increasing id of the request; a fetch is dropped when a newer request arrives meanwhile.</param>
    /// <param name="startMs">The start of the needed window, as Unix time in milliseconds.</param>
    /// <param name="endMs">The end of the needed window, as Unix time in milliseconds.</param>
    [JSInvokable("RequestAllocationWindow")]
    public async Task _RequestAllocationWindow(long requestId, long startMs, long endMs)
    {
        if (IsDisposing || IsStarted() is false || LoadAllocationsAsync is null) return;

        _latestWindowRequestId = Math.Max(_latestWindowRequestId, requestId);

        await FetchAndApplyWindowAsync(requestId, startMs, endMs);
    }

    /// <summary>
    /// Called by the engine whenever the set or the order of the visible rows changes (a data load, a group collapsed or
    /// expanded), so the overlay of the ResourceTemplate renders them. Public only because the interop requires it; not
    /// part of the API of the component.
    /// </summary>
    /// <param name="rows">The visible rows in display order, as objects of id, name, depth, hasChildren, collapsed and height.</param>
    [JSInvokable("OnResourceRowsChanged")]
    public void _OnResourceRowsChanged(JsonElement rows)
    {
        if (IsDisposing) return;

        _resourceRows = ReadRows(rows);

        if (Config is not null)
        {
            var byId = new Dictionary<string, BitResourceTimelineResource>(Config.Resources.Count);

            foreach (var resource in Config.Resources)
            {
                byId[resource.Id] = resource;
            }

            _resourcesById = byId;
        }

        StateHasChanged();
    }

    /// <summary>
    /// Called by the engine when the selection changes, raising <see cref="OnSelectionChanged"/> with the instances of the
    /// app. Public only because the interop requires it; not part of the API of the component.
    /// </summary>
    /// <param name="selectedIds">The ids of the selected allocations, in selection order.</param>
    [JSInvokable("OnSelectionUpdated")]
    public async Task _OnSelectionUpdated(string[] selectedIds)
    {
        if (IsDisposing || OnSelectionChanged.HasDelegate is false) return;

        await OnSelectionChanged.InvokeAsync(ResolveAllocations(selectedIds ?? []));
    }

    /// <summary>
    /// Called by the engine at most once per frame when the visible range or the scale changes. Public only because the
    /// interop requires it; not part of the API of the component.
    /// </summary>
    [JSInvokable("OnViewChanged")]
    public async Task _OnViewChanged(long startMs, long endMs, double pixelsPerHour)
    {
        if (IsDisposing || OnViewChanged.HasDelegate is false) return;

        await OnViewChanged.InvokeAsync(new BitResourceTimelineView
        {
            Start = DateTimeOffset.FromUnixTimeMilliseconds(startMs),
            End = DateTimeOffset.FromUnixTimeMilliseconds(endMs),
            PixelsPerHour = pixelsPerHour,
        });
    }

    /// <summary>
    /// Called by the engine when the hovered bar changes (or the pointer leaves) while a TooltipTemplate is set. Public
    /// only because the interop requires it; not part of the API of the component.
    /// </summary>
    [JSInvokable("OnBarHover")]
    public void _OnBarHover(string? id, double clientX, double clientY)
    {
        if (IsDisposing) return;

        BitResourceTimelineAllocation? next = null;

        if (id is not null)
        {
            _allocationsById.TryGetValue(id, out next);
        }

        _hoverAllocation = next;
        _hoverClientX = clientX;
        _hoverClientY = clientY;

        StateHasChanged();
    }

    /// <summary>
    /// Called by the engine after a bar was moved or resized and the change was accepted. The resource and the times are
    /// applied to the app's own instance (resolved by id), so the <see cref="Config"/> reflects the change without a
    /// reload, and then <see cref="OnAllocationChanged"/> is raised. Public only because the interop requires it; not
    /// part of the API of the component.
    /// </summary>
    [JSInvokable("OnAllocationEdited")]
    public async Task _OnAllocationEdited(string id, string resourceId, long startTime, long endTime)
    {
        if (IsDisposing || _allocationsById.TryGetValue(id, out var allocation) is false) return;

        allocation.ResourceId = resourceId;
        allocation.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime);
        allocation.EndTime = DateTimeOffset.FromUnixTimeMilliseconds(endTime);

        if (OnAllocationChanged.HasDelegate)
        {
            await OnAllocationChanged.InvokeAsync(allocation);
        }
    }

    /// <summary>
    /// Called by the engine after it previewed an edit (a <c>move</c>, a <c>resize</c> or a <c>create</c>, as the kind
    /// says), before it commits it. Returns true (keep the preview) when no <see cref="OnAllocationChanging"/> is set.
    /// Public only because the interop requires it; not part of the API of the component.
    /// </summary>
    [JSInvokable("OnAllocationChanging")]
    public async Task<bool> _OnAllocationChanging(string id, string resourceId, long startTime, long endTime,
                                                  string previousResourceId, long previousStartTime, long previousEndTime,
                                                  string kind)
    {
        if (IsDisposing) return false;

        if (OnAllocationChanging is null)
        {
            if (OnAllocationsChanging is null) return true;

            return await _OnAllocationsChanging([id], [resourceId], [startTime], [endTime],
                                                [previousResourceId], [previousStartTime], [previousEndTime],
                                                kind);
        }

        if (_allocationsById.TryGetValue(id, out var allocation) is false) return false;

        allocation.ResourceId = resourceId;
        allocation.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime);
        allocation.EndTime = DateTimeOffset.FromUnixTimeMilliseconds(endTime);

        var change = new BitResourceTimelineAllocationChange
        {
            Allocation = allocation,
            PreviousResourceId = previousResourceId,
            PreviousStartTime = DateTimeOffset.FromUnixTimeMilliseconds(previousStartTime),
            PreviousEndTime = DateTimeOffset.FromUnixTimeMilliseconds(previousEndTime),
            Kind = ParseChangeKind(kind),
        };

        try
        {
            if (await OnAllocationChanging(change)) return true;
        }
        catch
        {
            RestoreAllocation(allocation, previousResourceId, previousStartTime, previousEndTime);
            throw;
        }

        RestoreAllocation(allocation, previousResourceId, previousStartTime, previousEndTime);

        return false;
    }

    /// <summary>
    /// Called by the engine for a commit of several bars (or of a single one while <see cref="OnAllocationsChanging"/> is
    /// set). Public only because the interop requires it; not part of the API of the component.
    /// </summary>
    [JSInvokable("OnAllocationsChanging")]
    public async Task<bool> _OnAllocationsChanging(string[] ids, string[] resourceIds, long[] startTimes, long[] endTimes,
                                                   string[] previousResourceIds, long[] previousStartTimes, long[] previousEndTimes,
                                                   string kind)
    {
        if (IsDisposing || ids is null || ids.Length == 0) return false;

        if (OnAllocationsChanging is null)
        {
            if (ids.Length == 1 && OnAllocationChanging is not null)
            {
                return await _OnAllocationChanging(ids[0], resourceIds[0], startTimes[0], endTimes[0],
                                                   previousResourceIds[0], previousStartTimes[0], previousEndTimes[0],
                                                   kind);
            }

            return true;
        }

        var parsedKind = ParseChangeKind(kind);
        var changes = new List<BitResourceTimelineAllocationChange>(ids.Length);

        for (var i = 0; i < ids.Length; i++)
        {
            if (_allocationsById.TryGetValue(ids[i], out var allocation) is false)
            {
                // The bars previewed so far are put back before the whole commit is refused.
                for (var j = 0; j < changes.Count; j++)
                {
                    RestoreAllocation(changes[j].Allocation, previousResourceIds[j], previousStartTimes[j], previousEndTimes[j]);
                }

                return false;
            }

            allocation.ResourceId = resourceIds[i];
            allocation.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTimes[i]);
            allocation.EndTime = DateTimeOffset.FromUnixTimeMilliseconds(endTimes[i]);

            changes.Add(new BitResourceTimelineAllocationChange
            {
                Allocation = allocation,
                PreviousResourceId = previousResourceIds[i],
                PreviousStartTime = DateTimeOffset.FromUnixTimeMilliseconds(previousStartTimes[i]),
                PreviousEndTime = DateTimeOffset.FromUnixTimeMilliseconds(previousEndTimes[i]),
                Kind = parsedKind,
            });
        }

        try
        {
            if (await OnAllocationsChanging(changes)) return true;
        }
        catch
        {
            for (var i = 0; i < changes.Count; i++)
            {
                RestoreAllocation(changes[i].Allocation, previousResourceIds[i], previousStartTimes[i], previousEndTimes[i]);
            }

            throw;
        }

        for (var i = 0; i < changes.Count; i++)
        {
            RestoreAllocation(changes[i].Allocation, previousResourceIds[i], previousStartTimes[i], previousEndTimes[i]);
        }

        return false;
    }

    /// <summary>
    /// Called by the engine before Delete/Backspace removes bars. Public only because the interop requires it; not part of
    /// the API of the component.
    /// </summary>
    [JSInvokable("OnAllocationsDeleting")]
    public async Task<bool> _OnAllocationsDeleting(string[] ids)
    {
        if (IsDisposing) return false;

        if (OnAllocationsDeleting is not null && await OnAllocationsDeleting(ids) is false) return false;

        foreach (var id in ids)
        {
            _allocationsById.Remove(id);
        }

        return true;
    }

    /// <summary>
    /// Called by the engine on paste. Public only because the interop requires it; not part of the API of the component.
    /// </summary>
    [JSInvokable("OnAllocationsCopying")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineAllocation))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineBarIcon))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineEdgeBar))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineBarSection))]
    public async Task<IReadOnlyList<BitResourceTimelineAllocation>?> _OnAllocationsCopying(string[] ids, long offsetMs, string? resourceId)
    {
        if (IsDisposing) return null;

        if (OnAllocationsCopying is null)
        {
            if (_warnedMissingCopy is false)
            {
                _warnedMissingCopy = true;
                Console.Error.WriteLine($"BitResourceTimeline: Ctrl/Cmd+V requires {nameof(OnAllocationsCopying)}.");
            }

            return null;
        }

        var sources = ResolveAllocations(ids);

        if (sources.Length == 0) return null;

        var created = await OnAllocationsCopying(new BitResourceTimelineCopyRequest
        {
            Allocations = sources,
            Offset = TimeSpan.FromMilliseconds(offsetMs),
            ResourceId = resourceId,
        });

        if (IsDisposing || created is null || created.Count == 0) return null;

        var accepted = new List<BitResourceTimelineAllocation>(created.Count);

        foreach (var allocation in created)
        {
            if (string.IsNullOrEmpty(allocation.Id)) continue;

            _allocationsById[allocation.Id] = allocation;
            accepted.Add(allocation);
        }

        return accepted;
    }

    /// <summary>
    /// Called by the engine after a create drag on empty content. Returns the allocation the app built, or null to
    /// cancel. Public only because the interop requires it; not part of the API of the component.
    /// </summary>
    [JSInvokable("OnAllocationCreating")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineAllocation))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineBarIcon))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineEdgeBar))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitResourceTimelineBarSection))]
    public async Task<BitResourceTimelineAllocation?> _OnAllocationCreating(string resourceId, long startTime, long endTime)
    {
        if (IsDisposing) return null;

        if (OnAllocationCreating is null)
        {
            if (_warnedMissingCreate is false)
            {
                _warnedMissingCreate = true;
                Console.Error.WriteLine($"BitResourceTimeline: EmptyDragAction.Create requires {nameof(OnAllocationCreating)}.");
            }

            return null;
        }

        var created = await OnAllocationCreating(new BitResourceTimelineCreateRequest
        {
            ResourceId = resourceId,
            StartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime),
            EndTime = DateTimeOffset.FromUnixTimeMilliseconds(endTime),
        });

        if (IsDisposing || created is null || string.IsNullOrEmpty(created.Id)) return null;

        _allocationsById[created.Id] = created;

        return created;
    }

    /// <summary>
    /// Called by the engine on a click (or a stationary tap). Public only because the interop requires it; not part of the
    /// API of the component.
    /// </summary>
    [JSInvokable("OnTimelineClick")]
    public Task _OnTimelineClick(string? allocationId, string[]? overflowIds, string? resourceId, long? time,
                                 string? area, double x, double y, double clientX, double clientY,
                                 bool ctrlKey, bool shiftKey, bool metaKey, bool altKey)
    {
        if (IsDisposing) return Task.CompletedTask;

        return RaisePointerAsync(OnClick, allocationId, overflowIds, resourceId, time,
                                 area, x, y, clientX, clientY, ctrlKey, shiftKey, metaKey, altKey);
    }

    /// <summary>
    /// Called by the engine on a double-click (or a double-tap). Public only because the interop requires it; not part of
    /// the API of the component.
    /// </summary>
    [JSInvokable("OnTimelineDoubleClick")]
    public Task _OnTimelineDoubleClick(string? allocationId, string[]? overflowIds, string? resourceId, long? time,
                                       string? area, double x, double y, double clientX, double clientY,
                                       bool ctrlKey, bool shiftKey, bool metaKey, bool altKey)
    {
        if (IsDisposing) return Task.CompletedTask;

        return RaisePointerAsync(OnDoubleClick, allocationId, overflowIds, resourceId, time,
                                 area, x, y, clientX, clientY, ctrlKey, shiftKey, metaKey, altKey);
    }

    /// <summary>
    /// Called by the engine on a right-click. Public only because the interop requires it; not part of the API of the
    /// component.
    /// </summary>
    [JSInvokable("OnTimelineContextMenu")]
    public async Task _OnTimelineContextMenu(string? allocationId, string[]? overflowIds, string? resourceId, long? time,
                                             string? area, double x, double y, double clientX, double clientY,
                                             bool ctrlKey, bool shiftKey, bool metaKey, bool altKey)
    {
        if (IsDisposing || OnContextMenu.HasDelegate is false) return;

        await OnContextMenu.InvokeAsync(BuildPointerArgs<BitResourceTimelineContextMenuArgs>(allocationId, overflowIds, resourceId, time,
                                                                                              area, x, y, clientX, clientY,
                                                                                              ctrlKey, shiftKey, metaKey, altKey));
    }

    /// <summary>
    /// Called by the engine after a resize of the resource column was committed. The layout the corner and the overlay are
    /// sized from is updated, then <see cref="OnResourceAxisWidthChanged"/> is raised. Public only because the interop
    /// requires it; not part of the API of the component.
    /// </summary>
    [JSInvokable("OnResourceAxisResized")]
    public async Task _OnResourceAxisResized(int width)
    {
        if (IsDisposing) return;

        if (_layout.ResourceAxisWidth != width)
        {
            _layout = _layout with { ResourceAxisWidth = width };
            StateHasChanged();
        }

        if (OnResourceAxisWidthChanged.HasDelegate)
        {
            await OnResourceAxisWidthChanged.InvokeAsync(width);
        }
    }

    // The rows are read off the JSON the engine sent rather than deserialized into a type of their own: nothing in them
    // needs the reflection of the serializer, which a trimmed app would have to be told to keep.
    private static BitResourceTimelineResourceRow[] ReadRows(JsonElement rows)
    {
        if (rows.ValueKind != JsonValueKind.Array) return [];

        var result = new BitResourceTimelineResourceRow[rows.GetArrayLength()];
        var index = 0;

        foreach (var row in rows.EnumerateArray())
        {
            result[index++] = new BitResourceTimelineResourceRow(Id: ReadString(row, "id"),
                                                                 Name: ReadString(row, "name"),
                                                                 Depth: ReadInt(row, "depth"),
                                                                 HasChildren: ReadBool(row, "hasChildren"),
                                                                 Collapsed: ReadBool(row, "collapsed"),
                                                                 Height: ReadInt(row, "height"));
        }

        return result;

        static string ReadString(JsonElement row, string name) =>
            row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

        static int ReadInt(JsonElement row, string name) =>
            row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? (int)Math.Round(value.GetDouble()) : 0;

        static bool ReadBool(JsonElement row, string name) =>
            row.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _dataLoaded = false;

        // The reference stays alive until the engine is gone, so a callback the engine sent before the teardown reached it
        // lands on a component that ignores it (every callback returns on IsDisposing) instead of failing on the JS side.
        // The gates are left undisposed on purpose: a load may still be queued on one, and disposing a SemaphoreSlim throws
        // into everyone waiting on it (it holds nothing disposable unless its AvailableWaitHandle is used).
        await _dotnetObj.DisposeAfterAsync(() => _js.BitResourceTimelineDispose(UniqueId));

        await base.DisposeAsync(disposing);
    }
}
