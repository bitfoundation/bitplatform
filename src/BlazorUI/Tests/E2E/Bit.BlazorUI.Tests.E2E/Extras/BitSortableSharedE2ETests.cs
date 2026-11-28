namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitSortable lists exchanging items, with real drags, on /e2e/sortable/shared and /e2e/sortable/clone: the placeholder
/// that previews where an item will land while its original is hidden at home, the move committed on drop, a drag taken
/// back home, a drag released outside every list, the class ItemClass gives an item wherever it is, PutGroups; a clone
/// source (Pull="Clone") that keeps its items; a list that neither sorts nor takes items (Sort="false", Put="false").
/// </summary>
/// <remarks>
/// An item dragged into another list is not moved until the drag ends: the target shows a placeholder (the item's own
/// template with the ghost classes) where it would land, and the source hides the original. Once the placeholder is in
/// a list it sorts like the item itself, so it comes to rest in the slot under the pointer.
/// </remarks>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableSharedE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/shared";
    private static readonly string[] A = ["A1", "A2", "A3", "A4", "A5"];
    private static readonly string[] B = ["B1", "B2", "B3", "B4", "B5"];

    [TestMethod]
    public async Task Shared_DraggingIntoTheOtherList_PreviewsAPlaceholderAndMovesTheItemOnDrop()
    {
        await OpenAsync(Url);
        await AssertOrderAsync("shared-a", "shared-a-order", A);
        await AssertOrderAsync("shared-b", "shared-b-order", B);

        await PressAsync(await CentreOfAsync("shared-a", "A1"));
        await MoveAsync((await BoxAsync(Item("shared-b", "B3"))).At(0.5, 0.3));

        // Mid-drag: the placeholder previews A1 between B2 and B3, wearing the ghost classes, while A1 is still in list A
        // (the bound lists are untouched until the drop), hidden there.
        await AssertLabelsAsync("shared-b", ["B1", "B2", "[placeholder: A1]", "B3", "B4", "B5"], withPlaceholder: true);
        await Expect(Placeholder("shared-b")).ToHaveClassAsync(HasClass("bit-srt-gst"));
        await Expect(Placeholder("shared-b")).ToHaveClassAsync(HasClass("demo-ghost"));
        await Expect(Placeholder("shared-b")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Item("shared-a", "A1")).ToHaveClassAsync(HasClass("bit-srt-hid"));
        await Expect(Item("shared-a", "A1")).ToBeHiddenAsync();
        await Expect(Page.Locator("#shared-a-order")).ToHaveTextAsync(string.Join(", ", A));

        await ReleaseAsync();

        await AssertOrderAsync("shared-a", "shared-a-order", "A2", "A3", "A4", "A5");
        await AssertOrderAsync("shared-b", "shared-b-order", "B1", "B2", "A1", "B3", "B4", "B5");
        await Expect(Page.Locator("[data-bit-srt-placeholder], .bit-srt-hid")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Shared_AnItemMovedToTheTop_KeepsTheClassItemClassGivesIt()
    {
        await OpenAsync(Url);
        await Expect(Item("shared-b", "B5")).ToHaveClassAsync(HasClass("tinted"));

        await DragAsync(await CentreOfAsync("shared-b", "B5"), (await BoxAsync(Item("shared-a", "A1"))).At(0.5, 0.3));

        await AssertOrderAsync("shared-a", "shared-a-order", "B5", "A1", "A2", "A3", "A4", "A5");
        await AssertOrderAsync("shared-b", "shared-b-order", "B1", "B2", "B3", "B4");

        // ItemClass styles the item, not the list: B5 is still tinted in list A, and the A items are not.
        await Expect(Item("shared-a", "B5")).ToHaveClassAsync(HasClass("tinted"));
        await Expect(Item("shared-a", "A1")).Not.ToHaveClassAsync(HasClass("tinted"));
    }

    [TestMethod]
    public async Task Shared_TakingTheItemBackHomeBeforeTheDrop_CancelsTheMove()
    {
        await OpenAsync(Url);
        var home = await BoxAsync(Item("shared-a", "A2"));

        await PressAsync(home.Centre);
        await MoveAsync(await CentreOfAsync("shared-b", "B2"));
        await Expect(Placeholder("shared-b")).ToHaveCountAsync(1);
        await Expect(Item("shared-a", "A2")).ToHaveClassAsync(HasClass("bit-srt-hid"));

        // Back over A2's own slot (A3 has slid up into it meanwhile): the preview goes, and A2 is back in place.
        await MoveAsync(home.At(0.5, 0.3));
        await Expect(Placeholder("shared-b")).ToHaveCountAsync(0);
        await Expect(Item("shared-a", "A2")).Not.ToHaveClassAsync(HasClass("bit-srt-hid"));
        await Expect(Item("shared-a", "A2")).ToHaveClassAsync(HasClass("bit-srt-gst"));
        await AssertLabelsAsync("shared-a", A);

        await ReleaseAsync();

        await AssertOrderAsync("shared-a", "shared-a-order", A);
        await AssertOrderAsync("shared-b", "shared-b-order", B);
    }

    [TestMethod]
    public async Task Shared_ReleasingOutsideEveryListAfterAPreview_KeepsTheMove()
    {
        await OpenAsync(Url);

        await PressAsync(await CentreOfAsync("shared-a", "A4"));
        var target = (await BoxAsync(Item("shared-b", "B4"))).At(0.5, 0.3);
        await MoveAsync(target);
        await Expect(Placeholder("shared-b")).ToHaveCountAsync(1);

        // Out to the right of list B, over nothing that takes a drop. No list sees the pointer there, so the preview
        // stays; releasing ends the drag with the browser's dragend, which commits it, as SortableJS does.
        var listB = await ListBoxAsync("shared-b");
        await MoveAsync(new Point(listB.Right + 60, target.Y));
        await AssertLabelsAsync("shared-b", ["B1", "B2", "B3", "[placeholder: A4]", "B4", "B5"], withPlaceholder: true);

        await ReleaseAsync();

        await AssertOrderAsync("shared-a", "shared-a-order", "A1", "A2", "A3", "A5");
        await AssertOrderAsync("shared-b", "shared-b-order", "B1", "B2", "B3", "A4", "B4", "B5");
    }

    [TestMethod]
    public async Task Shared_Items_GoBackAndForthBetweenTheLists()
    {
        await OpenAsync(Url);

        await DragAsync(await CentreOfAsync("shared-a", "A3"), (await BoxAsync(Item("shared-b", "B1"))).At(0.5, 0.3));
        await AssertOrderAsync("shared-b", "shared-b-order", "A3", "B1", "B2", "B3", "B4", "B5");

        // And back again, now from list B, to the end of list A: the empty slot below A5 (the two lists stretch to the
        // same height, so list A has room below its last item).
        var a4 = await BoxAsync(Item("shared-a", "A4"));
        var a5 = await BoxAsync(Item("shared-a", "A5"));
        await DragAsync(await CentreOfAsync("shared-b", "A3"), a5.Centre.Offset(0, a5.Y - a4.Y));
        await AssertOrderAsync("shared-a", "shared-a-order", "A1", "A2", "A4", "A5", "A3");
        await AssertOrderAsync("shared-b", "shared-b-order", B);
    }

    [TestMethod]
    public async Task PutGroups_TheTrash_TakesTasksAndBugsWhichNeverMix()
    {
        await OpenAsync(Url);
        await Expect(Page.Locator("#trash-bin > .bit-srt-emp")).ToHaveTextAsync("Drop a task or a bug here.");

        // A task released over Bugs: Bugs is another group and does not take it.
        await DragAsync(await CentreOfAsync("trash-tasks", "Write the docs"), await CentreOfAsync("trash-bugs", "Crash on save"));
        await Expect(Placeholder("trash-bugs")).ToHaveCountAsync(0);
        await AssertLabelsAsync("trash-bugs", ["Crash on save", "Typo in the footer"]);

        await DragAsync(await CentreOfAsync("trash-tasks", "Write the docs"), (await ListBoxAsync("trash-bin")).Centre);
        await AssertOrderAsync("trash-bin", "trash-order", "Write the docs");
        await Expect(Page.Locator("#trash-last")).ToHaveTextAsync("Write the docs, from the tasks group");
        await Expect(Page.Locator("#trash-bin > .bit-srt-emp")).ToHaveCountAsync(0);

        await DragAsync(await CentreOfAsync("trash-bugs", "Crash on save"), (await BoxAsync(Item("trash-bin", "Write the docs"))).At(0.5, 0.3));
        await AssertOrderAsync("trash-bin", "trash-order", "Crash on save", "Write the docs");
        await Expect(Page.Locator("#trash-last")).ToHaveTextAsync("Crash on save, from the bugs group");
        await AssertLabelsAsync("trash-tasks", ["Plan the release", "Tidy the backlog"]);
        await AssertLabelsAsync("trash-bugs", ["Typo in the footer"]);
    }

    [TestMethod]
    public async Task Clone_DraggingFromTheSource_CopiesTheItemAndLeavesTheSourceUnchanged()
    {
        string[] fruits = ["Apple", "Banana", "Cherry", "Date", "Elderberry"];
        await OpenAsync("/e2e/sortable/clone");
        await AssertOrderAsync("clone-source", "clone-source-order", fruits);

        await PressAsync(await CentreOfAsync("clone-source", "Apple"));
        await MoveAsync((await BoxAsync(Item("clone-target", "Item 2"))).At(0.5, 0.3));

        // Mid-drag: the copy is previewed in the target, and unlike a move the original stays visible at home.
        await AssertLabelsAsync("clone-target", ["Item 1", "[placeholder: Apple]", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"], withPlaceholder: true);
        await Expect(Item("clone-source", "Apple")).ToBeVisibleAsync();
        await Expect(Item("clone-source", "Apple")).Not.ToHaveClassAsync(HasClass("bit-srt-hid"));

        await ReleaseAsync();

        await AssertOrderAsync("clone-source", "clone-source-order", fruits);
        await AssertOrderAsync("clone-target", "clone-target-order", "Item 1", "Apple", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6");
    }

    [TestMethod]
    public async Task Clone_TheSameItem_CanBeClonedAgainAndAgain()
    {
        await OpenAsync("/e2e/sortable/clone");

        // The source sorts like any list, so each drag leaves it straight across, along Cherry's own row, before it
        // heads for its slot in the target: a diagonal over Banana could sort Cherry above it on the way out.
        await DragAcrossAsync(await CentreOfAsync("clone-source", "Cherry"), (await BoxAsync(Item("clone-target", "Item 1"))).At(0.5, 0.3));
        await AssertOrderAsync("clone-target", "clone-target-order", "Cherry", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6");

        // The clone factory makes a distinct object every time, so a second copy is not a duplicate key.
        await DragAcrossAsync(await CentreOfAsync("clone-source", "Cherry"), (await BoxAsync(Item("clone-target", "Item 4"))).At(0.5, 0.3));
        await AssertOrderAsync("clone-target", "clone-target-order", "Cherry", "Item 1", "Item 2", "Item 3", "Cherry", "Item 4", "Item 5", "Item 6");
        await AssertOrderAsync("clone-source", "clone-source-order", "Apple", "Banana", "Cherry", "Date", "Elderberry");
        await Expect(Item("clone-target", "Cherry")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task Sort_AListWithoutSorting_KeepsItsOrderWhileAnItemIsDraggedOverIt()
    {
        string[] source = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"];
        await OpenAsync("/e2e/sortable/clone");

        await PressAsync(await CentreOfAsync("disable-sort-source", "Item 1"));
        await MoveAsync(await CentreOfAsync("disable-sort-source", "Item 4"));

        // The drag is running (the item wears the ghost), but the list does not reorder under it.
        await Expect(Item("disable-sort-source", "Item 1")).ToHaveClassAsync(HasClass("bit-srt-gst"));
        await AssertLabelsAsync("disable-sort-source", source);

        await ReleaseAsync();
        await AssertOrderAsync("disable-sort-source", "disable-sort-source-order", source);
    }

    [TestMethod]
    public async Task Sort_AnItemOfAListWithoutSorting_CanStillBeDraggedOut()
    {
        await OpenAsync("/e2e/sortable/clone");

        await DragAsync(await CentreOfAsync("disable-sort-source", "Item 2"), (await BoxAsync(Item("disable-sort-target", "Item B"))).At(0.5, 0.3));

        await AssertOrderAsync("disable-sort-source", "disable-sort-source-order", "Item 1", "Item 3", "Item 4", "Item 5");
        await AssertOrderAsync("disable-sort-target", "disable-sort-target-order", "Item A", "Item 2", "Item B", "Item C");
    }

    [TestMethod]
    public async Task Put_AListThatDoesNotPut_RefusesItemsFromTheOtherList()
    {
        await OpenAsync("/e2e/sortable/clone");

        // Straight across, along Item A's own row, so that its home list has nothing to reorder on the way.
        await PressAsync(await CentreOfAsync("disable-sort-target", "Item A"));
        await MoveAsync(await CentreOfAsync("disable-sort-source", "Item 1"));
        await Expect(Placeholder("disable-sort-source")).ToHaveCountAsync(0);
        await Expect(Item("disable-sort-target", "Item A")).Not.ToHaveClassAsync(HasClass("bit-srt-hid"));

        await ReleaseAsync();
        await AssertOrderAsync("disable-sort-source", "disable-sort-source-order", "Item 1", "Item 2", "Item 3", "Item 4", "Item 5");
        await AssertOrderAsync("disable-sort-target", "disable-sort-target-order", "Item A", "Item B", "Item C");
    }

    // Drags from one list into the one beside it: straight across first, then to the target.
    private async Task DragAcrossAsync(Point from, Point to)
    {
        await PressAsync(from);
        await MoveAsync(new Point(to.X, from.Y));
        await MoveAsync(to);
        await ReleaseAsync();
    }

    [TestMethod]
    public async Task Axe_EachSection_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);
        await AssertNoAxeViolationsAsync("#shared");

        await OpenAsync("/e2e/sortable/clone");
        await AssertNoAxeViolationsAsync("#clone");
        await AssertNoAxeViolationsAsync("#disable-sort");
    }

    [TestMethod]
    public async Task Axe_TheEmptyTrash_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);

        await AssertNoAxeViolationsAsync("#trash");
    }
}
