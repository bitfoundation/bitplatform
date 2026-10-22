using Bit.Bink.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// The editing commands a whiteboard needs, driven through the browser: wheel and trackpad navigation, the system
/// clipboard, duplicating, the lasso.
/// </summary>
[TestClass]
public sealed class EditingTests : BinkTest
{
    /// <summary>The element count the canvas reports to assistive technology.</summary>
    private static async Task<int> ElementCountAsync(IPage page)
    {
        string text = await page.EvaluateAsync<string>("() => document.querySelector('.bit-bink-canvas [id$=\"-desc\"]').textContent");
        if (text.StartsWith("Empty", StringComparison.Ordinal)) return 0;
        var match = System.Text.RegularExpressions.Regex.Match(text, @"Drawing with (\d+) elements");
        Assert.IsTrue(match.Success, "Unexpected description: " + text);
        return int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task WaitForCountAsync(IPage page, int expected)
    {
        for (int i = 0; i < 40 && await ElementCountAsync(page) != expected; i++) await page.WaitForTimeoutAsync(100);
        Assert.AreEqual(expected, await ElementCountAsync(page));
    }

    private static Task ChooseToolAsync(IPage page, string name) =>
        page.Locator(".bb-segmented button", new PageLocatorOptions { HasTextString = name }).First.ClickAsync();

    private static async Task<(float X, float Y)> CanvasPointAsync(IPage page, float x, float y)
    {
        var box = (await page.Locator(".bit-bink-input").First.BoundingBoxAsync())!;
        return (box.X + x, box.Y + y);
    }

    [TestMethod]
    public async Task The_wheel_pans_and_a_pinch_zooms_without_scrolling_the_page()
    {
        var page = await OpenAsync("/whiteboard");
        await page.EvaluateAsync("() => { document.body.style.minHeight = '4000px'; window.scrollTo(0, 0); }");
        await DrawStrokeAsync(page, (300, 200), (420, 260));
        int[] before = await InkBoxAsync(page);

        (float x, float y) = await CanvasPointAsync(page, 400, 250);
        await page.Mouse.MoveAsync(x, y);
        await page.Mouse.WheelAsync(0, 100);
        await page.WaitForTimeoutAsync(300);
        int[] panned = await InkBoxAsync(page);
        AssertInRange(before[1] - panned[1], 90, 110, "vertical pan in pixels");
        Assert.AreEqual(0d, await page.EvaluateAsync<double>("() => window.scrollY"), "The page scrolled under the canvas.");

        // A trackpad pinch arrives as a wheel event with ctrlKey set.
        await page.Keyboard.DownAsync("Control");
        await page.Mouse.WheelAsync(0, -60);
        await page.Keyboard.UpAsync("Control");
        await page.WaitForTimeoutAsync(300);
        int[] zoomed = await InkBoxAsync(page);
        Assert.IsGreaterThan(panned[2] - panned[0] + 10, zoomed[2] - zoomed[0], "Pinching out should enlarge the ink.");
    }

    [TestMethod]
    public async Task Ctrl_D_duplicates_the_selection()
    {
        var page = await OpenAsync("/whiteboard");
        await DrawStrokeAsync(page, (200, 200), (320, 260));
        await WaitForCountAsync(page, 1);

        await page.Keyboard.PressAsync("Control+a");
        await page.Keyboard.PressAsync("Control+d");
        await WaitForCountAsync(page, 2);
        await page.Keyboard.PressAsync("Control+z");
        await WaitForCountAsync(page, 1);
    }

    [TestMethod]
    public async Task Copy_reaches_the_system_clipboard_and_a_paste_brings_it_back()
    {
        var context = await NewContextAsync();
        await context.GrantPermissionsAsync(["clipboard-read", "clipboard-write"], new BrowserContextGrantPermissionsOptions { Origin = BaseUrl });
        var page = await context.NewPageAsync();
        await GotoAsync(page, "/whiteboard");
        await WaitForCanvasAsync(page);
        await DrawStrokeAsync(page, (200, 200), (320, 260));
        await WaitForCountAsync(page, 1);

        await page.Keyboard.PressAsync("Control+a");
        await page.Keyboard.PressAsync("Control+c");
        string copied = "";
        for (int i = 0; i < 40 && !copied.StartsWith("{\"format\":\"bink\"", StringComparison.Ordinal); i++)
        {
            await page.WaitForTimeoutAsync(100);
            copied = await page.EvaluateAsync<string>("() => navigator.clipboard.readText()");
        }
        StringAssert.StartsWith(copied, "{\"format\":\"bink\"", "Ctrl+C should have written the selection to the system clipboard.");

        // The paste event is what Ctrl+V raises; dispatching it with the clipboard's content exercises the same path
        // without depending on how the automation maps the shortcut.
        await page.EvaluateAsync(
            """
            (text) => {
              const data = new DataTransfer();
              data.setData('text/plain', text);
              document.querySelector('.bit-bink-canvas').dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
            }
            """, copied);
        await WaitForCountAsync(page, 2);
    }

    [TestMethod]
    public async Task A_pasted_image_is_added_to_the_drawing()
    {
        var page = await OpenAsync("/whiteboard");
        await page.Locator(".bit-bink-canvas").First.FocusAsync();
        await page.EvaluateAsync(
            """
            async () => {
              const canvas = document.createElement('canvas');
              canvas.width = 40; canvas.height = 30;
              const ctx = canvas.getContext('2d');
              ctx.fillStyle = '#d00'; ctx.fillRect(0, 0, 40, 30);
              const blob = await new Promise(r => canvas.toBlob(r, 'image/png'));
              const data = new DataTransfer();
              data.items.add(new File([blob], 'shot.png', { type: 'image/png' }));
              document.querySelector('.bit-bink-canvas').dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
            }
            """);
        await WaitForCountAsync(page, 1);
        for (int i = 0; i < 20 && await CountInkAsync(page) < 500; i++) await page.WaitForTimeoutAsync(100);
        Assert.IsGreaterThan(500, await CountInkAsync(page), "The pasted image should be drawn.");
    }

    [TestMethod]
    public async Task The_lasso_selects_what_it_encloses_and_alt_drag_duplicates_it()
    {
        var page = await OpenAsync("/whiteboard");
        await DrawStrokeAsync(page, (200, 200), (260, 230));
        await DrawStrokeAsync(page, (500, 200), (560, 230));
        await WaitForCountAsync(page, 2);
        await ChooseToolAsync(page, "Select");
        await page.WaitForTimeoutAsync(200);

        // Alt switches the marquee to a lasso for this drag: a loop around the first stroke only.
        await page.Keyboard.DownAsync("Alt");
        (float x0, float y0) = await CanvasPointAsync(page, 170, 170);
        await page.Mouse.MoveAsync(x0, y0);
        await page.Mouse.DownAsync();
        foreach ((float x, float y) in new[] { (300f, 170f), (300f, 260f), (170f, 260f), (165f, 175f) })
        {
            (float px, float py) = await CanvasPointAsync(page, x, y);
            await page.Mouse.MoveAsync(px, py, new MouseMoveOptions { Steps = 6 });
        }
        await page.Mouse.UpAsync();
        await page.Keyboard.UpAsync("Alt");
        await page.WaitForTimeoutAsync(250);
        StringAssert.Contains(await page.Locator(".bit-bink-canvas [role=status]").TextContentAsync() ?? "", "1 elements selected");

        // Alt-dragging the selected stroke leaves it in place and moves a copy.
        await page.Keyboard.DownAsync("Alt");
        (float sx, float sy) = await CanvasPointAsync(page, 230, 215);
        await page.Mouse.MoveAsync(sx, sy);
        await page.Mouse.DownAsync();
        (float ex, float ey) = await CanvasPointAsync(page, 230, 400);
        await page.Mouse.MoveAsync(ex, ey, new MouseMoveOptions { Steps = 8 });
        await page.Mouse.UpAsync();
        await page.Keyboard.UpAsync("Alt");
        await WaitForCountAsync(page, 3);
        // Pressed straight after the release: the shortcut must not overtake the pointer-up still waiting for its
        // animation frame, or it would run while the drag's undo group is still open and be refused.
        await page.Keyboard.PressAsync("Control+z");
        await WaitForCountAsync(page, 2);
    }
}
