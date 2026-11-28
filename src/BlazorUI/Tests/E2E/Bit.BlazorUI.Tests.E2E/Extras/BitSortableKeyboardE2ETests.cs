namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Sorting a BitSortable without a pointer, with the real keyboard: a list is one tab stop whose items take the focus
/// from the arrow keys; Space picks the focused item up, the arrows move it - within its list and, past its edge, into
/// the next list of the group in that direction - Space drops it and Escape puts it back, the focus following the item
/// all the way and every step being read out through the live region. Keys pressed faster than the steps come back each
/// move the item one place, and keys pressed elsewhere, or on a control inside an item, are left alone.
/// </summary>
[TestClass]
[E2ERetry]
[DoNotParallelize]
public class BitSortableKeyboardE2ETests : BitSortableE2ETestBase
{
    private const string Url = "/e2e/sortable";
    private static readonly string[] Simple = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];

    [TestMethod]
    public async Task Keyboard_AList_IsOneTabStopWhoseItemsTakeTheFocusFromTheArrows()
    {
        await OpenAsync(Url);
        await Expect(Page.Locator("#simple-list > [data-bit-srt-item]")).ToHaveCountAsync(6);
        await Expect(Page.Locator("#simple-list > [tabindex='0']")).ToHaveCountAsync(1);

        await Item("simple-list", "Item 1").FocusAsync();
        await PressKeyAsync("ArrowDown");
        await PressKeyAsync("ArrowDown");

        Assert.AreEqual("Item 3", await FocusedAsync());
        // The focused item becomes the tab stop, and nothing moved.
        await Expect(Item("simple-list", "Item 3")).ToHaveAttributeAsync("tabindex", "0");
        await AssertOrderAsync("simple-list", "simple-order", Simple);

        await PressKeyAsync("End");
        Assert.AreEqual("Item 6", await FocusedAsync());
        await PressKeyAsync("Home");
        Assert.AreEqual("Item 1", await FocusedAsync());
    }

    [TestMethod]
    public async Task Keyboard_SpacePicksTheItemUp_TheArrowsMoveIt_AndSpaceDropsIt()
    {
        await OpenAsync(Url);

        await Item("simple-list", "Item 2").FocusAsync();
        await PressKeyAsync(" ");
        StringAssert.StartsWith(await AnnouncedAsync(), "Picked up Item 2. It is at position 2 of 6");
        await Expect(Item("simple-list", "Item 2")).ToHaveClassAsync(HasClass("bit-srt-chs"));

        await PressKeyAsync("ArrowDown");
        await PressKeyAsync("ArrowDown");
        // Live, like a pointer drag: the list shows the move, the bound list waits for the drop.
        await AssertLabelsAsync("simple-list", ["Item 1", "Item 3", "Item 4", "Item 2", "Item 5", "Item 6"]);
        StringAssert.StartsWith(await AnnouncedAsync(), "Item 2 moved to position 4 of 6");
        Assert.AreEqual("Item 2", await FocusedAsync(), "The focus follows the item.");

        await PressKeyAsync(" ");

        await AssertOrderAsync("simple-list", "simple-order", "Item 1", "Item 3", "Item 4", "Item 2", "Item 5", "Item 6");
        StringAssert.StartsWith(await AnnouncedAsync(), "Dropped Item 2 at position 4 of 6");
        Assert.AreEqual("Item 2", await FocusedAsync());
        await Expect(Page.Locator(".bit-srt-chs")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Keyboard_KeysPressedFasterThanTheStepsComeBack_EachMoveTheItemOnePlace()
    {
        await OpenAsync("/e2e/sortable/large-list");
        await Page.Locator("#large-list > [data-bit-srt-item]").First.FocusAsync();
        await PressKeyAsync(" ");

        // Without waiting in between: each press lands while the step before it is still in flight in .NET, or its
        // slide still runs - and the item, seen mid-slide, is still where it was.
        for (int i = 0; i < 12; i++) await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync(" ");
        await WaitForQuietAsync();

        var index = await Page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('#large-list > [data-bit-srt-item]')].findIndex(e => e.querySelector('.large-name').textContent === 'Row 1')");
        Assert.AreEqual(10, index, "Row 1 took one step per arrow pressed.");
        await Expect(Page.Locator("#large-order")).ToHaveTextAsync("Row 2, Row 3, Row 4, Row 5, Row 6");
        await Expect(Page.Locator(".bit-srt-chs")).ToHaveCountAsync(0);

        // The panel scrolled along with the row, below the rows first in view, whether the browser kept the focus on the
        // moved element (Firefox, WebKit) or took it away and had it given back (Chromium).
        await EventuallyAsync(() => Page.EvaluateAsync<bool>(
            """
            () => {
              const panel = document.getElementById('large-panel').getBoundingClientRect();
              const row = [...document.querySelectorAll('#large-list > [data-bit-srt-item]')].find(e => e.querySelector('.large-name').textContent === 'Row 1').getBoundingClientRect();
              return row.top >= panel.top - 1 && row.bottom <= panel.bottom + 1;
            }
            """), "Row 1 is scrolled into view in its panel");
    }

    [TestMethod]
    public async Task Keyboard_Escape_PutsTheItemBack()
    {
        await OpenAsync(Url);

        await Item("simple-list", "Item 1").FocusAsync();
        await PressKeyAsync("Enter");
        await PressKeyAsync("End");
        await AssertLabelsAsync("simple-list", ["Item 2", "Item 3", "Item 4", "Item 5", "Item 6", "Item 1"]);
        await PressKeyAsync("Escape");

        await AssertOrderAsync("simple-list", "simple-order", Simple);
        StringAssert.StartsWith(await AnnouncedAsync(), "Cancelled. Item 1 is back at position 1 of 6");
        Assert.AreEqual("Item 1", await FocusedAsync());
    }

    [TestMethod]
    public async Task Keyboard_ShiftEscape_DoesNotCancelTheDrag()
    {
        await OpenAsync(Url);

        await Item("simple-list", "Item 1").FocusAsync();
        await PressKeyAsync(" ");
        await PressKeyAsync("ArrowDown");

        // An Escape with a modifier is not the drag's (it is taken on the window only when bare).
        await PressKeyAsync("Shift+Escape");
        await Expect(Item("simple-list", "Item 1")).ToHaveClassAsync(HasClass("bit-srt-chs"));
        await AssertLabelsAsync("simple-list", ["Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6"]);

        await PressKeyAsync(" ");
        await AssertOrderAsync("simple-list", "simple-order", "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6");
    }

    [TestMethod]
    public async Task Keyboard_AnArrowPastTheList_CarriesTheItemIntoTheListBesideIt()
    {
        await OpenAsync("/e2e/sortable/shared");

        await Item("shared-a", "A2").FocusAsync();
        await PressKeyAsync(" ");
        await PressKeyAsync("ArrowRight");

        // The item itself is in list B now, focused there, beside the item level with it.
        await AssertLabelsAsync("shared-b", ["B1", "A2", "B2", "B3", "B4", "B5"]);
        await AssertLabelsAsync("shared-a", ["A1", "A3", "A4", "A5"]);
        Assert.AreEqual("A2", await FocusedAsync());
        StringAssert.StartsWith(await AnnouncedAsync(), "A2 moved to position 2 of 6 in List B");

        await PressKeyAsync("ArrowDown");
        await PressKeyAsync(" ");

        await AssertOrderAsync("shared-a", "shared-a-order", "A1", "A3", "A4", "A5");
        await AssertOrderAsync("shared-b", "shared-b-order", "B1", "B2", "A2", "B3", "B4", "B5");
        Assert.AreEqual("A2", await FocusedAsync());
    }

    [TestMethod]
    public async Task Keyboard_ADragIntoAnotherListCancelled_ComesHome()
    {
        await OpenAsync("/e2e/sortable/shared");

        await Item("shared-b", "B1").FocusAsync();
        await PressKeyAsync(" ");
        await PressKeyAsync("ArrowLeft");
        await AssertLabelsAsync("shared-a", ["B1", "A1", "A2", "A3", "A4", "A5"]);
        await PressKeyAsync("Escape");

        await AssertOrderAsync("shared-a", "shared-a-order", "A1", "A2", "A3", "A4", "A5");
        await AssertOrderAsync("shared-b", "shared-b-order", "B1", "B2", "B3", "B4", "B5");
        Assert.AreEqual("B1", await FocusedAsync());
    }

    [TestMethod]
    public async Task Keyboard_Tab_DropsTheItemAndTheFocusMovesOn()
    {
        await OpenAsync(Url);

        await Item("simple-list", "Item 1").FocusAsync();
        await PressKeyAsync(" ");
        await PressKeyAsync("ArrowDown");
        await PressKeyAsync("Tab");

        await AssertOrderAsync("simple-list", "simple-order", "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6");
        Assert.AreNotEqual("Item 1", await FocusedAsync(), "The focus was pulled back to the dropped item.");
    }

    [TestMethod]
    public async Task Keyboard_DuringADrag_KeysPressedElsewhereAreLeftAlone()
    {
        await OpenAsync(Url);

        await Item("simple-list", "Item 1").FocusAsync();
        await PressKeyAsync(" ");

        // A dialog the app opened (a ConfirmDrop asking, say) gets its keys: Enter, Space and Escape are not the drag's
        // there.
        var prevented = await Page.EvaluateAsync<bool[]>(
            """
            () => {
              const button = document.createElement('button');
              document.body.appendChild(button);
              const press = key => {
                const e = new KeyboardEvent('keydown', { key, bubbles: true, cancelable: true });
                button.dispatchEvent(e);
                return e.defaultPrevented;
              };
              const result = [press('Enter'), press(' '), press('Escape')];
              button.remove();
              return result;
            }
            """);
        Assert.IsFalse(prevented.Any(p => p), "A key pressed outside the dragged item was taken by the drag.");

        await Item("simple-list", "Item 1").FocusAsync();
        await PressKeyAsync("Escape");
        await AssertOrderAsync("simple-list", "simple-order", Simple);
    }

    [TestMethod]
    public async Task Keyboard_AKeyPressedInsideAnItem_IsLeftToTheItem()
    {
        await OpenAsync(Url);

        // A control of the app's own inside an item: its keys are its own.
        var picked = await Page.EvaluateAsync<bool>(
            """
            () => {
              const item = document.querySelector('#simple-list > [data-bit-srt-item]');
              const button = document.createElement('button');
              item.appendChild(button);
              const e = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
              button.dispatchEvent(e);
              button.remove();
              return e.defaultPrevented;
            }
            """);
        await WaitForQuietAsync();

        Assert.IsFalse(picked);
        await Expect(Page.Locator(".bit-srt-chs")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Keyboard_ShiftSpace_SelectsInAMultiDragList_AndPickingUpCarriesTheSelection()
    {
        await OpenAsync("/e2e/sortable/multi-drag");

        await Item("multi-list", "Item 2").FocusAsync();
        await PressKeyAsync("Shift+Space");
        StringAssert.StartsWith(await AnnouncedAsync(), "Item 2 selected. 1 item selected.");
        await PressKeyAsync("ArrowDown");
        await PressKeyAsync("ArrowDown");
        await PressKeyAsync("Shift+Space");
        StringAssert.StartsWith(await AnnouncedAsync(), "Item 4 selected. 2 items selected.");
        await Expect(Page.Locator("#multi-selected")).ToHaveTextAsync("Item 2, Item 4");

        await PressKeyAsync(" ");
        await PressKeyAsync("End");
        await PressKeyAsync(" ");

        await AssertOrderAsync("multi-list", "multi-order", "Item 1", "Item 3", "Item 5", "Item 6", "Item 2", "Item 4");
    }
}
