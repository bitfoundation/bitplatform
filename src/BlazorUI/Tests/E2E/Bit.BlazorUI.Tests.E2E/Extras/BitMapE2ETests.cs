using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitMap at /e2e/map in a real browser on the Leaflet provider, which ships inside the package and here draws
/// its tiles from one image of the host, so nothing leaves the machine: the chrome (a named, described, focusable
/// region with its instructions), the load states and OnReady, the zoom control, markers with their names, popups,
/// tooltips, icons, dragging and every marker method, markers updated where they stand (their element, focus, open
/// popup, tooltip and order kept), the MarkerPopupTemplate dialog, clustering, the two-way
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
    public async Task Markers_DoubleClick_FiresOnMarkerDoubleClickAndNeitherReachesNorZoomsTheMap()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-markers");

        // Leaflet reports both clicks of the pair as they land, then the double-click.
        await Marker("map-markers", "Tower Bridge").DblClickAsync();
        await Expect(Page.Locator("#marker-double-last")).ToHaveTextAsync("tower");
        await Expect(Page.Locator("#marker-double-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#marker-clicks")).ToHaveTextAsync("2");

        // The map's OnDoubleClick would have been raised by the same event, so once a later round trip has been
        // rendered it would show.
        await Page.Locator("#btn-markers-ping").ClickAsync();
        await Expect(Page.Locator("#markers-pings")).ToHaveTextAsync("1");
        Assert.AreEqual("0", await Page.Locator("#markers-map-double-clicks").InnerTextAsync());

        // Away from the markers the double-click is the map's - which is what makes the 0 above mean something - and
        // it zooms one level in, from 13: a 14 says the marker's double-click zoomed nothing.
        var canvas = Canvas("map-markers");
        await canvas.ScrollIntoViewIfNeededAsync();
        var box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.DblClickAsync(box.X + 300, box.Y + 330);
        await Expect(Page.Locator("#markers-map-double-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#markers-zoom")).ToHaveTextAsync("14.0");
        Assert.AreEqual("1", await Page.Locator("#marker-double-clicks").InnerTextAsync());
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
    public async Task MarkerUpdates_KeepTheElementItsFocusAndItsOpenPopup()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-updates");

        var alpha = Marker("map-updates", "Alpha");
        await Page.EvaluateAsync("el => window.__alpha = el", await alpha.ElementHandleAsync());
        await alpha.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        var popup = Canvas("map-updates").Locator(".leaflet-popup-content");
        await Expect(popup).ToHaveTextAsync("Alpha one");
        await Expect(alpha).ToBeFocusedAsync();

        // A new title and popup text: the marker keeps its element, the focus stays on it, and its popup stays open
        // with the new text in it.
        await Page.Locator("#btn-upd-restyle").DispatchEventAsync("click");
        await Expect(popup).ToHaveTextAsync("Alpha two");
        await Expect(alpha).ToHaveAttributeAsync("title", "Alpha, restyled");
        await Expect(alpha).ToBeFocusedAsync();
        Assert.IsTrue(await alpha.EvaluateAsync<bool>("el => el === window.__alpha"));
    }

    [TestMethod]
    public async Task MarkerUpdates_DroppingThePopupClosesTheOpenOne()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-updates");

        var alpha = Marker("map-updates", "Alpha");
        var popup = Canvas("map-updates").Locator(".leaflet-popup");
        await alpha.ClickAsync();
        await Expect(popup).ToHaveCountAsync(1);

        await Page.Locator("#btn-upd-drop-popup").DispatchEventAsync("click");
        await Expect(popup).ToHaveCountAsync(0);

        // The marker has no popup left for a click to open either.
        await alpha.ClickAsync();
        await UpdatesPingAsync();
        await Expect(popup).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task MarkerUpdates_ATooltipBoundAgainOpensAndClosesAsItIsNow()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-updates");

        var alpha = Marker("map-updates", "Alpha");
        var tooltip = Canvas("map-updates").Locator(".leaflet-tooltip");
        await alpha.FocusAsync();
        await Expect(tooltip).ToHaveTextAsync("Alpha tip");
        await alpha.BlurAsync();
        await Expect(tooltip).ToHaveCountAsync(0);

        // Made permanent, it stays open whatever the focus does - no listener of the hover tooltip it was is left to
        // close it.
        await Page.Locator("#btn-upd-tooltip-permanent").DispatchEventAsync("click");
        await Expect(tooltip).ToHaveTextAsync("Alpha tip");
        await alpha.FocusAsync();
        await alpha.BlurAsync();
        await UpdatesPingAsync();
        await Expect(tooltip).ToHaveTextAsync("Alpha tip");

        // Taken away, nothing opens on focus, nothing throws for the tooltip that is gone, and nothing points at it.
        await Page.Locator("#btn-upd-drop-tooltip").DispatchEventAsync("click");
        await Expect(tooltip).ToHaveCountAsync(0);
        await alpha.FocusAsync();
        await alpha.BlurAsync();
        await UpdatesPingAsync();
        await Expect(tooltip).ToHaveCountAsync(0);
        await Expect(alpha).Not.ToHaveAttributeAsync("aria-describedby", new Regex(".*"));
    }

    [TestMethod]
    public async Task MarkerUpdates_AMovedMarkerDoesNotPanTheMapBackToItsOpenPopup()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-updates");

        var alpha = Marker("map-updates", "Alpha");
        var popup = Canvas("map-updates").Locator(".leaflet-popup");
        await alpha.ClickAsync();
        await Expect(popup).ToHaveCountAsync(1);

        // The user goes elsewhere, leaving the popup out of sight.
        const string paneTransform = "() => document.querySelector('#map-updates-canvas .leaflet-map-pane').style.transform";
        var before = await Page.EvaluateAsync<string>(paneTransform);
        await Page.Locator("#btn-upd-pan-away").DispatchEventAsync("click");
        await Page.WaitForFunctionAsync($"before => ({paneTransform})() !== before", before);
        var away = await Page.EvaluateAsync<string>(paneTransform);

        // The marker moves, its popup open along with it, and the map stays where the user took it.
        var popupAt = await popup.EvaluateAsync<string>("el => el.style.transform");
        await Page.Locator("#btn-upd-move").DispatchEventAsync("click");
        await Page.WaitForFunctionAsync("before => document.querySelector('#map-updates-canvas .leaflet-popup').style.transform !== before", popupAt);
        // Well past the quarter of a second Leaflet's pan to a popup takes.
        await Page.WaitForTimeoutAsync(600);
        Assert.AreEqual(away, await Page.EvaluateAsync<string>(paneTransform));
        await Expect(popup).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task MarkerUpdates_SyncMarkersKeepsTheMarkersItKeepsAndPutsThemInItsOrder()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-updates");

        const string alts = "() => [...document.querySelectorAll('#map-updates-canvas img.leaflet-marker-icon')].map(e => e.alt).join(',')";
        Assert.AreEqual("Alpha,Bravo,Charlie", await Page.EvaluateAsync<string>(alts));
        await Page.EvaluateAsync("() => window.__markers = [...document.querySelectorAll('#map-updates-canvas img.leaflet-marker-icon')]");

        // The same three the other way round: each keeps its element, the focused one keeps the focus, and the
        // keyboard reaches them in the new order.
        var bravo = Marker("map-updates", "Bravo");
        await bravo.FocusAsync();
        await Page.Locator("#btn-upd-sync-reversed").DispatchEventAsync("click");
        await Page.WaitForFunctionAsync($"() => ({alts})() === 'Charlie,Bravo,Alpha'");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__markers.every(e => e.isConnected)"));
        await Expect(bravo).ToBeFocusedAsync();

        // A set that drops two of them and brings one: only those two are taken off.
        await Page.Locator("#btn-upd-sync-replace").DispatchEventAsync("click");
        await Page.WaitForFunctionAsync($"() => ({alts})() === 'Bravo,Delta'");
        Assert.IsTrue(await bravo.EvaluateAsync<bool>("el => el === window.__markers[1]"));
        await Expect(bravo).ToBeFocusedAsync();
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
    public async Task Clustering_DoubleClickOnABubble_IsOneClickOfIt()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-cluster");

        // Both clicks of the pair land on the bubble, which is still there while the first one's zoom plays - but it
        // expands once, and the double-click is not the map's either.
        await Marker("map-cluster", "Cluster of 10 markers").DblClickAsync();
        await Expect(Page.Locator("#cluster-clicks")).ToHaveTextAsync("1");
        await NumberAboveAsync("#cluster-zoom", 13);

        // A round trip of the page's own: once it has been rendered, so has every callback the map raised before it.
        await Page.Locator("#btn-markers-ping").ClickAsync();
        await Expect(Page.Locator("#markers-pings")).ToHaveTextAsync("1");
        Assert.AreEqual("1", await Page.Locator("#cluster-clicks").InnerTextAsync());
        Assert.AreEqual("0", await Page.Locator("#cluster-map-double-clicks").InnerTextAsync());
    }

    [TestMethod]
    public async Task Markers_DoubleClick_WithoutOnMarkerDoubleClick_IsTheMaps()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-cluster");

        // The clustering map has no OnMarkerDoubleClick, so double-clicking a marker of its is what it always was: the
        // map's OnDoubleClick and one level in, from 13.
        await Marker("map-cluster", "Far away").DblClickAsync();
        await Expect(Page.Locator("#cluster-map-double-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#cluster-zoom")).ToHaveTextAsync("14.0");
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

    // Leaflet draws its zoom buttons in the top-start corner, above the overlay, so the overlay's children start below
    // them - and back at the top once the buttons are switched off (#14317).
    [TestMethod]
    public async Task ChildContent_StartsBelowTheZoomButtonsWhileTheyShow()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-overlay");

        var overlay = Page.Locator("#map-overlay .bit-map-overlay");
        var zoomControl = Page.Locator("#map-overlay .leaflet-control-zoom");
        var canvasBox = (await Canvas("map-overlay").BoundingBoxAsync())!;
        var zoomBox = (await zoomControl.BoundingBoxAsync())!;
        var room = zoomBox.Y + zoomBox.Height - canvasBox.Y;
        await Expect(overlay).Not.ToHaveCSSAsync("padding-top", "0px");
        Assert.AreEqual(room, await PaddingTopAsync(overlay), 1);
        Assert.IsTrue((await Page.Locator("#overlay-badge").BoundingBoxAsync())!.Y >= zoomBox.Y + zoomBox.Height - 1,
            "The badge starts under the zoom buttons, which draw over it");

        await Page.Locator("#btn-toggle-overlay-zoom-control").ClickAsync();
        await Expect(zoomControl).ToHaveCountAsync(0);
        await Expect(overlay).ToHaveCSSAsync("padding-top", "0px");

        await Page.Locator("#btn-toggle-overlay-zoom-control").ClickAsync();
        await Expect(zoomControl).ToHaveCountAsync(1);
        await Expect(overlay).Not.ToHaveCSSAsync("padding-top", "0px");
        Assert.AreEqual(room, await PaddingTopAsync(overlay), 1);
    }

    private static async Task<double> PaddingTopAsync(ILocator element)
        => double.Parse((await element.EvaluateAsync<string>("el => getComputedStyle(el).paddingTop")).Replace("px", ""), CultureInfo.InvariantCulture);

    // Right to left, the overlay's start side is the right, and Leaflet keeps its zoom buttons on the left.
    [TestMethod]
    public async Task ChildContent_RightToLeft_StartsAtTheTopOfTheSideWithoutControls()
    {
        await OpenAsync(Route);
        await ReadyAsync("map-rtl");

        await Expect(Page.Locator("#map-rtl .leaflet-control-zoom")).ToBeVisibleAsync();
        await Expect(Page.Locator("#map-rtl .bit-map-overlay")).ToHaveCSSAsync("padding-top", "0px");
        var canvasBox = (await Canvas("map-rtl").BoundingBoxAsync())!;
        var badgeBox = (await Page.Locator("#rtl-badge").BoundingBoxAsync())!;
        Assert.AreEqual(canvasBox.X + canvasBox.Width, badgeBox.X + badgeBox.Width, 1, "The badge starts at the right");
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
        // Dimmed without fading the canvas itself, which would make one layer of it and take the controls back under
        // the overlay: the map goes under a cover of its background, and the controls fade on their own.
        Assert.AreEqual("1", await ComputedStyleAsync("#map-disabled-canvas", "opacity"));
        Assert.IsTrue(double.Parse(await CoverOpacityAsync("#map-disabled-canvas"), CultureInfo.InvariantCulture) > 0);
        Assert.IsTrue(double.Parse(await ComputedStyleAsync("#map-disabled .leaflet-top", "opacity"), CultureInfo.InvariantCulture) < 1);
        Assert.AreEqual("5", await ComputedStyleAsync("#map-disabled .leaflet-top", "z-index"));
        var listButton = Page.Locator("#map-disabled + .bit-map-marker-list button").First;
        await Expect(listButton).ToBeDisabledAsync();
        await Expect(listButton).ToHaveAttributeAsync("aria-disabled", "true");

        // Nothing inside it takes the focus.
        await Marker("map-disabled", "Big Ben").EvaluateAsync("m => m.focus()");
        await Expect(Marker("map-disabled", "Big Ben")).Not.ToBeFocusedAsync();

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(canvas).Not.ToHaveAttributeAsync("inert", new Regex(".*"));
        await Expect(listButton).ToBeEnabledAsync();
        Assert.AreEqual("none", await Canvas("map-disabled").EvaluateAsync<string>("el => getComputedStyle(el, '::after').content"));
        Assert.AreEqual("1", await ComputedStyleAsync("#map-disabled .leaflet-top", "opacity"));
    }

    [TestMethod]
    public async Task CssVariables_SetTheHeightBorderRadiusAndDisabledOpacity()
    {
        await OpenAsync(Route);

        Assert.AreEqual(250, (await Page.Locator("#map-vars").BoundingBoxAsync())!.Height, 1);
        Assert.AreEqual("3px", await ComputedStyleAsync("#map-vars", "border-top-width"));
        Assert.AreEqual("rgb(255, 0, 0)", await ComputedStyleAsync("#map-vars", "border-top-color"));
        Assert.AreEqual("12px", await ComputedStyleAsync("#map-vars", "border-top-left-radius"));
        // The map shows through its cover as much as the variable says.
        Assert.AreEqual("0.75", await CoverOpacityAsync("#map-vars-canvas"));
    }

    private Task<string> CoverOpacityAsync(string canvasSelector)
        => Page.Locator(canvasSelector).EvaluateAsync<string>("el => getComputedStyle(el, '::after').opacity");

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
    public async Task Failed_MethodsCalledOnAMapThatIsNotReady_DoNothingRatherThanThrow()
    {
        // The missing script is the setup: the browser logs its failed request.
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-show-failed").ClickAsync();
        await Expect(Page.Locator("#failed-state")).ToHaveTextAsync("Failed");

        await Page.Locator("#btn-call-failed").ClickAsync();
        await Expect(Page.Locator("#failed-calls")).ToHaveTextAsync("ready=False;markers=0;located=False;closed=False");
        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
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

    // A round trip of the page's own: once it has been rendered, so has every update the map was handed before it.
    private async Task UpdatesPingAsync()
    {
        var pings = int.Parse(await Page.Locator("#updates-pings").InnerTextAsync(), CultureInfo.InvariantCulture);
        await Page.Locator("#btn-upd-ping").DispatchEventAsync("click");
        await Expect(Page.Locator("#updates-pings")).ToHaveTextAsync((pings + 1).ToString(CultureInfo.InvariantCulture));
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
/// Drives BitMap at /e2e/map/providers on the other providers that need no key, OpenLayers and MapLibre, whose
/// libraries come from a CDN: the same chrome, markers, zoom and keyboard as on Leaflet, and the same markers updated
/// where they stand, against the host's own tiles.
/// ArcGIS, loaded on its own demand, covers the marker double-click its synchronous hit test decides.
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

    [TestMethod]
    public async Task OpenLayers_AMarkerDoubleClickIsTheMarkers()
    {
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-load").ClickAsync();
        await Expect(Page.Locator("#ol-state")).ToHaveTextAsync(new Regex("^(Ready|Failed)$"), new() { Timeout = 60000 });
        if (await Page.Locator("#ol-state").InnerTextAsync() == "Failed") Assert.Inconclusive("OpenLayers could not be loaded from its CDN.");

        var canvas = Page.Locator("#map-ol-canvas");
        await canvas.ScrollIntoViewIfNeededAsync();
        await Page.Locator("#btn-ol-project").ClickAsync();
        await Expect(Page.Locator("#ol-projected")).ToHaveTextAsync(new Regex(@"^\d+,\d+$"));
        var xy = (await Page.Locator("#ol-projected").InnerTextAsync()).Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        var box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.DblClickAsync(box.X + xy[0], box.Y + xy[1] - 12);
        await Expect(Page.Locator("#ol-marker-double")).ToHaveTextAsync("big-ben");
        await AssertNoMapDoubleClickYetAsync("#ol-map-double-clicks");

        // Away from the markers the double-click is the map's: one OnDoubleClick and one level in, from 13.
        await canvas.ScrollIntoViewIfNeededAsync();
        box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.DblClickAsync(box.X + 300, box.Y + 330);
        await Expect(Page.Locator("#ol-map-double-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#ol-zoom")).ToHaveTextAsync("14.0");
        // OpenLayers holds a click back until it cannot be the start of a double-click, so neither pair was a click.
        Assert.AreEqual("none", await Page.Locator("#ol-marker").InnerTextAsync());
    }

    [TestMethod]
    public async Task MapLibre_AMarkerDoubleClickIsTheMarkers()
    {
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-load").ClickAsync();
        await Expect(Page.Locator("#gl-state")).ToHaveTextAsync(new Regex("^(Ready|Failed|Unsupported)$"), new() { Timeout = 60000 });
        var state = await Page.Locator("#gl-state").InnerTextAsync();
        if (state == "Unsupported") Assert.Inconclusive("This browser has no WebGL for MapLibre.");
        if (state == "Failed") Assert.Inconclusive("MapLibre could not be loaded from its CDN.");

        var canvas = Page.Locator("#map-gl-canvas");
        await canvas.Locator(".maplibregl-marker[aria-label='Tower Bridge']").DblClickAsync();
        await Expect(Page.Locator("#gl-marker-double")).ToHaveTextAsync("tower");
        await Expect(Page.Locator("#gl-marker")).ToHaveTextAsync("tower");
        await AssertNoMapDoubleClickYetAsync("#gl-map-double-clicks");

        // Away from the markers the double-click is the map's: one OnDoubleClick and one level in, from 13.
        await canvas.ScrollIntoViewIfNeededAsync();
        var box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.DblClickAsync(box.X + 300, box.Y + 330);
        await Expect(Page.Locator("#gl-map-double-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#gl-zoom")).ToHaveTextAsync("14.0");
    }

    [TestMethod]
    public async Task OpenLayers_AnUpdatedMarkerTakesItsTooltipAndOpenPopupAlongWithoutPanning()
    {
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-load").ClickAsync();
        await Expect(Page.Locator("#ol-state")).ToHaveTextAsync(new Regex("^(Ready|Failed)$"), new() { Timeout = 60000 });
        if (await Page.Locator("#ol-state").InnerTextAsync() == "Failed") Assert.Inconclusive("OpenLayers could not be loaded from its CDN.");

        var canvas = Page.Locator("#map-ol-canvas");
        await canvas.ScrollIntoViewIfNeededAsync();
        await Page.Locator("#btn-ol-give").DispatchEventAsync("click");
        await PingAsync();
        await Page.Locator("#btn-ol-project").ClickAsync();
        await Expect(Page.Locator("#ol-projected")).ToHaveTextAsync(new Regex(@"^\d+,\d+$"));
        var xy = (await Page.Locator("#ol-projected").InnerTextAsync()).Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        // The buttons below the map scrolled it part of the way out of sight as they were pressed.
        await canvas.ScrollIntoViewIfNeededAsync();
        var box = (await canvas.BoundingBoxAsync())!;

        // The tooltip under the pointer says what the marker now says, without the pointer having to move.
        await Page.Mouse.MoveAsync(box.X + xy[0], box.Y + xy[1] - 12);
        var tooltip = canvas.Locator(".bit-map-ol-tooltip");
        await Expect(tooltip).ToHaveTextAsync("Big Ben's tooltip");
        await Page.Locator("#btn-ol-retooltip").DispatchEventAsync("click");
        await Expect(tooltip).ToHaveTextAsync("Big Ben's new tooltip");

        // The popup open on it goes where it goes and says what it now says, but leaves the map where the user took it.
        await Page.Mouse.ClickAsync(box.X + xy[0], box.Y + xy[1] - 12);
        await Expect(canvas.Locator(".bit-map-ol-popup__content")).ToHaveTextAsync("The clock tower");
        const string popupOutOfSight = """
            () => {
                const p = document.querySelector('#map-ol-canvas .bit-map-ol-popup').getBoundingClientRect();
                const c = document.querySelector('#map-ol-canvas').getBoundingClientRect();
                return p.right < c.left || p.left > c.right;
            }
            """;
        await Page.Locator("#btn-ol-pan-away").DispatchEventAsync("click");
        await Page.WaitForFunctionAsync(popupOutOfSight);
        await Page.Locator("#btn-ol-move").DispatchEventAsync("click");
        await Expect(canvas.Locator(".bit-map-ol-popup__content")).ToHaveTextAsync("The clock tower, moved");
        // Well past the second OpenLayers' pan to a popup takes.
        await Page.WaitForTimeoutAsync(1500);
        Assert.IsTrue(await Page.EvaluateAsync<bool>(popupOutOfSight));
        await Expect(canvas.Locator(".bit-map-ol-popup")).Not.ToHaveClassAsync(HasClass("bit-map-ol-popup--hidden"));
    }

    [TestMethod]
    public async Task MapLibre_AnUpdateKeepsEachMarkersElementItsFocusAndTheListsOrder()
    {
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-load").ClickAsync();
        await Expect(Page.Locator("#gl-state")).ToHaveTextAsync(new Regex("^(Ready|Failed|Unsupported)$"), new() { Timeout = 60000 });
        var state = await Page.Locator("#gl-state").InnerTextAsync();
        if (state == "Unsupported") Assert.Inconclusive("This browser has no WebGL for MapLibre.");
        if (state == "Failed") Assert.Inconclusive("MapLibre could not be loaded from its CDN.");

        var canvas = Page.Locator("#map-gl-canvas");
        const string labels = "() => [...document.querySelectorAll('#map-gl-canvas .maplibregl-marker')].map(e => e.getAttribute('aria-label')).join('|')";
        Assert.AreEqual("Big Ben|Tower Bridge", await Page.EvaluateAsync<string>(labels));
        await Page.EvaluateAsync("() => window.__glMarkers = [...document.querySelectorAll('#map-gl-canvas .maplibregl-marker')]");

        // The other way round: each marker keeps its element and Tower Bridge the focus, and the keyboard reaches
        // them in the new order.
        var tower = canvas.Locator(".maplibregl-marker[aria-label='Tower Bridge']");
        await tower.FocusAsync();
        await Page.Locator("#btn-gl-sync-reversed").DispatchEventAsync("click");
        await Page.WaitForFunctionAsync($"() => ({labels})() === 'Tower Bridge|Big Ben'");
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__glMarkers.every(e => e.isConnected)"));
        await Expect(tower).ToBeFocusedAsync();

        // Moved and renamed where it stands: the same element, still focused, drawn somewhere else.
        var at = await tower.EvaluateAsync<string>("el => el.style.transform");
        await Page.Locator("#btn-gl-move-tower").DispatchEventAsync("click");
        var moved = canvas.Locator(".maplibregl-marker[aria-label='Tower Bridge, moved']");
        await Expect(moved).ToBeFocusedAsync();
        Assert.IsTrue(await moved.EvaluateAsync<bool>("el => el === window.__glMarkers[1]"));
        Assert.AreNotEqual(at, await moved.EvaluateAsync<string>("el => el.style.transform"));
    }

    [TestMethod]
    public async Task ArcGis_AMarkerDoubleClickIsTheMarkers()
    {
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-load-arcgis").ClickAsync();
        await Expect(Page.Locator("#ag-state")).ToHaveTextAsync(new Regex("^(Ready|Failed|Unsupported)$"), new() { Timeout = 90000 });
        if (await Page.Locator("#ag-state").InnerTextAsync() != "Ready") Assert.Inconclusive("The ArcGIS SDK could not be loaded from its CDN.");

        // The markers are drawn on the view's canvas, and the double-click is told apart from the map's by the
        // provider's own synchronous hit test, since the view's hitTest answers too late to stop the zoom.
        var canvas = Page.Locator("#map-ag-canvas");
        await canvas.ScrollIntoViewIfNeededAsync();
        await Page.Locator("#btn-ag-project").ClickAsync();
        await Expect(Page.Locator("#ag-projected")).ToHaveTextAsync(new Regex(@"^\d+,\d+$"));
        var xy = (await Page.Locator("#ag-projected").InnerTextAsync()).Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        var box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.DblClickAsync(box.X + xy[0], box.Y + xy[1] - 12);
        await Expect(Page.Locator("#ag-marker-double")).ToHaveTextAsync("big-ben");
        await AssertNoMapDoubleClickYetAsync("#ag-map-double-clicks");

        // Away from the markers the double-click is the map's: one OnDoubleClick and one level in, from 13.
        await canvas.ScrollIntoViewIfNeededAsync();
        box = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.DblClickAsync(box.X + 300, box.Y + 330);
        await Expect(Page.Locator("#ag-map-double-clicks")).ToHaveTextAsync("1", new() { Timeout = 15000 });
        await Expect(Page.Locator("#ag-zoom")).ToHaveTextAsync("14.0", new() { Timeout = 15000 });
        // ArcGIS holds a click back until it cannot be the start of a double-click, so neither pair was a click.
        Assert.AreEqual("none", await Page.Locator("#ag-marker").InnerTextAsync());
    }

    [TestMethod]
    public async Task Cesium_ADraggableMarkerIsClickedDoubleClickedAndDragged()
    {
        AllowConsoleErrors = true;

        await OpenAsync(Route);
        await Page.Locator("#btn-load-cesium").ClickAsync();
        await Expect(Page.Locator("#cs-state")).ToHaveTextAsync(new Regex("^(Ready|Failed|Unsupported)$"), new() { Timeout = 90000 });
        var state = await Page.Locator("#cs-state").InnerTextAsync();
        if (state == "Unsupported") Assert.Inconclusive("This browser has no WebGL for Cesium.");
        if (state == "Failed") Assert.Inconclusive("CesiumJS could not be loaded from its CDN.");

        var canvas = Page.Locator("#map-cs-canvas");
        await canvas.ScrollIntoViewIfNeededAsync();
        await Page.Locator("#btn-cs-project").ClickAsync();
        await Expect(Page.Locator("#cs-projected")).ToHaveTextAsync(new Regex(@"^\d+,\d+$"));
        var xy = (await Page.Locator("#cs-projected").InnerTextAsync()).Split(',').Select(v => float.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        async Task<(float X, float Y)> BigBenAsync()
        {
            await canvas.ScrollIntoViewIfNeededAsync();
            var box = (await canvas.BoundingBoxAsync())!;
            return (box.X + xy[0], box.Y + xy[1] - 12);
        }

        // A press that does not move is a click, on a draggable marker too - not a drag to where it already was. Cesium
        // picks only what it has drawn, and the pin's image loads after the marker is added, so the click is repeated
        // until it lands.
        var (x, y) = await BigBenAsync();
        for (var attempt = 0; ; attempt++)
        {
            await Page.Mouse.ClickAsync(x, y);
            try
            {
                await Expect(Page.Locator("#cs-marker")).ToHaveTextAsync("big-ben", new() { Timeout = 500 });
                break;
            }
            catch (PlaywrightException) when (attempt < 20) { }
        }
        await PingAsync();
        Assert.AreEqual("none", await Page.Locator("#cs-drag-end").InnerTextAsync());
        var clicks = int.Parse(await Page.Locator("#cs-marker-clicks").InnerTextAsync(), CultureInfo.InvariantCulture);

        // Cesium reports both clicks of a double-click as they land; the double-click is the marker's, and the camera
        // does not fly to the marker either - or the drag below would start where the marker no longer is.
        (x, y) = await BigBenAsync();
        await Page.Mouse.DblClickAsync(x, y);
        await Expect(Page.Locator("#cs-marker-double")).ToHaveTextAsync("big-ben");
        await AssertNoMapDoubleClickYetAsync("#cs-map-double-clicks");
        Assert.AreEqual((clicks + 2).ToString(CultureInfo.InvariantCulture), await Page.Locator("#cs-marker-clicks").InnerTextAsync());

        // A press that moves is a drag, and its release is no click.
        (x, y) = await BigBenAsync();
        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(x + 80, y, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await Expect(Page.Locator("#cs-drag-end")).ToHaveTextAsync("big-ben");
        await PingAsync();
        Assert.AreEqual((clicks + 2).ToString(CultureInfo.InvariantCulture), await Page.Locator("#cs-marker-clicks").InnerTextAsync());

        // Away from the markers the double-click is the map's.
        await canvas.ScrollIntoViewIfNeededAsync();
        var mapBox = (await canvas.BoundingBoxAsync())!;
        await Page.Mouse.DblClickAsync(mapBox.X + 300, mapBox.Y + 330);
        await Expect(Page.Locator("#cs-map-double-clicks")).ToHaveTextAsync("1");
    }

    // A round trip of the page's own: once it has been rendered, so has every callback a map raised before it.
    private async Task PingAsync()
    {
        var pings = int.Parse(await Page.Locator("#pings").InnerTextAsync(), CultureInfo.InvariantCulture);
        await Page.Locator("#btn-ping").ClickAsync();
        await Expect(Page.Locator("#pings")).ToHaveTextAsync((pings + 1).ToString(CultureInfo.InvariantCulture));
    }

    // The map's OnDoubleClick would have been raised by the same event as the marker's, so once a later round trip
    // has been rendered it would show.
    private async Task AssertNoMapDoubleClickYetAsync(string counter)
    {
        await PingAsync();
        Assert.AreEqual("0", await Page.Locator(counter).InnerTextAsync());
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
