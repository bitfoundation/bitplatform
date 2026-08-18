using NUnit.Framework;
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
[TestFixture]
[Parallelizable(ParallelScope.Self)]
public class SearchTests : McpTestBase
{
    /// <summary>
    /// A question in the words someone would actually ask it, and a title that has to come back.
    /// None of the queries contains the name of the thing it should find.
    /// </summary>
    private static readonly (string Query, string Expected)[] _capabilityQueries =
    [
        ("copy some text so the user can paste it", "Clipboard"),
        ("keep the screen awake while a recipe is on screen", "WakeLock"),
        ("observe when an element enters the viewport", "IntersectionObserver"),
        ("store data in a transactional database in the browser", "IndexedDb"),
        ("read a file the user picked", "FileReader"),
        ("record the microphone to a file", "MediaRecorder"),
        ("hash a value with sha-256", "Crypto"),
        ("send a message to another tab", "BroadcastChannel"),
        ("passkeys", "WebAuthn"),
        ("watch an element resize", "ResizeObserver"),
    ];

    [Test]
    public async Task A_capability_finds_the_api_that_implements_it([ValueSource(nameof(_capabilityQueries))] (string Query, string Expected) query)
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = query.Query, limit = 20 });

        Assert.That(result.Hits, Is.Not.Empty, $"'{query.Query}' found nothing. {result.Message}");

        // Title, owning context or follow-up call: any of the three naming the API means the search
        // put the agent one call away from it, which is all the tool promises.
        var found = result.Hits.Any(hit => $"{hit.Title} {hit.Context} {hit.Tool}".Contains(query.Expected, StringComparison.OrdinalIgnoreCase));

        Assert.That(found, Is.True,
            $"'{query.Query}' should surface {query.Expected}, but the hits were: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}.");
    }

    [Test]
    public async Task The_thing_asked_for_by_name_ranks_first()
    {
        // A term in a name is worth far more than the same term buried in prose: someone asking for
        // "WriteText" wants the method, not the paragraphs that happen to mention it.
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "Clipboard.WriteText" });

        Assert.That(result.Hits, Is.Not.Empty, result.Message);

        // The page that documents the API can legitimately outrank one of its members - it matches
        // the same words and carries the samples. What must not happen is the named member being
        // buried: an agent reads the first few hits and calls what they point at.
        Assert.Multiple(() =>
        {
            Assert.That(result.Hits[0].Title, Does.Contain("Clipboard"),
                $"The top hit was '{result.Hits[0].Title}', which is not about Clipboard at all.");

            Assert.That(result.Hits.Take(5).Any(hit => hit.Title.Contains("WriteText", StringComparison.Ordinal)), Is.True,
                $"The member that was named by its own name is not in the top five: {string.Join(" | ", result.Hits.Take(5).Select(hit => hit.Title))}.");
        });
    }

    [Test]
    public async Task Plurals_are_the_same_word()
    {
        // Nobody phrases a question in the number the API happens to use.
        var singular = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "cookie" });
        var plural = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "cookies" });

        Assert.Multiple(() =>
        {
            Assert.That(singular.Hits, Is.Not.Empty);
            Assert.That(plural.Hits, Is.Not.Empty);
            Assert.That(plural.Hits.Select(hit => hit.Title), Does.Contain(singular.Hits[0].Title));
        });
    }

    [Test]
    public async Task Every_hit_is_complete_enough_to_act_on()
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "clipboard", limit = 20 });

        Assert.Multiple(() =>
        {
            Assert.That(result.Hits, Is.Not.Empty);
            Assert.That(result.Message, Is.Null, "A search with hits should not also carry an explanation of having none.");

            foreach (var hit in result.Hits)
            {
                Assert.That(hit.Kind, Is.Not.Empty);
                Assert.That(hit.Title, Is.Not.Empty);
                Assert.That(hit.Tool, Is.Not.Empty, $"The '{hit.Title}' hit names no follow-up call, so an agent has nowhere to go with it.");
                Assert.That(ToolCallReference.Parse(hit.Tool), Is.Not.Null, $"'{hit.Tool}' is not a call an agent can make verbatim.");
            }
        });
    }

    [Test]
    public async Task The_limit_is_honoured()
    {
        var result = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 3 });

        Assert.That(result.Hits, Has.Length.EqualTo(3));
    }

    [Test]
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
            ("InspectButilApi", new { name = "NoSuchApi" }),
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

        Assert.Multiple(() =>
        {
            Assert.That(seen, Is.Not.Empty, "No follow-up calls were exercised, so this test proved nothing.");
            Assert.That(undetected, Is.Empty, $"The shapes a miss is recognised by no longer match what the server says: {string.Join(" | ", undetected)}");
            Assert.That(failures, Is.Empty, $"Follow-up calls that did not resolve:\n{string.Join("\n", failures)}");
        });
    }

    /// <summary>Every "not found" answer on this server says so in one of these shapes.</summary>
    private static bool ResolvedToNothing(string text)
        => text.StartsWith("No documentation page has the slug", StringComparison.Ordinal)
        || text.StartsWith("No source file at", StringComparison.Ordinal)
        || text.Contains("has no section called", StringComparison.Ordinal)
        || text.Contains("has no public type called", StringComparison.Ordinal)
        || text.Contains("has nothing called", StringComparison.Ordinal);

    [Test]
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

        Assert.Multiple(() =>
        {
            Assert.That(kinds, Does.Contain("Guide section"));
            Assert.That(kinds, Does.Contain("Docs page"));
            Assert.That(kinds, Does.Contain("Browser support"));
            Assert.That(kinds, Does.Contain("Source file"));
            Assert.That(kinds.Any(kind => kind.StartsWith("API ", StringComparison.Ordinal)), Is.True,
                $"Nothing from the reflected API surface was indexed. Kinds seen: {string.Join(", ", kinds)}.");
        });
    }

    [Test]
    public async Task A_search_is_repeatable()
    {
        // The tool is annotated idempotent, and the index is built once and shared. Two identical
        // searches disagreeing would mean the ranking depends on something that is not the query -
        // which is exactly what a dictionary's enumeration order would give you.
        var first = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 20 });
        var second = await CallStructuredAsync<SearchResult>("SearchButil", new { query = "storage", limit = 20 });

        Assert.That(second.Hits.Select(hit => hit.Title), Is.EqualTo(first.Hits.Select(hit => hit.Title)).AsCollection);
    }
}
