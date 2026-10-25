using Bit.Bdnd.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Drag around: the container is a drop target that moves a box by how far the pointer travelled, which the library
/// works out from the pointer at <c>dragstart</c> and at <c>drop</c>. With a custom drag layer the browser's own drag
/// image is hidden and a preview follows the pointer instead, drawn from the offsets the module reports while the
/// drag runs.
/// </summary>
[TestClass]
public sealed class DragAroundTests : BdndTest
{
    [TestMethod]
    public async Task Dropping_a_box_moves_it_by_exactly_the_distance_the_pointer_travelled()
    {
        var page = await OpenAsync("/drag-around/naive");
        var box = page.Locator(".drag-around-box", new() { HasText = "Drag me around" });
        var before = await BoxAsync(box);
        var grab = before.Centre.Rounded();
        var drop = grab.Offset(180, 110);

        await StartDragAsync(page, grab);
        await Expect(box).ToHaveCSSAsync("opacity", "0.5");
        await DragToAsync(page, drop);
        await Expect(box).ToHaveCSSAsync("opacity", "1");

        // The browser starts a drag where the button went down (a few pixels of movement trigger it, but are not
        // part of it): that is where the travelled distance is measured from.
        var start = (await DragStartsAsync(page.MainFrame)).Single();
        Assert.AreEqual(grab, new Point(start.X, start.Y), "where the drag started");

        var after = await BoxAsync(box);
        AssertNear(before.TopLeft.Offset(drop.X - start.X, drop.Y - start.Y), after.TopLeft, 1, "the box after the drop");

        // The other box did not move.
        var other = await BoxAsync(page.Locator(".drag-around-box", new() { HasText = "Drag me too" }));
        AssertNear(before.TopLeft.Offset(160, 120), other.TopLeft, 1, "the box that was not dragged");
    }

    [TestMethod]
    public async Task The_custom_drag_layer_follows_the_pointer_and_goes_away_on_drop()
    {
        var page = await OpenAsync("/drag-around/custom-layer");
        var box = page.Locator(".drag-around-box", new() { HasText = "Drag me" });
        var preview = page.Locator(".drag-layer-preview");
        await Expect(preview).ToHaveCountAsync(0);

        var before = await BoxAsync(box);
        var grab = before.Centre.Rounded();
        await StartDragAsync(page, grab);
        await Expect(box).ToHaveCSSAsync("opacity", "0.5");
        var start = (await DragStartsAsync(page.MainFrame)).Single();

        // Wherever the pointer goes, the preview is drawn where the box would be if it moved with the pointer: the
        // box's corner at the start, plus how far the pointer has gone since.
        var at = grab;
        foreach (var (dx, dy) in new[] { (60.0, 40.0), (220.0, -30.0), (-40.0, 150.0) })
        {
            at = grab.Offset(dx, dy);
            await MoveAsync(page, at);
            var expected = before.TopLeft.Offset(at.X - start.X, at.Y - start.Y);
            await Expect(preview).ToHaveTextAsync("Drag me");
            await EventuallyAsync(async () => (await BoxAsync(preview)).TopLeft.DistanceTo(expected) <= 1,
                $"the preview is drawn at {expected} for the pointer at {at}");
        }

        await DragToAsync(page, at);

        // The drag is over: the preview is gone and the box itself was dropped where the preview was.
        await Expect(preview).ToHaveCountAsync(0);
        await Expect(box).ToHaveCSSAsync("opacity", "1");
        AssertNear(before.TopLeft.Offset(at.X - start.X, at.Y - start.Y), (await BoxAsync(box)).TopLeft, 1, "the box after the drop");
    }

    [TestMethod]
    public async Task Snapped_to_the_grid_the_preview_and_the_box_move_by_whole_cells()
    {
        var page = await OpenAsync("/drag-around/custom-layer");
        await page.GetByRole(Microsoft.Playwright.AriaRole.Button, new() { Name = "Snap to grid" }).ClickAsync();
        var board = await BoxAsync(page.Locator(".drag-around-container"));
        var box = page.Locator(".drag-around-box", new() { HasText = "Drag me" });
        var preview = page.Locator(".drag-layer-preview");

        var before = await BoxAsync(box);
        var grab = before.Centre.Rounded();
        await StartDragAsync(page, grab);
        var start = (await DragStartsAsync(page.MainFrame)).Single();
        var at = grab.Offset(57, 33);
        await MoveAsync(page, at);

        // the corner the pointer would put the box at, on the board (inside its 1px border), to the nearest 40px cell
        var left = before.X - board.X - 1 + (at.X - start.X);
        var top = before.Y - board.Y - 1 + (at.Y - start.Y);
        var snapped = new Point(board.X + 1 + Math.Round(left / 40) * 40, board.Y + 1 + Math.Round(top / 40) * 40);
        await EventuallyAsync(async () => (await BoxAsync(preview)).TopLeft.DistanceTo(snapped) <= 1, $"the preview is drawn on the grid at {snapped}");

        await DragToAsync(page, at);
        AssertNear(snapped, (await BoxAsync(box)).TopLeft, 1, "the box after the drop");
    }

    [TestMethod]
    public async Task Kept_to_the_horizontal_axis_the_box_keeps_its_height()
    {
        var page = await OpenAsync("/drag-around/custom-layer");
        await page.GetByRole(Microsoft.Playwright.AriaRole.Button, new() { Name = "Horizontal only" }).ClickAsync();
        var box = page.Locator(".drag-around-box", new() { HasText = "Drag me" });

        var before = await BoxAsync(box);
        var grab = before.Centre.Rounded();
        await StartDragAsync(page, grab);
        var start = (await DragStartsAsync(page.MainFrame)).Single();
        var at = grab.Offset(80, 90);
        await DragToAsync(page, at);

        AssertNear(new Point(before.X + at.X - start.X, before.Y), (await BoxAsync(box)).TopLeft, 1, "the box after the drop");
    }
}
