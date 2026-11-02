namespace Boilerplate.WorkbenchTests.Features.Flows;

/// <summary>Moving around a board: a mouse or a finger pans it, the wheel or a pinch zooms it, and its minimap shows the screens, follows the view and steers it.</summary>
[TestClass, TestCategory("UITest")]
public class FlowBoardPanAndZoomUITests : FlowPageTest
{
    private ILocator Board => Page.Locator(".bit-blow.flow-board");

    private ILocator MiniMap => Page.Locator(".bit-blow-minimap");

    private ILocator MiniMapMask => MiniMap.Locator(".bit-blow-minimap-mask");

    private async Task<LocatorBoundingBoxResult> BoxOf(ILocator element) => (await element.BoundingBoxAsync())!;

    /// <summary>Reads an element's place on the screen until it's as expected.</summary>
    private Task<LocatorBoundingBoxResult> WaitForBox(ILocator element, Func<LocatorBoundingBoxResult, bool> condition) => WorkbenchTestReviewer.WaitFor(() => BoxOf(element), condition);

    /// <summary>A spot of the board with nothing on it, where a drag pans and a wheel zooms.</summary>
    private async Task<(float X, float Y)> ABlankSpot()
    {
        var spot = await Board.EvaluateAsync<double[]>("""
            board => {
                const box = board.getBoundingClientRect();
                const taken = '.bit-blow-node, .bit-blow-edge, .bit-blow-controls, .bit-blow-minimap, .bit-blow-panel, a, button';
                for (let y = box.bottom - 40; y > box.top; y -= 40) {
                    for (let x = box.left + 40; x < box.right; x += 40) {
                        const under = document.elementFromPoint(x, y);
                        if (under && under.closest('.bit-blow') === board && under.closest(taken) === null) return [x, y];
                    }
                }
                return null;
            }
            """);

        Assert.IsNotNull(spot, "The board has no blank spot.");

        return ((float)spot[0], (float)spot[1]);
    }

    private async Task Fingers(ICDPSession devTools, string type, params (double X, double Y)[] points)
    {
        await devTools.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = type,
            ["touchPoints"] = points.Select((point, id) => (object)new Dictionary<string, object> { ["x"] = point.X, ["y"] = point.Y, ["id"] = id }).ToArray()
        });
    }

    /// <summary>Drags from one spot to another: with the mouse on a desktop and a finger on a phone.</summary>
    private async Task Drag((float X, float Y) from, (float X, float Y) to)
    {
        if (IsPhone)
        {
            var devTools = await Context.NewCDPSessionAsync(Page);

            await Fingers(devTools, "touchStart", from);

            for (var step = 1; step <= 8; step++)
            {
                await Fingers(devTools, "touchMove", (from.X + (to.X - from.X) * step / 8d, from.Y + (to.Y - from.Y) * step / 8d));
            }

            await Fingers(devTools, "touchEnd");
        }
        else
        {
            await Page.Mouse.MoveAsync(from.X, from.Y);
            await Page.Mouse.DownAsync();
            await Page.Mouse.MoveAsync(to.X, to.Y, new() { Steps = 8 });
            await Page.Mouse.UpAsync();
        }
    }

    /// <summary>Two fingers going from one distance to another on a phone; a desktop's trackpad pinch reaches a page as the wheel with Ctrl held.</summary>
    private async Task Pinch((float X, float Y) around, double fromGap, double toGap)
    {
        if (IsPhone)
        {
            var devTools = await Context.NewCDPSessionAsync(Page);

            (double, double)[] FingersAt(double gap) => [(around.X - gap / 2, around.Y), (around.X + gap / 2, around.Y)];

            await Fingers(devTools, "touchStart", FingersAt(fromGap));

            for (var step = 1; step <= 8; step++)
            {
                await Fingers(devTools, "touchMove", FingersAt(fromGap + (toGap - fromGap) * step / 8));
            }

            await Fingers(devTools, "touchEnd");
        }
        else
        {
            await Page.Mouse.MoveAsync(around.X, around.Y);
            await Page.Keyboard.DownAsync("Control");
            await Page.Mouse.WheelAsync(0, toGap > fromGap ? -120 : 120);
            await Page.Keyboard.UpAsync("Control");
        }
    }

    [TestMethod, EverySurface]
    public async Task ADragOnABlankSpot_Should_MoveTheScreensWithIt(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        var card = CardOf(flow.Screens[0].Id);
        var before = await BoxOf(card);
        var spot = await ABlankSpot();

        await Drag(spot, (spot.X + 60, spot.Y - 50));

        var after = await WaitForBox(card, box => Math.Abs(box.X - before.X - 60) <= 12 && Math.Abs(box.Y - before.Y + 50) <= 12);

        Assert.AreEqual(before.Width, after.Width, 1, "A pan leaves the zoom alone.");
    }

    [TestMethod, OnDesktops]
    public async Task TheWheel_Should_ZoomInAroundThePointerAndBackOut(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        var card = CardOf(flow.Screens[0].Id);
        var before = await BoxOf(card);
        var spot = await ABlankSpot();

        await Page.Mouse.MoveAsync(spot.X, spot.Y);
        await Page.Mouse.WheelAsync(0, -240);

        await WaitForBox(card, box => box.Width > before.Width * 1.05);

        await SettledBoardViewport();

        var zoomedIn = await BoxOf(card);
        var zoom = zoomedIn.Width / before.Width;

        Assert.AreEqual(spot.X + (before.X - spot.X) * zoom, zoomedIn.X, 3, "The spot under the pointer stays where it is.");

        await Page.Mouse.WheelAsync(0, 240);

        await WaitForBox(card, box => box.Width < zoomedIn.Width * 0.95);
    }

    [TestMethod, OnDesktops]
    public async Task TheWheelOverAPresentedScreen_Should_ScrollThatScreenAndLeaveTheBoard(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Press(PresentButton);

        await Expect(CardOf(flow.Screens[0].Id)).ToBeInViewportAsync(new() { Ratio = 0.8f });

        var html = Screen(flow.Screens[0].Id).Locator("html");

        await WorkbenchTestReviewer.WaitFor(() => html.EvaluateAsync<bool>("() => window.__workbenchReady === true"), isReady => isReady);

        var board = await SettledBoardViewport();
        var frame = await BoxOf(Page.Locator($"iframe[data-screen-id='{flow.Screens[0].Id}']"));

        await Page.Mouse.MoveAsync(frame.X + frame.Width / 2, frame.Y + frame.Height / 2);
        await Page.Mouse.WheelAsync(0, 240);

        await WorkbenchTestReviewer.WaitFor(() => html.EvaluateAsync<double>("html => Math.max(...[html, ...html.querySelectorAll('*')].map(e => e.scrollTop))"), scrolled => scrolled > 0);

        Assert.AreEqual(board, await BoardViewport());
    }

    [TestMethod, EverySurface]
    public async Task APinch_Should_ZoomTheBoardAndNotThePage(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        var card = CardOf(flow.Screens[0].Id);
        var before = await BoxOf(card);
        var board = await BoxOf(Board);
        var spot = (X: board.X + board.Width / 2, (await ABlankSpot()).Y);

        await Pinch(spot, fromGap: 60, toGap: 200);

        var zoomedIn = await WaitForBox(card, box => box.Width > before.Width * 1.05);

        Assert.AreEqual(1, await Page.EvaluateAsync<double>("() => window.visualViewport.scale"), "The page itself zoomed.");

        await SettledBoardViewport();

        var settled = await BoxOf(card);

        Assert.IsGreaterThanOrEqualTo(zoomedIn.Width, settled.Width);

        await Pinch(spot, fromGap: 200, toGap: 60);

        await WaitForBox(card, box => box.Width < settled.Width * 0.95);
    }

    [TestMethod, OnPhones]
    public async Task APhone_Should_LeaveTheMiniMapOutAndKeepTheBoardsControls(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Expect(MiniMap).ToBeHiddenAsync();
        await Expect(Page.Locator(".bit-blow-controls-button").First).ToBeVisibleAsync();
    }

    [TestMethod, OnDesktops]
    public async Task TheMiniMap_Should_ShowTheScreensAndFollowAndSteerTheBoard(WorkbenchSurface surface)
    {
        var flow = await DrawThemeFlow();

        await OpenBoard(flow.Id, 2);

        await Expect(MiniMap).ToBeVisibleAsync();
        await Expect(MiniMap.Locator(".bit-blow-minimap-node")).ToHaveCountAsync(2);

        var fitted = await MiniMapMask.GetAttributeAsync("d");

        await Press(Page.Locator(".bit-blow-controls-button").Nth(0));

        await Expect(MiniMapMask).Not.ToHaveAttributeAsync("d", fitted!);

        await SettledBoardViewport();

        var second = await BoxOf(MiniMap.Locator(".bit-blow-minimap-node").Nth(1));

        await Page.Mouse.ClickAsync(second.X + second.Width / 2, second.Y + second.Height / 2);

        var board = await BoxOf(Board);

        await WaitForBox(CardOf(flow.Screens[1].Id), card => Math.Abs(card.X + card.Width / 2 - (board.X + board.Width / 2)) <= 30
                                                            && Math.Abs(card.Y + card.Height / 2 - (board.Y + board.Height / 2)) <= 30);
    }
}
