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
        var page = await OpenAsync("/");

        // The right half of the item, its upper half: after it along a row, before it down a column.
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: flex", "", 0.75, 0.25));
        Assert.AreEqual(0, await DropIndexOverSingleItemAsync(page, "display: flex", "", 0.25, 0.75));
        // The gap to the right of the item, level with its top: the end of the row.
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: flex", "", 3, 0.25));
    }

    [TestMethod]
    public async Task A_single_item_in_a_flex_column_sorts_down_the_column()
    {
        var page = await OpenAsync("/");

        Assert.AreEqual(0, await DropIndexOverSingleItemAsync(page, "display: flex; flex-direction: column", "", 0.75, 0.25));
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: flex; flex-direction: column", "", 0.25, 0.75));
    }

    [TestMethod]
    public async Task A_single_item_reads_its_own_layout_in_a_block_list()
    {
        var page = await OpenAsync("/");

        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: block", "display: inline-block", 0.75, 0.25));
        Assert.AreEqual(1, await DropIndexOverSingleItemAsync(page, "display: block", "float: left", 0.75, 0.25));
        Assert.AreEqual(0, await DropIndexOverSingleItemAsync(page, "display: block", "display: block", 0.75, 0.25));
    }

    [TestMethod]
    public async Task Only_a_drag_of_the_lists_own_is_allowed_to_drop_on_a_list()
    {
        var page = await OpenAsync("/");

        // A dragover of a list's own drag is cancelled, which is what lets the browser drop it there; a file or
        // a piece of text dragged over the same list keeps its default.
        var cancelled = await page.EvaluateAsync<bool[]>(
            """
            () => {
              const list = document.querySelector('[data-bortable-list]');
              const dragOver = type => {
                const data = new DataTransfer();
                data.setData(type, 'x');
                const e = new DragEvent('dragover', { bubbles: true, cancelable: true, dataTransfer: data });
                list.dispatchEvent(e);
                return e.defaultPrevented;
              };
              return [dragOver('application/x-bit-bortable'), dragOver('text/plain')];
            }
            """);

        Assert.IsTrue(cancelled[0], "The list's own drag was not allowed to drop.");
        Assert.IsFalse(cancelled[1], "Text dragged over the list was swallowed.");
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
