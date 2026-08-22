using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bit.Butil.Tests.Mcp.Infrastructure;

namespace Bit.Butil.Tests.Mcp;

/// <summary>
/// SearchButil is the tool an agent is told to reach for first, so it is the one whose failure is
/// most expensive: every other tool needs a name, and this is what turns a task into one.
/// <para>
/// The tests are written the way the tool is meant to be used - a capability phrased as a person
/// would phrase it, not the identifier the platform happens to use - because that gap is the entire
/// reason the index exists. "copy some text" is Clipboard.WriteText, "keep the screen on" is
/// WakeLock, and a search that only finds what you could already name is a search nobody needed.
/// </para>
/// </summary>
[TestClass]
public class SearchTests : McpTestBase
{
    /// <summary>
    /// A question in the words someone would actually ask it, and a title that has to come back.
    /// None of the queries contains the name of the thing it should find.
    /// </summary>
    public static IEnumerable<object[]> CapabilityQueries =>
    [
        ["copy some text so the user can paste it", "Clipboard"],
        ["keep the screen awake while a recipe is on screen", "WakeLock"],
        ["observe when an element enters the viewport", "IntersectionObserver"],
        ["store data in a transactional database in the browser", "IndexedDb"],
        ["read a file the user picked", "FileReader"],
        ["record the microphone to a file", "MediaRecorder"],
        ["hash a value with sha-256", "Crypto"],
        ["send a message to another tab", "BroadcastChannel"],
        ["passkeys", "WebAuthn"],
        ["watch an element resize", "ResizeObserver"],
    ];

    [TestMethod]
    [DynamicData(nameof(CapabilityQueries))]
    public async Task A_capability_finds_the_api_that_implements_it(string query, string expected)
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query, limit = 20 });

        Assert.IsNotEmpty(result.Hits, $"'{query}' found nothing. {result.Message}");

        // Title, owning context or follow-up call: any of the three naming the API means the search
        // put the agent one call away from it, which is all the tool promises.
        var found = result.Hits.Any(hit => $"{hit.Title} {hit.Context} {hit.Tool}".Contains(expected, StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(found,
            $"'{query}' should surface {expected}, but the hits were: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}.");
    }

    [TestMethod]
    public async Task The_thing_asked_for_by_name_ranks_first()
    {
        // A term in a name is worth far more than the same term buried in prose: someone asking for
        // "WriteText" wants the method, not the paragraphs that happen to mention it.
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "Clipboard.WriteText" });

        Assert.IsNotEmpty(result.Hits, result.Message);

        // The page that documents the API can legitimately outrank one of its members - it matches
        // the same words and carries the samples. What must not happen is the named member being
        // buried: an agent reads the first few hits and calls what they point at.
        using (Assert.Scope())
        {
            Assert.Contains("Clipboard", result.Hits[0].Title,
                $"The top hit was '{result.Hits[0].Title}', which is not about Clipboard at all.");

            Assert.IsTrue(result.Hits.Take(5).Any(hit => hit.Title.Contains("WriteText", StringComparison.Ordinal)),
                $"The member that was named by its own name is not in the top five: {string.Join(" | ", result.Hits.Take(5).Select(hit => hit.Title))}.");
        }
    }

    [TestMethod]
    public async Task Plurals_are_the_same_word()
    {
        // Nobody phrases a question in the number the API happens to use.
        var singular = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "cookie" });
        var plural = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "cookies" });

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(singular.Hits);
            Assert.IsNotEmpty(plural.Hits);
            Assert.Contains(singular.Hits[0].Title, plural.Hits.Select(hit => hit.Title));
        }
    }

    [TestMethod]
    public async Task Every_hit_is_complete_enough_to_act_on()
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "clipboard", limit = 20 });

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(result.Hits);
            Assert.IsNull(result.Message, "A search with hits should not also carry an explanation of having none.");

            foreach (var hit in result.Hits)
            {
                Assert.IsNotEmpty(hit.Kind);
                Assert.IsNotEmpty(hit.Title);
                Assert.IsNotEmpty(hit.Tool, $"The '{hit.Title}' hit names no follow-up call, so an agent has nowhere to go with it.");
                Assert.IsNotNull(ToolCallReference.Parse(hit.Tool), $"'{hit.Tool}' is not a call an agent can make verbatim.");
            }
        }
    }

    [TestMethod]
    public async Task A_snippet_is_prose_rather_than_the_markup_it_was_found_in()
    {
        // A docs page is indexed as the Razor component that renders it, because that is where its
        // prose lives - but a window cut out of that source is attribute soup, and a snippet is the
        // one part of a hit that is read rather than acted on. Hits used to quote things like
        // `jectAs="Clipboard clipboard" /> <div class="stack"> <DemoSection Title=`.
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "clipboard", limit = 20 });

        var pages = result.Hits.Where(hit => hit.Kind == "Docs page").ToArray();

        Assert.IsNotEmpty(pages, "A search for an API that has a page found none.");

        using (Assert.Scope())
        {
            foreach (var hit in pages)
            {
                Assert.IsNotEmpty(hit.Snippet, $"The '{hit.Title}' hit quotes nothing.");
                Assert.DoesNotContain("<", hit.Snippet,
                    $"The '{hit.Title}' hit quotes markup rather than prose: {hit.Snippet}");
                Assert.DoesNotContain("=\"", hit.Snippet,
                    $"The '{hit.Title}' hit quotes markup rather than prose: {hit.Snippet}");
            }
        }
    }

    [TestMethod]
    public async Task The_page_documenting_this_server_is_not_in_the_corpus_it_serves()
    {
        // It quotes example queries and every tool name, so it matched questions about the library
        // as readily as questions about itself - and what it explains is what the client was handed
        // at initialize. It is still a page: GetButilDocsPage(slug: "mcp-server") answers with it.
        var queries = new[] { "copy text to clipboard", "search", "tools", "mcp server" };

        foreach (var query in queries)
        {
            var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query, limit = 50 });

            Assert.IsEmpty(result.Hits.Where(hit => hit.Kind == "Docs page" && hit.Title == "MCP server"),
                $"'{query}' surfaced the page documenting this server.");
        }

        var page = Text(await CallAsync("GetButilDocsPage", new { slug = "mcp-server" }));

        Assert.StartsWith("Bit.Butil documentation page: /mcp-server", page,
            "The page is out of the search corpus, not out of reach.");
    }

    [TestMethod]
    public async Task The_limit_is_honoured()
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 3 });

        Assert.HasCount(3, result.Hits);
    }

    [TestMethod]
    public async Task Every_follow_up_call_a_search_names_actually_resolves()
    {
        // The promise the whole design rests on: one search is enough to know what to ask for next.
        // A hit whose follow-up call answers "no such page" sends an agent somewhere there is
        // nothing, and it has no way to tell that from the tool being broken.
        var queries = new[] { "clipboard", "storage", "geolocation", "getting started", "wake lock", "observer", "crypto" };

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var failures = new List<string>();

        foreach (var query in queries)
        {
            var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query, limit = 15 });

            foreach (var hit in result.Hits)
            {
                if (seen.Add(hit.Tool) is false) continue;

                var call = ToolCallReference.Parse(hit.Tool);

                if (call is null)
                {
                    failures.Add($"'{hit.Tool}' could not be parsed as a call.");
                    continue;
                }

                if (ButilMcp.Tools.ContainsKey(call.Tool) is false)
                {
                    failures.Add($"'{hit.Tool}' names a tool that does not exist.");
                    continue;
                }

                var answer = await CallRawAsync(call.Tool, call.Arguments);
                var text = Text(answer);

                if (answer.IsError is true)
                {
                    failures.Add($"'{hit.Tool}' answered with an error: {text}");
                    continue;
                }

                // A follow-up call landing on a "not found" answer means the hit pointed at nothing.
                if (ResolvedToNothing(text))
                {
                    failures.Add($"'{hit.Tool}' resolved to nothing: {text[..Math.Min(200, text.Length)]}");
                }
            }
        }

        // The detection above reads prose, because a miss here is not a protocol error - it is a
        // sentence naming the nearest candidates, deliberately. Prose the server later rewords would
        // turn this test green by ceasing to detect anything at all, so the detector is first shown
        // to fire on calls that are known to miss.
        var controls = new (string Tool, object Arguments)[]
        {
            ("GetButilDocsPage", new { slug = "no-such-page" }),
            ("GetButilSourceFile", new { path = "Demo/Client/Pages/NoSuchPage.razor" }),
            ("GetButilGuideSection", new { heading = "No such section" }),
            ("GetButilApiDetails", new { typeName = "NoSuchType" }),
            ("PlanButilFeature", new { apis = "NoSuchApi" }),
        };

        var undetected = new List<string>();

        foreach (var (tool, arguments) in controls)
        {
            var control = Text(await CallRawAsync(tool, arguments));

            if (ResolvedToNothing(control) is false)
            {
                undetected.Add($"{tool} answered a miss this test would have read as a hit: {control[..Math.Min(200, control.Length)]}");
            }
        }

        using (Assert.Scope())
        {
            Assert.IsNotEmpty(seen, "No follow-up calls were exercised, so this test proved nothing.");
            Assert.IsEmpty(undetected, $"The shapes a miss is recognised by no longer match what the server says: {string.Join(" | ", undetected)}");
            Assert.IsEmpty(failures, $"Follow-up calls that did not resolve:\n{string.Join("\n", failures)}");
        }
    }

    /// <summary>Every "not found" answer on this server says so in one of these shapes.</summary>
    private static bool ResolvedToNothing(string text)
        => text.StartsWith("No documentation page has the slug", StringComparison.Ordinal)
        || text.StartsWith("No source file at", StringComparison.Ordinal)
        || text.Contains("has no section called", StringComparison.Ordinal)
        || text.Contains("has no public type called", StringComparison.Ordinal)
        || text.Contains("has nothing called", StringComparison.Ordinal);

    [TestMethod]
    public async Task The_index_covers_every_corpus_the_tool_claims()
    {
        // "the reference guide, the documentation pages, every public type and member, the
        // browser-support matrix and the demo's source files" - the description's own list. A
        // corpus that silently failed to build would just never appear in a result.
        var queries = new[] { "clipboard", "storage", "getting started", "geolocation", "prerendering", "subscription", "trimming" };

        var kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var query in queries)
        {
            var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query, limit = 50 });

            foreach (var hit in result.Hits) kinds.Add(hit.Kind);
        }

        using (Assert.Scope())
        {
            Assert.Contains("Guide section", kinds);
            Assert.Contains("Docs page", kinds);
            Assert.Contains("Browser support", kinds);
            Assert.Contains("Source file", kinds);
            Assert.IsTrue(kinds.Any(kind => kind.StartsWith("API ", StringComparison.Ordinal)),
                $"Nothing from the reflected API surface was indexed. Kinds seen: {string.Join(", ", kinds)}.");
        }
    }

    [TestMethod]
    public async Task A_search_is_repeatable()
    {
        // The tool is annotated idempotent, and the index is built once and shared. Two identical
        // searches disagreeing would mean the ranking depends on something that is not the query -
        // which is exactly what a dictionary's enumeration order would give you.
        var first = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 20 });
        var second = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 20 });

        Assert.AreSequenceEqual(first.Hits.Select(hit => hit.Title), second.Hits.Select(hit => hit.Title));
    }
}
