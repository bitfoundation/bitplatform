using System.Globalization;
using Bit.Bink.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// Defects found by driving the demo pages in a browser. Each one passed every unit test, because it only shows
/// up when the real canvas, the real DOM and the real event order meet.
/// </summary>
[TestClass]
public sealed class DemoRegressionTests : BinkTest
{
    private static Task<string> StatusAsync(IPage page, int index = 0) => page.Locator(".bink-status").Nth(index).InnerTextAsync();

    private static async Task<int> ElementCountAsync(IPage page, int index = 0)
        => int.Parse(await page.Locator(".bink-status").Nth(index).Locator("strong").First.InnerTextAsync(), CultureInfo.InvariantCulture);

    private static async Task<int> UndoStepsAsync(IPage page)
        => int.Parse(await page.Locator(".bink-status").First.Locator("strong").Nth(2).InnerTextAsync(), CultureInfo.InvariantCulture);

    private static ILocator Button(IPage page, string name)
        => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name, Exact = true });

    /// <summary>A 2 by 2 PNG, for opening a picture of a known size.</summary>
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAFElEQVR42mP8z8DwnwEJMDGgAQBJXQIDxFqXkwAAAABJRU5ErkJggg==");

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
        Assert.AreEqual(0, await page.Locator("li.bink-annotation").CountAsync(), "the annotation list still shows the highlight");
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
        Assert.AreEqual("replicas identical", await page.Locator(".bb-badge[role=status]").InnerTextAsync());
    }

    [TestMethod]
    public async Task The_first_canvas_shows_its_status_line_before_anything_is_drawn()
    {
        var page = await OpenAsync("/getting-started");

        // The line reads the canvas through its @ref, which the page only sees when it renders again after the canvas
        // is ready. Nothing asked for that render, so the line stayed missing until the first stroke.
        await Expect(page.Locator(".bink-status")).ToContainTextAsync("0 elements");
    }

    [TestMethod]
    public async Task The_align_list_can_apply_the_same_alignment_twice()
    {
        var page = await OpenAsync("/whiteboard");
        await DrawStrokeAsync(page, (60, 60), (200, 100));
        await DrawStrokeAsync(page, (160, 160), (300, 200));
        var align = page.GetByLabel("Align the selection");

        await Button(page, "Select all").ClickAsync();
        await align.SelectOptionAsync("Left");
        await page.WaitForTimeoutAsync(250);
        int aligned = await UndoStepsAsync(page);

        // The list is a menu of actions. While it went on showing the last choice, picking that choice again for the
        // next selection was no change to the browser, so nothing was aligned.
        Assert.AreEqual(string.Empty, await align.InputValueAsync(), "the list still shows the alignment it applied");
        await Button(page, "Undo").ClickAsync();
        await Button(page, "Select all").ClickAsync();
        await align.SelectOptionAsync("Left");
        await page.WaitForTimeoutAsync(250);
        Assert.AreEqual(aligned, await UndoStepsAsync(page), "picking the same alignment again did nothing");
    }

    [TestMethod]
    public async Task Undoing_an_opened_picture_names_the_picture_that_came_back()
    {
        var page = await OpenAsync("/image-annotation");
        var status = page.Locator(".bink-status");
        await Expect(status).ToContainTextAsync("sample.png: 640 by 400 pixels");

        await page.Locator("input[type=file]").SetInputFilesAsync(new FilePayload { Name = "tiny.png", MimeType = "image/png", Buffer = TinyPng });
        await Expect(status).ToContainTextAsync("tiny.png: 2 by 2 pixels");

        // The line was written when a picture was placed, so undo brought the sample back under the new picture's name.
        await Button(page, "Undo").ClickAsync();
        await Expect(status).ToContainTextAsync("sample.png: 640 by 400 pixels");
        await Button(page, "Redo").ClickAsync();
        await Expect(status).ToContainTextAsync("tiny.png: 2 by 2 pixels");
    }

    [TestMethod]
    public async Task A_file_that_is_not_a_picture_is_refused_and_the_picture_stays()
    {
        var page = await OpenAsync("/image-annotation");
        var status = page.Locator(".bink-status");
        await Expect(status).ToContainTextAsync("sample.png: 640 by 400 pixels");

        // It was placed as a picture with no size: the sample vanished and nothing said why.
        await page.Locator("input[type=file]").SetInputFilesAsync(new FilePayload { Name = "notes.png", MimeType = "image/png", Buffer = "not a picture"u8.ToArray() });
        await Expect(page.GetByRole(AriaRole.Alert)).ToContainTextAsync("notes.png could not be opened as a picture");
        await Expect(status).ToContainTextAsync("sample.png: 640 by 400 pixels");
    }

    [TestMethod]
    public async Task On_a_phone_the_picture_to_annotate_is_shown_whole()
    {
        var context = await NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 390, Height = 844 },
            DeviceScaleFactor = 1,
            HasTouch = true,
        });
        var page = await context.NewPageAsync();
        await GotoAsync(page, "/image-annotation");
        await WaitForCanvasAsync(page);

        // The 900 unit page opened at 100% in a canvas a third as wide, which showed a third of the picture. Fitted, the
        // 640 unit picture fills the canvas's width less 8 pixels a side. That width depends on the engine's scrollbar.
        var status = page.Locator(".bink-status");
        await Expect(status).ToContainTextAsync("sample.png");
        double width = (await page.Locator(".bit-bink-input").First.BoundingBoxAsync())!.Width;
        string shown = System.Text.RegularExpressions.Regex.Match(await status.InnerTextAsync(), @"Zoom\s+(\d+)%").Groups[1].Value;
        AssertInRange(int.Parse(shown, CultureInfo.InvariantCulture), (width - 16) / 640 * 100 - 1, (width - 16) / 640 * 100 + 1, "the zoom in percent");
    }

    [TestMethod]
    public async Task Emptying_a_comment_shows_the_text_it_keeps()
    {
        var page = await OpenAsync("/document-annotation");
        var box = await CanvasBoxAsync(page);
        await Button(page, "Add comment").ClickAsync();
        await DragAsync(page, box.X + 420, box.Y + 250, box.X + 580, box.Y + 320);
        var comment = page.GetByLabel("Comment text");
        string text = await comment.InputValueAsync();

        // A comment keeps its text when its box is emptied, but the box stayed empty, showing text the comment no
        // longer matched.
        await comment.FillAsync(string.Empty);
        await comment.BlurAsync();
        await Expect(comment).ToHaveValueAsync(text);
    }

    [TestMethod]
    public async Task The_status_line_follows_a_zoom_no_button_made()
    {
        var page = await OpenAsync("/whiteboard");
        await page.Locator("[role=application]").First.FocusAsync();

        // Only the page's own buttons re-rendered the line, so a zoom from the keyboard, the wheel or a pinch left it
        // showing the old figure.
        await page.Keyboard.PressAsync("Control+Equal");
        await Expect(page.Locator(".bink-status")).ToContainTextAsync("Zoom 125%");
        await page.Keyboard.PressAsync("Control+0");
        await Expect(page.Locator(".bink-status")).ToContainTextAsync("Zoom 100%");
    }

    [TestMethod]
    public async Task Drawing_on_a_locked_layer_is_refused_and_the_page_says_why()
    {
        var page = await OpenAsync("/whiteboard");
        var layers = page.GetByRole(AriaRole.Group, new PageGetByRoleOptions { Name = "Layers" });
        await layers.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Lock", Exact = true }).ClickAsync();

        // The ink used to go on the layer above while the layer list still named the locked one. It is refused now,
        // and the page says why nothing is drawn.
        await Expect(page.GetByText("\"Drawing\" is locked: unlock it, or choose another layer, to draw.")).ToBeVisibleAsync();
        // The layer row is below the canvas, so reaching it scrolled the canvas's top out of the window.
        await page.Locator(".bit-bink-input").First.ScrollIntoViewIfNeededAsync();
        await DrawStrokeAsync(page, (60, 60), (200, 100));
        Assert.AreEqual(0, await ElementCountAsync(page), "the stroke went on a layer nobody chose");

        await layers.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Unlock", Exact = true }).ClickAsync();
        await page.Locator(".bit-bink-input").First.ScrollIntoViewIfNeededAsync();
        await DrawStrokeAsync(page, (60, 60), (200, 100));
        Assert.AreEqual(1, await ElementCountAsync(page), "the unlocked layer does not take the stroke");
    }

    [TestMethod]
    public async Task The_landing_canvas_does_not_ask_for_a_desynchronized_context()
    {
        // Chromium on Windows shows a desynchronized canvas that is not fully opaque as solid black inside a frame that
        // clips to rounded corners, which is where the landing page's sketch sits. What was asked for is recorded
        // rather than what the browser granted: most platforms ignore the hint, and the test has to fail on them too.
        var page = await (await NewContextAsync()).NewPageAsync();
        await page.AddInitScriptAsync(
            """
            window.binkContextRequests = [];
            const getContext = HTMLCanvasElement.prototype.getContext;
            HTMLCanvasElement.prototype.getContext = function (type, attributes) {
              if (this.dataset.binkLayer) window.binkContextRequests.push(!!(attributes && attributes.desynchronized));
              return getContext.call(this, type, attributes);
            };
            """);
        await GotoAsync(page, "/");
        await WaitForCanvasAsync(page);

        bool[] requests = await page.EvaluateAsync<bool[]>("() => window.binkContextRequests");
        Assert.IsNotEmpty(requests, "no canvas layer asked for a context");
        Assert.IsFalse(requests.Any(desynchronized => desynchronized), "a canvas layer asked for a desynchronized context");
    }

    [TestMethod]
    public async Task The_landing_sketch_ink_follows_the_theme_without_a_filter()
    {
        var context = await NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            DeviceScaleFactor = 1,
            ColorScheme = ColorScheme.Dark,
        });
        var page = await context.NewPageAsync();
        await GotoAsync(page, "/");
        await WaitForCanvasAsync(page);

        // The ink is drawn in the theme's own text colour: inverting the canvas with a CSS filter turned every colour
        // drawn on it into its complement, and cost a filter pass on every frame of the live stroke.
        Assert.AreEqual("none", await page.Locator(".bink-hero-live canvas[data-bink-layer=\"overlay\"]").EvaluateAsync<string>("c => getComputedStyle(c).filter"));

        await DrawStrokeAsync(page, (60, 60), (160, 120), (260, 70));
        await WaitForInkAsync(page, light: true, "the dark theme's ink is not light");

        // Ink already drawn takes the new theme's colour,
        await page.EvaluateAsync("() => bitDemoTheme.toggle()");
        await WaitForInkAsync(page, light: false, "the ink drawn in the dark theme stayed light in the light one");

        // and so does ink that undo puts back as history recorded it: cleared in the light theme, it comes back in
        // the dark one with the light theme's ink.
        await Button(page, "Clear").ClickAsync();
        await page.EvaluateAsync("() => bitDemoTheme.toggle()");
        await Button(page, "Undo").ClickAsync();
        await WaitForInkAsync(page, light: true, "the ink undo put back is in the other theme's colour");
    }

    /// <summary>Waits until the opaque pixels of the landing page's sketch are light, or dark.</summary>
    private static Task WaitForInkAsync(IPage page, bool light, string failure) => WaitOrFailAsync(page.WaitForFunctionAsync(
        """
        light => {
          const c = document.querySelector('.bink-hero-live canvas[data-bink-layer="document"]');
          const d = c.getContext('2d', { willReadFrequently: true }).getImageData(0, 0, c.width, c.height).data;
          let sum = 0, n = 0;
          for (let i = 0; i < d.length; i += 4) if (d[i + 3] > 200) { sum += d[i] + d[i + 1] + d[i + 2]; n++; }
          return n > 100 && (light ? sum / (3 * n) > 200 : sum / (3 * n) < 60);
        }
        """, light, new PageWaitForFunctionOptions { Timeout = 10_000 }), failure);

    private static async Task WaitOrFailAsync(Task wait, string failure)
    {
        try
        {
            await wait;
        }
        catch (PlaywrightException ex)
        {
            Assert.Fail($"{failure} ({ex.Message.Split('\n')[0]})");
        }
    }
}
