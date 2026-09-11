using System.Text.Json;

namespace Bit.BlazorUI;

/// <summary>
/// BitMap is a generic, provider-pluggable map component. Pick a backend by setting the
/// <typeparamref name="TMapProvider"/> type argument and pass an instance via <see cref="Provider"/>.
/// Built-in providers: <see cref="BitLeafletMapProvider"/>, <see cref="BitMapLibreMapProvider"/>,
/// <see cref="BitMapboxMapProvider"/>, <see cref="BitOpenLayersMapProvider"/>,
/// <see cref="BitArcGisMapProvider"/>, <see cref="BitAzureMapsMapProvider"/>, <see cref="BitCesiumMapProvider"/>.
/// </summary>
public partial class BitMap<TMapProvider> : BitComponentBase
    where TMapProvider : class, IBitMapProvider, new()
{
    /// <summary>
    /// Clears the process-wide script/stylesheet load cache. Intended for unit tests only -
    /// production code should not need to invalidate the cache because the browser already
    /// dedupes the underlying network requests.
    /// </summary>
    public static void ResetAssetLoadCacheForTesting() => BitMapAssetCache.Reset();

    private bool _initialized;
    private string _canvasId = string.Empty;
    private string _helpId = string.Empty;
    private string _hintId = string.Empty;
    private string _hintTextId = string.Empty;
    private TMapProvider? _activeProvider;
    private ElementReference _mapElement;
    private DotNetObjectReference<BitMap<TMapProvider>>? _dotnetObj;

    private BitMapLoadState _loadState = BitMapLoadState.Idle;
    private Exception? _loadError;
    private string? _announcement;
    private DateTimeOffset _lastAnnouncementAt = DateTimeOffset.MinValue;

    // Resolved once after init and cached: matchMedia is a DOM read, and FlyTo consults
    // this on every call.
    private bool _prefersReducedMotion;

    private bool _isFullscreen;
    private string _popupAnchorId = string.Empty;
    private BitMapMarker? _openPopupMarker;
    private ElementReference _popupElement;

    // Snapshot of the options the chrome was last attached with, so a re-render that changed
    // none of them doesn't pay an interop round-trip.
    private (string?, bool, bool, string, string, bool)? _chromeSignature;

    // Last viewport the map reported. Two-way camera binding is diffed against it, which is what
    // stops the parameter -> map -> callback -> parameter round trip from looping forever.
    private BitMapViewState? _lastView;

    // Raised while pushing a map-reported camera back into Center/Zoom, so the resulting
    // parameter write is not mistaken for the consumer asking to move the map.
    private bool _applyingCameraFromMap;

    // Set when Center or Zoom changed this render. Both are applied together in
    // OnParametersSetAsync so a render that moved both costs one interop call, not two.
    private bool _cameraParametersDirty;

    // Serialises lifecycle transitions (init from first-render, sync, swap, dispose). Without
    // this guard, a rapid Provider change can interleave a dispose-then-init swap with a sync
    // call, leaving the JS side either double-initialized or unable to find its map id.
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    // Set at the top of DisposeAsync. IsDisposed only flips once the base class has finished
    // tearing down, so everything that can resume after an await - the visibility wait, the
    // asset load, the init interop - would otherwise see a live component while teardown is
    // already disposing the gate underneath it.
    private bool _disposing;

    // True once teardown has begun, whether or not the base class has finished it.
    private bool Gone => IsDisposed || _disposing;

    // Snapshot of imperatively-added state. We replay it after a destructive provider swap when
    // ReplayStateOnProviderSwap is true. Plain dictionaries - all access is serialised by
    // _lifecycleGate or by the calling thread (the Blazor renderer is single-threaded per circuit).
    private readonly Dictionary<string, BitMapMarker> _markerState = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VectorLayerSnapshot> _vectorState = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BitMapTileOverlay> _tileOverlayState = new(StringComparer.Ordinal);

    // Layers and overlays the caller hid. Their definitions stay in the snapshots above - only
    // their presence on the map is toggled - so showing one again needs no re-declaration, and a
    // provider swap replays them in whatever visibility they were left in.
    private readonly HashSet<string> _hiddenLayers = new(StringComparer.Ordinal);
    private readonly HashSet<string> _hiddenTileOverlays = new(StringComparer.Ordinal);

    private abstract record VectorLayerSnapshot(string LayerId, BitMapVectorPathStyle? Style)
    {
        /// <summary>
        /// Returns this layer's definition carrying a different style. Declared on the base so
        /// restyling does not have to switch over every geometry kind at the call site.
        /// </summary>
        public VectorLayerSnapshot WithStyle(BitMapVectorPathStyle? style) => this switch
        {
            PolylineSnapshot p => p with { Style = style },
            PolygonSnapshot p => p with { Style = style },
            CircleSnapshot c => c with { Style = style },
            RectangleSnapshot r => r with { Style = style },
            GeoJsonSnapshot g => g with { Style = style },
            _ => this,
        };
    }
    private sealed record PolylineSnapshot(string LayerId, IReadOnlyList<BitMapLatLng> Path, BitMapVectorPathStyle? Style)
        : VectorLayerSnapshot(LayerId, Style);
    private sealed record PolygonSnapshot(string LayerId, IReadOnlyList<BitMapLatLng> Ring, BitMapVectorPathStyle? Style)
        : VectorLayerSnapshot(LayerId, Style);
    private sealed record CircleSnapshot(string LayerId, BitMapLatLng Center, double RadiusMeters, BitMapVectorPathStyle? Style)
        : VectorLayerSnapshot(LayerId, Style);
    private sealed record RectangleSnapshot(string LayerId, BitMapLatLngBounds Bounds, BitMapVectorPathStyle? Style)
        : VectorLayerSnapshot(LayerId, Style);
    private sealed record GeoJsonSnapshot(string LayerId, string GeoJson, BitMapVectorPathStyle? Style)
        : VectorLayerSnapshot(LayerId, Style);



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Optional content rendered above the map canvas (overlays, custom controls, etc.).
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The provider configuration (basemap, tokens, options). When null on first render, a
    /// default <typeparamref name="TMapProvider"/> instance is created. Setting <c>Provider</c>
    /// to <c>null</c> on a live component is a no-op (use a different non-null instance to
    /// reconfigure).
    /// <para>
    /// The parameter is compared by reference, so mutating the instance you already passed in
    /// does not trigger a sync - assign a new instance to push changed options to the map.
    /// </para>
    /// </summary>
    [Parameter, CallOnSetAsync(nameof(OnProviderSet))]
    public TMapProvider? Provider { get; set; }

    /// <summary>
    /// Two-way bindable centre of the map.
    /// <para>
    /// Bound with <c>@bind-Center</c> it works in both directions: assigning it moves the map, and
    /// panning the map writes the new centre back. Leave it unset to let the provider's
    /// <see cref="BitMapProviderBase.Center"/> own the camera.
    /// </para>
    /// </summary>
    [Parameter, TwoWayBound, CallOnSetAsync(nameof(OnCameraParameterSet))]
    public BitMapLatLng? Center { get; set; }

    /// <summary>
    /// Two-way bindable zoom level. Behaves like <see cref="Center"/>: assigning it zooms the map,
    /// and zooming the map writes the new level back.
    /// </summary>
    [Parameter, TwoWayBound, CallOnSetAsync(nameof(OnCameraParameterSet))]
    public double? Zoom { get; set; }

    /// <summary>
    /// The markers the map should show, as a collection rather than a sequence of imperative
    /// calls. Assign a new collection to change them.
    /// <para>
    /// Only what actually changed is sent: the incoming markers are compared by value against the
    /// ones already rendered, so a render that moved one pin does not tear down and rebuild the
    /// rest. When most of the set changed, it is replaced in a single batched call instead.
    /// </para>
    /// <para>
    /// This parameter owns the marker set. Mixing it with <see cref="AddMarker"/> /
    /// <see cref="RemoveMarker"/> means the next change to the collection reconciles those
    /// imperative additions away.
    /// </para>
    /// </summary>
    [Parameter, CallOnSetAsync(nameof(OnMarkersSet))]
    public IEnumerable<BitMapMarker>? Markers { get; set; }

    /// <summary>
    /// Groups nearby markers into a single bubble showing how many they stand for, and expands it
    /// on click. Leave null to draw every marker individually.
    /// <para>
    /// The clustering runs inside BitMap rather than in the provider, so it behaves identically on
    /// all seven backends. While it is on, this component owns what the provider draws: the markers
    /// you add are the source set, and what reaches the map is whatever the current viewport calls
    /// for.
    /// </para>
    /// </summary>
    [Parameter, CallOnSetAsync(nameof(OnClusteringSet))]
    public BitMapClustering? Clustering { get; set; }

    /// <summary>
    /// Fired when the user clicks a cluster bubble. Fires instead of
    /// <see cref="OnMarkerClick"/> - a cluster is not one of your markers.
    /// </summary>
    [Parameter] public EventCallback<BitMapClusterClickArgs> OnClusterClick { get; set; }

    /// <summary>
    /// When true, markers, vector layers, and tile overlays added imperatively are replayed
    /// after a destructive provider swap (i.e. when the new provider has a different
    /// <see cref="IBitMapProvider.JsObjectName"/>). Default is false to preserve the existing
    /// "consumer reapplies on OnReady" behavior.
    /// </summary>
    [Parameter] public bool ReplayStateOnProviderSwap { get; set; }

    /// <summary>
    /// Keep the map sized to its container automatically (via a <c>ResizeObserver</c>) instead of
    /// requiring a manual <see cref="InvalidateSize"/> call.
    /// <para>
    /// This also covers the classic "map inside a hidden tab renders grey" case: a container that
    /// starts at zero size and later becomes visible is re-measured as soon as it has a size.
    /// </para>
    /// </summary>
    [Parameter] public bool AutoResize { get; set; } = true;

    /// <summary>
    /// Defer creating the map until its container is at (or near) the viewport.
    /// <para>
    /// A map costs a few hundred KB of library plus a burst of tile requests, all competing with
    /// the page's own first paint - worth avoiding for a map that is below the fold. While the
    /// component is waiting, <see cref="LoadState"/> stays <see cref="BitMapLoadState.Idle"/>.
    /// </para>
    /// </summary>
    [Parameter] public bool LazyLoad { get; set; }

    /// <summary>
    /// How far outside the viewport the container may be and still count as visible for
    /// <see cref="LazyLoad"/>. Any CSS margin syntax accepted by <c>IntersectionObserver</c>.
    /// </summary>
    [Parameter] public string LazyLoadRootMargin { get; set; } = "200px";

    /// <summary>
    /// Require a modifier before the map takes over the gesture: ctrl/⌘ + wheel to zoom on
    /// desktop, two fingers to pan on touch. A bare wheel or one-finger drag scrolls the page
    /// instead and shows a short hint.
    /// <para>
    /// Recommended for any map embedded in a scrolling page - without it the map traps the
    /// scroll and the reader cannot get past it.
    /// </para>
    /// </summary>
    [Parameter] public bool CooperativeGestures { get; set; }

    /// <summary>Hint shown when a bare wheel gesture is blocked by <see cref="CooperativeGestures"/>.</summary>
    [Parameter] public string CooperativeGesturesWheelHint { get; set; } = "Use ctrl + scroll to zoom the map";

    /// <summary>Hint shown when a one-finger drag is blocked by <see cref="CooperativeGestures"/>.</summary>
    [Parameter] public string CooperativeGesturesTouchHint { get; set; } = "Use two fingers to move the map";

    /// <summary>
    /// Move focus out of the map canvas when Escape is pressed. A focused map consumes the arrow
    /// keys, so leaving it has to be possible without tabbing past every marker
    /// (WCAG 2.1.2, No Keyboard Trap).
    /// </summary>
    [Parameter] public bool EscapeToExit { get; set; } = true;

    /// <summary>
    /// Description of the keyboard model, associated with the canvas through
    /// <c>aria-describedby</c> and shown to sighted users while the map has keyboard focus.
    /// </summary>
    [Parameter] public string KeyboardInstructions { get; set; } =
        "Use the arrow keys to pan the map, plus and minus to zoom, and Escape to leave the map.";

    /// <summary>
    /// Announce the new centre and zoom through a polite live region after the user pans or
    /// zooms. Announcements are throttled (see <see cref="ViewAnnouncementThrottle"/>) so a drag
    /// cannot flood the screen reader.
    /// </summary>
    [Parameter] public bool AnnounceViewChanges { get; set; }

    /// <summary>
    /// Builds the text announced when <see cref="AnnounceViewChanges"/> is on. Supply one to
    /// announce a place name instead of coordinates - a reverse-geocoded "Central London,
    /// zoom 13" is far more useful than a pair of decimals.
    /// </summary>
    [Parameter] public Func<BitMapViewState, string>? ViewAnnouncementFormatter { get; set; }

    /// <summary>Minimum interval between two view announcements. Defaults to 2 seconds.</summary>
    [Parameter] public TimeSpan ViewAnnouncementThrottle { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Honour the operating system's reduced-motion preference: <see cref="FlyTo"/> and an
    /// animated <see cref="SetView"/> jump straight to the destination instead of animating.
    /// <para>
    /// Both methods take an <c>essential</c> argument to opt a specific move back into animating
    /// (a "locate me" recentre, say, where the motion carries the meaning).
    /// </para>
    /// </summary>
    [Parameter] public bool RespectReducedMotion { get; set; } = true;

    /// <summary>
    /// Content of the popup that opens when a marker is clicked, as live Blazor markup rather than
    /// an HTML string.
    /// <para>
    /// This is worth preferring over <see cref="BitMapMarker.PopupHtml"/> for two reasons. Blazor
    /// escapes what it renders, so a popup built from user-supplied data cannot become an
    /// injection point - the whole class of bug that <c>PopupHtml</c> puts on you. And the content
    /// is ordinary markup: components, event handlers, bindings and validation all work inside it,
    /// where an HTML string is inert.
    /// </para>
    /// <para>
    /// The popup is rendered by this component and pinned to its marker, so it looks and behaves
    /// the same on all seven backends instead of inheriting each vendor's popup. It closes on
    /// Escape, on a click elsewhere on the map, and through its own close button.
    /// </para>
    /// </summary>
    [Parameter] public RenderFragment<BitMapMarker>? MarkerPopupTemplate { get; set; }

    /// <summary>Accessible name of the popup when its marker has neither an <c>Alt</c> nor a <c>Title</c>.</summary>
    [Parameter] public string PopupLabel { get; set; } = "Marker details";

    /// <summary>Accessible name of the popup's close button.</summary>
    [Parameter] public string PopupCloseLabel { get; set; } = "Close";

    /// <summary>Fired when a <see cref="MarkerPopupTemplate"/> popup opens, with the marker it belongs to.</summary>
    [Parameter] public EventCallback<BitMapMarker> OnPopupOpened { get; set; }

    /// <summary>Fired when a <see cref="MarkerPopupTemplate"/> popup closes.</summary>
    [Parameter] public EventCallback OnPopupClosed { get; set; }

    /// <summary>
    /// Renders a text alternative to the markers - a table of names and coordinates, each row able
    /// to bring its marker into view.
    /// <para>
    /// A map is a picture, so its markers are unreachable to a screen reader no matter how well the
    /// canvas is labelled. <see cref="BitMapMarkerListMode.ScreenReaderOnly"/> is the usual choice:
    /// the map looks unchanged, and the same data becomes reachable.
    /// </para>
    /// </summary>
    [Parameter] public BitMapMarkerListMode MarkerListMode { get; set; }

    /// <summary>
    /// Replaces the built-in marker table. Receives the markers in no guaranteed order.
    /// </summary>
    [Parameter] public RenderFragment<IReadOnlyList<BitMapMarker>>? MarkerListTemplate { get; set; }

    /// <summary>Caption of the built-in marker table. Say what the markers are, not that they are markers.</summary>
    [Parameter] public string MarkerListCaption { get; set; } = "Map markers";

    /// <summary>Column heading for the marker's name.</summary>
    [Parameter] public string MarkerListNameHeader { get; set; } = "Name";

    /// <summary>Column heading for the marker's latitude.</summary>
    [Parameter] public string MarkerListLatitudeHeader { get; set; } = "Latitude";

    /// <summary>Column heading for the marker's longitude.</summary>
    [Parameter] public string MarkerListLongitudeHeader { get; set; } = "Longitude";

    /// <summary>Label of the button that brings a marker into view. Also the hidden column heading.</summary>
    [Parameter] public string MarkerListActionHeader { get; set; } = "Show on map";

    /// <summary>Zoom applied when a marker-list row brings its marker into view. Null keeps the current zoom.</summary>
    [Parameter] public double? MarkerListZoom { get; set; } = 15;

    /// <summary>Show the built-in loading indicator while the provider's assets and map instance are being created.</summary>
    [Parameter] public bool ShowLoading { get; set; } = true;

    /// <summary>Replaces the built-in loading indicator.</summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Replaces the built-in failure message. Receives the exception that caused the failure, when there is one.</summary>
    [Parameter] public RenderFragment<Exception?>? ErrorTemplate { get; set; }

    /// <summary>Replaces the built-in "this browser cannot render the map" message.</summary>
    [Parameter] public RenderFragment? UnsupportedTemplate { get; set; }

    /// <summary>Text of the built-in loading indicator.</summary>
    [Parameter] public string LoadingLabel { get; set; } = "Loading map…";

    /// <summary>Text of the built-in failure message.</summary>
    [Parameter] public string ErrorLabel { get; set; } = "The map could not be loaded.";

    /// <summary>Text of the built-in unsupported-browser message.</summary>
    [Parameter] public string UnsupportedLabel { get; set; } = "This browser cannot display the map (WebGL is unavailable).";



    /// <summary>
    /// Fired once after the map is ready and imperative methods can be called safely.
    /// Also fires again if the active provider is swapped to one with a different JS backend
    /// (which destructively re-initializes the map); consumers can use this to rebuild any
    /// imperatively-added markers/layers/overlays on the new provider.
    /// </summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <summary>Fired whenever <see cref="LoadState"/> changes.</summary>
    [Parameter] public EventCallback<BitMapLoadState> OnLoadStateChanged { get; set; }

    /// <summary>
    /// Fired when the user clicks the map canvas (not on a marker or vector layer).
    /// </summary>
    [Parameter] public EventCallback<BitMapLatLng> OnClick { get; set; }

    /// <summary>
    /// Fired when the user double-clicks the map.
    /// </summary>
    [Parameter] public EventCallback<BitMapLatLng> OnDoubleClick { get; set; }

    /// <summary>
    /// Fired when the user right-clicks (or long-presses on touch) the map, with the coordinate
    /// under the pointer - the usual way to offer "add a marker here" or "what is this place".
    /// <para>
    /// The browser's own menu still opens unless
    /// <see cref="BitMapProviderBase.SuppressBrowserContextMenu"/> is set. Only suppress it when
    /// you replace it with a menu of your own: taking it away costs the user "open in new tab",
    /// "copy image" and their assistive-technology equivalents.
    /// </para>
    /// </summary>
    [Parameter] public EventCallback<BitMapLatLng> OnContextMenu { get; set; }

    /// <summary>
    /// Fired whenever the map view (center/zoom/bounds) changes.
    /// <para>
    /// The providers coalesce the burst of move/zoom events a single drag produces into one
    /// notification per idle window, so this is safe to subscribe to over a Blazor Server circuit.
    /// </para>
    /// </summary>
    [Parameter] public EventCallback<BitMapViewState> OnViewChanged { get; set; }

    /// <summary>
    /// Fired when the user clicks a marker. The argument is the marker id.
    /// </summary>
    [Parameter] public EventCallback<string> OnMarkerClick { get; set; }

    /// <summary>
    /// Fired when a draggable marker has been dropped at a new position.
    /// </summary>
    [Parameter] public EventCallback<BitMapMarkerDragEndArgs> OnMarkerDragEnd { get; set; }

    /// <summary>
    /// Fired when the user clicks a vector layer (polyline, polygon, circle, rectangle).
    /// </summary>
    [Parameter] public EventCallback<BitMapVectorClickArgs> OnVectorClick { get; set; }

    /// <summary>
    /// Fired when the user clicks a feature inside a GeoJSON layer.
    /// </summary>
    [Parameter] public EventCallback<BitMapGeoJsonFeatureClickArgs> OnGeoJsonFeatureClick { get; set; }

    /// <summary>
    /// Fired when the browser drops the WebGL context the map renders into. Browsers cap how many
    /// contexts may be live at once (roughly 8-16), and the least-recently-used one is dropped
    /// silently - so without this a map simply turns black with no error.
    /// </summary>
    [Parameter] public EventCallback OnRenderContextLost { get; set; }

    /// <summary>Fired when a lost WebGL context is restored.</summary>
    [Parameter] public EventCallback OnRenderContextRestored { get; set; }

    /// <summary>
    /// Fired when the map enters or leaves fullscreen, including when the user leaves it with
    /// Escape or the browser's own control rather than through <see cref="ExitFullscreen"/>.
    /// </summary>
    [Parameter] public EventCallback<bool> OnFullscreenChanged { get; set; }

    /// <summary>
    /// Fired when an interop call into the underlying provider fails. Consumers can subscribe to
    /// surface errors to telemetry/log channels - by default the component swallows interop
    /// failures so a misbehaving provider can't tear down the host page.
    /// </summary>
    [Parameter] public EventCallback<BitMapInteropErrorArgs> OnInteropError { get; set; }



    /// <summary>True after the underlying map instance has been created and is ready for interop calls.</summary>
    public bool IsReady => _initialized;

    /// <summary>Where the map is in its lifecycle. See <see cref="BitMapLoadState"/>.</summary>
    public BitMapLoadState LoadState => _loadState;

    /// <summary>The exception behind <see cref="BitMapLoadState.Failed"/>, when one was captured.</summary>
    public Exception? LoadError => _loadError;

    /// <summary>
    /// Ids of the markers currently on the map. Read from the component's own snapshot, so it
    /// costs no interop round-trip. The order is the snapshot dictionary's own and is not
    /// guaranteed - a removal makes a later addition reuse the freed slot.
    /// </summary>
    public IReadOnlyCollection<string> MarkerIds => [.. _markerState.Keys];

    /// <summary>Ids of the vector layers currently on the map, in no guaranteed order. Costs no interop round-trip.</summary>
    public IReadOnlyCollection<string> LayerIds => [.. _vectorState.Keys];

    /// <summary>Ids of the tile overlays currently on the map, in no guaranteed order. Costs no interop round-trip.</summary>
    public IReadOnlyCollection<string> TileOverlayIds => [.. _tileOverlayState.Keys];

    /// <summary>Whether the map is currently displayed fullscreen.</summary>
    public bool IsFullscreen => _isFullscreen;

    /// <summary>The marker whose <see cref="MarkerPopupTemplate"/> popup is open, or null when none is.</summary>
    public BitMapMarker? OpenPopupMarker => _openPopupMarker;

    /// <summary>The markers currently on the map, in no guaranteed order.</summary>
    public IReadOnlyList<BitMapMarker> OrderedMarkers => [.. _markerState.Values];



    /// <summary>
    /// Takes the map's container fullscreen - not the page - so overlay content and custom
    /// controls go with it.
    /// </summary>
    /// <returns><c>true</c> when the browser granted the request.</returns>
    /// <remarks>
    /// Call this from a user gesture. Browsers only honour a fullscreen request inside the short
    /// activation window a click opens; a slow Blazor Server circuit can miss it, and the browser
    /// then refuses without raising an error.
    /// </remarks>
    public async ValueTask<bool> RequestFullscreen()
    {
        if (_initialized is false) return false;
        try { return await _js.BitMapChromeRequestFullscreen(_Id); }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(RequestFullscreen));
            return false;
        }
    }

    /// <summary>Leaves fullscreen, if this map is the element currently displayed fullscreen.</summary>
    public async ValueTask<bool> ExitFullscreen()
    {
        if (_initialized is false) return false;
        try { return await _js.BitMapChromeExitFullscreen(_Id); }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(ExitFullscreen));
            return false;
        }
    }

    /// <summary>Enters fullscreen, or leaves it if the map is already fullscreen.</summary>
    public ValueTask<bool> ToggleFullscreen() => _isFullscreen ? ExitFullscreen() : RequestFullscreen();

    /// <summary>Recalculate map size after a container resize.</summary>
    /// <remarks>Rarely needed while <see cref="AutoResize"/> is on, which observes the container for you.</remarks>
    public async ValueTask InvalidateSize()
    {
        if (_initialized is false) return;
        await SafeInvokeAsync(_js.BitMapInvalidateSize(JsObject, _Id), nameof(InvalidateSize));
    }

    /// <summary>Returns a snapshot of the current viewport.</summary>
    /// <remarks>Throws <see cref="JSException"/> if the underlying provider's getView fails.</remarks>
    public async ValueTask<BitMapViewState> GetView()
    {
        EnsureReady();
        var el = await _js.BitMapGetView(JsObject, _Id);
        return ParseViewState(el);
    }

    /// <summary>Pan and (optionally) zoom the map to the given center.</summary>
    /// <param name="center">Destination centre.</param>
    /// <param name="zoom">Destination zoom, or null to keep the current one.</param>
    /// <param name="animate">Animate the move. Ignored when the user prefers reduced motion, unless <paramref name="essential"/> is true.</param>
    /// <param name="essential">
    /// Marks the movement as essential to the task, so it animates even under a reduced-motion
    /// preference. Use it sparingly - for a move whose motion is what conveys the meaning.
    /// </param>
    public async ValueTask SetView(BitMapLatLng center, double? zoom = null, bool animate = true, bool essential = false)
    {
        EnsureReady();
        BitMapValidation.ValidateOptionalZoom(zoom, nameof(zoom));
        await SafeInvokeAsync(_js.BitMapSetView(JsObject, _Id, center.Latitude, center.Longitude, zoom, ShouldAnimate(animate, essential)), nameof(SetView));
    }

    /// <summary>Animated pan/zoom to the given center.</summary>
    /// <param name="center">Destination centre.</param>
    /// <param name="zoom">Destination zoom, or null to keep the current one.</param>
    /// <param name="essential">
    /// Marks the flight as essential to the task, so it animates even under a reduced-motion
    /// preference. Otherwise a reduced-motion user gets an instant jump to the same place.
    /// </param>
    public async ValueTask FlyTo(BitMapLatLng center, double? zoom = null, bool essential = false)
    {
        EnsureReady();
        BitMapValidation.ValidateOptionalZoom(zoom, nameof(zoom));

        if (ShouldAnimate(true, essential) is false)
        {
            // Under reduced motion the destination matters, the journey does not - jump.
            await SafeInvokeAsync(_js.BitMapSetView(JsObject, _Id, center.Latitude, center.Longitude, zoom, false), nameof(FlyTo));
            return;
        }

        await SafeInvokeAsync(_js.BitMapFlyTo(JsObject, _Id, center.Latitude, center.Longitude, zoom), nameof(FlyTo));
    }

    /// <summary>Set an absolute zoom level, keeping the current centre.</summary>
    public async ValueTask SetZoom(double zoom, bool animate = true, bool essential = false)
    {
        EnsureReady();
        BitMapValidation.ValidateZoom(zoom, nameof(zoom));

        // The centre comes from the last reported viewport when there is one. Reading it back over
        // interop would double the cost of what is meant to be a single camera command.
        var center = _lastView?.Center;
        if (center is null)
        {
            // GetView is the one call here that can throw at the caller: every other camera
            // command on this component reports a provider failure through OnInteropError. Read
            // it back the same way so a failing getView doesn't escape as a JSException from what
            // looks like a plain SetZoom.
            var view = await SafeInvokeAsync(_js.BitMapGetView(JsObject, _Id), nameof(SetZoom));
            if (view is null) return;
            center = ParseViewState(view.Value).Center;
        }

        await SafeInvokeAsync(_js.BitMapSetView(JsObject, _Id, center.Value.Latitude, center.Value.Longitude, zoom, ShouldAnimate(animate, essential)), nameof(SetZoom));
    }

    /// <summary>Zoom in by <paramref name="delta"/> levels around the current centre.</summary>
    public ValueTask ZoomIn(double delta = 1, bool animate = true, bool essential = false) => ZoomBy(delta, animate, essential);

    /// <summary>Zoom out by <paramref name="delta"/> levels around the current centre.</summary>
    public ValueTask ZoomOut(double delta = 1, bool animate = true, bool essential = false) => ZoomBy(-delta, animate, essential);

    /// <summary>Change the zoom by a relative number of levels. Negative values zoom out.</summary>
    public async ValueTask ZoomBy(double delta, bool animate = true, bool essential = false)
    {
        EnsureReady();
        BitMapValidation.ValidateFinite(delta, nameof(delta));
        await SafeInvokeAsync(_js.BitMapZoomBy(JsObject, _Id, delta, ShouldAnimate(animate, essential)), nameof(ZoomBy));
    }

    /// <summary>
    /// Pan by a pixel offset. Positive <paramref name="offsetX"/> moves the map content left
    /// (the viewport moves east); positive <paramref name="offsetY"/> moves it up.
    /// <para>
    /// This is what pan buttons are built from - and shipping those matters: WCAG 2.2 SC 2.5.7
    /// requires a single-pointer alternative to dragging the map.
    /// </para>
    /// </summary>
    public async ValueTask PanBy(double offsetX, double offsetY, bool animate = true, bool essential = false)
    {
        EnsureReady();
        BitMapValidation.ValidateFinite(offsetX, nameof(offsetX));
        BitMapValidation.ValidateFinite(offsetY, nameof(offsetY));
        await SafeInvokeAsync(_js.BitMapPanBy(JsObject, _Id, offsetX, offsetY, ShouldAnimate(animate, essential)), nameof(PanBy));
    }

    /// <summary>
    /// Pan to a new centre, keeping the current zoom. Shorthand for
    /// <see cref="SetView"/> with no zoom.
    /// </summary>
    public ValueTask PanTo(BitMapLatLng center, bool animate = true, bool essential = false)
        => SetView(center, null, animate, essential);

    /// <summary>
    /// Where a geographic coordinate currently sits inside the map's container, in CSS pixels
    /// measured from its top-left corner. Returns null when the coordinate is not on screen - or,
    /// on the 3D backend, when it is behind the globe.
    /// <para>
    /// This is what a custom overlay of your own is positioned from, the same way the built-in
    /// <see cref="MarkerPopupTemplate"/> popup is. The answer is only valid for the viewport it was
    /// read at, so recompute it whenever <see cref="OnViewChanged"/> fires.
    /// </para>
    /// </summary>
    public async ValueTask<BitMapPoint?> Project(BitMapLatLng position)
    {
        if (_initialized is false) return null;
        try
        {
            var point = await _js.BitMapProject(JsObject, _Id, position.Latitude, position.Longitude);
            if (point.ValueKind is not JsonValueKind.Object) return null;
            return new BitMapPoint(point.GetProperty("x").GetDouble(), point.GetProperty("y").GetDouble());
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(Project));
            return null;
        }
    }

    /// <summary>Fit the view to the given bounding box.</summary>
    /// <param name="bounds">The box to frame.</param>
    /// <param name="paddingPixels">Breathing room, in screen pixels, left on every side.</param>
    /// <param name="maxZoom">
    /// Ceiling on how far the fit may zoom in. Without one, framing a box that encloses a single
    /// place drops the view to street level; the default of 18 is what every backend that honours
    /// a ceiling has always used here.
    /// <para><b>Provider support:</b> Leaflet, MapLibre, Mapbox, OpenLayers and Azure Maps.
    /// ArcGIS and Cesium have no equivalent and ignore it.</para>
    /// </param>
    public async ValueTask FitBounds(BitMapLatLngBounds bounds, int paddingPixels = 48, double maxZoom = 18)
    {
        EnsureReady();
        BitMapValidation.ValidatePadding(paddingPixels, nameof(paddingPixels));
        BitMapValidation.ValidateZoom(maxZoom, nameof(maxZoom));
        await SafeInvokeAsync(_js.BitMapFitBounds(JsObject, _Id,
            bounds.SouthWest.Latitude, bounds.SouthWest.Longitude,
            bounds.NorthEast.Latitude, bounds.NorthEast.Longitude,
            paddingPixels, maxZoom), nameof(FitBounds));
    }

    /// <summary>Fit the view to include all currently rendered markers.</summary>
    /// <param name="paddingPixels">Breathing room, in screen pixels, left on every side.</param>
    /// <param name="maxZoom">Ceiling on how far the fit may zoom in. See <see cref="FitBounds"/>.</param>
    /// <remarks>
    /// While <see cref="Clustering"/> is on this fits what is drawn - the cluster bubbles - rather
    /// than every source marker, so a marker culled as offscreen is not accounted for. Fit the box
    /// from <see cref="BitMapLatLngBounds.FromMarkers"/> over <see cref="OrderedMarkers"/> when you
    /// need every source marker framed regardless.
    /// </remarks>
    public async ValueTask FitBoundsToMarkers(int paddingPixels = 48, double maxZoom = 18)
    {
        EnsureReady();
        BitMapValidation.ValidatePadding(paddingPixels, nameof(paddingPixels));
        BitMapValidation.ValidateZoom(maxZoom, nameof(maxZoom));
        await SafeInvokeAsync(_js.BitMapFitBoundsToMarkers(JsObject, _Id, paddingPixels, maxZoom), nameof(FitBoundsToMarkers));
    }

    /// <summary>
    /// Asks the browser for the user's current position and, unless
    /// <see cref="BitMapGeolocationOptions.SetView"/> is turned off, pans there.
    /// </summary>
    /// <returns>The reported position, or null when the browser refused or timed out.</returns>
    /// <remarks>
    /// The browser shows its own permission prompt; a denial arrives here as a null result rather
    /// than an exception, and is reported through <see cref="OnInteropError"/>.
    /// </remarks>
    public async ValueTask<BitMapGeolocationResult?> Locate(BitMapGeolocationOptions? options = null)
    {
        EnsureReady();
        var opts = options ?? new BitMapGeolocationOptions();

        JsonElement payload;
        try
        {
            payload = await _js.BitMapChromeLocate(opts.EnableHighAccuracy, opts.TimeoutMilliseconds, opts.MaximumAgeMilliseconds);
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(Locate));
            return null;
        }

        BitMapGeolocationResult result;
        try
        {
            result = new BitMapGeolocationResult
            {
                Position = ReadLatLng(payload),
                AccuracyMeters = payload.TryGetProperty("accuracy", out var accuracy) ? accuracy.GetDouble() : 0,
            };
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(Locate));
            return null;
        }

        if (opts.SetView)
        {
            // A "locate me" recentre is essential motion: the movement is what tells the user
            // where they were taken, so it animates even under a reduced-motion preference.
            await SetView(result.Position, opts.Zoom, animate: true, essential: true);
        }

        return result;
    }

    /// <summary>Add a marker to the map.</summary>
    public async ValueTask AddMarker(BitMapMarker marker)
    {
        EnsureReady();
        ArgumentNullException.ThrowIfNull(marker);
        BitMapValidation.ValidateId(marker.Id, $"{nameof(BitMapMarker)}.{nameof(BitMapMarker.Id)}");

        if (IsClustering)
        {
            // With clustering on, the snapshot IS the source of truth the clustering layer works
            // from, so it is updated first and pushed as a whole - the provider never sees this
            // marker individually.
            _markerState[marker.Id] = marker;
            await PushClusteredMarkersAsync(nameof(AddMarker));
            await NotifyMarkerListChanged();
            return;
        }

        // Snapshot mutation is deferred until the interop call succeeds so that a failed
        // BitMapAddMarker doesn't leave the replay dictionary referencing a marker the JS
        // side never created (which would re-add a phantom on the next provider swap).
        if (await SafeInvokeAsync(_js.BitMapAddMarker(JsObject, _Id, marker.Id, ToMarkerPayload(marker)), nameof(AddMarker)))
        {
            _markerState[marker.Id] = marker;
            await NotifyMarkerListChanged();
        }
    }

    /// <summary>Remove a single marker by its id.</summary>
    public async ValueTask RemoveMarker(string markerId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(markerId);

        if (IsClustering)
        {
            if (_markerState.Remove(markerId))
            {
                await PushClusteredMarkersAsync(nameof(RemoveMarker));
                await NotifyMarkerListChanged();
            }
            // A popup for a marker that is no longer on the map would hang in space - true
            // whether the marker went through the clustering layer or straight to the provider.
            if (_openPopupMarker?.Id == markerId) await ClosePopup();
            return;
        }

        if (await SafeInvokeAsync(_js.BitMapRemoveMarker(JsObject, _Id, markerId), nameof(RemoveMarker)))
        {
            _markerState.Remove(markerId);
            await NotifyMarkerListChanged();
        }

        if (_openPopupMarker?.Id == markerId) await ClosePopup();
    }

    /// <summary>Remove all markers from the map.</summary>
    public async ValueTask ClearMarkers()
    {
        EnsureReady();

        if (IsClustering)
        {
            _markerState.Clear();
            await PushClusteredMarkersAsync(nameof(ClearMarkers));
            await NotifyMarkerListChanged();
            await ClosePopup();
            return;
        }

        if (await SafeInvokeAsync(_js.BitMapClearMarkers(JsObject, _Id), nameof(ClearMarkers)))
        {
            _markerState.Clear();
            await NotifyMarkerListChanged();
        }

        await ClosePopup();
    }

    /// <summary>Move an existing marker to a new position.</summary>
    public async ValueTask SetMarkerPosition(string markerId, BitMapLatLng position)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(markerId);

        if (IsClustering)
        {
            if (_markerState.TryGetValue(markerId, out var clustered))
            {
                _markerState[markerId] = CloneWithPosition(clustered, position);
                await PushClusteredMarkersAsync(nameof(SetMarkerPosition));
                await NotifyMarkerListChanged();
            }
            return;
        }

        // Keep the snapshot in sync so a later replay places the marker at its current
        // position. We only commit the position update after the JS side accepted it,
        // otherwise a replay would relocate the marker even though the live map didn't.
        if (await SafeInvokeAsync(_js.BitMapSetMarkerPosition(JsObject, _Id, markerId, position.Latitude, position.Longitude), nameof(SetMarkerPosition)))
        {
            if (_markerState.TryGetValue(markerId, out var prev))
            {
                _markerState[markerId] = CloneWithPosition(prev, position);
                await NotifyMarkerListChanged();
            }
        }
    }

    /// <summary>
    /// Opens the <see cref="MarkerPopupTemplate"/> popup for a marker. Does nothing when no
    /// template is set - use <see cref="OpenMarkerPopup"/> for the provider's own popup.
    /// </summary>
    public async ValueTask OpenPopup(string markerId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(markerId);

        if (MarkerPopupTemplate is null) return;
        if (_markerState.TryGetValue(markerId, out var marker) is false) return;

        _openPopupMarker = marker;
        await InvokeAsync(StateHasChanged);

        // The element only exists after the render above, so the anchor is attached afterwards.
        await SafeInvokeAsync(
            _js.BitMapChromeTrackAnchor(_Id, _popupAnchorId, marker.Position.Latitude, marker.Position.Longitude),
            nameof(OpenPopup));

        // A dialog nobody is standing in is a dialog whose Escape handler never fires and whose
        // content a screen reader never reaches - the WAI-ARIA pattern puts focus inside it on
        // open, and returns it to the opener on close.
        if (_popupElement.Id is not null)
        {
            try { await _popupElement.FocusAsync(preventScroll: true); }
            catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(OpenPopup)); }
        }

        if (OnPopupOpened.HasDelegate is false) return;
        try { await OnPopupOpened.InvokeAsync(marker); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnPopupOpened)); }
    }

    /// <summary>Closes the <see cref="MarkerPopupTemplate"/> popup, if one is open.</summary>
    /// <param name="restoreFocus">
    /// Return keyboard focus to the map canvas. On by default when the popup is dismissed from the
    /// keyboard; a close triggered by something other than the popup itself passes false so focus
    /// is not yanked away from wherever the user actually is.
    /// </param>
    public async ValueTask ClosePopup(bool restoreFocus = false)
    {
        if (_openPopupMarker is null) return;

        _openPopupMarker = null;
        await SafeInvokeAsync(_js.BitMapChromeUntrackAnchor(_Id), nameof(ClosePopup));
        await InvokeAsync(StateHasChanged);

        if (restoreFocus)
        {
            // Blazor's own focus call rather than an interop helper of ours - the canvas is an
            // element this component already holds a reference to.
            try { await _mapElement.FocusAsync(); }
            catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(ClosePopup)); }
        }

        if (OnPopupClosed.HasDelegate is false) return;
        try { await OnPopupClosed.InvokeAsync(); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnPopupClosed)); }
    }

    /// <summary>Open the popup of the marker with the given id.</summary>
    /// <remarks>
    /// Does nothing while <see cref="Clustering"/> has that marker grouped into a bubble - it is
    /// not on the map to open. Zoom past <see cref="BitMapClustering.MaxZoom"/>, or expand its
    /// cluster, first.
    /// </remarks>
    public async ValueTask OpenMarkerPopup(string markerId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(markerId);
        await SafeInvokeAsync(_js.BitMapOpenMarkerPopup(JsObject, _Id, markerId), nameof(OpenMarkerPopup));
    }

    /// <summary>Replace all markers in a single batch operation.</summary>
    /// <remarks>
    /// One interop call regardless of how many markers there are, so prefer it over a loop of
    /// <see cref="AddMarker"/> - each of those is its own round-trip (a SignalR message under
    /// Blazor Server).
    /// </remarks>
    public async ValueTask SyncMarkers(IEnumerable<BitMapMarker> markers)
    {
        EnsureReady();
        ArgumentNullException.ThrowIfNull(markers);

        var list = markers as ICollection<BitMapMarker> ?? [.. markers];

        // A wholesale replace can drop or move the marker an open popup belongs to, so the id is
        // taken before the snapshot is rewritten and the popup reconciled against it afterwards -
        // the same contract RemoveMarker and ClearMarkers already honour.
        var openId = _openPopupMarker?.Id;

        if (IsClustering)
        {
            // The clustering layer builds its own payloads from the snapshot, so the per-marker
            // payload below would be thrown away - only the validation it does is still owed.
            foreach (var m in list)
            {
                ArgumentNullException.ThrowIfNull(m);
                BitMapValidation.ValidateId(m.Id, $"{nameof(BitMapMarker)}.{nameof(BitMapMarker.Id)}");
            }

            _markerState.Clear();
            foreach (var m in list) _markerState[m.Id] = m;
            await PushClusteredMarkersAsync(nameof(SyncMarkers));
            await NotifyMarkerListChanged();
            await ReconcileOpenPopupAsync(openId);
            return;
        }

        var payload = new object[list.Count];
        var ids = new string[list.Count];
        var i = 0;
        foreach (var m in list)
        {
            ArgumentNullException.ThrowIfNull(m);
            BitMapValidation.ValidateId(m.Id, $"{nameof(BitMapMarker)}.{nameof(BitMapMarker.Id)}");
            ids[i] = m.Id;
            payload[i] = ToMarkerPayload(m);
            i++;
        }

        // Defer the snapshot rewrite until the JS-side bulk replace succeeds. If interop
        // failed we keep the previous _markerState so a provider swap still replays the
        // last state the JS layer actually rendered.
        if (await SafeInvokeAsync(_js.BitMapSyncMarkers(JsObject, _Id, ids, payload), nameof(SyncMarkers)))
        {
            _markerState.Clear();
            foreach (var m in list)
            {
                _markerState[m.Id] = m;
            }
            await NotifyMarkerListChanged();
            await ReconcileOpenPopupAsync(openId);
        }
    }

    /// <summary>Add a polyline vector layer.</summary>
    public async ValueTask AddPolyline(string layerId, IReadOnlyList<BitMapLatLng> path, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);
        ArgumentNullException.ThrowIfNull(path);
        BitMapValidation.ValidatePointCount(path.Count, 2, nameof(path), "polyline");

        var snapshot = new PolylineSnapshot(layerId, [.. path], style);
        if (await SafeInvokeAsync(_js.BitMapAddPolyline(JsObject, _Id, layerId, ToLatLngArray(path), ToStylePayload(style)), nameof(AddPolyline)))
        {
            _vectorState[layerId] = snapshot;
            // Adding under an id that was hidden draws it, so the hidden flag has to go with it -
            // otherwise IsLayerVisible would report false for a layer that is plainly on screen.
            _hiddenLayers.Remove(layerId);
        }
    }

    /// <summary>Add a polygon vector layer.</summary>
    public async ValueTask AddPolygon(string layerId, IReadOnlyList<BitMapLatLng> ring, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);
        ArgumentNullException.ThrowIfNull(ring);
        BitMapValidation.ValidatePointCount(ring.Count, 3, nameof(ring), "polygon");

        var snapshot = new PolygonSnapshot(layerId, [.. ring], style);
        if (await SafeInvokeAsync(_js.BitMapAddPolygon(JsObject, _Id, layerId, ToLatLngArray(ring), ToStylePayload(style)), nameof(AddPolygon)))
        {
            _vectorState[layerId] = snapshot;
            _hiddenLayers.Remove(layerId);
        }
    }

    /// <summary>Add a circle vector layer (radius in meters).</summary>
    public async ValueTask AddCircle(string layerId, BitMapLatLng center, double radiusMeters, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);
        BitMapValidation.ValidateRadius(radiusMeters, nameof(radiusMeters));

        if (await SafeInvokeAsync(_js.BitMapAddCircle(JsObject, _Id, layerId, center.Latitude, center.Longitude, radiusMeters, ToStylePayload(style)), nameof(AddCircle)))
        {
            _vectorState[layerId] = new CircleSnapshot(layerId, center, radiusMeters, style);
            _hiddenLayers.Remove(layerId);
        }
    }

    /// <summary>Add a rectangle vector layer.</summary>
    public async ValueTask AddRectangle(string layerId, BitMapLatLngBounds bounds, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);

        if (await SafeInvokeAsync(_js.BitMapAddRectangle(JsObject, _Id, layerId,
            bounds.SouthWest.Latitude, bounds.SouthWest.Longitude,
            bounds.NorthEast.Latitude, bounds.NorthEast.Longitude,
            ToStylePayload(style)), nameof(AddRectangle)))
        {
            _vectorState[layerId] = new RectangleSnapshot(layerId, bounds, style);
            _hiddenLayers.Remove(layerId);
        }
    }

    /// <summary>Add a GeoJSON layer rendered with the given style.</summary>
    /// <remarks>
    /// The GeoJSON is passed through as a string rather than a modelled object graph: it is
    /// already the wire format, so serializing it twice would be pure overhead on large payloads.
    /// </remarks>
    public async ValueTask AddGeoJson(string layerId, string geoJson, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);
        ArgumentException.ThrowIfNullOrEmpty(geoJson);

        if (await SafeInvokeAsync(_js.BitMapAddGeoJson(JsObject, _Id, layerId, geoJson, ToStylePayload(style)), nameof(AddGeoJson)))
        {
            _vectorState[layerId] = new GeoJsonSnapshot(layerId, geoJson, style);
            _hiddenLayers.Remove(layerId);
        }
    }

    /// <summary>
    /// Shows or hides a vector layer without discarding it.
    /// <para>
    /// The layer's definition is kept, so hiding and showing again costs no re-declaration - and
    /// unlike a zero opacity, a hidden layer stops hit-testing, so it cannot swallow clicks meant
    /// for what is underneath it.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no layer with that id has been added.</exception>
    public async ValueTask SetLayerVisible(string layerId, bool visible)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);

        if (_vectorState.TryGetValue(layerId, out var snapshot) is false)
        {
            throw new InvalidOperationException($"BitMap has no vector layer with id '{layerId}'.");
        }

        if (visible)
        {
            if (_hiddenLayers.Remove(layerId) is false) return;
            await AddVectorLayerAsync(snapshot, nameof(SetLayerVisible));
        }
        else
        {
            if (_hiddenLayers.Add(layerId) is false) return;
            await SafeInvokeAsync(_js.BitMapRemoveLayer(JsObject, _Id, layerId), nameof(SetLayerVisible));
        }
    }

    /// <summary>Whether a vector layer is currently drawn. Unknown ids report false.</summary>
    public bool IsLayerVisible(string layerId)
        => _vectorState.ContainsKey(layerId) && _hiddenLayers.Contains(layerId) is false;

    /// <summary>
    /// Restyles an existing vector layer, keeping its geometry.
    /// <para>
    /// The layer is redrawn with the new style; a layer that is currently hidden keeps the style
    /// for whenever it is shown again.
    /// </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no layer with that id has been added.</exception>
    public async ValueTask SetLayerStyle(string layerId, BitMapVectorPathStyle? style)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);

        if (_vectorState.TryGetValue(layerId, out var snapshot) is false)
        {
            throw new InvalidOperationException($"BitMap has no vector layer with id '{layerId}'.");
        }

        var restyled = snapshot.WithStyle(style);
        _vectorState[layerId] = restyled;

        if (_hiddenLayers.Contains(layerId)) return;
        await AddVectorLayerAsync(restyled, nameof(SetLayerStyle));
    }

    /// <summary>Remove a vector layer by id.</summary>
    public async ValueTask RemoveLayer(string layerId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);

        if (await SafeInvokeAsync(_js.BitMapRemoveLayer(JsObject, _Id, layerId), nameof(RemoveLayer)))
        {
            _vectorState.Remove(layerId);
            _hiddenLayers.Remove(layerId);
        }
    }

    /// <summary>Remove all vector layers.</summary>
    public async ValueTask ClearVectorLayers()
    {
        EnsureReady();
        if (await SafeInvokeAsync(_js.BitMapClearVectorLayers(JsObject, _Id), nameof(ClearVectorLayers)))
        {
            _vectorState.Clear();
            _hiddenLayers.Clear();
        }
    }

    /// <summary>Add a tile overlay (raster XYZ layer) above the base map.</summary>
    public async ValueTask AddTileOverlay(BitMapTileOverlay overlay)
    {
        EnsureReady();
        ArgumentNullException.ThrowIfNull(overlay);

        overlay.Validate();
        if (await SafeInvokeAsync(_js.BitMapAddTileOverlay(JsObject, _Id, ToTileOverlayPayload(overlay)), nameof(AddTileOverlay)))
        {
            _tileOverlayState[overlay.Id] = overlay;
            _hiddenTileOverlays.Remove(overlay.Id);
        }
    }

    /// <summary>
    /// Shows or hides a tile overlay without discarding it. Its definition is kept, so showing it
    /// again needs no re-declaration.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no overlay with that id has been added.</exception>
    public async ValueTask SetTileOverlayVisible(string overlayId, bool visible)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(overlayId);

        if (_tileOverlayState.TryGetValue(overlayId, out var overlay) is false)
        {
            throw new InvalidOperationException($"BitMap has no tile overlay with id '{overlayId}'.");
        }

        if (visible)
        {
            if (_hiddenTileOverlays.Remove(overlayId) is false) return;
            await SafeInvokeAsync(_js.BitMapAddTileOverlay(JsObject, _Id, ToTileOverlayPayload(overlay)), nameof(SetTileOverlayVisible));
        }
        else
        {
            if (_hiddenTileOverlays.Add(overlayId) is false) return;
            await SafeInvokeAsync(_js.BitMapRemoveTileOverlay(JsObject, _Id, overlayId), nameof(SetTileOverlayVisible));
        }
    }

    /// <summary>Whether a tile overlay is currently drawn. Unknown ids report false.</summary>
    public bool IsTileOverlayVisible(string overlayId)
        => _tileOverlayState.ContainsKey(overlayId) && _hiddenTileOverlays.Contains(overlayId) is false;

    /// <summary>
    /// Changes a tile overlay's opacity, which is how a raster overlay is usually blended against
    /// the basemap underneath it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no overlay with that id has been added.</exception>
    public async ValueTask SetTileOverlayOpacity(string overlayId, double opacity)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(overlayId);

        if (_tileOverlayState.TryGetValue(overlayId, out var overlay) is false)
        {
            throw new InvalidOperationException($"BitMap has no tile overlay with id '{overlayId}'.");
        }

        // Opacity clamps itself on the way in, so a non-finite or out-of-range value here is
        // corrected rather than rejected.
        var updated = overlay with { Opacity = opacity };
        _tileOverlayState[overlayId] = updated;

        if (_hiddenTileOverlays.Contains(overlayId)) return;
        await SafeInvokeAsync(_js.BitMapAddTileOverlay(JsObject, _Id, ToTileOverlayPayload(updated)), nameof(SetTileOverlayOpacity));
    }

    /// <summary>Remove a tile overlay by id.</summary>
    public async ValueTask RemoveTileOverlay(string overlayId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(overlayId);

        if (await SafeInvokeAsync(_js.BitMapRemoveTileOverlay(JsObject, _Id, overlayId), nameof(RemoveTileOverlay)))
        {
            _tileOverlayState.Remove(overlayId);
            _hiddenTileOverlays.Remove(overlayId);
        }
    }

    /// <summary>
    /// Remove every tile overlay, leaving the base map alone. The counterpart of
    /// <see cref="ClearMarkers"/> and <see cref="ClearVectorLayers"/>.
    /// </summary>
    public async ValueTask ClearTileOverlays()
    {
        EnsureReady();

        // Removed one at a time rather than through a bulk call: the overlays are keyed on the JS
        // side too, and a per-id remove is the one operation every backend already implements.
        foreach (var overlayId in _tileOverlayState.Keys.ToList())
        {
            if (_hiddenTileOverlays.Contains(overlayId)) continue;
            await SafeInvokeAsync(_js.BitMapRemoveTileOverlay(JsObject, _Id, overlayId), nameof(ClearTileOverlays));
        }

        _tileOverlayState.Clear();
        _hiddenTileOverlays.Clear();
    }



    [JSInvokable("OnClick")]
    public async Task _OnClick(JsonElement e)
    {
        // A click on the map is a click outside the popup - the usual way to dismiss one. The
        // popup itself never reaches here: it is a sibling of the canvas, not a part of it.
        if (_openPopupMarker is not null) await ClosePopup();

        await InvokeUserCallback(OnClick, ReadLatLng, e, nameof(OnClick));
    }

    [JSInvokable("OnDoubleClick")]
    public Task _OnDoubleClick(JsonElement e) => InvokeUserCallback(OnDoubleClick, ReadLatLng, e, nameof(OnDoubleClick));

    [JSInvokable("OnContextMenu")]
    public Task _OnContextMenu(JsonElement e) => InvokeUserCallback(OnContextMenu, ReadLatLng, e, nameof(OnContextMenu));

    [JSInvokable("OnViewChanged")]
    public async Task _OnViewChanged(JsonElement e)
    {
        BitMapViewState view;
        try { view = ParseViewState(e); }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnViewChanged));
            return;
        }

        _lastView = view;

        // Clusters are computed in screen space, so they are only correct for the zoom they were
        // computed at - a settled pan or zoom has to recompute them. The providers already coalesce
        // this notification, so this is one extra call per settled view change, not per frame.
        if (IsClustering) await SafeInvokeAsync(_js.BitMapClusterRender(_Id), nameof(Clustering));

        if (AnnounceViewChanges) await AnnounceView(view);

        await WriteCameraBackAsync(view);

        if (OnViewChanged.HasDelegate is false) return;
        try { await OnViewChanged.InvokeAsync(view); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnViewChanged)); }
    }

    /// <summary>
    /// Publishes a map-reported viewport to the two-way <see cref="Center"/> / <see cref="Zoom"/>
    /// parameters, with the echo guard raised so the resulting parameter write does not read as a
    /// request to move the map again.
    /// </summary>
    private async Task WriteCameraBackAsync(BitMapViewState view)
    {
        if (CenterChanged.HasDelegate is false && ZoomChanged.HasDelegate is false) return;

        _applyingCameraFromMap = true;
        try
        {
            // Each half is written only when it is actually bound. Assigning an unbound one would
            // still set the backing parameter, quietly turning a null "the provider owns this"
            // into a value the component then treats as a requested camera.
            if (CenterChanged.HasDelegate) await AssignCenter(view.Center);
            if (ZoomChanged.HasDelegate) await AssignZoom(view.Zoom);
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnViewChanged));
        }
        finally
        {
            _applyingCameraFromMap = false;
        }
    }

    [JSInvokable("OnMarkerClick")]
    public async Task _OnMarkerClick(string markerId)
    {
        // A cluster bubble is drawn as a marker, so it arrives here - but it is not one of the
        // caller's markers and must not be reported as one.
        if (IsClustering && markerId.StartsWith(ClusterIdPrefix, StringComparison.Ordinal))
        {
            await HandleClusterClick(markerId);
            return;
        }

        // A template popup opens on the same click that reports the marker, so a consumer who
        // wants both gets both.
        if (MarkerPopupTemplate is not null) await OpenPopup(markerId);

        if (OnMarkerClick.HasDelegate is false) return;
        try
        {
            await OnMarkerClick.InvokeAsync(markerId);
        }
        catch (Exception ex)
        {
            // A consumer-thrown exception in their event handler must not propagate back into JS
            // as an unhandled exception (which would surface as a circuit-breaking error in Blazor
            // Server / WASM). Surface via OnInteropError so observability isn't lost.
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnMarkerClick));
        }
    }

    /// <summary>
    /// Prefix the clustering layer stamps on a generated bubble's id. Kept in step with
    /// <c>BitMapCluster.clusterIdPrefix</c> in BitMapCluster.ts.
    /// </summary>
    private const string ClusterIdPrefix = "__bitmap_cluster_";

    private async Task HandleClusterClick(string clusterId)
    {
        var count = 0;
        try
        {
            // expand() reports how many markers the bubble stood for either way, and only zooms
            // when asked to - so the count costs no second round-trip, and a consumer who handles
            // the click themselves still learns how big the bubble was.
            count = await _js.BitMapClusterExpand(_Id, clusterId,
                Clustering!.ExpandPaddingPixels, Clustering!.ZoomOnClick);
        }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(OnClusterClick)); }

        if (OnClusterClick.HasDelegate is false) return;
        try { await OnClusterClick.InvokeAsync(new BitMapClusterClickArgs { ClusterId = clusterId, Count = count }); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnClusterClick)); }
    }

    [JSInvokable("OnMarkerDragEnd")]
    public async Task _OnMarkerDragEnd(string markerId, JsonElement position)
    {
        if (OnMarkerDragEnd.HasDelegate is false && _markerState.Count == 0) return;

        // Keep the snapshot fresh so a replay after a provider swap drops the marker at its
        // current dragged-to position rather than its original definition.
        BitMapLatLng pos;
        try { pos = ReadLatLng(position); }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnMarkerDragEnd));
            return;
        }

        if (_markerState.TryGetValue(markerId, out var prev))
        {
            _markerState[markerId] = CloneWithPosition(prev, pos);
            await NotifyMarkerListChanged();
        }

        if (OnMarkerDragEnd.HasDelegate is false) return;
        try
        {
            await OnMarkerDragEnd.InvokeAsync(new BitMapMarkerDragEndArgs { Id = markerId, Position = pos });
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnMarkerDragEnd));
        }
    }

    [JSInvokable("OnVectorClick")]
    public async Task _OnVectorClick(string layerId, string kind, JsonElement position)
    {
        if (OnVectorClick.HasDelegate is false) return;
        try
        {
            await OnVectorClick.InvokeAsync(new BitMapVectorClickArgs { LayerId = layerId, Kind = kind, Position = ReadLatLng(position) });
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnVectorClick));
        }
    }

    [JSInvokable("OnGeoJsonFeatureClick")]
    public async Task _OnGeoJsonFeatureClick(string layerId, JsonElement properties)
    {
        if (OnGeoJsonFeatureClick.HasDelegate is false) return;
        try
        {
            await OnGeoJsonFeatureClick.InvokeAsync(new BitMapGeoJsonFeatureClickArgs { LayerId = layerId, Properties = properties });
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnGeoJsonFeatureClick));
        }
    }

    [JSInvokable("OnFullscreenChanged")]
    public async Task _OnFullscreenChanged(bool isFullscreen)
    {
        if (_isFullscreen == isFullscreen) return;
        _isFullscreen = isFullscreen;
        await InvokeAsync(StateHasChanged);

        if (OnFullscreenChanged.HasDelegate is false) return;
        try { await OnFullscreenChanged.InvokeAsync(isFullscreen); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnFullscreenChanged)); }
    }

    [JSInvokable("OnRenderContextLost")]
    public async Task _OnRenderContextLost()
    {
        // The map is still "initialized" from the component's point of view - the instance and
        // its state are intact, only the GPU surface is gone - so IsReady stays true and the
        // consumer decides whether to tear down or wait for a restore.
        await SetLoadState(BitMapLoadState.Failed);
        try
        {
            await OnRenderContextLost.InvokeAsync();
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnRenderContextLost));
        }
    }

    [JSInvokable("OnRenderContextRestored")]
    public async Task _OnRenderContextRestored()
    {
        if (_initialized) await SetLoadState(BitMapLoadState.Ready);
        try
        {
            await OnRenderContextRestored.InvokeAsync();
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnRenderContextRestored));
        }
    }



    protected override string RootElementClass => "bit-map";

    protected override void OnInitialized()
    {
        _canvasId = $"{_Id}-canvas";
        _helpId = $"{_Id}-help";
        _hintId = $"{_Id}-hint";
        _hintTextId = $"{_Id}-hint-text";
        _popupAnchorId = $"{_Id}-popup-anchor";
        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        // Prerender / static SSR: there is no browser yet, so leave the component in Idle and
        // let the interactive render pass do the work.
        if (_js.IsRuntimeInvalid()) return;

        // Wait for visibility BEFORE taking the lifecycle gate: this can block for as long as it
        // takes the user to scroll, and holding the gate would stall a Provider change or a
        // dispose behind it.
        if (LazyLoad)
        {
            try
            {
                await _js.BitMapChromeWaitForVisible(_canvasId, _mapElement, LazyLoadRootMargin);
            }
            catch (Exception ex)
            {
                // A failed observer must not mean "no map ever" - fall through and initialize now.
                await RaiseInteropError(BitMapInteropErrorSource.Init, ex, nameof(LazyLoad));
            }
            if (Gone) return;
        }

        await SetLoadState(BitMapLoadState.Loading);

        // Teardown disposes the gate, and cancelling the visibility wait above resumes this
        // method inside DisposeAsync - so both the wait and the release have to tolerate a
        // gate that is already gone.
        try { await _lifecycleGate.WaitAsync(); }
        catch (ObjectDisposedException) { return; /* disposed mid-flight */ }

        try
        {
            if (Gone) return;

            var initial = Provider ?? new TMapProvider();
            BitMapValidation.ValidateJsObjectName(initial.JsObjectName);

            // A WebGL-backed provider on a browser that cannot give out the context version it
            // needs renders a permanently blank canvas. Check first so the consumer sees a message
            // instead of an empty box.
            if (await HasWebGlSupport(initial.WebGlRequirement) is false)
            {
                await SetLoadState(BitMapLoadState.Unsupported);
                return;
            }

            // A failed script load leaves the map unbuildable, but it must not escape as an
            // unhandled exception - on Blazor Server that tears down the circuit, so a CDN
            // outage would take the whole page with it instead of showing the error state.
            if (await LoadAssetsAsync(initial) is false) return;
            if (Gone) return;

            // Build the options payload outside the interop try/catch so that provider
            // configuration errors (missing tokens, invalid URLs, etc.) surface to the
            // caller instead of being swallowed and leaving the map silently uninitialized.
            // Built before creating the DotNetObjectReference so a payload exception
            // doesn't leak a live interop handle for a map that never initializes.
            var initOptions = initial.BuildOptionsPayload();

            _activeProvider = initial;
            _dotnetObj = DotNetObjectReference.Create(this);

            try
            {
                await _js.BitMapInit(initial.JsObjectName, _Id, _canvasId, _mapElement, _dotnetObj, initOptions);
            }
            catch (JSDisconnectedException ex)
            {
                CleanupFailedInit();
                await SetLoadState(BitMapLoadState.Failed, ex);
                await RaiseInteropError(BitMapInteropErrorSource.Init, ex, "first render");
                return;
            }
            catch (Exception ex)
            {
                // Most common cause: the canvas div was removed from the DOM before JS init ran
                // (parent component re-render or page navigation). Tear down rather than leaking
                // an interop handle.
                CleanupFailedInit();
                await SetLoadState(BitMapLoadState.Failed, ex);
                await RaiseInteropError(BitMapInteropErrorSource.Init, ex, "first render");
                return;
            }

            _initialized = true;
            await AttachChromeAsync();
            _prefersReducedMotion = await ReadReducedMotionPreference();
        }
        finally
        {
            try { _lifecycleGate.Release(); } catch (ObjectDisposedException) { }
        }

        if (_initialized && Gone is false)
        {
            await SetLoadState(BitMapLoadState.Ready);

            // Bound parameters were assigned before the map existed, so apply them now. The camera
            // goes first: markers added against the pre-move viewport would otherwise be placed
            // and then immediately panned away from.
            _cameraParametersDirty = false;
            await PushCameraParametersAsync();
            // Clustering is configured before the markers land so the first set is grouped on the
            // way in rather than drawn individually and then regrouped.
            if (IsClustering) await ApplyClusteringAsync();
            if (Markers is not null) await ApplyMarkersAsync();
        }

        try
        {
            await OnReady.InvokeAsync();
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnReady));
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (_cameraParametersDirty)
        {
            _cameraParametersDirty = false;
            await PushCameraParametersAsync();
        }

        // Re-install the chrome only when one of the options that shapes it actually changed.
        // Attaching rebuilds observers and listeners on the JS side, so doing it on every
        // re-render of a live map would be a wasted interop round-trip per render.
        if (_initialized && _chromeSignature != BuildChromeSignature())
        {
            await AttachChromeAsync();
        }
    }



    /// <summary>
    /// Records that <see cref="Center"/> or <see cref="Zoom"/> changed. The move itself is
    /// deferred to <c>OnParametersSetAsync</c> so a render that changed both pays one interop
    /// call rather than two.
    /// </summary>
    private ValueTask OnCameraParameterSet()
    {
        // A write that came from the map itself is an echo, not a request to move.
        if (_applyingCameraFromMap is false) _cameraParametersDirty = true;
        return ValueTask.CompletedTask;
    }

    private async ValueTask PushCameraParametersAsync()
    {
        if (_initialized is false) return;
        if (Center is null && Zoom is null) return;

        // Skip when the map is already there. Without this the loop never settles: moving the map
        // writes Center back, the parent re-renders, and that write would move the map again.
        if (_lastView is not null
            && (Center is null || AreCoordinatesEquivalent(Center.Value, _lastView.Center))
            && (Zoom is null || Math.Abs(Zoom.Value - _lastView.Zoom) < ZoomEpsilon))
        {
            return;
        }

        var center = Center ?? _lastView?.Center;

        // A camera the consumer asked for is not a gesture the user is watching, so it jumps
        // rather than animating - the same call with animate:true would fight a rapid @bind write.
        if (center is null)
        {
            // Zoom is bound but Center is not, and the map has not reported a viewport yet - so
            // there is no centre to restate. Push the zoom on its own rather than dropping it,
            // which is what an initial `@bind-Zoom` with no `@bind-Center` would otherwise do.
            // SetZoom reads the centre back over interop here, and this runs inside
            // OnParametersSetAsync - so a provider whose getView fails must not escape as an
            // unhandled exception from a render.
            if (Zoom is null) return;
            try { await SetZoom(Zoom.Value, animate: false); }
            catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(Zoom)); }
            return;
        }

        // Same reasoning as the SetZoom branch above: this runs inside OnParametersSetAsync, so
        // a bound Zoom outside the provider's min/max - or a provider whose setView fails - must
        // surface through OnInteropError instead of escaping a render as an unhandled exception.
        try { await SetView(center.Value, Zoom, animate: false); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, nameof(Center)); }
    }

    /// <summary>
    /// Two coordinates that differ below this are the same place for binding purposes. A map
    /// reports its centre as a float, so an exact comparison against a bound value would treat
    /// every echo as a change.
    /// </summary>
    private const double CoordinateEpsilon = 1e-9;
    private const double ZoomEpsilon = 1e-9;

    private static bool AreCoordinatesEquivalent(BitMapLatLng a, BitMapLatLng b)
        => Math.Abs(a.Latitude - b.Latitude) < CoordinateEpsilon
        && Math.Abs(a.Longitude - b.Longitude) < CoordinateEpsilon;

    /// <summary>
    /// True while the clustering layer, rather than the provider, decides which markers are drawn.
    /// </summary>
    private bool IsClustering => Clustering is not null;

    private async ValueTask OnClusteringSet()
    {
        if (_initialized is false) return;
        await ApplyClusteringAsync();
    }

    /// <summary>
    /// Enables, reconfigures or disables the clustering layer, then hands it the current marker set.
    /// Disabling gives the full set straight back to the provider, so turning it off leaves the map
    /// exactly as it would have been without it.
    /// </summary>
    private async ValueTask ApplyClusteringAsync()
    {
        if (IsClustering is false)
        {
            await SafeInvokeAsync(_js.BitMapClusterDisable(_Id), nameof(Clustering));
            return;
        }

        var options = Clustering!;
        if (await SafeInvokeAsync(_js.BitMapClusterConfigure(_Id, JsObject, new Dictionary<string, object?>
        {
            ["radius"] = options.RadiusPixels,
            ["maxZoom"] = options.MaxZoom,
            ["minPoints"] = options.MinPoints,
            ["color"] = options.Color,
            ["textColor"] = options.TextColor,
            ["cullOffscreen"] = options.CullOffscreen,
            ["maxRenderedMarkers"] = options.MaxRenderedMarkers,
            ["ariaLabelFormat"] = options.AriaLabelFormat,
        }), nameof(Clustering)) is false) return;

        await PushClusteredMarkersAsync(nameof(Clustering));
    }

    /// <summary>
    /// Hands the whole marker snapshot to the clustering layer, which decides what the provider
    /// actually draws. One call regardless of how many markers changed - the layer needs the full
    /// set anyway to recompute the grouping.
    /// </summary>
    private async ValueTask<bool> PushClusteredMarkersAsync(string callSite)
    {
        var ids = new string[_markerState.Count];
        var payloads = new object[_markerState.Count];
        var i = 0;
        foreach (var (id, marker) in _markerState)
        {
            ids[i] = id;
            payloads[i] = ToMarkerPayload(marker);
            i++;
        }

        return await SafeInvokeAsync(_js.BitMapClusterSetMarkers(_Id, ids, payloads), callSite);
    }

    private async ValueTask OnMarkersSet()
    {
        // Before the map exists there is nothing to reconcile against; OnAfterRenderAsync applies
        // the initial collection once the provider is ready.
        if (_initialized is false) return;
        await ApplyMarkersAsync();
    }

    /// <summary>
    /// Reconciles the live marker set with <see cref="Markers"/>.
    /// <para>
    /// Markers compare by value, so this can tell an unchanged pin from a moved one and leave the
    /// unchanged ones alone - which matters beyond performance: re-adding a marker closes its open
    /// popup and drops its keyboard focus.
    /// </para>
    /// </summary>
    private async ValueTask ApplyMarkersAsync()
    {
        var openId = _openPopupMarker?.Id;

        var desired = Markers as IReadOnlyList<BitMapMarker> ?? Markers?.ToList() ?? [];

        var desiredById = new Dictionary<string, BitMapMarker>(desired.Count, StringComparer.Ordinal);
        foreach (var marker in desired)
        {
            ArgumentNullException.ThrowIfNull(marker);
            BitMapValidation.ValidateId(marker.Id, $"{nameof(BitMapMarker)}.{nameof(BitMapMarker.Id)}");
            // Last one wins, matching what the JS layer does with a duplicate id.
            desiredById[marker.Id] = marker;
        }

        var stale = _markerState.Keys.Where(id => desiredById.ContainsKey(id) is false).ToList();
        var changed = desired.Where(m => _markerState.TryGetValue(m.Id, out var live) is false || live != m).ToList();

        if (stale.Count == 0 && changed.Count == 0) return;

        // Past roughly half the set, one batched replace beats a stream of individual calls -
        // each of those is its own interop round-trip, and a SignalR message under Blazor Server.
        if (desired.Count > 0 && stale.Count + changed.Count >= Math.Max(2, (desired.Count + 1) / 2))
        {
            await SyncMarkers(desired);
        }
        else
        {
            foreach (var id in stale)
            {
                await RemoveMarker(id);
            }

            foreach (var marker in changed)
            {
                // AddMarker replaces a marker that already carries this id, so an update needs no
                // separate remove.
                await AddMarker(marker);
            }
        }

        // Reached on both paths: a wholesale replace is exactly the case where an open popup is
        // most likely to be pointing at a marker that is gone or has moved.
        await ReconcileOpenPopupAsync(openId);
    }

    /// <summary>
    /// Keeps an open popup in step with the marker it belongs to: closed if that marker is gone,
    /// re-anchored and re-rendered if it moved or its data changed.
    /// </summary>
    private async ValueTask ReconcileOpenPopupAsync(string? openId)
    {
        if (openId is null || _openPopupMarker is null) return;

        if (_markerState.TryGetValue(openId, out var current) is false)
        {
            await ClosePopup();
            return;
        }

        if (current == _openPopupMarker) return;

        _openPopupMarker = current;
        await InvokeAsync(StateHasChanged);
        await SafeInvokeAsync(
            _js.BitMapChromeTrackAnchor(_Id, _popupAnchorId, current.Position.Latitude, current.Position.Longitude),
            nameof(OpenPopup));
    }

    private async ValueTask OnProviderSet()
    {
        if (_js.IsRuntimeInvalid()) return;

        try { await _lifecycleGate.WaitAsync(); }
        catch (ObjectDisposedException) { return; /* disposed mid-flight */ }

        try
        {
            // First render hasn't run yet, or a previous init failed - nothing to sync.
            if (_initialized is false || _activeProvider is null) return;

            // When Provider is reset to null on a live component, treat it as "no change". The
            // alternative (silently swap to a default-constructed provider) would surprise
            // callers who simply un-set the parameter during a parent-component re-render.
            if (Provider is null) return;

            var effective = Provider;
            BitMapValidation.ValidateJsObjectName(effective.JsObjectName);

            var jsObjectChanged = !string.Equals(_activeProvider.JsObjectName, effective.JsObjectName, StringComparison.Ordinal);

            // A failed asset load leaves the current map alone rather than propagating out of
            // SetParametersAsync, where it would surface as an unhandled exception.
            if (await LoadAssetsAsync(effective) is false) return;
            if (Gone) return;

            if (jsObjectChanged)
            {
                await SwapProviderAsync(effective);
                return;
            }

            // Same JS object - just sync the updated options. Build the payload outside the
            // try/catch so configuration errors surface instead of being swallowed.
            var syncOptions = effective.BuildOptionsPayload();

            try
            {
                await _js.BitMapSync(effective.JsObjectName, _Id, syncOptions);
            }
            catch (JSDisconnectedException ex)
            {
                await RaiseInteropError(BitMapInteropErrorSource.Sync, ex);
                return;
            }
            catch (Exception ex)
            {
                await RaiseInteropError(BitMapInteropErrorSource.Sync, ex);
                return;
            }

            _activeProvider = effective;
        }
        finally
        {
            try { _lifecycleGate.Release(); } catch (ObjectDisposedException) { }
        }
    }

    private async ValueTask SwapProviderAsync(TMapProvider effective)
    {
        // Build (and implicitly validate) the new provider's options payload BEFORE
        // disposing the active map. If BuildOptionsPayload throws (missing token, invalid
        // URL, out-of-range zoom, etc.) the current map stays intact and the caller sees
        // the configuration error instead of being left with a torn-down backend that
        // reports IsReady=true.
        var swapInitOptions = effective.BuildOptionsPayload();

        if (Gone) return;

        // The new backend may need WebGL - or a newer WebGL - than the old one did. Check before
        // tearing the working map down, so an unsupported swap leaves the existing map alone.
        if (await HasWebGlSupport(effective.WebGlRequirement) is false)
        {
            await SetLoadState(BitMapLoadState.Unsupported);
            return;
        }

        try { await _js.BitMapChromeDetach(_Id); } catch { /* best effort */ }

        try
        {
            await _js.BitMapDispose(_activeProvider!.JsObjectName, _Id);
        }
        catch (JSDisconnectedException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Dispose, ex, "provider swap");
            return;
        }
        catch (Exception ex)
        {
            // Best-effort dispose. Continue with re-init regardless.
            await RaiseInteropError(BitMapInteropErrorSource.Dispose, ex, "provider swap");
        }

        // The old JS instance is gone (or could not be disposed cleanly). Clear the
        // ready/active state up front so a failed re-init below cannot leave the
        // component reporting IsReady=true while pointing at a disposed backend.
        _initialized = false;
        _activeProvider = null;
        await SetLoadState(BitMapLoadState.Loading);

        if (Gone) return;

        // The old DotNetObjectReference is still bound to the disposed JS instance. Recycle it
        // for the new init: dispose it so we don't leak the GC handle and create a fresh one.
        _dotnetObj?.Dispose();
        _dotnetObj = DotNetObjectReference.Create(this);

        try
        {
            await _js.BitMapInit(effective.JsObjectName, _Id, _canvasId, _mapElement, _dotnetObj, swapInitOptions);
        }
        catch (JSDisconnectedException ex)
        {
            CleanupFailedInit();
            await SetLoadState(BitMapLoadState.Failed, ex);
            await RaiseInteropError(BitMapInteropErrorSource.Init, ex, "provider swap");
            return;
        }
        catch (Exception ex)
        {
            CleanupFailedInit();
            await SetLoadState(BitMapLoadState.Failed, ex);
            await RaiseInteropError(BitMapInteropErrorSource.Init, ex, "provider swap");
            return;
        }

        _activeProvider = effective;
        _initialized = true;
        await AttachChromeAsync();
        // The camera goes back first: the new map was built from the provider's own Center/Zoom,
        // which is not necessarily where the user had panned to.
        _cameraParametersDirty = false;
        await PushCameraParametersAsync();
        // The clustering layer holds the name of the JS object it renders through, so a swap to a
        // different backend has to re-point it or it would keep syncing markers to the old one.
        // This also hands it the whole marker set again, which is why the replay below skips
        // markers while it is on.
        if (IsClustering) await ApplyClusteringAsync();
        await SetLoadState(BitMapLoadState.Ready);

        if (ReplayStateOnProviderSwap)
        {
            await ReplayImperativeStateAsync();
        }
        else if (Markers is not null && IsClustering is false)
        {
            // The declarative collection is this component's own state, not something the consumer
            // applied imperatively - so it is restored whether or not the replay opt-in is set.
            // Without this a provider swap silently empties a `Markers`-driven map.
            _markerState.Clear();
            await ApplyMarkersAsync();
        }

        // Fire OnReady again so consumers can rebuild their map state on the new provider.
        try
        {
            await OnReady.InvokeAsync();
        }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnReady));
        }
    }

    /// <summary>
    /// Loads the provider's stylesheets and scripts, deduped process-wide.
    /// Returns false when the scripts could not be loaded, which means the map cannot be built.
    /// </summary>
    private async ValueTask<bool> LoadAssetsAsync(TMapProvider provider)
    {
        // Process-wide dedup so mounting/unmounting multiple BitMaps over the same provider
        // doesn't pay an interop round-trip per mount. The browser dedupes by URL too, but
        // skipping the round-trip avoids serialising the URL list and waiting for a JS
        // promise that does nothing useful.
        var pendingStylesheets = BitMapAssetCache.FilterUnloadedStylesheets(provider.Stylesheets);
        if (pendingStylesheets.Count > 0)
        {
            try
            {
                await _js.BitExtrasInitStylesheets(pendingStylesheets);
                BitMapAssetCache.MarkStylesheetsLoaded(pendingStylesheets);
            }
            catch (Exception ex)
            {
                // A failed CDN stylesheet load shouldn't prevent the map from initializing -
                // the providers degrade gracefully (e.g., OpenLayers will still work, just
                // with unstyled controls if its CSS failed to load).
                await RaiseInteropError(BitMapInteropErrorSource.StylesheetLoad, ex);
            }
        }

        if (Gone) return false;

        var pendingScripts = BitMapAssetCache.FilterUnloadedScripts(provider.Scripts);
        if (pendingScripts.Count > 0)
        {
            try
            {
                await _js.BitExtrasInitScripts(pendingScripts, provider.ScriptsAreModules);
                BitMapAssetCache.MarkScriptsLoaded(pendingScripts);
            }
            catch (Exception ex)
            {
                await RaiseInteropError(BitMapInteropErrorSource.ScriptLoad, ex);
                await SetLoadState(BitMapLoadState.Failed, ex);
                return false; // without scripts the map cannot initialize
            }
        }

        return true;
    }

    private async ValueTask ReplayImperativeStateAsync()
    {
        // Replay everything that was added imperatively, in whatever order the snapshot
        // dictionaries enumerate. We tolerate individual failures per item so a single bad payload
        // doesn't abort the rest of the restore.
        //
        // Markers are skipped while clustering is on: the clustering layer was just re-pointed at
        // the new backend and handed the whole set, so adding each one again here would draw every
        // marker a second time, on top of the bubbles that stand for them.
        if (IsClustering is false)
        {
            foreach (var (id, marker) in _markerState)
            {
                try { await _js.BitMapAddMarker(JsObject, _Id, id, ToMarkerPayload(marker)); }
                catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, $"replay marker '{id}'"); }
            }
        }

        foreach (var snap in _vectorState.Values)
        {
            // A layer the caller hid stays hidden across the swap - replaying it visible would
            // silently undo their choice.
            if (_hiddenLayers.Contains(snap.LayerId)) continue;
            await AddVectorLayerAsync(snap, $"replay layer '{snap.LayerId}'");
        }

        foreach (var overlay in _tileOverlayState.Values)
        {
            if (_hiddenTileOverlays.Contains(overlay.Id)) continue;
            await SafeInvokeAsync(_js.BitMapAddTileOverlay(JsObject, _Id, ToTileOverlayPayload(overlay)), $"replay tile overlay '{overlay.Id}'");
        }
    }

    /// <summary>
    /// Draws a vector layer from its stored definition. Adding under an id that already exists
    /// replaces it, so this doubles as the restyle and re-show path.
    /// </summary>
    private ValueTask<bool> AddVectorLayerAsync(VectorLayerSnapshot snapshot, string callSite) => snapshot switch
    {
        PolylineSnapshot p => SafeInvokeAsync(_js.BitMapAddPolyline(JsObject, _Id, p.LayerId, ToLatLngArray(p.Path), ToStylePayload(p.Style)), callSite),
        PolygonSnapshot p => SafeInvokeAsync(_js.BitMapAddPolygon(JsObject, _Id, p.LayerId, ToLatLngArray(p.Ring), ToStylePayload(p.Style)), callSite),
        CircleSnapshot c => SafeInvokeAsync(_js.BitMapAddCircle(JsObject, _Id, c.LayerId, c.Center.Latitude, c.Center.Longitude, c.RadiusMeters, ToStylePayload(c.Style)), callSite),
        RectangleSnapshot r => SafeInvokeAsync(_js.BitMapAddRectangle(JsObject, _Id, r.LayerId,
            r.Bounds.SouthWest.Latitude, r.Bounds.SouthWest.Longitude,
            r.Bounds.NorthEast.Latitude, r.Bounds.NorthEast.Longitude,
            ToStylePayload(r.Style)), callSite),
        GeoJsonSnapshot g => SafeInvokeAsync(_js.BitMapAddGeoJson(JsObject, _Id, g.LayerId, g.GeoJson, ToStylePayload(g.Style)), callSite),
        _ => ValueTask.FromResult(false),
    };



    /// <summary>
    /// Installs (or re-installs) the provider-agnostic chrome: container resize observation,
    /// cooperative gestures, keyboard escape, and WebGL context-loss reporting. Kept out of the
    /// provider implementations so all seven backends behave identically.
    /// </summary>
    private async ValueTask AttachChromeAsync()
    {
        if (_initialized is false || _activeProvider is null || _dotnetObj is null) return;

        _chromeSignature = BuildChromeSignature();

        try
        {
            await _js.BitMapChromeAttach(_Id, _canvasId, _mapElement, _dotnetObj, new Dictionary<string, object?>
            {
                ["jsObjectName"] = _activeProvider.JsObjectName,
                ["autoResize"] = AutoResize,
                ["cooperativeGestures"] = CooperativeGestures,
                ["hintId"] = CooperativeGestures ? _hintId : null,
                ["hintTextId"] = CooperativeGestures ? _hintTextId : null,
                ["wheelHint"] = CooperativeGesturesWheelHint,
                ["touchHint"] = CooperativeGesturesTouchHint,
                ["escapeToExit"] = EscapeToExit,
            });
        }
        catch (Exception ex)
        {
            // The chrome is an enhancement; a failure here must not take the map down with it.
            await RaiseInteropError(BitMapInteropErrorSource.Init, ex, nameof(AttachChromeAsync));
        }
    }

    /// <summary>
    /// Everything the chrome is configured from, as one comparable value. Used to skip a
    /// re-attach when a re-render did not actually change any of it.
    /// </summary>
    private (string?, bool, bool, string, string, bool) BuildChromeSignature() => (
        _activeProvider?.JsObjectName,
        AutoResize,
        CooperativeGestures,
        CooperativeGesturesWheelHint,
        CooperativeGesturesTouchHint,
        EscapeToExit);

    private async ValueTask<bool> HasWebGlSupport(BitMapWebGlRequirement requirement)
    {
        if (requirement == BitMapWebGlRequirement.None) return true;

        try { return await _js.BitMapChromeHasWebGl((int)requirement); }
        catch { return true; /* can't tell - assume yes rather than block a working browser */ }
    }

    private async ValueTask<bool> ReadReducedMotionPreference()
    {
        if (RespectReducedMotion is false) return false;
        try { return await _js.BitMapChromePrefersReducedMotion(); }
        catch { return false; }
    }

    /// <summary>
    /// Decides whether a camera move animates. Reduced motion wins over the caller's
    /// <paramref name="animate"/>, except for a move the caller marked essential.
    /// </summary>
    private bool ShouldAnimate(bool animate, bool essential)
    {
        if (animate is false) return false;
        if (essential) return true;
        return (RespectReducedMotion && _prefersReducedMotion) is false;
    }

    private async Task AnnounceView(BitMapViewState view)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastAnnouncementAt < ViewAnnouncementThrottle)
        {
            // Inside the throttle window. Dropping this outright would lose the announcement the
            // user actually cares about - the one for where the map came to rest - so it is held
            // and spoken when the window closes instead.
            _pendingAnnouncementView = view;
            ScheduleTrailingAnnouncement();
            return;
        }

        _pendingAnnouncementView = null;
        _lastAnnouncementAt = now;
        await PublishAnnouncement(view);
    }

    /// <summary>
    /// Writes the announcement text into the live region. A screen reader ignores a live region
    /// whose text did not change, so an identical announcement is nudged with a trailing
    /// zero-width space rather than being silently swallowed.
    /// </summary>
    private async Task PublishAnnouncement(BitMapViewState view)
    {
        var text = DefaultAnnouncement(view);

        if (ViewAnnouncementFormatter is not null)
        {
            // Reached from a [JSInvokable] callback and from the trailing-announcement timer.
            // A throwing formatter would escape the first as an unhandled interop exception and
            // the second as an unobserved task exception, so it is treated like any other
            // consumer callback: reported, and the built-in wording used instead.
            try { text = ViewAnnouncementFormatter(view) ?? text; }
            catch (Exception ex)
            {
                await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(ViewAnnouncementFormatter));
            }
        }

        _announcement = string.Equals(_announcement, text, StringComparison.Ordinal)
            ? text + AnnouncementNudge
            : text;

        // Reached from a JS callback, which does not necessarily arrive on the renderer's
        // dispatcher - so the re-render is marshalled onto it rather than assumed.
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Zero-width space appended to an announcement identical to the one already in the live
    /// region. It changes the text - which is what makes the region announce again - without
    /// changing a single spoken character.
    /// </summary>
    private const string AnnouncementNudge = "\u200b";

    private static string DefaultAnnouncement(BitMapViewState view)
        => $"Map centred at {view.Center.Latitude:F4}, {view.Center.Longitude:F4}, zoom level {view.Zoom:F0}.";

    private BitMapViewState? _pendingAnnouncementView;
    private CancellationTokenSource? _announcementCts;

    /// <summary>
    /// Arms a single timer to speak the most recent held-back view once the throttle window has
    /// passed. Re-arming replaces the previous timer, so a continuous drag produces exactly one
    /// trailing announcement rather than one per event.
    /// </summary>
    private void ScheduleTrailingAnnouncement()
    {
        _announcementCts?.Cancel();
        _announcementCts?.Dispose();
        var cts = new CancellationTokenSource();
        _announcementCts = cts;

        var due = ViewAnnouncementThrottle - (DateTimeOffset.UtcNow - _lastAnnouncementAt);
        if (due < TimeSpan.Zero) due = TimeSpan.Zero;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(due, cts.Token);
                if (cts.IsCancellationRequested || Gone) return;

                var view = _pendingAnnouncementView;
                if (view is null || AnnounceViewChanges is false) return;

                _pendingAnnouncementView = null;
                _lastAnnouncementAt = DateTimeOffset.UtcNow;
                await PublishAnnouncement(view);
            }
            catch (OperationCanceledException) { /* superseded by a newer view */ }
            catch (ObjectDisposedException) { /* the component went away mid-wait */ }
            catch (Exception ex)
            {
                // Nothing awaits this task, so anything escaping it would surface as an
                // unobserved exception on the finalizer thread rather than at the consumer.
                await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(AnnounceViewChanges));
            }
        }, cts.Token);
    }

    private async ValueTask SetLoadState(BitMapLoadState state, Exception? error = null)
    {
        if (_loadState == state && ReferenceEquals(_loadError, error)) return;

        _loadState = state;
        _loadError = error;
        // Also reached from the WebGL context-loss callback, so marshal rather than assume.
        await InvokeAsync(StateHasChanged);

        if (OnLoadStateChanged.HasDelegate is false) return;
        try { await OnLoadStateChanged.InvokeAsync(state); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnLoadStateChanged)); }
    }



    /// <summary>
    /// Close button handler. A separate Task-returning wrapper because Blazor's event binding
    /// cannot take a ValueTask-returning method group.
    /// </summary>
    private async Task ClosePopupFromUi() => await ClosePopup(restoreFocus: true);

    /// <summary>
    /// Escape closes the popup and returns focus to the map, which is where the user was before
    /// it opened. Without this the popup is a keyboard trap the WAI-ARIA dialog pattern warns about.
    /// </summary>
    private async Task HandlePopupKeyDown(KeyboardEventArgs e)
    {
        if (e.Key != "Escape") return;
        await ClosePopup(restoreFocus: true);
    }

    private Task NotifyMarkerListChanged()
        => MarkerListMode == BitMapMarkerListMode.None ? Task.CompletedTask : InvokeAsync(StateHasChanged);

    private string MarkerListClass => MarkerListMode == BitMapMarkerListMode.ScreenReaderOnly
        ? "bit-map-marker-list bit-map-help"
        : "bit-map-marker-list";

    /// <summary>
    /// What a marker is called in the list. Falls back through the fields most likely to be
    /// meaningful, and only reaches the id - which is usually a database key - as a last resort.
    /// </summary>
    private static string DescribeMarker(BitMapMarker marker)
        => marker.Alt ?? marker.Title ?? marker.PopupText ?? marker.Id;

    /// <summary>
    /// Brings a marker into view from its row in the list, and reports the same click the marker
    /// itself would have - so a keyboard user reaches the behaviour a mouse user gets by clicking
    /// the pin.
    /// </summary>
    private async Task ShowMarker(string markerId)
    {
        if (_initialized is false) return;
        if (_markerState.TryGetValue(markerId, out var marker) is false) return;

        // Essential motion: the movement is what tells the user where they were taken.
        await SetView(marker.Position, MarkerListZoom, animate: true, essential: true);

        // Whichever popup this map actually uses - the Blazor-rendered one when a template is set,
        // the provider's otherwise.
        if (MarkerPopupTemplate is not null) await OpenPopup(markerId);
        else await OpenMarkerPopup(markerId);

        if (OnMarkerClick.HasDelegate is false) return;
        try { await OnMarkerClick.InvokeAsync(markerId); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, nameof(OnMarkerClick)); }
    }

    private string JsObject => _activeProvider!.JsObjectName;

    private static object ToMarkerPayload(BitMapMarker m) => new Dictionary<string, object?>
    {
        ["lat"] = m.Position.Latitude,
        ["lng"] = m.Position.Longitude,
        ["title"] = m.Title,
        ["alt"] = m.Alt,
        ["popupHtml"] = m.PopupHtml?.Value,
        ["popupText"] = m.PopupText,
        ["tooltipHtml"] = m.TooltipHtml?.Value,
        ["tooltipText"] = m.TooltipText,
        ["tooltipPermanent"] = m.TooltipPermanent,
        ["tooltipDirection"] = m.TooltipDirection.ToString().ToLowerInvariant(),
        ["focusable"] = m.Focusable,
        ["draggable"] = m.Draggable,
        ["iconUrl"] = m.IconUrl,
        ["iconWidth"] = m.IconWidth,
        ["iconHeight"] = m.IconHeight,
        ["iconAnchorX"] = m.IconAnchorX,
        ["iconAnchorY"] = m.IconAnchorY,
        ["opacity"] = m.Opacity,
        ["riseOnHover"] = m.RiseOnHover,
        ["zIndexOffset"] = m.ZIndexOffset,
    };

    private static object ToTileOverlayPayload(BitMapTileOverlay o) => new Dictionary<string, object?>
    {
        ["id"] = o.Id,
        ["urlTemplate"] = o.UrlTemplate,
        ["attribution"] = o.Attribution,
        ["opacity"] = o.Opacity,
        ["zIndex"] = o.ZIndex,
        ["maxZoom"] = o.MaxZoom,
        ["minZoom"] = o.MinZoom,
        ["subdomains"] = o.Subdomains,
    };

    private static BitMapMarker CloneWithPosition(BitMapMarker prev, BitMapLatLng position)
        => prev with { Position = position };

    private static object[] ToLatLngArray(IReadOnlyList<BitMapLatLng> pts)
    {
        var arr = new object[pts.Count];
        for (var i = 0; i < pts.Count; i++)
        {
            arr[i] = new Dictionary<string, object?> { ["lat"] = pts[i].Latitude, ["lng"] = pts[i].Longitude };
        }
        return arr;
    }

    private static object? ToStylePayload(BitMapVectorPathStyle? s) => s is null
        ? null
        : new Dictionary<string, object?>
        {
            ["color"] = s.Color,
            ["weight"] = s.Weight,
            ["opacity"] = s.Opacity,
            ["fillColor"] = s.FillColor,
            ["fillOpacity"] = s.FillOpacity,
            ["dashArray"] = s.DashArray,
            ["dashOffset"] = s.DashOffset,
            ["lineCap"] = s.LineCap.ToString().ToLowerInvariant(),
            ["lineJoin"] = s.LineJoin.ToString().ToLowerInvariant(),
            ["fill"] = s.Fill,
        };

    private static BitMapLatLng ReadLatLng(JsonElement e) =>
        new(e.GetProperty("lat").GetDouble(), e.GetProperty("lng").GetDouble());

    private static BitMapViewState ParseViewState(JsonElement e)
    {
        var center = ReadLatLng(e.GetProperty("center"));
        var zoom = e.GetProperty("zoom").GetDouble();
        var b = e.GetProperty("bounds");
        var sw = ReadLatLng(b.GetProperty("southWest"));
        var ne = ReadLatLng(b.GetProperty("northEast"));
        return new BitMapViewState
        {
            Center = center,
            Zoom = zoom,
            Bounds = new BitMapLatLngBounds(sw, ne),
        };
    }

    private void EnsureReady()
    {
        if (_initialized is false)
        {
            throw new InvalidOperationException("BitMap is not ready yet. Wait for the OnReady event before calling map methods.");
        }
    }

    private void CleanupFailedInit()
    {
        // The DotNetObjectReference we created for this failed init still owns a GC handle to
        // this component. Release it so we don't keep a live interop reference around for a map
        // that never wired up.
        _dotnetObj?.Dispose();
        _dotnetObj = null;
        _activeProvider = null;
        _initialized = false;
    }

    private async Task InvokeUserCallback<T>(EventCallback<T> callback, Func<JsonElement, T> reader, JsonElement payload, string source)
    {
        if (callback.HasDelegate is false) return;
        T value;
        try { value = reader(payload); }
        catch (Exception ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, source);
            return;
        }

        try { await callback.InvokeAsync(value); }
        catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Callback, ex, source); }
    }

    private async ValueTask RaiseInteropError(BitMapInteropErrorSource source, Exception exception, string? context = null)
    {
        if (OnInteropError.HasDelegate is false) return;
        try
        {
            await OnInteropError.InvokeAsync(new BitMapInteropErrorArgs { Source = source, Exception = exception, Context = context });
        }
        catch
        {
            // Last-resort safety net: if the consumer's error handler also throws, swallow. We
            // cannot do anything more useful here without risking a JS-side circuit break.
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        // Flag teardown before the first await: IsDisposed is only set once the base class has
        // run, so without this every continuation resuming below still sees a live component.
        _disposing = true;

        // Acquire the lifecycle gate so we don't race with an in-flight provider swap.
        try { await _lifecycleGate.WaitAsync(); }
        catch (ObjectDisposedException) { /* already gone */ }

        try
        {
            _announcementCts?.Cancel();
            _announcementCts?.Dispose();
            _announcementCts = null;

            _dotnetObj?.Dispose();
            _dotnetObj = null;

            try
            {
                // A map disposed while still below the fold is still being watched by the
                // visibility observer that would have started it. Stop that first: nothing else
                // holds a reference to it, so nothing else would ever take it down.
                if (LazyLoad) await _js.BitMapChromeCancelWaitForVisible(_canvasId);
                // Detach the chrome next: its observers and listeners are ours, and the
                // provider's dispose is free to remove the container out from under them.
                await _js.BitMapChromeDetach(_Id);
                // The clustering layer keeps the whole marker set alive; drop it with the map.
                // Discard rather than disable: disabling hands the full unclustered set back to
                // the provider, and the provider is being destroyed on the next line.
                await _js.BitMapClusterDiscard(_Id);
            }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { /* runtime unavailable during prerender teardown */ }

            try
            {
                if (_initialized && _activeProvider is not null)
                {
                    await _js.BitMapDispose(_activeProvider.JsObjectName, _Id);
                }
            }
            catch (JSDisconnectedException) { }
            catch (JSException) { /* a misbehaving provider's dispose must not crash teardown */ }
            catch (ObjectDisposedException) { }

            _initialized = false;
            _activeProvider = null;
        }
        finally
        {
            // Release before disposing so anything already waiting on the gate observes an
            // ObjectDisposedException from WaitAsync rather than deadlocking - OnProviderSet
            // catches exactly that and bails out.
            try { _lifecycleGate.Release(); } catch (ObjectDisposedException) { }
            _lifecycleGate.Dispose();
        }

        await base.DisposeAsync(disposing);
    }

    /// <summary>
    /// Awaits a JS interop call and surfaces common transport / provider failures via
    /// <see cref="OnInteropError"/> instead of letting them propagate as unhandled exceptions
    /// that would tear down the host app. Returns <c>true</c> when the call completed without
    /// a known interop failure, so callers that mirror state in managed snapshots can decide
    /// whether to commit or skip the mutation.
    /// </summary>
    private async ValueTask<bool> SafeInvokeAsync(ValueTask task, string callSite)
    {
        try
        {
            await task;
            return true;
        }
        catch (JSDisconnectedException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, callSite);
        }
        catch (JSException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, callSite);
        }
        catch (ObjectDisposedException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, callSite);
        }
        catch (TaskCanceledException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, callSite);
        }
        return false;
    }

    /// <summary>
    /// The value-returning counterpart of <see cref="SafeInvokeAsync(ValueTask, string)"/>: the
    /// same interop failures reach <see cref="OnInteropError"/>, and the caller is handed null
    /// instead of a value the call never produced.
    /// </summary>
    private async ValueTask<T?> SafeInvokeAsync<T>(ValueTask<T> task, string callSite) where T : struct
    {
        try
        {
            return await task;
        }
        catch (JSDisconnectedException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, callSite);
        }
        catch (JSException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, callSite);
        }
        catch (ObjectDisposedException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, callSite);
        }
        catch (TaskCanceledException ex)
        {
            await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, callSite);
        }
        return null;
    }
}
