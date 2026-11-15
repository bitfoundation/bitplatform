using System.Text.RegularExpressions;
using Bit.BlazorUI.Tests.E2E.Buttons;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitContextMenu at /e2e/context-menu in a real browser: the menu opened where the area is right-clicked, with
/// the focus in it and handed back on Escape; moved by a second right-click on the area and dismissed by one, or by a
/// click, elsewhere; opened from the keyboard with Shift+F10 and the Menu key at the focused row; the browser's own menu
/// taken away only while there is one to show instead; and the menu named and checked by axe.
/// </summary>
[TestClass]
public class BitContextMenuE2ETests : E2ETestBase
{
    private const string Route = "/e2e/context-menu";

    private ILocator Root => Page.Locator("#ctm");

    private ILocator Area => Page.Locator("#ctm-area");

    private ILocator Menu => Page.Locator("[role=menu][aria-labelledby='ctm-mnb-opb']");

    private ILocator Callout => Menu.Locator("xpath=..");

    private ILocator Items => Menu.Locator(":scope > li > .bit-mnb-itm");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    // The menu slides in from the side it is placed on; its box is measured once that transition is over.
    private async Task SettledAsync()
    {
        await Expect(Callout).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
    }

    private static void Near(double expected, double actual, string what)
    {
        Assert.IsTrue(Math.Abs(expected - actual) < 1.5, $"{what}: expected about {expected}, got {actual}.");
    }

    // The point the browser reports for the last contextmenu on the area, which is what a right-click aimed at a point
    // turns into: Firefox hands mouse events whole pixels.
    private async Task RecordContextMenuPointAsync()
    {
        await Root.EvaluateAsync("el => el.addEventListener('contextmenu', e => window.__ctxPoint = [e.clientX, e.clientY], { capture: true })");
    }

    private Task<double[]> ContextMenuPointAsync() => Page.EvaluateAsync<double[]>("() => window.__ctxPoint");

    [TestMethod]
    public async Task ARightClick_OpensTheMenuAtThePoint_WithTheFocusInIt_AndEscapeHandsItBack()
    {
        await OpenAsync(Route);
        await RecordContextMenuPointAsync();

        await Area.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 380, Y = 30 } });

        await Expect(Menu).ToBeVisibleAsync();
        await Expect(Text("ctm-open")).ToHaveTextAsync("True");
        await SettledAsync();

        var point = await ContextMenuPointAsync();
        var box = await Callout.BoxAsync();
        Near(point[0], box.X, "left at the point");
        Near(point[1] + 1, box.Y, "top at the point");

        // The press focused the area, the tab stop it is; the keyboard is taken into the menu from there.
        await Expect(Items.First).ToBeFocusedAsync();
        await Expect(Menu).ToHaveAccessibleNameAsync("Context menu");
        await AssertNoAxeViolationsAsync("[role=menu][aria-labelledby='ctm-mnb-opb']");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Menu).ToBeHiddenAsync();
        await Expect(Text("ctm-open")).ToHaveTextAsync("False");
        await Expect(Root).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task ASecondRightClickOnTheArea_MovesTheMenu_AndOneElsewhereDismissesIt()
    {
        await OpenAsync(Route);
        await RecordContextMenuPointAsync();

        await Area.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 380, Y = 30 } });
        await Expect(Menu).ToBeVisibleAsync();
        await SettledAsync();

        await Area.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 60, Y = 120 } });

        var point = await ContextMenuPointAsync();
        Assert.IsTrue(point[0] < 200, "the second right-click reached the area.");
        var started = DateTime.UtcNow;
        while (Math.Abs((await Callout.BoxAsync()).X - point[0]) >= 1.5)
        {
            Assert.IsTrue(DateTime.UtcNow - started < TimeSpan.FromSeconds(5), "the menu did not move to the new point.");
            await NextFramesAsync();
        }
        await Expect(Menu).ToBeVisibleAsync();
        await Expect(Items.First).ToBeFocusedAsync();

        // The page does not take a right-click on its own text, so that one is the user going elsewhere.
        await Text("ctm-page-clicks").ClickAsync(new() { Button = MouseButton.Right });

        await Expect(Menu).ToBeHiddenAsync();
        await Expect(Text("ctm-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task AClickOnThePage_ReachesIt_AndDismissesTheMenu()
    {
        await OpenAsync(Route);

        await Area.ClickAsync(new() { Button = MouseButton.Right, Position = new() { X = 380, Y = 30 } });
        await Expect(Menu).ToBeVisibleAsync();

        await Page.Locator("#btn-page").ClickAsync();

        await Expect(Text("ctm-page-clicks")).ToHaveTextAsync("1");
        await Expect(Menu).ToBeHiddenAsync();
        await Expect(Text("ctm-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    [DataRow("Shift+F10")]
    [DataRow("ContextMenu")]
    public async Task TheKeyboard_OpensTheMenuAtTheFocusedRow_AndAnItemHandsTheFocusBackToIt(string key)
    {
        await OpenAsync(Route);

        var row = Page.Locator("#row-2");
        await row.FocusAsync();

        await Page.Keyboard.PressAsync(key);

        await Expect(Menu).ToBeVisibleAsync();
        await SettledAsync();

        // At the middle of the row, not of the area: where Chromium puts its own menu for the key, too.
        var rowBox = await row.BoxAsync();
        var box = await Callout.BoxAsync();
        Near(Math.Round(rowBox.X + rowBox.Width / 2), box.X, "left at the middle of the row");
        Near(Math.Round(rowBox.Y + rowBox.Height / 2) + 1, box.Y, "top at the middle of the row");

        await Expect(Items.First).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Items.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Text("ctm-clicked")).ToHaveTextAsync("Paste");
        await Expect(Menu).ToBeHiddenAsync();
        await Expect(row).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task TheBrowsersOwnMenu_IsTakenAwayOnlyWhileThereIsOneToShowInstead()
    {
        await OpenAsync(Route);

        // Read once every handler has run, Blazor's among them.
        await Page.EvaluateAsync("() => window.addEventListener('contextmenu', e => window.__prevented = e.defaultPrevented)");
        Task<bool> PreventedAsync() => Page.EvaluateAsync<bool>("() => window.__prevented");

        await Page.Locator("#dis-area").DispatchEventAsync("contextmenu", new { bubbles = true, cancelable = true, button = 2 });
        Assert.IsFalse(await PreventedAsync(), "a disabled context menu leaves the right-click to the browser.");
        await Expect(Page.Locator("[role=menu][aria-labelledby='ctm-dis-mnb-opb']")).ToBeHiddenAsync();

        await Area.DispatchEventAsync("contextmenu", new { bubbles = true, cancelable = true, button = 2 });
        Assert.IsTrue(await PreventedAsync(), "an enabled one takes it, to show its own menu instead.");
        await Expect(Menu).ToBeVisibleAsync();
    }
}
