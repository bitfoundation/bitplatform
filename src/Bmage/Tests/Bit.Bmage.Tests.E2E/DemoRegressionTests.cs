using Bit.Bmage.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// Regression tests for defects found by driving every demo page in a browser: overlays drifting off the image,
/// pointer input lost over the crop handles, undo history, paste, and page state that only shows up in a real DOM.
/// </summary>
[TestClass]
public class DemoRegressionTests : DemoTest
{
    private async Task<IPage> OpenEditorWithImageAsync()
    {
        var page = await OpenAsync("editor");
        await LoadSampleIntoEditorAsync(page);
        await page.WaitForTimeoutAsync(1000);
        return page;
    }

    private static async Task DragAsync(IPage page, float x0, float y0, float x1, float y1)
    {
        await page.Mouse.MoveAsync(x0, y0);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(x1, y1, new MouseMoveOptions { Steps = 8 });
        await page.Mouse.UpAsync();
        await page.WaitForTimeoutAsync(800);
    }

    /// <summary>The live document readout under the editor; on the editor page it is the only <c>pre.code</c>.</summary>
    private static Task<string> ReadDocumentAsync(IPage page) => page.Locator("pre.code").First.InnerTextAsync();

    [TestMethod]
    public async Task TheImageIsFittedInsideTheEditingArea()
    {
        var page = await OpenEditorWithImageAsync();
        var canvas = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        var host = (await page.Locator(".bmg-canvas-host").BoundingBoxAsync())!;
        Assert.IsInRange(host.X, host.X + host.Width, canvas.X);
        Assert.IsInRange(host.Y, host.Y + host.Height, canvas.Y);
        Assert.IsTrue(canvas.X + canvas.Width <= host.X + host.Width + 1 && canvas.Y + canvas.Height <= host.Y + host.Height + 1,
            $"the image ({canvas.Width}x{canvas.Height}) overflows the editing area ({host.Width}x{host.Height})");
    }

    [TestMethod]
    public async Task TheCropFrameFollowsTheDragAndItsHandles()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Crop]").ClickAsync();
        var canvas = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        var x = canvas.X + canvas.Width * 0.2f;
        var y = canvas.Y + canvas.Height * 0.2f;
        await DragAsync(page, x, y, x + 200, y + 120);

        var frame = (await page.Locator(".bmg-crop-frame").BoundingBoxAsync())!;
        Assert.IsInRange(x - 2, x + 2, frame.X);
        Assert.IsInRange(y - 2, y + 2, frame.Y);
        Assert.IsInRange(198, 202, frame.Width);

        // Shrinking from a handle moves the pointer over the frame; the drag must keep tracking it.
        var handle = (await page.Locator(".bmg-handle-bottomright").BoundingBoxAsync())!;
        await DragAsync(page, handle.X + handle.Width / 2, handle.Y + handle.Height / 2,
            handle.X + handle.Width / 2 - 60, handle.Y + handle.Height / 2 - 30);
        frame = (await page.Locator(".bmg-crop-frame").BoundingBoxAsync())!;
        Assert.IsInRange(137, 143, frame.Width);
        Assert.IsInRange(87, 93, frame.Height);

        // And the next drag still works: a release over the frame must not leave the pointer stuck.
        await DragAsync(page, frame.X + frame.Width / 2, frame.Y + frame.Height / 2,
            frame.X + frame.Width / 2 + 40, frame.Y + frame.Height / 2 + 20);
        var moved = (await page.Locator(".bmg-crop-frame").BoundingBoxAsync())!;
        Assert.IsInRange(38, 42, moved.X - frame.X);
    }

    [TestMethod]
    public async Task ThePanToolMovesTheImageWithThePointer()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Pan]").ClickAsync();
        var before = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        await DragAsync(page, before.X + 100, before.Y + 100, before.X + 200, before.Y + 150);
        var after = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        Assert.IsInRange(98, 102, after.X - before.X);
        Assert.IsInRange(48, 52, after.Y - before.Y);
    }

    [TestMethod]
    public async Task ShapesAndTheirSelectionLandWhereTheyWereDrawn()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Rectangle]").ClickAsync();
        var canvas = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        var x0 = canvas.X + canvas.Width * 0.25f;
        var y0 = canvas.Y + canvas.Height * 0.25f;
        await DragAsync(page, x0, y0, canvas.X + canvas.Width * 0.75f, canvas.Y + canvas.Height * 0.75f);

        // The document is in image pixels: the 800x600 sample, so a 25%..75% drag is (200,150) 400x300.
        var document = await ReadDocumentAsync(page);
        Assert.Contains("at (200,150) 400×300", document, StringComparison.Ordinal);

        await page.Locator(".bmg-tool[title=Select]").ClickAsync();
        await page.Mouse.ClickAsync(x0 + 1, canvas.Y + canvas.Height * 0.5f);
        await page.WaitForTimeoutAsync(500);
        var selection = (await page.Locator(".bmg-selection").BoundingBoxAsync())!;
        Assert.IsInRange(x0 - 3, x0 + 3, selection.X);
        Assert.IsInRange(y0 - 3, y0 + 3, selection.Y);
    }

    [TestMethod]
    public async Task OneUndoReversesMovingAnAnnotation()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Rectangle]").ClickAsync();
        var canvas = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        var x0 = canvas.X + canvas.Width * 0.25f;
        var ym = canvas.Y + canvas.Height * 0.5f;
        await DragAsync(page, x0, canvas.Y + canvas.Height * 0.25f, canvas.X + canvas.Width * 0.75f, canvas.Y + canvas.Height * 0.75f);
        var drawn = (await ReadDocumentAsync(page)).Split('\n').Last();

        await page.Locator(".bmg-tool[title=Select]").ClickAsync();
        await DragAsync(page, x0 + 1, ym, x0 + 61, ym + 30);
        var moved = (await ReadDocumentAsync(page)).Split('\n').Last();
        Assert.AreNotEqual(drawn, moved);

        await page.Locator(".bmg-canvas-host").FocusAsync();
        await page.Keyboard.PressAsync("Control+z");
        await page.WaitForTimeoutAsync(1200);
        Assert.AreEqual(drawn, (await ReadDocumentAsync(page)).Split('\n').Last());
    }

    [TestMethod]
    public async Task ChoosingAHistoryEntryShowsThatState()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Rectangle]").ClickAsync();
        var canvas = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        await DragAsync(page, canvas.X + 50, canvas.Y + 50, canvas.X + 150, canvas.Y + 120);
        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "History" }).ClickAsync();
        await page.Locator(".bmg-history-button").First.ClickAsync();
        await page.WaitForTimeoutAsync(1200);
        Assert.Contains("Annotations (0)", await ReadDocumentAsync(page), StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task PastingAnImageLoadsItIntoTheAnnotationEditor()
    {
        var page = await OpenAsync("annotate");
        await page.WaitForTimeoutAsync(1000);
        await page.EvaluateAsync("""
            (b64) => {
                const bytes = Uint8Array.from(atob(b64), c => c.charCodeAt(0));
                const data = new DataTransfer();
                data.items.add(new File([bytes], 'screenshot.png', { type: 'image/png' }));
                document.body.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
            }
            """, Convert.ToBase64String(SampleImage.Png));
        await page.WaitForFunctionAsync($"() => document.querySelector('.bmg-canvas')?.width === {SampleImage.Width}", null,
            new PageWaitForFunctionOptions { Timeout = 60_000 });
        await ScrollEditorIntoViewAsync(page);

        // The page keeps passing the pasted image to the editor; re-rendering must not reload it and lose the edit.
        await page.Locator(".bmg-tool[title=Rectangle]").ClickAsync();
        var canvas = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        await DragAsync(page, canvas.X + 40, canvas.Y + 40, canvas.X + 140, canvas.Y + 110);
        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Layers" }).ClickAsync();
        await page.WaitForTimeoutAsync(800);
        Assert.Contains("Rectangle", await page.Locator(".bmg-layer-name").AllInnerTextsAsync());
    }

    [TestMethod]
    public async Task OptimisingReportsFilesThatFailEvenWhenAllOfThemDo()
    {
        var page = await OpenAsync("optimize");
        var path = Path.Combine(Path.GetTempPath(), $"bmage-{Guid.NewGuid():N}-notes.txt");
        await File.WriteAllTextAsync(path, "not an image");
        try
        {
            await page.SetInputFilesAsync("input[type=file]", path);
            await page.WaitForSelectorAsync("table.data", new PageWaitForSelectorOptions { Timeout = 60_000 });
            Assert.Contains("Not a readable image", await page.Locator("table.data").InnerTextAsync(), StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task ToggleButtonsExposeTheirStateToAssistiveTechnology()
    {
        var page = await OpenAsync("product");
        Assert.AreEqual("true", await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "512", Exact = true }).GetAttributeAsync("aria-pressed"));
        Assert.AreEqual("false", await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "2048", Exact = true }).GetAttributeAsync("aria-pressed"));
    }

    [TestMethod]
    public async Task TheSampleImageRunsEveryHeadlessExample()
    {
        var page = await OpenAsync("headless");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Use a sample image" }).ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelectorAll('.grid .card img').length === 7 && !document.querySelector('.working')", null,
            new PageWaitForFunctionOptions { Timeout = 180_000 });
        Assert.AreEqual(7, await page.Locator(".grid .card").CountAsync());
    }
}
