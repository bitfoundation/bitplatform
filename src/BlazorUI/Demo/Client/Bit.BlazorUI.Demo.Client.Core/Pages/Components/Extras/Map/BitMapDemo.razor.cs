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
            Name = "Center",
            Type = "BitMapLatLng?",
            DefaultValue = "null",
            Description = "Two-way bindable centre. Assigning it moves the map, and panning the map writes the new centre back. Leave it unset to let the provider's Center own the camera.",
         },
         new()
         {
            Name = "Zoom",
            Type = "double?",
            DefaultValue = "null",
            Description = "Two-way bindable zoom level. Behaves like Center: assigning it zooms the map, and zooming the map writes the new level back.",
         },
         new()
         {
            Name = "Markers",
            Type = "IEnumerable<BitMapMarker>?",
            DefaultValue = "null",
            Description = "The markers the map should show, as a collection instead of imperative calls. Markers compare by value, so only the ones that actually changed are sent; a wholesale change is replaced in one batched call. This parameter owns the marker set - mixing it with AddMarker/RemoveMarker means the next collection change reconciles those away.",
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
            Name = "Clustering",
            Type = "BitMapClustering?",
            DefaultValue = "null",
            Description = "Groups nearby markers into a bubble showing how many they stand for, and expands it on click. The grouping runs inside BitMap in screen space, so it behaves identically on all seven backends. While it is on, the markers you add are the source set and BitMap decides what the provider actually draws.",
         },
         new()
         {
            Name = "OnClusterClick",
            Type = "EventCallback<BitMapClusterClickArgs>",
            DefaultValue = "",
            Description = "Fires when the user clicks a cluster bubble, instead of OnMarkerClick - a cluster is not one of your markers.",
         },
         new()
         {
            Name = "MarkerPopupTemplate",
            Type = "RenderFragment<BitMapMarker>?",
            DefaultValue = "null",
            Description = "Content of the popup that opens when a marker is clicked, as live Blazor markup rather than an HTML string - so it is escaped automatically, and components, event handlers and bindings work inside it. Rendered by BitMap and pinned to its marker, so it behaves the same on all seven backends.",
         },
         new()
         {
            Name = "PopupLabel",
            Type = "string",
            DefaultValue = "Marker details",
            Description = "Accessible name of the popup when its marker has neither an Alt nor a Title.",
         },
         new()
         {
            Name = "PopupCloseLabel",
            Type = "string",
            DefaultValue = "Close",
            Description = "Accessible name of the popup's close button.",
         },
         new()
         {
            Name = "OnPopupOpened",
            Type = "EventCallback<BitMapMarker>",
            DefaultValue = "",
            Description = "Fires when a MarkerPopupTemplate popup opens, with the marker it belongs to.",
         },
         new()
         {
            Name = "OnPopupClosed",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Fires when a MarkerPopupTemplate popup closes.",
         },
         new()
         {
            Name = "MarkerListMode",
            Type = "BitMapMarkerListMode",
            DefaultValue = "BitMapMarkerListMode.None",
            Description = "Renders a text alternative to the markers - a table of names and coordinates whose rows bring their marker into view. A map is a picture, so its markers are unreachable to a screen reader no matter how well the canvas is labelled; ScreenReaderOnly leaves the map looking unchanged while making the same data reachable.",
         },
         new()
         {
            Name = "MarkerListTemplate",
            Type = "RenderFragment<IReadOnlyList<BitMapMarker>>?",
            DefaultValue = "null",
            Description = "Replaces the built-in marker table. Receives the markers in the order they were added.",
         },
         new()
         {
            Name = "MarkerListCaption",
            Type = "string",
            DefaultValue = "Map markers",
            Description = "Caption of the built-in marker table. Say what the markers are, not that they are markers.",
         },
         new()
         {
            Name = "MarkerListZoom",
            Type = "double?",
            DefaultValue = "15",
            Description = "Zoom applied when a marker-list row brings its marker into view. Null keeps the current zoom.",
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
            Name = "OnContextMenu",
            Type = "EventCallback<BitMapLatLng>",
            DefaultValue = "",
            Description = "Fires when the user right-clicks (or long-presses on touch) the map, with the coordinate under the pointer. The browser's own menu still opens unless the provider sets SuppressBrowserContextMenu.",
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
            Name = "AutoResize",
            Type = "bool",
            DefaultValue = "true",
            Description = "Keeps the map sized to its container through a ResizeObserver. Also recovers the classic case of a map created inside a hidden tab, which would otherwise stay grey after the tab is shown.",
         },
         new()
         {
            Name = "LazyLoad",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defers creating the map until its container scrolls into view, so a map below the fold doesn't compete with the page's first paint. LoadState stays Idle while it waits.",
         },
         new()
         {
            Name = "LazyLoadRootMargin",
            Type = "string",
            DefaultValue = "200px",
            Description = "How far outside the viewport the container may be and still count as visible for LazyLoad. Any margin syntax IntersectionObserver accepts.",
         },
         new()
         {
            Name = "CooperativeGestures",
            Type = "bool",
            DefaultValue = "false",
            Description = "Requires ctrl/⌘ + wheel to zoom and two fingers to pan, so an embedded map doesn't swallow the page scroll. A blocked gesture shows a short hint instead.",
         },
         new()
         {
            Name = "CooperativeGesturesWheelHint",
            Type = "string",
            DefaultValue = "Use ctrl + scroll to zoom the map",
            Description = "Hint shown when a bare wheel gesture is blocked by CooperativeGestures.",
         },
         new()
         {
            Name = "CooperativeGesturesTouchHint",
            Type = "string",
            DefaultValue = "Use two fingers to move the map",
            Description = "Hint shown when a one-finger drag is blocked by CooperativeGestures.",
         },
         new()
         {
            Name = "EscapeToExit",
            Type = "bool",
            DefaultValue = "true",
            Description = "Moves focus out of the map canvas on Escape. A focused map consumes the arrow keys, so without a way out it is a keyboard trap (WCAG 2.1.2).",
         },
         new()
         {
            Name = "KeyboardInstructions",
            Type = "string",
            DefaultValue = "Use the arrow keys to pan the map, plus and minus to zoom, and Escape to leave the map.",
            Description = "Description of the keyboard model. Associated with the canvas via aria-describedby, and shown on screen while the map has keyboard focus.",
         },
         new()
         {
            Name = "AnnounceViewChanges",
            Type = "bool",
            DefaultValue = "false",
            Description = "Announces the new centre and zoom through a polite live region after a pan or zoom, throttled by ViewAnnouncementThrottle.",
         },
         new()
         {
            Name = "ViewAnnouncementFormatter",
            Type = "Func<BitMapViewState, string>?",
            DefaultValue = "null",
            Description = "Builds the announcement text. Supply one to announce a place name instead of coordinates - far more useful than a pair of decimals.",
         },
         new()
         {
            Name = "ViewAnnouncementThrottle",
            Type = "TimeSpan",
            DefaultValue = "00:00:02",
            Description = "Minimum interval between two view announcements, so a drag can't flood the screen reader.",
         },
         new()
         {
            Name = "RespectReducedMotion",
            Type = "bool",
            DefaultValue = "true",
            Description = "Turns FlyTo and an animated SetView into an instant jump when the visitor prefers reduced motion. Both methods take an essential argument to opt a specific move back into animating.",
         },
         new()
         {
            Name = "ShowLoading",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the built-in loading indicator while the provider's assets and map instance are being created.",
         },
         new()
         {
            Name = "LoadingTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces the built-in loading indicator.",
         },
         new()
         {
            Name = "ErrorTemplate",
            Type = "RenderFragment<Exception?>?",
            DefaultValue = "null",
            Description = "Replaces the built-in failure message. Receives the exception behind the failure, when one was captured.",
         },
         new()
         {
            Name = "UnsupportedTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces the built-in message shown when the browser cannot give a WebGL-backed provider a context.",
         },
         new()
         {
            Name = "LoadingLabel",
            Type = "string",
            DefaultValue = "Loading map…",
            Description = "Text of the built-in loading indicator.",
         },
         new()
         {
            Name = "ErrorLabel",
            Type = "string",
            DefaultValue = "The map could not be loaded.",
            Description = "Text of the built-in failure message.",
         },
         new()
         {
            Name = "UnsupportedLabel",
            Type = "string",
            DefaultValue = "This browser cannot display the map (WebGL is unavailable).",
            Description = "Text of the built-in unsupported-browser message.",
         },
         new()
         {
            Name = "OnLoadStateChanged",
            Type = "EventCallback<BitMapLoadState>",
            DefaultValue = "",
            Description = "Fires on every LoadState transition (Idle, Loading, Ready, Failed, Unsupported).",
         },
         new()
         {
            Name = "OnRenderContextLost",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Fires when the browser drops the WebGL context the map renders into. Browsers cap how many contexts may be live at once, and the oldest is dropped silently - without this the map just turns black.",
         },
         new()
         {
            Name = "OnRenderContextRestored",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Fires when a lost WebGL context is restored.",
         },
         new()
         {
            Name = "OnFullscreenChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Fires when the map enters or leaves fullscreen, including when the user leaves it with Escape or the browser's own control.",
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
            Name = "LoadState",
            Type = "BitMapLoadState",
            DefaultValue = "BitMapLoadState.Idle",
            Description = "Where the map is in its lifecycle: Idle, Loading, Ready, Failed, or Unsupported.",
         },
         new()
         {
            Name = "LoadError",
            Type = "Exception?",
            DefaultValue = "null",
            Description = "The exception behind a Failed LoadState, when one was captured.",
         },
         new()
         {
            Name = "MarkerIds",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "",
            Description = "Ids of the markers currently on the map, in insertion order. Read from the component's own snapshot, so it costs no interop round-trip.",
         },
         new()
         {
            Name = "OpenPopupMarker",
            Type = "BitMapMarker?",
            DefaultValue = "null",
            Description = "The marker whose MarkerPopupTemplate popup is open, or null when none is.",
         },
         new()
         {
            Name = "OpenPopup",
            Type = "Func<string, ValueTask>",
            DefaultValue = "",
            Description = "Opens the MarkerPopupTemplate popup for a marker. Does nothing when no template is set - use OpenMarkerPopup for the provider's own popup.",
         },
         new()
         {
            Name = "ClosePopup",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Closes the MarkerPopupTemplate popup, if one is open.",
         },
         new()
         {
            Name = "OrderedMarkers",
            Type = "IReadOnlyList<BitMapMarker>",
            DefaultValue = "",
            Description = "The markers currently on the map, in the order they were added. Costs no interop round-trip.",
         },
         new()
         {
            Name = "LayerIds",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "",
            Description = "Ids of the vector layers currently on the map, in insertion order. Costs no interop round-trip.",
         },
         new()
         {
            Name = "TileOverlayIds",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "",
            Description = "Ids of the tile overlays currently on the map, in insertion order. Costs no interop round-trip.",
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
            Type = "Func<BitMapLatLng, double?, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Pan and optionally zoom to the given center (center, zoom, animate, essential). Animation is skipped under a reduced-motion preference unless the move is marked essential.",
         },
         new()
         {
            Name = "FlyTo",
            Type = "Func<BitMapLatLng, double?, bool, ValueTask>",
            DefaultValue = "",
            Description = "Animated pan/zoom to the given center (center, zoom, essential). Becomes an instant jump under a reduced-motion preference unless marked essential.",
         },
         new()
         {
            Name = "SetZoom",
            Type = "Func<double, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Set an absolute zoom level, keeping the current centre.",
         },
         new()
         {
            Name = "ZoomIn",
            Type = "Func<double, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Zoom in by a number of levels (default 1) around the current centre.",
         },
         new()
         {
            Name = "ZoomOut",
            Type = "Func<double, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Zoom out by a number of levels (default 1) around the current centre.",
         },
         new()
         {
            Name = "ZoomBy",
            Type = "Func<double, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Change the zoom by a relative number of levels. Negative values zoom out.",
         },
         new()
         {
            Name = "PanBy",
            Type = "Func<double, double, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Pan by a pixel offset. This is what pan buttons are built from, and shipping those matters: WCAG 2.2 SC 2.5.7 requires a single-pointer alternative to dragging the map.",
         },
         new()
         {
            Name = "Locate",
            Type = "Func<BitMapGeolocationOptions?, ValueTask<BitMapGeolocationResult?>>",
            DefaultValue = "",
            Description = "Asks the browser for the visitor's position and, unless turned off, pans there. Returns null when the browser denies the permission prompt or times out.",
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
            Name = "IsFullscreen",
            Type = "bool",
            DefaultValue = "false",
            Description = "Whether the map is currently displayed fullscreen.",
         },
         new()
         {
            Name = "RequestFullscreen",
            Type = "Func<ValueTask<bool>>",
            DefaultValue = "",
            Description = "Takes the map's container fullscreen - not the page - so overlay content and custom controls go with it. Call it from a user gesture: browsers only honour the request inside the short activation window a click opens. Returns whether the browser granted it.",
         },
         new()
         {
            Name = "ExitFullscreen",
            Type = "Func<ValueTask<bool>>",
            DefaultValue = "",
            Description = "Leaves fullscreen, if this map is the element currently displayed fullscreen.",
         },
         new()
         {
            Name = "ToggleFullscreen",
            Type = "Func<ValueTask<bool>>",
            DefaultValue = "",
            Description = "Enters fullscreen, or leaves it if the map is already fullscreen.",
         },
         new()
         {
            Name = "SetLayerVisible",
            Type = "Func<string, bool, ValueTask>",
            DefaultValue = "",
            Description = "Shows or hides a vector layer while keeping its definition. Unlike a zero opacity, a hidden layer stops hit-testing rather than swallowing clicks meant for what is underneath it.",
         },
         new()
         {
            Name = "IsLayerVisible",
            Type = "Func<string, bool>",
            DefaultValue = "",
            Description = "Whether a vector layer is currently drawn. Unknown ids report false.",
         },
         new()
         {
            Name = "SetLayerStyle",
            Type = "Func<string, BitMapVectorPathStyle?, ValueTask>",
            DefaultValue = "",
            Description = "Restyles an existing vector layer, keeping its geometry. A hidden layer keeps the new style for when it is shown again.",
         },
         new()
         {
            Name = "SetTileOverlayVisible",
            Type = "Func<string, bool, ValueTask>",
            DefaultValue = "",
            Description = "Shows or hides a tile overlay while keeping its definition.",
         },
         new()
         {
            Name = "IsTileOverlayVisible",
            Type = "Func<string, bool>",
            DefaultValue = "",
            Description = "Whether a tile overlay is currently drawn. Unknown ids report false.",
         },
         new()
         {
            Name = "SetTileOverlayOpacity",
            Type = "Func<string, double, ValueTask>",
            DefaultValue = "",
            Description = "Changes a tile overlay's opacity, which is how a raster overlay is blended against the basemap underneath it.",
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
    private Task OnMapContextMenu(BitMapLatLng p) { eventsLog = $"Right-click → {p.Latitude:F5}, {p.Longitude:F5}"; return Task.CompletedTask; }
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

    // ── Example 8 – Navigation ────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> navMapRef = default!;
    private readonly BitLeafletMapProvider navProvider = new() { Center = new(41.9028, 12.4964), Zoom = 5 };
    private string navLog = "Use the buttons to drive the camera from code.";

    private async Task NavZoomIn() => await navMapRef.ZoomIn();

    private async Task NavZoomOut() => await navMapRef.ZoomOut();

    private async Task NavSetZoom() => await navMapRef.SetZoom(6);

    private async Task NavPan(double dx, double dy)
    {
        await navMapRef.PanBy(dx, dy);
        navLog = $"Panned by {dx}, {dy} pixels.";
    }

    private async Task FlyToRome()
    {
        await navMapRef.FlyTo(new(41.9028, 12.4964), 11);
        navLog = "Flew to Rome. Under a reduced-motion preference this jumps instead of animating.";
    }

    private async Task LocateMe()
    {
        var result = await navMapRef.Locate(new() { Zoom = 13 });
        navLog = result is null
            ? "Location unavailable - the browser denied the request or timed out."
            : $"Located at {result.Position.Latitude:F4}, {result.Position.Longitude:F4} (±{result.AccuracyMeters:F0} m)";
    }

    // ── Example 9 – Accessibility ─────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> a11yMapRef = default!;
    private bool a11yCooperative = true;
    private bool a11yAnnounce = true;
    private bool a11yShowList = true;
    private readonly BitLeafletMapProvider a11yProvider = new() { Center = new(52.5200, 13.4050), Zoom = 12 };

    // Every marker carries an Alt: it is the accessible name, and it is what the marker table
    // lists. Without one both would fall back to the id.
    private readonly List<BitMapMarker> a11yMarkers =
    [
        new() { Id = "gate", Position = new(52.5163, 13.3777), Alt = "Brandenburg Gate", PopupText = "Brandenburg Gate" },
        new() { Id = "island", Position = new(52.5169, 13.4019), Alt = "Museum Island", PopupText = "Museum Island" },
        new() { Id = "tower", Position = new(52.5208, 13.4094), Alt = "TV Tower", PopupText = "TV Tower" },
    ];

    private async Task OnAccessibilityReady()
    {
        foreach (var marker in a11yMarkers)
        {
            await a11yMapRef.AddMarker(marker);
        }
        await a11yMapRef.FitBoundsToMarkers();
    }

    // Announcing a place beats announcing coordinates: "zoom level 12" tells a screen-reader
    // user nothing about where they are. A real app would reverse-geocode here.
    private string FormatAnnouncement(BitMapViewState view)
        => $"Map showing Berlin at zoom {view.Zoom:F0}, centred near {view.Center.Latitude:F2}, {view.Center.Longitude:F2}.";

    // ── Example 10 – Loading and lifecycle ────────────────────────────────────

    private BitMapLoadState lifecycleState = BitMapLoadState.Idle;
    private readonly BitLeafletMapProvider lifecycleProvider = new() { Center = new(59.9139, 10.7522), Zoom = 10 };

    // ── Example 11 – Data binding ─────────────────────────────────────────────

    private BitMapLatLng? boundCenter = new(41.9028, 12.4964);
    private double? boundZoom = 5;
    private List<BitMapMarker> boundMarkers =
    [
        new() { Id = "rome", Position = new(41.9028, 12.4964), Alt = "Rome", PopupText = "Rome" },
        new() { Id = "paris", Position = new(48.8566, 2.3522), Alt = "Paris", PopupText = "Paris" },
    ];
    private int boundMarkerCounter;

    private void AddBoundMarker()
    {
        boundMarkerCounter++;
        var id = $"m{boundMarkerCounter}";
        // A new list instance: the parameter is compared by reference, and the markers inside it
        // by value, so only this one addition reaches the map.
        boundMarkers = [.. boundMarkers, new BitMapMarker
        {
            Id = id,
            Position = new(boundCenter?.Latitude ?? 0, boundCenter?.Longitude ?? 0),
            Alt = $"Marker {id}",
            PopupText = $"Marker {id}",
        }];
    }

    private void MoveFirstBoundMarker()
    {
        if (boundMarkers.Count == 0) return;
        var first = boundMarkers[0];
        boundMarkers = [first with { Position = new(first.Position.Latitude + 1, first.Position.Longitude + 1) },
                        .. boundMarkers.Skip(1)];
    }

    // ── Example 12 – Clustering ───────────────────────────────────────────────

    private readonly BitLeafletMapProvider clusterProvider = new() { Center = new(51.5074, -0.1278), Zoom = 5 };
    private bool clusterEnabled = true;
    private BitMapClustering? clusterOptions = new() { RadiusPixels = 60, MaxZoom = 14 };
    private List<BitMapMarker> clusterMarkers = BuildScatteredMarkers();
    private string clusterLog = "Click a bubble to zoom into it, or a single pin for its own click event.";

    private void ToggleClustering(bool enabled)
    {
        clusterEnabled = enabled;
        // Null turns clustering off and hands every marker straight back to the provider.
        clusterOptions = enabled ? new BitMapClustering { RadiusPixels = 60, MaxZoom = 14 } : null;
    }

    private void RegenerateClusterMarkers()
    {
        clusterMarkers = BuildScatteredMarkers();
        clusterLog = $"Scattered {clusterMarkers.Count} markers again.";
    }

    /// <summary>Scatters markers around a handful of European cities so the clusters are uneven.</summary>
    private static List<BitMapMarker> BuildScatteredMarkers()
    {
        (string Name, double Lat, double Lng, int Count)[] cities =
        [
            ("London", 51.5074, -0.1278, 160),
            ("Paris", 48.8566, 2.3522, 120),
            ("Berlin", 52.5200, 13.4050, 90),
            ("Madrid", 40.4168, -3.7038, 70),
            ("Rome", 41.9028, 12.4964, 60),
        ];

        var markers = new List<BitMapMarker>();
        foreach (var (name, lat, lng, count) in cities)
        {
            for (var i = 0; i < count; i++)
            {
                markers.Add(new BitMapMarker
                {
                    Id = $"{name}-{i}",
                    Position = new(
                        Math.Clamp(lat + (Random.Shared.NextDouble() - 0.5) * 2.5, -85, 85),
                        Math.Clamp(lng + (Random.Shared.NextDouble() - 0.5) * 2.5, -180, 180)),
                    Alt = $"{name} location {i + 1}",
                    PopupText = $"{name} #{i + 1}",
                });
            }
        }
        return markers;
    }

    private Task OnClusterClick(BitMapClusterClickArgs e)
    {
        clusterLog = $"Cluster of {e.Count} markers - zoomed to fit them.";
        return Task.CompletedTask;
    }

    private Task OnClusterMarkerClick(string id)
    {
        clusterLog = $"Marker click: {id}";
        return Task.CompletedTask;
    }

    // ── Example 13 – Layer control ────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> layersMapRef = default!;
    private readonly BitLeafletMapProvider layersProvider = new() { Center = new(51.5074, -0.1278), Zoom = 11 };
    private bool layerVisible = true;
    private bool overlayVisible = true;
    private int layerStyleIndex;
    private string layersLog = "Toggle the area and the overlay, or restyle the area in place.";

    private static readonly BitMapVectorPathStyle[] LayerStyles =
    [
        new() { Color = "#3388ff", FillColor = "#3388ff", FillOpacity = 0.2, Weight = 3 },
        new() { Color = "#e53935", FillColor = "#e53935", FillOpacity = 0.3, Weight = 5, DashArray = "6 4" },
        new() { Color = "#2e7d32", Fill = false, Weight = 4 },
    ];

    private async Task OnLayersReady()
    {
        await layersMapRef.AddCircle("area", new(51.5074, -0.1278), 6000, LayerStyles[0]);
        await layersMapRef.AddTileOverlay(new BitMapTileOverlay
        {
            Id = "labels",
            UrlTemplate = "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
            Attribution = "&copy; OpenStreetMap contributors",
            Opacity = 0.35,
            MinZoom = 3,
            MaxZoom = 19,
            Subdomains = "abc",
        });
    }

    private async Task ToggleLayerVisibility()
    {
        layerVisible = !layerVisible;
        await layersMapRef.SetLayerVisible("area", layerVisible);
        layersLog = layerVisible
            ? "The area is back - its definition was kept while it was hidden."
            : "The area is hidden. It no longer draws, and no longer swallows clicks.";
    }

    private async Task CycleLayerStyle()
    {
        layerStyleIndex = (layerStyleIndex + 1) % LayerStyles.Length;
        await layersMapRef.SetLayerStyle("area", LayerStyles[layerStyleIndex]);
        layersLog = $"Restyled the area (style {layerStyleIndex + 1} of {LayerStyles.Length}) without restating its geometry.";
    }

    private async Task ToggleOverlayVisibility()
    {
        overlayVisible = !overlayVisible;
        await layersMapRef.SetTileOverlayVisible("labels", overlayVisible);
        layersLog = overlayVisible ? "Overlay shown." : "Overlay hidden.";
    }

    private async Task DimOverlay()
    {
        await layersMapRef.SetTileOverlayOpacity("labels", 0.15);
        layersLog = "Overlay dimmed to 0.15 opacity.";
    }

    private async Task GoFullscreen()
    {
        // Must run from a real click: the browser only honours a fullscreen request inside the
        // short activation window a gesture opens.
        if (await layersMapRef.RequestFullscreen() is false)
        {
            layersLog = "The browser refused the fullscreen request.";
        }
    }

    private Task OnLayersFullscreenChanged(bool isFullscreen)
    {
        layersLog = isFullscreen ? "Fullscreen - press Escape to leave." : "Left fullscreen.";
        return Task.CompletedTask;
    }

    // ── Example 14 – Popup content ────────────────────────────────────────────

    private readonly BitLeafletMapProvider popupProvider = new() { Center = new(48.2082, 16.3738), Zoom = 12 };
    private readonly List<BitMapMarker> popupMarkers =
    [
        new() { Id = "opera", Position = new(48.2029, 16.3690), Alt = "Vienna State Opera" },
        new() { Id = "prater", Position = new(48.2166, 16.3960), Alt = "Prater" },
        new() { Id = "belvedere", Position = new(48.1915, 16.3809), Alt = "Belvedere" },
    ];
    private readonly Dictionary<string, int> popupVisits = [];
    private string popupLog = "Click a marker, then use the button inside its popup.";

    private int GetVisitorCount(string markerId) => popupVisits.GetValueOrDefault(markerId);

    // An event handler inside the popup - the thing an HTML-string popup cannot do at all.
    private void RecordVisit(string markerId)
    {
        popupVisits[markerId] = GetVisitorCount(markerId) + 1;
        popupLog = $"Recorded a visit to {markerId}. The popup re-rendered in place.";
    }

    private Task OnPopupOpened(BitMapMarker marker)
    {
        popupLog = $"Popup opened for {marker.Alt}.";
        return Task.CompletedTask;
    }

    // ── Example 21 – Style & Class ────────────────────────────────────────────

    private readonly BitLeafletMapProvider styleProvider = new() { Center = new(45.4642, 9.1900), Zoom = 11 };
    private readonly BitLeafletMapProvider classProvider = new() { Center = new(41.3874, 2.1686), Zoom = 11 };

    // ── Example 22 – RTL ──────────────────────────────────────────────────────

    private readonly BitLeafletMapProvider rtlProvider = new() { Center = new(35.6892, 51.3890), Zoom = 11 };

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
            OnContextMenu=""OnMapContextMenu""
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

// The browser menu still opens unless the provider sets SuppressBrowserContextMenu.
private Task OnMapContextMenu(BitMapLatLng p)
{
    eventsLog = $""Right-click → {p.Latitude:F5}, {p.Longitude:F5}"";
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
    <BitMap TMapProvider=""BitLeafletMapProvider"" @ref=""navMapRef"" Provider=""@navProvider"" />
</div>
<div style=""display:flex;gap:0.5rem;flex-wrap:wrap;align-items:center"">
    <BitButton OnClick=""NavZoomIn"">Zoom in</BitButton>
    <BitButton OnClick=""NavZoomOut"" Variant=""BitVariant.Outline"">Zoom out</BitButton>
    <BitButton OnClick=""NavSetZoom"" Variant=""BitVariant.Outline"">Zoom 6</BitButton>
    <BitButton OnClick=""() => NavPan(-120, 0)"" Variant=""BitVariant.Outline"" AriaLabel=""Pan west"">West</BitButton>
    <BitButton OnClick=""() => NavPan(120, 0)"" Variant=""BitVariant.Outline"" AriaLabel=""Pan east"">East</BitButton>
    <BitButton OnClick=""() => NavPan(0, -120)"" Variant=""BitVariant.Outline"" AriaLabel=""Pan north"">North</BitButton>
    <BitButton OnClick=""() => NavPan(0, 120)"" Variant=""BitVariant.Outline"" AriaLabel=""Pan south"">South</BitButton>
    <BitButton OnClick=""FlyToRome"">Fly to Rome</BitButton>
    <BitButton OnClick=""LocateMe"" Variant=""BitVariant.Outline"">Locate me</BitButton>
</div>
<pre>@navLog</pre>";
    private readonly string example8CsharpCode = @"
private BitMap<BitLeafletMapProvider> navMapRef = default!;
private readonly BitLeafletMapProvider navProvider = new() { Center = new(41.9028, 12.4964), Zoom = 5 };
private string navLog = ""Use the buttons to drive the camera from code."";

private async Task NavZoomIn() => await navMapRef.ZoomIn();

private async Task NavZoomOut() => await navMapRef.ZoomOut();

private async Task NavSetZoom() => await navMapRef.SetZoom(6);

// Pixel-space panning. Wire it to four arrow buttons and the map stops depending on
// dragging, which is what WCAG 2.2 SC 2.5.7 asks for.
private async Task NavPan(double dx, double dy)
{
    await navMapRef.PanBy(dx, dy);
    navLog = $""Panned by {dx}, {dy} pixels."";
}

private async Task FlyToRome()
{
    // Animated by default; becomes an instant jump when the visitor prefers reduced motion.
    // Pass essential: true to keep the animation regardless.
    await navMapRef.FlyTo(new(41.9028, 12.4964), 11);
    navLog = ""Flew to Rome."";
}

private async Task LocateMe()
{
    // Returns null when the browser denies the permission prompt or times out.
    var result = await navMapRef.Locate(new() { Zoom = 13 });
    navLog = result is null
        ? ""Location unavailable.""
        : $""Located at {result.Position.Latitude:F4}, {result.Position.Longitude:F4} (±{result.AccuracyMeters:F0} m)"";
}";

    private readonly string example9RazorCode = @"
<div style=""display:flex;gap:1rem;flex-wrap:wrap"">
    <BitToggle @bind-Value=""a11yCooperative"" Text=""Cooperative gestures"" />
    <BitToggle @bind-Value=""a11yAnnounce"" Text=""Announce view changes"" />
    <BitToggle @bind-Value=""a11yShowList"" Text=""Show the marker table"" />
</div>
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""a11yMapRef""
            Provider=""@a11yProvider""
            AriaLabel=""Map of central Berlin""
            KeyboardInstructions=""Arrow keys pan the map, plus and minus zoom, Escape leaves the map.""
            CooperativeGestures=""@a11yCooperative""
            AnnounceViewChanges=""@a11yAnnounce""
            ViewAnnouncementFormatter=""@FormatAnnouncement""
            MarkerListMode=""@(a11yShowList ? BitMapMarkerListMode.Visible : BitMapMarkerListMode.ScreenReaderOnly)""
            MarkerListCaption=""Berlin landmarks""
            OnReady=""OnAccessibilityReady"" />
</div>";
    private readonly string example9CsharpCode = @"
private BitMap<BitLeafletMapProvider> a11yMapRef = default!;
private bool a11yCooperative = true;
private bool a11yAnnounce = true;
private bool a11yShowList = true;
private readonly BitLeafletMapProvider a11yProvider = new() { Center = new(52.5200, 13.4050), Zoom = 12 };

// Every marker carries an Alt: it is the accessible name, and it is what the marker table
// lists. Without one both would fall back to the id.
private readonly List<BitMapMarker> a11yMarkers =
[
    new() { Id = ""gate"", Position = new(52.5163, 13.3777), Alt = ""Brandenburg Gate"", PopupText = ""Brandenburg Gate"" },
    new() { Id = ""island"", Position = new(52.5169, 13.4019), Alt = ""Museum Island"", PopupText = ""Museum Island"" },
    new() { Id = ""tower"", Position = new(52.5208, 13.4094), Alt = ""TV Tower"", PopupText = ""TV Tower"" },
];

private async Task OnAccessibilityReady()
{
    foreach (var marker in a11yMarkers)
    {
        await a11yMapRef.AddMarker(marker);
    }
    await a11yMapRef.FitBoundsToMarkers();
}

// Announcing a place beats announcing coordinates: ""zoom level 12"" tells a screen-reader
// user nothing about where they are. A real app would reverse-geocode here.
private string FormatAnnouncement(BitMapViewState view)
    => $""Map showing Berlin at zoom {view.Zoom:F0}, centred near {view.Center.Latitude:F2}, {view.Center.Longitude:F2}."";";

    private readonly string example10RazorCode = @"
<div>State: <b>@lifecycleState</b></div>
<div style=""height:320px;resize:both;overflow:auto"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@lifecycleProvider""
            LazyLoad
            LoadingLabel=""Fetching the basemap…""
            OnLoadStateChanged=""@(s => { lifecycleState = s; StateHasChanged(); })"" />
</div>";
    private readonly string example10CsharpCode = @"
private BitMapLoadState lifecycleState = BitMapLoadState.Idle;
private readonly BitLeafletMapProvider lifecycleProvider = new() { Center = new(59.9139, 10.7522), Zoom = 10 };";

    private readonly string example11RazorCode = @"
<div style=""display:flex;gap:0.5rem;flex-wrap:wrap;align-items:center"">
    <BitButton OnClick=""() => boundCenter = new(41.9028, 12.4964)"">Rome</BitButton>
    <BitButton OnClick=""() => boundCenter = new(48.8566, 2.3522)"" Variant=""BitVariant.Outline"">Paris</BitButton>
    <BitButton OnClick=""AddBoundMarker"" Variant=""BitVariant.Outline"">Add a marker</BitButton>
    <BitButton OnClick=""MoveFirstBoundMarker"" Variant=""BitVariant.Outline"">Move the first marker</BitButton>
</div>
<div style=""height:380px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @bind-Center=""boundCenter""
            @bind-Zoom=""boundZoom""
            Markers=""boundMarkers"" />
</div>
<div>
    Centre @boundCenter?.Latitude.ToString(""F4""), @boundCenter?.Longitude.ToString(""F4"") —
    zoom @boundZoom?.ToString(""F1"") — @boundMarkers.Count marker(s)
</div>";
    private readonly string example11CsharpCode = @"
private BitMapLatLng? boundCenter = new(41.9028, 12.4964);
private double? boundZoom = 5;
private List<BitMapMarker> boundMarkers =
[
    new() { Id = ""rome"", Position = new(41.9028, 12.4964), Alt = ""Rome"", PopupText = ""Rome"" },
    new() { Id = ""paris"", Position = new(48.8566, 2.3522), Alt = ""Paris"", PopupText = ""Paris"" },
];
private int boundMarkerCounter;

private void AddBoundMarker()
{
    boundMarkerCounter++;
    var id = $""m{boundMarkerCounter}"";
    // A new list instance: the parameter is compared by reference, and the markers inside it
    // by value, so only this one addition reaches the map.
    boundMarkers = [.. boundMarkers, new BitMapMarker
    {
        Id = id,
        Position = new(boundCenter?.Latitude ?? 0, boundCenter?.Longitude ?? 0),
        Alt = $""Marker {id}"",
        PopupText = $""Marker {id}"",
    }];
}

private void MoveFirstBoundMarker()
{
    if (boundMarkers.Count == 0) return;
    // BitMapMarker is a record, so `with` produces a changed copy - and the component sees
    // exactly one marker differ rather than a whole new collection.
    var first = boundMarkers[0];
    boundMarkers = [first with { Position = new(first.Position.Latitude + 1, first.Position.Longitude + 1) },
                    .. boundMarkers.Skip(1)];
}";

    private readonly string example12RazorCode = @"
<div style=""display:flex;gap:1rem;flex-wrap:wrap;align-items:center"">
    <BitToggle Value=""clusterEnabled"" ValueChanged=""ToggleClustering"" Text=""Cluster markers"" />
    <BitButton OnClick=""RegenerateClusterMarkers"" Variant=""BitVariant.Outline"">Scatter again</BitButton>
</div>
<div style=""height:420px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@clusterProvider""
            Markers=""clusterMarkers""
            Clustering=""@clusterOptions""
            OnClusterClick=""OnClusterClick""
            OnMarkerClick=""OnClusterMarkerClick"" />
</div>
<pre>@clusterLog</pre>";
    private readonly string example12CsharpCode = @"
private readonly BitLeafletMapProvider clusterProvider = new() { Center = new(51.5074, -0.1278), Zoom = 5 };
private bool clusterEnabled = true;
private BitMapClustering? clusterOptions = new() { RadiusPixels = 60, MaxZoom = 14 };
private List<BitMapMarker> clusterMarkers = BuildScatteredMarkers();
private string clusterLog = ""Click a bubble to zoom into it."";

private void ToggleClustering(bool enabled)
{
    clusterEnabled = enabled;
    // Null turns clustering off and hands every marker straight back to the provider.
    clusterOptions = enabled ? new BitMapClustering { RadiusPixels = 60, MaxZoom = 14 } : null;
}

private void RegenerateClusterMarkers()
{
    clusterMarkers = BuildScatteredMarkers();
}

// Scatters markers around a handful of European cities so the clusters are uneven.
private static List<BitMapMarker> BuildScatteredMarkers()
{
    (string Name, double Lat, double Lng, int Count)[] cities =
    [
        (""London"", 51.5074, -0.1278, 160),
        (""Paris"", 48.8566, 2.3522, 120),
        (""Berlin"", 52.5200, 13.4050, 90),
        (""Madrid"", 40.4168, -3.7038, 70),
        (""Rome"", 41.9028, 12.4964, 60),
    ];

    var markers = new List<BitMapMarker>();
    foreach (var (name, lat, lng, count) in cities)
    {
        for (var i = 0; i < count; i++)
        {
            markers.Add(new BitMapMarker
            {
                Id = $""{name}-{i}"",
                Position = new(
                    Math.Clamp(lat + (Random.Shared.NextDouble() - 0.5) * 2.5, -85, 85),
                    Math.Clamp(lng + (Random.Shared.NextDouble() - 0.5) * 2.5, -180, 180)),
                Alt = $""{name} location {i + 1}"",
                PopupText = $""{name} #{i + 1}"",
            });
        }
    }
    return markers;
}

// A bubble is not one of your markers, so it reports here rather than through OnMarkerClick.
private Task OnClusterClick(BitMapClusterClickArgs e)
{
    clusterLog = $""Cluster of {e.Count} markers - zoomed to fit them."";
    return Task.CompletedTask;
}

private Task OnClusterMarkerClick(string id)
{
    clusterLog = $""Marker click: {id}"";
    return Task.CompletedTask;
}";

    private readonly string example13RazorCode = @"
<div style=""display:flex;gap:0.5rem;flex-wrap:wrap;align-items:center"">
    <BitButton OnClick=""ToggleLayerVisibility"" Variant=""BitVariant.Outline"">
        @(layerVisible ? ""Hide the area"" : ""Show the area"")
    </BitButton>
    <BitButton OnClick=""CycleLayerStyle"" Variant=""BitVariant.Outline"">Restyle the area</BitButton>
    <BitButton OnClick=""ToggleOverlayVisibility"" Variant=""BitVariant.Outline"">
        @(overlayVisible ? ""Hide the overlay"" : ""Show the overlay"")
    </BitButton>
    <BitButton OnClick=""DimOverlay"" Variant=""BitVariant.Outline"">Dim the overlay</BitButton>
    <BitButton OnClick=""GoFullscreen"">Fullscreen</BitButton>
</div>
<div style=""height:400px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""layersMapRef""
            Provider=""@layersProvider""
            OnReady=""OnLayersReady""
            OnFullscreenChanged=""OnLayersFullscreenChanged"" />
</div>
<pre>@layersLog</pre>";
    private readonly string example13CsharpCode = @"
private BitMap<BitLeafletMapProvider> layersMapRef = default!;
private readonly BitLeafletMapProvider layersProvider = new() { Center = new(51.5074, -0.1278), Zoom = 11 };
private bool layerVisible = true;
private bool overlayVisible = true;
private int layerStyleIndex;

private static readonly BitMapVectorPathStyle[] LayerStyles =
[
    new() { Color = ""#3388ff"", FillColor = ""#3388ff"", FillOpacity = 0.2, Weight = 3 },
    new() { Color = ""#e53935"", FillColor = ""#e53935"", FillOpacity = 0.3, Weight = 5, DashArray = ""6 4"" },
    new() { Color = ""#2e7d32"", Fill = false, Weight = 4 },
];

private async Task OnLayersReady()
{
    await layersMapRef.AddCircle(""area"", new(51.5074, -0.1278), 6000, LayerStyles[0]);
    await layersMapRef.AddTileOverlay(new BitMapTileOverlay
    {
        Id = ""labels"",
        UrlTemplate = ""https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"",
        Attribution = ""&copy; OpenStreetMap contributors"",
        Opacity = 0.35,
        MinZoom = 3,
        MaxZoom = 19,
        Subdomains = ""abc"",
    });
}

// Hiding keeps the definition, so showing again needs no re-declaration.
private async Task ToggleLayerVisibility()
{
    layerVisible = !layerVisible;
    await layersMapRef.SetLayerVisible(""area"", layerVisible);
}

// Restyles in place - the geometry is not restated.
private async Task CycleLayerStyle()
{
    layerStyleIndex = (layerStyleIndex + 1) % LayerStyles.Length;
    await layersMapRef.SetLayerStyle(""area"", LayerStyles[layerStyleIndex]);
}

private async Task ToggleOverlayVisibility()
{
    overlayVisible = !overlayVisible;
    await layersMapRef.SetTileOverlayVisible(""labels"", overlayVisible);
}

private async Task DimOverlay() => await layersMapRef.SetTileOverlayOpacity(""labels"", 0.15);

private async Task GoFullscreen()
{
    // Must run from a real click: the browser only honours a fullscreen request inside the
    // short activation window a gesture opens.
    await layersMapRef.RequestFullscreen();
}

private Task OnLayersFullscreenChanged(bool isFullscreen)
{
    // Also fires when the user leaves fullscreen with Escape.
    return Task.CompletedTask;
}";

    private readonly string example14RazorCode = @"
<div style=""height:400px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@popupProvider""
            Markers=""popupMarkers""
            OnPopupOpened=""OnPopupOpened"">
        <MarkerPopupTemplate Context=""marker"">
            <div style=""display:flex;flex-flow:column;gap:0.5rem"">
                <b>@marker.Alt</b>
                <div>Visitors this month: <b>@GetVisitorCount(marker.Id)</b></div>
                <BitButton Size=""BitSize.Small"" OnClick=""() => RecordVisit(marker.Id)"">Record a visit</BitButton>
            </div>
        </MarkerPopupTemplate>
    </BitMap>
</div>
<pre>@popupLog</pre>";
    private readonly string example14CsharpCode = @"
private readonly BitLeafletMapProvider popupProvider = new() { Center = new(48.2082, 16.3738), Zoom = 12 };
private readonly List<BitMapMarker> popupMarkers =
[
    new() { Id = ""opera"", Position = new(48.2029, 16.3690), Alt = ""Vienna State Opera"" },
    new() { Id = ""prater"", Position = new(48.2166, 16.3960), Alt = ""Prater"" },
    new() { Id = ""belvedere"", Position = new(48.1915, 16.3809), Alt = ""Belvedere"" },
];
private readonly Dictionary<string, int> popupVisits = [];

private int GetVisitorCount(string markerId) => popupVisits.GetValueOrDefault(markerId);

// An event handler inside the popup - the thing an HTML-string popup cannot do at all.
private void RecordVisit(string markerId)
{
    popupVisits[markerId] = GetVisitorCount(markerId) + 1;
}

private Task OnPopupOpened(BitMapMarker marker)
{
    popupLog = $""Popup opened for {marker.Alt}."";
    return Task.CompletedTask;
}";

    private readonly string example15RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitMapLibreMapProvider"" Provider=""@maplibreProvider"" />
</div>";
    private readonly string example15CsharpCode = @"
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitMapLibreMapProvider maplibreProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };";

    private readonly string example16RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitOpenLayersMapProvider"" Provider=""@olProvider"" />
</div>";
    private readonly string example16CsharpCode = @"
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitOpenLayersMapProvider olProvider = new() { Center = new(35.6762, 139.6503), Zoom = 4 };";

    private readonly string example17RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitMapboxMapProvider"" Provider=""@mapboxProvider"" />
</div>";
    private readonly string example17CsharpCode = @"
// Get your token from https://account.mapbox.com/access-tokens/
// and pass it via the AccessToken property on BitMapboxMapProvider.
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitMapboxMapProvider mapboxProvider = new()
{
    AccessToken = ""YOUR_MAPBOX_TOKEN"",
    Center = new(40, 0),
    Zoom = 2,
};";

    private readonly string example18RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitArcGisMapProvider"" Provider=""@arcGisProvider"" />
</div>";
    private readonly string example18CsharpCode = @"
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitArcGisMapProvider arcGisProvider = new() { Center = new(40, 0), Zoom = 2, BasemapId = ""osm"" };";

    private readonly string example19RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitAzureMapsMapProvider"" Provider=""@azureMapsProvider"" />
</div>";
    private readonly string example19CsharpCode = @"
// Get your key from Azure Portal > Maps account > Authentication > Shared Key
// and pass it via the SubscriptionKey property on BitAzureMapsMapProvider.
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitAzureMapsMapProvider azureMapsProvider = new()
{
    SubscriptionKey = ""YOUR_AZURE_MAPS_KEY"",
    Center = new(40, 0),
    Zoom = 2,
};";

    private readonly string example20RazorCode = @"
<div style=""height:420px"">
    <BitMap TMapProvider=""BitCesiumMapProvider"" Provider=""@cesiumProvider"" />
</div>";
    private readonly string example20CsharpCode = @"
// Bind a stable field so the provider isn't reallocated on every render.
private readonly BitCesiumMapProvider cesiumProvider = new() { Center = new(20, 0), Zoom = 2, SceneMode = ""scene3d"" };";

    private const string example21ScssCode = @"::deep {
    .custom-map {
        height: 260px;
        border: 2px dashed tomato;
        border-radius: 0.5rem;
        filter: saturate(0.4);
    }
}";
    private readonly string example21RazorCode = @"
<div style=""display:flex;gap:1rem;flex-wrap:wrap"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@styleProvider""
            Style=""height:260px;flex:1 1 260px;border-radius:1rem;box-shadow:0 0 1rem #6664"" />
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@classProvider""
            Class=""custom-map""
            Style=""flex:1 1 260px"" />
</div>";
    private readonly string example21CsharpCode = @"
private readonly BitLeafletMapProvider styleProvider = new() { Center = new(45.4642, 9.1900), Zoom = 11 };
private readonly BitLeafletMapProvider classProvider = new() { Center = new(41.3874, 2.1686), Zoom = 11 };";
    private readonly DemoCodeFile[] example21CodeFiles =
    [
        new("BitMapDemo.razor.scss", example21ScssCode),
    ];

    private readonly string example22RazorCode = @"
<div style=""height:320px"">
    <BitMap TMapProvider=""BitLeafletMapProvider"" Dir=""BitDir.Rtl"" Provider=""@rtlProvider"" AriaLabel=""نقشه تهران"">
        <div style=""position:absolute;inset-inline-start:0.75rem;inset-block-start:0.75rem;padding:0.5rem 0.75rem"">
            تهران
        </div>
    </BitMap>
</div>";
    private readonly string example22CsharpCode = @"
private readonly BitLeafletMapProvider rtlProvider = new() { Center = new(35.6892, 51.3890), Zoom = 11 };";
}
