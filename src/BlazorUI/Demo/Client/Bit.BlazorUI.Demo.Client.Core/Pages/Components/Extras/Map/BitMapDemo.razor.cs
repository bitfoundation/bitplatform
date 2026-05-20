namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Map;

public partial class BitMapDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new() { Name = "TMapProvider", Type = "Type (generic)", DefaultValue = "", Description = "The map provider type. One of: BitLeafletMapProvider, BitMapLibreMapProvider, BitMapboxMapProvider, BitOpenLayersMapProvider, BitArcGisMapProvider, BitAzureMapsMapProvider, BitCesiumMapProvider." },
        new() { Name = "Provider", Type = "TMapProvider?", DefaultValue = "null", Description = "Provider configuration instance (center, zoom, tokens, etc.). When null a default instance is created." },
        new() { Name = "ChildContent", Type = "RenderFragment?", DefaultValue = "null", Description = "Optional content rendered above the map canvas." },
        new() { Name = "OnReady", Type = "EventCallback", DefaultValue = "", Description = "Fires once after the map is ready for imperative calls." },
        new() { Name = "OnClick", Type = "EventCallback<BitMapLatLng>", DefaultValue = "", Description = "Fires when the user clicks the map canvas." },
        new() { Name = "OnDoubleClick", Type = "EventCallback<BitMapLatLng>", DefaultValue = "", Description = "Fires when the user double-clicks the map." },
        new() { Name = "OnViewChanged", Type = "EventCallback<BitMapViewState>", DefaultValue = "", Description = "Fires whenever the map view changes." },
        new() { Name = "OnMarkerClick", Type = "EventCallback<string>", DefaultValue = "", Description = "Fires when the user clicks a marker (argument is the marker id)." },
        new() { Name = "OnMarkerDragEnd", Type = "EventCallback<BitMapMarkerDragEndArgs>", DefaultValue = "", Description = "Fires when a draggable marker is dropped." },
        new() { Name = "OnVectorClick", Type = "EventCallback<BitMapVectorClickArgs>", DefaultValue = "", Description = "Fires when the user clicks a vector layer." },
        new() { Name = "OnGeoJsonFeatureClick", Type = "EventCallback<BitMapGeoJsonFeatureClickArgs>", DefaultValue = "", Description = "Fires when the user clicks a GeoJSON feature." },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
        new() { Name = "IsReady", Type = "bool", DefaultValue = "false", Description = "True after the map is ready for interop calls." },
        new() { Name = "GetView", Type = "Func<ValueTask<BitMapViewState>>", DefaultValue = "", Description = "Returns a snapshot of the current viewport." },
        new() { Name = "SetView", Type = "Func<BitMapLatLng, double?, bool, ValueTask>", DefaultValue = "", Description = "Pan and optionally zoom to the given center." },
        new() { Name = "FlyTo", Type = "Func<BitMapLatLng, double?, ValueTask>", DefaultValue = "", Description = "Animated pan/zoom to the given center." },
        new() { Name = "FitBounds", Type = "Func<BitMapLatLngBounds, int, ValueTask>", DefaultValue = "", Description = "Fit the view to the given bounding box." },
        new() { Name = "FitBoundsToMarkers", Type = "Func<int, ValueTask>", DefaultValue = "", Description = "Fit the view to include all current markers." },
        new() { Name = "InvalidateSize", Type = "Func<ValueTask>", DefaultValue = "", Description = "Recalculate map size after a container resize." },
        new() { Name = "AddMarker", Type = "Func<BitMapMarker, ValueTask>", DefaultValue = "", Description = "Add a marker to the map." },
        new() { Name = "RemoveMarker", Type = "Func<string, ValueTask>", DefaultValue = "", Description = "Remove a marker by id." },
        new() { Name = "ClearMarkers", Type = "Func<ValueTask>", DefaultValue = "", Description = "Remove all markers." },
        new() { Name = "SetMarkerPosition", Type = "Func<string, BitMapLatLng, ValueTask>", DefaultValue = "", Description = "Move a marker to a new position." },
        new() { Name = "OpenMarkerPopup", Type = "Func<string, ValueTask>", DefaultValue = "", Description = "Open a marker's popup." },
        new() { Name = "SyncMarkers", Type = "Func<IEnumerable<BitMapMarker>, ValueTask>", DefaultValue = "", Description = "Replace all markers in one batch." },
        new() { Name = "AddPolyline", Type = "Func<string, IReadOnlyList<BitMapLatLng>, BitMapVectorPathStyle?, ValueTask>", DefaultValue = "", Description = "Add a polyline." },
        new() { Name = "AddPolygon", Type = "Func<string, IReadOnlyList<BitMapLatLng>, BitMapVectorPathStyle?, ValueTask>", DefaultValue = "", Description = "Add a polygon." },
        new() { Name = "AddCircle", Type = "Func<string, BitMapLatLng, double, BitMapVectorPathStyle?, ValueTask>", DefaultValue = "", Description = "Add a circle (radius in meters)." },
        new() { Name = "AddRectangle", Type = "Func<string, BitMapLatLngBounds, BitMapVectorPathStyle?, ValueTask>", DefaultValue = "", Description = "Add a rectangle." },
        new() { Name = "AddGeoJson", Type = "Func<string, string, BitMapVectorPathStyle?, ValueTask>", DefaultValue = "", Description = "Add a GeoJSON layer." },
        new() { Name = "RemoveLayer", Type = "Func<string, ValueTask>", DefaultValue = "", Description = "Remove a vector layer by id." },
        new() { Name = "ClearVectorLayers", Type = "Func<ValueTask>", DefaultValue = "", Description = "Remove all vector layers." },
        new() { Name = "AddTileOverlay", Type = "Func<BitMapTileOverlay, ValueTask>", DefaultValue = "", Description = "Add a tile overlay above the base map." },
        new() { Name = "RemoveTileOverlay", Type = "Func<string, ValueTask>", DefaultValue = "", Description = "Remove a tile overlay by id." },
    ];


    // ── Provider instances ────────────────────────────────────────────────────

    private readonly BitMapLibreMapProvider maplibreProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
    private readonly BitOpenLayersMapProvider olProvider = new() { Center = new(35.6762, 139.6503), Zoom = 4 };
    private readonly BitArcGisMapProvider arcGisProvider = new() { Center = new(40, 0), Zoom = 2, BasemapId = "osm" };
    private readonly BitCesiumMapProvider cesiumProvider = new() { Center = new(20, 0), Zoom = 2, SceneMode = "scene3d" };

    // ── Example 2 – Markers ───────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> markersMapRef = default!;
    private readonly BitLeafletMapProvider markersProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
    private string markersLog = "Seed markers are added on OnReady. Try the buttons.";
    private int _markerCounter;

    private async Task OnMarkersReady()
    {
        await markersMapRef.AddMarker(new BitMapMarker
        {
            Id = "paris", Position = new(48.8566, 2.3522),
            Title = "Paris", PopupHtml = "<b>Paris</b><br/>Click to open popup.",
        });
        await markersMapRef.AddMarker(new BitMapMarker
        {
            Id = "london", Position = new(51.5074, -0.1278),
            Title = "London", PopupHtml = "<b>London</b><br/>Draggable marker.",
            Draggable = true,
            TooltipHtml = "Drag me!",
        });
        await markersMapRef.FitBoundsToMarkers();
    }

    private async Task AddRandomMarker()
    {
        _markerCounter++;
        var id = $"m{_markerCounter}";
        var lat = 48.85 + Random.Shared.NextDouble() * 0.12;
        var lng = 2.28 + Random.Shared.NextDouble() * 0.20;
        await markersMapRef.AddMarker(new BitMapMarker
        {
            Id = id, Position = new(lat, lng),
            PopupHtml = $"<span>Marker <code>{id}</code></span>",
        });
        markersLog = $"Added {id} at {lat:F4}, {lng:F4}";
    }

    private async Task ClearMarkers()
    {
        await markersMapRef.ClearMarkers();
        markersLog = "All markers cleared.";
    }

    private async Task OpenLondonPopup()
    {
        await markersMapRef.OpenMarkerPopup("london");
        markersLog = "Opened London popup.";
    }

    private async Task FitToMarkers()
    {
        await markersMapRef.FitBoundsToMarkers();
        markersLog = "Fitted view to all markers.";
    }

    private Task OnMarkerClick(string id) { markersLog = $"Marker click: {id}"; return Task.CompletedTask; }
    private Task OnMarkerDragEnd(BitMapMarkerDragEndArgs e) { markersLog = $"Drag end {e.Id} → {e.Position.Latitude:F5}, {e.Position.Longitude:F5}"; return Task.CompletedTask; }

    // ── Example 3 – Vectors ───────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> vectorsMapRef = default!;
    private readonly BitLeafletMapProvider vectorsProvider = new() { Center = new(37.7749, -122.4194), Zoom = 12 };
    private string vectorsLog = "Click Redraw to draw shapes, then click a shape.";

    private async Task OnVectorsReady() => await DrawVectors();

    private async Task RedrawVectors()
    {
        await vectorsMapRef.ClearVectorLayers();
        await DrawVectors();
        vectorsLog = "Vectors redrawn.";
    }

    private async Task DrawVectors()
    {
        await vectorsMapRef.AddPolyline("route",
        [
            new(37.80, -122.42), new(37.79, -122.41),
            new(37.78, -122.40), new(37.77, -122.395),
        ], new BitMapVectorPathStyle { Color = "#f85149", Weight = 5, Opacity = 0.9 });

        await vectorsMapRef.AddPolygon("park",
        [
            new(37.769, -122.486), new(37.771, -122.475),
            new(37.765, -122.472), new(37.762, -122.482),
        ], new BitMapVectorPathStyle { Color = "#3fb950", FillOpacity = 0.35, Weight = 2 });

        await vectorsMapRef.AddCircle("radius", new(37.7849, -122.4094), 900,
            new BitMapVectorPathStyle { Color = "#58a6ff", FillOpacity = 0.15, Weight = 2 });

        await vectorsMapRef.AddRectangle("box",
            new BitMapLatLngBounds(new(37.748, -122.44), new(37.756, -122.42)),
            new BitMapVectorPathStyle { Color = "#d29922", FillOpacity = 0.12, Weight = 2, DashArray = "6,4" });

        await vectorsMapRef.FitBounds(
            new BitMapLatLngBounds(new(37.755, -122.49), new(37.805, -122.38)));
    }

    private async Task ClearVectors()
    {
        await vectorsMapRef.ClearVectorLayers();
        vectorsLog = "All vector layers cleared.";
    }

    private Task OnVectorClick(BitMapVectorClickArgs e)
    {
        vectorsLog = $"{e.Kind} \"{e.LayerId}\" @ {e.Position.Latitude:F5}, {e.Position.Longitude:F5}";
        return Task.CompletedTask;
    }

    // ── Example 4 – GeoJSON ───────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> geoJsonMapRef = default!;
    private readonly BitLeafletMapProvider geoJsonProvider = new() { Center = new(40.7128, -74.0060), Zoom = 11 };
    private string geoJsonLog = "Click 'Load GeoJSON', then click a feature.";

    private async Task LoadGeoJson()
    {
        await geoJsonMapRef.RemoveLayer("demo");
        await geoJsonMapRef.AddGeoJson("demo", SampleGeoJson,
            new BitMapVectorPathStyle { Color = "#a371f7", Weight = 3, FillOpacity = 0.25 });
        await geoJsonMapRef.FitBounds(new BitMapLatLngBounds(new(40.71, -74.03), new(40.83, -73.96)));
        geoJsonLog = "GeoJSON loaded. Click a feature.";
    }

    private async Task RemoveGeoJson()
    {
        await geoJsonMapRef.RemoveLayer("demo");
        geoJsonLog = "Layer \"demo\" removed.";
    }

    private Task OnGeoJsonFeatureClick(BitMapGeoJsonFeatureClickArgs e)
    {
        var name = "(no name)";
        if (e.Properties.ValueKind == System.Text.Json.JsonValueKind.Object
            && e.Properties.TryGetProperty("name", out var n))
        {
            name = n.ValueKind == System.Text.Json.JsonValueKind.String ? n.GetString() : n.ToString();
        }
        geoJsonLog = $"Layer {e.LayerId} — properties.name = {name}";
        return Task.CompletedTask;
    }

    // Minimal GeoJSON FeatureCollection for the demo
    private const string SampleGeoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "name": "Central Park" },
              "geometry": {
                "type": "Polygon",
                "coordinates": [[
                  [-73.981, 40.768], [-73.958, 40.768],
                  [-73.958, 40.800], [-73.981, 40.800],
                  [-73.981, 40.768]
                ]]
              }
            },
            {
              "type": "Feature",
              "properties": { "name": "Brooklyn Bridge" },
              "geometry": {
                "type": "LineString",
                "coordinates": [[-73.9969, 40.7061], [-73.9875, 40.7026]]
              }
            }
          ]
        }
        """;

    // ── Example 5 – Custom tiles ──────────────────────────────────────────────

    private string tileProvider = "osm";

    private BitLeafletMapProvider currentTileLeafletProvider = new()
    {
        Center = new(51.505, -0.09), Zoom = 13,
    };

    private void SetTileProvider(string p)
    {
        tileProvider = p;
        currentTileLeafletProvider = p switch
        {
            "carto" => new BitLeafletMapProvider
            {
                Center = new(20, 0), Zoom = 2,
                TileUrl = "https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png",
                TileAttribution = "&copy; OpenStreetMap contributors &copy; <a href=\"https://carto.com/attributions\">CARTO</a>",
            },
            "topo" => new BitLeafletMapProvider
            {
                Center = new(46.5, 11.3), Zoom = 10,
                TileUrl = "https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png",
                TileAttribution = "Map data: &copy; OpenStreetMap contributors, SRTM | Map style: &copy; OpenTopoMap",
                TileMaxZoom = 17,
            },
            _ => new BitLeafletMapProvider { Center = new(51.505, -0.09), Zoom = 13 },
        };
    }

    // ── Example 6 – Events ────────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> eventsMapRef = default!;
    private readonly BitLeafletMapProvider eventsProvider = new() { Center = new(35.6762, 139.6503), Zoom = 11 };
    private string eventsLog = "Pan/zoom or click the map.";

    private Task OnMapClick(BitMapLatLng p) { eventsLog = $"Click → {p.Latitude:F5}, {p.Longitude:F5}"; return Task.CompletedTask; }
    private Task OnMapDoubleClick(BitMapLatLng p) { eventsLog = $"Double-click → {p.Latitude:F5}, {p.Longitude:F5}"; return Task.CompletedTask; }
    private Task OnViewChanged(BitMapViewState v)
    {
        eventsLog = $"View: zoom {v.Zoom:F1}, center {v.Center.Latitude:F4},{v.Center.Longitude:F4}";
        return Task.CompletedTask;
    }

    private async Task FlyToTokyo()
    {
        await eventsMapRef.FlyTo(new(35.6762, 139.6503), 12);
        eventsLog = "Flying to Tokyo…";
    }

    private async Task ReadView()
    {
        var v = await eventsMapRef.GetView();
        eventsLog = $"GetView → zoom {v.Zoom:F2}, center {v.Center.Latitude:F4},{v.Center.Longitude:F4}, " +
                    $"NE {v.Bounds.NorthEast.Latitude:F4},{v.Bounds.NorthEast.Longitude:F4}";
    }

    // ── Example 7 – Advanced ──────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> advMapRef = default!;
    private bool advScrollWheel = true;
    private bool advDragging = true;
    private bool advScaleBar = true;
    private bool advMaxBounds;
    private bool advOverlayOn;
    private string advLog = "Toggle options or use the buttons.";

    private BitLeafletMapProvider advProvider = new()
    {
        Center = new(51.5074, -0.1278), Zoom = 11,
        ScrollWheelZoom = true,
        Dragging = true,
        ShowScaleControl = true,
        MaxBounds = null,
    };

    private BitLeafletMapProvider BuildAdvancedProvider()
    {
        advProvider = new()
        {
            Center = new(51.5074, -0.1278), Zoom = 11,
            ScrollWheelZoom = advScrollWheel,
            Dragging = advDragging,
            ShowScaleControl = advScaleBar,
            MaxBounds = advMaxBounds
                ? new BitMapLatLngBounds(new(51.25, -0.55), new(51.75, 0.35))
                : null,
        };
        return advProvider;
    }

    private async Task OnAdvancedReady()
    {
        await AddTooltipMarkers();
    }

    private async Task AddTooltipMarkers()
    {
        await advMapRef.ClearMarkers();
        await advMapRef.AddMarker(new BitMapMarker { Id = "a", Position = new(51.52, -0.10), TooltipHtml = "<b>West End</b>", PopupHtml = "Popup A", ZIndexOffset = 10 });
        await advMapRef.AddMarker(new BitMapMarker { Id = "b", Position = new(51.50, -0.08), TooltipHtml = "City", PopupHtml = "Popup B" });
        await advMapRef.AddMarker(new BitMapMarker { Id = "c", Position = new(51.48, -0.06), TooltipHtml = "South Bank", PopupHtml = "Popup C" });
        await advMapRef.FitBoundsToMarkers(56);
        advLog = "Three tooltip markers added; view fitted.";
    }

    private async Task ToggleTileOverlay()
    {
        if (advOverlayOn)
        {
            await advMapRef.RemoveTileOverlay("labels");
            advOverlayOn = false;
            advLog = "Tile overlay removed.";
        }
        else
        {
            await advMapRef.AddTileOverlay(new BitMapTileOverlay
            {
                Id = "labels",
                UrlTemplate = "https://tiles.stadiamaps.com/tiles/stamen_toner_labels/{z}/{x}/{y}{r}.png",
                Attribution = "Map tiles by Stamen Design, hosted by Stadia Maps. Data by OpenStreetMap.",
                Opacity = 0.85,
                ZIndex = 400,
                MaxZoom = 20,
            });
            advOverlayOn = true;
            advLog = "Tile overlay added (may fail if the tile host blocks your origin).";
        }
    }

    private async Task ReadAdvancedView()
    {
        var v = await advMapRef.GetView();
        advLog = $"GetView → zoom {v.Zoom:F2}, center {v.Center.Latitude:F4},{v.Center.Longitude:F4}, " +
                 $"NE {v.Bounds.NorthEast.Latitude:F4},{v.Bounds.NorthEast.Longitude:F4}";
    }

    private Task OnAdvancedDoubleClick(BitMapLatLng p) { advLog = $"Double-click at {p.Latitude:F4}, {p.Longitude:F4}"; return Task.CompletedTask; }

    // ── Code strings ──────────────────────────────────────────────────────────

    private readonly string example1RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider"" />
</div>";

    private readonly string example2RazorCode = @"
<div style=""height:380px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""markersMapRef""
            Provider=""@markersProvider""
            OnReady=""OnMarkersReady""
            OnMarkerClick=""OnMarkerClick""
            OnMarkerDragEnd=""OnMarkerDragEnd"" />
</div>
<BitButton OnClick=""AddRandomMarker"">Add random marker</BitButton>
<BitButton OnClick=""ClearMarkers"">Clear all</BitButton>
<BitButton OnClick=""OpenLondonPopup"">Open London popup</BitButton>
<BitButton OnClick=""FitToMarkers"">Fit to markers</BitButton>";
    private readonly string example2CsharpCode = @"
private BitMap<BitLeafletMapProvider> markersMapRef = default!;
private readonly BitLeafletMapProvider markersProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };

private async Task OnMarkersReady()
{
    await markersMapRef.AddMarker(new BitMapMarker
    {
        Id = ""paris"", Position = new(48.8566, 2.3522),
        Title = ""Paris"", PopupHtml = ""<b>Paris</b>"",
    });
    await markersMapRef.AddMarker(new BitMapMarker
    {
        Id = ""london"", Position = new(51.5074, -0.1278),
        Title = ""London"", Draggable = true, TooltipHtml = ""Drag me!"",
    });
    await markersMapRef.FitBoundsToMarkers();
}

private Task OnMarkerClick(string id) { /* ... */ return Task.CompletedTask; }
private Task OnMarkerDragEnd(BitMapMarkerDragEndArgs e) { /* ... */ return Task.CompletedTask; }";

    private readonly string example3RazorCode = @"
<div style=""height:380px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""vectorsMapRef""
            Provider=""@vectorsProvider""
            OnReady=""OnVectorsReady""
            OnVectorClick=""OnVectorClick"" />
</div>";
    private readonly string example3CsharpCode = @"
private async Task OnVectorsReady()
{
    await vectorsMapRef.AddPolyline(""route"",
        [new(37.80, -122.42), new(37.79, -122.41), new(37.78, -122.40)],
        new BitMapVectorPathStyle { Color = ""#f85149"", Weight = 5 });

    await vectorsMapRef.AddPolygon(""park"",
        [new(37.769, -122.486), new(37.771, -122.475), new(37.765, -122.472), new(37.762, -122.482)],
        new BitMapVectorPathStyle { Color = ""#3fb950"", FillOpacity = 0.35 });

    await vectorsMapRef.AddCircle(""radius"", new(37.7849, -122.4094), 900,
        new BitMapVectorPathStyle { Color = ""#58a6ff"", FillOpacity = 0.15 });

    await vectorsMapRef.AddRectangle(""box"",
        new BitMapLatLngBounds(new(37.748, -122.44), new(37.756, -122.42)),
        new BitMapVectorPathStyle { Color = ""#d29922"", DashArray = ""6,4"" });

    await vectorsMapRef.FitBounds(
        new BitMapLatLngBounds(new(37.755, -122.49), new(37.805, -122.38)));
}

private Task OnVectorClick(BitMapVectorClickArgs e)
{
    // e.Kind = ""polyline"" | ""polygon"" | ""circle"" | ""rectangle""
    // e.LayerId = the id you passed to AddPolyline/AddPolygon/…
    return Task.CompletedTask;
}";

    private readonly string example4RazorCode = @"
<div style=""height:380px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""geoJsonMapRef""
            Provider=""@geoJsonProvider""
            OnGeoJsonFeatureClick=""OnGeoJsonFeatureClick"" />
</div>
<BitButton OnClick=""LoadGeoJson"">Load GeoJSON</BitButton>
<BitButton OnClick=""RemoveGeoJson"">Remove layer</BitButton>";
    private readonly string example4CsharpCode = @"
private async Task LoadGeoJson()
{
    await geoJsonMapRef.AddGeoJson(""demo"", geoJsonString,
        new BitMapVectorPathStyle { Color = ""#a371f7"", Weight = 3, FillOpacity = 0.25 });
}

private Task OnGeoJsonFeatureClick(BitMapGeoJsonFeatureClickArgs e)
{
    // e.LayerId = ""demo""
    // e.Properties = JsonElement of feature.properties
    return Task.CompletedTask;
}";

    private readonly string example5RazorCode = @"
@* @key forces a new map instance when the provider changes *@
<BitMap TMapProvider=""BitLeafletMapProvider"" @key=""tileProvider"" Provider=""@currentTileLeafletProvider"" />";
    private readonly string example5CsharpCode = @"
private string tileProvider = ""osm"";
private BitLeafletMapProvider currentTileLeafletProvider = new() { Center = new(51.505, -0.09), Zoom = 13 };

private void SetTileProvider(string p)
{
    tileProvider = p;
    currentTileLeafletProvider = p switch
    {
        ""carto"" => new BitLeafletMapProvider
        {
            TileUrl = ""https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png"",
            TileAttribution = ""&copy; OSM &copy; CARTO"",
        },
        ""topo"" => new BitLeafletMapProvider
        {
            TileUrl = ""https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png"",
            TileMaxZoom = 17,
        },
        _ => new BitLeafletMapProvider(),
    };
}";

    private readonly string example6RazorCode = @"
<div style=""height:320px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""eventsMapRef""
            Provider=""@eventsProvider""
            OnClick=""OnMapClick""
            OnDoubleClick=""OnMapDoubleClick""
            OnViewChanged=""OnViewChanged"" />
</div>
<BitButton OnClick=""FlyToTokyo"">Fly to Tokyo</BitButton>
<BitButton OnClick=""ReadView"">Log viewport</BitButton>";
    private readonly string example6CsharpCode = @"
private Task OnMapClick(BitMapLatLng p) { /* p.Latitude, p.Longitude */ return Task.CompletedTask; }
private Task OnViewChanged(BitMapViewState v) { /* v.Zoom, v.Center, v.Bounds */ return Task.CompletedTask; }
private async Task FlyToTokyo() => await eventsMapRef.FlyTo(new(35.6762, 139.6503), 12);
private async Task ReadView() { var v = await eventsMapRef.GetView(); }";

    private readonly string example7RazorCode = @"
<BitMap TMapProvider=""BitLeafletMapProvider""
        @ref=""advMapRef""
        Provider=""@BuildAdvancedProvider()""
        OnReady=""OnAdvancedReady""
        OnDoubleClick=""OnAdvancedDoubleClick"" />";
    private readonly string example7CsharpCode = @"
private BitLeafletMapProvider BuildAdvancedProvider() => new()
{
    Center = new(51.5074, -0.1278), Zoom = 11,
    ScrollWheelZoom = advScrollWheel,
    Dragging = advDragging,
    ShowScaleControl = advScaleBar,
    MaxBounds = advMaxBounds
        ? new BitMapLatLngBounds(new(51.25, -0.55), new(51.75, 0.35))
        : null,
};

// Tile overlay
await advMapRef.AddTileOverlay(new BitMapTileOverlay
{
    Id = ""labels"",
    UrlTemplate = ""https://example.com/tiles/{z}/{x}/{y}.png"",
    Opacity = 0.85,
    ZIndex = 400,
});
await advMapRef.RemoveTileOverlay(""labels"");";

    private readonly string example8RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitMapLibreMapProvider""
            Provider=""@(new BitMapLibreMapProvider { Center = new(48.8566, 2.3522), Zoom = 5 })"" />
</div>";

    private readonly string example9RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitOpenLayersMapProvider""
            Provider=""@(new BitOpenLayersMapProvider { Center = new(35.6762, 139.6503), Zoom = 4 })"" />
</div>";

    private readonly string example10RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitMapboxMapProvider""
            Provider=""@(new BitMapboxMapProvider { AccessToken = ""YOUR_MAPBOX_TOKEN"", Center = new(40, 0), Zoom = 2 })"" />
</div>";
    private readonly string example10CsharpCode = @"
// Get your token from https://account.mapbox.com/access-tokens/
// Pass it via the AccessToken property on BitMapboxMapProvider.";

    private readonly string example11RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitArcGisMapProvider""
            Provider=""@(new BitArcGisMapProvider { BasemapId = ""osm"", Center = new(40, 0), Zoom = 2 })"" />
</div>";

    private readonly string example12RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitAzureMapsMapProvider""
            Provider=""@(new BitAzureMapsMapProvider { SubscriptionKey = ""YOUR_AZURE_MAPS_KEY"", Center = new(40, 0), Zoom = 2 })"" />
</div>";
    private readonly string example12CsharpCode = @"
// Get your key from Azure Portal > Maps account > Authentication > Shared Key
// Pass it via the SubscriptionKey property on BitAzureMapsMapProvider.";

    private readonly string example13RazorCode = @"
<div style=""height:420px"">
    <BitMap TMapProvider=""BitCesiumMapProvider""
            Provider=""@(new BitCesiumMapProvider { Center = new(20, 0), Zoom = 2, SceneMode = ""scene3d"" })"" />
</div>";
}
