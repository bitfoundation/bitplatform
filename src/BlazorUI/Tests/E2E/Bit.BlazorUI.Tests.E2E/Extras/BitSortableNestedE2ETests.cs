using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// The nested tree of /e2e/sortable/nested, where every item holds a BitSortable of its own and all of them share a
/// group: items move into an empty branch and between branches, and never into their own subtree, which the script
/// refuses while the drag is still running.
/// </summary>
/// <remarks>
/// Every list of the tree sits inside the items of the lists around it, so a pointer travelling to a deep list crosses
/// the outer ones, and each of them previews the item where the pointer passes, shifting the rest of the tree down under
/// the pointer. These tests move the pointer to the target in one jump, as a quick hand does: one dragover on the way,
/// at the target, which is what decides where the item lands. And they take items upwards: the original of an item
/// previewed elsewhere is hidden, which closes its gap and pulls everything below it up, under a pointer aimed below it.
/// </remarks>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableNestedE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/nested";
    private const string Initial = "Item 1.1 [Item 2.1, Item 2.2 [Item 3.1, Item 3.2], Item 2.3], Item 1.2, Item 1.3 [Item 2.4, Item 2.5], Item 1.4";

    [TestMethod]
    public async Task Nested_AnItemDroppedIntoAnEmptyBranch_BecomesItsChild()
    {
        await OpenAsync(Url);
        await Expect(Page.Locator("#nested-order")).ToHaveTextAsync(Initial);
        await Expect(Page.Locator("#nested-item-2-1 > [data-bit-srt-item]")).ToHaveCountAsync(0);

        await PressAsync((await BoxAsync(Name("nested-root", "Item 1.2"))).Centre);
        await MoveAsync((await ListBoxAsync("nested-item-2-1")).Centre, steps: 1);
        await AssertLabelsAsync("nested-item-2-1", ["[placeholder: Item 1.2]"], withPlaceholder: true);
        await ReleaseAsync();

        await Expect(Page.Locator("#nested-order")).ToHaveTextAsync(
            "Item 1.1 [Item 2.1 [Item 1.2], Item 2.2 [Item 3.1, Item 3.2], Item 2.3], Item 1.3 [Item 2.4, Item 2.5], Item 1.4");
        await AssertLabelsAsync("nested-item-2-1", ["Item 1.2"]);
        await AssertLabelsAsync("nested-root", ["Item 1.1", "Item 1.3", "Item 1.4"]);
    }

    [TestMethod]
    public async Task Nested_AnItemMovesBetweenBranches_AndTakesItsPlaceAmongTheChildren()
    {
        await OpenAsync(Url);

        await PressAsync((await BoxAsync(Name("nested-item-1-3", "Item 2.4"))).Centre);
        await MoveAsync((await BoxAsync(Name("nested-item-1-1", "Item 2.3"))).Centre, steps: 1);
        await AssertLabelsAsync("nested-item-1-1", ["Item 2.1", "Item 2.2", "[placeholder: Item 2.4]", "Item 2.3"], withPlaceholder: true);
        await ReleaseAsync();

        await Expect(Page.Locator("#nested-order")).ToHaveTextAsync(
            "Item 1.1 [Item 2.1, Item 2.2 [Item 3.1, Item 3.2], Item 2.4, Item 2.3], Item 1.2, Item 1.3 [Item 2.5], Item 1.4");

        // A whole branch moves with its children: Item 1.3, from the top level, into the empty list of Item 2.1.
        await PressAsync((await BoxAsync(Name("nested-root", "Item 1.3"))).Centre);
        await MoveAsync((await ListBoxAsync("nested-item-2-1")).Centre, steps: 1);
        await ReleaseAsync();

        await Expect(Page.Locator("#nested-order")).ToHaveTextAsync(
            "Item 1.1 [Item 2.1 [Item 1.3 [Item 2.5]], Item 2.2 [Item 3.1, Item 3.2], Item 2.4, Item 2.3], Item 1.2, Item 1.4");
        await AssertLabelsAsync("nested-item-1-3", ["Item 2.5"]);
    }

    [TestMethod]
    public async Task Nested_AnItem_IsNeverDroppedIntoItsOwnSubtree()
    {
        await OpenAsync(Url);

        // Item 1.1 by its name, down into the list of its grandchild Item 2.2, inside the dragged item itself.
        await PressAsync((await BoxAsync(Name("nested-root", "Item 1.1"))).Centre);
        var inner = await ListBoxAsync("nested-item-2-2");
        await MoveAsync(new Point(inner.Centre.X, inner.Bottom - 6));
        await MoveAsync(inner.Centre);

        // The drag runs (Item 1.1 is the ghost), but no list inside it previews a drop, and the tree is unchanged.
        await Expect(Page.Locator("#nested-root > .bit-srt-gst > .nested-item__name")).ToHaveTextAsync("Item 1.1");
        await Expect(Page.Locator("[data-bit-srt-placeholder]")).ToHaveCountAsync(0);

        await ReleaseAsync();
        await Expect(Page.Locator("#nested-order")).ToHaveTextAsync(Initial);
        await Expect(Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task Axe_TheTree_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#nested");
    }

    // The name of a nested item of the list listId: the part of the item outside the list it holds, which is where an
    // item of the tree is grabbed (a press on the list inside it would grab nothing, or a child).
    private ILocator Name(string listId, string name) =>
        Page.Locator($"#{listId} > [data-bit-srt-item] > .nested-item__name")
            .Filter(new() { HasTextRegex = new Regex(@"^\s*" + Regex.Escape(name) + @"\s*$") });
}
