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

    /// <summary>The live document readout under the editor; on the editor page it is the only <c>pre.demo-readout</c>.</summary>
    private static Task<string> ReadDocumentAsync(IPage page) => page.Locator("pre.demo-readout").First.InnerTextAsync();

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
        // Firefox empties the clipboardData a synthetic paste event is given, so no file reaches the page; a real paste
        // from the system clipboard is not affected.
        if (E2EEnvironment.Browser == "firefox") Assert.Inconclusive("Firefox drops the data of a synthetic paste event, so a paste cannot be simulated.");
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
            await page.WaitForSelectorAsync("table.optimize-results", new PageWaitForSelectorOptions { Timeout = 60_000 });
            Assert.Contains("Not a readable image", await page.Locator("table.optimize-results").InnerTextAsync(), StringComparison.Ordinal);
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
    public async Task TheEditorPagesReadoutShowsAnImageOpenedThroughTheEditorItself()
    {
        // Opening an image is not an edit, so OnDocumentChanged waits for the first change; the page has to take the
        // new document from OnImageLoaded, or it goes on showing the previous image's edit.
        var page = await OpenEditorWithImageAsync();
        await page.WaitForFunctionAsync($"() => /Source\\s+{SampleImage.Width} × {SampleImage.Height}/.test(document.querySelector('pre.demo-readout')?.textContent ?? '')",
            null, new PageWaitForFunctionOptions { Timeout = 10_000 });
        StringAssert.Contains(await ReadDocumentAsync(page), "Layers (0, bottom first)");
    }

    [TestMethod]
    public async Task TheExportTabStartsFromTheEditorsExportOptions()
    {
        // The editor page exports with MetadataPolicy.StripSensitive; the built-in save button used to start from Remove.
        var page = await OpenEditorWithImageAsync();
        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Export" }).ClickAsync();
        Assert.AreEqual("StripSensitive", await page.Locator(".bmg-panel-body select").Nth(1).InputValueAsync());
    }

    [TestMethod]
    public async Task StepNumbersAreDrawnWithTheSitesFont()
    {
        // Text is drawn from a font file; without the one the site registers, a step marker is a plain red disc.
        var page = await OpenAsync("annotate");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Use a sample image" }).ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelector('.bmg-canvas')?.width > 1", null, new PageWaitForFunctionOptions { Timeout = 60_000 });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Add a worked example" }).ClickAsync();
        // The worked example puts step 1 at 26% across and 82% down: white digits on red.
        await page.WaitForFunctionAsync("""
            () => {
                const c = document.querySelector('.bmg-canvas');
                const px = c.getContext('2d').getImageData(Math.floor(c.width * 0.245), Math.floor(c.height * 0.8), Math.ceil(c.width * 0.03), Math.ceil(c.height * 0.04)).data;
                for (let i = 0; i < px.length; i += 4) if (px[i] > 220 && px[i + 1] > 220 && px[i + 2] > 220) return true;
                return false;
            }
            """, null, new PageWaitForFunctionOptions { Timeout = 30_000 });
    }

    [TestMethod]
    public async Task APersianLabelIsDrawnIntoTheImage()
    {
        // The site registers Vazirmatn beside Noto Sans: a Persian label is shaped and drawn, with no word about fonts.
        var page = await OpenEditorWithImageAsync();
        var vazirmatn = page.WaitForResponseAsync(r => r.Url.EndsWith("/fonts/Vazirmatn-Regular.ttf", StringComparison.Ordinal) && r.Ok,
            new PageWaitForResponseOptions { Timeout = 30_000 });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "فارسی" }).ClickAsync();
        // The font is fetched once the page is interactive; it may have arrived before this test asked.
        try { await vazirmatn; } catch (TimeoutException) { }

        const string Region = "[Math.floor(c.width * 0.45), Math.floor(c.height * 0.35), Math.ceil(c.width * 0.3), Math.ceil(c.height * 0.2)]";
        var before = await page.EvaluateAsync<int[]>($"() => {{ const c = document.querySelector('.bmg-canvas'); return Array.from(c.getContext('2d').getImageData(...{Region}).data); }}");
        await page.Locator(".bmg-tool[title='متن']").ClickAsync();
        var canvas = (await page.Locator(".bmg-canvas").BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(canvas.X + canvas.Width * 0.5f, canvas.Y + canvas.Height * 0.45f);
        await page.WaitForTimeoutAsync(1200);

        var messages = await page.Locator(".bmg-editor [role=status], .bmg-editor [role=alert]").AllInnerTextsAsync();
        Assert.IsFalse(messages.Any(m => m.Contains("قلم", StringComparison.Ordinal) || m.Contains("font", StringComparison.OrdinalIgnoreCase)), string.Join(" | ", messages));
        var after = await page.EvaluateAsync<int[]>($"() => {{ const c = document.querySelector('.bmg-canvas'); return Array.from(c.getContext('2d').getImageData(...{Region}).data); }}");
        var changed = 0;
        for (var i = 0; i < before.Length; i += 4)
            if (Math.Abs(before[i] - after[i]) + Math.Abs(before[i + 1] - after[i + 1]) + Math.Abs(before[i + 2] - after[i + 2]) > 60) changed++;
        Assert.IsGreaterThan(30, changed, "the label 'متن' was drawn");
    }

    [TestMethod]
    public async Task StoppingABatchKeepsWhatItFinished()
    {
        var page = await OpenAsync("batch");
        await page.Locator("#conc").FillAsync("1");
        var files = Enumerable.Range(0, 8).Select(i => SampleImage.WriteToTempFile(3000, 2000, $"large-{i}.png")).ToArray();
        try
        {
            await page.SetInputFilesAsync("input[type=file]", files);
            await page.WaitForFunctionAsync("() => /^[1-7] of 8$/.test(document.querySelector('.optimize-progress [role=status]')?.textContent?.trim() ?? '')", null,
                new PageWaitForFunctionOptions { Timeout = 120_000, PollingInterval = 50 });
            // Without waiting for the button to hold still: where the browser decodes on the page's own thread (no
            // OffscreenCanvas), frames are few while an image is processed, and the click is handled between images anyway.
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Stop" }).ClickAsync(new LocatorClickOptions { Force = true });
            await page.WaitForSelectorAsync(".batch-stopped", new PageWaitForSelectorOptions { Timeout = 60_000 });

            var rows = await page.Locator(".results-table tbody tr").AllInnerTextsAsync();
            Assert.HasCount(8, rows);
            Assert.IsTrue(rows.Any(r => r.Contains("WEBP", StringComparison.Ordinal) || r.Contains("JPEG", StringComparison.Ordinal)), "a finished image is kept");
            Assert.IsTrue(rows.Any(r => r.Contains("stopped", StringComparison.Ordinal)), "the rest are marked");
            Assert.AreEqual(0, await page.Locator(".demo-error").CountAsync());
            Assert.IsFalse(await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Download results" }).IsDisabledAsync());
        }
        finally
        {
            foreach (var file in files) File.Delete(file);
        }
    }

    [TestMethod]
    public async Task EscapeClosesTheSiteMenuAndReturnsFocusToItsButton()
    {
        // The header is the same file in every Utilities demo; its drawer closes the way the docs drawer and the search do.
        var page = await OpenAsync("pipelines", new ViewportSize { Width = 375, Height = 760 });
        var button = page.Locator(".bb-menu-btn");
        await button.ClickAsync();
        await page.WaitForSelectorAsync("#bb-header-drawer");
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForSelectorAsync("#bb-header-drawer", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 5_000 });
        Assert.AreEqual("false", await button.GetAttributeAsync("aria-expanded"));

        // From a link inside the drawer, too, and the focus goes back to the button rather than to the page's start.
        await button.ClickAsync();
        await page.Locator("#bb-header-drawer a").First.FocusAsync();
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForSelectorAsync("#bb-header-drawer", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 5_000 });
        await page.WaitForFunctionAsync("() => document.activeElement?.classList.contains('bb-menu-btn')", null, new PageWaitForFunctionOptions { Timeout = 5_000 });

        // The docs drawer, the same way. Opening either takes the focus into it, which is what lets Escape reach it in
        // Safari: a clicked button there does not take the focus.
        var toggle = page.Locator(".bb-docs-menu-toggle");
        await toggle.ClickAsync();
        await page.WaitForFunctionAsync("() => document.activeElement?.closest('#bb-docs-nav') != null", null, new PageWaitForFunctionOptions { Timeout = 5_000 });
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForFunctionAsync("() => document.querySelector('.bb-docs-menu-toggle')?.getAttribute('aria-expanded') === 'false'", null, new PageWaitForFunctionOptions { Timeout = 5_000 });
        await page.WaitForFunctionAsync("() => document.activeElement?.classList.contains('bb-docs-menu-toggle')", null, new PageWaitForFunctionOptions { Timeout = 5_000 });
    }

    [TestMethod]
    public async Task OpeningAFileThatIsNotAProjectSaysSo()
    {
        // Re-rendering the edit on screen used to clear the error before it was ever shown.
        var page = await OpenAsync("retouch");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Run on a sample image" }).ClickAsync();
        await page.WaitForSelectorAsync("img.pipeline-preview", new PageWaitForSelectorOptions { Timeout = 60_000 });
        var path = Path.Combine(Path.GetTempPath(), $"bmage-{Guid.NewGuid():N}.bmage");
        await File.WriteAllTextAsync(path, "not a project");
        try
        {
            await page.SetInputFilesAsync("input[type=file][accept='.bmage']", path);
            await page.WaitForSelectorAsync(".demo-error", new PageWaitForSelectorOptions { Timeout = 30_000 });
            Assert.AreEqual(1, await page.Locator("img.pipeline-preview").CountAsync());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task EnterInTheMcpSearchBoxSearches()
    {
        var page = await OpenAsync("mcp-server");
        var box = page.GetByRole(AriaRole.Searchbox, new PageGetByRoleOptions { Name = "Search Bmage" });
        await box.FillAsync("webp fallback");
        await box.PressAsync("Enter");
        await page.WaitForSelectorAsync(".mcp-hits li", new PageWaitForSelectorOptions { Timeout = 30_000 });
        StringAssert.EndsWith(page.Url, "/mcp-server");
    }

    [TestMethod]
    public async Task TheUploadResultsScrollInsideTheirBoxOnANarrowPhone()
    {
        // The results table's visually hidden "Actions" header used to be positioned against the page, past the edge
        // of the box that scrolls the table, and made the whole page scroll sideways.
        var page = await OpenAsync("optimize", new ViewportSize { Width = 320, Height = 700 });
        var file = SampleImage.WriteToTempFile();
        try
        {
            await page.SetInputFilesAsync("input[type=file]", file);
            await page.WaitForSelectorAsync("table.optimize-results", new PageWaitForSelectorOptions { Timeout = 60_000 });
            await page.WaitForFunctionAsync("() => !document.querySelector('.optimize-progress')", null, new PageWaitForFunctionOptions { Timeout = 60_000 });
            var overflow = await page.EvaluateAsync<int>("() => document.scrollingElement.scrollWidth - document.scrollingElement.clientWidth");
            Assert.IsLessThanOrEqualTo(0, overflow, $"the page scrolls sideways by {overflow}px");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [TestMethod]
    public async Task TheSampleImageRunsEveryHeadlessExample()
    {
        var page = await OpenAsync("headless");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Use a sample image" }).ClickAsync();
        await page.WaitForFunctionAsync("() => document.querySelectorAll('.headless-results .headless-result img').length === 11 && !document.querySelector('.working')", null,
            new PageWaitForFunctionOptions { Timeout = 180_000 });
        Assert.AreEqual(11, await page.Locator(".headless-results .headless-result").CountAsync());
    }

    [TestMethod]
    public async Task TheNewerToolsRunFromTheirDemoPages()
    {
        // Each page's live sample drives the operations, layers and measurements it documents; any of them throwing
        // shows as the page's error banner or a console error.
        static async Task SettleAsync(IPage page)
        {
            // Debounced, then a spinner until the render is done.
            await page.WaitForTimeoutAsync(300);
            await page.WaitForSelectorAsync(".working", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 60_000 });
        }

        static async Task AssertCleanAsync(IPage page, List<string> errors)
        {
            Assert.IsEmpty(errors, string.Join(" | ", errors));
            Assert.IsEmpty(await page.Locator(".demo-error").AllInnerTextsAsync());
        }

        var pipelines = await OpenAsync("pipelines");
        var errors = CollectErrors(pipelines);
        await pipelines.GetByRole(AriaRole.Button, new() { Name = "Run on a sample image" }).ClickAsync(new LocatorClickOptions { Timeout = 60_000 });
        await pipelines.WaitForSelectorAsync("img.pipeline-preview", new PageWaitForSelectorOptions { Timeout = 60_000 });
        foreach (var geometry in new[] { "Straighten 6°", "Pad 40px" })
        {
            await pipelines.GetByRole(AriaRole.Button, new() { Name = geometry, Exact = true }).ClickAsync();
            await SettleAsync(pipelines);
        }
        foreach (var step in new[] { "balance", "hsl", "bw", "duotone", "motion" })
            await pipelines.Locator($"#step-{step}").CheckAsync();
        await SettleAsync(pipelines);
        await AssertCleanAsync(pipelines, errors);

        var layers = await OpenAsync("layers-and-masks");
        errors = CollectErrors(layers);
        await layers.GetByRole(AriaRole.Button, new() { Name = "Run on a sample image" }).ClickAsync(new LocatorClickOptions { Timeout = 60_000 });
        await layers.WaitForSelectorAsync("img.preview-image", new PageWaitForSelectorOptions { Timeout = 60_000 });
        await layers.GetByRole(AriaRole.Button, new() { Name = "Clipped group", Exact = true }).ClickAsync();
        await SettleAsync(layers);
        var json = await layers.Locator(".layers-json").InnerTextAsync();
        StringAssert.Contains(json, "\"type\": \"group\"");
        StringAssert.Contains(json, "\"clipToLayerBelow\": true");
        await AssertCleanAsync(layers, errors);

        // Retouch: the eyedropper's white balance, then the trim, which crops the cut-out product to its bounds.
        var retouch = await OpenAsync("retouch");
        errors = CollectErrors(retouch);
        await retouch.GetByRole(AriaRole.Button, new() { Name = "Run on a sample image" }).ClickAsync(new LocatorClickOptions { Timeout = 60_000 });
        await retouch.WaitForSelectorAsync("img.pipeline-preview", new PageWaitForSelectorOptions { Timeout = 60_000 });
        await retouch.GetByRole(AriaRole.Button, new() { Name = "From the backdrop", Exact = true }).ClickAsync();
        await retouch.GetByText("The eyedropper read").WaitForAsync(new LocatorWaitForOptions { Timeout = 10_000 });
        const string OutputWidth = "() => parseInt(document.querySelectorAll('.playground-stats .bb-stat-value')[1].textContent)";
        var untrimmed = await retouch.EvaluateAsync<int>(OutputWidth);
        await retouch.Locator("#trim").CheckAsync();
        await retouch.WaitForFunctionAsync($"() => ({OutputWidth})() < {untrimmed}", null, new PageWaitForFunctionOptions { Timeout = 60_000 });
        await AssertCleanAsync(retouch, errors);

        var social = await OpenAsync("social");
        errors = CollectErrors(social);
        await social.GetByRole(AriaRole.Button, new() { Name = "Use a sample image" }).ClickAsync(new LocatorClickOptions { Timeout = 60_000 });
        await social.WaitForSelectorAsync("img.social-preview", new PageWaitForSelectorOptions { Timeout = 60_000 });
        await social.GetByRole(AriaRole.Button, new() { Name = "Duotone", Exact = true }).ClickAsync();
        await SettleAsync(social);
        await AssertCleanAsync(social, errors);
    }
}
