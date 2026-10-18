using System.Text.RegularExpressions;
using Bit.Bsqlite.Demo.Server.Dtos;

namespace Bit.Bsqlite.Tests.Mcp;

/// <summary>
/// The search, and the paths it opens: every hit hands the caller a follow-up call, and that call has
/// to answer with the thing rather than with an apology about a key that no longer exists.
/// </summary>
[TestClass]
public partial class McpSearchTests
{
    /// <summary>Questions phrased the way a developer asks them, and a title that has to rank among the first hits.</summary>
    private static readonly (string Query, string ExpectedTitle)[] _questions =
    [
        ("live query", "Live queries"),
        ("prerendering", "InteractiveWebAssembly with prerendering"),
        ("opfs fallback indexeddb", "BsqliteIndexedDbStorageProvider"),
        ("migrate to another backend", "Migration"),
        ("unit test without a browser", "Unit testing with Bit.Bsqlite.Testing"),
        ("attach", "Check SQL against the guards"),
        ("moving off besql", "Moving off Bit.Besql"),
        ("conformance kit", "The storage conformance kit"),
        ("custom storage provider", "Writing a custom storage provider"),
        ("export the database file", "ExportAsync"),
        ("sticky binding", "UseBindingStore"),
        ("interactive server", "InteractiveServer (not supported)"),
        ("transactions savepoint", "BsqliteTransaction"),
        ("non persistent fallback", "AllowNonPersistentFallback"),
        ("hosting under a sub-path", "Hosting under a sub-path"),
        ("SaveChanges synchronous", "BsqliteSynchronousApiNotSupportedException"),
    ];

    [TestMethod]
    public async Task Common_questions_find_their_answer_among_the_first_hits()
    {
        // Every question is asked before anything is asserted, so one run reports every miss at once.
        var misses = new List<string>();

        foreach (var (query, expected) in _questions)
        {
            var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = query, ["limit"] = 6 });

            if (result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase))) continue;

            misses.Add($"'{query}' did not find '{expected}' among: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task A_word_inside_another_word_does_not_count()
    {
        // Every public type is named Bsqlite..., so "sqlite" counted inside a word would match all of them.
        var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "sqlite", ["limit"] = 50 });

        Assert.IsNotEmpty(result.Hits);

        // A hit's snippet is cut around the occurrence that was counted, so every hit shows "sqlite" as a word of its own.
        foreach (var hit in result.Hits)
        {
            Assert.IsTrue(WordStartRegex().IsMatch($"{hit.Title} {hit.Snippet}"), $"'{hit.Title}' was ranked for 'sqlite' without the word in it: {hit.Snippet}");
        }
    }

    [GeneratedRegex("(?<![A-Za-z0-9])(?i:sqlite)|(?<=[a-z])Sqlite")]
    private static partial Regex WordStartRegex();

    [TestMethod]
    public async Task The_sections_about_the_documents_themselves_are_not_answers()
    {
        string[] meta = ["Table of Contents", "Documentation", "MCP server (for AI agents)", "License", "Links"];

        foreach (var query in new[] { "mcp server tools", "documentation", "license", "table of contents", "links", "storage" })
        {
            var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = query, ["limit"] = 50 });

            Assert.IsFalse(result.Hits.Any(hit => hit.Kind.EndsWith(" section", StringComparison.Ordinal) && meta.Contains(hit.Title)), query);
        }
    }

    [TestMethod]
    public async Task The_conformance_kits_rules_are_found_through_the_kit_not_one_by_one()
    {
        // The kit's tests are named as sentences about storage ("Export_ThrowsForADatabaseThatDoesNotExist"); hit
        // one by one they would answer every question about exporting or throwing with a test.
        var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "export throws", ["limit"] = 50 });

        Assert.IsFalse(result.Hits.Any(hit => hit.Context?.EndsWith("ConformanceTests", StringComparison.Ordinal) is true && hit.Kind == "API method"),
            string.Join(" | ", result.Hits.Select(hit => hit.Title)));

        var kit = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "conformance", ["limit"] = 12 });

        Assert.IsTrue(kit.Hits.Any(hit => hit.Title == "BsqliteStorageProviderConformanceTests"));
        Assert.IsTrue(kit.Hits.Any(hit => hit.Title == "BsqliteStorageProviderConformanceTests.CreateProvider"), "What a test class overrides stays findable.");
    }

    [TestMethod]
    public async Task A_query_of_filler_words_is_told_what_to_search_for()
    {
        var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "how do I do it" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Message);
        Assert.Contains("Search for the thing itself", result.Message);
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_falls_back_to_the_indexes()
    {
        var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "qqqq zzzz" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Terms);
        Assert.Contains("GetBsqliteApi", result.Message!);
    }

    [TestMethod]
    public async Task A_near_miss_suggests_what_to_search_for_instead()
    {
        // A typo past the end of a title word ranks nothing - and is close enough to that word to be worth
        // naming, rather than leaving the caller to conclude there is no such thing.
        var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "persistencex" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.DidYouMean);
        CollectionAssert.Contains(result.DidYouMean, "BsqlitePersistence");
        Assert.Contains("didYouMean", result.Message!);
    }

    [TestMethod]
    public async Task The_library_name_alone_still_finds_something()
    {
        var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "bsqlite" });

        Assert.IsNotEmpty(result.Hits);
    }

    [TestMethod]
    public async Task The_limit_is_honoured_and_clamped()
    {
        var three = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "storage", ["limit"] = 3 });
        var huge = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = "storage", ["limit"] = 5000 });

        Assert.HasCount(3, three.Hits);
        Assert.IsTrue(three.HasMore);
        Assert.HasCount(50, huge.Hits);
    }

    /// <summary>
    /// Every follow-up call a hit suggests is made, verbatim, over the protocol. A renamed heading, a moved
    /// file or a renamed tool leaves every tool working on its own and this path broken.
    /// </summary>
    [TestMethod]
    public async Task Every_follow_up_call_a_hit_suggests_answers_with_the_thing()
    {
        var queries = _questions.Select(question => question.Query)
            .Concat(["storage", "provider", "connection", "transaction", "live", "migration", "selection", "guard", "setup", "demo page", "readme", "binding", "limits"]);

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BsqliteSearchResultDto>("SearchBsqlite", new() { ["query"] = query, ["limit"] = 50 });

            foreach (var hit in result.Hits) calls.Add(hit.Tool);
        }

        Assert.IsGreaterThan(60, calls.Count, "The searches above should open many distinct follow-up calls.");

        foreach (var call in calls)
        {
            var match = FollowUpRegex().Match(call);
            Assert.IsTrue(match.Success, $"'{call}' is not a call a caller can make verbatim.");

            var tool = match.Groups["tool"].Value;
            CollectionAssert.Contains(McpSurfaceTests.ExpectedTools, tool, $"'{call}' names a tool that does not exist.");

            var arguments = new Dictionary<string, object?>();
            for (var i = 0; i < match.Groups["name"].Captures.Count; i++)
            {
                arguments[match.Groups["name"].Captures[i].Value] = match.Groups["value"].Captures[i].Value;
            }

            if (tool is "PlanBsqliteStorageSelection")
            {
                var plan = await McpCall.StructuredAsync<BsqliteStoragePlanDto>(tool, arguments);
                Assert.IsTrue(plan.Opens, call);
                continue;
            }

            if (tool is "CheckBsqliteSql")
            {
                var check = await McpCall.StructuredAsync<BsqliteSqlCheckDto>(tool, arguments);
                Assert.IsNotEmpty(check.Statements, call);
                continue;
            }

            var text = await McpCall.TextAsync(tool, arguments.Count == 0 ? null : arguments);

            Assert.DoesNotContain("has no section called", text, call);
            Assert.DoesNotContain("There is no Bit.Bsqlite document", text, call);
            Assert.DoesNotContain("has no public type called", text, call);
            Assert.DoesNotContain("No first-party storage backend", text, call);
            Assert.DoesNotContain("is not a known scenario", text, call);
            Assert.DoesNotContain("No source file at", text, call);
        }
    }

    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<name>\w+): "(?<value>[^"]*)"(?:, (?<name>\w+): "(?<value>[^"]*)")*)?\)$""")]
    private static partial Regex FollowUpRegex();
}
