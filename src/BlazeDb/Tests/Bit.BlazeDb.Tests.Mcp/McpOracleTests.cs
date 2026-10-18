using System.Net;
using System.Text.RegularExpressions;
using Bit.BlazeDb.Demo.Server.Services;

namespace Bit.BlazeDb.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the API index against the assemblies, the
/// guide sections against the README, the diagnostics table against the generator and the guide's own
/// copy of it, and the HTTP mirror against the protocol it mirrors. No expected answers are written down
/// here - the library, its files and the protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    [TestMethod]
    public void Every_exported_type_is_in_the_api_index()
    {
        var exported = BlazeDbApiCatalog.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsNested is false)
            .Select(BlazeDbApiCatalog.FriendlyName)
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BlazeDbApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
    }

    [TestMethod]
    public void Every_documented_public_type_has_its_summary_in_the_index()
    {
        // The XML documentation files have to reach the server's output folder for any of this to work;
        // a build that stops copying them leaves every summary empty without failing anything else.
        foreach (var package in BlazeDbApiCatalog.Types.GroupBy(type => type.Package))
        {
            var withSummary = package.Count(type => type.Summary is not null);

            Assert.IsGreaterThan(package.Count() * 8 / 10, withSummary, $"Most public types of {package.Key} should come with their XML summary.");
        }
    }

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BlazeDbSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
        }

        CollectionAssert.AreEqual(headings, BlazeDbSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BlazeDbSourceCatalog.GuideSections.Length, BlazeDbSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public void The_readme_s_diagnostics_table_is_the_generator_s()
    {
        // The guide carries its own copy of the table for a reader on GitHub or nuget.org; the generator
        // is what a build reports, so a diagnostic added or retitled there has to land here too.
        var section = BlazeDbSourceCatalog.GetGuideSection("Compile-time diagnostics")!;

        foreach (var descriptor in BlazeDbDiagnosticsReference.Descriptors)
        {
            Assert.Contains($"| {descriptor.Id} | {descriptor.DefaultSeverity} | {descriptor.Title} |", section);
        }

        Assert.HasCount(BlazeDbDiagnosticsReference.Descriptors.Length, TableRowRegex().Matches(section));
    }

    [TestMethod]
    public void Every_diagnostic_of_the_generator_is_tracked_as_a_release()
    {
        // RS2008 checks the tracking files against the descriptors inside the generator's own build; this
        // holds the catalog the docs are written from to the same set.
        CollectionAssert.AreEquivalent(
            BlazeDbDiagnosticsReference.Descriptors.Select(descriptor => descriptor.Id).ToArray(),
            BlazeDbDiagnosticsReference.Entries.Where(entry => entry.Doc is not null).Select(entry => entry.Id).ToArray());
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBlazeDb", "?query=opfs&limit=3", new() { ["query"] = "opfs", ["limit"] = 3 }),
            ("GetBlazeDbSetupGuide", "?hostingModel=server", new() { ["hostingModel"] = "server" }),
            ("GetBlazeDbGuideSection", "?heading=Storage%20quota", new() { ["heading"] = "Storage quota" }),
            ("GetBlazeDbApi", "?typeName=BlazeDbDatabase", new() { ["typeName"] = "BlazeDbDatabase" }),
            ("GetBlazeDbDiagnostics", "?code=BLZ001", new() { ["code"] = "BLZ001" }),
            ("InspectBlazeDbTable", "?source=%5BBlazeDbTable%5D%20public%20partial%20class%20T%20%7B%20%5BBlazeDbKey%5D%20public%20int%20Id%20%7B%20get%3B%20set%3B%20%7D%20%7D", null),
            ("GetBlazeDbSourceFile", "?path=Demo/Client/Data/DemoModels.cs", new() { ["path"] = "Demo/Client/Data/DemoModels.cs" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBlazeDb" or "InspectBlazeDbTable")
            {
                Assert.StartsWith("{", body, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^\| BLZ\d{3} \|", RegexOptions.Multiline)]
    private static partial Regex TableRowRegex();
}
