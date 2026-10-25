using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitDataGrid's row reordering on /e2e/data-grid/row-reorder: the drag handle column and its accessible name, a mouse
/// drag of a handle onto another row (HTML5 drag and drop) moving the item in the bound list, a touch drag (the pointer
/// events of a touch pointer, the drop target highlighted on the way), the handle's ArrowUp/ArrowDown with the focus kept
/// on the moved row, OnRowReorder with the dragged and target items and indexes, the pause while the view is sorted, and
/// a touch drag of a column header.
/// </summary>
[TestClass]
public class BitDataGridRowReorderE2ETests : BitDataGridE2ETestBase
{
    private const string Url = "/e2e/data-grid/row-reorder";
    private const string Id = "grid-row-reorder";

    private ILocator Handle(int row) => Cell(Id, row, 1).Locator("button.bit-dtg-drag-handle");

    // Drags with the pointer events a finger raises: native drag and drop does not fire for touch, so the grid follows
    // these instead. Returns whether the element under the finger was highlighted as the drop target on the way.
    private Task<bool> TouchDragAsync(string fromSelector, string toSelector) => Page.EvaluateAsync<bool>("""
        ([fromSel, toSel]) => {
            const from = document.querySelector(fromSel);
            const to = document.querySelector(toSel);
            const a = from.getBoundingClientRect(), b = to.getBoundingClientRect();
            const at = (r) => ({ bubbles: true, cancelable: true, composed: true, pointerId: 11, pointerType: 'touch', isPrimary: true, clientX: r.x + r.width / 2, clientY: r.y + r.height / 2 });
            from.dispatchEvent(new PointerEvent('pointerdown', at(a)));
            from.dispatchEvent(new PointerEvent('pointermove', at(b)));
            const highlighted = !!document.elementFromPoint(at(b).clientX, at(b).clientY)?.closest('.bit-dtg-drop-target');
            from.dispatchEvent(new PointerEvent('pointerup', at(b)));
            return highlighted;
        }
        """, new[] { fromSelector, toSelector });

    [TestMethod]
    public async Task Handle_TheReorderColumn_HoldsANamedDraggableHandlePerRow()
    {
        await OpenAsync(Url);

        await Expect(HeaderCell(Id, 1)).ToHaveTextAsync("Reorder");
        await Expect(HeaderCell(Id, 1).Locator(".bit-dtg-visually-hidden")).ToHaveCountAsync(1);
        await Expect(Handle(0)).ToHaveAccessibleNameAsync("Reorder row. Press Arrow Up or Arrow Down to move this row.");
        await Expect(Handle(0)).ToHaveAttributeAsync("title", "Drag to reorder, or focus and use the arrow keys to move this row");
        await Expect(Handle(0)).ToHaveAttributeAsync("draggable", "true");
        await Expect(Handle(0)).ToHaveTextAsync("⠿");
        var indexes = await DataRows(Id).EvaluateAllAsync<string[]>("rows => rows.map(r => r.getAttribute('data-ri'))");
        CollectionAssert.AreEqual(new[] { "0", "1", "2", "3", "4" }, indexes);
    }

    [TestMethod]
    public async Task Mouse_DraggingAHandleOntoARow_MovesTheItem()
    {
        await OpenAsync(Url);

        await Handle(0).DragToAsync(DataRow(Id, 2));

        await ExpectColumnAsync(Id, 3, "Novel", "Jacket", "Laptop", "Lamp", "Drone");
        await Expect(Text("reorder-log")).ToHaveTextAsync("Laptop->Jacket 0->2");
        await Expect(Text("reorder-order")).ToHaveTextAsync("Novel,Jacket,Laptop,Lamp,Drone");
        await Expect(Text("reorder-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Touch_DraggingAHandleOntoARow_MovesTheItem()
    {
        await OpenAsync(Url);

        var highlighted = await TouchDragAsync(
            "#grid-row-reorder .bit-dtg-body > .bit-dtg-row[data-ri='4'] .bit-dtg-drag-handle",
            "#grid-row-reorder .bit-dtg-body > .bit-dtg-row[data-ri='1'] [aria-colindex='3']");

        Assert.IsTrue(highlighted, "The row under the finger was not marked as the drop target.");
        await ExpectColumnAsync(Id, 3, "Laptop", "Drone", "Novel", "Jacket", "Lamp");
        await Expect(Text("reorder-log")).ToHaveTextAsync("Drone->Novel 4->1");
        await Expect(Page.Locator("#grid-row-reorder .bit-dtg-drop-target")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Keyboard_TheHandlesArrowKeys_MoveTheRowAndKeepTheFocusOnIt()
    {
        await OpenAsync(Url);

        await Handle(3).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");

        await ExpectColumnAsync(Id, 3, "Laptop", "Novel", "Lamp", "Jacket", "Drone");
        await Expect(Text("reorder-log")).ToHaveTextAsync("Lamp->Jacket 3->2");
        await ExpectFocusedAsync(Handle(2));

        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectColumnAsync(Id, 3, "Laptop", "Lamp", "Novel", "Jacket", "Drone");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectColumnAsync(Id, 3, "Laptop", "Novel", "Lamp", "Jacket", "Drone");
        await Expect(Text("reorder-count")).ToHaveTextAsync("3");

        // The arrows moved the row rather than scrolling the page.
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => document.scrollingElement.scrollTop"), 1);
    }

    [TestMethod]
    public async Task Sorted_TheView_PausesReorderingUntilTheSortIsGone()
    {
        await OpenAsync(Url);

        await HeaderButton(Id, "Name").ClickAsync();
        await ExpectHeaderTitlesAsync(Id, "Id", "Name");
        await Expect(Page.Locator("#grid-row-reorder .bit-dtg-drag-handle")).ToHaveCountAsync(0);
        await Expect(DataRow(Id, 0)).Not.ToHaveAttributeAsync("data-ri", new Regex(".*"));

        await HeaderButton(Id, "Name").ClickAsync();
        await HeaderButton(Id, "Name").ClickAsync();
        await Expect(Page.Locator("#grid-row-reorder .bit-dtg-body .bit-dtg-drag-handle")).ToHaveCountAsync(5);
    }

    [TestMethod]
    public async Task Touch_DraggingAColumnHeader_MovesTheColumn()
    {
        await OpenAsync(Url);

        const string id = "grid-touch-columns";
        var highlighted = await TouchDragAsync(
            "#grid-touch-columns .bit-dtg-header-row [data-col='Id'] .bit-dtg-htext",
            "#grid-touch-columns .bit-dtg-header-row [data-col='Stock']");

        Assert.IsTrue(highlighted, "The header under the finger was not marked as the drop target.");
        await Expect(HeaderCells(id)).ToHaveTextAsync(["Name", "Stock", "Id"]);
    }
}
