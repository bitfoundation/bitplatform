namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitSortable's script on its own (<c>BitBlazorUI.Sortable</c>), with the browser's real layout and event dispatch but
/// no mouse: called on lists built for the test, or handed events made by hand. It covers what a drag across the pages
/// would only reach by chance - the sort axis of a list with a single item, lists laid out against the axis, and which
/// drags the script leaves alone.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableScriptE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable";

    /// <summary>
    /// Builds a list whose root and single item take the given inline styles, drops a dragged item in from another list
    /// (no slot of its own here) with the pointer at (<paramref name="x"/>, <paramref name="y"/>), as fractions of the
    /// item's box, and returns where <c>dropIndex</c> puts it.
    /// </summary>
    private Task<int> DropIndexOverSingleItemAsync(string listStyle, string itemStyle, double x, double y) =>
        Page.EvaluateAsync<int>(
            """
            ([listStyle, itemStyle, x, y]) => {
              const list = document.createElement('div');
              list.setAttribute('data-bit-srt-list', 'test-list');
              list.style.cssText = 'position: fixed; left: 20px; top: 20px; width: 600px; ' + listStyle;
              const item = document.createElement('div');
              item.setAttribute('data-bit-srt-item', 'true');
              item.style.cssText = 'width: 100px; height: 40px; ' + itemStyle;
              list.appendChild(item);
              document.body.appendChild(list);
              try {
                const box = item.getBoundingClientRect();
                return BitBlazorUI.Sortable.dropIndex(list, box.left + box.width * x, box.top + box.height * y, 1, false, 0, 'auto');
              } finally {
                list.remove();
              }
            }
            """,
            new object[] { listStyle, itemStyle, x, y });

    /// <summary>
    /// Builds a list of <paramref name="count"/> items (100 x 40 each) whose root takes <paramref name="listStyle"/>, and
    /// returns where <c>dropIndex</c> puts an item dragged in from another list with the pointer at the point
    /// <paramref name="point"/> computes from the items' boxes (a JavaScript arrow function of them).
    /// </summary>
    private Task<int> DropIndexInBuiltListAsync(string listStyle, int count, string point) =>
        Page.EvaluateAsync<int>(
            """
            ([listStyle, count, point]) => {
              const list = document.createElement('div');
              list.setAttribute('data-bit-srt-list', 'test-list');
              list.style.cssText = 'position: fixed; left: 20px; top: 20px; width: 600px; padding: 50px; ' + listStyle;
              for (let i = 0; i < count; i++) {
                const item = document.createElement('div');
                item.setAttribute('data-bit-srt-item', 'true');
                item.style.cssText = 'width: 100px; height: 40px; flex: none;';
                list.appendChild(item);
              }
              document.body.appendChild(list);
              try {
                const boxes = [...list.children].map(c => c.getBoundingClientRect());
                const [x, y] = (0, eval)(point)(boxes);
                return BitBlazorUI.Sortable.dropIndex(list, x, y, 1, false, 0, 'auto');
              } finally {
                list.remove();
              }
            }
            """,
            new object[] { listStyle, count, point });

    /// <summary>
    /// Dispatches a dragover carrying <c>type</c> on each element the selectors pick, and answers whether each was
    /// cancelled - which is what lets the browser drop a drag there.
    /// </summary>
    private Task<bool[]> DragOversCancelledAsync(params (string Selector, string Type)[] targets) =>
        Page.EvaluateAsync<bool[]>(
            """
            targets => targets.map(([selector, type]) => {
              const data = new DataTransfer();
              data.setData(type, 'x');
              const e = new DragEvent('dragover', { bubbles: true, cancelable: true, dataTransfer: data, clientX: 1, clientY: 1 });
              document.querySelector(selector).dispatchEvent(e);
              return e.defaultPrevented;
            })
            """,
            targets.Select(t => new[] { t.Selector, t.Type }).ToArray());

    [TestMethod]
    public async Task DropIndex_ASingleItemInAFlexRow_SortsAlongTheRow()
    {
        await OpenAsync(Url);

        // The right half of the item, its upper half: after it along a row, before it down a column.
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync("display: flex", "", 0.75, 0.25));
        Assert.AreEqual(0, await DropIndexOverSingleItemAsync("display: flex", "", 0.25, 0.75));
        // The gap to the right of the item, level with its top: the end of the row.
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync("display: flex", "", 3, 0.25));
    }

    [TestMethod]
    public async Task DropIndex_ASingleItemInAFlexColumn_SortsDownTheColumn()
    {
        await OpenAsync(Url);

        Assert.AreEqual(0, await DropIndexOverSingleItemAsync("display: flex; flex-direction: column", "", 0.75, 0.25));
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync("display: flex; flex-direction: column", "", 0.25, 0.75));
    }

    [TestMethod]
    public async Task DropIndex_ASingleItem_ReadsItsOwnLayoutInABlockList()
    {
        await OpenAsync(Url);

        Assert.AreEqual(1, await DropIndexOverSingleItemAsync("display: block", "display: inline-block", 0.75, 0.25));
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync("display: block", "float: left", 0.75, 0.25));
        Assert.AreEqual(0, await DropIndexOverSingleItemAsync("display: block", "display: block", 0.75, 0.25));
    }

    [TestMethod]
    public async Task DropIndex_ARightToLeftRow_IsMeasuredInItsOwnOrder()
    {
        await OpenAsync(Url);

        // The first item is the rightmost one: left of the last item is the end of the list, right of the first its start.
        Assert.AreEqual(2, await DropIndexInBuiltListAsync("display: flex; direction: rtl", 2, "b => [b[1].left - 20, b[1].top + 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync("display: flex; direction: rtl", 2, "b => [b[0].right + 20, b[0].top + 20]"));
        // Over an item, its far half (the left one, right to left) is after it.
        Assert.AreEqual(1, await DropIndexInBuiltListAsync("display: flex; direction: rtl", 2, "b => [b[0].left + 10, b[0].top + 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync("display: flex; direction: rtl", 2, "b => [b[0].right - 10, b[0].top + 20]"));

        // The same row left to right, for contrast.
        Assert.AreEqual(2, await DropIndexInBuiltListAsync("display: flex", 2, "b => [b[1].right + 20, b[1].top + 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync("display: flex", 2, "b => [b[0].left - 20, b[0].top + 20]"));
    }

    [TestMethod]
    public async Task DropIndex_AReversedFlexDirection_IsMeasuredInItsOwnOrder()
    {
        await OpenAsync(Url);

        Assert.AreEqual(2, await DropIndexInBuiltListAsync("display: flex; flex-direction: row-reverse", 2, "b => [b[1].left - 20, b[1].top + 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync("display: flex; flex-direction: row-reverse", 2, "b => [b[0].right + 20, b[0].top + 20]"));
        Assert.AreEqual(2, await DropIndexInBuiltListAsync("display: flex; flex-direction: column-reverse", 2, "b => [b[1].left + 50, b[1].top - 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync("display: flex; flex-direction: column-reverse", 2, "b => [b[0].left + 50, b[0].bottom + 20]"));
        // A single item in a right-to-left row: the styles tell its order.
        Assert.AreEqual(1, await DropIndexInBuiltListAsync("display: flex; direction: rtl", 1, "b => [b[0].left + 10, b[0].top + 20]"));
    }

    [TestMethod]
    public async Task DragOver_OnlyADragOfTheListsOwn_IsAllowedToDrop_AndOnlyOnAListThatTakesIt()
    {
        await OpenAsync("/e2e/sortable/clone");

        // Without a drag in progress, nothing is a list's drag - not even one that claims to be.
        var idle = await DragOversCancelledAsync(("#disable-sort-target", "application/x-bit-sortable"));
        Assert.IsFalse(idle[0], "A drag no list started was allowed to drop.");

        // Whether the browser shows the mark to the events of the drag (WebKit on Windows carries none of a drag's data).
        await Page.EvaluateAsync("""
            () => document.addEventListener('dragover', e => {
              if (e.isTrusted) window.__markShown ??= e.dataTransfer.types.includes('application/x-bit-sortable');
            }, true)
            """);

        // A drag from the target list: the Put="false" source does not take it, the target does.
        await PressAsync(await CentreOfAsync("disable-sort-target", "Item A"));
        await EventuallyAsync(async () => (await DragOversCancelledAsync(("#disable-sort-target", "application/x-bit-sortable")))[0],
            "the list the drag started in allows its drop");

        var during = await DragOversCancelledAsync(
            ("#disable-sort-target", "text/plain"),
            ("#disable-sort-source", "application/x-bit-sortable"),
            ("h1", "application/x-bit-sortable"));
        bool markShown = await Page.EvaluateAsync<bool>("() => window.__markShown !== false");
        await ReleaseAsync();

        // Where the browser hides the mark, the drag in progress is the list's own whatever it carries.
        if (markShown) Assert.IsFalse(during[0], "Text dragged over the list was swallowed.");
        Assert.IsFalse(during[1], "A list that refuses the item allowed its drop.");
        Assert.IsFalse(during[2], "The page outside the lists allowed the drop.");
    }

    [TestMethod]
    public async Task Delay_ADelayedPressThatMovesTooSoon_GivesTheItemItsDraggableBack()
    {
        await OpenAsync(Url);

        var draggable = await Page.EvaluateAsync<string?[]>(
            """
            () => {
              const list = document.createElement('div');
              list.setAttribute('data-bit-srt-list', 'delayed');
              const item = document.createElement('div');
              item.setAttribute('data-bit-srt-item', 'true');
              item.setAttribute('data-bit-srt-key', 'delayed-1');
              item.setAttribute('draggable', 'true');
              list.appendChild(item);
              document.body.appendChild(list);
              BitBlazorUI.Sortable.register(list, { invokeMethodAsync: () => Promise.resolve(null) }, { id: 'delayed', handle: null,
                swapThreshold: 1, invertSwap: false, invertedSwapThreshold: 0, direction: 'auto', animation: 0, swap: false,
                multiDrag: false, keyboard: false, delay: 300, touchDelay: 300, delayTolerance: 5, forceFallback: false,
                dragClass: '', dragStyle: null, scroll: false, scrollSensitivity: 0, scrollSpeed: 0 });
              const pointer = (type, x) => item.dispatchEvent(new PointerEvent(type, { bubbles: true, pointerId: 7, pointerType: 'mouse', clientX: x, clientY: 0, button: 0 }));
              try {
                pointer('pointerdown', 0);
                const held = item.getAttribute('draggable');
                pointer('pointermove', 40);   // further than the tolerance, before the delay is over
                pointer('pointerup', 40);
                return [held, item.getAttribute('draggable')];
              } finally {
                BitBlazorUI.Sortable.unregister(list);
                list.remove();
              }
            }
            """);

        Assert.AreEqual("false", draggable[0], "The item is not draggable while the delay runs.");
        Assert.AreEqual("true", draggable[1], "The item never got its draggable back.");
    }

    [TestMethod]
    public async Task Handle_AnInvalidSelector_StartsNoDragAndThrowsNothing()
    {
        await OpenAsync(Url);

        // Any error the script raises fails the test at cleanup (E2ETestBase).
        var draggable = await Page.EvaluateAsync<string?>(
            """
            () => {
              const list = document.createElement('div');
              list.setAttribute('data-bit-srt-list', 'bad-handle');
              const item = document.createElement('div');
              item.setAttribute('data-bit-srt-item', 'true');
              item.setAttribute('data-bit-srt-key', '1');
              item.setAttribute('draggable', 'true');
              list.appendChild(item);
              document.body.appendChild(list);
              const bridge = { invokeMethodAsync: () => Promise.resolve(null) };
              BitBlazorUI.Sortable.register(list, bridge, { id: 'bad-handle', handle: '[[not css', swapThreshold: 1, invertSwap: false,
                invertedSwapThreshold: 0, direction: 'auto', animation: 0, swap: false });
              try {
                item.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerType: 'mouse' }));
                item.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerType: 'mouse' }));
                return item.getAttribute('draggable');
              } finally {
                BitBlazorUI.Sortable.unregister(list);
                list.remove();
              }
            }
            """);

        Assert.AreEqual("false", draggable);
    }

    [TestMethod]
    public async Task DragStart_InsideAnItemThatIsNotDraggable_StaysTheApps()
    {
        await OpenAsync("/e2e/sortable/handle");

        // An element of the app's own inside an item behind a handle, with a dragstart listener of its own.
        var seen = await Page.EvaluateAsync<string[]?>(
            """
            () => {
              const item = document.querySelector('#handle-list > [data-bit-srt-item]');
              const chip = document.createElement('span');
              chip.draggable = true;
              item.appendChild(chip);
              let types = null;
              chip.addEventListener('dragstart', e => { types = [...e.dataTransfer.types]; });
              const e = new DragEvent('dragstart', { bubbles: true, cancelable: true, dataTransfer: new DataTransfer() });
              chip.dispatchEvent(e);
              chip.remove();
              return types;
            }
            """);

        Assert.IsNotNull(seen, "The app's own dragstart listener never ran.");
        Assert.DoesNotContain("application/x-bit-sortable", seen, "The app's drag was taken for a sort.");
    }

    [TestMethod]
    public async Task WouldNest_AListOutsideAnyDrag_IsNeverRefused()
    {
        await OpenAsync("/e2e/sortable/nested");

        // With no drag running, no list is inside the dragged item.
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => BitBlazorUI.Sortable.wouldNest(document.getElementById('nested-item-2-2'))"));
        Assert.IsFalse(await Page.EvaluateAsync<bool>("() => BitBlazorUI.Sortable.wouldNest(null)"));
    }
}
