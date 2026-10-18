using System.Diagnostics;
using System.Text.RegularExpressions;
using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// Typed prefixes and the asynchronous sources behind them, against the browser's clock: the mode a prefix selects,
/// the caret a mode button leaves after its prefix, and results that arrive only once the debounce and the source's
/// latency have passed - with no network involved, so the timing is the palette's own.
/// </summary>
[TestClass]
public sealed class ModesAndSourcesTests : BaletteTest
{
    [TestMethod]
    public async Task A_prefix_selects_its_mode_and_shows_the_mode_chip()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        await TypeAsync(page, ">");
        await Assertions.Expect(ModeChip(page)).ToHaveTextAsync("Commands");
        await Assertions.Expect(Input(page)).ToHaveAttributeAsync("placeholder", "Run a command…");

        // The Commands mode lists only commands tagged "command": the actions, but none of the navigation.
        await Assertions.Expect(Item(page, "actions.rename-project")).ToBeVisibleAsync();
        await Assertions.Expect(Item(page, "navigation.settings")).ToHaveCountAsync(0);

        // Deleting the prefix leaves the mode.
        await page.Keyboard.PressAsync("Backspace");
        await Assertions.Expect(ModeChip(page)).ToHaveCountAsync(0);
        await Assertions.Expect(Item(page, "navigation.settings")).ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow("Commands mode", ">", "rename", "actions.rename-project")]
    [DataRow("Issues mode", "#", "105", "issue:105")]
    [DataRow("People mode", "@", "ada", "person:ada@example.com")]
    public async Task A_mode_button_opens_the_palette_with_its_prefix_kept_for_typing_after(string button, string prefix, string typed, string expected)
    {
        var page = await OpenAsync("/");

        await page.Locator($".topbar-modes button[title='{button}']").ClickAsync();
        await ExpectOpenAsync(page);
        await Assertions.Expect(Input(page)).ToHaveValueAsync(prefix);

        // The caret sits after the prefix with nothing selected. A selected prefix - the way a query left over from the
        // last time is presented - would be replaced by the first key typed, and the mode with it.
        Assert.AreEqual("1,1", await page.EvaluateAsync<string>("() => { const i = document.querySelector('[data-balette=\"input\"]'); return i.selectionStart + ',' + i.selectionEnd; }"));

        await TypeAsync(page, typed);
        await Assertions.Expect(Input(page)).ToHaveValueAsync(prefix + typed);
        await Assertions.Expect(ModeChip(page)).ToBeVisibleAsync();
        await ExpectFirstItemAsync(page, expected);
    }

    [TestMethod]
    public async Task An_issue_number_finds_that_issue()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        await TypeAsync(page, "#105");

        await Assertions.Expect(ModeChip(page)).ToHaveTextAsync("Issues");
        await Assertions.Expect(Item(page, "issue:105")).ToBeVisibleAsync();
        await Assertions.Expect(Items(page)).ToHaveCountAsync(1);
        await Assertions.Expect(GroupHeadings(page)).ToHaveTextAsync(["Issues"]);
        await Assertions.Expect(Items(page).First).ToContainTextAsync("#105 Nested pages lose query on back");
    }

    [TestMethod]
    public async Task People_answer_after_the_debounce_and_the_detail_pane_shows_the_person()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        await TypeAsync(page, "@ada");
        var sinceLastKey = Stopwatch.StartNew();

        // The palette asks the source 150 ms after the last keystroke, and the source takes 250 ms to answer: right
        // after typing there is nothing yet, and the row arrives no sooner than that.
        Assert.AreEqual(0, await Item(page, "person:ada@example.com").CountAsync(), "the people source answered before its debounce had passed");
        await Assertions.Expect(Item(page, "person:ada@example.com")).ToBeVisibleAsync();
        sinceLastKey.Stop();
        TestContext.WriteLine($"the people row arrived {sinceLastKey.ElapsedMilliseconds} ms after the last key");
        Assert.IsTrue(sinceLastKey.ElapsedMilliseconds >= 300, $"the people row arrived {sinceLastKey.ElapsedMilliseconds} ms after the last key, sooner than the debounce and the latency allow");

        await ExpectActiveAsync(page, "person:ada@example.com");
        var detail = Detail(page);
        await Assertions.Expect(detail).ToContainTextAsync("Ada Lovelace");
        await Assertions.Expect(detail).ToContainTextAsync("Engineering");
        await Assertions.Expect(detail).ToContainTextAsync("ada@example.com");
    }

    [TestMethod]
    public async Task Keystrokes_typed_in_a_burst_reach_the_source_far_fewer_times_than_they_were_typed()
    {
        var page = await OpenAsync("/modes");
        await OpenPaletteAsync(page);

        const string query = "ada lovelace";
        await TypeAsync(page, "@" + query);
        await Assertions.Expect(Item(page, "person:ada@example.com")).ToBeVisibleAsync();

        // The modes page logs every call that reached a source. Most keystrokes of a burst are swallowed by the
        // debounce, and the call for the whole query is the one that completed.
        var log = page.Locator("main .panel").Filter(new() { Has = page.GetByRole(AriaRole.Button, new() { Name = "Clear", Exact = true }) });
        var calls = log.Locator("tbody tr").Filter(new() { Has = page.Locator("td:nth-child(2)", new() { HasTextRegex = new Regex("^People$") }) });
        await Assertions.Expect(calls.First.Locator("code")).ToHaveTextAsync(query);
        await Assertions.Expect(calls.First).ToContainTextAsync("Completed");
        int count = await calls.CountAsync();
        TestContext.WriteLine($"{query.Length} keystrokes reached the people source {count} time(s)");
        Assert.IsTrue(count < query.Length / 2, $"{query.Length} keystrokes reached the source {count} times");
    }

    [TestMethod]
    public async Task The_documentation_source_answers_next_to_the_registry_in_the_default_mode()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        await TypeAsync(page, "theming");

        // The registry's own row is there at once; the documentation hit joins it once the source has answered.
        await ExpectFirstItemAsync(page, "navigation.theming-and-templates");
        await Assertions.Expect(Item(page, "doc:/theming")).ToBeVisibleAsync();
        await Assertions.Expect(GroupHeadings(page).Filter(new() { HasTextString = "Documentation" })).ToHaveCountAsync(1);
        await Assertions.Expect(page.Locator("[data-balette='spinner']")).ToHaveCountAsync(0);

        // Re-ranked with the registry, the documentation's low priority keeps the command ahead of an equal hit.
        await ExpectFirstItemAsync(page, "navigation.theming-and-templates");
    }
}
