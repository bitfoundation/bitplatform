namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitSortable's Swap mode on /e2e/sortable/swap, with real drags: the item under the pointer is highlighted (and only
/// that one, following the pointer) while nothing moves, the drop trades the two items' places within a list and across
/// lists and names the item traded with (SwappedWith), and a drag released with no target highlighted changes nothing.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableSwapE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/swap";
    private const string SwapTarget = "bit-srt-swp";
    private static readonly string[] Six = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];

    [TestMethod]
    public async Task Swap_HighlightsTheItemUnderThePointer_AndTradesPlacesWithItOnDrop()
    {
        await OpenAsync(Url);

        await PressAsync(await CentreOfAsync("swap-list", "Item 1"));
        await MoveAsync(await CentreOfAsync("swap-list", "Item 4"));

        // Mid-drag: only Item 4 is highlighted, and nothing has moved, not even the items the pointer passed.
        await Expect(Page.Locator($".{SwapTarget}")).ToHaveTextAsync(["Item 4"]);
        await Expect(Item("swap-list", "Item 4")).ToHaveClassAsync(HasClass(SwapTarget));
        await AssertLabelsAsync("swap-list", Six);

        // On to Item 5: the highlight follows the pointer.
        await MoveAsync(await CentreOfAsync("swap-list", "Item 5"), steps: 4);
        await Expect(Page.Locator($".{SwapTarget}")).ToHaveTextAsync(["Item 5"]);

        await ReleaseAsync();

        await AssertOrderAsync("swap-list", "swap-order", "Item 5", "Item 2", "Item 3", "Item 4", "Item 1", "Item 6");
        await Expect(Page.Locator($".{SwapTarget}")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#swap-last")).ToHaveTextAsync("Item 1 ⇄ Item 5");
    }

    [TestMethod]
    public async Task Swap_TradesPlacesAcrossLists()
    {
        await OpenAsync(Url);

        await PressAsync(await CentreOfAsync("swap-left", "Item 2"));
        await MoveAsync(await CentreOfAsync("swap-right", "Item C"));
        await Expect(Item("swap-right", "Item C")).ToHaveClassAsync(HasClass(SwapTarget));
        await Expect(Page.Locator($".{SwapTarget}")).ToHaveCountAsync(1);
        await Expect(Placeholder("swap-right")).ToHaveCountAsync(0);
        await ReleaseAsync();

        await AssertOrderAsync("swap-left", "swap-left-order", "Item 1", "Item C", "Item 3", "Item 4");
        await AssertOrderAsync("swap-right", "swap-right-order", "Item A", "Item B", "Item 2", "Item D");

        // The single list above is a group of its own: it is left alone.
        await AssertOrderAsync("swap-list", "swap-order", Six);
    }

    [TestMethod]
    public async Task Swap_ADragReleasedOverNoItem_ChangesNothing()
    {
        await OpenAsync(Url);

        await PressAsync(await CentreOfAsync("swap-list", "Item 2"));
        await MoveAsync(await CentreOfAsync("swap-list", "Item 3"));
        await Expect(Item("swap-list", "Item 3")).ToHaveClassAsync(HasClass(SwapTarget));

        // Back over the dragged item itself: no target is left highlighted, so the drop swaps nothing.
        await MoveAsync(await CentreOfAsync("swap-list", "Item 2"));
        await Expect(Page.Locator($".{SwapTarget}")).ToHaveCountAsync(0);
        await ReleaseAsync();

        await AssertOrderAsync("swap-list", "swap-order", Six);
        await Expect(Page.Locator("#swap-last")).ToHaveTextAsync("nothing yet");
    }

    [TestMethod]
    public async Task Axe_EachSection_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#swap");
        await AssertNoAxeViolationsAsync("#swap-shared");
    }
}
