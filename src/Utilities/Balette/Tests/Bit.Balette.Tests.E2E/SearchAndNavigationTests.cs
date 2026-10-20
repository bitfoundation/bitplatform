using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// Typing and moving through the results in a real browser: what the list shows for a query, how the highlighted
/// characters are laid out, what assistive technology is told, and which keys the browser layer claims before they
/// can do their default thing - leave the dialog, delete text, scroll the page.
/// </summary>
[TestClass]
public sealed class SearchAndNavigationTests : BaletteTest
{
    [TestMethod]
    public async Task Typing_filters_the_list_and_ranks_the_best_match_first()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        int all = await Items(page).CountAsync();

        await TypeAsync(page, "gs");
        await ExpectFirstItemAsync(page, "navigation.settings");
        await ExpectActiveAsync(page, "navigation.settings");
        int filtered = await Items(page).CountAsync();
        Assert.IsTrue(filtered < all, $"typing did not filter the list ({filtered} of {all} rows left)");

        // The rows follow every keystroke, the ones that narrow and the ones that widen.
        await page.Keyboard.PressAsync("Backspace");
        await page.Keyboard.PressAsync("Backspace");
        await TypeAsync(page, "theme");
        await ExpectFirstItemAsync(page, "preferences.theme");
    }

    [TestMethod]
    public async Task Highlighted_characters_are_the_matched_ones_and_leave_no_gaps_in_the_title()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        // "rc": R and C at the two word starts of "Registering commands", which splits the title into four segments:
        // a mark, plain text, a mark, plain text.
        await TypeAsync(page, "rc");
        await ExpectFirstItemAsync(page, "navigation.registering-commands");
        var label = Items(page).First.Locator("[data-balette='label']");
        await Assertions.Expect(label).ToHaveTextAsync("Registering commands");
        await Assertions.Expect(label.Locator("[data-balette='mark']")).ToHaveTextAsync(["R", "c"]);

        // The title's text and its marks share one inline box. As separate flex items of the title they would each be
        // laid out apart, the title's gap between every one of them, so a highlighted word reads as "R  egistering".
        // Each segment must start where the one before it ended (a mark's 1px padding is cancelled by its -1px margin).
        var segments = await page.EvaluateAsync<double[][]>(
            """
            () => {
              const label = document.querySelector('[data-balette="item"] [data-balette="label"]');
              // Blazor leaves comment markers between the segments; only text and elements are laid out.
              return [...label.childNodes]
                .filter(n => (n.nodeType === Node.TEXT_NODE || n.nodeType === Node.ELEMENT_NODE) && n.textContent.length > 0)
                .map(n => {
                let r;
                if (n.nodeType === Node.TEXT_NODE) { const range = document.createRange(); range.selectNodeContents(n); r = range.getBoundingClientRect(); }
                else r = n.getBoundingClientRect();
                return [r.left, r.right, r.top + r.height / 2];
              });
            }
            """);
        TestContext.WriteLine("segments (left, right, middle): " + string.Join(" | ", segments.Select(s => string.Join(", ", s.Select(v => v.ToString("0.#"))))));
        Assert.AreEqual(4, segments.Length, "the title is not split into mark / text / mark / text");
        for (int i = 1; i < segments.Length; i++)
        {
            AssertInRange(segments[i][0] - segments[i - 1][1], -2.5, 1.5, $"the gap between segment {i} and segment {i + 1}");
            AssertInRange(segments[i][2] - segments[i - 1][2], -1, 1, $"the vertical offset of segment {i + 1}");
        }
    }

    [TestMethod]
    public async Task The_live_region_announces_how_many_results_there_are()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        await TypeAsync(page, "theming");
        await ExpectFirstItemAsync(page, "navigation.theming-and-templates");
        // The documentation source answers 300 ms after the debounce; once its row is in and the spinner is gone the
        // count no longer changes under the test.
        await Assertions.Expect(Item(page, "doc:/theming")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("[data-balette='spinner']")).ToHaveCountAsync(0);
        int count = await Items(page).CountAsync();
        await Assertions.Expect(LiveRegion(page)).ToHaveTextAsync(count == 1 ? "1 result available" : $"{count} results available");

        // Nothing matches, but the feedback row is always listed: one result.
        await Input(page).FillAsync("");
        await TypeAsync(page, "zqxjv");
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await Assertions.Expect(LiveRegion(page)).ToHaveTextAsync("1 result available");
    }

    [TestMethod]
    public async Task Arrows_Ctrl_N_Ctrl_P_Home_and_End_move_the_active_row_and_aria_activedescendant_follows()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        var ids = await ItemIdsAsync(page);
        Assert.IsTrue(ids.Length > 10, $"the empty query lists only {ids.Length} rows");

        await ExpectActiveAsync(page, ids[0]);
        await page.Keyboard.PressAsync("ArrowDown");
        await ExpectActiveAsync(page, ids[1]);
        await page.Keyboard.PressAsync("Control+n");
        await ExpectActiveAsync(page, ids[2]);
        await page.Keyboard.PressAsync("Control+p");
        await ExpectActiveAsync(page, ids[1]);
        await page.Keyboard.PressAsync("ArrowUp");
        await ExpectActiveAsync(page, ids[0]);

        // Loop is on: past the first row comes the last.
        await page.Keyboard.PressAsync("ArrowUp");
        await ExpectActiveAsync(page, ids[^1]);
        await page.Keyboard.PressAsync("Home");
        await ExpectActiveAsync(page, ids[0]);
        await page.Keyboard.PressAsync("End");
        await ExpectActiveAsync(page, ids[^1]);

        // None of those keys typed anything or moved the caret's text.
        await Assertions.Expect(Input(page)).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task The_active_row_is_scrolled_into_view_in_a_long_list()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        var ids = await ItemIdsAsync(page);
        Assert.IsTrue(await page.EvaluateAsync<bool>("() => { const l = document.querySelector('[data-balette=\"list\"]'); return l.scrollHeight > l.clientHeight + 100; }"),
            "the empty query does not overflow the list, so there is nothing to scroll");

        await page.Keyboard.PressAsync("End");
        await ExpectActiveAsync(page, ids[^1]);
        await WaitForActiveRowInViewAsync(page);
        Assert.IsTrue(await page.EvaluateAsync<double>("() => document.querySelector('[data-balette=\"list\"]').scrollTop") > 0, "the list did not scroll to the last row");

        await page.Keyboard.PressAsync("Home");
        await ExpectActiveAsync(page, ids[0]);
        await WaitForActiveRowInViewAsync(page);

        // Walking down one row at a time keeps every row in view on the way.
        for (int i = 1; i < Math.Min(ids.Length, 16); i++)
        {
            await page.Keyboard.PressAsync("ArrowDown");
            await ExpectActiveAsync(page, ids[i]);
            await WaitForActiveRowInViewAsync(page);
        }
    }

    [TestMethod]
    public async Task Tab_stays_in_the_dialog_and_opens_the_action_panel()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "nuget");
        await ExpectFirstItemAsync(page, "navigation.nuget-package");

        await page.Keyboard.PressAsync("Tab");

        // The browser's own Tab would have moved the focus to the close button or out of the dialog.
        Assert.AreEqual("balette:input", await FocusedAsync(page));
        await Assertions.Expect(Crumbs(page)).ToHaveTextAsync(["NuGet package"]);
        await Assertions.Expect(Input(page)).ToHaveAttributeAsync("placeholder", "Actions");
        await Assertions.Expect(Item(page, "__pin:navigation.nuget-package")).ToBeVisibleAsync();

        // Esc steps back to the results, with the query that was typed.
        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(Crumbs(page)).ToHaveCountAsync(0);
        await Assertions.Expect(Input(page)).ToHaveValueAsync("nuget");
        await Assertions.Expect(Dialog(page)).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Backspace_on_an_empty_query_goes_back_without_eating_the_query_it_restores()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "theme");
        await ExpectFirstItemAsync(page, "preferences.theme");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Crumbs(page)).ToHaveTextAsync(["Theme"]);
        await Assertions.Expect(Item(page, "theme:dark")).ToBeVisibleAsync();

        // With text in the box Backspace is just Backspace: it deletes a character and stays on the page.
        await TypeAsync(page, "da");
        await page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(Input(page)).ToHaveValueAsync("d");
        await Assertions.Expect(Crumbs(page)).ToHaveTextAsync(["Theme"]);
        await page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(Input(page)).ToHaveValueAsync("");
        await Assertions.Expect(Crumbs(page)).ToHaveTextAsync(["Theme"]);

        // On the empty box it goes back, and the query typed before the page opened comes back whole. Had the browser
        // also run the key's default action, it would have deleted the last character of that restored query.
        await page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(Crumbs(page)).ToHaveCountAsync(0);
        await Assertions.Expect(Input(page)).ToHaveValueAsync("theme");
        await ExpectFirstItemAsync(page, "preferences.theme");
    }

    /// <summary>Waits until the active row lies inside the list's visible box (the group heading may stick over its top).</summary>
    private static Task WaitForActiveRowInViewAsync(IPage page) => page.WaitForFunctionAsync(
        """
        () => {
          const list = document.querySelector('[data-balette="list"]');
          const row = list?.querySelector('[data-balette="item"][data-active="true"]');
          if (!row) return false;
          const l = list.getBoundingClientRect(), r = row.getBoundingClientRect();
          return r.top >= l.top - 1 && r.bottom <= l.bottom + 1;
        }
        """, null, new PageWaitForFunctionOptions { Timeout = 5_000 });
}
