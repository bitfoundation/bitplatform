namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitSortable's MultiDrag on /e2e/sortable/multi-drag, with real clicks and drags: a click toggles an item's selection;
/// dragging a selected item moves the whole selection as a block, in its original order, within a list and into another
/// list, and spends the selection; dragging an unselected item drops the selection and moves only that item. A native
/// drag of a selection takes its drag image of a copy of the item that carries the count, gone right after, and never
/// marks the item itself; a drag of one item carries none.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableMultiDragE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable/multi-drag";
    private const string Selected = "bit-srt-sel";

    // Records the count on the drag image when the browser took it (after every dragstart handler), and whether the
    // dragged item itself was marked.
    private const string RecordCounts = """
        () => {
          window.__counts = [];
          const picture = () => document.querySelector('[data-bit-srt-count]');
          window.addEventListener('dragstart', e => {
            const item = e.target.closest('[data-bit-srt-item]');
            const image = picture();
            window.__counts.push(image ? image.getAttribute('data-bit-srt-count') + ':' + image.querySelector('.bit-srt-cnt')?.textContent : 'none');
            window.__counts.push('item:' + (item?.hasAttribute('data-bit-srt-count') ? 'marked' : 'clean'));
            setTimeout(() => window.__counts.push('after:' + (picture()?.getAttribute('data-bit-srt-count') ?? 'none')), 50);
          });
        }
        """;

    [TestMethod]
    public async Task Click_TogglesAnItemsSelection_BoundBothWays()
    {
        await OpenAsync(Url);

        await Item("multi-list", "Item 2").ClickAsync();
        await Item("multi-list", "Item 4").ClickAsync();
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveTextAsync(["Item 2", "Item 4"]);
        await Expect(Page.Locator("#multi-list > [data-bit-srt-selected='true']")).ToHaveCountAsync(2);
        await Expect(Page.Locator("#multi-selected")).ToHaveTextAsync("Item 2, Item 4");

        // A second click deselects; the selection belongs to its list, so the lists below are untouched.
        await Item("multi-list", "Item 2").ClickAsync();
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveTextAsync(["Item 4"]);
        await Expect(Page.Locator($"#multi-left > .{Selected}, #multi-right > .{Selected}")).ToHaveCountAsync(0);

        // And the page's own selection reaches the list.
        await Page.Locator("#multi-select-all").ClickAsync();
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(6);
        await Page.Locator("#multi-clear").ClickAsync();
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#multi-selected")).ToHaveTextAsync("nothing");
    }

    [TestMethod]
    public async Task Drag_ASelectedItem_MovesTheWholeSelectionAsABlock()
    {
        await OpenAsync(Url);
        await Item("multi-list", "Item 2").ClickAsync();
        await Item("multi-list", "Item 4").ClickAsync();
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(2);

        await DragAsync(await CentreOfAsync("multi-list", "Item 2"), await CentreOfAsync("multi-list", "Item 6"));

        // Item 2 lands where it was dropped, Item 4 joins it, in their original order, and the selection is spent.
        await AssertOrderAsync("multi-list", "multi-order", "Item 1", "Item 3", "Item 5", "Item 6", "Item 2", "Item 4");
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#multi-last")).ToHaveTextAsync("Item 2, Item 4 - dragged by Item 2");
    }

    [TestMethod]
    public async Task Drag_ASelection_MovesAsABlockIntoAnotherList()
    {
        await OpenAsync(Url);
        await Item("multi-left", "Item 1").ClickAsync();
        await Item("multi-left", "Item 3").ClickAsync();
        await Expect(Page.Locator($"#multi-left > .{Selected}")).ToHaveCountAsync(2);

        await DragAsync(await CentreOfAsync("multi-left", "Item 1"), (await BoxAsync(Item("multi-right", "Item C"))).At(0.5, 0.3));

        await AssertOrderAsync("multi-left", "multi-left-order", "Item 2", "Item 4");
        await AssertOrderAsync("multi-right", "multi-right-order", "Item A", "Item B", "Item 1", "Item 3", "Item C", "Item D");
        await Expect(Page.Locator($".{Selected}")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Drag_AnUnselectedItem_DropsTheSelectionAndMovesOnlyThatItem()
    {
        await OpenAsync(Url);
        await Item("multi-list", "Item 2").ClickAsync();
        await Item("multi-list", "Item 4").ClickAsync();
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(2);

        await PressAsync(await CentreOfAsync("multi-list", "Item 5"));
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(0);
        await MoveAsync(await CentreOfAsync("multi-list", "Item 1"));
        await ReleaseAsync();

        await AssertOrderAsync("multi-list", "multi-order", "Item 5", "Item 1", "Item 2", "Item 3", "Item 4", "Item 6");
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Count_ANativeDragOfASelection_ShowsHowManyItemsItCarries()
    {
        await OpenAsync(Url);
        await Page.EvaluateAsync(RecordCounts);
        await Item("multi-list", "Item 2").ClickAsync();
        await Item("multi-list", "Item 4").ClickAsync();
        await Item("multi-list", "Item 5").ClickAsync();
        await Expect(Page.Locator("#multi-list > [data-bit-srt-selected]")).ToHaveTextAsync(["Item 2", "Item 4", "Item 5"]);

        await DragAsync(await CentreOfAsync("multi-list", "Item 2"), await CentreOfAsync("multi-list", "Item 6"));

        // The count is on the drag image while the browser takes it, and gone right after; the item is left alone.
        await AssertOrderAsync("multi-list", "multi-order", "Item 1", "Item 3", "Item 6", "Item 2", "Item 4", "Item 5");
        CollectionAssert.AreEqual(new[] { "3:3", "item:clean", "after:none" }, await Page.EvaluateAsync<string[]>("() => window.__counts"));
        await Expect(Page.Locator("[data-bit-srt-count]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Count_ADragOfOneItem_CarriesNone()
    {
        await OpenAsync(Url);
        await Page.EvaluateAsync(RecordCounts);

        await DragAsync(await CentreOfAsync("multi-list", "Item 1"), (await BoxAsync(Item("multi-list", "Item 3"))).At(0.5, 0.7));

        Assert.AreEqual("none", (await Page.EvaluateAsync<string[]>("() => window.__counts"))[0]);
    }

    [TestMethod]
    public async Task Axe_EachSection_HasNoWcagAOrAAViolation()
    {
        await OpenAsync(Url);
        await Item("multi-list", "Item 2").ClickAsync();
        await Expect(Page.Locator($"#multi-list > .{Selected}")).ToHaveCountAsync(1);

        await AssertNoAxeViolationsAsync("#multi");
        await AssertNoAxeViolationsAsync("#multi-shared");
    }
}
