using Bit.Bortable.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bortable.Tests.E2E;

/// <summary>
/// Sorting without a pointer, with the real keyboard: a list is one tab stop whose items take the focus from the
/// arrow keys; Space picks the focused item up, the arrows move it - within its list and, past its edge, into the
/// next list of the group in that direction - Space drops it and Escape puts it back, the focus following the item
/// all the way and every step being read out through the live region.
/// </summary>
[TestClass]
public sealed class KeyboardTests : BortableTest
{
    private static readonly string[] Simple = ["Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6"];

    /// <summary>The text of the focused element, which is an item wrapper during a keyboard drag.</summary>
    private static Task<string> FocusedAsync(IPage page) =>
        page.EvaluateAsync<string>("() => document.activeElement?.textContent?.trim() ?? ''");

    /// <summary>What the live region last read out.</summary>
    private static Task<string> AnnouncedAsync(IPage page) =>
        page.EvaluateAsync<string>("() => document.querySelector('[data-bortable-live]')?.textContent?.trim() ?? ''");

    /// <summary>Presses a key and waits until the page has acted on it.</summary>
    private static async Task PressKeyAsync(IPage page, string key)
    {
        await page.Keyboard.PressAsync(key);
        await WaitForQuietAsync(page);
    }

    [TestMethod]
    public async Task A_list_is_one_tab_stop_whose_items_take_the_focus_from_the_arrows()
    {
        var page = await OpenAsync("/simple-list");
        var items = page.Locator("#simple-list > [data-bortable-item]");
        await Expect(items).ToHaveCountAsync(6);
        Assert.AreEqual(1, await page.Locator("#simple-list > [tabindex='0']").CountAsync(), "One item is the list's tab stop.");

        await Item(page, "simple-list", "Item 1").FocusAsync();
        await PressKeyAsync(page, "ArrowDown");
        await PressKeyAsync(page, "ArrowDown");

        Assert.AreEqual("Item 3", await FocusedAsync(page));
        // The focused item becomes the tab stop, and nothing moved.
        await Expect(Item(page, "simple-list", "Item 3")).ToHaveAttributeAsync("tabindex", "0");
        await AssertOrderAsync(page, "simple-list", "simple-order", Simple);

        await PressKeyAsync(page, "End");
        Assert.AreEqual("Item 6", await FocusedAsync(page));
        await PressKeyAsync(page, "Home");
        Assert.AreEqual("Item 1", await FocusedAsync(page));
    }

    [TestMethod]
    public async Task Space_picks_the_item_up_the_arrows_move_it_and_Space_drops_it()
    {
        var page = await OpenAsync("/simple-list");

        await Item(page, "simple-list", "Item 2").FocusAsync();
        await PressKeyAsync(page, " ");
        StringAssert.StartsWith(await AnnouncedAsync(page), "Picked up Item 2. It is at position 2 of 6");
        await Expect(Item(page, "simple-list", "Item 2")).ToHaveClassAsync(HasClass("bit-bortable-chosen"));

        await PressKeyAsync(page, "ArrowDown");
        await PressKeyAsync(page, "ArrowDown");
        // Live, like a pointer drag: the list shows the move, the bound list waits for the drop.
        await AssertLabelsAsync(page, "simple-list", ["Item 1", "Item 3", "Item 4", "Item 2", "Item 5", "Item 6"]);
        StringAssert.StartsWith(await AnnouncedAsync(page), "Item 2 moved to position 4 of 6");
        Assert.AreEqual("Item 2", await FocusedAsync(page), "The focus follows the item.");

        await PressKeyAsync(page, " ");

        await AssertOrderAsync(page, "simple-list", "simple-order", "Item 1", "Item 3", "Item 4", "Item 2", "Item 5", "Item 6");
        StringAssert.StartsWith(await AnnouncedAsync(page), "Dropped Item 2 at position 4 of 6");
        Assert.AreEqual("Item 2", await FocusedAsync(page));
        await Expect(page.Locator(".bit-bortable-chosen")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Keys_pressed_faster_than_the_steps_come_back_each_move_the_item_one_place()
    {
        var page = await OpenAsync("/large-list");
        await page.Locator("#large-list > [data-bortable-item]").First.FocusAsync();
        await PressKeyAsync(page, " ");

        // Without waiting in between: each press lands while the step before it is still in flight in .NET, or its
        // slide still runs - and the item, seen mid-slide, is still where it was.
        for (int i = 0; i < 12; i++) await page.Keyboard.PressAsync("ArrowDown");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync("ArrowUp");
        await page.Keyboard.PressAsync(" ");
        await WaitForQuietAsync(page);

        var index = await page.EvaluateAsync<int>(
            "() => [...document.querySelectorAll('#large-list > [data-bortable-item]')].findIndex(e => e.querySelector('.large-name').textContent === 'Row 1')");
        Assert.AreEqual(10, index, "Row 1 took one step per arrow pressed.");
        await Expect(page.Locator("#large-order")).ToHaveTextAsync("Row 2, Row 3, Row 4, Row 5, Row 6");
        await Expect(page.Locator(".bit-bortable-chosen")).ToHaveCountAsync(0);

        // The panel scrolled along with the row, below the rows first in view, whether the browser kept the focus on the
        // moved element (Firefox, WebKit) or took it away and had it given back (Chromium).
        await EventuallyAsync(() => page.EvaluateAsync<bool>(
            """
            () => {
              const panel = document.getElementById('large-panel').getBoundingClientRect();
              const row = [...document.querySelectorAll('#large-list > [data-bortable-item]')].find(e => e.querySelector('.large-name').textContent === 'Row 1').getBoundingClientRect();
              return row.top >= panel.top - 1 && row.bottom <= panel.bottom + 1;
            }
            """), "Row 1 is scrolled into view in its panel");
    }

    [TestMethod]
    public async Task Escape_puts_the_item_back()
    {
        var page = await OpenAsync("/simple-list");

        await Item(page, "simple-list", "Item 1").FocusAsync();
        await PressKeyAsync(page, "Enter");
        await PressKeyAsync(page, "End");
        await AssertLabelsAsync(page, "simple-list", ["Item 2", "Item 3", "Item 4", "Item 5", "Item 6", "Item 1"]);
        await PressKeyAsync(page, "Escape");

        await AssertOrderAsync(page, "simple-list", "simple-order", Simple);
        StringAssert.StartsWith(await AnnouncedAsync(page), "Cancelled. Item 1 is back at position 1 of 6");
        Assert.AreEqual("Item 1", await FocusedAsync(page));
    }

    [TestMethod]
    public async Task An_arrow_past_the_list_carries_the_item_into_the_list_beside_it()
    {
        var page = await OpenAsync("/shared");

        await Item(page, "shared-a", "A2").FocusAsync();
        await PressKeyAsync(page, " ");
        await PressKeyAsync(page, "ArrowRight");

        // The item itself is in list B now, focused there, beside the item level with it.
        await AssertLabelsAsync(page, "shared-b", ["B1", "A2", "B2", "B3", "B4", "B5"]);
        await AssertLabelsAsync(page, "shared-a", ["A1", "A3", "A4", "A5"]);
        Assert.AreEqual("A2", await FocusedAsync(page));

        await PressKeyAsync(page, "ArrowDown");
        await PressKeyAsync(page, " ");

        await AssertOrderAsync(page, "shared-a", "shared-a-order", "A1", "A3", "A4", "A5");
        await AssertOrderAsync(page, "shared-b", "shared-b-order", "B1", "B2", "A2", "B3", "B4", "B5");
        Assert.AreEqual("A2", await FocusedAsync(page));
    }

    [TestMethod]
    public async Task A_keyboard_drag_into_another_list_cancelled_comes_home()
    {
        var page = await OpenAsync("/shared");

        await Item(page, "shared-b", "B1").FocusAsync();
        await PressKeyAsync(page, " ");
        await PressKeyAsync(page, "ArrowLeft");
        await AssertLabelsAsync(page, "shared-a", ["B1", "A1", "A2", "A3", "A4", "A5"]);
        await PressKeyAsync(page, "Escape");

        await AssertOrderAsync(page, "shared-a", "shared-a-order", "A1", "A2", "A3", "A4", "A5");
        await AssertOrderAsync(page, "shared-b", "shared-b-order", "B1", "B2", "B3", "B4", "B5");
        Assert.AreEqual("B1", await FocusedAsync(page));
    }

    [TestMethod]
    public async Task Tab_drops_the_item_and_the_focus_moves_on()
    {
        var page = await OpenAsync("/simple-list");

        await Item(page, "simple-list", "Item 1").FocusAsync();
        await PressKeyAsync(page, " ");
        await PressKeyAsync(page, "ArrowDown");
        await PressKeyAsync(page, "Tab");

        await AssertOrderAsync(page, "simple-list", "simple-order", "Item 2", "Item 1", "Item 3", "Item 4", "Item 5", "Item 6");
        Assert.AreNotEqual("Item 1", await FocusedAsync(page), "The focus was pulled back to the dropped item.");
    }

    [TestMethod]
    public async Task During_a_keyboard_drag_keys_pressed_elsewhere_are_left_alone()
    {
        var page = await OpenAsync("/simple-list");

        await Item(page, "simple-list", "Item 1").FocusAsync();
        await PressKeyAsync(page, " ");

        // A dialog the app opened (a ConfirmDrop asking, say) gets its keys: Enter and Space are not the drag's there.
        var prevented = await page.EvaluateAsync<bool[]>(
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

        await Item(page, "simple-list", "Item 1").FocusAsync();
        await PressKeyAsync(page, "Escape");
        await AssertOrderAsync(page, "simple-list", "simple-order", Simple);
    }

    [TestMethod]
    public async Task A_key_pressed_inside_an_item_is_left_to_the_item()
    {
        var page = await OpenAsync("/simple-list");

        // A control of the app's own inside an item: its keys are its own.
        var picked = await page.EvaluateAsync<bool>(
            """
            () => {
              const item = document.querySelector('#simple-list > [data-bortable-item]');
              const button = document.createElement('button');
              item.appendChild(button);
              const e = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
              button.dispatchEvent(e);
              button.remove();
              return e.defaultPrevented;
            }
            """);
        await WaitForQuietAsync(page);

        Assert.IsFalse(picked);
        await Expect(page.Locator(".bit-bortable-chosen")).ToHaveCountAsync(0);
    }
}
