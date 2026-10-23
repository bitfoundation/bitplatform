using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bmage.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bmage.Tests.E2E;

/// <summary>
/// The editor's painting, filling, comparing, transform and layer tools, driven the way a user drives them: by
/// pointer and by the panel's controls, with the result read back from the canvas's pixels.
/// </summary>
[TestClass]
public class EditingToolTests : DemoTest
{
    private async Task<IPage> OpenEditorWithImageAsync()
    {
        var page = await OpenAsync("annotate");
        await LoadSampleIntoEditorAsync(page);
        return page;
    }

    /// <summary>The canvas pixel at a fraction of its width and height, as [r, g, b, a].</summary>
    private static Task<int[]> ReadPixelAsync(IPage page, double fx, double fy) => page.EvaluateAsync<int[]>("""
        ([fx, fy]) => {
            const c = document.querySelector('.bmg-canvas');
            const d = c.getContext('2d').getImageData(Math.floor(c.width * fx), Math.floor(c.height * fy), 1, 1).data;
            return [d[0], d[1], d[2], d[3]];
        }
        """, new[] { fx, fy });

    private static async Task<(int Width, int Height)> CanvasSizeAsync(IPage page)
    {
        var size = await page.EvaluateAsync<JsonElement>("() => { const c = document.querySelector('.bmg-canvas'); return { w: c.width, h: c.height }; }");
        return (size.GetProperty("w").GetInt32(), size.GetProperty("h").GetInt32());
    }

    /// <summary>Drags from one fraction of the canvas to another, in steps, as a user's pointer moves.</summary>
    private static async Task DragAsync(IPage page, (double X, double Y) from, (double X, double Y) to)
    {
        var box = await page.Locator(".bmg-canvas").BoundingBoxAsync();
        Assert.IsNotNull(box);
        await page.Mouse.MoveAsync((float)(box.X + box.Width * from.X), (float)(box.Y + box.Height * from.Y));
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync((float)(box.X + box.Width * to.X), (float)(box.Y + box.Height * to.Y), new MouseMoveOptions { Steps = 12 });
        await page.Mouse.UpAsync();
    }

    private static async Task ClickCanvasAsync(IPage page, double fx, double fy)
    {
        var box = await page.Locator(".bmg-canvas").BoundingBoxAsync();
        Assert.IsNotNull(box);
        await page.Mouse.ClickAsync((float)(box.X + box.Width * fx), (float)(box.Y + box.Height * fy));
    }

    /// <summary>Sets a range input by its accessible name and fires the events a drag and its release fire.</summary>
    // A slider's label carries its current value too, "Straighten 0", so it is found by the start of its name.
    private static Task SetSliderAsync(IPage page, string label, double value) => page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = label }).EvaluateAsync("""
        (slider, value) => {
            Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set.call(slider, String(value));
            slider.dispatchEvent(new Event('input', { bubbles: true }));
            slider.dispatchEvent(new Event('change', { bubbles: true }));
        }
        """, value);

    private static Task OpenTabAsync(IPage page, string name) => page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions { Name = name }).ClickAsync();

    private static Task WaitForCanvasAsync(IPage page, string condition) =>
        page.WaitForFunctionAsync($"() => {{ const c = document.querySelector('.bmg-canvas'); return c && ({condition}); }}", null, new PageWaitForFunctionOptions { Timeout = 60_000 });

    [TestMethod]
    public async Task CompareShowsTheOriginalUntilItIsTurnedOff()
    {
        var page = await OpenEditorWithImageAsync();
        var original = await ReadPixelAsync(page, 0.5, 0.5);

        await OpenTabAsync(page, "Filters");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Invert", Exact = true }).ClickAsync();
        await page.WaitForFunctionAsync($"() => document.querySelector('.bmg-canvas').getContext('2d').getImageData(400, 300, 1, 1).data[0] !== {original[0]}",
            null, new PageWaitForFunctionOptions { Timeout = 60_000 });
        var inverted = await ReadPixelAsync(page, 0.5, 0.5);

        var compare = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Compare with the original" });
        await compare.ClickAsync();
        await page.WaitForFunctionAsync($"() => document.querySelector('.bmg-canvas').getContext('2d').getImageData(400, 300, 1, 1).data[0] === {original[0]}",
            null, new PageWaitForFunctionOptions { Timeout = 60_000 });
        Assert.AreEqual("true", await compare.GetAttributeAsync("aria-pressed"));
        StringAssert.Contains(await page.Locator(".bmg-compare-badge").InnerTextAsync(), "original");

        await compare.ClickAsync();
        await page.WaitForFunctionAsync($"() => document.querySelector('.bmg-canvas').getContext('2d').getImageData(400, 300, 1, 1).data[0] === {inverted[0]}",
            null, new PageWaitForFunctionOptions { Timeout = 60_000 });
        Assert.AreEqual(0, await page.Locator(".bmg-compare-badge").CountAsync());
    }

    [TestMethod]
    public async Task TheBrushPaintsOntoANewPaintLayer()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Brush]").ClickAsync();
        await DragAsync(page, (0.2, 0.5), (0.8, 0.5));
        // The default drawing colour, painted where the brush passed.
        await WaitForCanvasAsync(page, "(() => { const d = c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data; return d[0] === 230 && d[1] === 40 && d[2] === 40; })()");

        await OpenTabAsync(page, "Layers");
        var layers = await page.Locator(".bmg-layer-name").AllInnerTextsAsync();
        Assert.Contains("Paint", layers);
        var untouched = await ReadPixelAsync(page, 0.5, 0.2);
        Assert.AreNotEqual(230, untouched[0], "the brush painted beyond its stroke");
    }

    [TestMethod]
    public async Task TheEraserRemovesPaintButNeverTheImage()
    {
        var page = await OpenEditorWithImageAsync();
        var image = await ReadPixelAsync(page, 0.5, 0.5);
        await page.Locator(".bmg-tool[title=Brush]").ClickAsync();
        await DragAsync(page, (0.2, 0.5), (0.8, 0.5));
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[1] === 40");

        await page.Locator(".bmg-tool[title=Eraser]").ClickAsync();
        await DragAsync(page, (0.5, 0.3), (0.5, 0.7));
        await WaitForCanvasAsync(page, $"c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[1] === {image[1]}");
        var erased = await ReadPixelAsync(page, 0.5, 0.5);
        CollectionAssert.AreEqual(image, erased, "the eraser shows the image again where it passed");
        var painted = await ReadPixelAsync(page, 0.3, 0.5);
        Assert.AreEqual(230, painted[0], "and leaves the rest of the stroke");
    }

    [TestMethod]
    public async Task TheFillToolFillsTheClickedRegionAsALayer()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Fill]").ClickAsync();
        await ClickCanvasAsync(page, 0.5, 0.5);
        await WaitForCanvasAsync(page, "(() => { const d = c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data; return d[0] === 230 && d[1] === 40 && d[2] === 40; })()");
        // The sample is a gradient: far enough away, the colour is beyond the tolerance and stays unfilled.
        var far = await ReadPixelAsync(page, 0.02, 0.02);
        Assert.AreNotEqual(230, far[0]);

        await OpenTabAsync(page, "Layers");
        Assert.Contains("Fill", await page.Locator(".bmg-layer-name").AllInnerTextsAsync());
    }

    [TestMethod]
    public async Task TheEyedropperPicksTheColourUnderThePointer()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title='Pick a colour']").ClickAsync();
        await ClickCanvasAsync(page, 0.25, 0.75);
        await page.WaitForSelectorAsync(".bmg-message", new PageWaitForSelectorOptions { Timeout = 30_000 });
        StringAssert.Contains(await page.Locator(".bmg-message").InnerTextAsync(), "Picked #");

        // The brush now paints with the picked colour, which the gradient says has red about a quarter of full.
        await page.Locator(".bmg-tool[title=Brush]").ClickAsync();
        await DragAsync(page, (0.6, 0.2), (0.9, 0.2));
        await page.WaitForTimeoutAsync(1500);
        var painted = await ReadPixelAsync(page, 0.75, 0.2);
        Assert.AreEqual(64, painted[0], 8, $"painted {string.Join(",", painted)}");
        Assert.AreEqual(191, painted[1], 8);
    }

    /// <summary>The histogram caption's clipping figures, in percent of the image.</summary>
    private static async Task<(double Black, double White)> ReadClippingAsync(IPage page)
    {
        var caption = await page.Locator(".bmg-histogram figcaption").InnerTextAsync();
        var match = Regex.Match(caption, @"([\d.]+)% black, ([\d.]+)% white");
        Assert.IsTrue(match.Success, caption);
        return (double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task TheHistogramFollowsTheImageAndReportsClipping()
    {
        var page = await OpenEditorWithImageAsync();
        var shape = page.Locator(".bmg-histogram path");
        await shape.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        var before = await shape.GetAttributeAsync("d");
        // The sample's brightest pixel has a luma of 252 and its darkest 3: nothing is clipped yet.
        var (black, white) = await ReadClippingAsync(page);
        Assert.AreEqual(0, black);
        Assert.AreEqual(0, white);

        await SetSliderAsync(page, "Exposure", 3);
        await page.WaitForFunctionAsync("before => document.querySelector('.bmg-histogram path')?.getAttribute('d') !== before",
            before, new PageWaitForFunctionOptions { Timeout = 60_000 });
        // Three stops brighter, every channel from 32 up clips, which is about three quarters of the gradient.
        (_, white) = await ReadClippingAsync(page);
        Assert.IsGreaterThan(50, white);
    }

    [TestMethod]
    public async Task StraighteningFromTheTransformTabCropsTheCorners()
    {
        var page = await OpenEditorWithImageAsync();
        await OpenTabAsync(page, "Transform");
        await SetSliderAsync(page, "Straighten", 10);
        await WaitForCanvasAsync(page, $"c.width < {SampleImage.Width}");
        var (width, height) = await CanvasSizeAsync(page);
        Assert.IsLessThan(SampleImage.Height, height);
        Assert.AreEqual(SampleImage.Width / (double)SampleImage.Height, width / (double)height, 0.02, "the crop keeps the photo's proportions");
        // No empty corners: the corner pixel is opaque photo.
        Assert.AreEqual(255, (await ReadPixelAsync(page, 0.001, 0.001))[3]);
    }

    [TestMethod]
    public async Task TheCanvasSizeAddsRoomAroundTheImage()
    {
        var page = await OpenEditorWithImageAsync();
        await OpenTabAsync(page, "Transform");
        var canvas = page.GetByRole(AriaRole.Group, new PageGetByRoleOptions { Name = "Canvas size" });
        await canvas.GetByLabel("Width").FillAsync("1000");
        await canvas.GetByLabel("Width").DispatchEventAsync("change");
        await canvas.GetByLabel("Height").FillAsync("700");
        await canvas.GetByLabel("Height").DispatchEventAsync("change");
        await canvas.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Apply" }).ClickAsync();
        await WaitForCanvasAsync(page, "c.width === 1000 && c.height === 700");
        // White around the centred photo, the photo where it was relative to its own centre.
        CollectionAssert.AreEqual(new[] { 255, 255, 255, 255 }, await ReadPixelAsync(page, 0.01, 0.5));
        Assert.AreEqual(255, (await ReadPixelAsync(page, 0.5, 0.5))[3]);
    }

    [TestMethod]
    public async Task AnArrowStaysOnWhatItMarksWhenTheImageIsTurned()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Arrow]").ClickAsync();
        // From the lower left towards the upper right; a point just inside the head is red before and after the turn.
        await DragAsync(page, (0.25, 0.7), (0.7, 0.3));
        await page.WaitForTimeoutAsync(1500);
        double[] inHead = [(560 - 8.3) / 800.0, (180 + 5.5) / 600.0];
        var before = await ReadPixelAsync(page, inHead[0], inHead[1]);
        Assert.IsGreaterThan(180, before[0], $"before the turn: {string.Join(",", before)}");

        await page.Locator(".bmg-tool[title='Rotate right']").ClickAsync();
        await WaitForCanvasAsync(page, $"c.width === {SampleImage.Height}");
        await page.WaitForTimeoutAsync(1000);
        // A quarter turn clockwise takes (x, y) of an 800 x 600 image to (600 - y, x).
        var after = await ReadPixelAsync(page, (600 - inHead[1] * 600) / 600.0, inHead[0] * 800 / 800.0);
        Assert.IsGreaterThan(180, after[0], $"after the turn the arrow is no longer on its target: {string.Join(",", after)}");
        Assert.IsLessThan(120, after[1]);
    }

    [TestMethod]
    public async Task ALayersOpacityAndBlendModeAreSetFromThePanel()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Brush]").ClickAsync();
        await DragAsync(page, (0.2, 0.5), (0.8, 0.5));
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[1] === 40");

        await OpenTabAsync(page, "Layers");
        await page.Locator(".bmg-layer-name", new PageLocatorOptions { HasText = "Paint" }).ClickAsync();
        await SetSliderAsync(page, "Opacity", 0.5);
        // Half the paint over the image: green half way between the image's and the paint's 40.
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[1] > 60");
        var half = await ReadPixelAsync(page, 0.5, 0.5);
        Assert.AreEqual((40 + 127) / 2.0, half[1], 6, $"half opacity: {string.Join(",", half)}");

        await page.GetByLabel("Blend mode").SelectOptionAsync("Multiply");
        await page.WaitForTimeoutAsync(1500);
        var multiplied = await ReadPixelAsync(page, 0.5, 0.5);
        Assert.IsLessThan(half[1], multiplied[1], "multiply darkens");
    }

    [TestMethod]
    public async Task TheNewControlsAreNamedForAssistiveTechnology()
    {
        var page = await OpenEditorWithImageAsync();
        foreach (var tool in new[] { "Brush", "Eraser", "Fill", "Magic eraser", "Pick a colour", "Compare with the original" })
        {
            var button = page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = tool, Exact = true });
            Assert.AreEqual(1, await button.CountAsync(), $"no single button named '{tool}'");
            Assert.IsNotNull(await button.GetAttributeAsync("aria-pressed"), $"'{tool}' does not say whether it is on");
        }
        await OpenTabAsync(page, "Transform");
        Assert.AreEqual(1, await page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Straighten" }).CountAsync(), "no slider named Straighten");
        await OpenTabAsync(page, "Layers");
        Assert.AreEqual(1, await page.GetByLabel("Add image layer").CountAsync());
    }

    [TestMethod]
    public async Task ARectangleSelectionIsOutlinedAndItsPixelsMadeTransparent()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title='Rectangle select']").ClickAsync();
        await DragAsync(page, (0.25, 0.25), (0.5, 0.5));
        // Marching ants around what was selected, and the actions on it in the toolbar.
        await page.WaitForSelectorAsync(".bmg-selection-outline path", new PageWaitForSelectorOptions { Timeout = 30_000 });
        StringAssert.StartsWith(await page.Locator(".bmg-ants").GetAttributeAsync("d") ?? "", "M");

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Make transparent" }).ClickAsync();
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(Math.floor(c.width * 0.375), Math.floor(c.height * 0.375), 1, 1).data[3] === 0");
        Assert.AreEqual(255, (await ReadPixelAsync(page, 0.75, 0.75))[3], "outside the selection nothing changed");

        // Escape lets go of the selection.
        await page.Locator(".bmg-canvas-host").FocusAsync();
        await page.Keyboard.PressAsync("Escape");
        await page.WaitForSelectorAsync(".bmg-selection-outline", new PageWaitForSelectorOptions { State = WaitForSelectorState.Detached, Timeout = 30_000 });
    }

    [TestMethod]
    public async Task TheSelectionComesBackWithTheImageWhenATurnIsUndone()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title='Rectangle select']").ClickAsync();
        // Taller than it is wide and off centre, so a selection left turned would cover somewhere else.
        await DragAsync(page, (0.1, 0.1), (0.3, 0.4));
        await page.WaitForSelectorAsync(".bmg-selection-outline path", new PageWaitForSelectorOptions { Timeout = 30_000 });

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Rotate right", Exact = true }).ClickAsync();
        await WaitForCanvasAsync(page, $"c.width === {SampleImage.Height}");
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Undo", Exact = true }).ClickAsync();
        await WaitForCanvasAsync(page, $"c.width === {SampleImage.Width}");

        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Make transparent" }).ClickAsync();
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(Math.floor(c.width * 0.2), Math.floor(c.height * 0.25), 1, 1).data[3] === 0");
        // Where the turned selection lay: x from 0.45 to 0.675, y from 0.13 to 0.4 of the image as it is again.
        Assert.AreEqual(255, (await ReadPixelAsync(page, 0.56, 0.25))[3], "the selection followed the image back");
    }

    [TestMethod]
    public async Task TheMagicWandSelectsARegionToCropTo()
    {
        // A plain square on a plain background: the wand inside it selects exactly the square.
        var page = await OpenAsync("annotate");
        var file = Path.Combine(Path.GetTempPath(), $"bmage-{Guid.NewGuid():N}-square.png");
        using (var image = ImageBuffer.Create(400, 300, Rgba32.White))
        {
            image.Fill(new System.Drawing.Rectangle(100, 80, 120, 90), new Rgba32(30, 90, 200));
            await File.WriteAllBytesAsync(file, Codecs.Png.PngEncoder.Encode(image).Data);
        }
        try
        {
            await page.SetInputFilesAsync(".bmg-editor input[type=file]", file);
            await WaitForCanvasAsync(page, "c.width === 400");
        }
        finally
        {
            File.Delete(file);
        }
        await ScrollEditorIntoViewAsync(page);

        await page.Locator(".bmg-tool[title='Magic wand']").ClickAsync();
        await ClickCanvasAsync(page, 160 / 400.0, 125 / 300.0);
        await page.WaitForSelectorAsync(".bmg-selection-outline path", new PageWaitForSelectorOptions { Timeout = 30_000 });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Crop to selection" }).ClickAsync();
        await WaitForCanvasAsync(page, "c.width === 120 && c.height === 90");
        var corner = await ReadPixelAsync(page, 0.01, 0.01);
        Assert.AreEqual(30, corner[0], 2, "the crop holds the square and nothing of the background");
    }

    [TestMethod]
    public async Task ASelectionCopiedToALayerBecomesAnImageLayer()
    {
        var page = await OpenEditorWithImageAsync();
        var original = await ReadPixelAsync(page, 0.45, 0.45);
        await page.Locator(".bmg-tool[title='Ellipse select']").ClickAsync();
        await DragAsync(page, (0.3, 0.3), (0.6, 0.6));
        await page.WaitForSelectorAsync(".bmg-selection-outline path", new PageWaitForSelectorOptions { Timeout = 30_000 });
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Copy to layer" }).ClickAsync();
        await OpenTabAsync(page, "Layers");
        await page.Locator(".bmg-layer-name", new PageLocatorOptions { HasText = "Copy to layer" }).WaitForAsync(new LocatorWaitForOptions { Timeout = 60_000 });

        // With the copy hidden, clear the image under the selection...
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Hide: Copy to layer" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Make transparent" }).ClickAsync();
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(Math.floor(c.width * 0.45), Math.floor(c.height * 0.45), 1, 1).data[3] === 0");

        // ...and shown again, the copy puts back exactly what was there: it holds the pixels, in their place.
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Show: Copy to layer" }).ClickAsync();
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(Math.floor(c.width * 0.45), Math.floor(c.height * 0.45), 1, 1).data[3] === 255");
        var restored = await ReadPixelAsync(page, 0.45, 0.45);
        for (var i = 0; i < 3; i++) Assert.AreEqual(original[i], restored[i], 2, $"channel {i}");
    }

    [TestMethod]
    public async Task PlacedTextIsEditedFromThePanel()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Text]").ClickAsync();
        await ClickCanvasAsync(page, 0.4, 0.4);

        // Placing text selects it, and the panel opens on its properties.
        var text = page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Text" });
        await text.WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
        Assert.AreEqual("Text", await text.InputValueAsync());
        await text.FillAsync("Hello there");
        await text.DispatchEventAsync("change");
        await SetSliderAsync(page, "Text Rotation", 30);
        await page.WaitForTimeoutAsync(1000);

        // The edit is in the document: away to another tab and back, the panel shows it again.
        await OpenTabAsync(page, "History");
        Assert.IsGreaterThanOrEqualTo(3, await page.Locator(".bmg-history-entry").CountAsync(), "placing, retyping and turning are each a step");
        await OpenTabAsync(page, "Tools");
        Assert.AreEqual("Hello there", await page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions { Name = "Text" }).InputValueAsync());
        StringAssert.Contains(await page.GetByRole(AriaRole.Slider, new PageGetByRoleOptions { Name = "Text Rotation" }).GetAttributeAsync("aria-valuetext") ?? "", "30");
    }

    [TestMethod]
    public async Task AProjectSavedAndReopenedIsStillEditable()
    {
        var page = await OpenEditorWithImageAsync();
        await page.Locator(".bmg-tool[title=Brush]").ClickAsync();
        await DragAsync(page, (0.2, 0.5), (0.8, 0.5));
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[1] === 40");

        await OpenTabAsync(page, "Export");
        var download = await page.RunAndWaitForDownloadAsync(() => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Save project" }).ClickAsync(),
            new PageRunAndWaitForDownloadOptions { Timeout = 60_000 });
        StringAssert.EndsWith(download.SuggestedFilename, ".bmage");
        var path = Path.Combine(Path.GetTempPath(), $"bmage-{Guid.NewGuid():N}.bmage");
        await download.SaveAsAsync(path);
        try
        {
            // Undo the painting, so only the reopened project can bring it back.
            await page.Locator(".bmg-tool[title=Undo]").ClickAsync();
            await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[1] !== 40");

            await page.SetInputFilesAsync(".bmg-panel input[type=file][aria-label='Open project']", path);
            await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[1] === 40");
        }
        finally
        {
            File.Delete(path);
        }

        // The stroke is a paint layer again, not pixels: it can still be erased.
        await OpenTabAsync(page, "Layers");
        Assert.Contains("Paint", await page.Locator(".bmg-layer-name").AllInnerTextsAsync());
        await page.Locator(".bmg-tool[title=Eraser]").ClickAsync();
        await DragAsync(page, (0.5, 0.3), (0.5, 0.7));
        await WaitForCanvasAsync(page, "c.getContext('2d').getImageData(c.width >> 1, c.height >> 1, 1, 1).data[1] !== 40");
    }
}
