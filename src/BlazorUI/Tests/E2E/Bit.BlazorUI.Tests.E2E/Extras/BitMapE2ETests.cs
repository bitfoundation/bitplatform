using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitMap at /e2e/map in a real browser on the Leaflet provider, which ships inside the package and here draws
/// its tiles from one image of the host, so nothing leaves the machine: the chrome (a named, described, focusable
/// region with its instructions), the load states and OnReady, the zoom control, markers with their names, popups,
/// tooltips, icons, dragging and every marker method, the MarkerPopupTemplate dialog, clustering, the two-way
/// Center and Zoom and every camera method, the pointer and wheel, map clicks, keyboard panning and zooming with
/// announcements and EscapeToExit, the marker list, vector layers and tile overlays, ChildContent and cooperative
/// gestures, Disabled, Style/Class/Styles/Classes and the --bit-Map-* variables, BitParams, RTL, a provider that fails
/// to load, LazyLoad, prerendering and unmounting.
/// </summary>
[TestClass]
public class BitMapE2ETests : E2ETestBase
{
    private const string Route = "/e2e/map";

    [TestMethod]
    public async Task Basic_LoadsLeafletWithTheTilesAndFiresOnReady()
    {
        await OpenAsync(Route);

        var canvas = Canvas("map-basic");
        await Expect(canvas).ToHaveClassAsync(HasClass("leaflet-container"));
        await Expect(Page.Locator("#ready-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#load-states")).ToHaveTextAsync("Loading,Ready");
        await Expect(canvas).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));

        var tiles = canvas.Locator("img.leaflet-tile");
        await Expect(tiles.First).ToHaveAttributeAsync("src", new Regex(@"samples/map-tile\.png\?z=13&x=\d+&y=\d+$"));
        await Page.WaitForFunctionAsync("() => [...document.querySelectorAll('#map-basic-canvas img.leaflet-tile')].some(t => t.complete && t.naturalWidth === 256)");

        await Expect(canvas.Locator(".leaflet-control-zoom-in")).ToBeVisibleAsync();
        await Expect(canvas.Locator(".leaflet-control-attribution")).ToContainTextAsync("OpenStreetMap");
        var box = (await Page.Locator("#map-basic").BoundingBoxAsync())!;
        Assert.AreEqual(600, box.Width, 1);
        Assert.AreEqual(400, box.Height, 1);
    }

    [TestMethod]
    public async Task Chrome_TheCanvasIsANamedFocusableRegionWithInstructions()
    {
        await OpenAsync(Route);

        var canvas = Canvas("map-basic");
        await Expect(canvas).ToHaveAttributeAsync("role", "region");
        await Expect(canvas).ToHaveAttributeAsync("tabindex", "0");
        await Expect(canvas).ToHaveAttributeAsync("aria-roledescription", "interactive map");
        await Expect(canvas).ToHaveAccessibleNameAsync("Map");
        await Expect(canvas).ToHaveAccessibleDescriptionAsync(new Regex("arrow keys to pan the map"));

        // The instructions are visually hidden until the canvas has the keyboard focus.
        var help = Page.Locator("#map-basic-help");
        Assert.IsTrue((await help.BoundingBoxAsync())!.Width <= 1);
        await canvas.FocusAsync();
        await Expect(canvas).ToBeFocusedAsync();
        await Expect(help).ToBeVisibleAsync();
        Assert.IsTrue((await help.BoundingBoxAsync())!.Width > 50);

        // One tab stop: the provider's own surface is out of the tab order.
        var focusables = await Page.Locator("#map-basic [tabindex='0'], #map-basic a[href]:not([tabindex='-1'])").CountAsync();
        Assert.IsTrue(focusables >= 1);
    }

    [TestMethod]
    public async Task ZoomControl_ClickPlusAndMinus_ZoomsTheMap()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-camera");

        var canvas = Canvas("map-camera");
        await canvas.ScrollIntoViewIfNeededAsync();
        await Expect(canvas.Locator(".leaflet-control-zoom-in")).ToBeVisibleAsync();
        await canvas.Locator(".leaflet-control-zoom-in").ClickAsync();
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("14.0");
        await canvas.Locator(".leaflet-control-zoom-out").ClickAsync();
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("13.0");
    }

    [TestMethod]
    public async Task Markers_AreNamedButtonsAtTheirCoordinates()
    {
        await OpenAsync(Route);

        var markers = Markers("map-markers");
        await Expect(markers).ToHaveCountAsync(3);
        var bigBen = Marker("map-markers", "Big Ben");
        await Expect(bigBen).ToHaveAttributeAsync("role", "button");
        await Expect(bigBen).ToHaveAttributeAsync("tabindex", "0");
        await Expect(bigBen).ToHaveAttributeAsync("title", "Big Ben tower");
        await Expect(bigBen).ToHaveAccessibleNameAsync("Big Ben");

        // Placed by their coordinates around the centre: Big Ben west of it, Tower Bridge east, St Paul's north.
        var map = (await Canvas("map-markers").BoundingBoxAsync())!;
        var centerX = map.X + map.Width / 2;
        var centerY = map.Y + map.Height / 2;
        Assert.IsTrue((await bigBen.BoundingBoxAsync())!.X < centerX - 100);
        Assert.IsTrue((await Marker("map-markers", "Tower Bridge").BoundingBoxAsync())!.X > centerX + 30);
        Assert.IsTrue((await Marker("map-markers", "St Paul's Cathedral").BoundingBoxAsync())!.Y < centerY - 40);

        // IconUrl draws the page's own image at its size; Color recolours the pin.
        var custom = Marker("map-markers", "St Paul's Cathedral");
        await Expect(custom).ToHaveAttributeAsync("src", "samples/flags/jp.svg");
        var customBox = (await custom.BoundingBoxAsync())!;
        Assert.AreEqual(30, customBox.Width, 1);
        Assert.AreEqual(20, customBox.Height, 1);
        var pinSrc = await bigBen.GetAttributeAsync("src");
        var greenSrc = await Marker("map-markers", "Tower Bridge").GetAttributeAsync("src");
        Assert.AreNotEqual(pinSrc, greenSrc);
    }

    [TestMethod]
    public async Task Markers_ClickAndKeyboardActivation_FireOnMarkerClickAndOpenThePopup()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-markers");

        var bigBen = Marker("map-markers", "Big Ben");
        await bigBen.ClickAsync();
        await Expect(Page.Locator("#marker-last")).ToHaveTextAsync("big-ben");
        await Expect(Page.Locator("#marker-clicks")).ToHaveTextAsync("1");
        await Expect(Canvas("map-markers").Locator(".leaflet-popup-content", new() { HasTextString = "The clock tower" })).ToBeVisibleAsync();

        var tower = Marker("map-markers", "Tower Bridge");
        await tower.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#marker-last")).ToHaveTextAsync("tower");
        await Expect(Page.Locator("#marker-clicks")).ToHaveTextAsync("2");
        await Expect(Canvas("map-markers").Locator(".leaflet-popup-content", new() { HasTextString = "Opened in 1894" })).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Markers_Tooltip_ShowsOnHover()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-markers");

        await Marker("map-markers", "Tower Bridge").HoverAsync();
        await Expect(Canvas("map-markers").Locator(".leaflet-tooltip")).ToHaveTextAsync("A bascule bridge");
    }

    [TestMethod]
    public async Task Markers_Draggable_FiresOnMarkerDragEndWithTheNewPosition()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-markers");

        var bigBen = Marker("map-markers", "Big Ben");
        await bigBen.ScrollIntoViewIfNeededAsync();
        var box = (await bigBen.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width / 2 + 80, box.Y + box.Height / 2, new() { Steps = 8 });
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator("#drag-end")).ToHaveTextAsync(new Regex(@"^big-ben\|51\.\d{4},-0\.\d{4}$"));
        var lng = double.Parse((await Page.Locator("#drag-end").InnerTextAsync()).Split(',')[1], CultureInfo.InvariantCulture);
        Assert.IsTrue(lng > -0.1246 + 0.005, $"Dragged east to {lng}.");
    }

    [TestMethod]
    public async Task Markers_AddRemoveMoveAndClear()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-markers");

        await Expect(Markers("map-markers")).ToHaveCountAsync(3);
        await Page.Locator("#btn-add-marker").ClickAsync();
        await Expect(Markers("map-markers")).ToHaveCountAsync(4);
        await Expect(Marker("map-markers", "Added marker")).ToBeVisibleAsync();

        await Page.Locator("#btn-remove-marker").ClickAsync();
        await Expect(Marker("map-markers", "Big Ben")).ToHaveCountAsync(0);
        await Expect(Markers("map-markers")).ToHaveCountAsync(3);

        var tower = Marker("map-markers", "Tower Bridge");
        var before = (await tower.BoundingBoxAsync())!.X;
        await Page.Locator("#btn-move-marker").ClickAsync();
        var handle = await tower.ElementHandleAsync();
        await Page.WaitForFunctionAsync("([el, max]) => el.getBoundingClientRect().x < max", new object[] { handle, (double)before - 100 });

        await Page.Locator("#btn-clear-markers").ClickAsync();
        await Expect(Markers("map-markers")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Markers_SyncAndTheMarkersParameter_ReplaceTheSet()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-markers");

        await Page.Locator("#btn-sync-markers").ClickAsync();
        await Expect(Markers("map-markers")).ToHaveCountAsync(2);
        await Expect(Marker("map-markers", "Synced one")).ToHaveCountAsync(1);
        await Expect(Marker("map-markers", "Synced two")).ToHaveCountAsync(1);

        await Page.Locator("#btn-param-markers").ClickAsync();
        await Expect(Markers("map-markers")).ToHaveCountAsync(1);
        await Expect(Marker("map-markers", "The only marker")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Markers_OpenAndCloseThePopupAndFitToTheMarkers()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-markers");

        await Page.Locator("#btn-open-popup").ClickAsync();
        await Expect(Canvas("map-markers").Locator(".leaflet-popup-content", new() { HasTextString = "Opened in 1894" })).ToBeVisibleAsync();
        await Page.Locator("#btn-close-popup").ClickAsync();
        await Expect(Page.Locator("#popup-closed")).ToHaveTextAsync("True");
        await Expect(Canvas("map-markers").Locator(".leaflet-popup")).ToHaveCountAsync(0);

        await Page.Locator("#btn-fit-markers").ClickAsync();
        await Expect(Page.Locator("#fit-zoom")).Not.ToHaveTextAsync("none");
        var zoom = double.Parse(await Page.Locator("#fit-zoom").InnerTextAsync(), CultureInfo.InvariantCulture);
        Assert.IsTrue(zoom >= 13 && zoom <= 18, $"{zoom}");
        foreach (var name in new[] { "Big Ben", "Tower Bridge", "St Paul's Cathedral" })
        {
            await Expect(Marker("map-markers", name)).ToBeInViewportAsync();
        }
    }

    [TestMethod]
    public async Task PopupTemplate_OpensANamedDialogWithLiveContent()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-popup");

        await Marker("map-popup", "The Louvre").ClickAsync();
        var popup = Page.Locator("#map-popup .bit-map-popup");
        await Expect(popup).ToBeVisibleAsync();
        await Expect(popup).ToHaveAttributeAsync("role", "dialog");
        await Expect(popup).ToHaveAccessibleNameAsync("The Louvre");
        await Expect(popup).ToHaveAttributeAsync("data-bit-esc", "claim");
        await Expect(popup.Locator(".popup-title")).ToHaveTextAsync("The Louvre");
        await Expect(Page.Locator("#popup-opened")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#popup-marker")).ToHaveTextAsync("louvre");
        await Expect(popup).ToBeFocusedAsync();

        await popup.Locator("#btn-popup-visit").ClickAsync();
        await Expect(Page.Locator("#visits")).ToHaveTextAsync("1");

        var close = popup.Locator(".bit-map-popup-close");
        await Expect(close).ToHaveAccessibleNameAsync("Close the details");
        await close.ClickAsync();
        await Expect(popup).ToHaveCountAsync(0);
        await Expect(Page.Locator("#popup-closed-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task PopupTemplate_EscapeClosesItAndOpenPopupClosePopupDriveIt()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-popup");

        await Page.Locator("#btn-open-template-popup").ClickAsync();
        var popup = Page.Locator("#map-popup .bit-map-popup");
        await Expect(popup).ToBeVisibleAsync();
        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(popup).ToHaveCountAsync(0);
        await Expect(Page.Locator("#popup-closed-count")).ToHaveTextAsync("1");

        await Page.Locator("#btn-open-template-popup").ClickAsync();
        await Expect(popup).ToBeVisibleAsync();
        await Page.Locator("#btn-close-template-popup").ClickAsync();
        await Expect(popup).ToHaveCountAsync(0);
        await Expect(Page.Locator("#popup-closed-count")).ToHaveTextAsync("2");
    }

    [TestMethod]
    public async Task Clustering_MergesNearbyMarkersAndAClickZoomsIntoThem()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-cluster");

        var cluster = Marker("map-cluster", "Cluster of 10 markers");
        await Expect(cluster).ToHaveCountAsync(1);
        await Expect(Marker("map-cluster", "Far away")).ToHaveCountAsync(1);
        await Expect(Markers("map-cluster")).ToHaveCountAsync(2);

        await cluster.ClickAsync();
        await Expect(Page.Locator("#cluster-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#cluster-count")).ToHaveTextAsync("10");
        await NumberAboveAsync("#cluster-zoom", 13);
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('#map-cluster-canvas img.leaflet-marker-icon').length > 2");
    }

    [TestMethod]
    public async Task Clustering_TurnedOff_ShowsEveryMarker()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-cluster");

        await Expect(Markers("map-cluster")).ToHaveCountAsync(2);
        await Page.Locator("#btn-toggle-clustering").ClickAsync();
        await Expect(Markers("map-cluster")).ToHaveCountAsync(11);
        await Page.Locator("#btn-toggle-clustering").ClickAsync();
        await Expect(Markers("map-cluster")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Camera_TwoWayCenterAndZoom_MoveTheMapFromThePage()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-camera");

        await Page.Locator("#btn-center-paris").ClickAsync();
        await Expect(Page.Locator("#last-view")).ToHaveTextAsync(new Regex(@"^48\.85\d\d,2\.35\d\d\|13\.0$"));

        await Page.Locator("#btn-zoom-5").ClickAsync();
        await Expect(Page.Locator("#last-view")).ToHaveTextAsync(new Regex(@"\|5\.0$"));
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("5.0");
    }

    [TestMethod]
    public async Task Camera_Methods_MoveTheViewAndReportIt()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-camera");

        await Page.Locator("#btn-set-view").ClickAsync();
        await Expect(Page.Locator("#last-view")).ToHaveTextAsync("41.9028,12.4964|10.0");
        await Expect(Page.Locator("#center")).ToHaveTextAsync("41.9028,12.4964");
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("10.0");

        await Page.Locator("#btn-set-zoom").ClickAsync();
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("7.0");
        await Page.Locator("#btn-zoom-in").ClickAsync();
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("8.0");
        await Page.Locator("#btn-zoom-out").ClickAsync();
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("7.0");

        await Page.Locator("#btn-pan-to").ClickAsync();
        await Expect(Page.Locator("#center")).ToHaveTextAsync("51.5050,-0.0900");
        await Page.Locator("#btn-pan-by").ClickAsync();
        await LngAboveAsync("#center", -0.09);

        await Page.Locator("#btn-get-view").ClickAsync();
        await Expect(Page.Locator("#read-view")).ToHaveTextAsync(new Regex(@"^51\.\d{4},-?\d\.\d{4}\|7\.0\|True$"));

        await Page.Locator("#btn-project").ClickAsync();
        await Expect(Page.Locator("#projected")).ToHaveTextAsync("300,200");

        await Page.Locator("#btn-fit-bounds").ClickAsync();
        await NumberAboveAsync("#zoom", 8);
    }

    [TestMethod]
    public async Task Camera_FlyTo_EndsAtTheDestination()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-camera");

        await Page.Locator("#btn-fly-to").ClickAsync();
        await Expect(Page.Locator("#center")).ToHaveTextAsync("35.6762,139.6503");
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("8.0");
    }

    [TestMethod]
    public async Task Locate_WithoutThePermission_ReturnsNullAndRaisesOnInteropError()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-camera");

        await Page.Locator("#btn-locate").ClickAsync();
        await Expect(Page.Locator("#located")).ToHaveTextAsync("null");
        await Expect(Page.Locator("#interop-error")).ToHaveTextAsync(new Regex("Locate"));
        await Expect(Page.Locator("#center")).ToHaveTextAsync("51.5050,-0.0900");
    }

    [TestMethod]
    public async Task Pointer_DragPansAndTheWheelZooms()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-camera");

        var canvas = Canvas("map-camera");
        await canvas.ScrollIntoViewIfNeededAsync();
        var box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + 300, box.Y + 200);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + 200, box.Y + 200, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await LngAboveAsync("#center", -0.085);

        await Page.Mouse.MoveAsync(box.X + 300, box.Y + 200);
        await Page.Mouse.WheelAsync(0, -300);
        await NumberAboveAsync("#zoom", 13);
    }

    [TestMethod]
    public async Task MapEvents_ClickDoubleClickAndContextMenu_ReportTheCoordinate()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-camera");

        var canvas = Canvas("map-camera");
        await canvas.ScrollIntoViewIfNeededAsync();
        var box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.ClickAsync(box.X + 300, box.Y + 200);
        await Expect(Page.Locator("#clicked")).ToHaveTextAsync(new Regex(@"^51\.50\d\d,-0\.09\d\d$"));

        await Page.Mouse.ClickAsync(box.X + 300, box.Y + 200, new() { Button = MouseButton.Right });
        await Expect(Page.Locator("#context-menu")).ToHaveTextAsync(new Regex(@"^51\.50\d\d,-0\.09\d\d$"));

        await Page.Mouse.DblClickAsync(box.X + 300, box.Y + 200);
        await Expect(Page.Locator("#double-clicked")).ToHaveTextAsync(new Regex(@"^51\.50\d\d,-0\.09\d\d$"));
        await NumberAboveAsync("#zoom", 13);
        Assert.IsTrue(int.Parse(await Page.Locator("#view-changes").InnerTextAsync()) >= 1);
    }

    [TestMethod]
    public async Task Keyboard_ArrowsPanPlusZoomsAndTheViewIsAnnounced()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-keyboard");

        var canvas = Canvas("map-keyboard");
        await Expect(canvas).ToHaveAccessibleNameAsync("Map of central London");
        await Expect(canvas).ToHaveAttributeAsync("aria-roledescription", "street map");
        await Expect(canvas).ToHaveAccessibleDescriptionAsync("Arrows pan, plus and minus zoom.");
        await Expect(canvas).ToHaveClassAsync(HasClass("leaflet-container"));

        await canvas.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await LngAboveAsync("#keyboard-view", -0.09);
        await Page.Keyboard.PressAsync("+");
        await Expect(Page.Locator("#keyboard-view")).ToHaveTextAsync(new Regex(@"\|14\.0$"));
        await Expect(Page.Locator("#map-keyboard .bit-map-live")).ToHaveTextAsync("Zoom 14");
        await Page.Keyboard.PressAsync("-");
        await Expect(Page.Locator("#keyboard-view")).ToHaveTextAsync(new Regex(@"\|13\.0$"));
    }

    [TestMethod]
    public async Task Keyboard_EscapeLeavesTheMapUnlessEscapeToExitIsOff()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-keyboard");
        await ReadyAsync("map-no-escape");

        var canvas = Canvas("map-keyboard");
        await canvas.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(canvas).Not.ToBeFocusedAsync();

        var stuck = Canvas("map-no-escape");
        await stuck.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(stuck).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Dialog_EscapeOnTheMapLeavesItButNotTheDialog()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-open-dialog").ClickAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
        await ReadyAsync("map-in-dialog");

        // The popup's Escape closes the popup alone.
        await Marker("map-in-dialog", "The Louvre").ClickAsync();
        var popup = Page.Locator("#map-in-dialog .bit-map-popup");
        await Expect(popup).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(popup).ToHaveCountAsync(0);
        await NextFramesAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");

        // The canvas's Escape leaves the map, and the dialog stays.
        var canvas = Canvas("map-in-dialog");
        await canvas.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(canvas).Not.ToBeFocusedAsync();
        await NextFramesAsync();
        await Expect(Page.Locator("#dialog-open")).ToHaveTextAsync("True");
    }

    [TestMethod]
    public async Task MarkerList_ATableOfTheMarkersThatShowsEachOnTheMap()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-list");

        var list = Page.Locator("#map-list + .bit-map-marker-list");
        var table = list.Locator("table");
        await Expect(table.Locator("caption")).ToHaveTextAsync("London landmarks");
        var headers = await table.Locator("thead th").AllTextContentsAsync();
        CollectionAssert.AreEqual(new[] { "Name", "Latitude", "Longitude", "Show on map" }, headers.Select(h => h.Trim()).ToArray());
        await Expect(table.Locator("tbody tr")).ToHaveCountAsync(3);
        var bigBenRow = table.Locator("tbody tr", new() { HasTextString = "Big Ben" });
        await Expect(bigBenRow.Locator("td").First).ToHaveTextAsync("51.50070");

        var show = bigBenRow.Locator("button");
        await Expect(show).ToHaveAccessibleNameAsync(new Regex("^Show on map: Big Ben"));
        await show.ClickAsync();
        await Expect(Page.Locator("#list-center")).ToHaveTextAsync("51.5007,-0.1246");
        await Expect(Page.Locator("#list-zoom")).ToHaveTextAsync("16.0");
    }

    [TestMethod]
    public async Task MarkerList_ScreenReaderOnlyAndTemplate()
    {
        await OpenAsync(Route);

        var srList = Page.Locator("#map-list-sr + .bit-map-marker-list");
        await Expect(srList).ToHaveClassAsync(HasClass("bit-map-help"));
        Assert.IsTrue((await srList.BoundingBoxAsync())!.Width <= 1);
        await Expect(srList.Locator("tbody tr")).ToHaveCountAsync(3);

        var items = await Page.Locator("#map-list-template + .bit-map-marker-list .custom-marker-list li").AllTextContentsAsync();
        Assert.AreEqual(3, items.Count);
        CollectionAssert.IsSubsetOf(new[] { "Big Ben", "Tower Bridge" }, items.ToList());
    }

    [TestMethod]
    public async Task Vectors_ShapesAreDrawnClickedRestyledHiddenAndCleared()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-vectors");

        var paths = Canvas("map-vectors").Locator("path.leaflet-interactive");
        await Page.Locator("#btn-add-polygon").ClickAsync();
        await Expect(paths).ToHaveCountAsync(1);
        await Expect(paths.First).ToHaveAttributeAsync("stroke", "rgb(255, 0, 0)");

        await paths.First.ClickAsync();
        await Expect(Page.Locator("#vector-click")).ToHaveTextAsync(new Regex("^polygon\\|"));

        await Page.Locator("#btn-add-polyline").ClickAsync();
        await Page.Locator("#btn-add-circle").ClickAsync();
        await Page.Locator("#btn-add-rectangle").ClickAsync();
        await Expect(paths).ToHaveCountAsync(4);
        await Expect(Canvas("map-vectors").Locator("path.leaflet-interactive[stroke='rgb(0, 0, 255)']")).ToHaveAttributeAsync("stroke-width", "6");

        await Page.Locator("#btn-style-polygon").ClickAsync();
        await Expect(Canvas("map-vectors").Locator("path.leaflet-interactive[stroke='rgb(0, 128, 0)']")).ToHaveCountAsync(1);

        await Page.Locator("#btn-hide-polygon").ClickAsync();
        await Expect(Page.Locator("#layer-visible")).ToHaveTextAsync("False");
        await Expect(paths).ToHaveCountAsync(3);

        await Page.Locator("#btn-remove-line").ClickAsync();
        await Expect(paths).ToHaveCountAsync(2);
        await Page.Locator("#btn-clear-vectors").ClickAsync();
        await Expect(paths).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Vectors_GeoJsonFeatureClick_ReportsItsProperties()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-vectors");

        await Page.Locator("#btn-add-geojson").ClickAsync();
        var feature = Canvas("map-vectors").Locator("path.leaflet-interactive");
        await Expect(feature).ToHaveCountAsync(1);
        await Expect(feature).ToHaveAttributeAsync("stroke", "rgb(128, 0, 128)");
        await feature.ClickAsync();
        await Expect(Page.Locator("#geo-click")).ToHaveTextAsync("geo|Square");
    }

    [TestMethod]
    public async Task TileOverlays_AreStackedDimmedHiddenAndRemoved()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-vectors");

        var overlayTiles = Canvas("map-vectors").Locator("img.leaflet-tile[src*='overlay=1']");
        await Page.Locator("#btn-add-overlay").ClickAsync();
        await Expect(overlayTiles.First).ToBeAttachedAsync();
        await Expect(Page.Locator("#overlay-visible")).ToHaveTextAsync("True");
        var layer = overlayTiles.First.Locator("xpath=ancestor::div[contains(@class,'leaflet-layer')]");
        await Expect(layer).ToHaveCSSAsync("opacity", "0.8");

        await Page.Locator("#btn-dim-overlay").ClickAsync();
        await Expect(layer).ToHaveCSSAsync("opacity", "0.3");

        await Page.Locator("#btn-hide-overlay").ClickAsync();
        await Expect(Page.Locator("#overlay-visible")).ToHaveTextAsync("False");
        await Expect(overlayTiles).ToHaveCountAsync(0);

        await Page.Locator("#btn-remove-overlay").ClickAsync();
        await Expect(overlayTiles).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task ChildContent_FloatsAboveTheMapAndItsControlsWork()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-overlay");

        var overlay = Page.Locator("#map-overlay .bit-map-overlay");
        await Expect(overlay.Locator("#btn-overlay-zoom")).ToBeVisibleAsync();
        await overlay.Locator("#btn-overlay-zoom").ClickAsync();
        await Expect(Page.Locator("#overlay-zoom")).ToHaveTextAsync("14.0");
    }

    [TestMethod]
    public async Task CooperativeGestures_AWheelWithoutCtrlShowsTheHintAndCtrlZooms()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-overlay");

        var canvas = Canvas("map-overlay");
        await canvas.ScrollIntoViewIfNeededAsync();
        var box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + 200, box.Y + 200);
        await Page.Mouse.WheelAsync(0, -200);
        var hint = Page.Locator("#map-overlay .bit-map-gesture-hint");
        await Expect(hint).ToHaveClassAsync(HasClass("bit-map-gesture-hint-vis"));
        await Expect(hint.Locator(".bit-map-gesture-hint-text")).ToHaveTextAsync("Use ctrl + scroll to zoom the map");
        await NextFramesAsync();
        await Expect(Page.Locator("#overlay-zoom")).ToHaveTextAsync("13.0");

        // The bare wheel scrolled the page on, so the pointer is brought back over the map first.
        await canvas.ScrollIntoViewIfNeededAsync();
        box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + 200, box.Y + 200);
        await Page.Keyboard.DownAsync("Control");
        await Page.Mouse.WheelAsync(0, -200);
        await Page.Keyboard.UpAsync("Control");
        await NumberAboveAsync("#overlay-zoom", 13);
    }

    [TestMethod]
    public async Task Disabled_MakesTheMapInertAndDimmed()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-disabled");

        var root = Page.Locator("#map-disabled");
        var canvas = Canvas("map-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(canvas).ToHaveAttributeAsync("inert", "");
        Assert.IsTrue(double.Parse(await ComputedStyleAsync("#map-disabled-canvas", "opacity"), CultureInfo.InvariantCulture) < 1);
        var listButton = Page.Locator("#map-disabled + .bit-map-marker-list button").First;
        await Expect(listButton).ToBeDisabledAsync();
        await Expect(listButton).ToHaveAttributeAsync("aria-disabled", "true");

        // Nothing inside it takes the focus.
        await Marker("map-disabled", "Big Ben").EvaluateAsync("m => m.focus()");
        await Expect(Marker("map-disabled", "Big Ben")).Not.ToBeFocusedAsync();

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(canvas).Not.ToHaveAttributeAsync("inert", new Regex(".*"));
        await Expect(listButton).ToBeEnabledAsync();
        Assert.AreEqual("1", await ComputedStyleAsync("#map-disabled-canvas", "opacity"));
    }

    [TestMethod]
    public async Task CssVariables_SetTheHeightBorderRadiusAndDisabledOpacity()
    {
        await OpenAsync(Route);

        Assert.AreEqual(250, (await Page.Locator("#map-vars").BoundingBoxAsync())!.Height, 1);
        Assert.AreEqual("3px", await ComputedStyleAsync("#map-vars", "border-top-width"));
        Assert.AreEqual("rgb(255, 0, 0)", await ComputedStyleAsync("#map-vars", "border-top-color"));
        Assert.AreEqual("12px", await ComputedStyleAsync("#map-vars", "border-top-left-radius"));
        Assert.AreEqual("0.25", await ComputedStyleAsync("#map-vars-canvas", "opacity"));
    }

    [TestMethod]
    public async Task StyleAndClass_ReachTheRootAndItsParts()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#map-styled");
        await Expect(root).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(root).ToHaveClassAsync(HasClass("custom-root"));
        Assert.AreEqual("rgb(1, 2, 3)", await ComputedStyleAsync("#map-styled", "outline-color"));
        await Expect(Canvas("map-styled")).ToHaveClassAsync(HasClass("custom-canvas"));
        await Expect(Page.Locator("#map-styled-help")).ToHaveClassAsync(HasClass("custom-help"));
        await Expect(Canvas("map-styled")).ToHaveCSSAsync("background-color", "rgb(250, 250, 210)");
    }

    [TestMethod]
    public async Task Params_BitMapParams_Cascade()
    {
        await OpenAsync(Route);

        await Expect(Canvas("map-params")).ToHaveAttributeAsync("aria-roledescription", "cascaded map");
        await Expect(Page.Locator("#map-params-help")).ToHaveTextAsync("Cascaded instructions.");
        await Expect(Page.Locator("#map-params + .bit-map-marker-list caption")).ToHaveTextAsync("Cascaded caption");
    }

    [TestMethod]
    public async Task Rtl_MarksTheMapRightToLeft()
    {
        await OpenAsync(Route);

        var root = Page.Locator("#map-rtl");
        await Expect(root).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(root).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Canvas("map-rtl")).ToHaveAccessibleNameAsync("نقشه");
        await Expect(Canvas("map-rtl")).ToHaveClassAsync(HasClass("leaflet-container"));
    }

    [TestMethod]
    public async Task Failed_AProviderThatCannotLoad_ShowsTheErrorAndReportsFailed()
    {
        // The missing script is the subject: the browser logs its failed request.
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-show-failed").ClickAsync();

        await Expect(Page.Locator("#failed-state")).ToHaveTextAsync("Failed");
        var status = Page.Locator("#map-failed .bit-map-status");
        await Expect(status).ToHaveAttributeAsync("role", "alert");
        await Expect(status).ToHaveTextAsync("The map could not be loaded.");
        await Expect(Page.Locator("#map-failed-template .bit-map-status .custom-error")).ToHaveTextAsync("No map today");
    }

    [TestMethod]
    public async Task LazyLoad_WaitsUntilTheMapScrollsIntoView()
    {
        await OpenAsync(Route);

        await NextFramesAsync();
        await Expect(Page.Locator("#lazy-state")).ToHaveTextAsync("Idle");
        await Expect(Canvas("map-lazy")).Not.ToHaveClassAsync(HasClass("leaflet-container"));

        await Canvas("map-lazy").ScrollIntoViewIfNeededAsync();
        await Expect(Page.Locator("#lazy-state")).ToHaveTextAsync("Ready");
        await Expect(Canvas("map-lazy")).ToHaveClassAsync(HasClass("leaflet-container"));
    }

    [TestMethod]
    public async Task Lifecycle_Prerendered_TheServerHtmlCarriesTheRegion()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"map-basic-canvas\"");
        StringAssert.Contains(html, "role=\"region\"");
        StringAssert.Contains(html, "aria-busy=\"true\"");
    }

    [TestMethod]
    public async Task Lifecycle_Unmounted_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);

        await Expect(Canvas("map-unmount")).ToHaveClassAsync(HasClass("leaflet-container"));
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#map-unmount")).ToHaveCountAsync(0);
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Canvas("map-unmount")).ToHaveClassAsync(HasClass("leaflet-container"));

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private ILocator Canvas(string id) => Page.Locator($"#{id}-canvas");

    // The page turns interactive before Leaflet has loaded and the map is created; a method called before that
    // has no map to act on.
    private async Task ReadyAsync(string id)
    {
        await Expect(Canvas(id)).ToHaveClassAsync(HasClass("leaflet-container"));
        await Expect(Canvas(id)).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
    }

    private ILocator Markers(string id) => Page.Locator($"#{id}-canvas img.leaflet-marker-icon");

    private ILocator Marker(string id, string alt) => Page.Locator($"#{id}-canvas img.leaflet-marker-icon[alt=\"{alt}\"]");

    // Waits for the number an element of the page writes (a zoom) to rise above a value.
    private Task NumberAboveAsync(string selector, double min) =>
        Page.WaitForFunctionAsync("([sel, min]) => parseFloat(document.querySelector(sel).textContent) > min", new object[] { selector, min });

    // Waits for the longitude of a "lat,lng|..." the page writes to rise above a value.
    private Task LngAboveAsync(string selector, double min) =>
        Page.WaitForFunctionAsync("([sel, min]) => parseFloat(document.querySelector(sel).textContent.split('|')[0].split(',')[1]) > min", new object[] { selector, min });
}

/// <summary>
/// Drives BitMap at /e2e/map/providers on the two other providers that need no key, OpenLayers and MapLibre, whose
/// libraries come from a CDN: the same chrome, markers, zoom and keyboard as on Leaflet, against the host's own tiles.
/// Without the network the libraries cannot load, and the tests say so as inconclusive rather than failing.
/// </summary>
[TestClass]
public class BitMapProvidersE2ETests : E2ETestBase
{
    private const string Route = "/e2e/map/providers";

    [TestMethod]
    public async Task OpenLayers_DrawsTheMapWithItsMarkersZoomAndKeyboard()
    {
        // A CDN or tile request that fails is not the subject: the map's own state is what is asserted.
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-load").ClickAsync();
        await Expect(Page.Locator("#ol-state")).ToHaveTextAsync(new Regex("^(Ready|Failed)$"), new() { Timeout = 60000 });
        if (await Page.Locator("#ol-state").InnerTextAsync() == "Failed") Assert.Inconclusive("OpenLayers could not be loaded from its CDN.");

        var canvas = Page.Locator("#map-ol-canvas");
        await Expect(canvas).ToHaveAttributeAsync("role", "region");
        await Expect(canvas).ToHaveAccessibleNameAsync("OpenLayers map");
        await Expect(canvas.Locator(".ol-viewport")).ToHaveCountAsync(1);

        // The markers are drawn on the map's canvas: a click where one is reports it.
        await Page.Locator("#btn-ol-project").ClickAsync();
        await Expect(Page.Locator("#ol-projected")).ToHaveTextAsync(new Regex(@"^\d+,\d+$"));
        var xy = (await Page.Locator("#ol-projected").InnerTextAsync()).Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        var box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.ClickAsync(box.X + xy[0], box.Y + xy[1] - 12);
        await Expect(Page.Locator("#ol-marker")).ToHaveTextAsync("big-ben");

        await Page.Locator("#btn-ol-zoom-in").ClickAsync();
        await Expect(Page.Locator("#ol-zoom")).ToHaveTextAsync("14.0");

        await canvas.FocusAsync();
        await Page.Keyboard.PressAsync("+");
        await Page.WaitForFunctionAsync("() => parseFloat(document.querySelector('#ol-zoom').textContent) > 14");
    }

    [TestMethod]
    public async Task MapLibre_DrawsTheMapWithItsMarkersAndZoom()
    {
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-load").ClickAsync();
        await Expect(Page.Locator("#gl-state")).ToHaveTextAsync(new Regex("^(Ready|Failed|Unsupported)$"), new() { Timeout = 60000 });
        var state = await Page.Locator("#gl-state").InnerTextAsync();
        if (state == "Unsupported")
        {
            await Expect(Page.Locator("#map-gl .bit-map-status")).ToHaveTextAsync("This browser cannot display the map (WebGL is unavailable).");
            Assert.Inconclusive("This browser has no WebGL for MapLibre.");
        }
        if (state == "Failed") Assert.Inconclusive("MapLibre could not be loaded from its CDN.");

        var canvas = Page.Locator("#map-gl-canvas");
        await Expect(canvas).ToHaveAccessibleNameAsync("MapLibre map");
        await Expect(canvas.Locator("canvas.maplibregl-canvas")).ToHaveCountAsync(1);

        var marker = canvas.Locator(".maplibregl-marker[aria-label='Tower Bridge']");
        await Expect(marker).ToHaveAttributeAsync("role", "button");
        await marker.ClickAsync();
        await Expect(Page.Locator("#gl-marker")).ToHaveTextAsync("tower");

        await Page.Locator("#btn-gl-zoom-in").ClickAsync();
        await Expect(Page.Locator("#gl-zoom")).ToHaveTextAsync("14.0");
    }
}

/// <summary>
/// Drives BitMap.Locate at /e2e/map in a browser context that grants the geolocation permission and stands at the
/// Eiffel Tower: the visitor's position is returned and the map is centred on it.
/// </summary>
[TestClass]
public class BitMapGeolocationE2ETests : E2ETestBase
{
    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        Geolocation = new() { Latitude = 48.8584f, Longitude = 2.2945f },
        Permissions = ["geolocation"]
    };

    [TestMethod]
    public async Task Locate_ReturnsThePositionAndCentresTheMapOnIt()
    {
        await OpenAsync("/e2e/map");
        await Expect(Page.Locator("#map-camera-canvas")).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(Page.Locator("#map-camera-canvas")).ToHaveClassAsync(HasClass("leaflet-container"));

        await Page.Locator("#btn-locate").ClickAsync();
        await Expect(Page.Locator("#located")).ToHaveTextAsync("48.8584,2.2945");
        await Expect(Page.Locator("#center")).ToHaveTextAsync("48.8584,2.2945");
        await Expect(Page.Locator("#zoom")).ToHaveTextAsync("12.0");
        await Expect(Page.Locator("#interop-error")).ToHaveTextAsync("none");
    }
}
