namespace Bit.BlazorUI.Tests.E2E.Surfaces;

/// <summary>
/// BitPanel as a bottom sheet with snap points (/e2e/panel-sheet) in a real browser: it opens at its first snap point,
/// as tall as the fraction of the viewport or the pixels it names; its grabber moves it with a tap and the keys and is
/// named after the size it is at; a drag by the grabber settles at the nearest snap point (and is no tap of it), a short
/// drag below the smallest springs back and a long one dismisses it; a SnapPoint the page holds one way is where a drag
/// settles; and the open sheet passes axe.
/// </summary>
[TestClass]
public class BitPanelSheetE2ETests : E2ETestBase
{
    private const string Route = "/e2e/panel-sheet";

    private ILocator Container(string id) => Page.Locator($"#{id} .bit-pnl-cnt");

    private ILocator Grabber(string id) => Page.Locator($"#{id} .bit-pnl-grb");

    private async Task<double> ViewportHeightAsync() => await Page.EvaluateAsync<double>("() => window.innerHeight");

    // The sheet's height once its transition is over, which it is when two reads a little apart agree.
    private async Task<double> SettledHeightAsync(string id)
    {
        double previous = -1;
        for (var i = 0; i < 40; i++)
        {
            var height = await Container(id).EvaluateAsync<double>("e => e.getBoundingClientRect().height");
            if (Math.Abs(height - previous) < 0.5) return height;
            previous = height;
            await Page.WaitForTimeoutAsync(100);
        }
        return previous;
    }

    private async Task ExpectHeightAsync(string id, double expected, string what)
    {
        var height = await SettledHeightAsync(id);
        Assert.AreEqual(expected, height, 3, $"{what}: expected a height of about {expected}px, got {height}px.");
    }

    private async Task OpenSheetAsync(string button, string id)
    {
        await OpenAsync(Route);
        await Page.Locator($"#{button}").ClickAsync();
        await Expect(Container(id)).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bbit-pnl-opn\b"));

        // Done sliding in: the sheet sits on the bottom edge of the viewport.
        await Page.WaitForFunctionAsync("id => Math.abs(document.querySelector('#' + id + ' .bit-pnl-cnt').getBoundingClientRect().bottom - window.innerHeight) < 1", id);
        await SettledHeightAsync(id);
    }

    // A drag by the grabber, which grows the sheet with the pointer (up) and shrinks it (down).
    private async Task DragGrabberAsync(string id, double dy)
    {
        var box = (await Grabber(id).BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        await Page.Mouse.MoveAsync(x, y);
        await Page.Mouse.DownAsync();
        for (var i = 1; i <= 12; i++)
        {
            await Page.Mouse.MoveAsync(x, (float)(y + dy * i / 12));
        }
        await Page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task TheSheet_OpensAtItsFirstSnapPoint()
    {
        await OpenSheetAsync("open-sheet", "sheet");

        var vh = await ViewportHeightAsync();
        await ExpectHeightAsync("sheet", vh * 0.3, "the first snap point");
        await Expect(Grabber("sheet")).ToHaveAttributeAsync("aria-label", "Resize, size 1 of 3");
    }

    [TestMethod]
    public async Task ASnapPointInPixels_IsThatManyPixelsTall()
    {
        await OpenSheetAsync("open-pixels", "pixels");

        await ExpectHeightAsync("pixels", 250, "250 pixels");

        await Grabber("pixels").ClickAsync();
        await ExpectHeightAsync("pixels", await ViewportHeightAsync() * 0.8, "eight tenths of the viewport");
    }

    [TestMethod]
    public async Task TheGrabber_MovesTheSheetWithATapAndTheKeys()
    {
        await OpenSheetAsync("open-sheet", "sheet");
        var vh = await ViewportHeightAsync();

        await Grabber("sheet").ClickAsync();
        await ExpectHeightAsync("sheet", vh * 0.6, "a tap moves it to the next height");
        await Expect(Page.Locator("#sheet-snap")).ToHaveTextAsync("1");
        await Expect(Grabber("sheet")).ToHaveAttributeAsync("aria-label", "Resize, size 2 of 3");

        await Grabber("sheet").FocusAsync();
        await Page.Keyboard.PressAsync("End");
        await ExpectHeightAsync("sheet", vh * 0.9, "End moves it to the largest");

        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectHeightAsync("sheet", vh * 0.6, "ArrowDown moves it one smaller");

        await Page.Keyboard.PressAsync("Home");
        await ExpectHeightAsync("sheet", vh * 0.3, "Home moves it to the smallest");
        await Expect(Page.Locator("#sheet-snap")).ToHaveTextAsync("0");

        // The keys the grabber answers are its own: the content behind it does not scroll.
        Assert.AreEqual(0, await Container("sheet").Locator(".bit-pnl-bdy").EvaluateAsync<double>("e => e.scrollTop"));
    }

    [TestMethod]
    public async Task ADragByTheGrabber_SettlesAtTheNearestSnapPoint_AndIsNoTapOfIt()
    {
        await OpenSheetAsync("open-sheet", "sheet");
        var vh = await ViewportHeightAsync();

        // From three tenths up by a little more than a quarter: nearest to six tenths, not nine.
        await DragGrabberAsync("sheet", -vh * 0.27);

        await ExpectHeightAsync("sheet", vh * 0.6, "the nearest snap point");
        await Expect(Page.Locator("#sheet-snap")).ToHaveTextAsync("1");

        // Up from six tenths by a little: nearest to six tenths still, and the release made no tap that moved it on.
        await DragGrabberAsync("sheet", -vh * 0.1);
        await ExpectHeightAsync("sheet", vh * 0.6, "back to where it was");
        await Expect(Page.Locator("#sheet-snap")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task BelowTheSmallest_AShortDragSpringsBack_AndALongOneDismisses()
    {
        await OpenSheetAsync("open-sheet", "sheet");
        var vh = await ViewportHeightAsync();

        await DragGrabberAsync("sheet", vh * 0.3 * 0.15);
        await ExpectHeightAsync("sheet", vh * 0.3, "less than a quarter of the smallest springs back");
        await Expect(Page.Locator("#sheet-open")).ToHaveTextAsync("True");

        await DragGrabberAsync("sheet", vh * 0.3 * 0.6);
        await Expect(Page.Locator("#sheet-open")).ToHaveTextAsync("False");

        // Opened again, it is the height it was at, not the one the drag left it at.
        await Page.Locator("#open-sheet").ClickAsync();
        await ExpectHeightAsync("sheet", vh * 0.3, "the height it was at");
    }

    [TestMethod]
    public async Task ASnapPointHeldOneWay_IsWhereADragSettles()
    {
        await OpenSheetAsync("open-held", "held");
        var vh = await ViewportHeightAsync();

        await ExpectHeightAsync("held", vh * 0.6, "the snap point the page holds");

        await DragGrabberAsync("held", -vh * 0.3);
        await ExpectHeightAsync("held", vh * 0.6, "back where the page holds it");

        await Grabber("held").ClickAsync();
        await ExpectHeightAsync("held", vh * 0.6, "nor does the grabber move it");
    }

    [TestMethod]
    public async Task TheOpenSheet_PassesAxe()
    {
        await OpenSheetAsync("open-sheet", "sheet");
        await SettledHeightAsync("sheet");

        await AssertNoAxeViolationsAsync("#sheet .bit-pnl-cnt");
    }
}
