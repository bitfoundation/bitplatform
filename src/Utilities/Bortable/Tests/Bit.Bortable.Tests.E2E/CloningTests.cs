using Bit.Bortable.Tests.E2E.Infrastructure;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// <c>Pull="BortablePull.Clone"</c> with real drags: the source keeps its items (visible, even mid-drag) and the
/// target gains a copy, as many times as the same item is dragged over.
/// </summary>
[TestClass]
public sealed class CloningTests : BortableTest
{
    private static readonly string[] Fruits = ["Apple", "Banana", "Cherry", "Date", "Elderberry"];

    [TestMethod]
    public async Task Dragging_from_the_clone_source_copies_the_item_and_leaves_the_source_unchanged()
    {
        var page = await OpenAsync("/cloning");
        await AssertOrderAsync(page, "clone-source", "clone-source-order", Fruits);

        await PressAsync(page, (await BoxAsync(Item(page, "clone-source", "Apple"))).Centre);
        await MoveAsync(page, (await BoxAsync(Item(page, "clone-target", "Item 2"))).At(0.5, 0.3));

        // Mid-drag: the copy is previewed in the target, and unlike a move the original stays visible at home.
        await AssertLabelsAsync(page, "clone-target", new[] { "Item 1", "[placeholder: Apple]", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6" }, withPlaceholder: true);
        await Expect(Item(page, "clone-source", "Apple")).ToBeVisibleAsync();
        await Expect(Item(page, "clone-source", "Apple")).Not.ToHaveClassAsync(HasClass("bit-bortable-hidden"));

        await ReleaseAsync(page);

        await AssertOrderAsync(page, "clone-source", "clone-source-order", Fruits);
        await AssertOrderAsync(page, "clone-target", "clone-target-order", "Item 1", "Apple", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6");
    }

    [TestMethod]
    public async Task The_same_item_can_be_cloned_again_and_again()
    {
        var page = await OpenAsync("/cloning");

        await DragAsync(page,
            (await BoxAsync(Item(page, "clone-source", "Cherry"))).Centre,
            (await BoxAsync(Item(page, "clone-target", "Item 1"))).At(0.5, 0.3));
        await AssertOrderAsync(page, "clone-target", "clone-target-order", "Cherry", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6");

        // The clone factory makes a distinct object every time, so a second copy is not a duplicate key.
        await DragAsync(page,
            (await BoxAsync(Item(page, "clone-source", "Cherry"))).Centre,
            (await BoxAsync(Item(page, "clone-target", "Item 4"))).At(0.5, 0.3));
        await AssertOrderAsync(page, "clone-target", "clone-target-order", "Cherry", "Item 1", "Item 2", "Item 3", "Cherry", "Item 4", "Item 5", "Item 6");
        await AssertOrderAsync(page, "clone-source", "clone-source-order", Fruits);
        await Expect(Item(page, "clone-target", "Cherry")).ToHaveCountAsync(2);
    }
}
