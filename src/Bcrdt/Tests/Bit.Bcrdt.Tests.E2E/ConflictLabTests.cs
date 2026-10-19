using System.Text.RegularExpressions;
using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// The conflict laboratory, run from its own buttons: every provoked conflict - seed, disconnect both replicas, edit
/// both, reconnect - ends with the two replicas identical, and the outcomes the page states before a scenario runs
/// are the ones the replicas show afterwards.
/// </summary>
[TestClass]
public sealed class ConflictLabTests : BcrdtTest
{
    private const string Identical = "The two replicas are identical.";

    [TestMethod]
    public async Task Every_scenario_ends_with_both_replicas_identical()
    {
        var page = await OpenReplicasAsync("/conflicts", 2);

        var scenarios = page.Locator("article.lab-case");
        await Assertions.Expect(scenarios).ToHaveCountAsync(6);

        for (var i = 0; i < 6; i++)
        {
            var title = await scenarios.Nth(i).Locator("h3").InnerTextAsync();
            await RunAsync(page, title, expectedRuns: i + 1);
        }

        await Assertions.Expect(LogEntries(page).Filter(new() { HasTextString = "still differ" })).ToHaveCountAsync(0);
        await ExpectPanelsAgreeAsync(page);
    }

    [TestMethod]
    public async Task The_stated_outcomes_are_what_the_replicas_show()
    {
        var page = await OpenReplicasAsync("/conflicts", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        // Concurrent insertion at the same index: both runs survive, each in one piece, in the same order on both.
        await RunAsync(page, "Concurrent insertion at the same position", expectedRuns: 1);
        var text = await Field(a, "Text").InnerTextAsync();
        StringAssert.Contains(text, "[from A]");
        StringAssert.Contains(text, "[from B]");
        StringAssert.Contains(text, "seed text.");
        await Assertions.Expect(Field(b, "Text")).ToHaveTextAsync(text);

        // The same map key written on both sides: one value wins, the same one on both replicas.
        await RunAsync(page, "Concurrent write to the same map key", expectedRuns: 2);
        var map = await Field(a, "Map").InnerTextAsync();
        Assert.AreNotEqual(map.Contains("written by A", StringComparison.Ordinal), map.Contains("written by B", StringComparison.Ordinal),
            $"exactly one of the two values must win: {map}");
        await Assertions.Expect(Field(b, "Map")).ToHaveTextAsync(map);

        // Different keys never conflict: both writes survive.
        await RunAsync(page, "Concurrent writes to different map keys", expectedRuns: 3);
        await Assertions.Expect(Field(a, "Map")).ToContainTextAsync("set by A");
        await Assertions.Expect(Field(a, "Map")).ToContainTextAsync("set by B");
        await ExpectPanelsAgreeAsync(page);

        // The reset clears every field on both replicas and empties the log.
        await page.GetByRole(AriaRole.Button, new() { Name = "Reset the document" }).ClickAsync();
        await Assertions.Expect(page.Locator(".lab-log")).ToHaveCountAsync(0);
        await Assertions.Expect(Field(a, "Text")).ToHaveTextAsync("(empty)");
        await Assertions.Expect(Field(b, "Text")).ToHaveTextAsync("(empty)");
        await ExpectPanelsAgreeAsync(page);
    }

    private static ILocator LogEntries(IPage page) => page.Locator(".lab-log li");

    /// <summary>
    /// Runs the scenario titled <paramref name="title"/> and waits until its last log line says the replicas are
    /// identical. The log is only cleared by a reset, so the <paramref name="expectedRuns"/>th "identical" line is
    /// the one this run wrote.
    /// </summary>
    private static async Task RunAsync(IPage page, string title, int expectedRuns)
    {
        var scenario = page.Locator("article.lab-case").Filter(new()
        {
            Has = page.Locator("h3", new() { HasTextRegex = new Regex($"^\\s*{Regex.Escape(title)}\\s*$") }),
        });
        await scenario.GetByRole(AriaRole.Button, new() { Name = "Run this scenario" }).ClickAsync();

        await Assertions.Expect(LogEntries(page).Filter(new() { HasTextString = Identical })).ToHaveCountAsync(expectedRuns, new() { Timeout = 30_000 });
        await Assertions.Expect(LogEntries(page).Last).ToHaveTextAsync(Identical);
        await Assertions.Expect(page.Locator(".verdict")).ToHaveClassAsync(new Regex("\\bverdict-ok\\b"));

        // The scenario reconnected both replicas, and its buttons are enabled again for the next one.
        await Assertions.Expect(page.Locator("section.panel .panel-head .badge")).ToHaveTextAsync(["synced", "synced"]);
        await Assertions.Expect(scenario.GetByRole(AriaRole.Button)).ToBeEnabledAsync();
    }

    /// <summary>One of the read-outs (Text, List, Map) a replica's panel shows its state in.</summary>
    private static ILocator Field(ILocator panel, string name) =>
        panel.Locator($"xpath=.//h4[normalize-space()='{name}']/following-sibling::pre[1]");

    /// <summary>The two panels show the same text, list and map, character for character.</summary>
    private static async Task ExpectPanelsAgreeAsync(IPage page)
    {
        IReadOnlyList<string> a = [], b = [];
        await EventuallyAsync(async () =>
        {
            a = await Replica(page, "Replica A").Locator("pre.json").AllInnerTextsAsync();
            b = await Replica(page, "Replica B").Locator("pre.json").AllInnerTextsAsync();
            return a.Count == 3 && a.SequenceEqual(b);
        }, () => $"both panels show the same state; A shows {Quote(a)} and B shows {Quote(b)}");
    }
}
