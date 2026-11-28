using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// BitCommandPalette's typed prefixes and the asynchronous sources behind them, against the browser's clock: the mode a
/// prefix selects, the caret a trigger leaves after its prefix, results that arrive only once the debounce and the
/// source's latency have passed, and a source that fails.
/// </summary>
[TestClass]
[E2ERetry]
public class BitCommandPaletteModesAndSourcesE2ETests : BitCommandPaletteE2ETestBase
{
    [TestMethod]
    public async Task APrefix_SelectsItsMode_AndShowsTheModeChip()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        await TypeAsync(">");
        await Expect(ModeChip).ToHaveTextAsync("Commands");
        await Expect(Input).ToHaveAttributeAsync("placeholder", "Run a command…");

        // The Commands mode lists only commands tagged "command": the actions, but none of the navigation.
        await Expect(Item("actions.rename-project")).ToBeVisibleAsync();
        await Expect(Item("navigation.settings")).ToHaveCountAsync(0);

        // Deleting the prefix leaves the mode.
        await Page.Keyboard.PressAsync("Backspace");
        await Expect(ModeChip).ToHaveCountAsync(0);
        await Expect(Item("navigation.settings")).ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow("cpl-mode-commands", ">", "rename", "actions.rename-project")]
    [DataRow("cpl-mode-issues", "#", "105", "issue:105")]
    [DataRow("cpl-mode-people", "@", "ada", "person:ada@example.com")]
    public async Task AModeTrigger_OpensThePaletteWithItsPrefix_KeptForTypingAfter(string trigger, string prefix, string typed, string expected)
    {
        await OpenPageAsync();

        await Page.Locator($"#{trigger}").ClickAsync();
        await ExpectOpenAsync();
        await Expect(Input).ToHaveValueAsync(prefix);

        // The caret sits after the prefix with nothing selected. A selected prefix - the way a query left over from the last
        // time is presented - would be replaced by the first key typed, and the mode with it.
        await Page.WaitForFunctionAsync("() => { const i = document.querySelector('dialog [data-bit-cpl=\"input\"]'); return i.selectionStart === 1 && i.selectionEnd === 1; }",
            null, new() { Timeout = 5_000 });

        await TypeAsync(typed);
        await Expect(Input).ToHaveValueAsync(prefix + typed);
        await Expect(ModeChip).ToBeVisibleAsync();
        await ExpectFirstItemAsync(expected);
    }

    [TestMethod]
    public async Task AnIssueNumber_FindsThatIssue()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        await TypeAsync("#105");

        await Expect(ModeChip).ToHaveTextAsync("Issues");
        await Expect(Item("issue:105")).ToBeVisibleAsync();
        await Expect(Items).ToHaveCountAsync(1);
        await Expect(GroupHeadings).ToHaveTextAsync(["Issues"]);
        await Expect(Items.First).ToContainTextAsync("#105 Nested pages lose query on back");
    }

    [TestMethod]
    public async Task People_AnswerAfterTheDebounce_AndTheDetailPaneShowsThePerson()
    {
        await OpenPageAsync();
        await Page.Locator("#cpl-detail").CheckAsync();
        await OpenPaletteAsync();

        await TypeAsync("@ada");
        var sinceLastKey = Stopwatch.StartNew();

        // The palette asks the source 150 ms after the last keystroke, and the source takes 250 ms to answer: right after
        // typing there is nothing yet, and the row arrives no sooner than that.
        Assert.AreEqual(0, await Item("person:ada@example.com").CountAsync(), "the people source answered before its debounce had passed");
        await Expect(Item("person:ada@example.com")).ToBeVisibleAsync();
        sinceLastKey.Stop();
        TestContext.WriteLine($"the people row arrived {sinceLastKey.ElapsedMilliseconds} ms after the last key");
        Assert.IsTrue(sinceLastKey.ElapsedMilliseconds >= 300, $"the people row arrived {sinceLastKey.ElapsedMilliseconds} ms after the last key, sooner than the debounce and the latency allow");

        await ExpectActiveAsync("person:ada@example.com");
        await Expect(Detail).ToContainTextAsync("Ada Lovelace");
        await Expect(Detail).ToContainTextAsync("Engineering");
        await Expect(Detail).ToContainTextAsync("ada@example.com");
    }

    [TestMethod]
    public async Task KeystrokesTypedInABurst_ReachTheSourceFarFewerTimesThanTheyWereTyped()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        const string Query = "ada lovelace";
        await TypeAsync("@" + Query);
        await Expect(Item("person:ada@example.com")).ToBeVisibleAsync();

        // The page logs every call that reached the source. Most keystrokes of a burst are swallowed by the debounce, and
        // the call for the whole query is the one that completed.
        await Expect(Text("cpl-people-last")).ToHaveTextAsync($"{Query}: Completed");
        var calls = int.Parse(Regex.Match(await Text("cpl-people-calls").InnerTextAsync(), @"\d+").Value, System.Globalization.CultureInfo.InvariantCulture);
        TestContext.WriteLine($"{Query.Length} keystrokes reached the people source {calls} time(s)");
        Assert.IsTrue(calls < Query.Length / 2, $"{Query.Length} keystrokes reached the source {calls} times");
    }

    [TestMethod]
    public async Task TheDocumentationSource_AnswersNextToTheRegistry_InTheDefaultMode()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        await TypeAsync("theming");

        // The registry's own row is there at once; the documentation hit joins it once the source has answered.
        await ExpectFirstItemAsync("navigation.theming-and-templates");
        await Expect(Item("doc:/theming")).ToBeVisibleAsync();
        await Expect(GroupHeadings.Filter(new() { HasTextString = "Documentation" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("[data-bit-cpl='spinner']")).ToHaveCountAsync(0);

        // Re-ranked with the registry, the documentation's low priority keeps the command ahead of an equal hit.
        await ExpectFirstItemAsync("navigation.theming-and-templates");
    }

    [TestMethod]
    public async Task AFailingSource_LeavesARetryRowInPlaceOfItsResults_ThatAsksItAgain()
    {
        await OpenPageAsync();
        await OpenPaletteAsync();

        await TypeAsync("!x");

        var retry = Item("__source-error:Flaky");
        await Expect(retry).ToBeVisibleAsync();
        await Expect(retry).ToHaveAttributeAsync("data-error", "true");
        await Expect(retry).ToContainTextAsync("Flaky");
        await Expect(Text("cpl-flaky-calls")).ToHaveTextAsync("Flaky calls: 1");

        // Choosing the row asks the source again, and the palette stays open on it.
        await ExpectActiveAsync("__source-error:Flaky");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("cpl-flaky-calls")).ToHaveTextAsync("Flaky calls: 2");
        await Expect(retry).ToBeVisibleAsync();
        await Expect(Dialog).ToBeVisibleAsync();
    }
}
