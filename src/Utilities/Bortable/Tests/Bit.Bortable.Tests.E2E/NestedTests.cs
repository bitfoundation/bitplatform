using System.Text.RegularExpressions;
using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// The nested tree, where every item holds a list of its own and all of them share a group: items move into an
/// empty branch and between branches, and never into their own subtree, which the browser layer refuses while the
/// drag is still running.
/// </summary>
/// <remarks>
/// Every list of the tree sits inside the items of the lists around it, so a pointer travelling to a deep list
/// crosses the outer ones, and each of them previews the item where the pointer passes, shifting the rest of the
/// tree down under the pointer. Where the item lands would then depend on how many dragovers the pointer happened
/// to fire on the way. These tests move the pointer to the target in one jump, as a quick hand does: one dragover
/// on the way, at the target, which is what decides where the item lands. And they take items upwards: the original
/// of an item previewed elsewhere is hidden, which closes its gap and pulls everything below it up, under a pointer
/// aimed at a target below it.
/// </remarks>
[TestClass]
public sealed class NestedTests : BortableTest
{
    private const string Initial = "Item 1.1 [Item 2.1, Item 2.2 [Item 3.1, Item 3.2], Item 2.3], Item 1.2, Item 1.3 [Item 2.4, Item 2.5], Item 1.4";

    [TestMethod]
    public async Task An_item_dropped_into_an_empty_branch_becomes_its_child()
    {
        var page = await OpenAsync("/nested");
        await Expect(page.Locator("#nested-order")).ToHaveTextAsync(Initial);
        await Expect(page.Locator("#nested-item-2-1 > [data-bortable-item]")).ToHaveCountAsync(0);

        await PressAsync(page, (await BoxAsync(Name(page, "nested-root", "Item 1.2"))).Centre);
        await MoveAsync(page, (await ListBoxAsync(page, "nested-item-2-1")).Centre, steps: 1);
        await AssertLabelsAsync(page, "nested-item-2-1", new[] { "[placeholder: Item 1.2]" }, withPlaceholder: true);
        await ReleaseAsync(page);

        await Expect(page.Locator("#nested-order")).ToHaveTextAsync(
            "Item 1.1 [Item 2.1 [Item 1.2], Item 2.2 [Item 3.1, Item 3.2], Item 2.3], Item 1.3 [Item 2.4, Item 2.5], Item 1.4");
        await AssertLabelsAsync(page, "nested-item-2-1", new[] { "Item 1.2" });
        await AssertLabelsAsync(page, "nested-root", new[] { "Item 1.1", "Item 1.3", "Item 1.4" });
    }

    [TestMethod]
    public async Task An_item_moves_between_branches_and_takes_its_place_among_the_children()
    {
        var page = await OpenAsync("/nested");

        await PressAsync(page, (await BoxAsync(Name(page, "nested-item-1-3", "Item 2.4"))).Centre);
        await MoveAsync(page, (await BoxAsync(Name(page, "nested-item-1-1", "Item 2.3"))).Centre, steps: 1);
        await AssertLabelsAsync(page, "nested-item-1-1", new[] { "Item 2.1", "Item 2.2", "[placeholder: Item 2.4]", "Item 2.3" }, withPlaceholder: true);
        await ReleaseAsync(page);

        await Expect(page.Locator("#nested-order")).ToHaveTextAsync(
            "Item 1.1 [Item 2.1, Item 2.2 [Item 3.1, Item 3.2], Item 2.4, Item 2.3], Item 1.2, Item 1.3 [Item 2.5], Item 1.4");

        // A whole branch moves with its children: Item 1.3, from the top level, into the empty list of Item 2.1.
        await PressAsync(page, (await BoxAsync(Name(page, "nested-root", "Item 1.3"))).Centre);
        await MoveAsync(page, (await ListBoxAsync(page, "nested-item-2-1")).Centre, steps: 1);
        await ReleaseAsync(page);

        await Expect(page.Locator("#nested-order")).ToHaveTextAsync(
            "Item 1.1 [Item 2.1 [Item 1.3 [Item 2.5]], Item 2.2 [Item 3.1, Item 3.2], Item 2.4, Item 2.3], Item 1.2, Item 1.4");
        await AssertLabelsAsync(page, "nested-item-1-3", new[] { "Item 2.5" });
    }

    [TestMethod]
    public async Task An_item_is_never_dropped_into_its_own_subtree()
    {
        var page = await OpenAsync("/nested");

        // Item 1.1 by its name, down into the list of its grandchild Item 2.2, inside the dragged item itself.
        await PressAsync(page, (await BoxAsync(Name(page, "nested-root", "Item 1.1"))).Centre);
        var inner = await ListBoxAsync(page, "nested-item-2-2");
        await MoveAsync(page, new Point(inner.Centre.X, inner.Bottom - 6));
        await MoveAsync(page, inner.Centre);

        // The drag runs (Item 1.1 is the ghost), but no list inside it previews a drop, and the tree is unchanged.
        await Expect(page.Locator("#nested-root > .demo-ghost > .nested-item__name")).ToHaveTextAsync("Item 1.1");
        await Expect(page.Locator("[data-bortable-placeholder]")).ToHaveCountAsync(0);

        await ReleaseAsync(page);
        await Expect(page.Locator("#nested-order")).ToHaveTextAsync(Initial);
        await Expect(page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
    }

    /// <summary>
    /// The name of a nested item of the list <paramref name="listId"/>: the part of the item outside the list it holds,
    /// which is where an item of the tree is grabbed (a press on the list inside it would grab nothing, or a child).
    /// </summary>
    private static ILocator Name(IPage page, string listId, string name) =>
        page.Locator($"#{listId} > [data-bortable-item] > .nested-item__name")
            .Filter(new LocatorFilterOptions { HasTextRegex = new Regex(@"^\s*" + Regex.Escape(name) + @"\s*$") });
}
