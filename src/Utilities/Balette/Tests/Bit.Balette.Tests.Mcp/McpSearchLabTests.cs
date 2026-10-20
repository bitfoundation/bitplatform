using System.Text.Json;
using Bit.Balette.Demo.Server.Dtos;
using Bit.Balette.Demo.Server.Services;

namespace Bit.Balette.Tests.Mcp;

/// <summary>
/// InspectBaletteSearch on the questions it exists for: why does this command rank first, why is that
/// one missing, what did each term match - over commands passed in, and over this demo's own registry.
/// The library's own unit tests pin the matcher; these pin what the tool says about it.
/// </summary>
[TestClass]
public class McpSearchLabTests
{
    private static readonly string _settings = JsonSerializer.Serialize(new[]
    {
        new { title = "Go to Settings", group = "Navigation" },
        new { title = "Close tabs", group = "View" },
        new { title = "Open logs", group = "View" },
        new { title = "Toggle sidebar", group = "View" },
    });

    [TestMethod]
    public async Task Gs_ranks_go_to_settings_first_with_its_word_starts_highlighted()
    {
        var report = await InspectAsync("gs", _settings);

        var first = report.Results[0];

        Assert.AreEqual("Go to Settings", first.Title);
        Assert.IsTrue(first.Matched);
        Assert.AreEqual(1, first.Order);
        Assert.AreEqual(1, first.Row);
        CollectionAssert.AreEqual(new[] { 0, 6 }, first.TitlePositions);
        Assert.AreEqual("[G]o to [S]ettings", first.HighlightedTitle);
        Assert.AreEqual("title", first.MatchedOn);
        Assert.AreEqual(first.Score, first.Rank, "With priority 0 and no history, the rank is the score.");
    }

    [TestMethod]
    public async Task Only_a_query_searches_the_demos_own_registry()
    {
        // The link on the demo's MCP page, and the first thing its home page tells a visitor to type.
        var report = await InspectAsync("rc");

        Assert.AreEqual(BaletteSearchLab.DemoRegistry.Count, report.Searched);
        Assert.Contains("this demo's registry", report.Commands);
        Assert.AreEqual("Registering commands", report.Results[0].Title);
        Assert.AreEqual("[R]egistering [c]ommands", report.Results[0].HighlightedTitle);
        Assert.AreEqual("fuzzy", report.Terms.Single().Operator);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("ln(1 + frecency)", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_negated_term_names_the_field_that_excluded_the_command()
    {
        var report = await InspectAsync("work !delete", """["Delete workspace", "Rename workspace"]""");

        var delete = report.Results.Single(result => result.Title == "Delete workspace");
        var rename = report.Results.Single(result => result.Title == "Rename workspace");

        Assert.IsFalse(delete.Matched);
        Assert.Contains("'!delete' excludes it", delete.Reason!);
        Assert.Contains("title", delete.Reason!);
        Assert.IsTrue(rename.Matched);
        Assert.AreEqual("negate", report.Terms[1].Operator);
        Assert.AreEqual("delete", report.Terms[1].Term);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("negation", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task The_operators_are_read_and_explained()
    {
        var commands = """["Go to Settings", "Settings", "Set timer"]""";

        var prefix = await InspectAsync("^set", commands);
        var suffix = await InspectAsync("ings$", commands);
        var exact = await InspectAsync("'to set", commands);

        Assert.AreEqual("prefix", prefix.Terms.Single().Operator);
        CollectionAssert.AreEquivalent(new[] { "Settings", "Set timer" }, Matched(prefix));
        Assert.Contains("no field starts with 'set'", prefix.Results.Single(result => result.Title == "Go to Settings").Reason!);

        Assert.AreEqual("suffix", suffix.Terms.Single().Operator);
        CollectionAssert.AreEquivalent(new[] { "Go to Settings", "Settings" }, Matched(suffix));

        Assert.AreEqual("exact", exact.Terms[0].Operator);
        CollectionAssert.AreEquivalent(new[] { "Go to Settings" }, Matched(exact));
        Assert.HasCount(2, exact.Terms);
    }

    [TestMethod]
    public async Task A_capital_letter_makes_a_term_case_sensitive_and_the_reason_says_so()
    {
        var lower = await InspectAsync("mcp");
        var upper = await InspectAsync("Mcp");

        Assert.Contains("MCP server", Matched(lower));
        Assert.DoesNotContain("MCP server", Matched(upper));
        Assert.IsTrue(upper.Terms.Single().CaseSensitive);

        var reason = upper.Results.Single(result => result.Title == "MCP server").Reason!;
        Assert.Contains("smart case", reason);

        var ignoringCase = await InspectAsync("Mcp", matcher: "fuzzy-ignore-case");
        Assert.Contains("MCP server", Matched(ignoringCase));
        Assert.IsFalse(ignoringCase.Terms.Single().CaseSensitive);
    }

    [TestMethod]
    public async Task A_keyword_hit_is_reported_as_the_keyword()
    {
        var report = await InspectAsync("landing");

        var introduction = report.Results.Single(result => result.Title == "Introduction");

        Assert.IsTrue(introduction.Matched);
        Assert.AreEqual("landing", introduction.MatchedKeyword);
        Assert.AreEqual("keyword 'landing'", introduction.MatchedOn);
        Assert.IsNull(introduction.TitlePositions, "A keyword hit highlights nothing in the title.");
    }

    [TestMethod]
    public async Task Several_terms_are_anded_and_each_ones_share_is_shown()
    {
        var report = await InspectAsync("wrap word");

        var wordWrap = report.Results.Single(result => result.Title == "Word wrap");

        Assert.IsTrue(wordWrap.Matched);
        Assert.HasCount(2, wordWrap.Terms!);
        Assert.AreEqual(wordWrap.Score!.Value, wordWrap.Terms!.Sum(term => term.Score!.Value), 0.01, "The command's score is the sum of its terms' scores.");
        Assert.AreEqual("[Word] [wrap]", wordWrap.HighlightedTitle);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("(AND)", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_term_won_by_the_group_the_description_or_a_tag_names_that_field()
    {
        var commands = JsonSerializer.Serialize(new[]
        {
            new { title = "Alpha", group = "Danger zone", description = "Removes everything", tags = new[] { "maintenance" } },
        });

        Assert.AreEqual("group", (await InspectAsync("danger", commands)).Results[0].MatchedOn);
        Assert.AreEqual("description", (await InspectAsync("removes", commands)).Results[0].MatchedOn);
        Assert.AreEqual("tag 'maintenance'", (await InspectAsync("maintenance", commands)).Results[0].MatchedOn);
    }

    [TestMethod]
    public async Task Priority_is_added_at_its_weight()
    {
        var commands = """[{ "title": "Open file" }, { "title": "Open file", "id": "pinned-open", "priority": "High" }]""";

        var report = await InspectAsync("open", commands);

        var high = report.Results.Single(result => result.Id == "pinned-open");
        var normal = report.Results.Single(result => result.Id != "pinned-open");
        var weight = new BaletteOptions().PriorityWeight;

        Assert.AreEqual(BaletteCommandPriority.High, high.Priority);
        Assert.AreEqual(1, high.Order);
        Assert.AreEqual(normal.Score!.Value, high.Score!.Value, 0.001);
        Assert.AreEqual(BaletteCommandPriority.High * weight, high.Rank!.Value - normal.Rank!.Value, 0.001);
    }

    [TestMethod]
    public async Task The_empty_query_is_ordered_by_priority_under_the_demos_group_order()
    {
        var report = await InspectAsync(string.Empty);

        var first = report.Results.Single(result => result.Row == 1);

        Assert.AreEqual(BaletteSearchLab.DemoOptions.GroupOrder[0], first.Group);
        Assert.AreEqual("Introduction", first.Title, "Introduction is the one High-priority command of the first group.");
        Assert.AreEqual(BaletteCommandPriority.High * 1000d, first.Rank!.Value);
        Assert.IsEmpty(report.Terms);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("priority x 1000", StringComparison.Ordinal)));

        // The groups come in options.GroupOrder order.
        var groups = report.Results.Where(result => result.Row is not null).OrderBy(result => result.Row).Select(result => result.Group!).Distinct().ToArray();
        CollectionAssert.AreEqual(BaletteSearchLab.DemoOptions.GroupOrder.Where(groups.Contains).ToArray(), groups);
    }

    [TestMethod]
    public async Task Without_grouping_the_row_is_the_rank()
    {
        var report = await InspectAsync("e", groupWhenSearching: false, limit: 200);

        foreach (var result in report.Results.Where(result => result.Row is not null))
        {
            Assert.AreEqual(result.Order, result.Row, result.Title);
        }
    }

    [TestMethod]
    public async Task With_grouping_the_rows_follow_the_groups()
    {
        var report = await InspectAsync("e", limit: 200);

        var listed = report.Results.Where(result => result.Row is not null).OrderBy(result => result.Row).ToArray();

        // Each group is one contiguous run of rows.
        var runs = listed.Select(result => result.Group).Where((group, index) => index == 0 || group != listed[index - 1].Group).ToArray();
        Assert.HasCount(runs.Distinct().Count(), runs);
    }

    [TestMethod]
    public async Task An_always_shown_command_is_listed_without_matching()
    {
        var report = await InspectAsync("zzzz");

        var feedback = report.Results.Single(result => result.Title.StartsWith("Feedback", StringComparison.Ordinal));

        Assert.IsFalse(feedback.Matched);
        Assert.IsTrue(feedback.AlwaysShown);
        Assert.IsNotNull(feedback.Row);
        Assert.IsNull(feedback.Score, "Minus infinity is not a number JSON can carry.");
        Assert.AreEqual(0, report.Matched);
    }

    [TestMethod]
    public async Task The_substring_matcher_has_no_typo_tolerance()
    {
        var fuzzy = await InspectAsync("sts", """["Settings"]""");
        var substring = await InspectAsync("sts", """["Settings"]""", matcher: "substring");

        Assert.IsTrue(fuzzy.Results.Single().Matched);
        Assert.IsFalse(substring.Results.Single().Matched);
        Assert.AreEqual("substring", substring.Matcher);
        Assert.AreEqual("substring", substring.Terms.Single().Operator);
    }

    [TestMethod]
    public async Task Plain_titles_one_per_line_are_commands_too()
    {
        var report = await InspectAsync("gs", "Go to Settings\nClose tabs\n\nOpen logs");

        Assert.AreEqual(3, report.Searched);
        Assert.AreEqual("Go to Settings", report.Results[0].Title);
    }

    [TestMethod]
    public async Task Commands_that_are_not_json_are_explained_rather_than_guessed()
    {
        var report = await InspectAsync("gs", "[{ not json");

        Assert.AreEqual(0, report.Searched);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("not valid JSON", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_mode_prefix_searches_that_mode_of_the_demo()
    {
        var report = await InspectAsync(">wrap");

        // The '>' mode lists only the commands tagged "command": no navigation, and the toggle among them.
        Assert.IsLessThan(BaletteSearchLab.DemoRegistry.Count, report.Searched);
        Assert.Contains("Word wrap", Matched(report));
        Assert.IsFalse(report.Results.Any(result => result.Group == "Navigation"));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'Commands' mode", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task The_limit_cuts_the_report_and_says_so()
    {
        var report = await InspectAsync("e", limit: 2);

        Assert.HasCount(2, report.Results);
        Assert.IsTrue(report.HasMore);
    }

    internal static Task<BaletteSearchInspectionDto> InspectAsync(string query, string? commands = null, string? matcher = null, bool? groupWhenSearching = null, int? limit = null)
    {
        var arguments = new Dictionary<string, object?> { ["query"] = query };
        if (commands is not null) arguments["commands"] = commands;
        if (matcher is not null) arguments["matcher"] = matcher;
        if (groupWhenSearching is not null) arguments["groupWhenSearching"] = groupWhenSearching;
        if (limit is not null) arguments["limit"] = limit;

        return McpCall.StructuredAsync<BaletteSearchInspectionDto>("InspectBaletteSearch", arguments);
    }

    private static string[] Matched(BaletteSearchInspectionDto report) => [.. report.Results.Where(result => result.Matched).Select(result => result.Title)];
}
