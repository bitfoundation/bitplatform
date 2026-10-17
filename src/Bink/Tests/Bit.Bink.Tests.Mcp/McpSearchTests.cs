using System.Text.RegularExpressions;
using Bit.Bink.Demo.Server.Dtos;

namespace Bit.Bink.Tests.Mcp;

/// <summary>
/// The search, and the paths it opens: every hit hands the caller a follow-up call, and that call
/// has to answer with the thing rather than with an apology about a key that no longer exists.
/// </summary>
[TestClass]
public partial class McpSearchTests
{
    /// <summary>Questions phrased the way a developer asks them, and a title that has to rank among the first hits.</summary>
    private static readonly (string Query, string ExpectedTitle)[] _questions =
    [
        ("a signature pad that exports a PNG", "A signature field"),
        ("save the drawing and load it back", "Saving and loading"),
        ("pressure sensitive pen", "Drawing, brushes and pressure"),
        ("highlighter brush", "BinkHighlighterBrush"),
        ("erase part of a stroke", "Erasing"),
        ("touch scrolls the page instead of drawing", "Scrolling versus drawing"),
        ("undo redo", "Undo"),
        ("ink lags on blazor server", "Blazor Server"),
        ("render png on the server", "On a server, or in tests"),
        ("validate a saved json document", "Inspect a Bink document"),
        ("focus outline", ".bit-bink-canvas:focus-visible"),
        ("real-time collaboration", "Collaboration"),
        ("custom brush", "A custom brush"),
    ];

    [TestMethod]
    public async Task Common_questions_find_their_answer_among_the_first_hits()
    {
        // Every question is asked before anything is asserted, so one run reports every miss at once.
        var misses = new List<string>();

        foreach (var (query, expected) in _questions)
        {
            var result = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = query, ["limit"] = 6 });

            if (result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase))) continue;

            misses.Add($"'{query}' did not find '{expected}' among: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task A_word_inside_another_word_does_not_count()
    {
        // Every public type is named Bink..., so "ink" counted inside a word would match all of them.
        var result = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = "ink", ["limit"] = 50 });

        Assert.IsNotEmpty(result.Hits);

        // A hit's snippet is cut around the occurrence that was counted, so every hit shows "ink" as a word of its own.
        foreach (var hit in result.Hits)
        {
            Assert.IsTrue(WordStartRegex().IsMatch($"{hit.Title} {hit.Snippet}"), $"'{hit.Title}' was ranked for 'ink' without the word in it: {hit.Snippet}");
        }
    }

    [GeneratedRegex("(?<![A-Za-z0-9])[Ii]nk|(?<=[a-z])Ink")]
    private static partial Regex WordStartRegex();

    [TestMethod]
    public async Task The_sections_about_the_readme_itself_are_not_answers()
    {
        foreach (var query in new[] { "ink lags on blazor server", "mcp server tools", "documentation", "export" })
        {
            var result = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = query, ["limit"] = 50 });

            Assert.IsFalse(result.Hits.Any(hit => hit.Kind == "Guide section" && hit.Title is "MCP server" or "Documentation"), query);
        }
    }

    [TestMethod]
    public async Task A_query_of_filler_words_is_told_what_to_search_for()
    {
        var result = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = "how do I do it" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Message);
        Assert.Contains("Search for the thing itself", result.Message);
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_falls_back_to_the_indexes()
    {
        var result = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = "qqqq zzzz" });

        Assert.IsEmpty(result.Hits);
        Assert.Contains("GetBinkApi", result.Message!);
    }

    [TestMethod]
    public async Task The_library_name_alone_still_finds_something()
    {
        var result = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = "bink" });

        Assert.IsNotEmpty(result.Hits);
    }

    [TestMethod]
    public async Task The_limit_is_honoured_and_clamped()
    {
        var three = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = "stroke", ["limit"] = 3 });
        var huge = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = "stroke", ["limit"] = 5000 });

        Assert.HasCount(3, three.Hits);
        Assert.IsTrue(three.HasMore);
        Assert.HasCount(50, huge.Hits);
    }

    /// <summary>
    /// Every follow-up call a hit suggests is made, verbatim, over the protocol. A renamed heading, a
    /// moved file or a renamed tool leaves every tool working on its own and this path broken.
    /// </summary>
    [TestMethod]
    public async Task Every_follow_up_call_a_hit_suggests_answers_with_the_thing()
    {
        var queries = _questions.Select(question => question.Query)
            .Concat(["stroke", "brush", "document", "layer", "export", "selection", "eraser", "canvas", "setup", "highlights", "demo page", "css"]);

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BinkSearchResultDto>("SearchBink", new() { ["query"] = query, ["limit"] = 50 });

            foreach (var hit in result.Hits) calls.Add(hit.Tool);
        }

        Assert.IsGreaterThan(40, calls.Count, "The searches above should open many distinct follow-up calls.");

        foreach (var call in calls)
        {
            var match = FollowUpRegex().Match(call);
            Assert.IsTrue(match.Success, $"'{call}' is not a call a caller can make verbatim.");

            var tool = match.Groups["tool"].Value;
            CollectionAssert.Contains(McpSurfaceTests.ExpectedTools, tool, $"'{call}' names a tool that does not exist.");

            var arguments = match.Groups["name"].Success
                ? new Dictionary<string, object?> { [match.Groups["name"].Value] = match.Groups["value"].Value }
                : null;

            if (tool == "InspectBinkDocument")
            {
                var report = await McpDocumentLabTests.InspectAsync(null);
                Assert.IsNotNull(report.Sample, call);
                continue;
            }

            var text = await McpCall.TextAsync(tool, arguments);

            Assert.DoesNotContain("has no section called", text, call);
            Assert.DoesNotContain("has no public type called", text, call);
            Assert.DoesNotContain("registers no brush called", text, call);
            Assert.DoesNotContain("is not a known render mode", text, call);
            Assert.DoesNotContain("No source file at", text, call);
        }
    }

    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<name>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex FollowUpRegex();
}
