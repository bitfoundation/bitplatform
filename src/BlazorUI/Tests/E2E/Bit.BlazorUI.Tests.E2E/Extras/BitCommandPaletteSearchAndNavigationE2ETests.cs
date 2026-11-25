namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Typing and moving through BitCommandPalette's results in a real browser: what the list shows for a query, how the
/// highlighted characters are laid out, what assistive technology is told, and which keys the browser layer claims
/// before they can do their default thing - leave the dialog, delete text, scroll the page.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteSearchAndNavigationE2ETests : BitCommandPaletteE2ETestBase
{
    [TestMethod]
    public async Task KeysTypedFasterThanThePaletteRenders_AreAllKept()
    {
        // On Blazor Server each key's render arrives a round trip after the key; one answering an earlier key must never
        // write its older value over the keys typed since.
        await OpenPageAsync();
        await OpenPaletteAsync();

        const string Typed = "keyboard and shortcuts";
        await TypeAsync(Typed);

        await Expect(Input).ToHaveValueAsync(Typed);
        await ExpectFirstItemAsync("navigation.keyboard-and-shortcuts");
    }

    [TestMethod]
    public async Task Typing_FiltersTheList_AndRanksTheBestMatchFirst()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await Expect(Items.First).ToBeVisibleAsync();
        var all = await Items.CountAsync();

        await TypeAsync("gs");
        await ExpectFirstItemAsync("navigation.settings");
        await ExpectActiveAsync("navigation.settings");
        var filtered = await Items.CountAsync();
        Assert.IsTrue(filtered < all, $"typing did not filter the list ({filtered} of {all} rows left)");

        // The rows follow every keystroke, the ones that narrow and the ones that widen.
        await Page.Keyboard.PressAsync("Backspace");
        await Page.Keyboard.PressAsync("Backspace");
        await TypeAsync("theme");
        await ExpectFirstItemAsync("preferences.theme");
    }

    [TestMethod]
    public async Task HighlightedCharacters_AreTheMatchedOnes_AndLeaveNoGapsInTheTitle()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        // "rc": R and C at the two word starts of "Registering commands", which splits the title into four segments: a
        // mark, plain text, a mark, plain text.
        await TypeAsync("rc");
        await ExpectFirstItemAsync("navigation.registering-commands");
        var label = Items.First.Locator("[data-bit-cpl='label']");
        await Expect(label).ToHaveTextAsync("Registering commands");
        await Expect(label.Locator("[data-bit-cpl='mark']")).ToHaveTextAsync(["R", "c"]);

        // The title's text and its marks share one inline box. As separate flex items of the title they would each be laid
        // out apart, so a highlighted word would read "R  egistering": each segment starts where the one before it ended.
        var segments = await label.EvaluateAsync<double[][]>(
            """
            label => {
              // Every text node and mark under the label, in order; Blazor leaves comment markers between them.
              const parts = [];
              const walk = node => {
                for (const n of node.childNodes) {
                  if (n.nodeType === Node.TEXT_NODE && n.textContent.length > 0) parts.push(n);
                  else if (n.nodeType === Node.ELEMENT_NODE) { if (n.matches('[data-bit-cpl="mark"]')) parts.push(n); else walk(n); }
                }
              };
              walk(label);
              return parts.map(n => {
                let r;
                if (n.nodeType === Node.TEXT_NODE) { const range = document.createRange(); range.selectNodeContents(n); r = range.getBoundingClientRect(); }
                else r = n.getBoundingClientRect();
                return [r.left, r.right, r.top + r.height / 2];
              });
            }
            """);
        TestContext.WriteLine("segments (left, right, middle): " + string.Join(" | ", segments.Select(s => string.Join(", ", s.Select(v => v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture))))));
        Assert.AreEqual(4, segments.Length, "the title is not split into mark / text / mark / text");
        for (var i = 1; i < segments.Length; i++)
        {
            AssertInRange(segments[i][0] - segments[i - 1][1], -2.5, 1.5, $"the gap between segment {i} and segment {i + 1}");
            AssertInRange(segments[i][2] - segments[i - 1][2], -1, 1, $"the vertical offset of segment {i + 1}");
        }
    }

    [TestMethod]
    public async Task TheLiveRegion_AnnouncesHowManyResultsThereAre()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        await TypeAsync("theming");
        await ExpectFirstItemAsync("navigation.theming-and-templates");
        // The documentation source answers 300 ms after the debounce; once its row is in and the spinner is gone the
        // count no longer changes under the test.
        await Expect(Item("doc:/theming")).ToBeVisibleAsync();
        await Expect(Page.Locator("[data-bit-cpl='spinner']")).ToHaveCountAsync(0);
        var count = await Items.CountAsync();
        await Expect(LiveRegion).ToHaveTextAsync(count == 1 ? "1 result available" : $"{count} results available");

        // Nothing matches, but the feedback row is always listed: one result.
        await ReplaceQueryAsync("theming", "zqxjv");
        await Expect(Items).ToHaveCountAsync(1);
        await Expect(LiveRegion).ToHaveTextAsync("1 result available");
    }

    [TestMethod]
    public async Task ArrowsCtrlNCtrlPHomeAndEnd_MoveTheActiveRow_AndAriaActiveDescendantFollows()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await Expect(Items.First).ToBeVisibleAsync();
        var ids = await ItemIdsAsync();
        Assert.IsTrue(ids.Length > 10, $"the empty query lists only {ids.Length} rows");

        await ExpectActiveAsync(ids[0]);
        await Page.Keyboard.PressAsync("ArrowDown");
        await ExpectActiveAsync(ids[1]);
        await Page.Keyboard.PressAsync("Control+n");
        await ExpectActiveAsync(ids[2]);
        await Page.Keyboard.PressAsync("Control+p");
        await ExpectActiveAsync(ids[1]);
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectActiveAsync(ids[0]);

        // Loop is on: past the first row comes the last.
        await Page.Keyboard.PressAsync("ArrowUp");
        await ExpectActiveAsync(ids[^1]);
        await Page.Keyboard.PressAsync("Home");
        await ExpectActiveAsync(ids[0]);
        await Page.Keyboard.PressAsync("End");
        await ExpectActiveAsync(ids[^1]);

        // None of those keys typed anything.
        await Expect(Input).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task TheActiveRow_IsScrolledIntoView_InALongList()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await Expect(Items.First).ToBeVisibleAsync();
        var ids = await ItemIdsAsync();
        Assert.IsTrue(await List.EvaluateAsync<bool>("l => l.scrollHeight > l.clientHeight + 100"),
            "the empty query does not overflow the list, so there is nothing to scroll");

        await Page.Keyboard.PressAsync("End");
        await ExpectActiveAsync(ids[^1]);
        await WaitForActiveRowInViewAsync();
        Assert.IsTrue(await List.EvaluateAsync<double>("l => l.scrollTop") > 0, "the list did not scroll to the last row");

        await Page.Keyboard.PressAsync("Home");
        await ExpectActiveAsync(ids[0]);
        await WaitForActiveRowInViewAsync();

        // Walking down one row at a time keeps every row in view on the way.
        for (var i = 1; i < Math.Min(ids.Length, 16); i++)
        {
            await Page.Keyboard.PressAsync("ArrowDown");
            await ExpectActiveAsync(ids[i]);
            await WaitForActiveRowInViewAsync();
        }
    }

    [TestMethod]
    public async Task Tab_StaysInTheDialog_AndOpensTheActionPanel()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("nuget");
        await ExpectFirstItemAsync("navigation.nuget-package");

        await Page.Keyboard.PressAsync("Tab");

        // The browser's own Tab would have moved the focus to the close button or out of the dialog.
        await Expect(Crumbs).ToHaveTextAsync(["NuGet package"]);
        Assert.AreEqual("cpl:input", await FocusedAsync());
        await Expect(Input).ToHaveAttributeAsync("placeholder", "Actions");
        await Expect(Item("__pin:navigation.nuget-package")).ToBeVisibleAsync();

        // Esc steps back to the results, with the query that was typed.
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Crumbs).ToHaveCountAsync(0);
        await Expect(Input).ToHaveValueAsync("nuget");
        await Expect(Dialog).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task BackspaceOnAnEmptyQuery_GoesBack_WithoutEatingTheQueryItRestores()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("theme");
        await ExpectFirstItemAsync("preferences.theme");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Crumbs).ToHaveTextAsync(["Theme"]);
        await Expect(Item("theme:dark")).ToBeVisibleAsync();

        // With text in the box Backspace is just Backspace: it deletes a character and stays on the page.
        await TypeAsync("da");
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Input).ToHaveValueAsync("d");
        await Expect(Crumbs).ToHaveTextAsync(["Theme"]);
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Input).ToHaveValueAsync("");
        await Expect(Crumbs).ToHaveTextAsync(["Theme"]);

        // On the empty box it goes back, and the query typed before the page opened comes back whole. Had the browser also
        // run the key's default action, it would have deleted the last character of that restored query.
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(Crumbs).ToHaveCountAsync(0);
        await Expect(Input).ToHaveValueAsync("theme");
        await ExpectFirstItemAsync("preferences.theme");
    }

    /// <summary>Waits until the active row lies inside the list's visible box (the group heading may stick over its top).</summary>
    private async Task WaitForActiveRowInViewAsync()
    {
        try
        {
            await Page.WaitForFunctionAsync(
                """
                () => {
                  const list = document.querySelector('dialog[data-bit-cpl="dialog"] [data-bit-cpl="list"]');
                  const row = list?.querySelector('[data-bit-cpl="item"][data-active="true"]');
                  if (!row) return false;
                  const l = list.getBoundingClientRect(), r = row.getBoundingClientRect();
                  return r.top >= l.top - 1 && r.bottom <= l.bottom + 1;
                }
                """, null, new() { Timeout = 5_000 });
        }
        catch (TimeoutException)
        {
            var where = await Page.EvaluateAsync<string>(
                """
                () => {
                  const list = document.querySelector('dialog[data-bit-cpl="dialog"] [data-bit-cpl="list"]');
                  const row = list?.querySelector('[data-bit-cpl="item"][data-active="true"]');
                  if (!row) return 'no active row';
                  const l = list.getBoundingClientRect(), r = row.getBoundingClientRect();
                  return `list ${l.top}..${l.bottom} scrollTop ${list.scrollTop}/${list.scrollHeight - list.clientHeight}, row ${row.dataset.value} ${r.top}..${r.bottom}`;
                }
                """);
            throw new TimeoutException("The active row did not come into view: " + where);
        }
    }
}
