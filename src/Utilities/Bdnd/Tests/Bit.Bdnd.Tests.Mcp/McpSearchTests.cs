using System.Text.RegularExpressions;
using Bit.Bdnd.Demo.Server.Dtos;

namespace Bit.Bdnd.Tests.Mcp;

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
        ("sortable list", "Sortable lists"),
        ("drop files from desktop", "Files"),
        ("custom drag preview", "Custom drag layers"),
        ("drag handle", "Drag handles"),
        ("nested drop targets", "Nested drop targets"),
        ("Blazor Server", "Blazor Server"),
        ("drop result", "Drop results"),
        ("register services", "Register the service"),
        ("coming from react dnd", "Coming from React DnD"),
        ("useDrop", "Coming from React DnD"),
        ("text links and html", "Text, links and HTML"),
        ("chessboard", "Chessboard Tutorial"),
        ("copy or move", "Dustbin - Copy or Move"),
        ("cancel on drop outside", "Sortable - Cancel on Drop Outside"),
        ("highlight only the innermost target", "Nested drop targets"),
        ("can drop predicate", "Gating drops with CanDropPredicate"),
        ("inspect a drop", "Inspect a Bdnd drop"),
        ("monitor offsets", "The monitor"),
        ("stylesheet", "Setup checklist"),
        ("OnDrop", "BdndDropTarget.OnDrop"),
    ];

    [TestMethod]
    public async Task Common_questions_find_their_answer_among_the_first_hits()
    {
        // Every question is asked before anything is asserted, so one run reports every miss at once.
        var misses = new List<string>();

        foreach (var (query, expected) in _questions)
        {
            var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = query, ["limit"] = 6 });

            if (result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase))) continue;

            misses.Add($"'{query}' did not find '{expected}' among: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task The_guide_section_written_to_answer_ranks_first()
    {
        // Among the first hits is not enough for the questions the guide has a section of that very
        // name for: the explanation has to come before the members and pages that mention it.
        foreach (var (query, expected) in new[] { ("sortable list", "Sortable lists"), ("custom drag preview", "Custom drag layers"), ("drag handle", "Drag handles"), ("nested drop targets", "Nested drop targets"), ("drop result", "Drop results") })
        {
            var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = query, ["limit"] = 3 });

            Assert.AreEqual(expected, result.Hits[0].Title, query);
            Assert.AreEqual("Guide section", result.Hits[0].Kind, query);
        }
    }

    [TestMethod]
    public async Task A_word_inside_another_word_does_not_count()
    {
        // Every other title of this library says "drag", so "rag" counted inside a word would match nearly everything.
        var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "rag", ["limit"] = 50 });

        // A hit's snippet is cut around the occurrence that was counted, so every hit shows "rag" as a word of its own.
        foreach (var hit in result.Hits)
        {
            Assert.IsTrue(WordStartRegex().IsMatch($"{hit.Title} {hit.Snippet}"), $"'{hit.Title}' was ranked for 'rag' without the word in it: {hit.Snippet}");
        }

        Assert.IsLessThan(10, result.Hits.Length, "'rag' should not match every entry that merely contains 'drag'.");
    }

    [GeneratedRegex("(?<![A-Za-z0-9])[Rr]ag|(?<=[a-z])Rag")]
    private static partial Regex WordStartRegex();

    [TestMethod]
    public async Task The_sections_about_the_readme_itself_are_not_answers()
    {
        foreach (var query in new[] { "mcp server tools", "documentation", "search", "setup guide" })
        {
            var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = query, ["limit"] = 50 });

            Assert.IsFalse(result.Hits.Any(hit => hit.Kind == "Guide section" && hit.Title is "MCP server" or "Documentation"), query);
        }
    }

    [TestMethod]
    public async Task A_demo_page_is_found_under_the_heading_it_shows()
    {
        var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "nesting drop targets", ["limit"] = 12 });

        var page = result.Hits.Single(hit => hit.Title == "Nesting - Drop Targets");

        Assert.AreEqual("Demo page", page.Kind);
        Assert.AreEqual("/nesting/drop-targets", page.Context);
        Assert.AreEqual("GetBdndSourceFile(path: \"Demo/Client/Pages/Nesting/DropTargets.razor\")", page.Tool);
        Assert.IsFalse(result.Hits.Any(hit => hit.Title.EndsWith("DropTargets.razor", StringComparison.Ordinal)), "The page should not be listed a second time under its path.");
    }

    [TestMethod]
    public async Task A_generic_type_is_found_and_read_by_its_plain_name()
    {
        var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "BdndDropTarget", ["limit"] = 6 });

        var type = result.Hits.Single(hit => hit.Kind == "API component" && hit.Title == "BdndDropTarget");

        Assert.AreEqual("GetBdndApi(typeName: \"BdndDropTarget\")", type.Tool);
    }

    [TestMethod]
    public async Task The_scripts_entry_points_are_not_offered_as_api()
    {
        // HandleDragEnd and the rest are public only for JS interop; a search must not hand them out as members to call.
        var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "HandleDragEnd HandleHoverChanged HandleNativeDrop", ["limit"] = 50 });

        Assert.IsFalse(result.Hits.Any(hit => hit.Kind.StartsWith("API", StringComparison.Ordinal) && hit.Title.Contains(".Handle", StringComparison.Ordinal)),
            string.Join(" | ", result.Hits.Select(hit => hit.Title)));
    }

    [TestMethod]
    public async Task A_query_of_filler_words_is_told_what_to_search_for()
    {
        var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "how do I do it" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Message);
        Assert.Contains("Search for the thing itself", result.Message);
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_falls_back_to_the_indexes()
    {
        var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "qqqq zzzz" });

        Assert.IsEmpty(result.Hits);
        Assert.Contains("GetBdndApi", result.Message!);
        Assert.Contains("BdndDropTarget", result.Message!, "The synonyms a React DnD user reaches for should be named.");
    }

    [TestMethod]
    public async Task A_near_miss_is_suggested_when_nothing_matches()
    {
        // Nothing says "handlebar", but it begins with the name of the grip component.
        var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "handlebar" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.DidYouMean);
        Assert.IsTrue(result.DidYouMean.Any(title => title.Contains("Handle", StringComparison.Ordinal)), string.Join(" | ", result.DidYouMean));
    }

    [TestMethod]
    public async Task The_library_name_alone_still_finds_something()
    {
        foreach (var query in new[] { "bdnd", "drag and drop" })
        {
            var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = query });

            Assert.IsNotEmpty(result.Hits, query);
        }
    }

    [TestMethod]
    public async Task The_limit_is_honoured_and_clamped()
    {
        var three = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "target", ["limit"] = 3 });
        var huge = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = "target", ["limit"] = 5000 });

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
            .Concat(["draggable", "target", "item", "type", "monitor", "provider", "layer", "native", "setup", "demo page", "source", "offset", "result"]);

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BdndSearchResultDto>("SearchBdnd", new() { ["query"] = query, ["limit"] = 50 });

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

            if (tool == "InspectBdndDrop")
            {
                var report = await McpDropLabTests.InspectAsync(null);
                Assert.IsNotNull(report.Sample, call);
                continue;
            }

            var text = await McpCall.TextAsync(tool, arguments);

            Assert.DoesNotContain("has no section called", text, call);
            Assert.DoesNotContain("has no public type called", text, call);
            Assert.DoesNotContain("is not a known render mode", text, call);
            Assert.DoesNotContain("No source file at", text, call);
        }
    }

    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<name>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex FollowUpRegex();
}
