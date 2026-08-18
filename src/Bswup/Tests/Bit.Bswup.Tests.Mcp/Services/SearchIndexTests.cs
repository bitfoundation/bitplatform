using Bit.Bswup.Demo.Server.Dtos;
using Bit.Bswup.Demo.Server.Services;

namespace Bit.Bswup.Tests.Mcp.Services;

/// <summary>
/// The default entry point: one query across everything, each hit carrying the follow-up call that
/// returns the full text. The tests below are phrased the way a question arrives - in symptoms, not
/// in setting names - because that is the case the index exists for, and they check the follow-up
/// call as much as the hit, since a hit an agent cannot act on is a dead end.
/// </summary>
[TestClass]
public class SearchIndexTests
{
    private static BswupSearchHitDto[] Search(string query, int limit = 12) => BswupSearchIndex.Search(query, limit);

    private static int RankOf(string query, string expectedTitle, int limit)
    {
        var hits = Search(query, limit);

        var rank = Array.FindIndex(hits, hit => hit.Title.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(rank >= 0,
            $"'{query}' did not surface '{expectedTitle}'. Got: {string.Join(", ", hits.Select(hit => hit.Title))}");

        return rank;
    }

    // -- Questions phrased the way they arrive ---------------------------------

    [TestMethod]
    [DataRow("cache an external CDN script", "externalAssets")]
    [DataRow("show a progress bar while installing", "BswupProgress")]
    [DataRow("keep the API out of the worker", "serverHandledUrls")]
    [DataRow("offline deep link shows the home page", "noPrerenderQuery")]
    [DataRow("reset the app and clear its caches", "forceRefresh")]
    [DataRow("check for updates on a timer", "updateInterval")]
    [DataRow("splash never finishes install", "stallTimeout")]
    [DataRow("block admin urls", "prohibitedUrls")]
    [DataRow("bump the cache bucket per build", "cacheVersion")]
    [DataRow("subresource integrity", "enableIntegrityCheck")]
    public void Search_AnswersASymptomWithTheSettingThatGovernsIt(string query, string expectedTitle)
    {
        // A page of hits is what a client shows; the answer has to be on it.
        RankOf(query, expectedTitle, limit: 12);
    }

    [TestMethod]
    [DataRow("app never picks up new versions", "Troubleshooting")]
    [DataRow("cache an external CDN script", "externalAssets")]
    [DataRow("offline deep link shows home page", "noPrerenderQuery")]
    [DataRow("show a progress bar while installing", "BswupProgress")]
    public void Search_AnswersTheExampleQueriesItsOwnDescriptionAdvertises(string query, string expectedTitle)
    {
        // These four are quoted verbatim in the SearchBswup tool description and in the site's MCP
        // explorer, so they are the first thing an agent and a visitor try. An example that does
        // not work teaches both of them that the tool does not.
        var rank = RankOf(query, expectedTitle, limit: 12);

        Assert.IsTrue(rank <= 2, $"'{query}' put '{expectedTitle}' at rank {rank + 1}");
    }

    [TestMethod]
    [DataRow("stallTimeout")]
    [DataRow("assetsExclude")]
    [DataRow("updateReady")]
    [DataRow("checkForUpdate")]
    [DataRow("forceRefresh")]
    [DataRow("noPrerenderQuery")]
    [DataRow("externalAssets")]
    [DataRow("AutoReload")]
    public void Search_PutsTheEntryNamedByAnExactQueryFirst(string name)
    {
        // The title is split at its camel-case humps AND kept whole, so the full name is one of the
        // words the entry answers to. Keeping only the humps left "stallTimeout" matching no word
        // of its own title, and the docs page that merely lists it as a keyword outranked it.
        var rank = RankOf(name, name, limit: 12);

        Assert.AreEqual(0, rank, $"'{name}' is its own name but ranked {rank + 1}th");
    }

    [TestMethod]
    public void Search_SplitsCamelCaseSoAWordInsideANameIsFound()
    {
        RankOf("visibility", "updateOnVisibility", limit: 12);
        RankOf("prerender", "noPrerenderQuery", limit: 20);
    }

    [TestMethod]
    public void Search_DoesNotAnswerAQuestionWithADirectoryListing()
    {
        // Source files are examples, not answers, and their titles are paths whose segments
        // ("Client", "Pages", "Shared") are common words carrying no topic at all.
        foreach (var query in new[] { "reset a stuck client", "handler events", "progress bar" })
        {
            var hits = Search(query, 5);

            Assert.IsTrue(hits.Any(hit => hit.Kind != "Source file"),
                $"'{query}' answered with nothing but source files");
            Assert.IsTrue(hits.Count(hit => hit.Kind == "Source file") <= 2,
                $"'{query}' filled its first page with source files: {string.Join(", ", hits.Select(hit => hit.Title))}");
        }
    }

    [TestMethod]
    public void Search_DoesNotAnswerEveryQuestionWithTheWholeGuide()
    {
        // The README's first heading covers two thirds of the file, so it matches nearly any query
        // and answers none of them with more than "read the guide".
        var broad = BswupSourceCatalog.GuideSections.OrderByDescending(section => section.Lines).First();

        foreach (var query in new[] { "cache an external CDN script", "bump the cache bucket per build", "subresource integrity", "app never picks up new versions" })
        {
            var hits = Search(query, 3);

            Assert.AreNotEqual(broad.Heading, hits[0].Title,
                $"'{query}' was answered with the whole guide first: {string.Join(", ", hits.Select(hit => hit.Title))}");
        }
    }

    [TestMethod]
    public void Search_StillFindsTheBroadGuideSectionWhenItIsWhatWasAsked()
    {
        // De-weighted, not hidden: two thirds of the guide is still the only place some of this
        // material is written down.
        var broad = BswupSourceCatalog.GuideSections.OrderByDescending(section => section.Lines).First();

        RankOf(broad.Heading, broad.Heading, limit: 20);
    }

    [TestMethod]
    public void Search_MatchesAcrossThePluralAHeadingHappensToUse()
    {
        // Nobody phrases a question in the number the heading is written in.
        Assert.IsTrue(Search("event", 30).Any(hit => hit.Kind == "Event" || hit.Title.Contains("Event", StringComparison.OrdinalIgnoreCase)));
    }

    // -- Every hit has to be actionable ----------------------------------------

    [TestMethod]
    public void Search_EveryHitNamesTheFollowUpCallThatReturnsItsFullText()
    {
        foreach (var hit in Search("service worker cache update offline progress", 50))
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.Tool), hit.Title);
            StringAssert.StartsWith(hit.Tool, "GetBswup", hit.Title);
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.Kind), hit.Title);
        }
    }

    [TestMethod]
    public void Search_CoversEveryCorpusItClaimsTo()
    {
        var kinds = new[] { "Guide section", "Docs page", "Script attribute", "Service worker setting", "Service worker mode", "Event", "JavaScript API", "Progress parameter", "Source file" };

        foreach (var kind in kinds)
        {
            // Each corpus has to be reachable, or a whole class of answer is invisible.
            var found = new[] { "update", "cache", "progress", "worker", "install", "asset", "mode", "handler", "service" }
                .SelectMany(term => Search(term, 50))
                .Any(hit => hit.Kind == kind);

            Assert.IsTrue(found, $"nothing of kind '{kind}' is reachable by search");
        }
    }

    [TestMethod]
    public void Search_SnippetsAreShortAndSingleLine()
    {
        foreach (var hit in Search("cache", 50))
        {
            Assert.IsFalse(hit.Snippet.Contains('\n'), hit.Title);
            Assert.IsTrue(hit.Snippet.Length <= 250, $"{hit.Title}: {hit.Snippet.Length} characters");
        }
    }

    // -- Bounds ----------------------------------------------------------------

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("a in do")]
    [DataRow("how does the what")]
    public void Search_QueriesWithNothingToMatchOnReturnNothing(string? query)
    {
        // Stop words and one/two-letter words match everything and rank nothing; answering with
        // an arbitrary slice of the corpus is worse than answering with none of it.
        Assert.AreEqual(0, BswupSearchIndex.Search(query!, 12).Length);
    }

    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(-5, 1)]
    [DataRow(1, 1)]
    [DataRow(1000, 50)]
    public void Search_ClampsTheLimitToTheDocumentedRange(int limit, int expectedMax)
    {
        var hits = BswupSearchIndex.Search("cache update worker asset", limit);

        Assert.IsTrue(hits.Length <= expectedMax, $"limit {limit} returned {hits.Length} hits");
        Assert.IsTrue(hits.Length > 0);
    }

    [TestMethod]
    public void Search_IsNotDerailedByAPastedFile()
    {
        // Every term is counted against every entry, so the term count is capped; a pasted file as
        // a query must come back rather than scan for minutes.
        var query = string.Join(" ", Enumerable.Range(0, 5000).Select(index => $"token{index}"));

        var hits = BswupSearchIndex.Search(query, 12);

        Assert.IsNotNull(hits);
    }

    [TestMethod]
    public void Search_IsCaseInsensitive()
    {
        var lower = Search("assetsexclude");
        var exact = Search("assetsExclude");

        CollectionAssert.AreEqual(exact.Select(hit => hit.Title).ToArray(), lower.Select(hit => hit.Title).ToArray());
    }

    [TestMethod]
    public void Search_IsStableForTheSameQuery()
    {
        var first = Search("update", 20).Select(hit => hit.Title).ToArray();
        var second = Search("update", 20).Select(hit => hit.Title).ToArray();

        CollectionAssert.AreEqual(first, second, "ties are broken by title, so results cannot reshuffle between calls");
    }
}
