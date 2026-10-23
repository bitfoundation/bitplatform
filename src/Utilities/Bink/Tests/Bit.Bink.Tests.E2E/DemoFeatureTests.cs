using System.Globalization;
using Bit.Bink.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bink.Tests.E2E;

/// <summary>
/// What the demo pages say they show, driven in a browser: each feature a page demonstrates does what the page's
/// text describes, through the page's own controls.
/// </summary>
[TestClass]
public sealed class DemoFeatureTests : BinkTest
{
    /// <summary>The element count a status line reports first.</summary>
    private static async Task<int> ElementCountAsync(IPage page, int index = 0)
        => int.Parse(await page.Locator(".bink-status").Nth(index).Locator("strong").First.InnerTextAsync(), CultureInfo.InvariantCulture);

    private static ILocator Button(IPage page, string name)
        => page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = name, Exact = true });

    private static async Task DragAsync(IPage page, float x0, float y0, float x1, float y1, int steps = 12)
    {
        await page.Mouse.MoveAsync(x0, y0);
        await page.Mouse.DownAsync();
        for (int i = 1; i <= steps; i++) await page.Mouse.MoveAsync(x0 + (x1 - x0) * i / steps, y0 + (y1 - y0) * i / steps);
        await page.Mouse.UpAsync();
        await page.WaitForTimeoutAsync(250);
    }

    private static async Task<LocatorBoundingBoxResult> CanvasBoxAsync(IPage page, int index = 0)
        => (await page.Locator("canvas[data-bink-layer=\"document\"]").Nth(index).BoundingBoxAsync())!;

    [TestMethod]
    public async Task An_imported_svg_becomes_editable_elements_in_one_undo_step()
    {
        var page = await OpenAsync("/whiteboard");
        const string svg = """
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 120">
              <rect x="10" y="10" width="80" height="60" fill="#fde047" stroke="#111827" stroke-width="3"/>
              <circle cx="150" cy="60" r="40" fill="#93c5fd"/>
              <path d="M10 110 L190 110" stroke="#dc2626" stroke-width="6" fill="none"/>
            </svg>
            """;

        await page.Locator("label:has-text(\"Import SVG\") input[type=file]").SetInputFilesAsync(
            new FilePayload { Name = "shapes.svg", MimeType = "image/svg+xml", Buffer = System.Text.Encoding.UTF8.GetBytes(svg) });

        await Expect(page.GetByText("Imported 3 elements from shapes.svg, grouped and selected.")).ToBeVisibleAsync();
        Assert.AreEqual(3, await ElementCountAsync(page), "the rectangle, the circle and the path were not all imported");
        await Expect(page.Locator(".bink-status")).ToContainTextAsync("3 selected");
        Assert.IsGreaterThan(1000, await CountInkAsync(page), "the imported shapes were not drawn");

        // Adding and grouping are one step to the user: one undo takes the whole import away.
        await Button(page, "Undo").ClickAsync();
        await page.WaitForTimeoutAsync(250);
        Assert.AreEqual(0, await ElementCountAsync(page), "one undo did not take the import away");
    }

    [TestMethod]
    public async Task The_picture_can_be_hidden_and_cropped_without_losing_its_pixels()
    {
        var page = await OpenAsync("/image-annotation");
        var status = page.Locator(".bink-status");
        await Expect(status).ToContainTextAsync("sample.png: 640 by 400 pixels");
        int shown = await CountInkAsync(page);

        await Button(page, "Hide picture").ClickAsync();
        await page.WaitForTimeoutAsync(250);
        Assert.IsLessThan(shown / 10, await CountInkAsync(page), "hiding the image layer left the picture on screen");
        await Button(page, "Show picture").ClickAsync();
        await page.WaitForTimeoutAsync(250);

        // A tenth off every side: 512 by 320 of the 640 by 400 source pixels, and one undo step.
        await Button(page, "Crop edges").ClickAsync();
        await Expect(status).ToContainTextAsync("cropped to 512 by 320");
        await Button(page, "Undo").ClickAsync();
        await Expect(status).Not.ToContainTextAsync("cropped");
        await Button(page, "Redo").ClickAsync();
        await Expect(status).ToContainTextAsync("cropped to 512 by 320");

        // The crop is a property, not an edit of the pixels: removing it brings the whole picture back.
        await Button(page, "Remove crop").ClickAsync();
        await Expect(status).ToContainTextAsync("sample.png: 640 by 400 pixels");
        await Expect(status).Not.ToContainTextAsync("cropped");
    }

    [TestMethod]
    public async Task A_participant_who_reloads_joins_again_from_a_snapshot()
    {
        var page = await OpenAsync("/collaboration");
        var alex = await CanvasBoxAsync(page, 0);
        await DragAsync(page, alex.X + 60, alex.Y + 80, alex.X + 220, alex.Y + 140);
        await DragAsync(page, alex.X + 60, alex.Y + 200, alex.X + 220, alex.Y + 260);
        Assert.AreEqual(2, await ElementCountAsync(page, 1), "Sam does not have Alex's strokes to begin with");

        // An empty document joins and asks for a snapshot; Alex answers with the drawing.
        await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Reload Sam's page", Exact = true }).ClickAsync();
        await page.WaitForTimeoutAsync(500);
        Assert.AreEqual(2, await ElementCountAsync(page, 1), "Sam did not get the drawing back from the snapshot");
        Assert.AreEqual("replicas identical", await page.Locator(".bb-badge[role=status]").InnerTextAsync());
        Assert.IsTrue(await Button(page, "Undo").Nth(1).IsDisabledAsync(), "the snapshot landed in Sam's undo history");

        // The new session is wired to the transport both ways.
        var sam = await CanvasBoxAsync(page, 1);
        await DragAsync(page, sam.X + 300, sam.Y + 80, sam.X + 420, sam.Y + 160);
        await page.WaitForTimeoutAsync(300);
        Assert.AreEqual(3, await ElementCountAsync(page, 0), "Alex did not receive what Sam drew after rejoining");
        Assert.AreEqual("replicas identical", await page.Locator(".bb-badge[role=status]").InnerTextAsync());
    }

    [TestMethod]
    public async Task A_picture_shown_twice_is_saved_once_in_a_version_2_file()
    {
        var page = await OpenAsync("/serialization");
        await Button(page, "Add a picture twice").ClickAsync();
        await page.WaitForTimeoutAsync(300);
        await Expect(page.Locator(".bink-hint").First).ToContainTextAsync("Not saved yet.");

        await Button(page, "Save").ClickAsync();
        await Expect(page.Locator(".bb-params")).ToContainTextAsync("2, with 1 picture stored once for several elements");
        await Expect(page.Locator(".bink-hint").First).ToContainTextAsync("Saved: nothing has changed since.");
    }

    [TestMethod]
    public async Task An_app_without_the_extensions_keeps_them_and_hands_them_back()
    {
        var page = await OpenAsync("/extending");
        var status = page.Locator(".bink-status");
        await Expect(status).ToContainTextAsync("1 sticky notes");
        await Expect(status).ToContainTextAsync("1 dashed strokes");

        // Read with the default registries: the note is kept as an element nobody here understands, and the stroke
        // keeps its brush kind while it is drawn as a plain pen.
        await Button(page, "Reload without the extensions").ClickAsync();
        await Expect(status).ToContainTextAsync("1 elements kept but not understood");
        await Expect(status).ToContainTextAsync("0 sticky notes");
        await Expect(status).ToContainTextAsync("1 dashed strokes");

        // Written back by that reader and read with the registrations, nothing was lost on the way.
        await Button(page, "Save and reload").ClickAsync();
        await Expect(status).ToContainTextAsync("1 sticky notes");
        await Expect(status).Not.ToContainTextAsync("not understood");
        await Expect(page.Locator("pre.bink-output")).ToContainTextAsync("\"demo-sticky-note\"");
        await Expect(page.Locator("pre.bink-output")).ToContainTextAsync("\"dashLength\"");
    }

    [TestMethod]
    public async Task A_vote_on_a_custom_element_is_an_ordinary_undoable_edit()
    {
        var page = await OpenAsync("/extending");
        var description = page.Locator(".bit-bink-canvas [id$=\"-desc\"]");

        await Button(page, "Add a sticky note").ClickAsync();
        await Expect(description).ToContainTextAsync("Ship the beta, 0 votes");
        await Button(page, "Vote").ClickAsync();
        await Expect(description).ToContainTextAsync("Ship the beta, 1 vote");

        await Button(page, "Undo").ClickAsync();
        await Expect(description).ToContainTextAsync("Ship the beta, 0 votes");
    }

    [TestMethod]
    public async Task A_shortcut_left_to_the_page_no_longer_reaches_the_canvas()
    {
        var page = await OpenAsync("/input-and-accessibility");
        int before = await ElementCountAsync(page);
        var leave = page.Locator("label:has-text(\"Leave Delete and R to the page\") input");

        await leave.CheckAsync();
        await page.WaitForTimeoutAsync(250);
        await page.Locator(".bit-bink-canvas").First.FocusAsync();
        // Enter selects the first element: navigating without a pointer starts there.
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForTimeoutAsync(200);
        await page.Keyboard.PressAsync("Delete");
        await page.WaitForTimeoutAsync(250);
        Assert.AreEqual(before, await ElementCountAsync(page), "Delete reached the canvas although it was left to the page");

        await leave.UncheckAsync();
        await page.WaitForTimeoutAsync(250);
        await page.Locator(".bit-bink-canvas").First.FocusAsync();
        await page.Keyboard.PressAsync("Delete");
        await page.WaitForTimeoutAsync(250);
        Assert.IsLessThan(before, await ElementCountAsync(page), "Delete did not delete once the canvas had it back");
    }
}
