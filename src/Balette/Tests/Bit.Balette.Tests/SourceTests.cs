using Microsoft.Extensions.DependencyInjection;

namespace Bit.Balette.Tests;

[TestClass]
public class SourceTests
{
    private static BaletteSourceContext NewContext()
        => new(null, null, new ServiceCollection().AddBitBaletteServices().BuildServiceProvider());

    [TestMethod]
    public async Task Static_source_finds_items_past_its_result_limit()
    {
        // The palette keeps only MaxResults of what a source returns, so the source has to match before it trims:
        // an item deep in the list is still found.
        var items = Enumerable.Range(0, 200).Select(i => (BaletteCommand)BaletteCmd.New($"Item {i}").Id($"item:{i}")).ToList();
        items.Insert(150, BaletteCmd.New("Needle in the haystack").Id("needle"));
        var source = new BaletteStaticSource("Static", items, maxResults: 10);

        var results = await source.SearchAsync("needle", NewContext(), CancellationToken.None);

        Assert.AreEqual("needle", Assert.ContainsSingle(results).Id);
    }

    [TestMethod]
    public async Task Static_source_returns_the_best_matches_first_and_caps_them()
    {
        var source = new BaletteStaticSource("Static",
        [
            BaletteCmd.New("Open recent file").Id("recent"),
            BaletteCmd.New("Open file").Id("open"),
            BaletteCmd.New("Close window").Id("close"),
            BaletteCmd.New("Open folder").Id("folder"),
        ], maxResults: 2);

        var results = await source.SearchAsync("open file", NewContext(), CancellationToken.None);

        Assert.HasCount(2, results);
        Assert.AreEqual("open", results[0].Id);
        Assert.IsFalse(results.Any(r => r.Id == "close"));
    }

    [TestMethod]
    public async Task Delegate_source_passes_the_query_through()
    {
        string? seen = null;
        var source = new BaletteDelegateSource("Remote", (query, _) =>
        {
            seen = query;
            return Task.FromResult<IReadOnlyList<BaletteCommand>>([BaletteCmd.New($"Hit for {query}").Build()]);
        });

        var results = await source.SearchAsync("abc", NewContext(), CancellationToken.None);

        Assert.AreEqual("abc", seen);
        Assert.AreEqual("Hit for abc", Assert.ContainsSingle(results).Title);
    }
}
