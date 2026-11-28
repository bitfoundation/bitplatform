namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitSortable laid out in two dimensions and its swap zones, on /e2e/sortable/grid and /e2e/sortable/thresholds: a
/// CSS grid reorders across its rows in both directions; with a small SwapThreshold the edge of an item is a dead zone
/// while its middle swaps, and with the full threshold the same edge swaps; InvertSwap makes the middle of an item dead
/// and its far edge sort the dragged item past it.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableLayoutE2ETests : BitSortableE2ETestBase
{
    private static readonly string[] Simple = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];

    [TestMethod]
    public async Task Grid_ReordersAcrossRows_InBothDirections()
    {
        await OpenAsync("/e2e/sortable/grid");
        var initial = Enumerable.Range(1, 16).Select(n => n.ToString()).ToArray();
        await AssertOrderAsync("grid-list", "grid-order", initial);

        // The grid wraps: square 7 sits on the second row, down and to the right of square 1.
        var one = await BoxAsync(Item("grid-list", "1"));
        var seven = await BoxAsync(Item("grid-list", "7"));
        Assert.IsTrue(seven.Y > one.Bottom && seven.X > one.Right, $"square 7 {seven} is not below and right of square 1 {one}");

        await DragAsync(one.Centre, seven.Centre);
        await AssertOrderAsync("grid-list", "grid-order", "2", "3", "4", "5", "6", "7", "1", "8", "9", "10", "11", "12", "13", "14", "15", "16");

        // From the last row back to the first slot.
        await DragAsync(await CentreOfAsync("grid-list", "16"), await CentreOfAsync("grid-list", "2"));
        await AssertOrderAsync("grid-list", "grid-order", "16", "2", "3", "4", "5", "6", "7", "1", "8", "9", "10", "11", "12", "13", "14", "15");
    }

    [TestMethod]
    public async Task SwapThreshold_ASmallOne_MakesTheEdgeOfAnItemADeadZone()
    {
        await OpenAsync("/e2e/sortable/thresholds");
        await SetSwapThresholdAsync("#swap-threshold", "0.1", "#swap-threshold-value", "0.10");

        // Only the middle tenth of Item 2 swaps now. The pointer resting just inside its top edge does nothing.
        var item1 = await BoxAsync(Item("thresholds-list", "Item 1"));
        var item2 = await BoxAsync(Item("thresholds-list", "Item 2"));
        await PressAsync(item1.Centre);
        await MoveAsync(item2.At(0.5, 0.15));
        await AssertLabelsAsync("thresholds-list", Simple, because: "the pointer in the dead zone swapped the items");

        // Into the middle of Item 2, the swap zone: the items swap.
        await MoveAsync(item2.At(0.5, 0.5), steps: 5);
        await AssertLabelsAsync("thresholds-list", ["Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6"]);
        await ReleaseAsync();
        await AssertOrderAsync("thresholds-list", "thresholds-list-order", "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6");

        // With the full threshold, the very same edge is part of the swap zone: Item 3 passes Item 4 at once.
        await SetSwapThresholdAsync("#swap-threshold", "1", "#swap-threshold-value", "1.00");
        var item3 = await BoxAsync(Item("thresholds-list", "Item 3"));
        var item4 = await BoxAsync(Item("thresholds-list", "Item 4"));
        await DragAsync(item3.Centre, item4.At(0.5, 0.15));
        await AssertOrderAsync("thresholds-list", "thresholds-list-order", "Item 2", "Item 1", "Item 4", "Item 3", "Item 5", "Item 6");
    }

    [TestMethod]
    public async Task InvertSwap_MovesTheSwapZonesToTheEdgesOfAnItem()
    {
        await OpenAsync("/e2e/sortable/thresholds");
        await Expect(Page.Locator("#invert-swap")).Not.ToBeCheckedAsync();
        await Expect(Page.Locator("#inverted-swap-threshold-value")).ToHaveTextAsync("0.65");

        // Without InvertSwap (SwapThreshold 0.65) the middle of an item swaps.
        await DragAsync(await CentreOfAsync("thresholds-list", "Item 1"), await CentreOfAsync("thresholds-list", "Item 2"));
        await AssertOrderAsync("thresholds-list", "thresholds-list-order", "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6");

        // With it, the middle of an item is dead, and the item's far edge sorts the dragged one past it.
        await Page.Locator("#invert-swap").CheckAsync();
        await Expect(Page.Locator("#thresholds-list")).ToHaveClassAsync(HasClass("zones-inverted"));
        await WaitForQuietAsync();

        var item3 = await BoxAsync(Item("thresholds-list", "Item 3"));
        var item4 = await BoxAsync(Item("thresholds-list", "Item 4"));
        await PressAsync(item3.Centre);
        await MoveAsync(item4.At(0.5, 0.5));
        await AssertLabelsAsync("thresholds-list", ["Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6"], because: "with InvertSwap, the middle of an item swapped");

        await MoveAsync(item4.At(0.5, 0.88), steps: 5);
        await AssertLabelsAsync("thresholds-list", ["Item 2", "Item 1", "Item 4", "Item 3", "Item 5", "Item 6"], because: "with InvertSwap, the far edge of an item did not swap");
        await ReleaseAsync();
        await AssertOrderAsync("thresholds-list", "thresholds-list-order", "Item 2", "Item 1", "Item 4", "Item 3", "Item 5", "Item 6");
    }

    [TestMethod]
    public async Task Axe_EachPage_HasNoWcagAOrAAViolation()
    {
        await OpenAsync("/e2e/sortable/grid");
        await AssertNoAxeViolationsAsync("#grid");

        await OpenAsync("/e2e/sortable/thresholds");
        await AssertNoAxeViolationsAsync("#thresholds");
    }

    // Sets one of the threshold sliders and waits until the page has taken the new value.
    private async Task SetSwapThresholdAsync(string slider, string value, string readout, string shown)
    {
        await Page.Locator(slider).FillAsync(value);
        await Expect(Page.Locator(readout)).ToHaveTextAsync(shown);
        await WaitForQuietAsync();
    }
}
