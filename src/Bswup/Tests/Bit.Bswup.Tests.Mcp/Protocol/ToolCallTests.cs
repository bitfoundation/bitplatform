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
        "SearchBswup", "GetBswupScriptOptions", "GetBswupServiceWorkerSettings",
        "InspectBswupServiceWorker", "GetBswupEvents", "GetBswupJsApi",
        "GetBswupProgressUI", "GetBswupSourceFiles",
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
            // decide between the eleven tools here.
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
    public void NoToolRestatesTheToolList()
    {
        // There used to be a "start here" tool whose 4,000-character answer was the list of tools
        // the client already holds, plus the rules the server instructions already carry. A client
        // paid for it twice and an agent paid a call to be told to make another one.
        Assert.IsFalse(_tools.Any(tool => tool.Name.Contains("Overview", StringComparison.OrdinalIgnoreCase)),
            string.Join(", ", _tools.Select(tool => tool.Name)));

        var instructions = _server.Mcp.ServerInstructions!;

        StringAssert.Contains(instructions, "autostart");
        StringAssert.Contains(instructions, "service-worker.published.js");
        StringAssert.Contains(instructions, "never be cached at the HTTP");
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
    public async Task GetBswupServiceWorkerSettings_CarriesTheModePresetsWithTheSettingTheyAreValuesOf()
    {
        // A preset is a bundle of the settings this tool already returns, so it was a second tool
        // for the same subject - and one an agent writing a worker had to know to call.
        var result = await _server.CallAsync("GetBswupServiceWorkerSettings", new { name = "mode" });
        var payload = McpTestServer.StructuredOf(result, "GetBswupServiceWorkerSettings");

        Assert.AreEqual(1, payload.GetProperty("settings").GetArrayLength());

        var names = payload.GetProperty("modes").EnumerateArray().Select(mode => mode.GetProperty("name").GetString()).ToArray();

        CollectionAssert.AreEquivalent(new[] { "NoPrerender", "InitialPrerender", "AlwaysPrerender", "FullOffline" }, names);
    }

    [TestMethod]
    public async Task ReferenceTools_AnswerWithOneEntryWhenOneIsAskedFor()
    {
        // The search index hands agents these narrowed calls, and following one has to cost a
        // fraction of the bare call - that is the whole reason the argument exists.
        var whole = await _server.CallTextAsync("GetBswupServiceWorkerSettings");
        var one = await _server.CallTextAsync("GetBswupServiceWorkerSettings", new { name = "self.assetsExclude" });

        Assert.IsTrue(one.Length * 4 < whole.Length, $"narrowing saved nothing: {one.Length} of {whole.Length} characters");

        var payload = JsonSerializer.Deserialize<JsonElement>(one);

        Assert.AreEqual(1, payload.GetProperty("settings").GetArrayLength());
        Assert.AreEqual("assetsExclude", payload.GetProperty("settings")[0].GetProperty("name").GetString());

        // The asset patterns explain exactly this setting, so they come with it - and the presets,
        // which explain a different one, are not merely null but absent from the wire.
        Assert.IsTrue(payload.TryGetProperty("defaultAssetsExclude", out _));
        Assert.IsFalse(payload.TryGetProperty("modes", out _));
    }

    [TestMethod]
    public async Task ReferenceTools_AnswerWithEverythingWhenTheNameIsNotOneOfThem()
    {
        // An empty answer to a typo reads as "this library has no such thing", which is the one
        // conclusion that must not be drawn from a misspelling.
        var result = await _server.CallAsync("GetBswupEvents", new { name = "updateRead" });
        var events = McpTestServer.StructuredOf(result, "GetBswupEvents");

        Assert.IsTrue(events.GetArrayLength() > 1);
        Assert.IsTrue(events.EnumerateArray().Any(message => message.GetProperty("name").GetString() == "updateReady"));
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
    public async Task InspectBswupServiceWorker_DecidesTheUrlsItIsGiven()
    {
        var result = await _server.CallAsync("InspectBswupServiceWorker", new
        {
            script = ServiceWorkerFixtures.Clean,
            assetUrls = "_framework/dotnet.native.wasm\ncss/app.css, service-worker.js; downloads/report.pdf"
        });

        var analysis = McpTestServer.StructuredOf(result, "InspectBswupServiceWorker").GetProperty("assets");
        var assets = analysis.GetProperty("assets").EnumerateArray()
            .ToDictionary(asset => asset.GetProperty("url").GetString()!, asset => asset.GetProperty("cached").GetBoolean());

        Assert.AreEqual(4, assets.Count, "newlines, commas and semicolons all separate URLs");
        Assert.IsTrue(assets["_framework/dotnet.native.wasm"]);
        Assert.IsTrue(assets["css/app.css"]);
        Assert.IsFalse(assets["service-worker.js"]);
        Assert.IsFalse(assets["downloads/report.pdf"]);
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_LeavesTheAssetsOutWhenNoneWereAskedAbout()
    {
        var result = await _server.CallAsync("InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.Clean });
        var report = McpTestServer.StructuredOf(result, "InspectBswupServiceWorker");

        Assert.IsFalse(report.TryGetProperty("assets", out _),
            "a review that was not asked about any asset must not pay for an empty analysis");
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_SaysSoWhenItAnsweredForOnlyPartOfAPastedManifest()
    {
        // A silently truncated list reads as "these are all of them".
        var urls = string.Join("\n", Enumerable.Range(0, 260).Select(index => $"_framework/asset{index}.dll"));

        var result = await _server.CallAsync("InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.Clean, assetUrls = urls });
        var analysis = McpTestServer.StructuredOf(result, "InspectBswupServiceWorker").GetProperty("assets");

        Assert.AreEqual(200, analysis.GetProperty("assets").GetArrayLength());

        var notes = string.Join("\n", analysis.GetProperty("notes").EnumerateArray().Select(note => note.GetString()));

        StringAssert.Contains(notes, "Only the first 200 of the 260 URLs");
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_StopsReadingAUrlListThatRunsPastTheLengthCap()
    {
        // The 200-URL cap applies only after the list is split, so the split itself is bounded by
        // length - otherwise a body of any size is parsed in full before anything is discarded.
        var urls = string.Join("\n", Enumerable.Range(0, 6_000).Select(index => $"_framework/asset{index}.dll"));

        Assert.IsTrue(urls.Length > 64_000, "the fixture has to reach the cap for this to test anything");

        var result = await _server.CallAsync("InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.Clean, assetUrls = urls });
        var analysis = McpTestServer.StructuredOf(result, "InspectBswupServiceWorker").GetProperty("assets");

        Assert.AreEqual(200, analysis.GetProperty("assets").GetArrayLength());

        // Cutting mid-URL would invent an entry nobody passed, so every one that came back has to
        // be a whole URL from the list.
        var analyzed = analysis.GetProperty("assets").EnumerateArray().Select(asset => asset.GetProperty("url").GetString()).ToArray();

        CollectionAssert.IsSubsetOf(analyzed, urls.Split('\n'), "a cut inside the cap must land on a separator");

        var notes = string.Join("\n", analysis.GetProperty("notes").EnumerateArray().Select(note => note.GetString()));

        StringAssert.Contains(notes, "ran past 64000 characters");
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
    public void GetBswupDocsPage_NamesEverySlugInItsOwnDescription()
    {
        // There was a listing tool for this, whose 4,900-character answer was fourteen slugs with
        // their descriptions and the site's search keywords. Fourteen slugs fit in a sentence, and
        // a description is read once per session rather than fetched.
        var description = _tools.Single(tool => tool.Name == "GetBswupDocsPage").Description!;

        foreach (var page in Bit.Bswup.Demo.Client.DocsCatalog.AllPages)
        {
            var slug = page.Slug.Length == 0 ? "introduction" : page.Slug;

            StringAssert.Contains(description, $"'{slug}'", $"the '{slug}' page is not reachable from the description");
        }
    }

    [TestMethod]
    public async Task TheReadmeIsAResourceRatherThanAPairOfTools()
    {
        // Its one 30,000-character section said what the documentation pages say, so an agent
        // could pay three times over for the same material. It stays readable for a person who
        // wants to pin it; it is no longer something a search can spend a context window on.
        Assert.IsFalse(_tools.Any(tool => tool.Name.Contains("GuideSection", StringComparison.OrdinalIgnoreCase)),
            string.Join(", ", _tools.Select(tool => tool.Name)));

        var guide = await _server.ReadResourceTextAsync("bswup://guide");

        Assert.IsTrue(guide.Length > 1000);
    }

    [TestMethod]
    public async Task GetBswupSourceFiles_ListsFilesThatCanEachBeFetched()
    {
        var result = await _server.CallAsync("GetBswupSourceFiles");
        var files = McpTestServer.StructuredOf(result, "GetBswupSourceFiles");

        Assert.IsTrue(files.GetArrayLength() > 15);

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
    public async Task LongSourceFiles_ComeBackOneWindowAtATimeAndSayHowToGoOn()
    {
        // The service worker runs past 120,000 characters. It used to come back cut off at 40,000
        // with no way to reach the rest: 10,000 tokens spent on a third of a file. A window that
        // does not reach the end has to say where it stopped, or the missing part reads as absent.
        var longest = BswupSourceCatalog.SourceFiles
            .OrderByDescending(file => BswupSourceCatalog.GetSourceFile(file.Path)!.Length)
            .First();

        Assert.IsTrue(BswupSourceCatalog.GetSourceFile(longest.Path)!.Length > 16_000,
            "no embedded source file is long enough to be windowed - the cap is untested");

        var first = await _server.CallTextAsync("GetBswupSourceFile", new { path = longest.Path });

        StringAssert.StartsWith(first, "[lines 1-");
        StringAssert.Contains(first, $"startLine: ");
        Assert.IsTrue(first.Length < 17_000, $"{longest.Path} came back as {first.Length} characters");

        var next = int.Parse(System.Text.RegularExpressions.Regex.Match(first, @"startLine: (?<line>\d+)").Groups["line"].Value);
        var second = await _server.CallTextAsync("GetBswupSourceFile", new { path = longest.Path, startLine = next });

        StringAssert.StartsWith(second, $"[lines {next}-");
    }

    [TestMethod]
    public async Task ShortSourceFiles_ComeBackVerbatimWithNoWindowHeader()
    {
        var text = await _server.CallTextAsync("GetBswupSourceFile", new { path = "Demo/Client/wwwroot/service-worker.published.js" });

        Assert.AreEqual(BswupSourceCatalog.GetSourceFile("Demo/Client/wwwroot/service-worker.published.js"), text);
    }

    [TestMethod]
    public async Task SourceFileWindow_NamesTheRangeWhenStartLineIsPastTheEnd()
    {
        // A startLine past the end used to be clamped onto the last line, which handed back the
        // window the caller already had and read as the next one. The line count and the range
        // say what to ask for instead, the way an unknown path names the paths that exist.
        const string path = "Demo/Client/wwwroot/service-worker.published.js";

        var lines = BswupSourceCatalog.SourceFiles.Single(file => file.Path == path).Lines;

        var past = await _server.CallTextAsync("GetBswupSourceFile", new { path, startLine = lines + 1 });

        StringAssert.Contains(past, $"{lines} lines");
        StringAssert.Contains(past, $"no line {lines + 1}");
        StringAssert.Contains(past, $"between 1 and {lines}");
    }

    [TestMethod]
    public async Task SourceFileWindow_ReadsFromTheStartWhenStartLineIsBelowTheRange()
    {
        // Below the range there is nothing to disambiguate - line 0, which is what a caller
        // counting from zero asks for, can only mean the start of the file - so erroring there
        // would spend a call saying what the obvious reading already says.
        const string path = "Demo/Client/wwwroot/service-worker.published.js";

        var text = await _server.CallTextAsync("GetBswupSourceFile", new { path, startLine = 0 });

        Assert.AreEqual(BswupSourceCatalog.GetSourceFile(path), text);
    }

    [TestMethod]
    public async Task SourceFileWindow_CountsTheSameLinesTheListingAdvertises()
    {
        // Splitting on the newline that ends a file leaves an empty element behind, and counting
        // it would put the window one line ahead of what GetBswupSourceFiles advertises. The two
        // disagreeing is worse than either being wrong alone: the range the window names would
        // then invite a call whose entire answer is that line which is not there.
        foreach (var file in BswupSourceCatalog.SourceFiles.Where(file => file.Lines > 1))
        {
            var last = await _server.CallTextAsync("GetBswupSourceFile", new { path = file.Path, startLine = file.Lines });
            var header = $"[lines {file.Lines}-{file.Lines} of {file.Lines} - this is the end of the file]";

            StringAssert.StartsWith(last, header,
                $"the last line of '{file.Path}' does not line up with the {file.Lines} lines the listing advertises");
        }
    }
}
