using Bit.Bswup.Demo.Server.Services;
using Bit.Bswup.Tests.Mcp.TestInfra;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// The same knowledge the tools serve, exposed for a client that wants to attach documentation up
/// front or let a person browse and pin it. Both sides read the same catalogs, so the assertions
/// that matter are the ones that would catch them drifting apart - and that a templated URI still
/// resolves for values a person will type by hand.
/// </summary>
[TestClass]
public class ResourceTests
{
    private static McpTestServer _server = null!;

    [ClassInitialize]
    public static async Task StartAsync(TestContext _) => _server = await McpTestServer.StartAsync();

    [ClassCleanup]
    public static async Task StopAsync() => await _server.DisposeAsync();

    [TestMethod]
    public async Task Guide_IsTheWholeReadme()
    {
        var text = await _server.ReadResourceTextAsync("bswup://guide");

        Assert.AreEqual(BswupSourceCatalog.Readme, text);
    }

    [TestMethod]
    public async Task Settings_ListsEverySettingWithItsDefaultAndTheBuiltInPatterns()
    {
        var text = await _server.ReadResourceTextAsync("bswup://settings");

        Assert.Contains("**self.assetsInclude**", text);
        Assert.Contains("**self.errorTolerance**", text);
        Assert.Contains("`lax`", text);
        Assert.Contains("## Built-in asset include patterns", text);
        Assert.Contains("## Built-in asset exclude patterns", text);
        Assert.Contains("BEFORE", text);
    }

    [TestMethod]
    public async Task Settings_CoversTheSameNamesTheToolDoes()
    {
        var text = await _server.ReadResourceTextAsync("bswup://settings");

        foreach (var setting in BswupScriptCatalog.WorkerSettings)
        {
            Assert.Contains($"**self.{setting.Name}**", text, "the resource and the tool must not drift apart");
        }
    }

    [TestMethod]
    public async Task Options_CoversTheSameAttributesTheToolDoes()
    {
        var text = await _server.ReadResourceTextAsync("bswup://options");

        foreach (var option in BswupScriptCatalog.ScriptOptions)
        {
            Assert.Contains($"**{option.Name}**", text);
        }
    }

    [TestMethod]
    public async Task Events_CoversTheSameMessagesTheToolDoes()
    {
        var text = await _server.ReadResourceTextAsync("bswup://events");

        foreach (var message in BswupScriptCatalog.Events)
        {
            Assert.Contains($"**BswupMessage.{message.Name}**", text);
        }

        Assert.Contains("'DOWNLOAD_PROGRESS'", text);
    }

    [TestMethod]
    public async Task GuideSection_ResolvesAHeadingOutOfTheUri()
    {
        var text = await _server.ReadResourceTextAsync("bswup://guide/JavaScript%20API");

        Assert.StartsWith("## JavaScript API", text);
    }

    [TestMethod]
    public async Task GuideSection_ForAnUnknownHeading_AnswersInsteadOfFailing()
    {
        var text = await _server.ReadResourceTextAsync("bswup://guide/Nope");

        Assert.Contains("has no section called 'Nope'", text);
    }

    [TestMethod]
    public async Task SourceFile_ResolvesAnEncodedPath()
    {
        var text = await _server.ReadResourceTextAsync("bswup://source/Library%2FScripts%2Fbit-bswup.sw.ts");

        Assert.Contains("DEFAULT_ASSETS_INCLUDE", text);
    }

    [TestMethod]
    public async Task SourceFile_ForAnUnknownPath_AnswersInsteadOfFailing()
    {
        var text = await _server.ReadResourceTextAsync("bswup://source/nope.ts");

        Assert.Contains("No source file at 'nope.ts'", text);
    }

    [TestMethod]
    public async Task DocsPage_RendersThroughAScopedRenderer()
    {
        // The resource resolves its own HtmlRenderer out of the request scope; a lifetime that is
        // wrong here fails only for resources, never for the tool that renders the same page.
        var text = await _server.ReadResourceTextAsync("bswup://docs/service-worker");

        Assert.IsFalse(text.Contains("could not be rendered", StringComparison.Ordinal), text);
        Assert.Contains("assetsInclude", text);
    }

    [TestMethod]
    public async Task DocsPage_ForAnUnknownSlug_AnswersInsteadOfFailing()
    {
        var text = await _server.ReadResourceTextAsync("bswup://docs/nope");

        Assert.Contains("No documentation page has the slug 'nope'", text);
    }

    [TestMethod]
    public async Task EveryListedResource_CanBeRead()
    {
        foreach (var resource in await _server.Mcp.ListResourcesAsync(cancellationToken: TestContext.CancellationToken))
        {
            var text = await _server.ReadResourceTextAsync(resource.Uri);

            Assert.IsGreaterThan(100, text.Length, $"{resource.Uri} came back with {text.Length} characters");
        }
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
