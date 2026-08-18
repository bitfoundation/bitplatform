using System.Text.Json;
using Bit.Bswup.Demo.Server.Services;
using Bit.Bswup.Tests.Mcp.TestInfra;
using ModelContextProtocol.Client;

namespace Bit.Bswup.Tests.Mcp.Protocol;

/// <summary>
/// Every tool, called the way an agent calls it - over the protocol, through the real controller
/// and the real DI container.
/// <para>
/// A tool can be perfectly correct as a C# method and still be useless or broken once it is a
/// tool: an argument the SDK cannot describe, a return type with no output schema, a scoped
/// dependency that does not resolve outside a request, a "not found" that comes back as a failed
/// call instead of a sentence an agent can act on. None of that shows up anywhere but here.
/// </para>
/// </summary>
[TestClass]
public class ToolCallTests
{
    private static McpTestServer _server = null!;
    private static IList<McpClientTool> _tools = null!;

    /// <summary>The tools declared with UseStructuredContent, which a typed client reads as JSON.</summary>
    private static readonly string[] _structuredTools =
    [
        "SearchBswup", "GetBswupScriptOptions", "GetBswupServiceWorkerSettings", "GetBswupServiceWorkerModes",
        "InspectBswupServiceWorker", "AnalyzeBswupAssetCaching", "GetBswupEvents", "GetBswupJsApi",
        "GetBswupProgressUI", "GetBswupDocsList", "GetBswupGuideSections", "GetBswupSourceFiles",
    ];

    [ClassInitialize]
    public static async Task StartAsync(TestContext _)
    {
        _server = await McpTestServer.StartAsync();
        _tools = await _server.Mcp.ListToolsAsync();
    }

    [ClassCleanup]
    public static async Task StopAsync() => await _server.DisposeAsync();

    // -- What a client sees before it calls anything ---------------------------

    [TestMethod]
    public void EveryTool_CarriesATitleAndADescriptionThatSaysWhenToCallIt()
    {
        foreach (var tool in _tools)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(tool.ProtocolTool.Title), tool.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Description), tool.Name);

            // A one-liner is a name restated; the description is the only thing a model uses to
            // decide between seventeen tools.
            Assert.IsTrue(tool.Description!.Length > 80, $"{tool.Name}: description is {tool.Description.Length} characters");
        }
    }

    [TestMethod]
    public void EveryTool_IsAnnotatedAsAReadOnlyIdempotentLookup()
    {
        foreach (var tool in _tools)
        {
            var annotations = tool.ProtocolTool.Annotations;

            Assert.IsNotNull(annotations, tool.Name);
            Assert.AreEqual(true, annotations.ReadOnlyHint, $"{tool.Name} changes nothing and must say so");
            Assert.AreEqual(true, annotations.IdempotentHint, tool.Name);
            Assert.AreEqual(false, annotations.DestructiveHint, tool.Name);
            Assert.AreEqual(false, annotations.OpenWorldHint, $"{tool.Name} answers from this build, not from the internet");
        }
    }

    [TestMethod]
    public void EveryToolArgument_IsDescribedInTheInputSchema()
    {
        foreach (var tool in _tools)
        {
            if (tool.ProtocolTool.InputSchema.TryGetProperty("properties", out var properties) is false) continue;

            foreach (var property in properties.EnumerateObject())
            {
                Assert.IsTrue(property.Value.TryGetProperty("description", out var description),
                    $"{tool.Name}.{property.Name} has no description - a model has to guess what to pass");
                Assert.IsTrue(description.GetString()!.Length > 20, $"{tool.Name}.{property.Name}");
            }
        }
    }

    [TestMethod]
    public void ToolsThatReturnData_PublishAnOutputSchema()
    {
        foreach (var name in _structuredTools)
        {
            var tool = _tools.Single(t => t.Name == name);

            Assert.IsNotNull(tool.ProtocolTool.OutputSchema,
                $"{name} returns structured content, so a typed client needs its shape - an anonymous type would have none");
        }
    }

    [TestMethod]
    public void ToolsThatReturnProse_DoNotPretendToBeStructured()
    {
        foreach (var tool in _tools.Where(tool => _structuredTools.Contains(tool.Name) is false))
        {
            Assert.IsNull(tool.ProtocolTool.OutputSchema, tool.Name);
        }
    }

    // -- Calling them ----------------------------------------------------------

    [TestMethod]
    public async Task GetBswupOverview_ExplainsTheThreePlacesBswupIsConfigured_AndNamesTheOtherTools()
    {
        var text = await _server.CallTextAsync("GetBswupOverview");

        StringAssert.Contains(text, "bit-bswup.js");
        StringAssert.Contains(text, "service-worker.published.js");
        StringAssert.Contains(text, "BswupProgress");
        StringAssert.Contains(text, BswupScriptCatalog.Version, "the overview says which build the answers come from");
        StringAssert.Contains(text, "SearchBswup");
    }

    [TestMethod]
    public async Task GetBswupOverview_NamesOnlyToolsThatExist()
    {
        var text = await _server.CallTextAsync("GetBswupOverview");
        var names = _tools.Select(tool => tool.Name).ToArray();

        foreach (var mentioned in System.Text.RegularExpressions.Regex.Matches(text, @"`(?<name>(?:Get|Search|Inspect|Analyze)Bswup\w*)`")
                                                                     .Select(match => match.Groups["name"].Value)
                                                                     .Distinct())
        {
            CollectionAssert.Contains(names, mentioned, $"the overview points at '{mentioned}', which is not a tool");
        }
    }

    [TestMethod]
    public async Task SearchBswup_ReturnsHitsAsStructuredData()
    {
        var result = await _server.CallAsync("SearchBswup", new { query = "cache an external CDN script", limit = 5 });
        var hits = McpTestServer.StructuredOf(result, "SearchBswup");

        Assert.AreEqual(JsonValueKind.Array, hits.ValueKind);
        Assert.IsTrue(hits.GetArrayLength() is > 0 and <= 5);

        foreach (var hit in hits.EnumerateArray())
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.GetProperty("tool").GetString()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(hit.GetProperty("title").GetString()));
        }
    }

    [TestMethod]
    public async Task SearchBswup_WithNothingToMatchOn_AnswersEmptyRatherThanFailing()
    {
        var result = await _server.CallAsync("SearchBswup", new { query = "   " });

        Assert.IsTrue(result.IsError is not true);
    }

    [TestMethod]
    [DataRow("standalone-wasm", "index.html")]
    [DataRow("blazor-web-app", "App.razor")]
    public async Task GetBswupSetupGuide_ReturnsTheWiringForAHostingModel(string model, string expected)
    {
        var text = await _server.CallTextAsync("GetBswupSetupGuide", new { hostingModel = model });

        StringAssert.Contains(text, "## Checklist");
        StringAssert.Contains(text, expected);
    }

    [TestMethod]
    public async Task GetBswupSetupGuide_ForAnUnknownModel_AnswersWithTheOnesItKnows()
    {
        var text = await _server.CallTextAsync("GetBswupSetupGuide", new { hostingModel = "maui-hybrid" });

        StringAssert.Contains(text, "is not a known hosting model");
        StringAssert.Contains(text, "standalone-wasm");
        StringAssert.Contains(text, "blazor-web-app");
    }

    [TestMethod]
    public async Task GetBswupScriptOptions_ReturnsEveryAttributeWithItsDefault()
    {
        var result = await _server.CallAsync("GetBswupScriptOptions");
        var options = McpTestServer.StructuredOf(result, "GetBswupScriptOptions");

        var names = options.EnumerateArray().Select(option => option.GetProperty("name").GetString()).ToArray();

        CollectionAssert.Contains(names, "stallTimeout");
        CollectionAssert.Contains(names, "updateOnVisibility");
    }

    [TestMethod]
    public async Task GetBswupServiceWorkerSettings_ReturnsTheSettingsAndTheBuiltInAssetLists()
    {
        var result = await _server.CallAsync("GetBswupServiceWorkerSettings");
        var payload = McpTestServer.StructuredOf(result, "GetBswupServiceWorkerSettings");

        Assert.IsTrue(payload.GetProperty("settings").GetArrayLength() > 15);
        Assert.IsTrue(payload.GetProperty("defaultAssetsInclude").GetArrayLength() > 10);
        Assert.IsTrue(payload.GetProperty("defaultAssetsExclude").GetArrayLength() > 0);

        var notes = string.Join("\n", payload.GetProperty("notes").EnumerateArray().Select(note => note.GetString()));

        StringAssert.Contains(notes, "BEFORE");
        StringAssert.Contains(notes, "service-worker.published.js");
        StringAssert.Contains(notes, "exclude always beats an include");
    }

    [TestMethod]
    public async Task GetBswupServiceWorkerModes_ReturnsTheFourPresetsWithTheirSettings()
    {
        var result = await _server.CallAsync("GetBswupServiceWorkerModes");
        var modes = McpTestServer.StructuredOf(result, "GetBswupServiceWorkerModes");

        var names = modes.EnumerateArray().Select(mode => mode.GetProperty("name").GetString()).ToArray();

        CollectionAssert.AreEquivalent(new[] { "NoPrerender", "InitialPrerender", "AlwaysPrerender", "FullOffline" }, names);
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_ReviewsAFilePassedAsAnArgument()
    {
        var result = await _server.CallAsync("InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.SettingAfterImport });
        var report = McpTestServer.StructuredOf(result, "InspectBswupServiceWorker");

        Assert.IsTrue(report.GetProperty("importsBswup").GetBoolean());

        var problems = string.Join("\n", report.GetProperty("problems").EnumerateArray().Select(problem => problem.GetString()));

        StringAssert.Contains(problems, "AFTER the importScripts line");
    }

    [TestMethod]
    public async Task AnalyzeBswupAssetCaching_DecidesTheUrlsItIsGiven()
    {
        var result = await _server.CallAsync("AnalyzeBswupAssetCaching", new
        {
            script = ServiceWorkerFixtures.Clean,
            assetUrls = "_framework/dotnet.native.wasm\ncss/app.css, service-worker.js; downloads/report.pdf"
        });

        var analysis = McpTestServer.StructuredOf(result, "AnalyzeBswupAssetCaching");
        var assets = analysis.GetProperty("assets").EnumerateArray()
            .ToDictionary(asset => asset.GetProperty("url").GetString()!, asset => asset.GetProperty("cached").GetBoolean());

        Assert.AreEqual(4, assets.Count, "newlines, commas and semicolons all separate URLs");
        Assert.IsTrue(assets["_framework/dotnet.native.wasm"]);
        Assert.IsTrue(assets["css/app.css"]);
        Assert.IsFalse(assets["service-worker.js"]);
        Assert.IsFalse(assets["downloads/report.pdf"]);
    }

    [TestMethod]
    public async Task AnalyzeBswupAssetCaching_SaysSoWhenItAnsweredForOnlyPartOfAPastedManifest()
    {
        // A silently truncated list reads as "these are all of them".
        var urls = string.Join("\n", Enumerable.Range(0, 260).Select(index => $"_framework/asset{index}.dll"));

        var result = await _server.CallAsync("AnalyzeBswupAssetCaching", new { script = ServiceWorkerFixtures.Clean, assetUrls = urls });
        var analysis = McpTestServer.StructuredOf(result, "AnalyzeBswupAssetCaching");

        Assert.AreEqual(200, analysis.GetProperty("assets").GetArrayLength());

        var notes = string.Join("\n", analysis.GetProperty("notes").EnumerateArray().Select(note => note.GetString()));

        StringAssert.Contains(notes, "Only the first 200 of the 260 URLs");
    }

    [TestMethod]
    public async Task GetBswupEvents_ReturnsTheMessagesAHandlerSwitchesOn()
    {
        var result = await _server.CallAsync("GetBswupEvents");
        var events = McpTestServer.StructuredOf(result, "GetBswupEvents");

        var messages = events.EnumerateArray().ToDictionary(
            message => message.GetProperty("name").GetString()!,
            message => message.GetProperty("message").GetString());

        Assert.AreEqual("DOWNLOAD_PROGRESS", messages["downloadProgress"]);
        Assert.AreEqual("UPDATE_READY", messages["updateReady"]);
    }

    [TestMethod]
    public async Task GetBswupJsApi_ReturnsTheGlobalObjectsMembers()
    {
        var result = await _server.CallAsync("GetBswupJsApi");
        var api = McpTestServer.StructuredOf(result, "GetBswupJsApi");

        var names = api.EnumerateArray().Select(member => member.GetProperty("name").GetString()).ToArray();

        CollectionAssert.Contains(names, "checkForUpdate");
        CollectionAssert.Contains(names, "forceRefresh");
    }

    [TestMethod]
    public async Task GetBswupProgressUI_ReturnsTheParametersAndTheElementIds()
    {
        var result = await _server.CallAsync("GetBswupProgressUI");
        var ui = McpTestServer.StructuredOf(result, "GetBswupProgressUI");

        var autoReload = ui.GetProperty("parameters").EnumerateArray()
                           .Single(parameter => parameter.GetProperty("name").GetString() == "AutoReload");

        Assert.AreEqual("false", autoReload.GetProperty("default").GetString());

        var ids = ui.GetProperty("elements").EnumerateArray().Select(element => element.GetProperty("id").GetString()).ToArray();

        CollectionAssert.Contains(ids, "bit-bswup-progress-bar");
    }

    [TestMethod]
    public async Task GetBswupDocsList_ListsEveryPageOfTheSite()
    {
        var result = await _server.CallAsync("GetBswupDocsList");
        var pages = McpTestServer.StructuredOf(result, "GetBswupDocsList");

        Assert.AreEqual(Bit.Bswup.Demo.Client.DocsCatalog.AllPages.Count(), pages.GetArrayLength());

        foreach (var page in pages.EnumerateArray())
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.GetProperty("title").GetString()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.GetProperty("description").GetString()));
            Assert.IsFalse(string.IsNullOrWhiteSpace(page.GetProperty("keywords").GetString()));
            StringAssert.StartsWith(page.GetProperty("url").GetString(), "/");
        }
    }

    [TestMethod]
    public async Task GetBswupGuideSections_And_GetBswupGuideSection_AgreeWithEachOther()
    {
        var result = await _server.CallAsync("GetBswupGuideSections");
        var sections = McpTestServer.StructuredOf(result, "GetBswupGuideSections");

        foreach (var heading in sections.EnumerateArray().Select(section => section.GetProperty("heading").GetString()!))
        {
            var text = await _server.CallTextAsync("GetBswupGuideSection", new { heading });

            Assert.IsFalse(text.StartsWith("The guide has no section", StringComparison.Ordinal),
                $"'{heading}' is listed but cannot be fetched");
        }
    }

    [TestMethod]
    public async Task GetBswupGuideSection_ForAnUnknownHeading_AnswersWithTheHeadingsItHas()
    {
        var text = await _server.CallTextAsync("GetBswupGuideSection", new { heading = "Quantum Tunnelling" });

        StringAssert.Contains(text, "has no section called");
        StringAssert.Contains(text, "'JavaScript API'");
    }

    [TestMethod]
    public async Task GetBswupSourceFiles_ListsFilesThatCanEachBeFetched()
    {
        var result = await _server.CallAsync("GetBswupSourceFiles");
        var files = McpTestServer.StructuredOf(result, "GetBswupSourceFiles");

        Assert.IsTrue(files.GetArrayLength() > 20);

        // Fetching all of them would be a slow way of re-testing the catalog; the point here is
        // that a path off the listing really is fetchable through the protocol.
        foreach (var path in new[] { "Library/Scripts/bit-bswup.sw.ts", "Demo/Client/wwwroot/service-worker.published.js" })
        {
            var text = await _server.CallTextAsync("GetBswupSourceFile", new { path });

            Assert.IsFalse(text.StartsWith("No source file at", StringComparison.Ordinal), path);
            Assert.IsTrue(text.Length > 100, path);
        }
    }

    [TestMethod]
    public async Task GetBswupSourceFile_ForAPartialPath_SuggestsTheMatchesInstead()
    {
        var text = await _server.CallTextAsync("GetBswupSourceFile", new { path = "service-worker.published.js" });

        StringAssert.Contains(text, "Did you mean");
        StringAssert.Contains(text, "Demo/Client/wwwroot/service-worker.published.js");
    }

    [TestMethod]
    public async Task GetBswupSourceFile_ForNothingLikeAPath_PointsAtTheListing()
    {
        var text = await _server.CallTextAsync("GetBswupSourceFile", new { path = "zzz-not-a-file" });

        StringAssert.Contains(text, "Call GetBswupSourceFiles for the full list");
    }

    [TestMethod]
    public async Task LongDocuments_AreTruncatedWithTheTruncationSaidOutLoud()
    {
        // The cap exists so one document cannot dominate a client's context window; a document cut
        // off without a word would read as the whole thing.
        var longest = BswupSourceCatalog.SourceFiles
            .OrderByDescending(file => file.Lines)
            .FirstOrDefault(file => BswupSourceCatalog.GetSourceFile(file.Path)!.Length > 40_000);

        Assert.IsNotNull(longest, "no embedded source file is long enough to be truncated - the cap is untested");

        var text = await _server.CallTextAsync("GetBswupSourceFile", new { path = longest.Path });

        StringAssert.Contains(text, "[truncated");
        Assert.IsTrue(text.Length < 41_000, $"{longest.Path} came back as {text.Length} characters");
    }
}
