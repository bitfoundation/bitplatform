namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Map;

public partial class BitMapDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "TMapProvider",
            Type = "Type (generic)",
            DefaultValue = "",
            Description = "The map provider type. One of: BitLeafletMapProvider, BitMapLibreMapProvider, BitMapboxMapProvider, BitOpenLayersMapProvider, BitArcGisMapProvider, BitAzureMapsMapProvider, BitCesiumMapProvider.",
         },
         new()
         {
            Name = "Provider",
            Type = "TMapProvider?",
            DefaultValue = "null",
            Description = "Provider configuration instance (center, zoom, tokens, etc.). When null a default instance is created.",
         },
         new()
         {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Optional content rendered above the map canvas.",
         },
         new()
         {
            Name = "ReplayStateOnProviderSwap",
            Type = "bool",
            DefaultValue = "false",
            Description = "When true, imperatively-added markers, vector layers, and tile overlays are replayed after a destructive provider swap (different JsObjectName).",
         },
         new()
         {
            Name = "OnReady",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Fires after the map is ready for imperative calls. Fires once on initial mount, and fires again after a destructive provider swap each time the new provider becomes ready.",
         },
         new()
         {
            Name = "OnClick",
            Type = "EventCallback<BitMapLatLng>",
            DefaultValue = "",
            Description = "Fires when the user clicks the map canvas.",
         },
         new()
         {
            Name = "OnDoubleClick",
            Type = "EventCallback<BitMapLatLng>",
            DefaultValue = "",
            Description = "Fires when the user double-clicks the map.",
         },
         new()
         {
            Name = "OnViewChanged",
            Type = "EventCallback<BitMapViewState>",
            DefaultValue = "",
            Description = "Fires whenever the map view changes.",
         },
         new()
         {
            Name = "OnMarkerClick",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Fires when the user clicks a marker (argument is the marker id).",
         },
         new()
         {
            Name = "OnMarkerDragEnd",
            Type = "EventCallback<BitMapMarkerDragEndArgs>",
            DefaultValue = "",
            Description = "Fires when a draggable marker is dropped.",
         },
         new()
         {
            Name = "OnVectorClick",
            Type = "EventCallback<BitMapVectorClickArgs>",
            DefaultValue = "",
            Description = "Fires when the user clicks a vector layer.",
         },
         new()
         {
            Name = "OnGeoJsonFeatureClick",
            Type = "EventCallback<BitMapGeoJsonFeatureClickArgs>",
            DefaultValue = "",
            Description = "Fires when the user clicks a GeoJSON feature.",
         },
         new()
         {
            Name = "OnInteropError",
            Type = "EventCallback<BitMapInteropErrorArgs>",
            DefaultValue = "",
            Description = "Fires when an interop call into the underlying provider fails. Lets consumers surface errors that the component would otherwise swallow to prevent circuit-breaking exceptions.",
         },
    ];

    private readonly List<ComponentParameter> componentPublicMembers =
    [
         new()
         {
            Name = "IsReady",
            Type = "bool",
            DefaultValue = "false",
            Description = "True after the map is ready for interop calls.",
         },
         new()
         {
            Name = "GetView",
            Type = "Func<ValueTask<BitMapViewState>>",
            DefaultValue = "",
            Description = "Returns a snapshot of the current viewport.",
         },
         new()
         {
            Name = "SetView",
            Type = "Func<BitMapLatLng, double?, bool, ValueTask>",
            DefaultValue = "",
            Description = "Pan and optionally zoom to the given center.",
         },
         new()
         {
            Name = "FlyTo",
            Type = "Func<BitMapLatLng, double?, ValueTask>",
            DefaultValue = "",
            Description = "Animated pan/zoom to the given center.",
         },
         new()
         {
            Name = "FitBounds",
            Type = "Func<BitMapLatLngBounds, int, ValueTask>",
            DefaultValue = "",
            Description = "Fit the view to the given bounding box.",
         },
         new()
         {
            Name = "FitBoundsToMarkers",
            Type = "Func<int, ValueTask>",
            DefaultValue = "",
            Description = "Fit the view to include all current markers.",
         },
         new()
         {
            Name = "InvalidateSize",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Recalculate map size after a container resize.",
         },
         new()
         {
            Name = "AddMarker",
            Type = "Func<BitMapMarker, ValueTask>",
            DefaultValue = "",
            Description = "Add a marker to the map.",
         },
         new()
         {
            Name = "RemoveMarker",
            Type = "Func<string, ValueTask>",
            DefaultValue = "",
            Description = "Remove a marker by id.",
         },
         new()
         {
            Name = "ClearMarkers",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Remove all markers.",
         },
         new()
         {
            Name = "SetMarkerPosition",
            Type = "Func<string, BitMapLatLng, ValueTask>",
            DefaultValue = "",
            Description = "Move a marker to a new position.",
         },
         new()
         {
            Name = "OpenMarkerPopup",
            Type = "Func<string, ValueTask>",
            DefaultValue = "",
            Description = "Open a marker's popup.",
         },
         new()
         {
            Name = "SyncMarkers",
            Type = "Func<IEnumerable<BitMapMarker>, ValueTask>",
            DefaultValue = "",
            Description = "Replace all markers in one batch.",
         },
         new()
         {
            Name = "AddPolyline",
            Type = "Func<string, IReadOnlyList<BitMapLatLng>, BitMapVectorPathStyle?, ValueTask>",
            DefaultValue = "",
            Description = "Add a polyline.",
         },
         new()
         {
            Name = "AddPolygon",
            Type = "Func<string, IReadOnlyList<BitMapLatLng>, BitMapVectorPathStyle?, ValueTask>",
            DefaultValue = "",
            Description = "Add a polygon.",
         },
         new()
         {
            Name = "AddCircle",
            Type = "Func<string, BitMapLatLng, double, BitMapVectorPathStyle?, ValueTask>",
            DefaultValue = "",
            Description = "Add a circle (radius in meters).",
         },
         new()
         {
            Name = "AddRectangle",
            Type = "Func<string, BitMapLatLngBounds, BitMapVectorPathStyle?, ValueTask>",
            DefaultValue = "",
            Description = "Add a rectangle.",
         },
         new()
         {
            Name = "AddGeoJson",
            Type = "Func<string, string, BitMapVectorPathStyle?, ValueTask>",
            DefaultValue = "",
            Description = "Add a GeoJSON layer.",
         },
         new()
         {
            Name = "RemoveLayer",
            Type = "Func<string, ValueTask>",
            DefaultValue = "",
            Description = "Remove a vector layer by id.",
         },
         new()
         {
            Name = "ClearVectorLayers",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Remove all vector layers.",
         },
         new()
         {
            Name = "AddTileOverlay",
            Type = "Func<BitMapTileOverlay, ValueTask>",
            DefaultValue = "",
            Description = "Add a tile overlay above the base map.",
         },
         new()
         {
            Name = "RemoveTileOverlay",
            Type = "Func<string, ValueTask>",
            DefaultValue = "",
            Description = "Remove a tile overlay by id.",
         },
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
            Title = "Paris", PopupHtml = (MarkupString)"<b>Paris</b><br/>Click to open popup.",
        });
        await markersMapRef.AddMarker(new BitMapMarker
        {
            Id = "london", Position = new(51.5074, -0.1278),
            Title = "London", PopupHtml = (MarkupString)"<b>London</b><br/>Draggable marker.",
            Draggable = true,
            TooltipHtml = (MarkupString)"Drag me!",
        });
        await markersMapRef.FitBoundsToMarkers();
    }

    private async Task AddRandomMarker()
    {
        _markerCounter++;
        var id = $"m{_markerCounter}";

        // Scatter inside the current viewport so new markers are always visible
        // wherever the user has panned/zoomed to. We inset the bounds slightly so
        // markers don't land right on the edge.
        var view = await markersMapRef.GetView();
        var sw = view.Bounds.SouthWest;
        var ne = view.Bounds.NorthEast;

        var latSpan = ne.Latitude - sw.Latitude;
        // Handle the antimeridian: when crossing it, NE.lng < SW.lng, so add 360°.
        var lngSpan = ne.Longitude - sw.Longitude;
        if (lngSpan < 0) lngSpan += 360;

        const double inset = 0.1; // keep markers ~10% inside each edge
        var lat = sw.Latitude + (inset + Random.Shared.NextDouble() * (1 - 2 * inset)) * latSpan;
        var lng = sw.Longitude + (inset + Random.Shared.NextDouble() * (1 - 2 * inset)) * lngSpan;
        lat = Math.Clamp(lat, -85, 85);
        if (lng > 180) lng -= 360;
        else if (lng < -180) lng += 360;

        // Randomly make some markers draggable so the demo shows OnMarkerDragEnd in action.
        var draggable = Random.Shared.Next(2) == 0;

        await markersMapRef.AddMarker(new BitMapMarker
        {
            Id = id, Position = new(lat, lng),
            Title = $"Marker {id}{(draggable ? " (draggable)" : "")}",
            PopupHtml = (MarkupString)($"Marker <code>{id}</code><br/>{lat:F4}, {lng:F4}" +
                        (draggable ? "<br/><i>Drag me!</i>" : "")),
            Draggable = draggable,
            TooltipHtml = draggable ? (MarkupString?)(MarkupString)"Drag me!" : null,
        });
        markersLog = $"Added {id}{(draggable ? " (draggable)" : "")} at {lat:F4}, {lng:F4}";
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
        geoJsonLog = $"Layer {e.LayerId} - properties.name = {name}";
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
        // Rebuilding the provider replaces the underlying Leaflet map instance,
        // so any previously-added overlays no longer exist on the new map.
        // Reset the toggle state so the UI label/branch reflects that.
        advOverlayOn = false;
        return advProvider;
    }

    private async Task OnAdvancedReady()
    {
        await AddTooltipMarkers();
    }

    private async Task AddTooltipMarkers()
    {
        await advMapRef.ClearMarkers();
        await advMapRef.AddMarker(new BitMapMarker { Id = "a", Position = new(51.52, -0.10), TooltipHtml = (MarkupString)"<b>West End</b>", PopupHtml = (MarkupString)"Popup A", ZIndexOffset = 10 });
        await advMapRef.AddMarker(new BitMapMarker { Id = "b", Position = new(51.50, -0.08), TooltipHtml = (MarkupString)"City", PopupHtml = (MarkupString)"Popup B" });
        await advMapRef.AddMarker(new BitMapMarker { Id = "c", Position = new(51.48, -0.06), TooltipHtml = (MarkupString)"South Bank", PopupHtml = (MarkupString)"Popup C" });
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
<div style=""display:flex;gap:0.5rem;flex-wrap:wrap;align-items:center"">
    <BitButton OnClick=""AddRandomMarker"">Add random marker</BitButton>
    <BitButton OnClick=""ClearMarkers"" Variant=""BitVariant.Outline"">Clear all</BitButton>
    <BitButton OnClick=""OpenLondonPopup"" Variant=""BitVariant.Outline"">Open London popup</BitButton>
    <BitButton OnClick=""FitToMarkers"" Variant=""BitVariant.Outline"">Fit to markers</BitButton>
</div>
<pre>@markersLog</pre>";
    private readonly string example2CsharpCode = @"
private BitMap<BitLeafletMapProvider> markersMapRef = default!;
private readonly BitLeafletMapProvider markersProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
private string markersLog = ""Seed markers are added on OnReady. Try the buttons."";
private int _markerCounter;

private async Task OnMarkersReady()
{
    await markersMapRef.AddMarker(new BitMapMarker
    {
        Id = ""paris"", Position = new(48.8566, 2.3522),
        Title = ""Paris"", PopupHtml = (MarkupString)""<b>Paris</b><br/>Click to open popup."",
    });
    await markersMapRef.AddMarker(new BitMapMarker
    {
        Id = ""london"", Position = new(51.5074, -0.1278),
        Title = ""London"", PopupHtml = (MarkupString)""<b>London</b><br/>Draggable marker."",
        Draggable = true,
        TooltipHtml = (MarkupString)""Drag me!"",
    });
    await markersMapRef.FitBoundsToMarkers();
}

private async Task AddRandomMarker()
{
    _markerCounter++;
    var id = $""m{_markerCounter}"";

    // Scatter inside the current viewport so new markers are always visible.
    var view = await markersMapRef.GetView();
    var sw = view.Bounds.SouthWest;
    var ne = view.Bounds.NorthEast;

    var latSpan = ne.Latitude - sw.Latitude;
    var lngSpan = ne.Longitude - sw.Longitude;
    if (lngSpan < 0) lngSpan += 360; // antimeridian

    const double inset = 0.1;
    var lat = sw.Latitude + (inset + Random.Shared.NextDouble() * (1 - 2 * inset)) * latSpan;
    var lng = sw.Longitude + (inset + Random.Shared.NextDouble() * (1 - 2 * inset)) * lngSpan;
    lat = Math.Clamp(lat, -85, 85);
    if (lng > 180) lng -= 360;
    else if (lng < -180) lng += 360;

    // Roll a coin so some markers come in draggable.
    var draggable = Random.Shared.Next(2) == 0;

    await markersMapRef.AddMarker(new BitMapMarker
    {
        Id = id, Position = new(lat, lng),
        Title = $""Marker {id}{(draggable ? "" (draggable)"" : """")}"",
        PopupHtml = (MarkupString)($""Marker <code>{id}</code><br/>{lat:F4}, {lng:F4}"" +
                    (draggable ? ""<br/><i>Drag me!</i>"" : """")),
        Draggable = draggable,
        TooltipHtml = draggable ? (MarkupString?)(MarkupString)""Drag me!"" : null,
    });
    markersLog = $""Added {id}{(draggable ? "" (draggable)"" : """")} at {lat:F4}, {lng:F4}"";
}

private async Task ClearMarkers()
{
    await markersMapRef.ClearMarkers();
    markersLog = ""All markers cleared."";
}

private async Task OpenLondonPopup()
{
    await markersMapRef.OpenMarkerPopup(""london"");
    markersLog = ""Opened London popup."";
}

private async Task FitToMarkers()
{
    await markersMapRef.FitBoundsToMarkers();
    markersLog = ""Fitted view to all markers."";
}

private Task OnMarkerClick(string id)
{
    markersLog = $""Marker click: {id}"";
    return Task.CompletedTask;
}

private Task OnMarkerDragEnd(BitMapMarkerDragEndArgs e)
{
    markersLog = $""Drag end {e.Id} → {e.Position.Latitude:F5}, {e.Position.Longitude:F5}"";
    return Task.CompletedTask;
}";

    private readonly string example3RazorCode = @"
<div style=""height:380px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""vectorsMapRef""
            Provider=""@vectorsProvider""
            OnReady=""OnVectorsReady""
            OnVectorClick=""OnVectorClick"" />
</div>
<div style=""display:flex;gap:0.5rem;flex-wrap:wrap"">
    <BitButton OnClick=""RedrawVectors"">Redraw</BitButton>
    <BitButton OnClick=""ClearVectors"" Variant=""BitVariant.Outline"">Clear vectors</BitButton>
</div>
<pre>@vectorsLog</pre>";
    private readonly string example3CsharpCode = @"
private BitMap<BitLeafletMapProvider> vectorsMapRef = default!;
private readonly BitLeafletMapProvider vectorsProvider = new() { Center = new(37.7749, -122.4194), Zoom = 12 };
private string vectorsLog = ""Click Redraw to draw shapes, then click a shape."";

private async Task OnVectorsReady() => await DrawVectors();

private async Task DrawVectors()
{
    await vectorsMapRef.AddPolyline(""route"",
    [
        new(37.80, -122.42), new(37.79, -122.41),
        new(37.78, -122.40), new(37.77, -122.395),
    ], new BitMapVectorPathStyle { Color = ""#f85149"", Weight = 5, Opacity = 0.9 });

    await vectorsMapRef.AddPolygon(""park"",
    [
        new(37.769, -122.486), new(37.771, -122.475),
        new(37.765, -122.472), new(37.762, -122.482),
    ], new BitMapVectorPathStyle { Color = ""#3fb950"", FillOpacity = 0.35, Weight = 2 });

    await vectorsMapRef.AddCircle(""radius"", new(37.7849, -122.4094), 900,
        new BitMapVectorPathStyle { Color = ""#58a6ff"", FillOpacity = 0.15, Weight = 2 });

    await vectorsMapRef.AddRectangle(""box"",
        new BitMapLatLngBounds(new(37.748, -122.44), new(37.756, -122.42)),
        new BitMapVectorPathStyle { Color = ""#d29922"", FillOpacity = 0.12, Weight = 2, DashArray = ""6,4"" });

    await vectorsMapRef.FitBounds(
        new BitMapLatLngBounds(new(37.755, -122.49), new(37.805, -122.38)));
}

private async Task RedrawVectors()
{
    await vectorsMapRef.ClearVectorLayers();
    await DrawVectors();
    vectorsLog = ""Vectors redrawn."";
}

private async Task ClearVectors()
{
    await vectorsMapRef.ClearVectorLayers();
    vectorsLog = ""All vector layers cleared."";
}

private Task OnVectorClick(BitMapVectorClickArgs e)
{
    // e.Kind = ""polyline"" | ""polygon"" | ""circle"" | ""rectangle""
    // e.LayerId = the id you passed to AddPolyline/AddPolygon/…
    vectorsLog = $""{e.Kind} \""{e.LayerId}\"" @ {e.Position.Latitude:F5}, {e.Position.Longitude:F5}"";
    return Task.CompletedTask;
}";

    private readonly string example4RazorCode = @"
<div style=""height:380px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""geoJsonMapRef""
            Provider=""@geoJsonProvider""
            OnGeoJsonFeatureClick=""OnGeoJsonFeatureClick"" />
</div>
<div style=""display:flex;gap:0.5rem;flex-wrap:wrap"">
    <BitButton OnClick=""LoadGeoJson"">Load GeoJSON</BitButton>
    <BitButton OnClick=""RemoveGeoJson"" Variant=""BitVariant.Outline"">Remove layer</BitButton>
</div>
<pre>@geoJsonLog</pre>";
    private readonly string example4CsharpCode = @"
private BitMap<BitLeafletMapProvider> geoJsonMapRef = default!;
private readonly BitLeafletMapProvider geoJsonProvider = new() { Center = new(40.7128, -74.0060), Zoom = 11 };
private string geoJsonLog = ""Click 'Load GeoJSON', then click a feature."";

private async Task LoadGeoJson()
{
    await geoJsonMapRef.RemoveLayer(""demo"");
    await geoJsonMapRef.AddGeoJson(""demo"", SampleGeoJson,
        new BitMapVectorPathStyle { Color = ""#a371f7"", Weight = 3, FillOpacity = 0.25 });
    await geoJsonMapRef.FitBounds(new BitMapLatLngBounds(new(40.71, -74.03), new(40.83, -73.96)));
    geoJsonLog = ""GeoJSON loaded. Click a feature."";
}

private async Task RemoveGeoJson()
{
    await geoJsonMapRef.RemoveLayer(""demo"");
    geoJsonLog = ""Layer \""demo\"" removed."";
}

private Task OnGeoJsonFeatureClick(BitMapGeoJsonFeatureClickArgs e)
{
    // e.LayerId = ""demo""
    // e.Properties = JsonElement of feature.properties
    var name = ""(no name)"";
    if (e.Properties.ValueKind == System.Text.Json.JsonValueKind.Object
        && e.Properties.TryGetProperty(""name"", out var n))
    {
        name = n.ValueKind == System.Text.Json.JsonValueKind.String ? n.GetString() : n.ToString();
    }
    geoJsonLog = $""Layer {e.LayerId} - properties.name = {name}"";
    return Task.CompletedTask;
}

// Minimal GeoJSON FeatureCollection used by LoadGeoJson above.
private const string SampleGeoJson = """"""
    {
      ""type"": ""FeatureCollection"",
      ""features"": [
        {
          ""type"": ""Feature"",
          ""properties"": { ""name"": ""Central Park"" },
          ""geometry"": {
            ""type"": ""Polygon"",
            ""coordinates"": [[
              [-73.981, 40.768], [-73.958, 40.768],
              [-73.958, 40.800], [-73.981, 40.800],
              [-73.981, 40.768]
            ]]
          }
        },
        {
          ""type"": ""Feature"",
          ""properties"": { ""name"": ""Brooklyn Bridge"" },
          ""geometry"": {
            ""type"": ""LineString"",
            ""coordinates"": [[-73.9969, 40.7061], [-73.9875, 40.7026]]
          }
        }
      ]
    }
    """""";";

    private readonly string example5RazorCode = @"
<div style=""display:flex;gap:0.5rem;flex-wrap:wrap;margin-bottom:0.75rem"">
    <BitButton OnClick='() => SetTileProvider(""osm"")'
               Variant=""@(tileProvider == ""osm"" ? BitVariant.Fill : BitVariant.Outline)"">OSM default</BitButton>
    <BitButton OnClick='() => SetTileProvider(""carto"")'
               Variant=""@(tileProvider == ""carto"" ? BitVariant.Fill : BitVariant.Outline)"">Carto Voyager</BitButton>
    <BitButton OnClick='() => SetTileProvider(""topo"")'
               Variant=""@(tileProvider == ""topo"" ? BitVariant.Fill : BitVariant.Outline)"">OpenTopoMap</BitButton>
</div>

@* @key forces a new map instance when the provider changes *@
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider"" @key=""tileProvider"" Provider=""@currentTileLeafletProvider"" />
</div>";
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
            Center = new(20, 0), Zoom = 2,
            TileUrl = ""https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png"",
            TileAttribution = ""&copy; OpenStreetMap contributors &copy; <a href=\""https://carto.com/attributions\"">CARTO</a>"",
        },
        ""topo"" => new BitLeafletMapProvider
        {
            Center = new(46.5, 11.3), Zoom = 10,
            TileUrl = ""https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png"",
            TileAttribution = ""Map data: &copy; OpenStreetMap contributors, SRTM | Map style: &copy; OpenTopoMap"",
            TileMaxZoom = 17,
        },
        _ => new BitLeafletMapProvider { Center = new(51.505, -0.09), Zoom = 13 },
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
<div style=""display:flex;gap:0.5rem;flex-wrap:wrap"">
    <BitButton OnClick=""FlyToTokyo"">Fly to Tokyo</BitButton>
    <BitButton OnClick=""ReadView"" Variant=""BitVariant.Outline"">Log viewport</BitButton>
</div>
<pre>@eventsLog</pre>";
    private readonly string example6CsharpCode = @"
private BitMap<BitLeafletMapProvider> eventsMapRef = default!;
private readonly BitLeafletMapProvider eventsProvider = new() { Center = new(35.6762, 139.6503), Zoom = 11 };
private string eventsLog = ""Pan/zoom or click the map."";

private Task OnMapClick(BitMapLatLng p)
{
    eventsLog = $""Click → {p.Latitude:F5}, {p.Longitude:F5}"";
    return Task.CompletedTask;
}

private Task OnMapDoubleClick(BitMapLatLng p)
{
    eventsLog = $""Double-click → {p.Latitude:F5}, {p.Longitude:F5}"";
    return Task.CompletedTask;
}

private Task OnViewChanged(BitMapViewState v)
{
    eventsLog = $""View: zoom {v.Zoom:F1}, center {v.Center.Latitude:F4},{v.Center.Longitude:F4}"";
    return Task.CompletedTask;
}

private async Task FlyToTokyo()
{
    await eventsMapRef.FlyTo(new(35.6762, 139.6503), 12);
    eventsLog = ""Flying to Tokyo…"";
}

private async Task ReadView()
{
    var v = await eventsMapRef.GetView();
    eventsLog = $""GetView → zoom {v.Zoom:F2}, center {v.Center.Latitude:F4},{v.Center.Longitude:F4}, "" +
                $""NE {v.Bounds.NorthEast.Latitude:F4},{v.Bounds.NorthEast.Longitude:F4}"";
}";

    private readonly string example7RazorCode = @"
<div style=""display:flex;gap:1rem;flex-wrap:wrap;margin-bottom:0.75rem"">
    <BitToggle Value=""advScrollWheel""
               ValueChanged=""v => { advScrollWheel = v; BuildAdvancedProvider(); }""
               Text=""Scroll wheel zoom"" />
    <BitToggle Value=""advDragging""
               ValueChanged=""v => { advDragging = v; BuildAdvancedProvider(); }""
               Text=""Dragging"" />
    <BitToggle Value=""advScaleBar""
               ValueChanged=""v => { advScaleBar = v; BuildAdvancedProvider(); }""
               Text=""Scale bar"" />
    <BitToggle Value=""advMaxBounds""
               ValueChanged=""v => { advMaxBounds = v; BuildAdvancedProvider(); }""
               Text=""Limit pan (London)"" />
</div>

@* Bind a stable field, not a method call: a method call reallocates the provider on every render. *@
<div style=""height:380px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""advMapRef""
            Provider=""@advProvider""
            OnReady=""OnAdvancedReady""
            OnDoubleClick=""OnAdvancedDoubleClick"" />
</div>

<div style=""display:flex;gap:0.5rem;flex-wrap:wrap"">
    <BitButton OnClick=""AddTooltipMarkers"">Add tooltip markers + fit</BitButton>
    <BitButton OnClick=""ToggleTileOverlay""
               Variant=""BitVariant.Outline"">@(advOverlayOn ? ""Remove overlay"" : ""Add tile overlay"")</BitButton>
    <BitButton OnClick=""ReadAdvancedView"" Variant=""BitVariant.Outline"">Log viewport</BitButton>
</div>
<pre>@advLog</pre>";
    private readonly string example7CsharpCode = @"
private BitMap<BitLeafletMapProvider> advMapRef = default!;
private bool advScrollWheel = true;
private bool advDragging = true;
private bool advScaleBar = true;
private bool advMaxBounds;
private bool advOverlayOn;
private string advLog = ""Toggle options or use the buttons."";

private BitLeafletMapProvider advProvider = new()
{
    Center = new(51.5074, -0.1278), Zoom = 11,
    ScrollWheelZoom = true,
    Dragging = true,
    ShowScaleControl = true,
    MaxBounds = null,
};

// Mutate the stable field only when an option actually changes - not on every render.
private BitLeafletMapProvider BuildAdvancedProvider()
{
    advProvider = new BitLeafletMapProvider
    {
        Center = new(51.5074, -0.1278), Zoom = 11,
        ScrollWheelZoom = advScrollWheel,
        Dragging = advDragging,
        ShowScaleControl = advScaleBar,
        MaxBounds = advMaxBounds
            ? new BitMapLatLngBounds(new(51.25, -0.55), new(51.75, 0.35))
            : null,
    };
    // Rebuilding the provider replaces the underlying Leaflet map instance,
    // so any previously-added overlays no longer exist on the new map.
    // Reset the toggle state so the UI label/branch reflects that.
    advOverlayOn = false;
    return advProvider;
}

private async Task OnAdvancedReady() => await AddTooltipMarkers();

private async Task AddTooltipMarkers()
{
    await advMapRef.ClearMarkers();
    await advMapRef.AddMarker(new BitMapMarker { Id = ""a"", Position = new(51.52, -0.10), TooltipHtml = (MarkupString)""<b>West End</b>"", PopupHtml = (MarkupString)""Popup A"", ZIndexOffset = 10 });
    await advMapRef.AddMarker(new BitMapMarker { Id = ""b"", Position = new(51.50, -0.08), TooltipHtml = (MarkupString)""City"", PopupHtml = (MarkupString)""Popup B"" });
    await advMapRef.AddMarker(new BitMapMarker { Id = ""c"", Position = new(51.48, -0.06), TooltipHtml = (MarkupString)""South Bank"", PopupHtml = (MarkupString)""Popup C"" });
    await advMapRef.FitBoundsToMarkers(56);
    advLog = ""Three tooltip markers added; view fitted."";
}

private async Task ToggleTileOverlay()
{
    if (advOverlayOn)
    {
        await advMapRef.RemoveTileOverlay(""labels"");
        advOverlayOn = false;
        advLog = ""Tile overlay removed."";
    }
    else
    {
        await advMapRef.AddTileOverlay(new BitMapTileOverlay
        {
            Id = ""labels"",
            UrlTemplate = ""https://tiles.stadiamaps.com/tiles/stamen_toner_labels/{z}/{x}/{y}{r}.png"",
            Attribution = ""Map tiles by Stamen Design, hosted by Stadia Maps. Data by OpenStreetMap."",
            Opacity = 0.85,
            ZIndex = 400,
            MaxZoom = 20,
        });
        advOverlayOn = true;
        advLog = ""Tile overlay added (may fail if the tile host blocks your origin)."";
    }
}

private async Task ReadAdvancedView()
{
    var v = await advMapRef.GetView();
    advLog = $""GetView → zoom {v.Zoom:F2}, center {v.Center.Latitude:F4},{v.Center.Longitude:F4}, "" +
             $""NE {v.Bounds.NorthEast.Latitude:F4},{v.Bounds.NorthEast.Longitude:F4}"";
}

private Task OnAdvancedDoubleClick(BitMapLatLng p)
{
    advLog = $""Double-click at {p.Latitude:F4}, {p.Longitude:F4}"";
    return Task.CompletedTask;
}";

    private readonly string example8RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitMapLibreMapProvider"" Provider=""@maplibreProvider"" />
</div>";
    private readonly string example8CsharpCode = @"
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitMapLibreMapProvider maplibreProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };";

    private readonly string example9RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitOpenLayersMapProvider"" Provider=""@olProvider"" />
</div>";
    private readonly string example9CsharpCode = @"
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitOpenLayersMapProvider olProvider = new() { Center = new(35.6762, 139.6503), Zoom = 4 };";

    private readonly string example10RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitMapboxMapProvider"" Provider=""@mapboxProvider"" />
</div>";
    private readonly string example10CsharpCode = @"
// Get your token from https://account.mapbox.com/access-tokens/
// and pass it via the AccessToken property on BitMapboxMapProvider.
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitMapboxMapProvider mapboxProvider = new()
{
    AccessToken = ""YOUR_MAPBOX_TOKEN"",
    Center = new(40, 0),
    Zoom = 2,
};";

    private readonly string example11RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitArcGisMapProvider"" Provider=""@arcGisProvider"" />
</div>";
    private readonly string example11CsharpCode = @"
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitArcGisMapProvider arcGisProvider = new() { Center = new(40, 0), Zoom = 2, BasemapId = ""osm"" };";

    private readonly string example12RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitAzureMapsMapProvider"" Provider=""@azureMapsProvider"" />
</div>";
    private readonly string example12CsharpCode = @"
// Get your key from Azure Portal > Maps account > Authentication > Shared Key
// and pass it via the SubscriptionKey property on BitAzureMapsMapProvider.
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitAzureMapsMapProvider azureMapsProvider = new()
{
    SubscriptionKey = ""YOUR_AZURE_MAPS_KEY"",
    Center = new(40, 0),
    Zoom = 2,
};";

    private readonly string example13RazorCode = @"
<div style=""height:420px"">
    <BitMap TMapProvider=""BitCesiumMapProvider"" Provider=""@cesiumProvider"" />
</div>";
    private readonly string example13CsharpCode = @"
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitCesiumMapProvider cesiumProvider = new() { Center = new(20, 0), Zoom = 2, SceneMode = ""scene3d"" };";
}
