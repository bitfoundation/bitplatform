using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Nesting: a dragstart bubbles through every draggable around the one under the pointer, and only the innermost
/// may claim it; a dragenter bubbles through every drop target around the element under the pointer, and each of
/// them has to know whether the pointer is over it at all (<c>IsOver</c>) and whether over it rather than over a
/// target nested in it (<c>IsOverShallow</c>).
/// </summary>
[TestClass]
public sealed class NestingTests : BdndTest
{
    private const string Shallow = "rgb(255, 235, 59)";
    private const string AlsoOver = "rgba(255, 235, 59, 0.25)";

    [TestMethod]
    [DataRow("INNER")]
    [DataRow("MIDDLE")]
    [DataRow("OUTER")]
    public async Task Grabbing_a_nested_source_drags_that_source_and_not_the_ones_around_it(string grabbed)
    {
        var page = await OpenAsync("/nesting/drag-sources");
        var nest = page.Locator(".nest-box");
        var (outer, middle, inner) = (nest.Nth(0), nest.Nth(1), nest.Nth(2));
        var (o, m, i) = (await BoxAsync(outer), await BoxAsync(middle), await BoxAsync(inner));

        // The inner box by its centre; the middle and the outer ones by their labels, above the box nested in them.
        var (grab, dragged) = grabbed switch
        {
            "INNER" => (i.Centre, inner),
            "MIDDLE" => (new Point(m.Centre.X, (m.Y + i.Y) / 2), middle),
            _ => (new Point(o.Centre.X, (o.Y + m.Y) / 2), outer),
        };

        await StartDragAsync(page, grab);

        // Only the grabbed source renders as dragging.
        foreach (var box in new[] { outer, middle, inner })
        {
            await Expect(box).ToHaveCSSAsync("opacity", box == dragged ? "0.4" : "1");
        }

        var bin = page.Locator(".dustbin");
        await DragToAsync(page, (await BoxAsync(bin)).Centre, () => Expect(bin).ToHaveCSSAsync("background-color", "rgb(0, 100, 0)"));

        await Expect(bin).ToHaveTextAsync($"You dragged: {grabbed}");
        await Expect(dragged).ToHaveCSSAsync("opacity", "1");

        // One dragstart, claimed by one source: exactly one library tag on it.
        var starts = await DragStartsAsync(page.MainFrame);
        Assert.HasCount(1, starts, "one gesture raised more than one dragstart");
        Assert.HasCount(1, starts[0].Types.Where(t => t.StartsWith("application/x-bit-bdnd-", StringComparison.Ordinal)),
            "the dragstart was tagged by more than one source: " + string.Join(", ", starts[0].Types));
    }

    [TestMethod]
    public async Task Hovering_a_nested_target_marks_it_as_the_shallow_one_and_the_targets_around_it_as_also_over()
    {
        var page = await OpenAsync("/nesting/drop-targets");
        var levels = Enumerable.Range(0, 4).Select(n => page.Locator($".nest-box[data-level=\"{n}\"]")).ToArray();
        var boxes = new Box[4];
        for (var n = 0; n < 4; n++) boxes[n] = await BoxAsync(levels[n]);

        await StartDragAsync(page, page.Locator(".box", new() { HasText = "Drag me into the nest" }));

        // Over the innermost level: it is the shallow one, every level around it is "also over".
        await MoveAsync(page, boxes[3].Centre);
        await ExpectBackgroundsAsync(levels, AlsoOver, AlsoOver, AlsoOver, Shallow);

        // Back out to level 1's own label, above level 2: the levels inside it are not hovered any more.
        await MoveAsync(page, new Point(boxes[1].Centre.X, (boxes[1].Y + boxes[2].Y) / 2));
        await ExpectBackgroundsAsync(levels, AlsoOver, Shallow, Resting(2), Resting(3));

        // Out of the nest altogether, and released there: nothing is hovered, and the drag ended.
        await DragToAsync(page, new Point(boxes[0].Centre.X, boxes[0].Bottom + 40),
            () => ExpectBackgroundsAsync(levels, Resting(0), Resting(1), Resting(2), Resting(3)));
        await Expect(page.Locator(".box", new() { HasText = "Drag me into the nest" })).ToHaveCSSAsync("opacity", "1");
    }

    /// <summary>A level's own colour, darker the deeper it is nested.</summary>
    private static string Resting(int level)
    {
        var shade = 200 - level * 45;
        return $"rgb({shade}, {shade + 20}, {shade + 40})";
    }

    private static async Task ExpectBackgroundsAsync(ILocator[] levels, params string[] colours)
    {
        for (var n = 0; n < levels.Length; n++)
        {
            await Expect(levels[n]).ToHaveCSSAsync("background-color", colours[n]);
        }
    }
}
