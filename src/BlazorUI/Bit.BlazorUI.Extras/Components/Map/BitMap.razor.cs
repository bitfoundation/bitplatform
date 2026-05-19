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
    private bool _initialized;
    private TMapProvider? _activeProvider;
    private ElementReference _mapElement;
    private DotNetObjectReference<BitMap<TMapProvider>>? _dotnetObj;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Optional content rendered above the map canvas (overlays, custom controls, etc.).
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The provider configuration (basemap, tokens, options). When null, a default
    /// <typeparamref name="TMapProvider"/> instance is created.
    /// </summary>
    [Parameter, CallOnSetAsync(nameof(OnProviderSet))]
    public TMapProvider? Provider { get; set; }



    /// <summary>
    /// Fired once after the map is ready and imperative methods can be called safely.
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



    /// <summary>True after the underlying map instance has been created and is ready for interop calls.</summary>
    public bool IsReady => _initialized;



    /// <summary>Recalculate map size after a container resize.</summary>
    public ValueTask InvalidateSize()
        => _initialized ? _js.BitMapInvalidateSize(JsObject, _Id) : ValueTask.CompletedTask;

    /// <summary>Returns a snapshot of the current viewport.</summary>
    public async ValueTask<BitMapViewState> GetView()
    {
        EnsureReady();
        var el = await _js.BitMapGetView(JsObject, _Id);
        return ParseViewState(el);
    }

    /// <summary>Pan and (optionally) zoom the map to the given center.</summary>
    public ValueTask SetView(BitMapLatLng center, double? zoom = null, bool animate = true)
    {
        EnsureReady();
        return _js.BitMapSetView(JsObject, _Id, center.Latitude, center.Longitude, zoom, animate);
    }

    /// <summary>Animated pan/zoom to the given center.</summary>
    public ValueTask FlyTo(BitMapLatLng center, double? zoom = null)
    {
        EnsureReady();
        return _js.BitMapFlyTo(JsObject, _Id, center.Latitude, center.Longitude, zoom);
    }

    /// <summary>Fit the view to the given bounding box.</summary>
    public ValueTask FitBounds(BitMapLatLngBounds bounds, int paddingPixels = 48)
    {
        EnsureReady();
        return _js.BitMapFitBounds(JsObject, _Id,
            bounds.SouthWest.Latitude, bounds.SouthWest.Longitude,
            bounds.NorthEast.Latitude, bounds.NorthEast.Longitude,
            paddingPixels);
    }

    /// <summary>Fit the view to include all currently rendered markers.</summary>
    public ValueTask FitBoundsToMarkers(int paddingPixels = 48)
    {
        EnsureReady();
        return _js.BitMapFitBoundsToMarkers(JsObject, _Id, paddingPixels);
    }

    /// <summary>Add a marker to the map.</summary>
    public ValueTask AddMarker(BitMapMarker marker)
    {
        EnsureReady();
        return _js.BitMapAddMarker(JsObject, _Id, marker.Id, ToMarkerPayload(marker));
    }

    /// <summary>Remove a single marker by its id.</summary>
    public ValueTask RemoveMarker(string markerId)
        => _initialized ? _js.BitMapRemoveMarker(JsObject, _Id, markerId) : ValueTask.CompletedTask;

    /// <summary>Remove all markers from the map.</summary>
    public ValueTask ClearMarkers()
        => _initialized ? _js.BitMapClearMarkers(JsObject, _Id) : ValueTask.CompletedTask;

    /// <summary>Move an existing marker to a new position.</summary>
    public ValueTask SetMarkerPosition(string markerId, BitMapLatLng position)
        => _initialized
            ? _js.BitMapSetMarkerPosition(JsObject, _Id, markerId, position.Latitude, position.Longitude)
            : ValueTask.CompletedTask;

    /// <summary>Open the popup of the marker with the given id.</summary>
    public ValueTask OpenMarkerPopup(string markerId)
        => _initialized ? _js.BitMapOpenMarkerPopup(JsObject, _Id, markerId) : ValueTask.CompletedTask;

    /// <summary>Replace all markers in a single batch operation.</summary>
    public ValueTask SyncMarkers(IEnumerable<BitMapMarker> markers)
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
        return _js.BitMapSyncMarkers(JsObject, _Id, ids, payload);
    }

    /// <summary>Add a polyline vector layer.</summary>
    public ValueTask AddPolyline(string layerId, IReadOnlyList<BitMapLatLng> path, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        return _js.BitMapAddPolyline(JsObject, _Id, layerId, ToLatLngArray(path), ToStylePayload(style));
    }

    /// <summary>Add a polygon vector layer.</summary>
    public ValueTask AddPolygon(string layerId, IReadOnlyList<BitMapLatLng> ring, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        return _js.BitMapAddPolygon(JsObject, _Id, layerId, ToLatLngArray(ring), ToStylePayload(style));
    }

    /// <summary>Add a circle vector layer (radius in meters).</summary>
    public ValueTask AddCircle(string layerId, BitMapLatLng center, double radiusMeters, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        return _js.BitMapAddCircle(JsObject, _Id, layerId, center.Latitude, center.Longitude, radiusMeters, ToStylePayload(style));
    }

    /// <summary>Add a rectangle vector layer.</summary>
    public ValueTask AddRectangle(string layerId, BitMapLatLngBounds bounds, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        return _js.BitMapAddRectangle(JsObject, _Id, layerId,
            bounds.SouthWest.Latitude, bounds.SouthWest.Longitude,
            bounds.NorthEast.Latitude, bounds.NorthEast.Longitude,
            ToStylePayload(style));
    }

    /// <summary>Add a GeoJSON layer rendered with the given style.</summary>
    public ValueTask AddGeoJson(string layerId, string geoJson, BitMapVectorPathStyle? style = null)
    {
        EnsureReady();
        return _js.BitMapAddGeoJson(JsObject, _Id, layerId, geoJson, ToStylePayload(style));
    }

    /// <summary>Remove a vector layer by id.</summary>
    public ValueTask RemoveLayer(string layerId)
        => _initialized ? _js.BitMapRemoveLayer(JsObject, _Id, layerId) : ValueTask.CompletedTask;

    /// <summary>Remove all vector layers.</summary>
    public ValueTask ClearVectorLayers()
        => _initialized ? _js.BitMapClearVectorLayers(JsObject, _Id) : ValueTask.CompletedTask;

    /// <summary>Add a tile overlay (raster XYZ layer) above the base map.</summary>
    public ValueTask AddTileOverlay(BitMapTileOverlay overlay)
    {
        EnsureReady();
        return _js.BitMapAddTileOverlay(JsObject, _Id, new
        {
            id = overlay.Id,
            urlTemplate = overlay.UrlTemplate,
            attribution = overlay.Attribution,
            opacity = overlay.Opacity,
            zIndex = overlay.ZIndex,
            maxZoom = overlay.MaxZoom,
        });
    }

    /// <summary>Remove a tile overlay by id.</summary>
    public ValueTask RemoveTileOverlay(string overlayId)
        => _initialized ? _js.BitMapRemoveTileOverlay(JsObject, _Id, overlayId) : ValueTask.CompletedTask;



    [JSInvokable("OnClick")]
    public Task _OnClick(JsonElement e)
        => OnClick.HasDelegate ? OnClick.InvokeAsync(ReadLatLng(e)) : Task.CompletedTask;

    [JSInvokable("OnDoubleClick")]
    public Task _OnDoubleClick(JsonElement e)
        => OnDoubleClick.HasDelegate ? OnDoubleClick.InvokeAsync(ReadLatLng(e)) : Task.CompletedTask;

    [JSInvokable("OnViewChanged")]
    public Task _OnViewChanged(JsonElement e)
        => OnViewChanged.HasDelegate ? OnViewChanged.InvokeAsync(ParseViewState(e)) : Task.CompletedTask;

    [JSInvokable("OnMarkerClick")]
    public Task _OnMarkerClick(string markerId)
        => OnMarkerClick.HasDelegate ? OnMarkerClick.InvokeAsync(markerId) : Task.CompletedTask;

    [JSInvokable("OnMarkerDragEnd")]
    public Task _OnMarkerDragEnd(string markerId, JsonElement position)
    {
        if (OnMarkerDragEnd.HasDelegate is false) return Task.CompletedTask;
        return OnMarkerDragEnd.InvokeAsync(new BitMapMarkerDragEndArgs { Id = markerId, Position = ReadLatLng(position) });
    }

    [JSInvokable("OnVectorClick")]
    public Task _OnVectorClick(string layerId, string kind, JsonElement position)
    {
        if (OnVectorClick.HasDelegate is false) return Task.CompletedTask;
        return OnVectorClick.InvokeAsync(new BitMapVectorClickArgs { LayerId = layerId, Kind = kind, Position = ReadLatLng(position) });
    }

    [JSInvokable("OnGeoJsonFeatureClick")]
    public Task _OnGeoJsonFeatureClick(string layerId, JsonElement properties)
    {
        if (OnGeoJsonFeatureClick.HasDelegate is false) return Task.CompletedTask;
        return OnGeoJsonFeatureClick.InvokeAsync(new BitMapGeoJsonFeatureClickArgs { LayerId = layerId, Properties = properties });
    }



    protected override string RootElementClass => "bit-map";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        _activeProvider = Provider ?? new TMapProvider();

        if (_activeProvider.Stylesheets.Count > 0)
        {
            await _js.BitExtrasInitStylesheets(_activeProvider.Stylesheets);
        }

        if (_activeProvider.Scripts.Count > 0)
        {
            await _js.BitExtrasInitScripts(_activeProvider.Scripts, _activeProvider.ScriptsAreModules);
        }

        _dotnetObj = DotNetObjectReference.Create(this);

        await _js.BitMapInit(_activeProvider.JsObjectName, _Id, _mapElement, _dotnetObj, _activeProvider.BuildOptionsPayload());

        _initialized = true;

        await OnReady.InvokeAsync();
    }



    private async ValueTask OnProviderSet()
    {
        if (_initialized is false || _activeProvider is null) return;

        // When Provider is reset to null, revert to a default-constructed instance.
        var effective = Provider ?? new TMapProvider();
        await _js.BitMapSync(JsObject, _Id, effective.BuildOptionsPayload());
        _activeProvider = effective;
    }



    private string JsObject => _activeProvider!.JsObjectName;

    private static object ToMarkerPayload(BitMapMarker m) => new
    {
        lat = m.Position.Latitude,
        lng = m.Position.Longitude,
        title = m.Title,
        popupHtml = m.PopupHtml,
        popupText = m.PopupText,
        tooltipHtml = m.TooltipHtml,
        tooltipText = m.TooltipText,
        tooltipPermanent = m.TooltipPermanent,
        tooltipDirection = m.TooltipDirection,
        draggable = m.Draggable,
        iconUrl = m.IconUrl,
        iconWidth = m.IconWidth,
        iconHeight = m.IconHeight,
        zIndexOffset = m.ZIndexOffset,
    };

    private static object[] ToLatLngArray(IReadOnlyList<BitMapLatLng> pts)
    {
        var arr = new object[pts.Count];
        for (var i = 0; i < pts.Count; i++)
        {
            arr[i] = new { lat = pts[i].Latitude, lng = pts[i].Longitude };
        }
        return arr;
    }

    private static object? ToStylePayload(BitMapVectorPathStyle? s) => s is null
        ? null
        : new
        {
            color = s.Color,
            weight = s.Weight,
            opacity = s.Opacity,
            fillColor = s.FillColor,
            fillOpacity = s.FillOpacity,
            dashArray = s.DashArray,
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



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _dotnetObj?.Dispose();

        try
        {
            if (_initialized && _activeProvider is not null)
            {
                await _js.BitMapDispose(_activeProvider.JsObjectName, _Id);
            }
        }
        catch (JSDisconnectedException) { }

        await base.DisposeAsync(disposing);
    }
}
