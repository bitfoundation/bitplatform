using System.Text.RegularExpressions;
using Bit.Bortable.Demo.Server.Dtos;

namespace Bit.Bortable.Tests.Mcp;

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
        ("drag between two lists", "Shared lists"),
        ("clone items into another list", "Cloning"),
        ("drag handle", "Drag handles"),
        ("lock some items", "Filtering items"),
        ("swap two items", "Swap mode"),
        ("select several items and drag them together", "Multi-drag"),
        ("event order", "Order of events"),
        ("swap threshold", "Swap thresholds"),
        ("nested lists", "Nested lists"),
        ("read only list", "Read-only lists"),
        ("put false", "Pull and put"),
        ("ghost opacity", "--bit-bortable-ghost-opacity"),
        ("accent colour", "--bit-bortable-accent-rgb"),
        ("hidden original", "bit-bortable-hidden"),
        ("stylesheet link", "Setup checklist"),
        ("register services", "Register the services"),
        ("dragging lags on blazor server", "Blazor Server"),
        ("inspect a drag", "Inspect a Bortable drag"),
        ("coming from sortablejs", "Coming from SortableJS"),
        ("kanban board", "Shared lists"),
        ("BortablePull clone", "BortablePull.Clone"),
        ("prerendering", "Prerendering"),
    ];

    [TestMethod]
    public async Task Common_questions_find_their_answer_among_the_first_hits()
    {
        // Every question is asked before anything is asserted, so one run reports every miss at once.
        var misses = new List<string>();

        foreach (var (query, expected) in _questions)
        {
            var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = query, ["limit"] = 6 });

            if (result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase))) continue;

            misses.Add($"'{query}' did not find '{expected}' among: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task A_word_inside_another_word_does_not_count()
    {
        // "art" sits inside every OnStart and "able" inside Bortable, Sortable, Disabled and draggable;
        // counted there, both would match nearly everything.
        foreach (var word in new[] { "art", "able" })
        {
            var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = word, ["limit"] = 50 });

            // A hit's snippet is cut around the occurrence that was counted, so every hit shows the word as a word of its own.
            foreach (var hit in result.Hits)
            {
                Assert.IsTrue(new Regex($"(?<![A-Za-z0-9]){word}", RegexOptions.IgnoreCase).IsMatch($"{hit.Title} {hit.Snippet}"),
                    $"'{hit.Title}' was ranked for '{word}' without the word in it: {hit.Snippet}");
            }

            Assert.IsLessThan(10, result.Hits.Length, $"'{word}' should not match every entry that merely contains it.");
        }
    }

    [TestMethod]
    public async Task The_sections_about_the_readme_itself_are_not_answers()
    {
        foreach (var query in new[] { "mcp server tools", "documentation", "search", "setup guide" })
        {
            var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = query, ["limit"] = 50 });

            Assert.IsFalse(result.Hits.Any(hit => hit.Kind == "Guide section" && hit.Title is "MCP server" or "Documentation"), query);
        }
    }

    [TestMethod]
    public async Task A_drag_state_class_is_answered_by_the_theme_tool()
    {
        var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = "swap highlight", ["limit"] = 12 });

        var hit = result.Hits.Single(hit => hit.Kind == "CSS class" && hit.Title == "bit-bortable-swap-highlight");

        Assert.AreEqual("GetBortableTheme(name: \"bit-bortable-swap-highlight\")", hit.Tool);
        Assert.AreEqual("bit-bortable.css", hit.Context);
    }

    [TestMethod]
    public async Task A_demo_page_is_found_with_the_follow_up_that_reads_it()
    {
        var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = "nested list recursive", ["limit"] = 12 });

        var page = result.Hits.Single(hit => hit.Title == "Demo/Client/Pages/NestedList.razor");

        Assert.AreEqual("Source file", page.Kind);
        Assert.AreEqual("Page", page.Context);
        Assert.AreEqual("GetBortableSourceFile(path: \"Demo/Client/Pages/NestedList.razor\")", page.Tool);
    }

    [TestMethod]
    public async Task A_query_of_filler_words_is_told_what_to_search_for()
    {
        var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = "how do I do it" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Message);
        Assert.Contains("Search for the thing itself", result.Message);
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_falls_back_to_the_indexes()
    {
        var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = "qqqq zzzz" });

        Assert.IsEmpty(result.Hits);
        Assert.Contains("GetBortableApi", result.Message!);
        Assert.Contains("Pull clone", result.Message!);
    }

    [TestMethod]
    public async Task The_library_name_alone_still_finds_something()
    {
        var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = "bortable" });

        Assert.IsNotEmpty(result.Hits);
    }

    [TestMethod]
    public async Task The_limit_is_honoured_and_clamped()
    {
        var three = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = "item", ["limit"] = 3 });
        var huge = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = "item", ["limit"] = 5000 });

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
            .Concat(["item", "list", "drag", "group", "clone", "swap", "event", "theme", "css", "class", "setup", "demo page", "highlights", "troubleshooting"]);

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BortableSearchResultDto>("SearchBortable", new() { ["query"] = query, ["limit"] = 50 });

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

            if (tool == "InspectBortableDrag")
            {
                var report = await McpDragLabTests.InspectAsync(null);
                Assert.IsTrue(report.Valid, call);
                continue;
            }

            var text = await McpCall.TextAsync(tool, arguments);

            Assert.DoesNotContain("has no section called", text, call);
            Assert.DoesNotContain("has no public type called", text, call);
            Assert.DoesNotContain("has no custom property or class called", text, call);
            Assert.DoesNotContain("is not a known render mode", text, call);
            Assert.DoesNotContain("No source file at", text, call);
        }
    }

    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<name>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex FollowUpRegex();
}
