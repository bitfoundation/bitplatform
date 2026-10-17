using System.Globalization;
using Bit.Bink.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// Defects found by driving the demo pages in a browser. Each one passed every unit test, because it only shows
/// up when the real canvas, the real DOM and the real event order meet.
/// </summary>
[TestClass]
public sealed class DemoRegressionTests : BinkTest
{
    private static Task<string> StatusAsync(IPage page, int index = 0) => page.Locator(".statusbar").Nth(index).InnerTextAsync();

    private static async Task<int> ElementCountAsync(IPage page, int index = 0)
        => int.Parse(await page.Locator(".statusbar").Nth(index).Locator("strong").First.InnerTextAsync(), CultureInfo.InvariantCulture);

    private static ILocator Button(IPage page, string name)
        => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name, Exact = true });

    private static async Task DragAsync(IPage page, float x0, float y0, float x1, float y1, int steps = 12)
    {
        await page.Mouse.MoveAsync(x0, y0);
        await page.Mouse.DownAsync();
        for (int i = 1; i <= steps; i++)
        {
            await page.Mouse.MoveAsync(x0 + (x1 - x0) * i / steps, y0 + (y1 - y0) * i / steps);
        }
        await page.Mouse.UpAsync();
        await page.WaitForTimeoutAsync(250);
    }

    private static async Task<LocatorBoundingBoxResult> CanvasBoxAsync(IPage page, int index = 0)
        => (await page.Locator("canvas[data-bink-layer=\"document\"]").Nth(index).BoundingBoxAsync())!;

    [TestMethod]
    public async Task An_image_whose_size_is_known_is_drawn_on_screen()
    {
        var page = await OpenAsync("/image-annotation");

        // The sample is added with its size already known, the path that used to skip loading it in the browser,
        // so the document held an image the canvas never drew.
        await page.WaitForFunctionAsync(
            """
            () => {
              const c = document.querySelector('canvas[data-bink-layer="document"]');
              const p = c.getContext('2d').getImageData(Math.round(c.width / 2), Math.round(c.height / 2), 1, 1).data;
              return Math.abs(p[0] - 248) + Math.abs(p[1] - 249) + Math.abs(p[2] - 251) > 30;
            }
            """, null, new PageWaitForFunctionOptions { Timeout = 10_000 });

        // Opening the page is not an edit: undo must not be able to take the picture away.
        Assert.IsTrue(await Button(page, "Undo").IsDisabledAsync(), "undo can take the sample picture away");
    }

    [TestMethod]
    public async Task Text_typed_before_pressing_a_toolbar_button_is_kept()
    {
        var page = await OpenAsync("/whiteboard");
        var errors = new List<string>();
        page.Console += (_, message) => { if (message.Type == "error") lock (errors) errors.Add(message.Text); };
        var box = await CanvasBoxAsync(page);

        await Button(page, "Text").ClickAsync();
        await page.Mouse.ClickAsync(box.X + 120, box.Y + 80);
        await page.WaitForTimeoutAsync(250);
        await page.Keyboard.TypeAsync("kept");

        // The button takes focus on pointerdown and switches tools on click. The blur has to reach .NET before the
        // click does, or the tool change closes the editor and the text is thrown away.
        await Button(page, "Select").ClickAsync();
        await page.WaitForTimeoutAsync(400);

        Assert.AreEqual(1, await ElementCountAsync(page), "the typed text was not kept");
        Assert.AreEqual(0, await page.Locator("textarea.bit-bink-text-editor").CountAsync(), "the text editor is still open");
        lock (errors) Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task Undoing_a_highlight_removes_it_in_one_step()
    {
        var page = await OpenAsync("/document-annotation");
        var box = await CanvasBoxAsync(page);
        int page0 = await ElementCountAsync(page);

        await DragAsync(page, box.X + 50, box.Y + 140, box.X + 250, box.Y + 145);
        Assert.AreEqual(page0 + 1, await ElementCountAsync(page), "the drag did not add a highlight");

        // The page describes each new highlight; that edit has to join the highlight's own undo step.
        await Button(page, "Undo").ClickAsync();
        await page.WaitForTimeoutAsync(250);
        Assert.AreEqual(page0, await ElementCountAsync(page), "one undo did not remove the highlight");
        Assert.AreEqual(0, await page.Locator("li.card").CountAsync(), "the annotation list still shows the highlight");
    }

    [TestMethod]
    public async Task Deleting_a_diagram_node_takes_its_connectors_and_label_in_one_step()
    {
        var page = await OpenAsync("/diagram");
        var box = await CanvasBoxAsync(page);
        int before = await ElementCountAsync(page);

        // The sample's middle node sits at (330, 200) to (480, 260) and has two connectors and a label.
        await Button(page, "Select").ClickAsync();
        await page.Mouse.ClickAsync(box.X + 405, box.Y + 230);
        // Pointer input is processed once per animation frame, so the selection exists only after one has passed.
        await page.WaitForTimeoutAsync(250);
        await page.Keyboard.PressAsync("Delete");
        await page.WaitForTimeoutAsync(250);
        Assert.AreEqual(before - 4, await ElementCountAsync(page), "the node, its two connectors and its label were not all deleted");
        Assert.Contains("2 nodes, 0 connectors", await StatusAsync(page), StringComparison.Ordinal);

        await Button(page, "Undo").ClickAsync();
        await page.WaitForTimeoutAsync(250);
        Assert.AreEqual(before, await ElementCountAsync(page), "one undo did not bring everything back");
    }

    [TestMethod]
    public async Task Collaborators_converge_after_one_of_them_was_offline()
    {
        var page = await OpenAsync("/collaboration");
        var alex = await CanvasBoxAsync(page, 0);
        var sam = await CanvasBoxAsync(page, 1);
        var samOnline = page.Locator("label:has-text(\"Sam online\") input");

        await samOnline.UncheckAsync();
        await DragAsync(page, alex.X + 60, alex.Y + 80, alex.X + 220, alex.Y + 140);
        await DragAsync(page, sam.X + 60, sam.Y + 200, sam.X + 220, sam.Y + 260);
        await samOnline.CheckAsync();
        await page.WaitForTimeoutAsync(500);

        // Sam missed Alex's stroke while offline; it has to arrive on reconnect, not be lost.
        Assert.AreEqual(2, await ElementCountAsync(page, 0), "Alex does not have both strokes");
        Assert.AreEqual(2, await ElementCountAsync(page, 1), "Sam does not have both strokes");
        Assert.AreEqual("replicas identical", await page.Locator(".badge[role=status]").InnerTextAsync());
    }
}
