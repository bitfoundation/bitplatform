using Bit.Bswup.Demo.Client;
using Bit.Bswup.Demo.Server.Services;
using Bit.Bswup.Tests.Mcp.TestInfra;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// What a client sees the moment it connects, before it has called anything: who the server says
/// it is, what it says it can do, and what is on its lists.
/// <para>
/// These names are a public contract in a way ordinary code is not - a client stores them in a
/// saved configuration, and an agent's own instructions name them - so a rename is a breaking
/// change that no compiler catches. The exact-set assertions below are deliberate: adding a tool
/// should make someone update this list on purpose.
/// </para>
/// </summary>
[TestClass]
public class ServerContractTests
{
    private static McpTestServer _server = null!;

    [ClassInitialize]
    public static async Task StartAsync(TestContext _) => _server = await McpTestServer.StartAsync();

    [ClassCleanup]
    public static async Task StopAsync() => await _server.DisposeAsync();

    [TestMethod]
    public void ServerInfo_IdentifiesTheBuildItAnswersFor()
    {
        var info = _server.Mcp.ServerInfo;

        Assert.AreEqual("bit-bswup", info.Name, "this is what a client stores in its server list");
        Assert.AreEqual("bit Bswup", info.Title);
        Assert.AreEqual(BswupScriptCatalog.Version, info.Version,
            "left unset this would be the assembly's 1.0.0.0, which identifies nothing in a bug report");
        Assert.AreEqual(SiteMetadata.Origin, info.WebsiteUrl);
    }

    [TestMethod]
    public void ServerInstructions_CarryTheThingsAnAgentGetsWrongWithNothingToTellItOtherwise()
    {
        var instructions = _server.Mcp.ServerInstructions;

        Assert.IsFalse(string.IsNullOrWhiteSpace(instructions));
        StringAssert.Contains(instructions, "rather than from memory");
        StringAssert.Contains(instructions, "BEFORE the importScripts line");
        StringAssert.Contains(instructions, "service-worker.published.js");
        StringAssert.Contains(instructions, "SearchBswup");
        StringAssert.Contains(instructions, "AutoReload", "the default that changed is the one recalled knowledge gets wrong");
    }

    [TestMethod]
    public void ServerCapabilities_AdvertiseCompletions()
    {
        // The SDK only derives completions from enum-valued schemas; the values worth completing
        // here are catalog entries, so the capability is declared by hand and has to stay declared.
        Assert.IsNotNull(_server.Mcp.ServerCapabilities.Completions);
        Assert.IsNotNull(_server.Mcp.ServerCapabilities.Tools);
        Assert.IsNotNull(_server.Mcp.ServerCapabilities.Prompts);
        Assert.IsNotNull(_server.Mcp.ServerCapabilities.Resources);
    }

    [TestMethod]
    public async Task ToolList_IsExactlyTheToolsTheServerPublishes()
    {
        var tools = await _server.Mcp.ListToolsAsync();

        CollectionAssert.AreEquivalent(
            new[]
            {
                "SearchBswup", "GetBswupSetupGuide", "GetBswupScriptOptions",
                "GetBswupServiceWorkerSettings", "InspectBswupServiceWorker",
                "GetBswupEvents", "GetBswupJsApi", "GetBswupProgressUI",
                "GetBswupDocsPage", "GetBswupSourceFiles", "GetBswupSourceFile",
            },
            tools.Select(tool => tool.Name).ToArray());
    }

    [TestMethod]
    public async Task ToolList_StaysSmallEnoughToBeWorthItsPlaceInAContextWindow()
    {
        // Every client pays for this list on every request, before a question is asked. It is the
        // one cost here nobody can opt out of, so a tool whose answer another tool already
        // contains does not belong in it - which is why there is no overview tool restating the
        // list, no second pair of tools serving the README the docs pages already say, and no
        // listing tool for a set of slugs that fits in a description.
        var tools = await _server.Mcp.ListToolsAsync();

        var characters = tools.Sum(tool =>
            tool.Name.Length + (tool.Description?.Length ?? 0) +
            tool.ProtocolTool.InputSchema.GetRawText().Length +
            (tool.ProtocolTool.OutputSchema?.GetRawText().Length ?? 0));

        Assert.IsTrue(tools.Count <= 12, $"{tools.Count} tools");
        Assert.IsTrue(characters <= 18_000, $"the tool list costs {characters} characters before anyone asks anything");
    }

    [TestMethod]
    public async Task ToolList_DoesNotLeakTheBodyOnlyHttpOverloads()
    {
        var tools = await _server.Mcp.ListToolsAsync();

        // The POST forms exist so a whole file fits in a request; they are HTTP endpoints, not a
        // second copy of the same tool for an agent to choose between.
        Assert.IsFalse(tools.Any(tool => tool.Name.EndsWith("FromBody", StringComparison.Ordinal)),
            string.Join(", ", tools.Select(tool => tool.Name)));
    }

    [TestMethod]
    public async Task PromptList_IsExactlyTheFourWorkflows()
    {
        var prompts = await _server.Mcp.ListPromptsAsync();

        CollectionAssert.AreEquivalent(
            new[] { "add-bswup-to-app", "configure-bswup-caching", "debug-bswup", "remove-bswup" },
            prompts.Select(prompt => prompt.Name).ToArray());
    }

    [TestMethod]
    public async Task ResourceList_CarriesTheFixedDocumentsWithSlugNamesAndHumanTitles()
    {
        var resources = await _server.Mcp.ListResourcesAsync();

        CollectionAssert.AreEquivalent(
            new[] { "bswup://guide", "bswup://settings", "bswup://options", "bswup://events" },
            resources.Select(resource => resource.Uri).ToArray());

        foreach (var resource in resources)
        {
            // The name is what a client keys its cache on and has to stay put across a rewording;
            // the title is the only one of the two anyone reads.
            StringAssert.StartsWith(resource.Name, "bswup-", resource.Uri);
            Assert.IsFalse(string.IsNullOrWhiteSpace(resource.Title), resource.Uri);
            Assert.IsFalse(string.IsNullOrWhiteSpace(resource.Description), resource.Uri);
            Assert.AreEqual("text/markdown", resource.MimeType, resource.Uri);
        }
    }

    [TestMethod]
    public async Task ResourceTemplateList_CoversTheThreeCatalogsKeyedByAValue()
    {
        var templates = await _server.Mcp.ListResourceTemplatesAsync();

        CollectionAssert.AreEquivalent(
            new[] { "bswup://docs/{slug}", "bswup://guide/{heading}", "bswup://source/{path}" },
            templates.Select(template => template.UriTemplate).ToArray());

        foreach (var template in templates)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(template.Title), template.UriTemplate);
            Assert.IsFalse(string.IsNullOrWhiteSpace(template.Description), template.UriTemplate);
        }
    }
}
