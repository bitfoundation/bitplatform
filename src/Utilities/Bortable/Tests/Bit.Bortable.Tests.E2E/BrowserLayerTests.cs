using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// <c>bit-bortable.js</c> on its own, with the browser's real layout and event dispatch but no mouse: the module is
/// imported into a demo page and called on lists built for the test, or handed events made by hand. It covers what
/// a drag across the demo pages would only reach by chance - the sort axis of a list with a single item, and which
/// drags the module leaves alone.
/// </summary>
[TestClass]
public sealed class BrowserLayerTests : BortableTest
{
    /// <summary>
    /// Builds a list whose container and single item take the given inline styles, drops a dragged item in from
    /// another list (no slot of its own here) with the pointer at (<paramref name="x"/>, <paramref name="y"/>),
    /// expressed as fractions of the item's box, and returns where <c>dropIndex</c> puts it.
    /// </summary>
    private static Task<int> DropIndexOverSingleItemAsync(IPage page, string listStyle, string itemStyle, double x, double y) =>
        page.EvaluateAsync<int>(
            """
            async ([listStyle, itemStyle, x, y]) => {
              const module = await import('./_content/Bit.Bortable/bit-bortable.js');
              const list = document.createElement('div');
              list.setAttribute('data-bortable-list', 'test-list');
              list.style.cssText = 'position: fixed; left: 20px; top: 20px; width: 600px; ' + listStyle;
              const item = document.createElement('div');
              item.setAttribute('data-bortable-item', 'true');
              item.style.cssText = 'width: 100px; height: 40px; ' + itemStyle;
              list.appendChild(item);
              document.body.appendChild(list);
              try {
                const box = item.getBoundingClientRect();
                return module.dropIndex(list, box.left + box.width * x, box.top + box.height * y, 1, false, 0, 'auto');
              } finally {
                list.remove();
              }
            }
            """,
            new object[] { listStyle, itemStyle, x, y });

    [TestMethod]
    public async Task A_single_item_in_a_flex_row_sorts_along_the_row()
    {
        var page = await OpenAsync("/simple-list");

        // The right half of the item, its upper half: after it along a row, before it down a column.
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: flex", "", 0.75, 0.25));
        Assert.AreEqual(0, await DropIndexOverSingleItemAsync(page, "display: flex", "", 0.25, 0.75));
        // The gap to the right of the item, level with its top: the end of the row.
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: flex", "", 3, 0.25));
    }

    [TestMethod]
    public async Task A_single_item_in_a_flex_column_sorts_down_the_column()
    {
        var page = await OpenAsync("/simple-list");

        Assert.AreEqual(0, await DropIndexOverSingleItemAsync(page, "display: flex; flex-direction: column", "", 0.75, 0.25));
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: flex; flex-direction: column", "", 0.25, 0.75));
    }

    [TestMethod]
    public async Task A_single_item_reads_its_own_layout_in_a_block_list()
    {
        var page = await OpenAsync("/simple-list");

        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: block", "display: inline-block", 0.75, 0.25));
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: block", "float: left", 0.75, 0.25));
        Assert.AreEqual(0, await DropIndexOverSingleItemAsync(page, "display: block", "display: block", 0.75, 0.25));
    }

    /// <summary>
    /// Dispatches a dragover carrying <c>type</c> on each element the selectors pick, and answers whether each was
    /// cancelled - which is what lets the browser drop a drag there.
    /// </summary>
    private static Task<bool[]> DragOversCancelledAsync(IPage page, params (string Selector, string Type)[] targets) =>
        page.EvaluateAsync<bool[]>(
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
    public async Task Only_a_drag_of_the_lists_own_is_allowed_to_drop_and_only_on_a_list_that_takes_it()
    {
        var page = await OpenAsync("/disable-sort");

        // Without a drag in progress, nothing is a list's drag - not even one that claims to be.
        var idle = await DragOversCancelledAsync(page, ("#disable-sort-target", "application/x-bit-bortable"));
        Assert.IsFalse(idle[0], "A drag no list started was allowed to drop.");

        // Whether the browser shows the mark to the events of the drag (WebKit on Windows carries none of a drag's data).
        await page.EvaluateAsync("""
            () => document.addEventListener('dragover', e => {
              if (e.isTrusted) window.__markShown ??= e.dataTransfer.types.includes('application/x-bit-bortable');
            }, true)
            """);

        // A drag from the target list: the Put="false" source does not take it, the target does.
        await PressAsync(page, (await BoxAsync(Item(page, "disable-sort-target", "Item A"))).Centre);
        await EventuallyAsync(async () => (await DragOversCancelledAsync(page, ("#disable-sort-target", "application/x-bit-bortable")))[0],
            "the list the drag started in allows its drop");

        var during = await DragOversCancelledAsync(page,
            ("#disable-sort-target", "text/plain"),
            ("#disable-sort-source", "application/x-bit-bortable"),
            ("h1", "application/x-bit-bortable"));
        bool markShown = await page.EvaluateAsync<bool>("() => window.__markShown !== false");
        await ReleaseAsync(page);

        // Where the browser hides the mark, the drag in progress is the list's own whatever it carries.
        if (markShown) Assert.IsFalse(during[0], "Text dragged over the list was swallowed.");
        Assert.IsFalse(during[1], "A list that refuses the item allowed its drop.");
        Assert.IsFalse(during[2], "The page outside the lists allowed the drop.");
    }

    /// <summary>
    /// Builds a list of <paramref name="count"/> items (100 x 40 each) whose container takes <paramref name="listStyle"/>,
    /// and returns where <c>dropIndex</c> puts an item dragged in from another list with the pointer at the point
    /// <paramref name="point"/> computes from the items' boxes (a JavaScript arrow function of them).
    /// </summary>
    private static Task<int> DropIndexInBuiltListAsync(IPage page, string listStyle, int count, string point) =>
        page.EvaluateAsync<int>(
            """
            async ([listStyle, count, point]) => {
              const module = await import('./_content/Bit.Bortable/bit-bortable.js');
              const list = document.createElement('div');
              list.setAttribute('data-bortable-list', 'test-list');
              list.style.cssText = 'position: fixed; left: 20px; top: 20px; width: 600px; padding: 50px; ' + listStyle;
              for (let i = 0; i < count; i++) {
                const item = document.createElement('div');
                item.setAttribute('data-bortable-item', 'true');
                item.style.cssText = 'width: 100px; height: 40px; flex: none;';
                list.appendChild(item);
              }
              document.body.appendChild(list);
              try {
                const boxes = [...list.children].map(c => c.getBoundingClientRect());
                const [x, y] = (0, eval)(point)(boxes);
                return module.dropIndex(list, x, y, 1, false, 0, 'auto');
              } finally {
                list.remove();
              }
            }
            """,
            new object[] { listStyle, count, point });

    [TestMethod]
    public async Task A_right_to_left_row_is_measured_in_its_own_order()
    {
        var page = await OpenAsync("/simple-list");

        // The first item is the rightmost one: left of the last item is the end of the list, right of the first its start.
        Assert.AreEqual(2, await DropIndexInBuiltListAsync(page, "display: flex; direction: rtl", 2, "b => [b[1].left - 20, b[1].top + 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync(page, "display: flex; direction: rtl", 2, "b => [b[0].right + 20, b[0].top + 20]"));
        // Over an item, its far half (the left one, right to left) is after it.
        Assert.AreEqual(1, await DropIndexInBuiltListAsync(page, "display: flex; direction: rtl", 2, "b => [b[0].left + 10, b[0].top + 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync(page, "display: flex; direction: rtl", 2, "b => [b[0].right - 10, b[0].top + 20]"));

        // The same row left to right, for contrast.
        Assert.AreEqual(2, await DropIndexInBuiltListAsync(page, "display: flex", 2, "b => [b[1].right + 20, b[1].top + 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync(page, "display: flex", 2, "b => [b[0].left - 20, b[0].top + 20]"));
    }

    [TestMethod]
    public async Task A_reversed_flex_direction_is_measured_in_its_own_order()
    {
        var page = await OpenAsync("/simple-list");

        Assert.AreEqual(2, await DropIndexInBuiltListAsync(page, "display: flex; flex-direction: row-reverse", 2, "b => [b[1].left - 20, b[1].top + 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync(page, "display: flex; flex-direction: row-reverse", 2, "b => [b[0].right + 20, b[0].top + 20]"));
        Assert.AreEqual(2, await DropIndexInBuiltListAsync(page, "display: flex; flex-direction: column-reverse", 2, "b => [b[1].left + 50, b[1].top - 20]"));
        Assert.AreEqual(0, await DropIndexInBuiltListAsync(page, "display: flex; flex-direction: column-reverse", 2, "b => [b[0].left + 50, b[0].bottom + 20]"));
        // A single item in a right-to-left row: the styles tell its order.
        Assert.AreEqual(1, await DropIndexInBuiltListAsync(page, "display: flex; direction: rtl", 1, "b => [b[0].left + 10, b[0].top + 20]"));
    }

    [TestMethod]
    public async Task A_delayed_press_that_moves_too_soon_gives_the_item_its_draggable_back()
    {
        var page = await OpenAsync("/simple-list");

        var draggable = await page.EvaluateAsync<string?[]>(
            """
            async () => {
              const module = await import('./_content/Bit.Bortable/bit-bortable.js');
              const list = document.createElement('div');
              list.setAttribute('data-bortable-list', 'delayed');
              const item = document.createElement('div');
              item.setAttribute('data-bortable-item', 'true');
              item.setAttribute('data-bortable-key', 'delayed-1');
              item.setAttribute('draggable', 'true');
              list.appendChild(item);
              document.body.appendChild(list);
              module.register(list, { invokeMethodAsync: () => Promise.resolve(null) }, { id: 'delayed', handle: null,
                swapThreshold: 1, invertSwap: false, invertedSwapThreshold: 0, direction: 'auto', animation: 0, swap: false,
                multiDrag: false, keyboard: false, delay: 300, touchDelay: 300, delayTolerance: 5, forceFallback: false,
                dragClass: '', scroll: false, scrollSensitivity: 0, scrollSpeed: 0 });
              const pointer = (type, x) => item.dispatchEvent(new PointerEvent(type, { bubbles: true, pointerId: 7, pointerType: 'mouse', clientX: x, clientY: 0, button: 0 }));
              try {
                pointer('pointerdown', 0);
                const held = item.getAttribute('draggable');
                pointer('pointermove', 40);   // further than the tolerance, before the delay is over
                pointer('pointerup', 40);
                return [held, item.getAttribute('draggable')];
              } finally {
                module.unregister(list);
                list.remove();
              }
            }
            """);

        Assert.AreEqual("false", draggable[0], "The item is not draggable while the delay runs.");
        Assert.AreEqual("true", draggable[1], "The item never got its draggable back.");
    }

    [TestMethod]
    public async Task An_invalid_handle_selector_starts_no_drag_and_throws_nothing()
    {
        var page = await OpenAsync("/simple-list");
        var problems = TrackProblems(page, BaseUrl);

        var draggable = await page.EvaluateAsync<string?>(
            """
            async () => {
              const module = await import('./_content/Bit.Bortable/bit-bortable.js');
              const list = document.createElement('div');
              list.setAttribute('data-bortable-list', 'bad-handle');
              const item = document.createElement('div');
              item.setAttribute('data-bortable-item', 'true');
              item.setAttribute('data-bortable-key', '1');
              item.setAttribute('draggable', 'true');
              list.appendChild(item);
              document.body.appendChild(list);
              const bridge = { invokeMethodAsync: () => Promise.resolve(null) };
              module.register(list, bridge, { id: 'bad-handle', handle: '[[not css', swapThreshold: 1, invertSwap: false,
                invertedSwapThreshold: 0, direction: 'auto', animation: 0, swap: false });
              try {
                item.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerType: 'mouse' }));
                item.dispatchEvent(new PointerEvent('pointerdown', { bubbles: true, pointerType: 'mouse' }));
                return item.getAttribute('draggable');
              } finally {
                module.unregister(list);
                list.remove();
              }
            }
            """);

        Assert.AreEqual("false", draggable);
        Assert.IsEmpty(Snapshot(problems), string.Join("\n", Snapshot(problems)));
    }

    [TestMethod]
    public async Task A_drag_inside_an_item_that_is_not_draggable_stays_the_apps()
    {
        var page = await OpenAsync("/handle");

        // An element of the app's own inside an item behind a handle, with a dragstart listener of its own.
        var seen = await page.EvaluateAsync<string[]?>(
            """
            () => {
              const item = document.querySelector('#handle-list > [data-bortable-item]');
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
        Assert.DoesNotContain("application/x-bit-bortable", seen, "The app's drag was taken for a sort.");
    }
}
