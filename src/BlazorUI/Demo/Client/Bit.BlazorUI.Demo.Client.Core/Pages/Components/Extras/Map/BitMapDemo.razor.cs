namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.Map;

public partial class BitMapDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AnnounceViewChanges",
            Type = "bool",
            DefaultValue = "false",
            Description = "Announces the new centre and zoom through a polite live region after the user pans or zooms, throttled by ViewAnnouncementThrottle.",
        },
        new()
        {
            Name = "AutoResize",
            Type = "bool",
            DefaultValue = "true",
            Description = "Keeps the map sized to its container, which also recovers a map created inside a hidden tab.",
        },
        new()
        {
            Name = "Center",
            Type = "BitMapLatLng?",
            DefaultValue = "null",
            Description = "Two-way bindable centre: assigning it moves the map, panning writes it back. Unset, the provider's Center owns the camera.",
        },
        new()
        {
            Name = "ChildContent",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Content floated above the map, such as custom controls. Only its children take the pointer, each sized to its content from the top-start corner.",
        },
        new()
        {
            Name = "Classes",
            Type = "BitMapClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the map.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Clustering",
            Type = "BitMapClustering?",
            DefaultValue = "null",
            Description = "Groups nearby markers into a bubble showing their count, the same way on every provider. Null draws every marker.",
            LinkType = LinkType.Link,
            Href = "#clustering",
        },
        new()
        {
            Name = "CooperativeGestures",
            Type = "bool",
            DefaultValue = "false",
            Description = "Requires ctrl/⌘ + wheel to zoom and two fingers to pan, so a bare gesture scrolls the page instead and shows a hint.",
        },
        new()
        {
            Name = "CooperativeGesturesTouchHint",
            Type = "string",
            DefaultValue = "Use two fingers to move the map",
            Description = "The hint shown when a one-finger drag is blocked.",
        },
        new()
        {
            Name = "CooperativeGesturesWheelHint",
            Type = "string",
            DefaultValue = "Use ctrl + scroll to zoom the map",
            Description = "The hint shown when a bare wheel gesture is blocked.",
        },
        new()
        {
            Name = "ErrorLabel",
            Type = "string",
            DefaultValue = "The map could not be loaded.",
            Description = "The text of the built-in failure message.",
        },
        new()
        {
            Name = "ErrorTemplate",
            Type = "RenderFragment<Exception?>?",
            DefaultValue = "null",
            Description = "Replaces the built-in failure message, receiving the exception when there is one.",
        },
        new()
        {
            Name = "EscapeToExit",
            Type = "bool",
            DefaultValue = "true",
            Description = "Moves the focus out of the canvas on Escape, so a focused map is never a keyboard trap. An open marker popup is closed first.",
        },
        new()
        {
            Name = "KeyboardInstructions",
            Type = "string",
            DefaultValue = "Use the arrow keys to pan the map, plus and minus to zoom, and Escape to leave the map.",
            Description = "The keyboard model, read by assistive technologies and shown while the canvas has keyboard focus.",
        },
        new()
        {
            Name = "LazyLoad",
            Type = "bool",
            DefaultValue = "false",
            Description = "Defers creating the map until its container is near the viewport. LoadState stays Idle until then.",
        },
        new()
        {
            Name = "LazyLoadRootMargin",
            Type = "string",
            DefaultValue = "200px",
            Description = "How far outside the viewport the container still counts as visible for LazyLoad.",
        },
        new()
        {
            Name = "LoadingLabel",
            Type = "string",
            DefaultValue = "Loading map…",
            Description = "The text of the built-in loading indicator.",
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
            Name = "MarkerListActionHeader",
            Type = "string",
            DefaultValue = "Show on map",
            Description = "The label of the button bringing a marker into view, also the hidden column heading.",
        },
        new()
        {
            Name = "MarkerListCaption",
            Type = "string",
            DefaultValue = "Map markers",
            Description = "The caption of the marker table. Say what the markers are.",
        },
        new()
        {
            Name = "MarkerListLatitudeHeader",
            Type = "string",
            DefaultValue = "Latitude",
            Description = "The heading of the latitude column.",
        },
        new()
        {
            Name = "MarkerListLongitudeHeader",
            Type = "string",
            DefaultValue = "Longitude",
            Description = "The heading of the longitude column.",
        },
        new()
        {
            Name = "MarkerListMode",
            Type = "BitMapMarkerListMode",
            DefaultValue = "BitMapMarkerListMode.None",
            Description = "Renders the markers as a table of names and coordinates whose rows bring their marker into view - the text alternative a screen reader needs.",
            LinkType = LinkType.Link,
            Href = "#marker-list-mode-enum",
        },
        new()
        {
            Name = "MarkerListNameHeader",
            Type = "string",
            DefaultValue = "Name",
            Description = "The heading of the name column.",
        },
        new()
        {
            Name = "MarkerListTemplate",
            Type = "RenderFragment<IReadOnlyList<BitMapMarker>>?",
            DefaultValue = "null",
            Description = "Replaces the built-in marker table.",
        },
        new()
        {
            Name = "MarkerListZoom",
            Type = "double?",
            DefaultValue = "15",
            Description = "The zoom a marker-list row brings its marker into view at. Null keeps the current zoom.",
        },
        new()
        {
            Name = "MarkerPopupTemplate",
            Type = "RenderFragment<BitMapMarker>?",
            DefaultValue = "null",
            Description = "The content of the popup a marker click opens, as live Blazor markup - escaped, and able to hold components and handlers.",
        },
        new()
        {
            Name = "Markers",
            Type = "IEnumerable<BitMapMarker>?",
            DefaultValue = "null",
            Description = "The markers as a collection. They compare by value, so only the changed ones are sent. It owns the set: imperative additions are reconciled away on its next change.",
            LinkType = LinkType.Link,
            Href = "#marker",
        },
        new()
        {
            Name = "OnClick",
            Type = "EventCallback<BitMapLatLng>",
            DefaultValue = "",
            Description = "Fires when the map is clicked away from a marker or a vector layer.",
        },
        new()
        {
            Name = "OnClusterClick",
            Type = "EventCallback<BitMapClusterClickArgs>",
            DefaultValue = "",
            Description = "Fires when a cluster bubble is clicked, instead of OnMarkerClick.",
        },
        new()
        {
            Name = "OnContextMenu",
            Type = "EventCallback<BitMapLatLng>",
            DefaultValue = "",
            Description = "Fires on a right-click or a long press, with the coordinate under the pointer.",
        },
        new()
        {
            Name = "OnDoubleClick",
            Type = "EventCallback<BitMapLatLng>",
            DefaultValue = "",
            Description = "Fires when the map is double-clicked.",
        },
        new()
        {
            Name = "OnFullscreenChanged",
            Type = "EventCallback<bool>",
            DefaultValue = "",
            Description = "Fires when the map enters or leaves fullscreen, however it did.",
        },
        new()
        {
            Name = "OnGeoJsonFeatureClick",
            Type = "EventCallback<BitMapGeoJsonFeatureClickArgs>",
            DefaultValue = "",
            Description = "Fires when a feature of a GeoJSON layer is clicked, with its properties.",
        },
        new()
        {
            Name = "OnInteropError",
            Type = "EventCallback<BitMapInteropErrorArgs>",
            DefaultValue = "",
            Description = "Fires when a call into the provider fails. The component swallows these failures, so this is where to log them.",
        },
        new()
        {
            Name = "OnLoadStateChanged",
            Type = "EventCallback<BitMapLoadState>",
            DefaultValue = "",
            Description = "Fires whenever LoadState changes.",
        },
        new()
        {
            Name = "OnMarkerClick",
            Type = "EventCallback<string>",
            DefaultValue = "",
            Description = "Fires when a marker is clicked, with its id.",
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
            Name = "OnPopupClosed",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Fires when the MarkerPopupTemplate popup closes.",
        },
        new()
        {
            Name = "OnPopupOpened",
            Type = "EventCallback<BitMapMarker>",
            DefaultValue = "",
            Description = "Fires when the MarkerPopupTemplate popup opens, with its marker.",
        },
        new()
        {
            Name = "OnReady",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Fires once the map can take imperative calls, and again after a swap to a provider with another backend.",
        },
        new()
        {
            Name = "OnRenderContextLost",
            Type = "EventCallback",
            DefaultValue = "",
            Description = "Fires when the browser drops the WebGL context the map renders into.",
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
            Name = "OnVectorClick",
            Type = "EventCallback<BitMapVectorClickArgs>",
            DefaultValue = "",
            Description = "Fires when a vector layer is clicked.",
        },
        new()
        {
            Name = "OnViewChanged",
            Type = "EventCallback<BitMapViewState>",
            DefaultValue = "",
            Description = "Fires once a pan or zoom settles.",
        },
        new()
        {
            Name = "PopupAutoPan",
            Type = "bool",
            DefaultValue = "true",
            Description = "Pans the map as the MarkerPopupTemplate popup opens, just enough to bring the whole popup into view.",
        },
        new()
        {
            Name = "PopupCloseLabel",
            Type = "string",
            DefaultValue = "Close",
            Description = "The accessible name of the popup's close button.",
        },
        new()
        {
            Name = "PopupLabel",
            Type = "string",
            DefaultValue = "Marker details",
            Description = "The accessible name of the popup when its marker has neither an Alt nor a Title.",
        },
        new()
        {
            Name = "Provider",
            Type = "TMapProvider?",
            DefaultValue = "null",
            Description = "The provider's configuration: centre, zoom, tiles, tokens and interaction toggles. Compared by reference - assign a new instance to apply changes.",
            LinkType = LinkType.Link,
            Href = "#provider",
        },
        new()
        {
            Name = "ReplayStateOnProviderSwap",
            Type = "bool",
            DefaultValue = "false",
            Description = "Replays the imperatively added markers, layers and overlays after a swap to a provider with another backend.",
        },
        new()
        {
            Name = "RespectReducedMotion",
            Type = "bool",
            DefaultValue = "true",
            Description = "Makes every camera move - FlyTo, SetView, FitBounds, a cluster's zoom, the keyboard - jump under a reduced-motion preference, unless the move is essential or ForceAnimation is set.",
        },
        new()
        {
            Name = "RoleDescription",
            Type = "string",
            DefaultValue = "interactive map",
            Description = "What a screen reader calls the map canvas in place of \"region\". The canvas is named by AriaLabel.",
        },
        new()
        {
            Name = "ShowLoading",
            Type = "bool",
            DefaultValue = "true",
            Description = "Shows the built-in loading indicator while the map is being created.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitMapClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the map.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "TMapProvider",
            Type = "generic type",
            DefaultValue = "",
            Description = "The provider type: BitLeafletMapProvider, BitMapLibreMapProvider, BitMapboxMapProvider, BitOpenLayersMapProvider, BitArcGisMapProvider, BitAzureMapsMapProvider or BitCesiumMapProvider.",
        },
        new()
        {
            Name = "UnsupportedLabel",
            Type = "string",
            DefaultValue = "This browser cannot display the map (WebGL is unavailable).",
            Description = "The text of the built-in unsupported-browser message.",
        },
        new()
        {
            Name = "UnsupportedTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "Replaces the built-in unsupported-browser message.",
        },
        new()
        {
            Name = "ViewAnnouncementFormatter",
            Type = "Func<BitMapViewState, string>?",
            DefaultValue = "null",
            Description = "Builds the announced text - a place name beats a pair of coordinates.",
        },
        new()
        {
            Name = "ViewAnnouncementThrottle",
            Type = "TimeSpan",
            DefaultValue = "2 seconds",
            Description = "The minimum interval between two view announcements.",
        },
        new()
        {
            Name = "Zoom",
            Type = "double?",
            DefaultValue = "null",
            Description = "Two-way bindable zoom level: assigning it zooms the map, zooming writes it back.",
        },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitMapClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitMap.",
                },
                new()
                {
                    Name = "Canvas",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the focusable canvas the provider draws the map into.",
                },
                new()
                {
                    Name = "Overlay",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the layer holding the ChildContent above the map.",
                },
                new()
                {
                    Name = "Instructions",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the keyboard instructions shown while the canvas has keyboard focus.",
                },
                new()
                {
                    Name = "GestureHint",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the hint shown while a gesture is blocked by the cooperative gestures.",
                },
                new()
                {
                    Name = "Status",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the cover shown while the map is loading, has failed, or is unsupported.",
                },
                new()
                {
                    Name = "Spinner",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the spinner of the built-in loading indicator.",
                },
                new()
                {
                    Name = "Popup",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the popup the MarkerPopupTemplate is rendered in.",
                },
                new()
                {
                    Name = "PopupCloseButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the close button of the popup.",
                },
                new()
                {
                    Name = "PopupBody",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the body of the popup, holding the MarkerPopupTemplate content.",
                },
                new()
                {
                    Name = "MarkerList",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the container of the marker list rendered next to the map.",
                },
                new()
                {
                    Name = "MarkerListButton",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for each \"Show on map\" button of the built-in marker table.",
                },
            ]
        },
        new()
        {
            Id = "marker",
            Title = "BitMapMarker",
            Description = "A record, compared by value. Each property is honoured by every provider unless it says otherwise.",
            Parameters =
            [
                new()
                {
                    Name = "Id",
                    Type = "string",
                    DefaultValue = "",
                    Description = "Unique identifier of the marker within the map. Required.",
                },
                new()
                {
                    Name = "Position",
                    Type = "BitMapLatLng",
                    DefaultValue = "",
                    Description = "The coordinate of the marker. Required.",
                },
                new()
                {
                    Name = "Alt",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The accessible name of the marker, and its name in the marker table.",
                },
                new()
                {
                    Name = "Title",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The native tooltip of the marker, and its accessible name when Alt is not set.",
                },
                new()
                {
                    Name = "PopupText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Plain text shown in the provider's popup on click. Escaped - safe for user data.",
                },
                new()
                {
                    Name = "PopupHtml",
                    Type = "MarkupString?",
                    DefaultValue = "null",
                    Description = "Raw HTML shown in the provider's popup on click. Not escaped - never pass user data.",
                },
                new()
                {
                    Name = "TooltipText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Plain text shown on hover and on keyboard focus (not on ArcGIS and Cesium).",
                },
                new()
                {
                    Name = "TooltipHtml",
                    Type = "MarkupString?",
                    DefaultValue = "null",
                    Description = "Raw HTML shown on hover and on keyboard focus. Not escaped.",
                },
                new()
                {
                    Name = "TooltipPermanent",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Keeps the tooltip open instead of only on hover and focus.",
                },
                new()
                {
                    Name = "TooltipPlacement",
                    Type = "BitPlacement?",
                    DefaultValue = "null",
                    Description = "Where the tooltip opens against the marker. Only Top, Bottom, Left, Right and Center are honoured; any other value, or none, leaves the provider to choose.",
                    LinkType = LinkType.Link,
                    Href = "#placement-enum",
                },
                new()
                {
                    Name = "Focusable",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Makes the marker a tab stop that Enter or Space opens. Turn off for decorative pins.",
                },
                new()
                {
                    Name = "Draggable",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Lets the user move the marker, reported by OnMarkerDragEnd.",
                },
                new()
                {
                    Name = "Color",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The color of the default pin - any CSS color, a theme variable included. Null takes --bit-Map-marker-color.",
                },
                new()
                {
                    Name = "IconUrl",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The image drawn instead of the default pin.",
                },
                new()
                {
                    Name = "IconWidth",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The width of the icon in pixels. Null takes 25 for the pin, 32 for an IconUrl.",
                },
                new()
                {
                    Name = "IconHeight",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The height of the icon in pixels. Null takes 41 for the pin, 32 for an IconUrl.",
                },
                new()
                {
                    Name = "IconAnchorX",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The pixel of the icon, from its left, that sits on the coordinate. Null is the horizontal centre.",
                },
                new()
                {
                    Name = "IconAnchorY",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The pixel of the icon, from its top, that sits on the coordinate. Null is the bottom edge.",
                },
                new()
                {
                    Name = "Opacity",
                    Type = "double",
                    DefaultValue = "1",
                    Description = "The opacity of the marker, 0 to 1.",
                },
                new()
                {
                    Name = "RiseOnHover",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Brings the marker above the others under the pointer (Leaflet).",
                },
                new()
                {
                    Name = "ZIndexOffset",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "Stacks the marker above or below the others (Leaflet).",
                },
            ]
        },
        new()
        {
            Id = "clustering",
            Title = "BitMapClustering",
            Parameters =
            [
                new()
                {
                    Name = "RadiusPixels",
                    Type = "int",
                    DefaultValue = "60",
                    Description = "How close, in screen pixels, markers have to be to join one bubble: a larger radius gives fewer, denser bubbles.",
                },
                new()
                {
                    Name = "MaxZoom",
                    Type = "double",
                    DefaultValue = "16",
                    Description = "The zoom at and above which every marker is drawn individually.",
                },
                new()
                {
                    Name = "MinPoints",
                    Type = "int",
                    DefaultValue = "2",
                    Description = "The fewest markers a bubble stands for.",
                },
                new()
                {
                    Name = "Color",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The fill of the bubbles. Null takes --bit-Map-cluster-background, which falls back to the primary color.",
                },
                new()
                {
                    Name = "TextColor",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The color of the count. Null takes --bit-Map-cluster-color, which falls back to the text color of the primary color.",
                },
                new()
                {
                    Name = "CullOffscreen",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Keeps the markers outside the viewport off the map - most of the performance win.",
                },
                new()
                {
                    Name = "MaxRenderedMarkers",
                    Type = "int",
                    DefaultValue = "2000",
                    Description = "The most individual markers drawn at once past MaxZoom.",
                },
                new()
                {
                    Name = "ExpandPaddingPixels",
                    Type = "int",
                    DefaultValue = "48",
                    Description = "The padding of the zoom a bubble click makes to fit its markers.",
                },
                new()
                {
                    Name = "AriaLabelFormat",
                    Type = "string",
                    DefaultValue = "Cluster of {0} markers",
                    Description = "The accessible name of a bubble; {0} is its count.",
                },
                new()
                {
                    Name = "ZoomOnClick",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Zooms to fit a bubble's markers on click. Turn off to handle OnClusterClick yourself.",
                },
            ]
        },
        new()
        {
            Id = "vector-path-style",
            Title = "BitMapVectorPathStyle",
            Parameters =
            [
                new()
                {
                    Name = "Color",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The stroke color - any CSS color, a theme variable included. Null takes --bit-Map-vector-color.",
                },
                new()
                {
                    Name = "Weight",
                    Type = "double",
                    DefaultValue = "3",
                    Description = "The stroke width in pixels.",
                },
                new()
                {
                    Name = "Opacity",
                    Type = "double",
                    DefaultValue = "1",
                    Description = "The stroke opacity, 0 to 1.",
                },
                new()
                {
                    Name = "Fill",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Whether a closed shape is filled. Unlike a zero FillOpacity, no fill also takes no clicks.",
                },
                new()
                {
                    Name = "FillColor",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The fill color. Null takes Color.",
                },
                new()
                {
                    Name = "FillOpacity",
                    Type = "double",
                    DefaultValue = "0.2",
                    Description = "The fill opacity, 0 to 1.",
                },
                new()
                {
                    Name = "DashArray",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The dash pattern of the stroke, such as \"6 4\".",
                },
                new()
                {
                    Name = "DashOffset",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Where the dash pattern starts.",
                },
                new()
                {
                    Name = "LineCap",
                    Type = "BitMapLineCap",
                    DefaultValue = "BitMapLineCap.Round",
                    Description = "The shape of the stroke's ends: Round, Butt or Square.",
                },
                new()
                {
                    Name = "LineJoin",
                    Type = "BitMapLineJoin",
                    DefaultValue = "BitMapLineJoin.Round",
                    Description = "The shape of the stroke's corners: Round, Bevel or Miter.",
                },
            ]
        },
        new()
        {
            Id = "tile-overlay",
            Title = "BitMapTileOverlay",
            Parameters =
            [
                new()
                {
                    Name = "Id",
                    Type = "string",
                    DefaultValue = "",
                    Description = "Unique identifier of the overlay within the map. Required.",
                },
                new()
                {
                    Name = "UrlTemplate",
                    Type = "string",
                    DefaultValue = "",
                    Description = "The XYZ template with {z}, {x}, {y} and optional {s} placeholders. Required.",
                },
                new()
                {
                    Name = "Attribution",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "The credit the tile source requires, shown in the attribution control.",
                },
                new()
                {
                    Name = "Opacity",
                    Type = "double",
                    DefaultValue = "1",
                    Description = "The opacity of the overlay, 0 to 1.",
                },
                new()
                {
                    Name = "ZIndex",
                    Type = "int",
                    DefaultValue = "100",
                    Description = "The stacking order of the overlay.",
                },
                new()
                {
                    Name = "MinZoom",
                    Type = "int",
                    DefaultValue = "0",
                    Description = "The lowest zoom the source publishes tiles for.",
                },
                new()
                {
                    Name = "MaxZoom",
                    Type = "int",
                    DefaultValue = "19",
                    Description = "The highest zoom the source publishes tiles for.",
                },
                new()
                {
                    Name = "Subdomains",
                    Type = "string",
                    DefaultValue = "abc",
                    Description = "The values {s} takes, for sources that shard their tiles across hostnames.",
                },
            ]
        },
        new()
        {
            Id = "provider",
            Title = "BitMapProviderBase",
            Description = "What every provider shares. Each one adds its own options - tiles, style URLs, tokens - documented on its type.",
            Parameters =
            [
                new()
                {
                    Name = "Center",
                    Type = "BitMapLatLng",
                    DefaultValue = "51.505, -0.09",
                    Description = "The initial centre.",
                },
                new()
                {
                    Name = "Zoom",
                    Type = "double",
                    DefaultValue = "13",
                    Description = "The initial zoom.",
                },
                new()
                {
                    Name = "MinZoom",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The lowest zoom the user can reach.",
                },
                new()
                {
                    Name = "MaxZoom",
                    Type = "int?",
                    DefaultValue = "null",
                    Description = "The highest zoom the user can reach.",
                },
                new()
                {
                    Name = "ZoomControl",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Shows the provider's +/- buttons.",
                },
                new()
                {
                    Name = "AttributionControl",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Shows the attribution - often a licence term of the tiles.",
                },
                new()
                {
                    Name = "ScrollWheelZoom",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Zooms with the mouse wheel.",
                },
                new()
                {
                    Name = "DoubleClickZoom",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Zooms on a double click.",
                },
                new()
                {
                    Name = "BoxZoom",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Zooms to a shift-dragged box (Leaflet, OpenLayers, MapLibre, Mapbox).",
                },
                new()
                {
                    Name = "Dragging",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Pans by dragging.",
                },
                new()
                {
                    Name = "KeyboardNavigation",
                    Type = "bool",
                    DefaultValue = "true",
                    Description = "Pans and zooms with the keyboard while the canvas has focus.",
                },
                new()
                {
                    Name = "SuppressBrowserContextMenu",
                    Type = "bool",
                    DefaultValue = "false",
                    Description = "Keeps the browser's own menu from opening on a right-click. Only with a menu of your own in its place.",
                },
                new()
                {
                    Name = "MaxBounds",
                    Type = "BitMapLatLngBounds?",
                    DefaultValue = "null",
                    Description = "Keeps the view inside a rectangle (Leaflet, MapLibre, Mapbox).",
                },
                new()
                {
                    Name = "AdditionalOptions",
                    Type = "Dictionary<string, object?>",
                    DefaultValue = "",
                    Description = "Passed straight to the library's map constructor (Leaflet, MapLibre, Mapbox).",
                },
            ]
        },
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "marker-list-mode-enum",
            Name = "BitMapMarkerListMode",
            Description = "How the text alternative to the markers is rendered.",
            Items =
            [
                new() { Name = "None", Value = "0", Description = "No list is rendered." },
                new() { Name = "ScreenReaderOnly", Value = "1", Description = "The list is hidden visually but reachable by assistive technologies, and printed." },
                new() { Name = "Visible", Value = "2", Description = "The list is rendered below the map for everyone." },
            ]
        },
        new()
        {
            Id = "load-state-enum",
            Name = "BitMapLoadState",
            Description = "Where the map is in its lifecycle.",
            Items =
            [
                new() { Name = "Idle", Value = "0", Description = "Not started yet: prerendering, or waiting to scroll into view under LazyLoad." },
                new() { Name = "Loading", Value = "1", Description = "The provider's assets are loading or the map is being created." },
                new() { Name = "Ready", Value = "2", Description = "The map is live and every method can be called." },
                new() { Name = "Failed", Value = "3", Description = "Creating the map failed; LoadError holds the exception." },
                new() { Name = "Unsupported", Value = "4", Description = "The browser cannot run this provider - no WebGL for a GL-backed one." },
            ]
        },
        DemoSharedEnums.BitPlacement(),
    ];

    private readonly List<ComponentCssVariable> componentCssVariables =
    [
        new() { Name = "--bit-Map-height", DefaultValue = "100%", Description = "Height of the map. A percentage needs a container with a height of its own." },
        new() { Name = "--bit-Map-background", DefaultValue = "--bit-clr-bg-sec", Description = "What shows where no tile is drawn: before the tiles load, behind the loading cover and around a fullscreen map." },
        new() { Name = "--bit-Map-tile-filter", DefaultValue = "none", Description = "CSS filter on the raster tiles of Leaflet and OpenLayers - never the vectors and markers on them. invert(1) hue-rotate(180deg) is a dark basemap." },
        new() { Name = "--bit-Map-border", DefaultValue = "none", Description = "Border of the map." },
        new() { Name = "--bit-Map-radius", DefaultValue = "--bit-shp-radius-none", Description = "Corner radius of the map, which clips the tiles too." },
        new() { Name = "--bit-Map-focus-color", DefaultValue = "--bit-clr-pri-focus", Description = "Focus outline of the map canvas." },
        new() { Name = "--bit-Map-disabled-opacity", DefaultValue = "--bit-opa-dis", Description = "Opacity of the canvas of a disabled map." },
        new() { Name = "--bit-Map-popup-background", DefaultValue = "--bit-clr-bg-pri", Description = "Background of the popups (the providers' included) and the keyboard instructions." },
        new() { Name = "--bit-Map-popup-color", DefaultValue = "--bit-clr-fg-pri", Description = "Text color of the popups and the keyboard instructions." },
        new() { Name = "--bit-Map-popup-radius", DefaultValue = "--bit-shp-radius-popup", Description = "Corner radius of the popups, the tooltips and the keyboard instructions." },
        new() { Name = "--bit-Map-popup-shadow", DefaultValue = "--bit-shd-popup", Description = "Elevation of the popups and the keyboard instructions." },
        new() { Name = "--bit-Map-popup-padding", DefaultValue = "spacing(1) spacing(1.25)", Description = "Padding of the MarkerPopupTemplate popup." },
        new() { Name = "--bit-Map-popup-max-width", DefaultValue = "18rem", Description = "Widest the MarkerPopupTemplate popup gets before wrapping." },
        new() { Name = "--bit-Map-tooltip-background", DefaultValue = "--bit-clr-tooltip-bg", Description = "Background of the marker tooltips." },
        new() { Name = "--bit-Map-tooltip-color", DefaultValue = "--bit-clr-tooltip-fg", Description = "Text color of the marker tooltips." },
        new() { Name = "--bit-Map-hint-background", DefaultValue = "--bit-clr-bg-overlay", Description = "Backdrop of the cooperative-gestures hint." },
        new() { Name = "--bit-Map-hint-color", DefaultValue = "--bit-clr-ntr-white", Description = "Text color of the cooperative-gestures hint." },
        new() { Name = "--bit-Map-marker-color", DefaultValue = "--bit-clr-pri", Description = "The default pin, on every provider, unless BitMapMarker.Color is set." },
        new() { Name = "--bit-Map-vector-color", DefaultValue = "--bit-clr-pri", Description = "Stroke and fill of the shapes, on every provider, unless BitMapVectorPathStyle.Color is set." },
        new() { Name = "--bit-Map-cluster-background", DefaultValue = "--bit-clr-pri", Description = "Fill of the cluster bubbles, unless BitMapClustering.Color is set." },
        new() { Name = "--bit-Map-cluster-color", DefaultValue = "--bit-clr-pri-text", Description = "Count inside the cluster bubbles, unless BitMapClustering.TextColor is set." },
        new() { Name = "--bit-Map-cluster-border-color", DefaultValue = "--bit-clr-ntr-white", Description = "Ring around the cluster bubbles." },
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
            Description = "Ids of the markers currently on the map, in no guaranteed order. Read from the component's own snapshot, so it costs no interop round-trip.",
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
            Type = "Func<bool, ValueTask>",
            DefaultValue = "",
            Description = "Closes the MarkerPopupTemplate popup, if one is open. Pass true to return the focus to the map.",
         },
         new()
         {
            Name = "OrderedMarkers",
            Type = "IReadOnlyList<BitMapMarker>",
            DefaultValue = "",
            Description = "The markers currently on the map, in no guaranteed order. Costs no interop round-trip.",
         },
         new()
         {
            Name = "LayerIds",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "",
            Description = "Ids of the vector layers currently on the map, in no guaranteed order. Costs no interop round-trip.",
         },
         new()
         {
            Name = "TileOverlayIds",
            Type = "IReadOnlyCollection<string>",
            DefaultValue = "",
            Description = "Ids of the tile overlays currently on the map, in no guaranteed order. Costs no interop round-trip.",
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
            Name = "PanTo",
            Type = "Func<BitMapLatLng, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Move the centre while keeping the current zoom.",
         },
         new()
         {
            Name = "Project",
            Type = "Func<BitMapLatLng, ValueTask<BitMapPoint?>>",
            DefaultValue = "",
            Description = "Where a coordinate currently sits inside the map container, in CSS pixels from its top-left corner. Null when it is not on screen. Only valid for the viewport it was read at.",
         },
         new()
         {
            Name = "FitBounds",
            Type = "Func<BitMapLatLngBounds, int, double, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Fit the view to the given bounding box (bounds, padding, maxZoom, animate, essential); the maxZoom ceiling (default 18) keeps a box around a single place from dropping to street level.",
         },
         new()
         {
            Name = "FitBoundsToMarkers",
            Type = "Func<int, double, bool, bool, ValueTask>",
            DefaultValue = "",
            Description = "Fit the view to include every marker currently drawn, with the same arguments as FitBounds.",
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
            Name = "CloseMarkerPopup",
            Type = "Func<ValueTask<bool>>",
            DefaultValue = "",
            Description = "Close the provider's open marker popup; true when one was open. ClosePopup closes the MarkerPopupTemplate one.",
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
            Name = "ClearTileOverlays",
            Type = "Func<ValueTask>",
            DefaultValue = "",
            Description = "Remove every tile overlay, leaving the base map alone.",
         },
         new()
         {
            Name = "RemoveTileOverlay",
            Type = "Func<string, ValueTask>",
            DefaultValue = "",
            Description = "Remove a tile overlay by id.",
         },
    ];



    // ── Markers ───────────────────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> markersMapRef = default!;
    private readonly BitLeafletMapProvider markersProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
    private string markersLog = "Click, hover or drag a marker.";
    private int markerCounter;

    private async Task OnMarkersReady()
    {
        await markersMapRef.SyncMarkers(
        [
            new() { Id = "paris", Position = new(48.8566, 2.3522), Alt = "Paris", PopupText = "Paris - click a marker to open its popup.", TooltipText = "Paris" },
            new() { Id = "london", Position = new(51.5074, -0.1278), Alt = "London", PopupText = "London - drag me.", TooltipText = "Drag me", Draggable = true },
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

        var id = $"marker-{++markerCounter}";
        await markersMapRef.AddMarker(new BitMapMarker
        {
            Id = id,
            Position = position,
            Alt = $"Marker {markerCounter}",
            PopupText = $"Marker {markerCounter} at {position.Latitude:F4}, {position.Longitude:F4}",
            TooltipText = $"Marker {markerCounter}",
        });
        markersLog = $"Added {id}.";
    }

    private async Task OpenLondonPopup() => await markersMapRef.OpenMarkerPopup("london");

    private async Task ClosePopup() => markersLog = await markersMapRef.CloseMarkerPopup() ? "Popup closed." : "No popup was open.";

    private async Task FitToMarkers() => await markersMapRef.FitBoundsToMarkers();

    private async Task ClearMarkers()
    {
        await markersMapRef.ClearMarkers();
        markersLog = "All markers removed.";
    }

    private void OnMarkerClick(string id) => markersLog = $"Clicked {id}.";

    private void OnMarkerDragEnd(BitMapMarkerDragEndArgs e) => markersLog = $"Dropped {e.Id} at {e.Position.Latitude:F4}, {e.Position.Longitude:F4}.";

    // ── Data binding ──────────────────────────────────────────────────────────

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
        var id = $"marker-{++boundMarkerCounter}";
        // A new list: the parameter is compared by reference, the markers in it by value.
        boundMarkers = [.. boundMarkers, new BitMapMarker
        {
            Id = id,
            Position = boundCenter ?? new(0, 0),
            Alt = $"Marker {boundMarkerCounter}",
            PopupText = $"Marker {boundMarkerCounter}",
        }];
    }

    private void MoveFirstBoundMarker()
    {
        if (boundMarkers.Count == 0) return;

        var first = boundMarkers[0];
        boundMarkers = [first with { Position = first.Position.Offset(100_000, 45) }, .. boundMarkers.Skip(1)];
    }

    // ── Marker icons ──────────────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> iconsMapRef = default!;
    private readonly BitLeafletMapProvider iconsProvider = new() { Center = new(51.5045, -0.0865), Zoom = 15 };
    private bool iconCentreAnchor = true;
    private List<BitMapMarker> iconMarkers = BuildIconMarkers(true);

    // The coordinate the disc stands for, marked by a ring so the anchoring can be seen.
    private static readonly BitMapLatLng iconProbe = new(51.5045, -0.0865);

    private static string DiscIcon(string fill) => "data:image/svg+xml;charset=utf-8," + Uri.EscapeDataString(
        $"""<svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 28 28"><circle cx="14" cy="14" r="11" fill="{fill}" stroke="#fff" stroke-width="3"/></svg>""");

    private async Task OnIconsReady()
    {
        await iconsMapRef.AddCircle("probe", iconProbe, 12, new() { Color = "#111", Weight = 2, Fill = false });
    }

    private void ToggleIconAnchor(bool centred)
    {
        iconCentreAnchor = centred;
        iconMarkers = BuildIconMarkers(centred);
    }

    private static List<BitMapMarker> BuildIconMarkers(bool centred) =>
    [
        // The default pin, in the theme's primary color - or any other, a theme variable included.
        new() { Id = "pin", Position = iconProbe.Offset(110, 300), Alt = "Pin in the primary color" },
        new() { Id = "pin-sec", Position = iconProbe.Offset(110, 240), Alt = "Pin in the secondary color", Color = "var(--bit-clr-sec)" },
        // A disc stands for the coordinate at its centre, so it has to say so.
        new()
        {
            Id = "disc", Position = iconProbe, Alt = centred ? "Disc, anchored at its centre" : "Disc, on the default anchor",
            IconUrl = DiscIcon(centred ? "#107c10" : "#8a8886"), IconWidth = 28, IconHeight = 28,
            IconAnchorX = centred ? 14 : null, IconAnchorY = centred ? 14 : null,
        },
    ];

    // ── Popup content ─────────────────────────────────────────────────────────

    private readonly BitLeafletMapProvider popupProvider = new() { Center = new(48.2082, 16.3738), Zoom = 12 };
    private readonly List<BitMapMarker> popupMarkers =
    [
        new() { Id = "opera", Position = new(48.2029, 16.3690), Alt = "Vienna State Opera" },
        new() { Id = "prater", Position = new(48.2166, 16.3960), Alt = "Prater" },
        new() { Id = "belvedere", Position = new(48.1915, 16.3809), Alt = "Belvedere" },
    ];
    private readonly Dictionary<string, int> popupVisits = [];
    private string popupLog = "Click a marker, then the button in its popup.";

    private int GetVisitorCount(string markerId) => popupVisits.GetValueOrDefault(markerId);

    // An event handler inside the popup - which an HTML-string popup cannot have.
    private void RecordVisit(string markerId)
    {
        popupVisits[markerId] = GetVisitorCount(markerId) + 1;
        popupLog = $"Recorded a visit to {markerId}.";
    }

    private void OnPopupOpened(BitMapMarker marker) => popupLog = $"Opened the popup of {marker.Alt}.";

    // ── Clustering ────────────────────────────────────────────────────────────

    private readonly BitLeafletMapProvider clusterProvider = new() { Center = new(48.5, 5), Zoom = 4 };
    private bool clusterEnabled = true;
    private BitMapClustering? clusterOptions = new() { RadiusPixels = 60, MaxZoom = 14 };
    private List<BitMapMarker> clusterMarkers = ScatterMarkers();
    private string clusterLog = "Click a bubble to zoom into it.";

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
            ("London", 51.5074, -0.1278, 160), ("Paris", 48.8566, 2.3522, 120), ("Berlin", 52.5200, 13.4050, 90),
            ("Madrid", 40.4168, -3.7038, 70), ("Rome", 41.9028, 12.4964, 60),
        ];

        return [.. cities.SelectMany(c => Enumerable.Range(1, c.Count).Select(i => new BitMapMarker
        {
            Id = $"{c.Name}-{i}",
            Position = new(c.Lat + (Random.Shared.NextDouble() - 0.5) * 2.5, c.Lng + (Random.Shared.NextDouble() - 0.5) * 2.5),
            Alt = $"{c.Name} location {i}",
            PopupText = $"{c.Name} #{i}",
        }))];
    }

    private void OnClusterClick(BitMapClusterClickArgs e) => clusterLog = $"A bubble of {e.Count} markers - zoomed to fit them.";

    private void OnClusterMarkerClick(string id) => clusterLog = $"Clicked {id}.";

    // ── Vector layers ─────────────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> vectorsMapRef = default!;
    private readonly BitLeafletMapProvider vectorsProvider = new() { Center = new(37.7749, -122.4194), Zoom = 12 };
    private string vectorsLog = "Click a shape.";

    private async Task DrawVectors()
    {
        await vectorsMapRef.ClearVectorLayers();

        await vectorsMapRef.AddPolyline("route", [new(37.80, -122.42), new(37.79, -122.41), new(37.78, -122.40), new(37.77, -122.395)],
            new() { Color = "#d13438", Weight = 5 });
        await vectorsMapRef.AddPolygon("park", [new(37.769, -122.486), new(37.771, -122.475), new(37.765, -122.472), new(37.762, -122.482)],
            new() { Color = "#107c10", FillOpacity = 0.35, Weight = 2 });
        await vectorsMapRef.AddCircle("radius", new(37.7849, -122.4094), 900,
            new() { Color = "#0065ef", FillOpacity = 0.15, Weight = 2 });
        await vectorsMapRef.AddRectangle("box", new(new(37.748, -122.44), new(37.756, -122.42)),
            new() { Color = "#c19c00", FillOpacity = 0.12, Weight = 2, DashArray = "6 4" });

        await vectorsMapRef.FitBounds(new(new(37.755, -122.49), new(37.805, -122.38)));
        vectorsLog = "Four shapes drawn. Click one.";
    }

    private async Task LoadGeoJson()
    {
        await vectorsMapRef.ClearVectorLayers();
        await vectorsMapRef.AddGeoJson("nyc", sampleGeoJson, new() { Color = "#8764b8", Weight = 3, FillOpacity = 0.25 });
        await vectorsMapRef.FitBounds(new(new(40.70, -74.01), new(40.81, -73.95)));
        vectorsLog = "GeoJSON loaded. Click a feature.";
    }

    private async Task ClearVectors()
    {
        await vectorsMapRef.ClearVectorLayers();
        vectorsLog = "All vector layers removed.";
    }

    private void OnVectorClick(BitMapVectorClickArgs e) => vectorsLog = $"Clicked the {e.Kind} \"{e.LayerId}\".";

    private void OnGeoJsonFeatureClick(BitMapGeoJsonFeatureClickArgs e)
    {
        // The properties are whatever the document carries - read them as data, never render them as HTML.
        var name = e.Properties.TryGetProperty("name", out var value) ? value.ToString() : "(unnamed)";
        vectorsLog = $"Clicked the feature \"{name}\" of the layer \"{e.LayerId}\".";
    }

    private const string sampleGeoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "properties": { "name": "Central Park" },
              "geometry": { "type": "Polygon", "coordinates": [[[-73.981, 40.768], [-73.958, 40.768], [-73.958, 40.800], [-73.981, 40.800], [-73.981, 40.768]]] }
            },
            {
              "type": "Feature",
              "properties": { "name": "Brooklyn Bridge" },
              "geometry": { "type": "LineString", "coordinates": [[-73.9969, 40.7061], [-73.9875, 40.7026]] }
            }
          ]
        }
        """;

    // ── Layers ────────────────────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> layersMapRef = default!;
    private string basemap = "osm";
    private BitLeafletMapProvider layersProvider = new() { Center = new(51.5074, -0.1278), Zoom = 11 };
    private bool layerVisible = true;
    private bool overlayVisible = true;
    private bool overlayDimmed;
    private int layerStyleIndex;
    private string layersLog = "Switch the basemap, or change the area and the labels.";

    private static readonly BitMapVectorPathStyle[] layerStyles =
    [
        new() { Color = "#0065ef", FillOpacity = 0.2, Weight = 3 },
        new() { Color = "#d13438", FillOpacity = 0.3, Weight = 5, DashArray = "6 4" },
        new() { Color = "#107c10", Fill = false, Weight = 4 },
    ];

    private void SetBasemap(string name)
    {
        basemap = name;
        // A new instance is what pushes the change: the provider is compared by reference.
        layersProvider = name switch
        {
            "carto" => new()
            {
                Center = new(51.5074, -0.1278), Zoom = 11,
                TileUrl = "https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png",
                TileAttribution = "&copy; OpenStreetMap contributors &copy; <a href=\"https://carto.com/attributions\">CARTO</a>",
            },
            "topo" => new()
            {
                Center = new(51.5074, -0.1278), Zoom = 11, TileMaxZoom = 17,
                TileUrl = "https://{s}.tile.opentopomap.org/{z}/{x}/{y}.png",
                TileAttribution = "Map data: &copy; OpenStreetMap contributors, SRTM | Map style: &copy; OpenTopoMap",
            },
            _ => new() { Center = new(51.5074, -0.1278), Zoom = 11 },
        };
    }

    private async Task OnLayersReady()
    {
        await layersMapRef.AddCircle("area", new(51.5074, -0.1278), 6000, layerStyles[0]);
        await layersMapRef.AddTileOverlay(new()
        {
            Id = "labels",
            UrlTemplate = "https://tiles.stadiamaps.com/tiles/stamen_toner_labels/{z}/{x}/{y}{r}.png",
            Attribution = "Map tiles by Stamen Design, hosted by Stadia Maps. Data by OpenStreetMap.",
            Opacity = 0.8,
            MinZoom = 3,
        });
    }

    private async Task ToggleLayerVisibility()
    {
        layerVisible = !layerVisible;
        await layersMapRef.SetLayerVisible("area", layerVisible);
        layersLog = layerVisible ? "The area is back, as it was defined." : "The area is hidden - and takes no clicks.";
    }

    private async Task CycleLayerStyle()
    {
        layerStyleIndex = (layerStyleIndex + 1) % layerStyles.Length;
        await layersMapRef.SetLayerStyle("area", layerStyles[layerStyleIndex]);
        layersLog = $"Restyled the area (style {layerStyleIndex + 1} of {layerStyles.Length}).";
    }

    private async Task ToggleOverlayVisibility()
    {
        overlayVisible = !overlayVisible;
        await layersMapRef.SetTileOverlayVisible("labels", overlayVisible);
        layersLog = overlayVisible ? "The labels are shown." : "The labels are hidden.";
    }

    private async Task ToggleOverlayOpacity()
    {
        overlayDimmed = !overlayDimmed;
        await layersMapRef.SetTileOverlayOpacity("labels", overlayDimmed ? 0.25 : 0.8);
        layersLog = overlayDimmed ? "The labels are dimmed." : "The labels are bright again.";
    }

    // ── Camera & events ───────────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> eventsMapRef = default!;
    private readonly BitLeafletMapProvider eventsProvider = new() { Center = new(41.9028, 12.4964), Zoom = 5 };
    private string eventsLog = "Click, double-click or right-click the map, or pan it.";

    private void OnMapClick(BitMapLatLng p) => eventsLog = $"Click at {p.Latitude:F4}, {p.Longitude:F4}";

    private void OnMapDoubleClick(BitMapLatLng p) => eventsLog = $"Double-click at {p.Latitude:F4}, {p.Longitude:F4}";

    private void OnMapContextMenu(BitMapLatLng p) => eventsLog = $"Right-click at {p.Latitude:F4}, {p.Longitude:F4}";

    private void OnViewChanged(BitMapViewState v) => eventsLog = $"View settled: zoom {v.Zoom:F1}, centre {v.Center.Latitude:F4}, {v.Center.Longitude:F4}";

    private async Task FlyToTokyo() => await eventsMapRef.FlyTo(new(35.6762, 139.6503), 11);

    private async Task ZoomToSix() => await eventsMapRef.SetZoom(6);

    private async Task LocateMe()
    {
        var result = await eventsMapRef.Locate(new() { Zoom = 13 });
        eventsLog = result is null
            ? "The browser refused the location request, or it timed out."
            : $"Located at {result.Position.Latitude:F4}, {result.Position.Longitude:F4} (±{result.AccuracyMeters:F0} m)";
    }

    private async Task ReadView()
    {
        var v = await eventsMapRef.GetView();
        eventsLog = $"Zoom {v.Zoom:F2}, centre {v.Center.Latitude:F4}, {v.Center.Longitude:F4}, " +
                    $"bounds {v.Bounds.SouthWest.Latitude:F2}, {v.Bounds.SouthWest.Longitude:F2} - {v.Bounds.NorthEast.Latitude:F2}, {v.Bounds.NorthEast.Longitude:F2}";
    }

    // ── Interaction ───────────────────────────────────────────────────────────

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
    }

    // ── Custom controls ───────────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> controlsMapRef = default!;
    private readonly BitLeafletMapProvider controlsProvider = new() { Center = new(40.4168, -3.7038), Zoom = 11, ZoomControl = false };
    private bool isFullscreen;

    // ── Geographic helpers ────────────────────────────────────────────────────

    private BitMap<BitLeafletMapProvider> geoMapRef = default!;
    private readonly BitLeafletMapProvider geoProvider = new() { Center = new(51.5045, -0.0865), Zoom = 12 };
    private string geoLog = "Click the map to drop a probe.";

    private static readonly BitMapLatLng bigBen = new(51.5007, -0.1246);

    private readonly List<BitMapMarker> geoMarkers =
    [
        new() { Id = "bigben", Position = bigBen, Alt = "Big Ben", TooltipText = "Big Ben" },
        new() { Id = "tower", Position = new(51.5055, -0.0754), Alt = "Tower Bridge", TooltipText = "Tower Bridge" },
        new() { Id = "eye", Position = new(51.5033, -0.1196), Alt = "London Eye", TooltipText = "London Eye" },
    ];

    private void OnGeoMapClick(BitMapLatLng point)
    {
        var box = BitMapLatLngBounds.FromMarkers(geoMarkers);
        geoLog = $"{bigBen.DistanceTo(point) / 1000:F2} km from Big Ben, {(box.Contains(point) ? "inside" : "outside")} the markers' box.";
    }

    private async Task FitGeoMarkers()
    {
        // Padded in map units, and capped so three nearby markers do not zoom to street level.
        await geoMapRef.FitBounds(BitMapLatLngBounds.FromMarkers(geoMarkers).Pad(0.15), paddingPixels: 24, maxZoom: 15);
        geoLog = "Fitted the view to the markers.";
    }

    private async Task FitGeoRadius()
    {
        await geoMapRef.FitBounds(bigBen.ToBounds(5_000), maxZoom: 16);
        geoLog = "Framed everything within 5 km of Big Ben.";
    }

    private async Task ProjectGeoOrigin()
    {
        var point = await geoMapRef.Project(bigBen);
        geoLog = point is null ? "Big Ben is off screen." : $"Big Ben is at {point.Value.X:F0}, {point.Value.Y:F0} px inside the map.";
    }

    // ── Accessibility ─────────────────────────────────────────────────────────

    private bool a11yCooperative = true;
    private bool a11yAnnounce = true;
    private bool a11yShowList = true;
    private readonly BitLeafletMapProvider a11yProvider = new() { Center = new(52.5180, 13.3950), Zoom = 13 };

    // Every marker has an Alt: it is the marker's accessible name and its name in the table.
    private readonly List<BitMapMarker> a11yMarkers =
    [
        new() { Id = "gate", Position = new(52.5163, 13.3777), Alt = "Brandenburg Gate", PopupText = "Brandenburg Gate" },
        new() { Id = "island", Position = new(52.5169, 13.4019), Alt = "Museum Island", PopupText = "Museum Island" },
        new() { Id = "tower", Position = new(52.5208, 13.4094), Alt = "TV Tower", PopupText = "TV Tower" },
    ];

    // A real app would reverse-geocode the centre into a place name here.
    private static string FormatAnnouncement(BitMapViewState view) => $"Central Berlin at zoom {view.Zoom:F0}.";

    // ── Loading and lifecycle ─────────────────────────────────────────────────

    private BitMapLoadState lifecycleState = BitMapLoadState.Idle;
    private readonly BitLeafletMapProvider lifecycleProvider = new() { Center = new(59.9139, 10.7522), Zoom = 10 };

    // ── Providers ─────────────────────────────────────────────────────────────

    private string providerName = "leaflet";
    private readonly List<BitChoiceGroupItem<string>> providerItems =
    [
        new() { Text = "Leaflet", Value = "leaflet" },
        new() { Text = "MapLibre GL", Value = "maplibre" },
        new() { Text = "OpenLayers", Value = "openlayers" },
        new() { Text = "ArcGIS", Value = "arcgis" },
        new() { Text = "CesiumJS", Value = "cesium" },
    ];
    private readonly BitLeafletMapProvider leafletProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
    private readonly BitMapLibreMapProvider maplibreProvider = new() { Center = new(48.8566, 2.3522), Zoom = 3 };
    private readonly BitOpenLayersMapProvider olProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5 };
    private readonly BitArcGisMapProvider arcGisProvider = new() { Center = new(48.8566, 2.3522), Zoom = 5, BasemapId = "osm" };
    private readonly BitCesiumMapProvider cesiumProvider = new() { Center = new(48.8566, 2.3522), Zoom = 3, SceneMode = "scene3d" };

    // ── Cascading parameters ──────────────────────────────────────────────────

    private readonly BitLeafletMapProvider paramsProvider1 = new() { Center = new(52.3676, 4.9041), Zoom = 12 };
    private readonly BitLeafletMapProvider paramsProvider2 = new() { Center = new(38.7223, -9.1393), Zoom = 12 };

    private readonly BitMapParams[] mapParams =
    [
        new()
        {
            CooperativeGestures = true,
            CooperativeGesturesWheelHint = "Hold ctrl while scrolling to zoom",
            KeyboardInstructions = "Arrows pan, plus and minus zoom, Escape leaves.",
            Style = "--bit-Map-radius: 0.75rem; --bit-Map-border: 1px solid var(--bit-clr-brd-sec)",
        }
    ];

    // ── Style & Class ─────────────────────────────────────────────────────────

    private readonly BitLeafletMapProvider styleProvider = new() { Center = new(45.4642, 9.1900), Zoom = 12 };
    private readonly BitLeafletMapProvider classProvider = new() { Center = new(45.4642, 9.1900), Zoom = 12 };
    private readonly List<BitMapMarker> styleMarkers =
    [
        new() { Id = "duomo", Position = new(45.4642, 9.1916), Alt = "Duomo", PopupText = "Duomo di Milano", TooltipText = "Duomo" },
        new() { Id = "scala", Position = new(45.4674, 9.1895), Alt = "La Scala", PopupText = "Teatro alla Scala", TooltipText = "La Scala" },
        new() { Id = "galleria", Position = new(45.4659, 9.1900), Alt = "Galleria", PopupText = "Galleria Vittorio Emanuele II", TooltipText = "Galleria" },
        new() { Id = "castello", Position = new(45.4705, 9.1793), Alt = "Castello", PopupText = "Castello Sforzesco", TooltipText = "Castello" },
        new() { Id = "brera", Position = new(45.4719, 9.1879), Alt = "Brera", PopupText = "Pinacoteca di Brera", TooltipText = "Brera" },
    ];

    // ── RTL ───────────────────────────────────────────────────────────────────

    private readonly BitLeafletMapProvider rtlProvider = new() { Center = new(35.6892, 51.3890), Zoom = 11 };
}
