using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// Sorting within one list with real drags: the live reorder while the pointer moves, the order the bound list
/// ends with, grids, the FLIP animation, and the swap thresholds deciding when the dragged item passes another.
/// </summary>
/// <remarks>
/// The demo's items are all the same size, so wherever the pointer comes to rest the dragged item ends up in the
/// slot under it: a drop "on Item 4" lands the dragged item where Item 4 was, whichever way it came from.
/// </remarks>
[TestClass]
public sealed class SortingTests : BortableTest
{
    private static readonly string[] Simple = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];

    [TestMethod]
    public async Task Dragging_an_item_down_reorders_the_list_live_and_binds_the_order_on_drop()
    {
        var page = await OpenAsync("/simple-list");
        await AssertOrderAsync(page, "simple-list", "simple-order", Simple);

        var from = (await BoxAsync(Item(page, "simple-list", "Item 1"))).Centre;
        var to = (await BoxAsync(Item(page, "simple-list", "Item 4"))).Centre;
        await PressAsync(page, from);
        await MoveAsync(page, to);

        // Mid-drag: the list has been reordered under the pointer, and the dragged item wears the ghost and chosen
        // classes. The page's readout is not re-rendered until the list reports the change, when the drag ends.
        await AssertLabelsAsync(page, "simple-list", new[] { "Item 2", "Item 3", "Item 4", "Item 1", "Item 5", "Item 6" });
        var dragged = Item(page, "simple-list", "Item 1");
        await Expect(dragged).ToHaveClassAsync(HasClass("demo-ghost"));
        await Expect(dragged).ToHaveClassAsync(HasClass("bit-bortable-chosen"));
        await Expect(page.Locator("#simple-order")).ToHaveTextAsync(string.Join(", ", Simple));

        await ReleaseAsync(page);

        await AssertOrderAsync(page, "simple-list", "simple-order", "Item 2", "Item 3", "Item 4", "Item 1", "Item 5", "Item 6");
        await Expect(page.Locator("#simple-list .demo-ghost, #simple-list .bit-bortable-chosen")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Dragging_an_item_up_moves_it_above_the_item_it_is_dropped_on()
    {
        var page = await OpenAsync("/simple-list");

        await DragAsync(page,
            (await BoxAsync(Item(page, "simple-list", "Item 5"))).Centre,
            (await BoxAsync(Item(page, "simple-list", "Item 2"))).Centre);

        await AssertOrderAsync(page, "simple-list", "simple-order", "Item 1", "Item 5", "Item 2", "Item 3", "Item 4", "Item 6");
    }

    [TestMethod]
    public async Task Successive_drags_keep_sorting_the_already_sorted_list()
    {
        var page = await OpenAsync("/simple-list");

        await DragAsync(page,
            (await BoxAsync(Item(page, "simple-list", "Item 6"))).Centre,
            (await BoxAsync(Item(page, "simple-list", "Item 1"))).Centre);
        await AssertOrderAsync(page, "simple-list", "simple-order", "Item 6", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5");

        await DragAsync(page,
            (await BoxAsync(Item(page, "simple-list", "Item 1"))).Centre,
            (await BoxAsync(Item(page, "simple-list", "Item 5"))).Centre);
        await AssertOrderAsync(page, "simple-list", "simple-order", "Item 6", "Item 2", "Item 3", "Item 4", "Item 5", "Item 1");
    }

    [TestMethod]
    public async Task A_grid_reorders_across_rows_in_both_directions()
    {
        var page = await OpenAsync("/grid");
        var initial = Enumerable.Range(1, 16).Select(n => n.ToString()).ToArray();
        await AssertOrderAsync(page, "grid-list", "grid-order", initial);

        // The grid wraps: square 7 sits on the second row, down and to the right of square 1.
        var one = await BoxAsync(Item(page, "grid-list", "1"));
        var seven = await BoxAsync(Item(page, "grid-list", "7"));
        Assert.IsTrue(seven.Y > one.Bottom && seven.X > one.Right, $"square 7 {seven} is not below and right of square 1 {one}");

        await DragAsync(page, one.Centre, seven.Centre);
        await AssertOrderAsync(page, "grid-list", "grid-order", "2", "3", "4", "5", "6", "7", "1", "8", "9", "10", "11", "12", "13", "14", "15", "16");

        // From the last row back to the first slot.
        await DragAsync(page,
            (await BoxAsync(Item(page, "grid-list", "16"))).Centre,
            (await BoxAsync(Item(page, "grid-list", "2"))).Centre);
        await AssertOrderAsync(page, "grid-list", "grid-order", "16", "2", "3", "4", "5", "6", "7", "1", "8", "9", "10", "11", "12", "13", "14", "15");
    }

    [TestMethod]
    public async Task The_FLIP_animation_slides_items_without_disturbing_the_final_order()
    {
        var page = await OpenAsync("/simple-list");

        // Counts the slides the browser runs: a FLIP inverts a moved item with a transform and transitions it back
        // to none, all within one task, so the transition itself is the trace it leaves.
        await page.EvaluateAsync(
            """
            () => {
              window.__flips = 0;
              document.getElementById('simple-list').addEventListener('transitionrun', e => {
                if (e.propertyName === 'transform') window.__flips++;
              });
            }
            """);

        // Down past every item and straight back up to the third slot, without waiting in between: every reorder
        // starts while the slides of the previous ones are still running, and has to take over from them.
        var slots = new List<Box>();
        foreach (var label in Simple) slots.Add(await BoxAsync(Item(page, "simple-list", label)));
        await PressAsync(page, slots[0].Centre);
        await page.Mouse.MoveAsync((float)slots[5].Centre.X, (float)slots[5].Centre.Y, new MouseMoveOptions { Steps = 15 });
        await MoveAsync(page, slots[2].Centre, steps: 10);
        await ReleaseAsync(page);

        await AssertOrderAsync(page, "simple-list", "simple-order", "Item 2", "Item 3", "Item 1", "Item 4", "Item 5", "Item 6");
        Assert.IsTrue(await page.EvaluateAsync<int>("() => window.__flips") > 0, "no item was animated into its new place");

        // Once the slides are over, no item is left offset or mid-transition, and the items are painted top to
        // bottom in the order the list holds them, each in its original slot.
        var leftovers = await page.EvaluateAsync<string[]>(
            "() => [...document.querySelectorAll('#simple-list > [data-bortable-item]')].filter(i => i.style.transform || i.style.transition).map(i => i.textContent.trim())");
        Assert.IsEmpty(leftovers, $"items still carry an animation style: {string.Join(", ", leftovers)}");

        string[] expected = ["Item 2", "Item 3", "Item 1", "Item 4", "Item 5", "Item 6"];
        for (int i = 0; i < expected.Length; i++)
        {
            var box = await BoxAsync(Item(page, "simple-list", expected[i]));
            Assert.AreEqual(slots[i].Y, box.Y, 0.5, $"{expected[i]} is not painted in slot {i}");
        }
    }

    [TestMethod]
    public async Task With_a_small_swap_threshold_the_edge_of_an_item_is_a_dead_zone()
    {
        var page = await OpenAsync("/thresholds");
        await SetSwapThresholdAsync(page, "#swap-threshold", "0.1", "#swap-threshold-value", "0.10");

        // Only the middle tenth of Item 2 swaps now. The pointer resting just inside its top edge does nothing.
        var item1 = await BoxAsync(Item(page, "thresholds-list", "Item 1"));
        var item2 = await BoxAsync(Item(page, "thresholds-list", "Item 2"));
        await PressAsync(page, item1.Centre);
        await MoveAsync(page, item2.At(0.5, 0.15));
        await AssertLabelsAsync(page, "thresholds-list", Simple, because: "the pointer in the dead zone swapped the items");

        // Into the middle of Item 2, the swap zone: the items swap.
        await MoveAsync(page, item2.At(0.5, 0.5), steps: 5);
        await AssertLabelsAsync(page, "thresholds-list", new[] { "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6" });
        await ReleaseAsync(page);
        await AssertOrderAsync(page, "thresholds-list", "thresholds-list-order", "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6");

        // With the full threshold, the very same edge is part of the swap zone: Item 3 passes Item 4 at once.
        await SetSwapThresholdAsync(page, "#swap-threshold", "1", "#swap-threshold-value", "1.00");
        var item3 = await BoxAsync(Item(page, "thresholds-list", "Item 3"));
        var item4 = await BoxAsync(Item(page, "thresholds-list", "Item 4"));
        await DragAsync(page, item3.Centre, item4.At(0.5, 0.15));
        await AssertOrderAsync(page, "thresholds-list", "thresholds-list-order", "Item 2", "Item 1", "Item 4", "Item 3", "Item 5", "Item 6");
    }

    [TestMethod]
    public async Task Invert_swap_moves_the_swap_zones_to_the_edges_of_an_item()
    {
        var page = await OpenAsync("/thresholds");
        await Expect(page.Locator("#invert-swap")).Not.ToBeCheckedAsync();
        await Expect(page.Locator("#inverted-swap-threshold-value")).ToHaveTextAsync("0.65");

        // Without InvertSwap (SwapThreshold 0.65) the middle of an item swaps.
        await DragAsync(page,
            (await BoxAsync(Item(page, "thresholds-list", "Item 1"))).Centre,
            (await BoxAsync(Item(page, "thresholds-list", "Item 2"))).Centre);
        await AssertOrderAsync(page, "thresholds-list", "thresholds-list-order", "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6");

        // With it, the middle of an item is dead, and the item's far edge sorts the dragged one past it.
        await page.Locator("#invert-swap").CheckAsync();
        await Expect(page.Locator("#thresholds-list")).ToHaveClassAsync(HasClass("zones-inverted"));

        var item3 = await BoxAsync(Item(page, "thresholds-list", "Item 3"));
        var item4 = await BoxAsync(Item(page, "thresholds-list", "Item 4"));
        await PressAsync(page, item3.Centre);
        await MoveAsync(page, item4.At(0.5, 0.5));
        await AssertLabelsAsync(page, "thresholds-list", new[] { "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6" }, because: "with InvertSwap, the middle of an item swapped");

        await MoveAsync(page, item4.At(0.5, 0.88), steps: 5);
        await AssertLabelsAsync(page, "thresholds-list", new[] { "Item 2", "Item 1", "Item 4", "Item 3", "Item 5", "Item 6" }, because: "with InvertSwap, the far edge of an item did not swap");
        await ReleaseAsync(page);
        await AssertOrderAsync(page, "thresholds-list", "thresholds-list-order", "Item 2", "Item 1", "Item 4", "Item 3", "Item 5", "Item 6");
    }

    /// <summary>Sets one of the threshold sliders and waits until the page has taken the new value.</summary>
    private static async Task SetSwapThresholdAsync(IPage page, string slider, string value, string readout, string shown)
    {
        await page.Locator(slider).FillAsync(value);
        await Expect(page.Locator(readout)).ToHaveTextAsync(shown);
        await WaitForQuietAsync(page);
    }
}
