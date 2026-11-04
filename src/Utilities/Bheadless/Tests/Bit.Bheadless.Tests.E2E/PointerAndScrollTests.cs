using System.Text.RegularExpressions;
using Bit.Bheadless.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bheadless.Tests.E2E;

/// <summary>
/// Behaviour the runtime decides from layout, pointer presses and the browser's own focus handling, which bUnit has
/// none of: scrolling an option fully into view, placing a submenu that has no room, judging a focus change by the
/// press that caused it, giving the focus back to a button the press did not focus (Safari focuses no button it
/// clicks; the tests make Chromium behave the same), and keeping the keys on a menu whose item was pressed.
/// </summary>
[TestClass]
public sealed class PointerAndScrollTests : DemoTest
{
    /// <summary>The live sample of the example titled <paramref name="title"/>.</summary>
    private static ILocator Sample(IPage page, string title) =>
        page.Locator($"{Content} .bh-example", new PageLocatorOptions { Has = page.Locator("h2", new PageLocatorOptions { HasTextRegex = new Regex($"^\\s*{Regex.Escape(title)}\\s*$") }) })
            .Locator(".bh-example-panel .demo-row").First;

    /// <summary>
    /// Makes a press on <paramref name="target"/> leave the focus where it was, or put it on the example's Preview
    /// panel - the button's focusable ancestor - as Safari does.
    /// </summary>
    private static Task PressLikeSafariAsync(ILocator target, bool focusAncestor) => target.EvaluateAsync(
        """
        (el, focusAncestor) => el.addEventListener('mousedown', e => {
            e.preventDefault();
            if (focusAncestor) el.closest('.bh-example-panel').focus();
        }, true)
        """, focusAncestor);

    [TestMethod]
    public async Task End_scrolls_the_last_option_of_a_virtualized_list_fully_into_view()
    {
        var page = await OpenDemoAsync("/listbox");
        var button = Sample(page, "Large lists (virtualized)").Locator("[role=combobox]").First;
        await button.ClickAsync();
        await page.Keyboard.PressAsync("End");

        // The panel's padding-top is part of every option's offset: left out, the last one ended 4px under the edge.
        await PollAsync(() => button.EvaluateAsync<double>(
            """
            b => {
              const option = document.getElementById(b.getAttribute('aria-activedescendant'));
              if (!option) return 99;
              const list = option.closest('[role=listbox]');
              return option.getBoundingClientRect().bottom - (list.getBoundingClientRect().top + list.clientTop + list.clientHeight);
            }
            """), overflow => overflow <= 0.5, "the last option inside the panel");
    }

    [TestMethod]
    public async Task A_query_typed_into_a_scrolled_combobox_shows_the_option_it_highlights()
    {
        var page = await OpenDemoAsync("/combobox");
        var input = Sample(page, "Basic Combobox").Locator("input[role=combobox]");
        await input.ClickAsync();
        await page.Keyboard.PressAsync("ArrowDown");
        var panel = Sample(page, "Basic Combobox").Locator("[role=listbox]");
        await Expect(panel).ToBeVisibleAsync();
        await panel.EvaluateAsync("p => p.scrollTop = p.scrollHeight");

        // Enter selects the highlight: it has to be in sight.
        await page.Keyboard.TypeAsync("a");
        await PollAsync(() => input.EvaluateAsync<bool>(
            """
            i => {
              const option = document.getElementById(i.getAttribute('aria-activedescendant'));
              if (!option) return false;
              const o = option.getBoundingClientRect(), l = option.closest('[role=listbox]').getBoundingClientRect();
              return o.top >= l.top - 1 && o.bottom <= l.bottom + 1;
            }
            """), true, "the highlighted option inside the panel");
    }

    [TestMethod]
    public async Task A_press_on_a_checkable_menu_item_keeps_the_keys_on_the_menu()
    {
        var page = await OpenDemoAsync("/menubar");
        var sample = Sample(page, "Application menubar");
        await sample.Locator("[role=menubar] [role=menuitem]", new LocatorLocatorOptions { HasText = "View" }).ClickAsync();
        var item = page.Locator("[role=menuitemcheckbox]:visible").First;
        await item.ClickAsync();
        await page.Mouse.MoveAsync(5, 400);

        // Focused, the item would take the keys from the menu: arrows and Space would scroll the page.
        await Expect(page.Locator("[role=menu]:visible").First).ToBeFocusedAsync();
        var before = await page.EvaluateAsync<double>("() => scrollY");
        foreach (var key in new[] { "ArrowDown", "ArrowDown", "Space", "End" }) await page.Keyboard.PressAsync(key);
        Assert.AreEqual(before, await page.EvaluateAsync<double>("() => scrollY"), "The keys scrolled the page.");
    }

    [TestMethod]
    public async Task Typeahead_reaches_a_checked_menu_item()
    {
        var page = await OpenDemoAsync("/menubar");
        var sample = Sample(page, "Checkbox and radio items driving the UI");
        await sample.Locator("[role=menubar] [role=menuitem]", new LocatorLocatorOptions { HasText = "View" }).ClickAsync();
        await Expect(page.Locator("[role=menu]:visible").First).ToBeFocusedAsync();
        await page.Keyboard.PressAsync("End");

        // "Line Numbers" is checked: its aria-hidden ✓ is not part of the text typeahead matches.
        await page.Keyboard.TypeAsync("l");
        await PollAsync(() => page.EvaluateAsync<string?>("() => document.querySelector('[role=menu]:not([hidden]) [data-active]')?.textContent.trim()"),
            text => text?.Contains("Line Numbers") == true, "typeahead on \"Line Numbers\"");
    }

    [TestMethod]
    public async Task A_submenu_with_no_room_on_either_side_stays_on_screen()
    {
        var page = await OpenDemoAsync("/menubar");
        await page.SetViewportSizeAsync(390, 800);
        var sample = Sample(page, "Nested submenus");
        await sample.ScrollIntoViewIfNeededAsync();
        await sample.Locator("[role=menubar] [role=menuitem]", new LocatorLocatorOptions { HasText = "Insert" }).ClickAsync();
        await page.Locator("[role=menu]:visible [role=menuitem]", new PageLocatorOptions { HasText = "Shape" }).First.ClickAsync();
        await Expect(page.Locator("[role=menu]:visible")).ToHaveCountAsync(2);

        await PollAsync(() => page.EvaluateAsync<bool>(
            """
            () => [...document.querySelectorAll('[role=menu]')]
              .filter(m => m.getClientRects().length && getComputedStyle(m).visibility !== 'hidden')
              .every(m => { const r = m.getBoundingClientRect(); return r.left >= 0 && r.right <= innerWidth; })
            """), true, "every open menu inside the 390px viewport");
    }

    [TestMethod]
    public async Task After_a_re_sort_the_grid_keyboard_follows_the_new_row_order()
    {
        var page = await OpenDemoAsync("/grid");
        var sample = Sample(page, "Sortable data grid");
        await sample.Locator("[role=columnheader]", new LocatorLocatorOptions { HasText = "Moons" }).ClickAsync();
        await Expect(sample.Locator("[role=columnheader]", new LocatorLocatorOptions { HasText = "Moons" })).ToHaveAttributeAsync("aria-sort", new Regex("ascending|descending"));

        // @key moves the rows without re-registering them: the grid reads their order again after the render.
        var visual = await sample.EvaluateAsync<string[]>(
            "s => [...s.querySelectorAll('tbody [role=row]')].map(r => r.querySelector('[role=rowheader],[role=gridcell]').textContent.trim())");
        var walked = new List<string>();
        foreach (var _ in visual)
        {
            await page.Keyboard.PressAsync("ArrowDown");
            walked.Add(await page.EvaluateAsync<string>("() => document.activeElement.closest('[role=row]').querySelector('[role=rowheader],[role=gridcell]').textContent.trim()"));
        }
        Assert.AreSequenceEqual(visual, walked.ToArray());
    }

    [TestMethod]
    public async Task A_press_on_a_range_slider_track_focuses_the_thumb_that_moved()
    {
        var page = await OpenDemoAsync("/slider");
        var sample = Sample(page, "Range with two thumbs");
        var root = sample.Locator("[role=slider]").First.Locator("xpath=..");
        await root.ScrollIntoViewIfNeededAsync();
        var box = (await root.BoundingBoxAsync())!;
        await page.Mouse.ClickAsync(box.X + box.Width * 0.75f, box.Y + box.Height / 2);

        await PollAsync(() => page.EvaluateAsync<string?>("() => document.activeElement.getAttribute('aria-label')"), "Maximum price", "the focused thumb");
    }

    [TestMethod]
    public async Task A_dialog_gives_the_focus_back_to_a_button_its_press_did_not_focus()
    {
        var page = await OpenDemoAsync("/dialog");
        var trigger = Sample(page, "Basic Dialog").Locator("button").First;
        await PressLikeSafariAsync(trigger, focusAncestor: true);
        await trigger.ClickAsync();
        await Expect(page.Locator("[role=dialog]").First).ToBeVisibleAsync();
        await PollAsync(() => page.EvaluateAsync<bool>("() => !!document.activeElement.closest('[role=dialog]')"), true, "the focus inside the dialog");

        // The focus was on the Preview panel when the dialog opened; the press says where the user was.
        await page.Keyboard.PressAsync("Escape");
        await Expect(page.Locator("[role=dialog]:visible")).ToHaveCountAsync(0);
        await Expect(trigger).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task A_second_press_on_a_popover_trigger_closes_it_when_the_press_focuses_an_ancestor()
    {
        var page = await OpenDemoAsync("/popover");
        var trigger = Sample(page, "Basic Popover").Locator("[aria-expanded]").First;
        await PressLikeSafariAsync(trigger, focusAncestor: true);
        await trigger.ClickAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");

        // The focus leaving for the trigger's ancestor is the press's, on the trigger: no dismissal before the
        // click toggles (which used to close the popover and open it again).
        await trigger.ClickAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }
}
