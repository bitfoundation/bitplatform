namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives the full screen of BitImageViewer at /e2e/image-viewer in a real browser: its button covering the page with a
/// modal dialog the focus is taken into and kept inside, the page held still behind it and the plain wheel zooming it; a
/// plain Escape and the close button leaving it, the focus back on what opened it; a lightbox opened from a thumbnail of
/// the page's own, out of the page until then; and the dialog checked by axe.
/// </summary>
[TestClass]
public class BitImageViewerFullScreenE2ETests : E2ETestBase
{
    private const string Route = "/e2e/image-viewer";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Viewport(string id) => Page.Locator($"#{id} .bit-imv-vpt");

    private ILocator FullScreenButton => Page.Locator("#imv-fs .bit-imv-fsb");

    private ILocator CloseButton(string id) => Page.Locator($"#{id} .bit-imv-cls");

    private static void Near(double expected, double actual, string what, double tolerance = 1.5)
    {
        Assert.IsTrue(Math.Abs(expected - actual) < tolerance, $"{what}: expected about {expected}, got {actual}.");
    }

    private async Task OpenViewerAsync()
    {
        await OpenAsync(Route);
        await Page.WaitForFunctionAsync("() => [...document.querySelectorAll('.bit-imv-img')].every(i => i.complete && i.naturalWidth > 0)");
    }

    private async Task<(double Width, double Height)> WindowAsync()
    {
        var size = await Page.EvaluateAsync<double[]>("() => [document.documentElement.clientWidth, window.innerHeight]");
        return (size[0], size[1]);
    }

    private async Task EnterAsync()
    {
        await FullScreenButton.ScrollIntoViewIfNeededAsync();
        await FullScreenButton.ClickAsync();

        await Expect(Root("imv-fs")).ToHaveAttributeAsync("role", "dialog");
        await Expect(Viewport("imv-fs")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TheButton_CoversThePageWithAModalDialog_TheFocusTakenIntoTheFrame()
    {
        await OpenViewerAsync();

        await EnterAsync();

        await Expect(Page.Locator("#imv-fs-state")).ToHaveTextAsync("True");
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Full screen" })).ToBeVisibleAsync();
        await Expect(Root("imv-fs")).ToHaveAttributeAsync("aria-modal", "true");

        var (width, height) = await WindowAsync();
        var box = (await Root("imv-fs").BoundingBoxAsync())!;
        Near(0, box.X, "left");
        Near(0, box.Y, "top");
        Near(width, box.Width, "width");
        Near(height, box.Height, "height");

        // The frame takes the room the toolbar leaves it, not the 300px it was given in the page.
        Assert.IsTrue((await Viewport("imv-fs").BoundingBoxAsync())!.Height > 300, "the frame fills the screen.");

        await AssertNoAxeViolationsAsync("#imv-fs");
    }

    [TestMethod]
    public async Task TheFocus_IsKeptInside_AndThePageHoldsStillBehindIt()
    {
        await OpenViewerAsync();
        await EnterAsync();

        // Read once it is full screen: bringing its button into view to press it scrolled the page on the way.
        var scrolled = await Page.EvaluateAsync<double>("() => window.scrollY");

        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.IsTrue(await Root("imv-fs").EvaluateAsync<bool>("el => el.contains(document.activeElement)"), $"Tab {i + 1} left the viewer.");
        }

        for (var i = 0; i < 8; i++)
        {
            await Page.Keyboard.PressAsync("Shift+Tab");
            Assert.IsTrue(await Root("imv-fs").EvaluateAsync<bool>("el => el.contains(document.activeElement)"), $"Shift+Tab {i + 1} left the viewer.");
        }

        // The wheel over the margin around the frame has no page to scroll.
        var (_, height) = await WindowAsync();
        await Page.Mouse.MoveAsync(4, (float)height - 4);
        await Page.Mouse.WheelAsync(0, 400);
        await NextFramesAsync();
        Assert.AreEqual(scrolled, await Page.EvaluateAsync<double>("() => window.scrollY"), "the page scrolled behind the viewer");
    }

    [TestMethod]
    public async Task ThePlainWheel_ZoomsAFullScreenViewer()
    {
        await OpenViewerAsync();
        await EnterAsync();

        var box = (await Viewport("imv-fs").BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        await Page.Mouse.WheelAsync(0, -200);

        await Expect(Page.Locator("#imv-fs .bit-imv-zmt")).Not.ToHaveTextAsync("100%");
    }

    [TestMethod]
    public async Task APlainEscape_LeavesIt_AndHandsTheFocusBackToTheButton()
    {
        await OpenViewerAsync();
        await EnterAsync();

        // Zoomed in on the way, which the viewer keeps.
        await Page.Keyboard.PressAsync("+");
        await Expect(Page.Locator("#imv-fs .bit-imv-zmt")).ToHaveTextAsync("125%");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Root("imv-fs")).Not.ToHaveAttributeAsync("role", "dialog");
        await Expect(Page.Locator("#imv-fs-state")).ToHaveTextAsync("False");
        await Expect(FullScreenButton).ToBeFocusedAsync();
        await Expect(FullScreenButton).ToHaveAccessibleNameAsync("Full screen");
        await Expect(Page.Locator("#imv-fs .bit-imv-zmt")).ToHaveTextAsync("125%");

        Near(300, (await Viewport("imv-fs").BoundingBoxAsync())!.Height, "back in the page at the height it was given");
    }

    [TestMethod]
    public async Task TheCloseButton_LeavesIt_AndHandsTheFocusBack()
    {
        await OpenViewerAsync();
        await EnterAsync();

        await Expect(CloseButton("imv-fs")).ToHaveAccessibleNameAsync("Exit full screen");
        await CloseButton("imv-fs").ClickAsync();

        await Expect(Page.Locator("#imv-fs-state")).ToHaveTextAsync("False");
        await Expect(CloseButton("imv-fs")).ToHaveCountAsync(0);
        await Expect(FullScreenButton).ToBeFocusedAsync();

        // And the page scrolls again.
        await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        await Page.Mouse.MoveAsync(700, 100);
        await Page.Mouse.WheelAsync(0, 200);
        await Page.WaitForFunctionAsync("() => window.scrollY > 0");
    }

    [TestMethod]
    public async Task TheMethods_ZoomAndShowFullScreenAViewerThePageHasNotBound()
    {
        await OpenViewerAsync();

        var image = Page.Locator("#imv-api .bit-imv-img");
        await image.ScrollIntoViewIfNeededAsync();

        await Page.Locator("#btn-api-zoom-in").ClickAsync();
        await Page.WaitForFunctionAsync("() => Math.abs(document.querySelector('#imv-api .bit-imv-img').getBoundingClientRect().width - 500) < 1.5");

        await Page.Locator("#btn-api-reset").ClickAsync();
        await Page.WaitForFunctionAsync("() => Math.abs(document.querySelector('#imv-api .bit-imv-img').getBoundingClientRect().width - 400) < 1.5");

        await Page.Locator("#btn-api-full-screen").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Methods" })).ToBeVisibleAsync();
        await Expect(Viewport("imv-api")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Root("imv-api")).Not.ToHaveAttributeAsync("role", "dialog");
    }

    [TestMethod]
    public async Task ALightbox_IsOutOfThePageUntilOpened_AndClosesBackToItsThumbnail()
    {
        await OpenViewerAsync();

        await Expect(Root("imv-lb")).ToBeHiddenAsync();

        // Opened from the keyboard, which is where the focus handed back matters: a click on the page's own thumbnail
        // does not focus it in WebKit, which leaves nothing there to hand the focus back to.
        var thumb = Page.Locator("#imv-thumb");
        await thumb.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Root("imv-lb")).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Dialog, new() { Name = "Lightbox" })).ToBeVisibleAsync();
        await Expect(Viewport("imv-lb")).ToBeFocusedAsync();

        var (width, height) = await WindowAsync();
        var box = (await Root("imv-lb").BoundingBoxAsync())!;
        Near(width, box.Width, "width");
        Near(height, box.Height, "height");

        await AssertNoAxeViolationsAsync("#imv-lb");

        await CloseButton("imv-lb").ClickAsync();

        await Expect(Root("imv-lb")).ToBeHiddenAsync();
        await Expect(thumb).ToBeFocusedAsync();
    }
}
