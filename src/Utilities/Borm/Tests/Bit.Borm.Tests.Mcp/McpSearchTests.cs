using System.Text.RegularExpressions;
using Bit.Borm.Demo.Server.Dtos;

namespace Bit.Borm.Tests.Mcp;

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
        ("a field that only shows when a checkbox is ticked", "Conditional fields and wizards"),
        ("a field that only shows when a checkbox is ticked", "VisibleWhen"),
        ("load a dropdown's options from a service when another field changes", "Async and cascading options"),
        ("load a dropdown's options from a service when another field changes", "OptionsFrom"),
        ("import a JSON Schema from a user safely", "Untrusted documents"),
        ("import a JSON Schema from a user safely", "TryImport"),
        ("make two forms on one page not share ids", "Ids on the page"),
        ("make two forms on one page not share ids", "IdPrefix"),
        ("wizard steps", "Wizards"),
        ("add and remove repeater rows", "AddArrayItem"),
        ("computed total", "Computed values"),
        ("fluentvalidation", "FluentValidation"),
        ("dark mode colours", "Theme property catalog"),
        ("error colour", "--bit-borm-error"),
        ("custom renderer", "Custom renderers"),
        ("trim whitespace", "Tidying a value"),
        ("inspect a json schema", "Inspect a JSON Schema"),
        ("stylesheet link", "Setup checklist"),
        ("no registered service IBormFieldRendererRegistry", "Setup checklist"),
        ("date range", "Date and time windows"),
        ("file upload", "File upload"),
        ("combobox", "Combobox and tags"),
        ("unsaved changes", "Editing an existing record"),
        ("server errors after submit", "SetServerError"),
        ("validate on blur", "When validation runs"),
        ("per row conditions in a repeater", "Per-row conditions"),
        ("two columns", "Columns"),
        ("translate the messages", "Localization"),
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
            var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = query, ["limit"] = 6 });

            if (result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase))) continue;

            misses.Add($"'{query}' did not find '{expected}' among: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task A_word_inside_another_word_does_not_count()
    {
        // Every public type is named Borm..., so "orm" counted inside a word would match nearly everything.
        var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "orm", ["limit"] = 50 });

        // A hit's snippet is cut around the occurrence that was counted, so every hit shows "orm" as a word of its own.
        foreach (var hit in result.Hits)
        {
            Assert.IsTrue(WordStartRegex().IsMatch($"{hit.Title} {hit.Snippet}"), $"'{hit.Title}' was ranked for 'orm' without the word in it: {hit.Snippet}");
        }

        Assert.IsLessThan(50, result.Hits.Length, "'orm' should not match every entry that merely contains 'Borm' or 'form'.");
    }

    [GeneratedRegex("(?<![A-Za-z0-9])[Oo]rm|(?<=[a-z])Orm")]
    private static partial Regex WordStartRegex();

    [TestMethod]
    public async Task The_sections_about_the_readme_the_demo_and_this_server_are_not_answers()
    {
        foreach (var query in new[] { "mcp server tools", "documentation", "search", "setup guide", "demo pages" })
        {
            var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = query, ["limit"] = 50 });

            Assert.IsFalse(result.Hits.Any(hit => hit.Kind == "Guide section" && hit.Title is "MCP server" or "Documentation" or "The demo"), query);
        }
    }

    [TestMethod]
    public async Task The_classes_of_the_stylesheet_take_one_hit_between_them()
    {
        // They are all read through the same call, so a second one would only repeat the first.
        var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "css class field input invalid", ["limit"] = 50 });

        Assert.IsLessThanOrEqualTo(1, result.Hits.Count(hit => hit.Kind == "CSS class"));
    }

    [TestMethod]
    public async Task A_demo_page_is_found_under_the_heading_it_shows()
    {
        var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "per row conditions", ["limit"] = 12 });

        var page = result.Hits.Single(hit => hit.Kind == "Demo page" && hit.Title == "Per-row conditions");

        Assert.AreEqual("/per-row-conditions", page.Context);
        Assert.AreEqual("GetBormSourceFile(path: \"Demo/Client/Pages/PerRowConditionsDemo.razor\")", page.Tool);
        Assert.IsFalse(result.Hits.Any(hit => hit.Title.EndsWith("PerRowConditionsDemo.razor", StringComparison.Ordinal)), "The page's file should not be listed a second time under its path.");
    }

    [TestMethod]
    public async Task The_askers_words_find_the_librarys_words()
    {
        // "Shows" is VisibleWhen, "ticked" is IsTrue, a "dropdown" is a Select: a question is written in
        // the asker's vocabulary and the answer in the library's.
        var shows = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "field shows", ["limit"] = 12 });
        var dropdown = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "dropdown", ["limit"] = 12 });

        Assert.IsTrue(shows.Hits.Any(hit => hit.Title.Contains("VisibleWhen", StringComparison.Ordinal)), string.Join(" | ", shows.Hits.Select(hit => hit.Title)));
        Assert.IsTrue(dropdown.Hits.Any(hit => hit.Title.Contains("Select", StringComparison.Ordinal)), string.Join(" | ", dropdown.Hits.Select(hit => hit.Title)));
    }

    [TestMethod]
    public async Task A_query_of_filler_words_is_told_what_to_search_for()
    {
        var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "how do I do it" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Message);
        Assert.Contains("Search for the thing itself", result.Message);
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_falls_back_to_the_indexes()
    {
        var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "qqqq zzzz" });

        Assert.IsEmpty(result.Hits);
        Assert.Contains("GetBormApi", result.Message!);
    }

    [TestMethod]
    public async Task The_library_name_alone_still_finds_something()
    {
        var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "borm" });

        Assert.IsNotEmpty(result.Hits);
    }

    [TestMethod]
    public async Task The_limit_is_honoured_and_clamped()
    {
        var three = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "field", ["limit"] = 3 });
        var huge = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = "field", ["limit"] = 5000 });

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
            .Concat(["field", "rule", "condition", "state", "builder", "options", "wizard", "theme", "css", "setup", "demo page", "highlights", "error", "json schema"]);

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BormSearchResultDto>("SearchBorm", new() { ["query"] = query, ["limit"] = 50 });

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

            if (tool == "InspectBormSchema")
            {
                var report = await McpSchemaLabTests.InspectAsync(null);
                Assert.IsNotNull(report.Sample, call);
                continue;
            }

            var text = await McpCall.TextAsync(tool, arguments);

            Assert.DoesNotContain("has no section called", text, call);
            Assert.DoesNotContain("has no public type called", text, call);
            Assert.DoesNotContain("declares no custom property called", text, call);
            Assert.DoesNotContain("is not a known hosting model", text, call);
            Assert.DoesNotContain("No source file at", text, call);
        }
    }

    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<name>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex FollowUpRegex();
}
