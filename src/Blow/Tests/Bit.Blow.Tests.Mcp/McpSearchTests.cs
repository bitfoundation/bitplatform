using System.Text.RegularExpressions;
using Bit.Blow.Demo.Server.Dtos;

namespace Bit.Blow.Tests.Mcp;

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
        ("a custom node with several handles", "Custom nodes"),
        ("save the flow and restore it", "Saving and restoring"),
        ("undo redo", "Undo and redo"),
        ("copy and paste a selection", "Copy and paste"),
        ("auto layout left to right", "Auto-layout"),
        ("group nodes into a sub-flow", "Sub-flows and grouping"),
        ("drag and drop from a sidebar", "Drag and Drop"),
        ("connection validation", "Validation"),
        ("custom edges", "Custom edges"),
        ("resize a node", "Resizing"),
        ("align and distribute", "Align and distribute"),
        ("alignment guides", "Alignment guides"),
        ("dark mode colours", "Theme property catalog"),
        ("selected node colour", "--bit-blow-node-selected"),
        ("dragging lags on blazor server", "Blazor Server"),
        ("inspect a flow json", "Inspect a Blow flow"),
        ("minimap", "Mini map"),
        ("coming from react flow", "Coming from React Flow"),
        ("stylesheet link", "Setup checklist"),
        // The stylesheet's classes are one hit between them, so the first focus rule stands for them all.
        ("focus outline", ":focus-visible"),
    ];

    [TestMethod]
    public async Task Common_questions_find_their_answer_among_the_first_hits()
    {
        // Every question is asked before anything is asserted, so one run reports every miss at once.
        var misses = new List<string>();

        foreach (var (query, expected) in _questions)
        {
            var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = query, ["limit"] = 6 });

            if (result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase))) continue;

            misses.Add($"'{query}' did not find '{expected}' among: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task A_word_inside_another_word_does_not_count()
    {
        // Every public type is named Blow..., and the graph is a flow, so "low" counted inside a word
        // would match nearly everything.
        var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = "low", ["limit"] = 50 });

        // A hit's snippet is cut around the occurrence that was counted, so every hit shows "low" as a word of its own.
        foreach (var hit in result.Hits)
        {
            Assert.IsTrue(WordStartRegex().IsMatch($"{hit.Title} {hit.Snippet}"), $"'{hit.Title}' was ranked for 'low' without the word in it: {hit.Snippet}");
        }

        Assert.IsLessThan(50, result.Hits.Length, "'low' should not match every entry that merely contains 'Blow' or 'flow'.");
    }

    [GeneratedRegex("(?<![A-Za-z0-9])[Ll]ow|(?<=[a-z])Low")]
    private static partial Regex WordStartRegex();

    [TestMethod]
    public async Task The_sections_about_the_readme_itself_are_not_answers()
    {
        foreach (var query in new[] { "mcp server tools", "documentation", "search", "setup guide" })
        {
            var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = query, ["limit"] = 50 });

            Assert.IsFalse(result.Hits.Any(hit => hit.Kind == "Guide section" && hit.Title is "MCP server" or "Documentation"), query);
        }
    }

    [TestMethod]
    public async Task The_classes_of_the_stylesheet_take_one_hit_between_them()
    {
        // They are all read through the same call, so a second one would only repeat the first.
        var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = "css class handle node", ["limit"] = 50 });

        Assert.IsLessThanOrEqualTo(1, result.Hits.Count(hit => hit.Kind == "CSS class"));
    }

    [TestMethod]
    public async Task An_example_of_the_gallery_is_found_under_the_title_the_gallery_shows()
    {
        var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = "save restore", ["limit"] = 12 });

        var example = result.Hits.Single(hit => hit.Title == "Save & Restore");

        Assert.AreEqual("Example", example.Kind);
        Assert.AreEqual("State & API", example.Context);
        Assert.AreEqual("GetBlowSourceFile(path: \"Demo/Client/Pages/Examples/SaveRestoreExample.razor\")", example.Tool);
        Assert.IsFalse(result.Hits.Any(hit => hit.Title.EndsWith("SaveRestoreExample.razor", StringComparison.Ordinal)), "The example's file should not be listed a second time under its path.");
    }

    [TestMethod]
    public async Task A_query_of_filler_words_is_told_what_to_search_for()
    {
        var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = "how do I do it" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Message);
        Assert.Contains("Search for the thing itself", result.Message);
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_falls_back_to_the_indexes()
    {
        var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = "qqqq zzzz" });

        Assert.IsEmpty(result.Hits);
        Assert.Contains("GetBlowApi", result.Message!);
    }

    [TestMethod]
    public async Task The_library_name_alone_still_finds_something()
    {
        var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = "blow" });

        Assert.IsNotEmpty(result.Hits);
    }

    [TestMethod]
    public async Task The_limit_is_honoured_and_clamped()
    {
        var three = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = "node", ["limit"] = 3 });
        var huge = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = "node", ["limit"] = 5000 });

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
            .Concat(["node", "edge", "handle", "canvas", "viewport", "layout", "theme", "css", "setup", "example", "demo page", "highlights", "error"]);

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BlowSearchResultDto>("SearchBlow", new() { ["query"] = query, ["limit"] = 50 });

            foreach (var hit in result.Hits) calls.Add(hit.Tool);
        }

        Assert.IsGreaterThan(60, calls.Count, "The searches above should open many distinct follow-up calls.");

        foreach (var call in calls)
        {
            var match = FollowUpRegex().Match(call);
            Assert.IsTrue(match.Success, $"'{call}' is not a call a caller can make verbatim.");

            var tool = match.Groups["tool"].Value;
            CollectionAssert.Contains(McpSurfaceTests.ExpectedTools, tool, $"'{call}' names a tool that does not exist.");

            var arguments = match.Groups["name"].Success
                ? new Dictionary<string, object?> { [match.Groups["name"].Value] = match.Groups["value"].Value }
                : null;

            if (tool == "InspectBlowFlow")
            {
                var report = await McpFlowLabTests.InspectAsync(null);
                Assert.IsNotNull(report.Sample, call);
                continue;
            }

            var text = await McpCall.TextAsync(tool, arguments);

            Assert.DoesNotContain("has no section called", text, call);
            Assert.DoesNotContain("has no public type called", text, call);
            Assert.DoesNotContain("declares no custom property called", text, call);
            Assert.DoesNotContain("is not a known render mode", text, call);
            Assert.DoesNotContain("No source file at", text, call);
        }
    }

    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<name>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex FollowUpRegex();
}
