using Bit.Bswup.Demo.Client;
using Bit.Bswup.Tests.Mcp.TestInfra;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// Every documentation page, rendered the way the tool renders it.
/// <para>
/// This is the part of the server most likely to break without anyone noticing. The pages are the
/// site's own components, rendered here OUTSIDE the router and the layout, so a page that reaches
/// for the navigation manager, for JS interop or for a cascading parameter while initializing
/// renders fine for a human and throws for an agent - and the tool answers with a placeholder that
/// still looks like a successful call. Adding a page is enough to introduce it, which is exactly
/// why this walks the whole catalog instead of a sample of it.
/// </para>
/// </summary>
[TestClass]
public class DocsPageTests
{
    private static McpTestServer _server = null!;

    [ClassInitialize]
    public static async Task StartAsync(TestContext _) => _server = await McpTestServer.StartAsync();

    [ClassCleanup]
    public static async Task StopAsync() => await _server.DisposeAsync();

    public static IEnumerable<object?[]> AllPages =>
        DocsCatalog.AllPages.Select(page => new object?[] { page.Slug.Length == 0 ? null : page.Slug, page.Title });

    [TestMethod]
    [DynamicData(nameof(AllPages))]
    public async Task EveryDocumentationPage_RendersToMarkdownOnTheServer(string? slug, string title)
    {
        var text = await _server.CallTextAsync("GetBswupDocsPage", new Dictionary<string, object?> { ["slug"] = slug });

        Assert.IsFalse(text.Contains("could not be rendered on the server", StringComparison.Ordinal),
            $"'{title}' failed to render outside the router: {text}");

        StringAssert.StartsWith(text, "bit Bswup documentation page: ", title);
        StringAssert.Contains(text, "# ", $"'{title}' rendered without a single heading - it is probably empty");
        Assert.IsTrue(text.Length > 400, $"'{title}' rendered to only {text.Length} characters");
    }

    [TestMethod]
    [DynamicData(nameof(AllPages))]
    public async Task EveryDocumentationPage_FitsInAClientsContextBudget(string? slug, string title)
    {
        var text = await _server.CallTextAsync("GetBswupDocsPage", new Dictionary<string, object?> { ["slug"] = slug });

        // A page that reaches the server's document cap is served truncated, with nothing naming
        // the rest - a worse answer than a page split into sections, so it is worth knowing about
        // here rather than when a user hits it.
        Assert.IsFalse(text.Contains("[truncated", StringComparison.Ordinal),
            $"'{title}' is over the document cap and is being served truncated");
    }

    [TestMethod]
    [DynamicData(nameof(AllPages))]
    public async Task EveryDocumentationPage_ComesBackAsMarkdownRatherThanMarkup(string? slug, string title)
    {
        var text = await _server.CallTextAsync("GetBswupDocsPage", new Dictionary<string, object?> { ["slug"] = slug });

        // Only outside the samples: the pages teach a script tag and an app container, so `<script`
        // and `<div` inside a code block are the content, not leftover markup.
        var prose = ProseOnly(text);

        foreach (var leftover in new[] { "<div", "<span", "<script", "<svg", "class=\"" })
        {
            Assert.IsFalse(prose.Contains(leftover, StringComparison.OrdinalIgnoreCase),
                $"'{title}' still carries '{leftover}' outside its code samples - markup an agent has no use for eats its budget");
        }
    }

    /// <summary>The document with its fenced blocks and inline code removed.</summary>
    private static string ProseOnly(string markdown)
    {
        var prose = new System.Text.StringBuilder(markdown.Length);
        string? fence = null;

        foreach (var line in markdown.Split('\n'))
        {
            var trimmed = line.TrimStart();
            var backticks = new string('`', trimmed.TakeWhile(c => c == '`').Count());

            if (fence is null)
            {
                if (backticks.Length >= 3) { fence = backticks; continue; }
            }
            else
            {
                if (backticks.Length >= fence.Length) fence = null;
                continue;
            }

            prose.AppendLine(System.Text.RegularExpressions.Regex.Replace(line, "`[^`]*`", " "));
        }

        return prose.ToString();
    }

    [TestMethod]
    public async Task DocsPage_WithNoSlug_ReturnsTheIntroduction()
    {
        var text = await _server.CallTextAsync("GetBswupDocsPage");

        StringAssert.Contains(text, "bit Bswup documentation page: /");
    }

    [TestMethod]
    [DataRow("introduction")]
    [DataRow("home")]
    public async Task DocsPage_MapsTheWordsAgentsReachForToTheIntroductionsEmptySlug(string slug)
    {
        // The introduction's own slug is the empty string, which is not a value anyone types.
        var text = await _server.CallTextAsync("GetBswupDocsPage", new { slug });

        Assert.IsFalse(text.StartsWith("No documentation page", StringComparison.Ordinal), slug);
    }

    [TestMethod]
    public async Task DocsPage_ForAnUnknownSlug_AnswersWithTheSlugsThatExist()
    {
        var text = await _server.CallTextAsync("GetBswupDocsPage", new { slug = "not-a-page" });

        StringAssert.Contains(text, "No documentation page has the slug 'not-a-page'");
        StringAssert.Contains(text, "service-worker", "the available slugs have to be in the answer");
    }

    [TestMethod]
    public async Task DocsPage_IsServedFromCacheOnASecondCall()
    {
        // Rendering and flattening a page costs far more than serving it, and the pages are static.
        var first = await _server.CallTextAsync("GetBswupDocsPage", new { slug = "service-worker" });
        var second = await _server.CallTextAsync("GetBswupDocsPage", new { slug = "service-worker" });

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public async Task DocsPages_CoverTheReferenceMaterialTheirDescriptionsPromise()
    {
        var settings = await _server.CallTextAsync("GetBswupDocsPage", new { slug = "service-worker" });

        StringAssert.Contains(settings, "assetsInclude");
        StringAssert.Contains(settings, "externalAssets");

        var events = await _server.CallTextAsync("GetBswupDocsPage", new { slug = "events" });

        StringAssert.Contains(events, "downloadProgress");
        StringAssert.Contains(events, "updateReady");
    }
}
