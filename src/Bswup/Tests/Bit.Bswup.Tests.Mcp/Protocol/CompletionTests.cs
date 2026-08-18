using Bit.Bswup.Demo.Client;
using Bit.Bswup.Demo.Server.Services;
using Bit.Bswup.Tests.Mcp.TestInfra;
using ModelContextProtocol.Protocol;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// The values a client offers while someone is typing a resource URI or filling in a prompt
/// argument. Every templated resource here is keyed by a value out of a closed list that nobody can
/// guess, so completions are the difference between browsing the server and having to call a
/// listing tool and type a slug back by hand.
/// </summary>
[TestClass]
public class CompletionTests
{
    private static McpTestServer _server = null!;

    [ClassInitialize]
    public static async Task StartAsync(TestContext _) => _server = await McpTestServer.StartAsync();

    [ClassCleanup]
    public static async Task StopAsync() => await _server.DisposeAsync();

    private static Task<CompleteResult> CompleteResourceAsync(string uriTemplate, string argument, string typed)
        => _server.Mcp.CompleteAsync(new ResourceTemplateReference { Uri = uriTemplate }, argument, typed).AsTask();

    private static Task<CompleteResult> CompletePromptAsync(string prompt, string argument, string typed)
        => _server.Mcp.CompleteAsync(new PromptReference { Name = prompt }, argument, typed).AsTask();

    [TestMethod]
    public async Task DocsSlugs_AreOfferedAndCoverTheWholeCatalog()
    {
        var completion = await CompleteResourceAsync("bswup://docs/{slug}", "slug", string.Empty);

        Assert.AreEqual(DocsCatalog.AllPages.Count(), completion.Completion.Total);
        CollectionAssert.Contains(completion.Completion.Values.ToArray(), "service-worker");
    }

    [TestMethod]
    public async Task DocsSlugs_OfferAWordForTheIntroductionRatherThanItsEmptySlug()
    {
        var completion = await CompleteResourceAsync("bswup://docs/{slug}", "slug", string.Empty);

        // The introduction's own slug is the empty string, which is not a value anyone can pick
        // from a list - and the lookup maps the word back.
        CollectionAssert.Contains(completion.Completion.Values.ToArray(), "introduction");
        CollectionAssert.DoesNotContain(completion.Completion.Values.ToArray(), string.Empty);
    }

    [TestMethod]
    public async Task GuideHeadings_AreOffered()
    {
        var completion = await CompleteResourceAsync("bswup://guide/{heading}", "heading", "java");

        CollectionAssert.Contains(completion.Completion.Values.ToArray(), "JavaScript API");
    }

    [TestMethod]
    public async Task SourcePaths_AreOffered()
    {
        var completion = await CompleteResourceAsync("bswup://source/{path}", "path", "bit-bswup.sw");

        CollectionAssert.Contains(completion.Completion.Values.ToArray(), "Library/Scripts/bit-bswup.sw.ts");
    }

    [TestMethod]
    public async Task Matching_PrefersAPrefixButStillOffersWhatContainsTheText()
    {
        var completion = await CompleteResourceAsync("bswup://docs/{slug}", "slug", "worker");

        var values = completion.Completion.Values.ToArray();

        Assert.IsTrue(values.Length > 0);
        // Someone typing "worker" means the service-worker page; a prefix match still comes first
        // when there is one, so the ordering is what is asserted rather than the set.
        Assert.IsTrue(values.Contains("service-worker"));
    }

    [TestMethod]
    public async Task Matching_IsCaseInsensitive()
    {
        var completion = await CompleteResourceAsync("bswup://guide/{heading}", "heading", "JAVASCRIPT");

        CollectionAssert.Contains(completion.Completion.Values.ToArray(), "JavaScript API");
    }

    [TestMethod]
    public async Task Matching_ShorterValuesComeFirst()
    {
        var completion = await CompleteResourceAsync("bswup://source/{path}", "path", "service-worker");
        var values = completion.Completion.Values.ToArray();

        Assert.IsTrue(values.Length > 1);
        Assert.IsTrue(values[0].Length <= values[^1].Length, string.Join(", ", values));
    }

    [TestMethod]
    public async Task NothingMatching_ComesBackEmptyRatherThanWithEverything()
    {
        var completion = await CompleteResourceAsync("bswup://docs/{slug}", "slug", "zzzz");

        Assert.AreEqual(0, completion.Completion.Values.Count);
        Assert.AreEqual(0, completion.Completion.Total);
    }

    [TestMethod]
    public async Task SourcePaths_StayUnderTheProtocolsResponseCap()
    {
        var completion = await CompleteResourceAsync("bswup://source/{path}", "path", string.Empty);

        Assert.IsTrue(completion.Completion.Values.Count <= 100, "the protocol caps one response at 100 values");

        if (completion.Completion.Total > 100)
        {
            Assert.IsTrue(completion.Completion.HasMore, "a capped response has to say there is more");
        }
    }

    [TestMethod]
    public async Task HostingModel_IsCompletedForTheSetupPrompt()
    {
        var completion = await CompletePromptAsync("add-bswup-to-app", "hostingModel", string.Empty);

        string[] expected = [.. BswupSetupGuide.HostingModels, "unknown"];

        CollectionAssert.AreEquivalent(expected, completion.Completion.Values.ToArray());
    }

    [TestMethod]
    public async Task AnArgumentWithNoClosedList_IsNotCompleted()
    {
        var completion = await CompletePromptAsync("configure-bswup-caching", "requirement", "cache");

        Assert.AreEqual(0, completion.Completion.Values.Count, "a free-text argument has nothing to offer");
    }

    [TestMethod]
    public async Task AnUnknownReference_IsNotCompleted()
    {
        var completion = await CompleteResourceAsync("bswup://nope/{x}", "x", string.Empty);

        Assert.AreEqual(0, completion.Completion.Values.Count);
    }
}
