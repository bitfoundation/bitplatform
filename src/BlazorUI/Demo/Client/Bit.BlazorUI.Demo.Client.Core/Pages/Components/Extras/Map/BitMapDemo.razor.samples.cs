namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Map;

public partial class BitMapDemo
{
    private readonly string example1RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider"" />
</div>";

    private readonly string example2RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""markersMapRef""
            Provider=""@markersProvider""
            OnReady=""OnMarkersReady""
            OnMarkerClick=""OnMarkerClick""
            OnMarkerDragEnd=""OnMarkerDragEnd"" />
</div>

<BitButton OnClick=""AddRandomMarker"">Add a marker</BitButton>
<BitButton OnClick=""OpenLondonPopup"" Variant=""BitVariant.Outline"">Open London's popup</BitButton>
<BitButton OnClick=""ClosePopup"" Variant=""BitVariant.Outline"">Close the popup</BitButton>
<BitButton OnClick=""FitToMarkers"" Variant=""BitVariant.Outline"">Fit to markers</BitButton>
<BitButton OnClick=""ClearMarkers"" Variant=""BitVariant.Outline"">Clear</BitButton>

<pre>@markersLog</pre>";
    private readonly string example2CsharpCode = @"
private BitMap<BitLeafletMapProvider> markersMapRef = default!;
private readonly BitLeafletMapProvider markersProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
private string markersLog = ""Click, hover or drag a marker."";
private int markerCounter;

private async Task OnMarkersReady()
{
    await markersMapRef.SyncMarkers(
    [
        new() { Id = ""paris"", Position = new(48.8566, 2.3522), Alt = ""Paris"", PopupText = ""Paris - click a marker to open its popup."", TooltipText = ""Paris"" },
        new() { Id = ""london"", Position = new(51.5074, -0.1278), Alt = ""London"", PopupText = ""London - drag me."", TooltipText = ""Drag me"", Draggable = true },
    ]);
    await markersMapRef.FitBoundsToMarkers();
}

private async Task AddRandomMarker()
{
    // Somewhere inside the current view, so the new marker is always visible.
    var view = await markersMapRef.GetView();
    var bounds = view.Bounds;
    var lat = bounds.SouthWest.Latitude + (0.1 + Random.Shared.NextDouble() * 0.8) * bounds.LatitudeSpan;
    var lng = bounds.SouthWest.Longitude + (0.1 + Random.Shared.NextDouble() * 0.8) * bounds.LongitudeSpan;
    var position = new BitMapLatLng(lat, lng > 180 ? lng - 360 : lng); // a view across the antimeridian

    var id = $""marker-{++markerCounter}"";
    await markersMapRef.AddMarker(new BitMapMarker
    {
        Id = id,
        Position = position,
        Alt = $""Marker {markerCounter}"",
        PopupText = $""Marker {markerCounter} at {position.Latitude:F4}, {position.Longitude:F4}"",
        TooltipText = $""Marker {markerCounter}"",
    });
    markersLog = $""Added {id}."";
}

private async Task OpenLondonPopup() => await markersMapRef.OpenMarkerPopup(""london"");

private async Task ClosePopup() => markersLog = await markersMapRef.CloseMarkerPopup() ? ""Popup closed."" : ""No popup was open."";

private async Task FitToMarkers() => await markersMapRef.FitBoundsToMarkers();

private async Task ClearMarkers()
{
    await markersMapRef.ClearMarkers();
    markersLog = ""All markers removed."";
}

private void OnMarkerClick(string id) => markersLog = $""Clicked {id}."";

private void OnMarkerDragEnd(BitMapMarkerDragEndArgs e) => markersLog = $""Dropped {e.Id} at {e.Position.Latitude:F4}, {e.Position.Longitude:F4}."";";

    private readonly string example3RazorCode = @"
<BitButton OnClick=""() => boundCenter = new(41.9028, 12.4964)"">Rome</BitButton>
<BitButton OnClick=""() => boundCenter = new(48.8566, 2.3522)"" Variant=""BitVariant.Outline"">Paris</BitButton>
<BitButton OnClick=""AddBoundMarker"" Variant=""BitVariant.Outline"">Add a marker</BitButton>
<BitButton OnClick=""MoveFirstBoundMarker"" Variant=""BitVariant.Outline"">Move the first marker</BitButton>

<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @bind-Center=""boundCenter""
            @bind-Zoom=""boundZoom""
            Markers=""boundMarkers"" />
</div>

<pre>Centre @boundCenter?.Latitude.ToString(""F4""), @boundCenter?.Longitude.ToString(""F4"") - zoom @boundZoom?.ToString(""F1"") - @boundMarkers.Count marker(s)</pre>";
    private readonly string example3CsharpCode = @"
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
    var id = $""marker-{++boundMarkerCounter}"";
    // A new list: the parameter is compared by reference, the markers in it by value.
    boundMarkers = [.. boundMarkers, new BitMapMarker
    {
        Id = id,
        Position = boundCenter ?? new(0, 0),
        Alt = $""Marker {boundMarkerCounter}"",
        PopupText = $""Marker {boundMarkerCounter}"",
    }];
}

private void MoveFirstBoundMarker()
{
    if (boundMarkers.Count == 0) return;

    var first = boundMarkers[0];
    boundMarkers = [first with { Position = first.Position.Offset(100_000, 45) }, .. boundMarkers.Skip(1)];
}";

    private readonly string example4RazorCode = @"
<BitToggle Value=""iconCentreAnchor"" ValueChanged=""ToggleIconAnchor"" Text=""Anchor the disc at its centre"" />

<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""iconsMapRef""
            Provider=""@iconsProvider""
            Markers=""iconMarkers""
            OnReady=""OnIconsReady"" />
</div>";
    private readonly string example4CsharpCode = @"
private BitMap<BitLeafletMapProvider> iconsMapRef = default!;
private readonly BitLeafletMapProvider iconsProvider = new() { Center = new(51.5045, -0.0865), Zoom = 15 };
private bool iconCentreAnchor = true;
private List<BitMapMarker> iconMarkers = BuildIconMarkers(true);

// The coordinate the disc stands for, marked by a ring so the anchoring can be seen.
private static readonly BitMapLatLng iconProbe = new(51.5045, -0.0865);

private static string DiscIcon(string fill) => ""data:image/svg+xml;charset=utf-8,"" + Uri.EscapeDataString(
    $""""""<svg xmlns=""http://www.w3.org/2000/svg"" width=""28"" height=""28"" viewBox=""0 0 28 28""><circle cx=""14"" cy=""14"" r=""11"" fill=""{fill}"" stroke=""#fff"" stroke-width=""3""/></svg>"""""");

private async Task OnIconsReady()
{
    await iconsMapRef.AddCircle(""probe"", iconProbe, 12, new() { Color = ""#111"", Weight = 2, Fill = false });
}

private void ToggleIconAnchor(bool centred)
{
    iconCentreAnchor = centred;
    iconMarkers = BuildIconMarkers(centred);
}

private static List<BitMapMarker> BuildIconMarkers(bool centred) =>
[
    // The default pin, in the theme's primary color - or any other, a theme variable included.
    new() { Id = ""pin"", Position = iconProbe.Offset(110, 300), Alt = ""Pin in the primary color"" },
    new() { Id = ""pin-sec"", Position = iconProbe.Offset(110, 240), Alt = ""Pin in the secondary color"", Color = ""var(--bit-clr-sec)"" },
    // A disc stands for the coordinate at its centre, so it has to say so.
    new()
    {
        Id = ""disc"", Position = iconProbe, Alt = centred ? ""Disc, anchored at its centre"" : ""Disc, on the default anchor"",
        IconUrl = DiscIcon(centred ? ""#107c10"" : ""#8a8886""), IconWidth = 28, IconHeight = 28,
        IconAnchorX = centred ? 14 : null, IconAnchorY = centred ? 14 : null,
    },
];";

    private readonly string example5RazorCode = @"
<div style=""height:360px"">
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
    private readonly string example5CsharpCode = @"
private readonly BitLeafletMapProvider popupProvider = new() { Center = new(48.2082, 16.3738), Zoom = 12 };
private readonly List<BitMapMarker> popupMarkers =
[
    new() { Id = ""opera"", Position = new(48.2029, 16.3690), Alt = ""Vienna State Opera"" },
    new() { Id = ""prater"", Position = new(48.2166, 16.3960), Alt = ""Prater"" },
    new() { Id = ""belvedere"", Position = new(48.1915, 16.3809), Alt = ""Belvedere"" },
];
private readonly Dictionary<string, int> popupVisits = [];
private string popupLog = ""Click a marker, then the button in its popup."";

private int GetVisitorCount(string markerId) => popupVisits.GetValueOrDefault(markerId);

// An event handler inside the popup - which an HTML-string popup cannot have.
private void RecordVisit(string markerId)
{
    popupVisits[markerId] = GetVisitorCount(markerId) + 1;
    popupLog = $""Recorded a visit to {markerId}."";
}

private void OnPopupOpened(BitMapMarker marker) => popupLog = $""Opened the popup of {marker.Alt}."";";

    private readonly string example6RazorCode = @"
<BitToggle Value=""clusterEnabled"" ValueChanged=""ToggleClustering"" Text=""Cluster markers"" />
<BitButton OnClick=""RegenerateClusterMarkers"" Variant=""BitVariant.Outline"">Scatter again</BitButton>

<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@clusterProvider""
            Markers=""clusterMarkers""
            Clustering=""@clusterOptions""
            OnClusterClick=""OnClusterClick""
            OnMarkerClick=""OnClusterMarkerClick"" />
</div>

<pre>@clusterLog</pre>";
    private readonly string example6CsharpCode = @"
private readonly BitLeafletMapProvider clusterProvider = new() { Center = new(48.5, 5), Zoom = 4 };
private bool clusterEnabled = true;
private BitMapClustering? clusterOptions = new() { RadiusPixels = 60, MaxZoom = 14 };
private List<BitMapMarker> clusterMarkers = ScatterMarkers();
private string clusterLog = ""Click a bubble to zoom into it."";

private void ToggleClustering(bool enabled)
{
    clusterEnabled = enabled;
    // Null turns clustering off and hands every marker back to the provider.
    clusterOptions = enabled ? new() { RadiusPixels = 60, MaxZoom = 14 } : null;
}

private void RegenerateClusterMarkers() => clusterMarkers = ScatterMarkers();

private static List<BitMapMarker> ScatterMarkers()
{
    (string Name, double Lat, double Lng, int Count)[] cities =
    [
        (""London"", 51.5074, -0.1278, 160), (""Paris"", 48.8566, 2.3522, 120), (""Berlin"", 52.5200, 13.4050, 90),
        (""Madrid"", 40.4168, -3.7038, 70), (""Rome"", 41.9028, 12.4964, 60),
    ];

    return [.. cities.SelectMany(c => Enumerable.Range(1, c.Count).Select(i => new BitMapMarker
    {
        Id = $""{c.Name}-{i}"",
        Position = new(c.Lat + (Random.Shared.NextDouble() - 0.5) * 2.5, c.Lng + (Random.Shared.NextDouble() - 0.5) * 2.5),
        Alt = $""{c.Name} location {i}"",
        PopupText = $""{c.Name} #{i}"",
    }))];
}

private void OnClusterClick(BitMapClusterClickArgs e) => clusterLog = $""A bubble of {e.Count} markers - zoomed to fit them."";

private void OnClusterMarkerClick(string id) => clusterLog = $""Clicked {id}."";";

    private readonly string example7RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""vectorsMapRef""
            Provider=""@vectorsProvider""
            OnReady=""DrawVectors""
            OnVectorClick=""OnVectorClick""
            OnGeoJsonFeatureClick=""OnGeoJsonFeatureClick"" />
</div>

<BitButton OnClick=""DrawVectors"">Draw shapes</BitButton>
<BitButton OnClick=""LoadGeoJson"" Variant=""BitVariant.Outline"">Load GeoJSON</BitButton>
<BitButton OnClick=""ClearVectors"" Variant=""BitVariant.Outline"">Clear</BitButton>

<pre>@vectorsLog</pre>";
    private readonly string example7CsharpCode = @"
private BitMap<BitLeafletMapProvider> vectorsMapRef = default!;
private readonly BitLeafletMapProvider vectorsProvider = new() { Center = new(37.7749, -122.4194), Zoom = 12 };
private string vectorsLog = ""Click a shape."";

private async Task DrawVectors()
{
    await vectorsMapRef.ClearVectorLayers();

    await vectorsMapRef.AddPolyline(""route"", [new(37.80, -122.42), new(37.79, -122.41), new(37.78, -122.40), new(37.77, -122.395)],
        new() { Color = ""#d13438"", Weight = 5 });
    await vectorsMapRef.AddPolygon(""park"", [new(37.769, -122.486), new(37.771, -122.475), new(37.765, -122.472), new(37.762, -122.482)],
        new() { Color = ""#107c10"", FillOpacity = 0.35, Weight = 2 });
    await vectorsMapRef.AddCircle(""radius"", new(37.7849, -122.4094), 900,
        new() { Color = ""#0065ef"", FillOpacity = 0.15, Weight = 2 });
    await vectorsMapRef.AddRectangle(""box"", new(new(37.748, -122.44), new(37.756, -122.42)),
        new() { Color = ""#c19c00"", FillOpacity = 0.12, Weight = 2, DashArray = ""6 4"" });

    await vectorsMapRef.FitBounds(new(new(37.755, -122.49), new(37.805, -122.38)));
    vectorsLog = ""Four shapes drawn. Click one."";
}

private async Task LoadGeoJson()
{
    await vectorsMapRef.ClearVectorLayers();
    await vectorsMapRef.AddGeoJson(""nyc"", sampleGeoJson, new() { Color = ""#8764b8"", Weight = 3, FillOpacity = 0.25 });
    await vectorsMapRef.FitBounds(new(new(40.70, -74.01), new(40.81, -73.95)));
    vectorsLog = ""GeoJSON loaded. Click a feature."";
}

private async Task ClearVectors()
{
    await vectorsMapRef.ClearVectorLayers();
    vectorsLog = ""All vector layers removed."";
}

private void OnVectorClick(BitMapVectorClickArgs e) => vectorsLog = $""Clicked the {e.Kind} \""{e.LayerId}\""."";

private void OnGeoJsonFeatureClick(BitMapGeoJsonFeatureClickArgs e)
{
    // The properties are whatever the document carries - read them as data, never render them as HTML.
    var name = e.Properties.TryGetProperty(""name"", out var value) ? value.ToString() : ""(unnamed)"";
    vectorsLog = $""Clicked the feature \""{name}\"" of the layer \""{e.LayerId}\""."";
}

private const string sampleGeoJson = """"""
    {
      ""type"": ""FeatureCollection"",
      ""features"": [
        {
          ""type"": ""Feature"",
          ""properties"": { ""name"": ""Central Park"" },
          ""geometry"": { ""type"": ""Polygon"", ""coordinates"": [[[-73.981, 40.768], [-73.958, 40.768], [-73.958, 40.800], [-73.981, 40.800], [-73.981, 40.768]]] }
        },
        {
          ""type"": ""Feature"",
          ""properties"": { ""name"": ""Brooklyn Bridge"" },
          ""geometry"": { ""type"": ""LineString"", ""coordinates"": [[-73.9969, 40.7061], [-73.9875, 40.7026]] }
        }
      ]
    }
    """""";";

    private readonly string example8RazorCode = @"
<BitButton OnClick='() => SetBasemap(""osm"")' Variant=""@(basemap == ""osm"" ? BitVariant.Fill : BitVariant.Outline)"">OpenStreetMap</BitButton>
<BitButton OnClick='() => SetBasemap(""carto"")' Variant=""@(basemap == ""carto"" ? BitVariant.Fill : BitVariant.Outline)"">Carto Voyager</BitButton>
<BitButton OnClick='() => SetBasemap(""topo"")' Variant=""@(basemap == ""topo"" ? BitVariant.Fill : BitVariant.Outline)"">OpenTopoMap</BitButton>

<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""layersMapRef""
            Provider=""@layersProvider""
            OnReady=""OnLayersReady"" />
</div>

<BitButton OnClick=""ToggleLayerVisibility"" Variant=""BitVariant.Outline"">@(layerVisible ? ""Hide the area"" : ""Show the area"")</BitButton>
<BitButton OnClick=""CycleLayerStyle"" Variant=""BitVariant.Outline"">Restyle the area</BitButton>
<BitButton OnClick=""ToggleOverlayVisibility"" Variant=""BitVariant.Outline"">@(overlayVisible ? ""Hide the labels"" : ""Show the labels"")</BitButton>
<BitButton OnClick=""ToggleOverlayOpacity"" Variant=""BitVariant.Outline"">@(overlayDimmed ? ""Brighten the labels"" : ""Dim the labels"")</BitButton>

<pre>@layersLog</pre>";
    private readonly string example8CsharpCode = @"
private BitMap<BitLeafletMapProvider> layersMapRef = default!;
private string basemap = ""osm"";
private BitLeafletMapProvider layersProvider = new() { Center = new(51.5074, -0.1278), Zoom = 11 };
private bool layerVisible = true;
private bool overlayVisible = true;
private bool overlayDimmed;
private int layerStyleIndex;
private string layersLog = ""Switch the basemap, or change the area and the labels."";

private static readonly BitMapVectorPathStyle[] layerStyles =
[
    new() { Color = ""#0065ef"", FillOpacity = 0.2, Weight = 3 },
    new() { Color = ""#d13438"", FillOpacity = 0.3, Weight = 5, DashArray = ""6 4"" },
    new() { Color = ""#107c10"", Fill = false, Weight = 4 },
];

private void SetBasemap(string name)
{
    basemap = name;
    // A new instance is what pushes the change: the provider is compared by reference.
    layersProvider = name switch
    {
        ""carto"" => new()
        {
            Center = new(51.5074, -0.1278), Zoom = 11,
            TileUrl = ""https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png"",
            TileAttribution = ""&copy; OpenStreetMap contributors &copy; <a href=\""https://carto.com/attributions\"">CARTO</a>"",
        },
        ""topo"" => new()
        {
            Center = new(51.5074, -0.1278), Zoom = 11, TileMaxZoom = 17,
            TileUrl = ""https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png"",
            TileAttribution = ""Map data: &copy; OpenStreetMap contributors, SRTM | Map style: &copy; OpenTopoMap"",
        },
        _ => new() { Center = new(51.5074, -0.1278), Zoom = 11 },
    };
}

private async Task OnLayersReady()
{
    await layersMapRef.AddCircle(""area"", new(51.5074, -0.1278), 6000, layerStyles[0]);
    await layersMapRef.AddTileOverlay(new()
    {
        Id = ""labels"",
        UrlTemplate = ""https://tiles.stadiamaps.com/tiles/stamen_toner_labels/{z}/{x}/{y}{r}.png"",
        Attribution = ""Map tiles by Stamen Design, hosted by Stadia Maps. Data by OpenStreetMap."",
        Opacity = 0.8,
        MinZoom = 3,
    });
}

private async Task ToggleLayerVisibility()
{
    layerVisible = !layerVisible;
    await layersMapRef.SetLayerVisible(""area"", layerVisible);
    layersLog = layerVisible ? ""The area is back, as it was defined."" : ""The area is hidden - and takes no clicks."";
}

private async Task CycleLayerStyle()
{
    layerStyleIndex = (layerStyleIndex + 1) % layerStyles.Length;
    await layersMapRef.SetLayerStyle(""area"", layerStyles[layerStyleIndex]);
    layersLog = $""Restyled the area (style {layerStyleIndex + 1} of {layerStyles.Length})."";
}

private async Task ToggleOverlayVisibility()
{
    overlayVisible = !overlayVisible;
    await layersMapRef.SetTileOverlayVisible(""labels"", overlayVisible);
    layersLog = overlayVisible ? ""The labels are shown."" : ""The labels are hidden."";
}

private async Task ToggleOverlayOpacity()
{
    overlayDimmed = !overlayDimmed;
    await layersMapRef.SetTileOverlayOpacity(""labels"", overlayDimmed ? 0.25 : 0.8);
    layersLog = overlayDimmed ? ""The labels are dimmed."" : ""The labels are bright again."";
}";

    private readonly string example9RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""eventsMapRef""
            Provider=""@eventsProvider""
            OnClick=""OnMapClick""
            OnDoubleClick=""OnMapDoubleClick""
            OnContextMenu=""OnMapContextMenu""
            OnViewChanged=""OnViewChanged"" />
</div>

<BitButton OnClick=""FlyToTokyo"">Fly to Tokyo</BitButton>
<BitButton OnClick=""ZoomToSix"" Variant=""BitVariant.Outline"">Zoom 6</BitButton>
<BitButton OnClick=""LocateMe"" Variant=""BitVariant.Outline"">Locate me</BitButton>
<BitButton OnClick=""ReadView"" Variant=""BitVariant.Outline"">Log the viewport</BitButton>

<pre>@eventsLog</pre>";
    private readonly string example9CsharpCode = @"
private BitMap<BitLeafletMapProvider> eventsMapRef = default!;
private readonly BitLeafletMapProvider eventsProvider = new() { Center = new(41.9028, 12.4964), Zoom = 5 };
private string eventsLog = ""Click, double-click or right-click the map, or pan it."";

private void OnMapClick(BitMapLatLng p) => eventsLog = $""Click at {p.Latitude:F4}, {p.Longitude:F4}"";

private void OnMapDoubleClick(BitMapLatLng p) => eventsLog = $""Double-click at {p.Latitude:F4}, {p.Longitude:F4}"";

private void OnMapContextMenu(BitMapLatLng p) => eventsLog = $""Right-click at {p.Latitude:F4}, {p.Longitude:F4}"";

private void OnViewChanged(BitMapViewState v) => eventsLog = $""View settled: zoom {v.Zoom:F1}, centre {v.Center.Latitude:F4}, {v.Center.Longitude:F4}"";

private async Task FlyToTokyo() => await eventsMapRef.FlyTo(new(35.6762, 139.6503), 11);

private async Task ZoomToSix() => await eventsMapRef.SetZoom(6);

private async Task LocateMe()
{
    var result = await eventsMapRef.Locate(new() { Zoom = 13 });
    eventsLog = result is null
        ? ""The browser refused the location request, or it timed out.""
        : $""Located at {result.Position.Latitude:F4}, {result.Position.Longitude:F4} (±{result.AccuracyMeters:F0} m)"";
}

private async Task ReadView()
{
    var v = await eventsMapRef.GetView();
    eventsLog = $""Zoom {v.Zoom:F2}, centre {v.Center.Latitude:F4}, {v.Center.Longitude:F4}, "" +
                $""bounds {v.Bounds.SouthWest.Latitude:F2}, {v.Bounds.SouthWest.Longitude:F2} - {v.Bounds.NorthEast.Latitude:F2}, {v.Bounds.NorthEast.Longitude:F2}"";
}";

    private readonly string example10RazorCode = @"
<BitToggle Value=""interScrollWheel"" ValueChanged=""v => { interScrollWheel = v; BuildInteractionProvider(); }"" Text=""Scroll wheel zoom"" />
<BitToggle Value=""interDragging"" ValueChanged=""v => { interDragging = v; BuildInteractionProvider(); }"" Text=""Dragging"" />
<BitToggle Value=""interScaleBar"" ValueChanged=""v => { interScaleBar = v; BuildInteractionProvider(); }"" Text=""Scale bar"" />
<BitToggle Value=""interMaxBounds"" ValueChanged=""v => { interMaxBounds = v; BuildInteractionProvider(); }"" Text=""Keep inside London"" />
<BitToggle @bind-Value=""interDisabled"" Text=""Disabled"" />

<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider"" Provider=""@interProvider"" Disabled=""interDisabled"" />
</div>";
    private readonly string example10CsharpCode = @"
private bool interScrollWheel = true;
private bool interDragging = true;
private bool interScaleBar = true;
private bool interMaxBounds;
private bool interDisabled;
private BitLeafletMapProvider interProvider = new() { Center = new(51.5074, -0.1278), Zoom = 10, ShowScaleControl = true };

private void BuildInteractionProvider()
{
    interProvider = new()
    {
        Center = new(51.5074, -0.1278), Zoom = 10,
        ScrollWheelZoom = interScrollWheel,
        Dragging = interDragging,
        ShowScaleControl = interScaleBar,
        MaxBounds = interMaxBounds ? new(new(51.25, -0.55), new(51.75, 0.35)) : null,
    };
}";

    private readonly string example11RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""controlsMapRef""
            Provider=""@controlsProvider""
            OnFullscreenChanged=""v => isFullscreen = v"">
        <div role=""toolbar"" aria-label=""Map controls""
             style=""display:flex;flex-flow:column;gap:0.25rem;margin:0.75rem;margin-inline-start:auto;padding:0.25rem;border-radius:0.5rem;background:var(--bit-clr-bg-pri);box-shadow:var(--bit-shd-popup)"">
            <BitButton IconOnly IconName=""@BitIconName.Add"" Size=""BitSize.Small"" AriaLabel=""Zoom in"" Title=""Zoom in"" OnClick=""async () => await controlsMapRef.ZoomIn()"" />
            <BitButton IconOnly IconName=""@BitIconName.Remove"" Size=""BitSize.Small"" AriaLabel=""Zoom out"" Title=""Zoom out"" OnClick=""async () => await controlsMapRef.ZoomOut()"" />
            <BitButton IconOnly IconName=""@BitIconName.Up"" Size=""BitSize.Small"" AriaLabel=""Pan north"" Title=""Pan north"" OnClick=""async () => await controlsMapRef.PanBy(0, -120)"" />
            <BitButton IconOnly IconName=""@BitIconName.Down"" Size=""BitSize.Small"" AriaLabel=""Pan south"" Title=""Pan south"" OnClick=""async () => await controlsMapRef.PanBy(0, 120)"" />
            <BitButton IconOnly IconName=""@BitIconName.Back"" Size=""BitSize.Small"" AriaLabel=""Pan west"" Title=""Pan west"" OnClick=""async () => await controlsMapRef.PanBy(-120, 0)"" />
            <BitButton IconOnly IconName=""@BitIconName.Forward"" Size=""BitSize.Small"" AriaLabel=""Pan east"" Title=""Pan east"" OnClick=""async () => await controlsMapRef.PanBy(120, 0)"" />
            <BitButton IconOnly IconName=""@(isFullscreen ? BitIconName.BackToWindow : BitIconName.FullScreen)"" Size=""BitSize.Small""
                       AriaLabel=""@(isFullscreen ? ""Exit fullscreen"" : ""Fullscreen"")"" Title=""@(isFullscreen ? ""Exit fullscreen"" : ""Fullscreen"")""
                       OnClick=""async () => await controlsMapRef.ToggleFullscreen()"" />
        </div>
    </BitMap>
</div>";
    private readonly string example11CsharpCode = @"
private BitMap<BitLeafletMapProvider> controlsMapRef = default!;
// The provider's own zoom buttons are turned off - the toolbar replaces them.
private readonly BitLeafletMapProvider controlsProvider = new() { Center = new(40.4168, -3.7038), Zoom = 11, ZoomControl = false };
private bool isFullscreen;";

    private readonly string example12RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            @ref=""geoMapRef""
            Provider=""@geoProvider""
            Markers=""geoMarkers""
            OnClick=""OnGeoMapClick"" />
</div>

<BitButton OnClick=""FitGeoMarkers"">Fit to the markers</BitButton>
<BitButton OnClick=""FitGeoRadius"" Variant=""BitVariant.Outline"">Frame 5 km around Big Ben</BitButton>
<BitButton OnClick=""ProjectGeoOrigin"" Variant=""BitVariant.Outline"">Where is Big Ben on screen?</BitButton>

<pre>@geoLog</pre>";
    private readonly string example12CsharpCode = @"
private BitMap<BitLeafletMapProvider> geoMapRef = default!;
private readonly BitLeafletMapProvider geoProvider = new() { Center = new(51.5045, -0.0865), Zoom = 12 };
private string geoLog = ""Click the map to drop a probe."";

private static readonly BitMapLatLng bigBen = new(51.5007, -0.1246);

private readonly List<BitMapMarker> geoMarkers =
[
    new() { Id = ""bigben"", Position = bigBen, Alt = ""Big Ben"", TooltipText = ""Big Ben"" },
    new() { Id = ""tower"", Position = new(51.5055, -0.0754), Alt = ""Tower Bridge"", TooltipText = ""Tower Bridge"" },
    new() { Id = ""eye"", Position = new(51.5033, -0.1196), Alt = ""London Eye"", TooltipText = ""London Eye"" },
];

private void OnGeoMapClick(BitMapLatLng point)
{
    var box = BitMapLatLngBounds.FromMarkers(geoMarkers);
    geoLog = $""{bigBen.DistanceTo(point) / 1000:F2} km from Big Ben, {(box.Contains(point) ? ""inside"" : ""outside"")} the markers' box."";
}

private async Task FitGeoMarkers()
{
    // Padded in map units, and capped so three nearby markers do not zoom to street level.
    await geoMapRef.FitBounds(BitMapLatLngBounds.FromMarkers(geoMarkers).Pad(0.15), paddingPixels: 24, maxZoom: 15);
    geoLog = ""Fitted the view to the markers."";
}

private async Task FitGeoRadius()
{
    await geoMapRef.FitBounds(bigBen.ToBounds(5_000), maxZoom: 16);
    geoLog = ""Framed everything within 5 km of Big Ben."";
}

private async Task ProjectGeoOrigin()
{
    var point = await geoMapRef.Project(bigBen);
    geoLog = point is null ? ""Big Ben is off screen."" : $""Big Ben is at {point.Value.X:F0}, {point.Value.Y:F0} px inside the map."";
}";

    private readonly string example13RazorCode = @"
<BitToggle @bind-Value=""a11yCooperative"" Text=""Cooperative gestures"" />
<BitToggle @bind-Value=""a11yAnnounce"" Text=""Announce view changes"" />
<BitToggle @bind-Value=""a11yShowList"" Text=""Show the marker table"" />

<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@a11yProvider""
            Markers=""a11yMarkers""
            AriaLabel=""Map of central Berlin""
            KeyboardInstructions=""Arrow keys pan the map, plus and minus zoom, Escape leaves the map.""
            CooperativeGestures=""@a11yCooperative""
            AnnounceViewChanges=""@a11yAnnounce""
            ViewAnnouncementFormatter=""@FormatAnnouncement""
            MarkerListMode=""@(a11yShowList ? BitMapMarkerListMode.Visible : BitMapMarkerListMode.ScreenReaderOnly)""
            MarkerListCaption=""Berlin landmarks"" />
</div>";
    private readonly string example13CsharpCode = @"
private bool a11yCooperative = true;
private bool a11yAnnounce = true;
private bool a11yShowList = true;
private readonly BitLeafletMapProvider a11yProvider = new() { Center = new(52.5180, 13.3950), Zoom = 13 };

// Every marker has an Alt: it is the marker's accessible name and its name in the table.
private readonly List<BitMapMarker> a11yMarkers =
[
    new() { Id = ""gate"", Position = new(52.5163, 13.3777), Alt = ""Brandenburg Gate"", PopupText = ""Brandenburg Gate"" },
    new() { Id = ""island"", Position = new(52.5169, 13.4019), Alt = ""Museum Island"", PopupText = ""Museum Island"" },
    new() { Id = ""tower"", Position = new(52.5208, 13.4094), Alt = ""TV Tower"", PopupText = ""TV Tower"" },
];

// A real app would reverse-geocode the centre into a place name here.
private static string FormatAnnouncement(BitMapViewState view) => $""Central Berlin at zoom {view.Zoom:F0}."";";

    private readonly string example14RazorCode = @"
<div>State: <b>@lifecycleState</b></div>

<div style=""height:360px;max-width:100%;resize:both;overflow:auto"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@lifecycleProvider""
            LazyLoad
            OnLoadStateChanged=""s => lifecycleState = s"">
        <LoadingTemplate>
            <div style=""display:flex;gap:0.5rem;align-items:center"">
                <BitSpinnerLoading CustomSize=""20"" />
                <span>Fetching the basemap…</span>
            </div>
        </LoadingTemplate>
    </BitMap>
</div>";
    private readonly string example14CsharpCode = @"
private BitMapLoadState lifecycleState = BitMapLoadState.Idle;
private readonly BitLeafletMapProvider lifecycleProvider = new() { Center = new(59.9139, 10.7522), Zoom = 10 };";

    private readonly string example15RazorCode = @"
<BitChoiceGroup Items=""providerItems"" @bind-Value=""providerName"" Horizontal Label=""Provider"" />

<div style=""height:360px"">
    @switch (providerName)
    {
        case ""maplibre"":
            <BitMap TMapProvider=""BitMapLibreMapProvider"" Provider=""@maplibreProvider"" />
            break;
        case ""openlayers"":
            <BitMap TMapProvider=""BitOpenLayersMapProvider"" Provider=""@olProvider"" />
            break;
        case ""arcgis"":
            <BitMap TMapProvider=""BitArcGisMapProvider"" Provider=""@arcGisProvider"" />
            break;
        case ""cesium"":
            <BitMap TMapProvider=""BitCesiumMapProvider"" Provider=""@cesiumProvider"" />
            break;
        default:
            <BitMap TMapProvider=""BitLeafletMapProvider"" Provider=""@leafletProvider"" />
            break;
    }
</div>";
    private readonly string example15CsharpCode = @"
private string providerName = ""leaflet"";
private readonly List<BitChoiceGroupItem<string>> providerItems =
[
    new() { Text = ""Leaflet"", Value = ""leaflet"" },
    new() { Text = ""MapLibre GL"", Value = ""maplibre"" },
    new() { Text = ""OpenLayers"", Value = ""openlayers"" },
    new() { Text = ""ArcGIS"", Value = ""arcgis"" },
    new() { Text = ""CesiumJS"", Value = ""cesium"" },
];
private readonly BitLeafletMapProvider leafletProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
private readonly BitMapLibreMapProvider maplibreProvider = new() { Center = new(48.8566, 2.3522), Zoom = 3 };
private readonly BitOpenLayersMapProvider olProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
// The osm basemap needs no key; any other needs an API key restricted to your domains.
private readonly BitArcGisMapProvider arcGisProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5, BasemapId = ""osm"" };
private readonly BitCesiumMapProvider cesiumProvider = new() { Center = new(48.8566, 2.3522), Zoom = 3, SceneMode = ""scene3d"" };";

    private readonly string example16RazorCode = @"
<BitParams Parameters=""@mapParams"">
    <div style=""height:280px"">
        <BitMap TMapProvider=""BitLeafletMapProvider"" Provider=""@paramsProvider1"" AriaLabel=""Map of Amsterdam"" />
    </div>
    <div style=""height:280px"">
        <BitMap TMapProvider=""BitLeafletMapProvider"" Provider=""@paramsProvider2"" AriaLabel=""Map of Lisbon""
                CooperativeGestures=""false"" />
    </div>
</BitParams>";
    private readonly string example16CsharpCode = @"
private readonly BitLeafletMapProvider paramsProvider1 = new() { Center = new(52.3676, 4.9041), Zoom = 12 };
private readonly BitLeafletMapProvider paramsProvider2 = new() { Center = new(38.7223, -9.1393), Zoom = 12 };

private readonly BitMapParams[] mapParams =
[
    new()
    {
        CooperativeGestures = true,
        CooperativeGesturesWheelHint = ""Hold ctrl while scrolling to zoom"",
        KeyboardInstructions = ""Arrows pan, plus and minus zoom, Escape leaves."",
        Style = ""--bit-Map-radius: 0.75rem; --bit-Map-border: 1px solid var(--bit-clr-brd-sec)"",
    }
];";

    private readonly string example17RazorCode = @"
<div style=""height:280px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@styleProvider""
            Markers=""styleMarkers""
            Style=""--bit-Map-radius: 1rem; --bit-Map-border: 2px solid var(--bit-clr-pri); --bit-Map-popup-background: var(--bit-clr-pri); --bit-Map-popup-color: var(--bit-clr-pri-text); --bit-Map-tooltip-background: var(--bit-clr-sec); --bit-Map-tooltip-color: var(--bit-clr-sec-text);"" />
</div>

<div style=""height:280px"">
    <BitMap TMapProvider=""BitLeafletMapProvider""
            Provider=""@classProvider""
            Markers=""styleMarkers""
            Clustering=""@(new() { RadiusPixels = 120 })""
            Classes=""@(new() { Root = ""custom-map"", Canvas = ""custom-map-canvas"" })""
            Styles=""@(new() { Root = ""--bit-Map-cluster-background: var(--bit-clr-err); --bit-Map-cluster-color: var(--bit-clr-err-text)"" })"" />
</div>";
    private readonly string example17CsharpCode = @"
private readonly BitLeafletMapProvider styleProvider = new() { Center = new(45.4642, 9.1900), Zoom = 12 };
private readonly BitLeafletMapProvider classProvider = new() { Center = new(45.4642, 9.1900), Zoom = 12 };
private readonly List<BitMapMarker> styleMarkers =
[
    new() { Id = ""duomo"", Position = new(45.4642, 9.1916), Alt = ""Duomo"", PopupText = ""Duomo di Milano"", TooltipText = ""Duomo"" },
    new() { Id = ""scala"", Position = new(45.4674, 9.1895), Alt = ""La Scala"", PopupText = ""Teatro alla Scala"", TooltipText = ""La Scala"" },
    new() { Id = ""galleria"", Position = new(45.4659, 9.1900), Alt = ""Galleria"", PopupText = ""Galleria Vittorio Emanuele II"", TooltipText = ""Galleria"" },
    new() { Id = ""castello"", Position = new(45.4705, 9.1793), Alt = ""Castello"", PopupText = ""Castello Sforzesco"", TooltipText = ""Castello"" },
    new() { Id = ""brera"", Position = new(45.4719, 9.1879), Alt = ""Brera"", PopupText = ""Pinacoteca di Brera"", TooltipText = ""Brera"" },
];";
    private const string example17ScssCode = @"::deep {
    .custom-map {
        border: 2px dashed var(--bit-clr-err);
        border-radius: 0.5rem;
    }

    .custom-map-canvas {
        --bit-Map-tile-filter: saturate(0.3);
    }
}";
    private readonly DemoCodeFile[] example17CodeFiles =
    [
        new("BitMapDemo.razor.scss", example17ScssCode),
    ];

    private readonly string example18RazorCode = @"
<div style=""height:360px"">
    <BitMap TMapProvider=""BitLeafletMapProvider"" Dir=""BitDir.Rtl"" Provider=""@rtlProvider"" AriaLabel=""نقشه تهران"">
        <div style=""margin:0.75rem;padding:0.5rem 0.75rem;border-radius:0.5rem;background:var(--bit-clr-bg-pri)"">تهران</div>
    </BitMap>
</div>";
    private readonly string example18CsharpCode = @"
private readonly BitLeafletMapProvider rtlProvider = new() { Center = new(35.6892, 51.3890), Zoom = 11 };";
}
