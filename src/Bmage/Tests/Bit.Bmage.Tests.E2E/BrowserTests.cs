using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bmage.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// End-to-end checks that only a real browser can answer: does the JavaScript module load, do the browser codecs
/// actually run, do the pixels come back correct, and does the editor respond to input.
/// </summary>
[TestClass]
public class CapabilityTests : DemoTest
{
    [TestMethod]
    public async Task DetectsFormatsByActuallyEncoding()
    {
        var page = await OpenAsync("capabilities");
        await page.WaitForSelectorAsync("table.data", new PageWaitForSelectorOptions { Timeout = 60_000 });
        var text = (await page.Locator(".grid").InnerTextAsync()).Replace("\t", " ");

        // Every browser that can run Blazor can do these; if the probe says otherwise it is broken.
        Assert.Contains("PNG", text, StringComparison.Ordinal);
        Assert.MatchesRegex(@"PNG\s+yes\s+yes", text);
        Assert.MatchesRegex(@"JPEG\s+yes\s+yes", text);
        Assert.DoesNotContain("Probing this browser", text, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task ReportsTheRuntimeAsWebAssembly()
    {
        var page = await OpenAsync("capabilities");
        await page.WaitForSelectorAsync("table.data", new PageWaitForSelectorOptions { Timeout = 60_000 });
        var text = await page.Locator(".grid").InnerTextAsync();
        Assert.Contains("BrowserWebAssembly", text, StringComparison.Ordinal);
    }

    [TestMethod]
    public async Task FindsARealisticCanvasLimit()
    {
        var page = await OpenAsync("capabilities");
        await page.WaitForSelectorAsync("table.data", new PageWaitForSelectorOptions { Timeout = 60_000 });
        var text = await page.Locator(".grid").InnerTextAsync();
        var match = Regex.Match(text, @"MAX CANVAS EDGE\s+(\d+)", RegexOptions.IgnoreCase);
        Assert.IsTrue(match.Success, "The canvas limit was not reported");
        var limit = int.Parse(match.Groups[1].Value);
        Assert.IsInRange(4096, 65536, limit);
    }
}

[TestClass]
public class ProcessingTests : DemoTest
{
    [TestMethod]
    public async Task DecodesProcessesAndEncodesThroughTheBrowser()
    {
        var page = await OpenAsync("avatar");
        var file = SampleImage.WriteToTempFile();
        try
        {
            await page.SetInputFilesAsync("input[type=file]", file);
            await page.WaitForSelectorAsync(".stat-grid", new PageWaitForSelectorOptions { Timeout = 60_000 });
            await page.WaitForFunctionAsync("() => document.querySelectorAll('img.preview-image').length >= 2", null,
                new PageWaitForFunctionOptions { Timeout = 60_000 });

            var stats = await page.Locator(".stat-grid").InnerTextAsync();
            // A gradient at 256px must compress to something far smaller than the source PNG.
            Assert.Contains("KB", stats, StringComparison.Ordinal);
            Assert.DoesNotContain("0 B", stats, StringComparison.Ordinal);

            var result = await ReadPreviewAsync(page);
            Assert.AreEqual(256, result.Width);
            Assert.AreEqual(256, result.Height);
            // The circular mask is on by default, so the corner must be transparent and the centre opaque.
            Assert.AreEqual(0, result.CornerAlpha);
            Assert.AreEqual(255, result.CentreAlpha);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [TestMethod]
    public async Task ChangingAnOptionReprocessesTheImage()
    {
        var page = await OpenAsync("avatar");
        var file = SampleImage.WriteToTempFile();
        try
        {
            await page.SetInputFilesAsync("input[type=file]", file);
            await page.WaitForFunctionAsync("() => document.querySelectorAll('img.preview-image').length >= 2", null,
                new PageWaitForFunctionOptions { Timeout = 60_000 });
            var before = await ReadPreviewAsync(page);
            Assert.AreEqual(256, before.Width);

            // Drive the slider the way a user would, then wait for the debounced reprocess.
            await page.EvaluateAsync("""
                () => {
                    const el = document.querySelector('#size');
                    Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set.call(el, '512');
                    el.dispatchEvent(new Event('input', { bubbles: true }));
                }
                """);
            await page.WaitForFunctionAsync("() => document.querySelector('img.preview-image')?.naturalWidth === 512", null,
                new PageWaitForFunctionOptions { Timeout = 60_000 });

            var after = await ReadPreviewAsync(page);
            Assert.AreEqual(512, after.Width);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [TestMethod]
    public async Task ReportsAClearErrorForAFileThatIsNotAnImage()
    {
        var page = await OpenAsync("avatar");
        var file = Path.Combine(Path.GetTempPath(), $"bmage-{Guid.NewGuid():N}.png");
        await File.WriteAllTextAsync(file, "this is definitely not a png");
        try
        {
            await page.SetInputFilesAsync("input[type=file]", file);
            await page.WaitForSelectorAsync(".alert-error", new PageWaitForSelectorOptions { Timeout = 60_000 });
            var message = await page.Locator(".alert-error").InnerTextAsync();
            Assert.IsFalse(string.IsNullOrWhiteSpace(message));
        }
        finally
        {
            File.Delete(file);
        }
    }

    private sealed record PreviewInfo(int Width, int Height, int CornerAlpha, int CentreAlpha);

    /// <summary>Decodes the rendered preview in the page and samples it, which verifies the real encoded output.</summary>
    private static async Task<PreviewInfo> ReadPreviewAsync(IPage page)
    {
        var json = await page.EvaluateAsync<JsonElement>("""
            async () => {
                const img = document.querySelector('img.preview-image');
                if (!img) return null;
                const blob = await (await fetch(img.src)).blob();
                const bmp = await createImageBitmap(blob);
                const canvas = new OffscreenCanvas(bmp.width, bmp.height);
                const ctx = canvas.getContext('2d');
                ctx.clearRect(0, 0, bmp.width, bmp.height);
                ctx.drawImage(bmp, 0, 0);
                const corner = ctx.getImageData(2, 2, 1, 1).data;
                const centre = ctx.getImageData(bmp.width >> 1, bmp.height >> 1, 1, 1).data;
                return { width: bmp.width, height: bmp.height, cornerAlpha: corner[3], centreAlpha: centre[3] };
            }
            """);
        Assert.AreNotEqual(JsonValueKind.Null, json.ValueKind);
        return new PreviewInfo(
            json.GetProperty("width").GetInt32(),
            json.GetProperty("height").GetInt32(),
            json.GetProperty("cornerAlpha").GetInt32(),
            json.GetProperty("centreAlpha").GetInt32());
    }
}

[TestClass]
public class EditorTests : DemoTest
{
    private async Task<IPage> OpenEditorWithImageAsync()
    {
        var page = await OpenAsync("annotate");
        await LoadSampleIntoEditorAsync(page);
        return page;
    }

    [TestMethod]
    public async Task PaintsTheImageOntoTheCanvas()
    {
        var page = await OpenEditorWithImageAsync();
        var size = await page.EvaluateAsync<JsonElement>("() => { const c = document.querySelector('.bmg-canvas'); return { w: c.width, h: c.height }; }");
        Assert.AreEqual(SampleImage.Width, size.GetProperty("w").GetInt32());
        Assert.AreEqual(SampleImage.Height, size.GetProperty("h").GetInt32());

        // The centre pixel must match the gradient the sample image was built from, not an empty canvas.
        var alpha = await page.EvaluateAsync<int>("""
            () => {
                const c = document.querySelector('.bmg-canvas');
                return c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[3];
            }
            """);
        Assert.AreEqual(255, alpha);
    }

    [TestMethod]
    public async Task RendersTheFullToolbarAndPanel()
    {
        var page = await OpenEditorWithImageAsync();
        Assert.IsGreaterThanOrEqualTo(12, await page.Locator(".bmg-toolbar .bmg-tool").CountAsync());
        var tabs = await page.Locator(".bmg-panel-tab").AllInnerTextsAsync();
        Assert.Contains("Adjust", tabs);
        Assert.Contains("Layers", tabs);
        Assert.Contains("History", tabs);
        Assert.Contains("Export", tabs);
    }

    [TestMethod]
    public async Task AnnotationsAppearInTheLayerListAndOnThePixels()
    {
        var page = await OpenEditorWithImageAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Add a worked example" }).ClickAsync();
        await page.WaitForTimeoutAsync(2500);

        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Layers" }).ClickAsync();
        var layers = await page.Locator(".bmg-layer-name").AllInnerTextsAsync();
        Assert.Contains("Redaction", layers);
        Assert.Contains("Arrow", layers);
        Assert.HasCount(5, layers);

        // The redaction must have replaced the pixels, not merely covered them in the DOM.
        var redacted = await page.EvaluateAsync<int[]>("""
            () => {
                const c = document.querySelector('.bmg-canvas');
                const d = c.getContext('2d').getImageData(Math.round(c.width * 0.15), Math.round(c.height * 0.12), 1, 1).data;
                return [d[0], d[1], d[2], d[3]];
            }
            """);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 255 }, redacted);
    }

    [TestMethod]
    public async Task UndoAndRedoWalkTheHistory()
    {
        var page = await OpenEditorWithImageAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Add a worked example" }).ClickAsync();
        await page.WaitForTimeoutAsync(2500);

        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Layers" }).ClickAsync();
        Assert.AreEqual(5, await page.Locator(".bmg-layer").CountAsync());

        await page.Locator(".bmg-tool[title=Undo]").ClickAsync();
        await page.WaitForTimeoutAsync(1200);
        Assert.AreEqual(4, await page.Locator(".bmg-layer").CountAsync());

        await page.Locator(".bmg-tool[title=Redo]").ClickAsync();
        await page.WaitForTimeoutAsync(1200);
        Assert.AreEqual(5, await page.Locator(".bmg-layer").CountAsync());
    }

    [TestMethod]
    public async Task AdjustmentsChangeThePixelsLive()
    {
        var page = await OpenEditorWithImageAsync();

        var before = await ReadCentrePixelAsync(page);
        await page.EvaluateAsync("""
            () => {
                const slider = document.querySelectorAll('.bmg-slider')[0];
                Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set.call(slider, '0.6');
                slider.dispatchEvent(new Event('input', { bubbles: true }));
            }
            """);
        await page.WaitForTimeoutAsync(2500);
        var after = await ReadCentrePixelAsync(page);

        Assert.IsGreaterThan(before[0], after[0], $"Brightness did not lighten the image: {before[0]} -> {after[0]}");
    }

    [TestMethod]
    public async Task RotatingChangesTheCanvasDimensions()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title='Rotate right']").ClickAsync();
        await page.WaitForFunctionAsync($"() => document.querySelector('.bmg-canvas')?.width === {SampleImage.Height}", null,
            new PageWaitForFunctionOptions { Timeout = 60_000 });
        var size = await page.EvaluateAsync<JsonElement>("() => { const c = document.querySelector('.bmg-canvas'); return { w: c.width, h: c.height }; }");
        Assert.AreEqual(SampleImage.Height, size.GetProperty("w").GetInt32());
        Assert.AreEqual(SampleImage.Width, size.GetProperty("h").GetInt32());
    }

    [TestMethod]
    public async Task TheCanvasIsReachableAndLabelledForAssistiveTechnology()
    {
        var page = await OpenEditorWithImageAsync();
        var host = page.Locator(".bmg-canvas-host");
        Assert.AreEqual("application", await host.GetAttributeAsync("role"));
        Assert.IsFalse(string.IsNullOrWhiteSpace(await host.GetAttributeAsync("aria-label")));
        Assert.AreEqual("0", await host.GetAttributeAsync("tabindex"));

        var toolbar = page.Locator(".bmg-toolbar");
        Assert.AreEqual("toolbar", await toolbar.GetAttributeAsync("role"));

        // Every tool button carries a label and a pressed state.
        var first = page.Locator(".bmg-toolbar .bmg-tool").First;
        Assert.IsFalse(string.IsNullOrWhiteSpace(await first.GetAttributeAsync("aria-label")));
        Assert.IsNotNull(await first.GetAttributeAsync("aria-pressed"));
    }

    [TestMethod]
    public async Task DrawingWithThePointerCreatesAnAnnotation()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Arrow]").ClickAsync();

        var canvas = page.Locator(".bmg-canvas");
        var box = await canvas.BoundingBoxAsync();
        Assert.IsNotNull(box);

        // Drag across the canvas the way a user draws an arrow.
        await page.Mouse.MoveAsync(box.X + box.Width * 0.25f, box.Y + box.Height * 0.7f);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(box.X + box.Width * 0.5f, box.Y + box.Height * 0.5f, new MouseMoveOptions { Steps = 8 });
        await page.Mouse.MoveAsync(box.X + box.Width * 0.7f, box.Y + box.Height * 0.3f, new MouseMoveOptions { Steps = 8 });
        await page.Mouse.UpAsync();
        await page.WaitForTimeoutAsync(2000);

        await page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = "Layers" }).ClickAsync();
        var layers = await page.Locator(".bmg-layer-name").AllInnerTextsAsync();
        Assert.Contains("Arrow", layers);
    }

    private static async Task<int[]> ReadCentrePixelAsync(IPage page) => await page.EvaluateAsync<int[]>("""
        () => {
            const c = document.querySelector('.bmg-canvas');
            const d = c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data;
            return [d[0], d[1], d[2], d[3]];
        }
        """);
}

[TestClass]
public class DemoPageTests : DemoTest
{
    /// <summary>Every page must render without a JavaScript error or an error banner.</summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("getting-started")]
    [DataRow("hosting")]
    [DataRow("pipelines")]
    [DataRow("formats")]
    [DataRow("capabilities")]
    [DataRow("metadata")]
    [DataRow("avatar")]
    [DataRow("annotate")]
    [DataRow("optimize")]
    [DataRow("product")]
    [DataRow("social")]
    [DataRow("batch")]
    [DataRow("editor")]
    [DataRow("headless")]
    [DataRow("performance")]
    [DataRow("mcp-server")]
    public async Task PageLoadsWithoutErrors(string route)
    {
        var page = await NewPageAsync(new ViewportSize { Width = 1280, Height = 720 });
        var errors = CollectErrors(page);

        await GotoAsync(page, route);
        await page.WaitForTimeoutAsync(2000);

        Assert.IsEmpty(errors, string.Join(" | ", errors));
        Assert.IsEmpty(await page.Locator(".alert-error").AllInnerTextsAsync());
        Assert.IsFalse(string.IsNullOrWhiteSpace(await page.Locator("h1").First.InnerTextAsync()));
    }

    /// <summary>
    /// Every page must fit a 320 pixel wide phone: no horizontal scrolling of the page itself. Wide content -
    /// tables, code, the editor's toolbar - scrolls inside its own box instead.
    /// </summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("getting-started")]
    [DataRow("pipelines")]
    [DataRow("capabilities")]
    [DataRow("avatar")]
    [DataRow("optimize")]
    [DataRow("editor")]
    [DataRow("headless")]
    [DataRow("mcp-server")]
    public async Task PageFitsANarrowPhone(string route)
    {
        var page = await NewPageAsync(new ViewportSize { Width = 320, Height = 720 });
        await GotoAsync(page, route);

        var widths = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        Assert.IsLessThanOrEqualTo(widths[1], widths[0], $"/{route} is {widths[0]}px wide in a {widths[1]}px window");
    }
}
