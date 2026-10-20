using System.Globalization;
using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// The engine inspector: a burst of five hundred single-character insertions reaches the peer and is stored in a
/// handful of blocks on both replicas, the update log tells local edits from received ones, and both replicas project
/// the same document.
/// </summary>
[TestClass]
public sealed class InternalsTests : BcrdtTest
{
    // The order of the read-outs in each panel's statistics list.
    private const int Characters = 0;
    private const int Blocks = 1;

    [TestMethod]
    public async Task A_typed_burst_reaches_the_peer_in_a_handful_of_blocks_and_the_log_tells_local_from_received()
    {
        var page = await OpenReplicasAsync("/internals", 2);
        ILocator a = Replica(page, "Replica A"), b = Replica(page, "Replica B");

        await a.GetByRole(AriaRole.Button, new() { Name = "Type 500 characters" }).ClickAsync();

        await Assertions.Expect(Stat(a, Characters)).ToHaveTextAsync("500");
        await Assertions.Expect(Stat(b, Characters)).ToHaveTextAsync("500");
        await Assertions.Expect(Editor(b)).ToHaveValueAsync(new string('x', 500));

        // Five hundred insertions, one at a time, at the end: run-length encoding merges them back into a run on
        // both replicas, the one that typed them and the one that received them one update at a time.
        foreach (var panel in new[] { a, b })
        {
            var blocks = await StatAsync(panel, Blocks);
            Assert.IsTrue(blocks is > 0 and < 10, $"500 consecutive characters are held in {blocks} blocks");
        }

        // Every update is logged with where it came from: A produced them, B received them.
        var log = page.Locator("table.log-table tbody tr");
        await Assertions.Expect(log.Filter(new() { HasTextString = "received from a peer" }).First).ToContainTextAsync("Replica B");
        await Assertions.Expect(log.Filter(new() { HasTextString = "local edit" }).First).ToContainTextAsync("Replica A");

        // Typing into B's editor goes the other way, and is logged as B's first local edit. The editor makes its
        // edits untagged, the way the buttons do, so they are listed as plain local edits.
        await TypeAtEndAsync(Editor(b), "!");
        await Assertions.Expect(Stat(a, Characters)).ToHaveTextAsync("501");
        await Assertions.Expect(log.Filter(new() { HasTextString = "Replica B" }).Filter(new() { HasTextString = "local edit" })).ToHaveCountAsync(1);
        await Assertions.Expect(log.Filter(new() { HasTextString = "Replica A" }).First).ToContainTextAsync("received from a peer");

        // Scattered edits are the case run-length encoding cannot help: the block count grows with them.
        await b.GetByRole(AriaRole.Button, new() { Name = "Make 200 scattered edits" }).ClickAsync();
        await Assertions.Expect(Stat(a, Characters)).ToHaveTextAsync("701");
        await Assertions.Expect(Stat(b, Characters)).ToHaveTextAsync("701");
        var scattered = await StatAsync(a, Blocks);
        Assert.IsTrue(scattered > 100, $"200 scattered insertions left only {scattered} blocks");

        await ExpectSameProjectionsAsync(page);
    }

    /// <summary>One read-out of the panel's statistics list, by position.</summary>
    private static ILocator Stat(ILocator panel, int index) => panel.Locator("dl.stats dd").Nth(index);

    private static async Task<int> StatAsync(ILocator panel, int index) =>
        int.Parse(await Stat(panel, index).InnerTextAsync(), NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
}
