using System.Text.RegularExpressions;
using Bit.Bonboarding.Demo.Server.Dtos;

namespace Bit.Bonboarding.Tests.Mcp;

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
        ("advance when the user clicks the button", "AdvanceOn"),
        ("element not rendered yet", "Dynamic and async targets"),
        ("different steps for admins", "Conditional"),
        ("tour across several pages", "Multi-page tours"),
        ("resume progress after reload", "Persistence"),
        ("dark theme colours", "Theme variable catalog"),
        ("spotlight padding", "SpotlightPadding"),
        ("custom popover markup headless", "Headless and custom rendering"),
        ("right to left", "Right-to-left"),
    ];

    [TestMethod]
    public async Task Common_questions_find_their_answer_among_the_first_hits()
    {
        foreach (var (query, expected) in _questions)
        {
            var result = await McpCall.StructuredAsync<BonboardingSearchResultDto>("SearchBonboarding", new() { ["query"] = query, ["limit"] = 6 });

            Assert.IsTrue(result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase)),
                $"'{query}' did not find '{expected}' among: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }
    }

    /// <summary>
    /// Questions that say what the app has to do rather than what the feature is called, and the titles
    /// one of which has to be the first hit: an agent that trusts the first hit goes wherever it points.
    /// </summary>
    private static readonly (string Query, string[] Answers)[] _firstHits =
    [
        ("a step that waits for a panel that loads later", ["How waiting works", "Missing-target strategies"]),
        ("wait for an element that appears later", ["How waiting works", "Missing-target strategies"]),
        ("start a tour from code", ["Start it"]),
        ("tour across several pages", ["Multi-page tours"]),
    ];

    [TestMethod]
    public async Task A_question_in_plain_words_puts_its_answer_first()
    {
        var misses = new List<string>();

        // However few hits were asked for, the first one is the same.
        foreach (var (query, answers) in _firstHits)
        {
            foreach (var limit in new[] { 1, 3, 6 })
            {
                var result = await McpCall.StructuredAsync<BonboardingSearchResultDto>("SearchBonboarding", new() { ["query"] = query, ["limit"] = limit });
                var first = result.Hits.FirstOrDefault()?.Title ?? string.Empty;

                if (answers.Any(answer => first.Contains(answer, StringComparison.OrdinalIgnoreCase))) continue;

                misses.Add($"'{query}' (limit {limit}) ranked first: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
            }
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task A_member_every_type_has_is_not_an_answer()
    {
        // Split into words, GetHashCode is "code": left in, it answered "start a tour from code".
        var result = await McpCall.StructuredAsync<BonboardingSearchResultDto>("SearchBonboarding", new() { ["query"] = "code", ["limit"] = 50 });

        Assert.IsFalse(result.Hits.Any(hit => hit.Title.EndsWith(".GetHashCode", StringComparison.Ordinal) || hit.Title.EndsWith(".Equals", StringComparison.Ordinal)),
            string.Join(" | ", result.Hits.Select(hit => hit.Title)));
    }

    [TestMethod]
    public async Task A_query_of_filler_words_is_told_what_to_search_for()
    {
        var result = await McpCall.StructuredAsync<BonboardingSearchResultDto>("SearchBonboarding", new() { ["query"] = "how do I do it" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Message);
        Assert.Contains("Search for the thing itself", result.Message);
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_falls_back_to_the_indexes()
    {
        var result = await McpCall.StructuredAsync<BonboardingSearchResultDto>("SearchBonboarding", new() { ["query"] = "qqqq zzzz" });

        Assert.IsEmpty(result.Hits);
        Assert.Contains("GetBonboardingApi", result.Message!);
    }

    [TestMethod]
    public async Task The_limit_is_honoured_and_clamped()
    {
        var three = await McpCall.StructuredAsync<BonboardingSearchResultDto>("SearchBonboarding", new() { ["query"] = "step", ["limit"] = 3 });
        var huge = await McpCall.StructuredAsync<BonboardingSearchResultDto>("SearchBonboarding", new() { ["query"] = "step", ["limit"] = 5000 });

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
            .Concat(["step", "tour", "theme", "placement", "store", "label", "event", "route", "focus", "demo page"]);

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BonboardingSearchResultDto>("SearchBonboarding", new() { ["query"] = query, ["limit"] = 50 });

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

            var text = await McpCall.TextAsync(tool, arguments);

            Assert.DoesNotContain("has no section called", text, call);
            Assert.DoesNotContain("has no public type called", text, call);
            Assert.DoesNotContain("No source file at", text, call);
        }
    }

    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<name>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex FollowUpRegex();
}
