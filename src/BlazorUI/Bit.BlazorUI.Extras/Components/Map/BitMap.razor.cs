using System.Collections.Concurrent;
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
    // Process-wide cache of script/stylesheet URLs that have already been requested by any
    // BitMap instance. The browser will dedupe by URL anyway, but skipping the JS interop
    // round-trip when nothing new is needed makes mount/unmount of map-heavy UIs noticeably
    // cheaper. Keyed by URL (case-sensitive - URLs are case-sensitive on most servers).
    private static readonly ConcurrentDictionary<string, byte> _loadedScripts = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, byte> _loadedStylesheets = new(StringComparer.Ordinal);

    /// <summary>
    /// Clears the process-wide script/stylesheet load cache. Intended for unit tests only -
    /// production code should not need to invalidate the cache because the browser already
    /// dedupes the underlying network requests.
    /// </summary>
    public static void ResetAssetLoadCacheForTesting()
    {
        _loadedScripts.Clear();
        _loadedStylesheets.Clear();
    }

    private bool _initialized;
    private string _canvasId = string.Empty;
    private TMapProvider? _activeProvider;
    private ElementReference _mapElement;
    private DotNetObjectReference<BitMap<TMapProvider>>? _dotnetObj;

    // Serialises lifecycle transitions (init from first-render, sync, swap, dispose). Without
    // this guard, a rapid Provider change can interleave a dispose-then-init swap with a sync
    // call, leaving the JS side either double-initialized or unable to find its map id.
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    // Snapshot of imperatively-added state. We replay it after a destructive provider swap when
    // ReplayStateOnProviderSwap is true. Plain dictionaries - all access is serialised by
    // _lifecycleGate or by the calling thread (the Blazor renderer is single-threaded per circuit).
    private readonly Dictionary<string, BitMapMarker> _markerState = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VectorLayerSnapshot> _vectorState = new(StringComparer.Ordinal);
    private readonly Dictionary<string, BitMapTileOverlay> _tileOverlayState = new(StringComparer.Ordinal);

    private abstract record VectorLayerSnapshot(string LayerId, BitMapVectorPathStyle? Style);
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
    /// </summary>
    [Parameter, CallOnSetAsync(nameof(OnProviderSet))]
    public TMapProvider? Provider { get; set; }

    /// <summary>
    /// When true, markers, vector layers, and tile overlays added imperatively are replayed
    /// after a destructive provider swap (i.e. when the new provider has a different
    /// <see cref="IBitMapProvider.JsObjectName"/>). Default is false to preserve the existing
    /// "consumer reapplies on OnReady" behavior.
    /// </summary>
    [Parameter] public bool ReplayStateOnProviderSwap { get; set; }



    /// <summary>
    /// Fired once after the map is ready and imperative methods can be called safely.
    /// Also fires again if the active provider is swapped to one with a different JS backend
    /// (which destructively re-initializes the map); consumers can use this to rebuild any
    /// imperatively-added markers/layers/overlays on the new provider.
    /// </summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <summary>
    /// Fired when the user clicks the map canvas (not on a marker or vector layer).
    /// </summary>
    [Parameter] public EventCallback<BitMapLatLng> OnClick { get; set; }

    /// <summary>
    /// Fired when the user double-clicks the map.
    /// </summary>
    [Parameter] public EventCallback<BitMapLatLng> OnDoubleClick { get; set; }

    /// <summary>
    /// Fired whenever the map view (center/zoom/bounds) changes.
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
    /// Fired when an interop call into the underlying provider fails. Consumers can subscribe to
    /// surface errors to telemetry/log channels - by default the component swallows interop
    /// failures so a misbehaving provider can't tear down the host page.
    /// </summary>
    [Parameter] public EventCallback<BitMapInteropErrorArgs> OnInteropError { get; set; }



    /// <summary>True after the underlying map instance has been created and is ready for interop calls.</summary>
    public bool IsReady => _initialized;



    /// <summary>Recalculate map size after a container resize.</summary>
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
    public async ValueTask SetView(BitMapLatLng center, double? zoom = null, bool animate = true)
    {
        EnsureReady();
        await SafeInvokeAsync(_js.BitMapSetView(JsObject, _Id, center.Latitude, center.Longitude, zoom, animate), nameof(SetView));
    }

    /// <summary>Animated pan/zoom to the given center.</summary>
    public async ValueTask FlyTo(BitMapLatLng center, double? zoom = null)
    {
        EnsureReady();
        await SafeInvokeAsync(_js.BitMapFlyTo(JsObject, _Id, center.Latitude, center.Longitude, zoom), nameof(FlyTo));
    }

    /// <summary>Fit the view to the given bounding box.</summary>
    public async ValueTask FitBounds(BitMapLatLngBounds bounds, int paddingPixels = 48)
    {
        EnsureReady();
        await SafeInvokeAsync(_js.BitMapFitBounds(JsObject, _Id,
            bounds.SouthWest.Latitude, bounds.SouthWest.Longitude,
            bounds.NorthEast.Latitude, bounds.NorthEast.Longitude,
            paddingPixels), nameof(FitBounds));
    }

    /// <summary>Fit the view to include all currently rendered markers.</summary>
    public async ValueTask FitBoundsToMarkers(int paddingPixels = 48)
    {
        EnsureReady();
        await SafeInvokeAsync(_js.BitMapFitBoundsToMarkers(JsObject, _Id, paddingPixels), nameof(FitBoundsToMarkers));
    }

    /// <summary>Add a marker to the map.</summary>
    public async ValueTask AddMarker(BitMapMarker marker)
    {
        EnsureReady();
        ArgumentNullException.ThrowIfNull(marker);

        // Snapshot mutation is deferred until the interop call succeeds so that a failed
        // BitMapAddMarker doesn't leave the replay dictionary referencing a marker the JS
        // side never created (which would re-add a phantom on the next provider swap).
        if (await SafeInvokeAsync(_js.BitMapAddMarker(JsObject, _Id, marker.Id, ToMarkerPayload(marker)), nameof(AddMarker)))
        {
            _markerState[marker.Id] = marker;
        }
    }

    /// <summary>Remove a single marker by its id.</summary>
    public async ValueTask RemoveMarker(string markerId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(markerId);

        if (await SafeInvokeAsync(_js.BitMapRemoveMarker(JsObject, _Id, markerId), nameof(RemoveMarker)))
        {
            _markerState.Remove(markerId);
        }
    }

    /// <summary>Remove all markers from the map.</summary>
    public async ValueTask ClearMarkers()
    {
        EnsureReady();
        if (await SafeInvokeAsync(_js.BitMapClearMarkers(JsObject, _Id), nameof(ClearMarkers)))
        {
            _markerState.Clear();
        }
    }

    /// <summary>Move an existing marker to a new position.</summary>
    public async ValueTask SetMarkerPosition(string markerId, BitMapLatLng position)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(markerId);

        // Keep the snapshot in sync so a later replay places the marker at its current
        // position. We only commit the position update after the JS side accepted it,
        // otherwise a replay would relocate the marker even though the live map didn't.
        if (await SafeInvokeAsync(_js.BitMapSetMarkerPosition(JsObject, _Id, markerId, position.Latitude, position.Longitude), nameof(SetMarkerPosition)))
        {
            if (_markerState.TryGetValue(markerId, out var prev))
            {
                _markerState[markerId] = CloneWithPosition(prev, position);
            }
        }
    }

    /// <summary>Open the popup of the marker with the given id.</summary>
    public async ValueTask OpenMarkerPopup(string markerId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(markerId);
        await SafeInvokeAsync(_js.BitMapOpenMarkerPopup(JsObject, _Id, markerId), nameof(OpenMarkerPopup));
    }

    /// <summary>Replace all markers in a single batch operation.</summary>
    public async ValueTask SyncMarkers(IEnumerable<BitMapMarker> markers)
    {
        EnsureReady();
        ArgumentNullException.ThrowIfNull(markers);

        var list = markers as ICollection<BitMapMarker> ?? [.. markers];
        var payload = new object[list.Count];
        var ids = new string[list.Count];
        var i = 0;
        foreach (var m in list)
        {
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
        }
    }

    /// <summary>Add a polyline vector layer.</summary>
    public async ValueTask AddPolyline(string layerId, IReadOnlyList<BitMapLatLng> path, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);
        ArgumentNullException.ThrowIfNull(path);

        var snapshot = new PolylineSnapshot(layerId, [.. path], style);
        if (await SafeInvokeAsync(_js.BitMapAddPolyline(JsObject, _Id, layerId, ToLatLngArray(path), ToStylePayload(style)), nameof(AddPolyline)))
        {
            _vectorState[layerId] = snapshot;
        }
    }

    /// <summary>Add a polygon vector layer.</summary>
    public async ValueTask AddPolygon(string layerId, IReadOnlyList<BitMapLatLng> ring, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);
        ArgumentNullException.ThrowIfNull(ring);

        var snapshot = new PolygonSnapshot(layerId, [.. ring], style);
        if (await SafeInvokeAsync(_js.BitMapAddPolygon(JsObject, _Id, layerId, ToLatLngArray(ring), ToStylePayload(style)), nameof(AddPolygon)))
        {
            _vectorState[layerId] = snapshot;
        }
    }

    /// <summary>Add a circle vector layer (radius in meters).</summary>
    public async ValueTask AddCircle(string layerId, BitMapLatLng center, double radiusMeters, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);

        if (await SafeInvokeAsync(_js.BitMapAddCircle(JsObject, _Id, layerId, center.Latitude, center.Longitude, radiusMeters, ToStylePayload(style)), nameof(AddCircle)))
        {
            _vectorState[layerId] = new CircleSnapshot(layerId, center, radiusMeters, style);
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
        }
    }

    /// <summary>Add a GeoJSON layer rendered with the given style.</summary>
    public async ValueTask AddGeoJson(string layerId, string geoJson, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);
        ArgumentException.ThrowIfNullOrEmpty(geoJson);

        if (await SafeInvokeAsync(_js.BitMapAddGeoJson(JsObject, _Id, layerId, geoJson, ToStylePayload(style)), nameof(AddGeoJson)))
        {
            _vectorState[layerId] = new GeoJsonSnapshot(layerId, geoJson, style);
        }
    }

    /// <summary>Remove a vector layer by id.</summary>
    public async ValueTask RemoveLayer(string layerId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(layerId);

        if (await SafeInvokeAsync(_js.BitMapRemoveLayer(JsObject, _Id, layerId), nameof(RemoveLayer)))
        {
            _vectorState.Remove(layerId);
        }
    }

    /// <summary>Remove all vector layers.</summary>
    public async ValueTask ClearVectorLayers()
    {
        EnsureReady();
        if (await SafeInvokeAsync(_js.BitMapClearVectorLayers(JsObject, _Id), nameof(ClearVectorLayers)))
        {
            _vectorState.Clear();
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
        }
    }

    /// <summary>Remove a tile overlay by id.</summary>
    public async ValueTask RemoveTileOverlay(string overlayId)
    {
        EnsureReady();
        ArgumentException.ThrowIfNullOrEmpty(overlayId);

        if (await SafeInvokeAsync(_js.BitMapRemoveTileOverlay(JsObject, _Id, overlayId), nameof(RemoveTileOverlay)))
        {
            _tileOverlayState.Remove(overlayId);
        }
    }



    [JSInvokable("OnClick")]
    public Task _OnClick(JsonElement e) => InvokeUserCallback(OnClick, ReadLatLng, e, nameof(OnClick));

    [JSInvokable("OnDoubleClick")]
    public Task _OnDoubleClick(JsonElement e) => InvokeUserCallback(OnDoubleClick, ReadLatLng, e, nameof(OnDoubleClick));

    [JSInvokable("OnViewChanged")]
    public Task _OnViewChanged(JsonElement e) => InvokeUserCallback(OnViewChanged, ParseViewState, e, nameof(OnViewChanged));

    [JSInvokable("OnMarkerClick")]
    public async Task _OnMarkerClick(string markerId)
    {
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



    protected override string RootElementClass => "bit-map";

    protected override void OnInitialized()
    {
        _canvasId = $"{_Id}-canvas";
        base.OnInitialized();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        if (_js.IsRuntimeInvalid()) return;

        await _lifecycleGate.WaitAsync();
        try
        {
            if (IsDisposed) return;

            var initial = Provider ?? new TMapProvider();
            BitMapValidation.ValidateJsObjectName(initial.JsObjectName);

            await LoadAssetsAsync(initial);
            if (IsDisposed) return;

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
                await RaiseInteropError(BitMapInteropErrorSource.Init, ex, "first render");
                return;
            }
            catch (Exception ex)
            {
                // Most common cause: the canvas div was removed from the DOM before JS init ran
                // (parent component re-render or page navigation). Tear down rather than leaking
                // an interop handle.
                CleanupFailedInit();
                await RaiseInteropError(BitMapInteropErrorSource.Init, ex, "first render");
                return;
            }

            _initialized = true;
        }
        finally
        {
            _lifecycleGate.Release();
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



    private async ValueTask OnProviderSet()
    {
        if (_js.IsRuntimeInvalid()) return;

        await _lifecycleGate.WaitAsync();
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

            await LoadAssetsAsync(effective);
            if (IsDisposed) return;

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
            _lifecycleGate.Release();
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

        if (IsDisposed) return;

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

        if (IsDisposed) return;

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
            await RaiseInteropError(BitMapInteropErrorSource.Init, ex, "provider swap");
            return;
        }
        catch (Exception ex)
        {
            CleanupFailedInit();
            await RaiseInteropError(BitMapInteropErrorSource.Init, ex, "provider swap");
            return;
        }

        _activeProvider = effective;
        _initialized = true;

        if (ReplayStateOnProviderSwap)
        {
            await ReplayImperativeStateAsync();
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

    private async ValueTask LoadAssetsAsync(TMapProvider provider)
    {
        // Process-wide dedup so mounting/unmounting multiple BitMaps over the same provider
        // doesn't pay an interop round-trip per mount. The browser dedupes by URL too, but
        // skipping the round-trip avoids serialising the URL list and waiting for a JS
        // promise that does nothing useful.
        var pendingStylesheets = FilterUnloaded(provider.Stylesheets, _loadedStylesheets);
        if (pendingStylesheets.Count > 0)
        {
            try
            {
                await _js.BitExtrasInitStylesheets(pendingStylesheets);
                foreach (var url in pendingStylesheets) _loadedStylesheets[url] = 1;
            }
            catch (Exception ex)
            {
                // A failed CDN stylesheet load shouldn't prevent the map from initializing -
                // the providers degrade gracefully (e.g., OpenLayers will still work, just
                // with unstyled controls if its CSS failed to load).
                await RaiseInteropError(BitMapInteropErrorSource.StylesheetLoad, ex);
            }
        }

        if (IsDisposed) return;

        var pendingScripts = FilterUnloaded(provider.Scripts, _loadedScripts);
        if (pendingScripts.Count > 0)
        {
            try
            {
                await _js.BitExtrasInitScripts(pendingScripts, provider.ScriptsAreModules);
                foreach (var url in pendingScripts) _loadedScripts[url] = 1;
            }
            catch (Exception ex)
            {
                await RaiseInteropError(BitMapInteropErrorSource.ScriptLoad, ex);
                throw; // without scripts the map cannot initialize; let the caller skip init
            }
        }
    }

    private static List<string> FilterUnloaded(IReadOnlyList<string> urls, ConcurrentDictionary<string, byte> cache)
    {
        if (urls.Count == 0) return [];
        var result = new List<string>(urls.Count);
        foreach (var url in urls)
        {
            if (cache.ContainsKey(url) is false) result.Add(url);
        }
        return result;
    }

    private async ValueTask ReplayImperativeStateAsync()
    {
        // Replay everything that was added imperatively, in stable insertion order. We tolerate
        // individual failures per item so a single bad payload doesn't abort the rest of the
        // restore.
        foreach (var (id, marker) in _markerState)
        {
            try { await _js.BitMapAddMarker(JsObject, _Id, id, ToMarkerPayload(marker)); }
            catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, $"replay marker '{id}'"); }
        }

        foreach (var snap in _vectorState.Values)
        {
            try
            {
                switch (snap)
                {
                    case PolylineSnapshot p:
                        await _js.BitMapAddPolyline(JsObject, _Id, p.LayerId, ToLatLngArray(p.Path), ToStylePayload(p.Style));
                        break;
                    case PolygonSnapshot p:
                        await _js.BitMapAddPolygon(JsObject, _Id, p.LayerId, ToLatLngArray(p.Ring), ToStylePayload(p.Style));
                        break;
                    case CircleSnapshot c:
                        await _js.BitMapAddCircle(JsObject, _Id, c.LayerId, c.Center.Latitude, c.Center.Longitude, c.RadiusMeters, ToStylePayload(c.Style));
                        break;
                    case RectangleSnapshot r:
                        await _js.BitMapAddRectangle(JsObject, _Id, r.LayerId,
                            r.Bounds.SouthWest.Latitude, r.Bounds.SouthWest.Longitude,
                            r.Bounds.NorthEast.Latitude, r.Bounds.NorthEast.Longitude,
                            ToStylePayload(r.Style));
                        break;
                    case GeoJsonSnapshot g:
                        await _js.BitMapAddGeoJson(JsObject, _Id, g.LayerId, g.GeoJson, ToStylePayload(g.Style));
                        break;
                }
            }
            catch (Exception ex)
            {
                await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, $"replay layer '{snap.LayerId}'");
            }
        }

        foreach (var overlay in _tileOverlayState.Values)
        {
            try { await _js.BitMapAddTileOverlay(JsObject, _Id, ToTileOverlayPayload(overlay)); }
            catch (Exception ex) { await RaiseInteropError(BitMapInteropErrorSource.Imperative, ex, $"replay tile overlay '{overlay.Id}'"); }
        }
    }



    private string JsObject => _activeProvider!.JsObjectName;

    private static object ToMarkerPayload(BitMapMarker m) => new Dictionary<string, object?>
    {
        ["lat"] = m.Position.Latitude,
        ["lng"] = m.Position.Longitude,
        ["title"] = m.Title,
        ["popupHtml"] = m.PopupHtml?.Value,
        ["popupText"] = m.PopupText,
        ["tooltipHtml"] = m.TooltipHtml?.Value,
        ["tooltipText"] = m.TooltipText,
        ["tooltipPermanent"] = m.TooltipPermanent,
        ["tooltipDirection"] = m.TooltipDirection.ToString().ToLowerInvariant(),
        ["draggable"] = m.Draggable,
        ["iconUrl"] = m.IconUrl,
        ["iconWidth"] = m.IconWidth,
        ["iconHeight"] = m.IconHeight,
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
    };

    private static BitMapMarker CloneWithPosition(BitMapMarker prev, BitMapLatLng position) => new()
    {
        Id = prev.Id,
        Position = position,
        PopupHtml = prev.PopupHtml,
        PopupText = prev.PopupText,
        TooltipHtml = prev.TooltipHtml,
        TooltipText = prev.TooltipText,
        TooltipPermanent = prev.TooltipPermanent,
        TooltipDirection = prev.TooltipDirection,
        Title = prev.Title,
        Draggable = prev.Draggable,
        IconUrl = prev.IconUrl,
        IconWidth = prev.IconWidth,
        IconHeight = prev.IconHeight,
        ZIndexOffset = prev.ZIndexOffset,
    };

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

        // Acquire the lifecycle gate so we don't race with an in-flight provider swap.
        try { await _lifecycleGate.WaitAsync(); }
        catch (ObjectDisposedException) { /* already gone */ }

        try
        {
            _dotnetObj?.Dispose();
            _dotnetObj = null;

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
}
