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
    public void Search_DoesNotIndexTheReadme()
    {
        // The README says what the documentation pages say, in one 30,000-character section that
        // matched nearly every query and answered none of them with more than "read the guide".
        // It is still served as a resource; it is no longer something a search can spend a
        // client's context window on.
        var hits = new[] { "cache", "update", "progress", "worker", "install", "handler", "cleanup", "upgrade" }
            .SelectMany(term => Search(term, 50))
            .Where(hit => hit.Kind == "Guide section")
            .ToArray();

        Assert.AreEqual(0, hits.Length, string.Join(", ", hits.Select(hit => hit.Title)));
    }

    [TestMethod]
    public void Search_IgnoresTheWordsEveryEntryHereShares()
    {
        // "bswup" is in every entry, so it separates nothing - and it used to count towards the
        // "how many terms matched" multiplier, which handed every query to the longest document.
        var withSubject = Search("how do I install bswup", 5).Select(hit => hit.Title).ToArray();
        var without = Search("how do I install", 5).Select(hit => hit.Title).ToArray();

        CollectionAssert.AreEqual(without, withSubject,
            $"naming the subject changed the answer: {string.Join(", ", withSubject)}");
    }

    [TestMethod]
    [DataRow("bswup")]
    [DataRow("what is bswup")]
    [DataRow("bit bswup blazor")]
    [DataRow("bit-bswup.js")]
    public void Search_ForTheLibrarysOwnNameStillAnswers(string query)
    {
        // Those shared words are dropped only when the query says something else as well.
        // These are the first thing anyone types, and every word of them is on that list - so
        // filtered out unconditionally they would have the library answer its own name with
        // nothing at all, which reads as "no such thing" rather than "ask more precisely".
        Assert.IsTrue(Search(query, 5).Length > 0, $"'{query}' came back empty");
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
    public void Search_NarrowsTheFollowUpCallToTheHitItself()
    {
        // The whole point of a hit naming a call is that the agent runs it verbatim. A bare
        // GetBswupServiceWorkerSettings() answers a question about one setting with twenty-four,
        // which is the cost this index exists to avoid.
        var narrowed = new[] { "Script attribute", "Service worker setting", "Service worker mode", "Event", "JavaScript API" };

        foreach (var hit in Search("cache update install handler version scope", 50).Where(hit => narrowed.Contains(hit.Kind)))
        {
            StringAssert.Contains(hit.Tool, "(name: \"", $"{hit.Kind} '{hit.Title}' points at an unnarrowed call: {hit.Tool}");
        }
    }

    [TestMethod]
    public void Search_CoversEveryCorpusItClaimsTo()
    {
        var kinds = new[] { "Docs page", "Script attribute", "Service worker setting", "Service worker mode", "Event", "JavaScript API", "Progress parameter", "Source file" };

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
    [Timeout(30_000)]
    public void Search_IsNotDerailedByAPastedFile()
    {
        // Every term is counted against every entry, so the term count is capped; a pasted file as
        // a query must come back rather than scan for minutes - hence the timeout, which is half
        // of what is checked here. A term that does match leads the query, so the other half is
        // that the search still answers the query rather than merely returning from it.
        var query = "assetsExclude " + string.Join(" ", Enumerable.Range(0, 5000).Select(index => $"token{index}"));

        var hits = BswupSearchIndex.Search(query, 12);

        Assert.IsTrue(hits.Length > 0, "the one real term in the query still has to be searched for");
        Assert.IsTrue(hits.Length <= 12, $"the limit still applies to a pasted query; it returned {hits.Length} hits");
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
