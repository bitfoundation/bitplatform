using System.Text.RegularExpressions;
using Bit.BbScanner.Demo.Server.Dtos;

namespace Bit.BbScanner.Tests.Mcp;

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
        ("scan a qr code from an uploaded photo", "Scanning still images"),
        ("turn the torch on", "SetTorchAsync"),
        ("ean-13", "EAN-13"),
        ("decode images on the server", "DecodeImage"),
        ("use the decoder without blazor", "Using the decoder without Blazor"),
        ("the same barcode is reported again and again", "Duplicate"),
        ("camera permission denied", "Errors and permissions"),
        ("not supported on blazor server", "Blazor Server"),
        ("scan region", "Scan region"),
        ("reticle color", "--bit-bbscanner-reticle-color"),
        ("pdf417 status", "PDF417 status"),
        ("itf lengths", "ItfLengths"),
        ("result metadata", "BbBarcodeResultMetadata"),
        ("inspect a decoded symbol", "Inspect a decoded symbol"),
        ("replace the reticle with an overlay", "Overlay"),
        ("switch cameras", "SwitchCameraAsync"),
    ];

    [TestMethod]
    public async Task Common_questions_find_their_answer_among_the_first_hits()
    {
        // Every question is asked before anything is asserted, so one run reports every miss at once.
        var misses = new List<string>();

        foreach (var (query, expected) in _questions)
        {
            var result = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = query, ["limit"] = 6 });

            if (result.Hits.Any(hit => hit.Title.Contains(expected, StringComparison.OrdinalIgnoreCase))) continue;

            misses.Add($"'{query}' did not find '{expected}' among: {string.Join(" | ", result.Hits.Select(hit => hit.Title))}");
        }

        Assert.IsEmpty(misses, string.Join(Environment.NewLine, misses));
    }

    [TestMethod]
    public async Task A_word_inside_another_word_does_not_count()
    {
        // Every BbScanner... name has "can" inside "Scanner", so "can" counted inside a word would match all of them.
        var result = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = "can", ["limit"] = 50 });

        // A hit's snippet is cut around the occurrence that was counted, so every hit shows "can" as a word of its own.
        foreach (var hit in result.Hits)
        {
            Assert.IsTrue(WordStartRegex().IsMatch($"{hit.Title} {hit.Snippet}"), $"'{hit.Title}' was ranked for 'can' without the word in it: {hit.Snippet}");
        }
    }

    [GeneratedRegex("(?<![A-Za-z0-9])[Cc]an|(?<=[a-z])Can")]
    private static partial Regex WordStartRegex();

    [TestMethod]
    public async Task The_sections_about_the_readme_itself_are_not_answers()
    {
        foreach (var query in new[] { "mcp server tools", "documentation", "scan image", "performance" })
        {
            var result = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = query, ["limit"] = 50 });

            Assert.IsFalse(result.Hits.Any(hit => hit.Kind == "Guide section" && hit.Title is "MCP server" or "Documentation"), query);
        }
    }

    [TestMethod]
    public async Task A_query_of_filler_words_is_told_what_to_search_for()
    {
        var result = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = "how do I do it" });

        Assert.IsEmpty(result.Hits);
        Assert.IsNotNull(result.Message);
        Assert.Contains("Search for the thing itself", result.Message);
    }

    [TestMethod]
    public async Task A_query_that_matches_nothing_falls_back_to_the_indexes()
    {
        var result = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = "qqqq zzzz" });

        Assert.IsEmpty(result.Hits);
        Assert.Contains("GetBbScannerApi", result.Message!);
    }

    [TestMethod]
    public async Task The_library_name_alone_still_finds_something()
    {
        var result = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = "bbscanner" });

        Assert.IsNotEmpty(result.Hits);
    }

    [TestMethod]
    public async Task The_limit_is_honoured_and_clamped()
    {
        var three = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = "camera", ["limit"] = 3 });
        var huge = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = "camera", ["limit"] = 5000 });

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
            .Concat(["scanner", "camera", "decoder", "format", "symbology", "region", "result", "options", "setup", "css", "demo page", "torch", "decode"]);

        var calls = new HashSet<string>(StringComparer.Ordinal);

        foreach (var query in queries)
        {
            var result = await McpCall.StructuredAsync<BbScannerSearchResultDto>("SearchBbScanner", new() { ["query"] = query, ["limit"] = 50 });

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

            if (tool == "InspectBbScannerSymbol")
            {
                var report = await McpCall.StructuredAsync<BbScannerDecodeReportDto>(tool, arguments);
                Assert.IsTrue(report.Encoded, call);
                continue;
            }

            var text = await McpCall.TextAsync(tool, arguments);

            Assert.DoesNotContain("has no section called", text, call);
            Assert.DoesNotContain("has no public type called", text, call);
            Assert.DoesNotContain("reads no symbology called", text, call);
            Assert.DoesNotContain("is not a known render mode", text, call);
            Assert.DoesNotContain("No source file at", text, call);
        }
    }

    [GeneratedRegex("""^(?<tool>\w+)\((?:(?<name>\w+): "(?<value>[^"]*)")?\)$""")]
    private static partial Regex FollowUpRegex();
}
