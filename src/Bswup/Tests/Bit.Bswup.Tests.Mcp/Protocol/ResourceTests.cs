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

        StringAssert.Contains(text, "**self.assetsInclude**");
        StringAssert.Contains(text, "**self.errorTolerance**");
        StringAssert.Contains(text, "`lax`");
        StringAssert.Contains(text, "## Built-in asset include patterns");
        StringAssert.Contains(text, "## Built-in asset exclude patterns");
        StringAssert.Contains(text, "BEFORE");
    }

    [TestMethod]
    public async Task Settings_CoversTheSameNamesTheToolDoes()
    {
        var text = await _server.ReadResourceTextAsync("bswup://settings");

        foreach (var setting in BswupScriptCatalog.WorkerSettings)
        {
            StringAssert.Contains(text, $"**self.{setting.Name}**", "the resource and the tool must not drift apart");
        }
    }

    [TestMethod]
    public async Task Options_CoversTheSameAttributesTheToolDoes()
    {
        var text = await _server.ReadResourceTextAsync("bswup://options");

        foreach (var option in BswupScriptCatalog.ScriptOptions)
        {
            StringAssert.Contains(text, $"**{option.Name}**");
        }
    }

    [TestMethod]
    public async Task Events_CoversTheSameMessagesTheToolDoes()
    {
        var text = await _server.ReadResourceTextAsync("bswup://events");

        foreach (var message in BswupScriptCatalog.Events)
        {
            StringAssert.Contains(text, $"**BswupMessage.{message.Name}**");
        }

        StringAssert.Contains(text, "'DOWNLOAD_PROGRESS'");
    }

    [TestMethod]
    public async Task GuideSection_ResolvesAHeadingOutOfTheUri()
    {
        var text = await _server.ReadResourceTextAsync("bswup://guide/JavaScript%20API");

        StringAssert.StartsWith(text, "## JavaScript API");
    }

    [TestMethod]
    public async Task GuideSection_ForAnUnknownHeading_AnswersInsteadOfFailing()
    {
        var text = await _server.ReadResourceTextAsync("bswup://guide/Nope");

        StringAssert.Contains(text, "has no section called 'Nope'");
    }

    [TestMethod]
    public async Task SourceFile_ResolvesAnEncodedPath()
    {
        var text = await _server.ReadResourceTextAsync("bswup://source/Library%2FScripts%2Fbit-bswup.sw.ts");

        StringAssert.Contains(text, "DEFAULT_ASSETS_INCLUDE");
    }

    [TestMethod]
    public async Task SourceFile_ForAnUnknownPath_AnswersInsteadOfFailing()
    {
        var text = await _server.ReadResourceTextAsync("bswup://source/nope.ts");

        StringAssert.Contains(text, "No source file at 'nope.ts'");
    }

    [TestMethod]
    public async Task DocsPage_RendersThroughAScopedRenderer()
    {
        // The resource resolves its own HtmlRenderer out of the request scope; a lifetime that is
        // wrong here fails only for resources, never for the tool that renders the same page.
        var text = await _server.ReadResourceTextAsync("bswup://docs/service-worker");

        Assert.IsFalse(text.Contains("could not be rendered", StringComparison.Ordinal), text);
        StringAssert.Contains(text, "assetsInclude");
    }

    [TestMethod]
    public async Task DocsPage_ForAnUnknownSlug_AnswersInsteadOfFailing()
    {
        var text = await _server.ReadResourceTextAsync("bswup://docs/nope");

        StringAssert.Contains(text, "No documentation page has the slug 'nope'");
    }

    [TestMethod]
    public async Task EveryListedResource_CanBeRead()
    {
        foreach (var resource in await _server.Mcp.ListResourcesAsync())
        {
            var text = await _server.ReadResourceTextAsync(resource.Uri);

            Assert.IsTrue(text.Length > 100, $"{resource.Uri} came back with {text.Length} characters");
        }
    }
}
