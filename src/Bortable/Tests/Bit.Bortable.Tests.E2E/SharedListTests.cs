using Bit.Bortable.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// Lists that share a group, with real drags between them: the placeholder that previews where an item will land
/// while its original is hidden at home, the move committed on drop, a drag taken back home, and a drag released
/// outside every list.
/// </summary>
/// <remarks>
/// An item dragged into another list is not moved until the drag ends: the target shows a placeholder (the item's
/// own template with the ghost classes) where it would land, and the source hides the original. Once the
/// placeholder is in a list it sorts like the item itself, so it comes to rest in the slot under the pointer: a drag
/// that ends on B3 previews (and drops) the item where B3 was, pushing B3 down.
/// </remarks>
[TestClass]
public sealed class SharedListTests : BortableTest
{
    private static readonly string[] A = ["A1", "A2", "A3", "A4", "A5"];
    private static readonly string[] B = ["B1", "B2", "B3", "B4", "B5"];

    [TestMethod]
    public async Task Dragging_from_A_into_B_previews_a_placeholder_and_moves_the_item_on_drop()
    {
        var page = await OpenAsync("/shared");
        await AssertOrderAsync(page, "shared-a", "shared-a-order", A);
        await AssertOrderAsync(page, "shared-b", "shared-b-order", B);

        await PressAsync(page, (await BoxAsync(Item(page, "shared-a", "A1"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "shared-b", "B3"))).At(0.5, 0.3));

        // Mid-drag: the placeholder previews A1 between B2 and B3, wearing the ghost classes, while A1 is still in
        // list A (the bound lists are untouched until the drop), hidden there.
        await AssertLabelsAsync(page, "shared-b", new[] { "B1", "B2", "[placeholder: A1]", "B3", "B4", "B5" }, withPlaceholder: true);
        await Expect(Placeholder(page, "shared-b")).ToHaveClassAsync(HasClass("demo-ghost"));
        await Expect(Item(page, "shared-a", "A1")).ToHaveClassAsync(HasClass("bit-bortable-hidden"));
        await Expect(Item(page, "shared-a", "A1")).ToBeHiddenAsync();
        await Expect(page.Locator("#shared-a-order")).ToHaveTextAsync(string.Join(", ", A));

        await ReleaseAsync(page);

        await AssertOrderAsync(page, "shared-a", "shared-a-order", "A2", "A3", "A4", "A5");
        await AssertOrderAsync(page, "shared-b", "shared-b-order", "B1", "B2", "A1", "B3", "B4", "B5");
        await Expect(page.Locator("[data-bortable-placeholder], .bit-bortable-hidden")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Dragging_from_B_into_A_lands_at_the_top_and_the_item_keeps_its_own_class()
    {
        var page = await OpenAsync("/shared");
        await Expect(Item(page, "shared-b", "B5")).ToHaveClassAsync(HasClass("tinted"));

        await DragAsync(page,
            (await BoxAsync(Item(page, "shared-b", "B5"))).Centre,
            (await BoxAsync(Item(page, "shared-a", "A1"))).At(0.5, 0.3));

        await AssertOrderAsync(page, "shared-a", "shared-a-order", "B5", "A1", "A2", "A3", "A4", "A5");
        await AssertOrderAsync(page, "shared-b", "shared-b-order", "B1", "B2", "B3", "B4");

        // ItemClassSelector styles the item, not the list: B5 is still tinted in list A, and the A items are not.
        await Expect(Item(page, "shared-a", "B5")).ToHaveClassAsync(HasClass("tinted"));
        await Expect(Item(page, "shared-a", "A1")).Not.ToHaveClassAsync(HasClass("tinted"));
    }

    [TestMethod]
    public async Task Taking_the_item_back_home_before_the_drop_cancels_the_move()
    {
        var page = await OpenAsync("/shared");
        var home = await BoxAsync(Item(page, "shared-a", "A2"));

        await PressAsync(page, home.Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "shared-b", "B2"))).Centre);
        await Expect(Placeholder(page, "shared-b")).ToHaveCountAsync(1);
        await Expect(Item(page, "shared-a", "A2")).ToHaveClassAsync(HasClass("bit-bortable-hidden"));

        // Back over A2's own slot (A3 has slid up into it meanwhile): the preview goes, and A2 is back in place.
        await MoveAsync(page, home.At(0.5, 0.3));
        await Expect(Placeholder(page, "shared-b")).ToHaveCountAsync(0);
        await Expect(Item(page, "shared-a", "A2")).Not.ToHaveClassAsync(HasClass("bit-bortable-hidden"));
        await Expect(Item(page, "shared-a", "A2")).ToHaveClassAsync(HasClass("demo-ghost"));
        await AssertLabelsAsync(page, "shared-a", A);

        await ReleaseAsync(page);

        await AssertOrderAsync(page, "shared-a", "shared-a-order", A);
        await AssertOrderAsync(page, "shared-b", "shared-b-order", B);
    }

    [TestMethod]
    public async Task Releasing_outside_every_list_after_a_preview_keeps_the_move()
    {
        var page = await OpenAsync("/shared");

        await PressAsync(page, (await BoxAsync(Item(page, "shared-a", "A4"))).Centre);
        var target = (await BoxAsync(Item(page, "shared-b", "B4"))).At(0.5, 0.3);
        await MoveAsync(page, target);
        await Expect(Placeholder(page, "shared-b")).ToHaveCountAsync(1);

        // Out to the right of list B, over nothing that takes a drop. No list sees the pointer there, so the
        // preview stays; releasing ends the drag with the browser's dragend, which commits it, as SortableJS does.
        var listB = await ListBoxAsync(page, "shared-b");
        await MoveAsync(page, new Point(listB.Right + 60, target.Y));
        await AssertLabelsAsync(page, "shared-b", new[] { "B1", "B2", "B3", "[placeholder: A4]", "B4", "B5" }, withPlaceholder: true);

        await ReleaseAsync(page);

        await AssertOrderAsync(page, "shared-a", "shared-a-order", "A1", "A2", "A3", "A5");
        await AssertOrderAsync(page, "shared-b", "shared-b-order", "B1", "B2", "B3", "A4", "B4", "B5");
    }

    [TestMethod]
    public async Task Items_go_back_and_forth_between_the_lists()
    {
        var page = await OpenAsync("/shared");

        await DragAsync(page,
            (await BoxAsync(Item(page, "shared-a", "A3"))).Centre,
            (await BoxAsync(Item(page, "shared-b", "B1"))).At(0.5, 0.3));
        await AssertOrderAsync(page, "shared-b", "shared-b-order", "A3", "B1", "B2", "B3", "B4", "B5");

        // And back again, now from list B, to the end of list A: the empty slot below A5 (the two lists stretch to
        // the same height, so list A has room below its last item).
        var a4 = await BoxAsync(Item(page, "shared-a", "A4"));
        var a5 = await BoxAsync(Item(page, "shared-a", "A5"));
        await DragAsync(page,
            (await BoxAsync(Item(page, "shared-b", "A3"))).Centre,
            a5.Centre.Offset(0, a5.Y - a4.Y));
        await AssertOrderAsync(page, "shared-a", "shared-a-order", "A1", "A2", "A4", "A5", "A3");
        await AssertOrderAsync(page, "shared-b", "shared-b-order", B);
    }
}
