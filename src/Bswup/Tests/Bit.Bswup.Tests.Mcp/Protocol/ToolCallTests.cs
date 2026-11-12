using System.Text.Json;
using Bit.Bswup.Demo.Server.Controllers;
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
        _tools = await _server.Mcp.ListToolsAsync(cancellationToken: _.CancellationToken);
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
            Assert.IsGreaterThan(80, tool.Description!.Length, $"{tool.Name}: description is {tool.Description.Length} characters");
        }
    }

    [TestMethod]
    public void EveryTool_IsAnnotatedAsAReadOnlyIdempotentLookup()
    {
        foreach (var tool in _tools)
        {
            var annotations = tool.ProtocolTool.Annotations;

            Assert.IsNotNull(annotations, tool.Name);
            Assert.IsTrue(annotations.ReadOnlyHint, $"{tool.Name} changes nothing and must say so");
            Assert.IsTrue(annotations.IdempotentHint, tool.Name);
            Assert.IsFalse(annotations.DestructiveHint, tool.Name);
            Assert.IsFalse(annotations.OpenWorldHint, $"{tool.Name} answers from this build, not from the internet");
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
                Assert.IsGreaterThan(20, description.GetString()!.Length, $"{tool.Name}.{property.Name}");
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
        Assert.DoesNotContain(tool => tool.Name.Contains("Overview", StringComparison.OrdinalIgnoreCase), _tools,
            string.Join(", ", _tools.Select(tool => tool.Name)));

        var instructions = _server.Mcp.ServerInstructions!;

        Assert.Contains("autostart", instructions);
        Assert.Contains("service-worker.published.js", instructions);
        Assert.Contains("never be cached at the HTTP", instructions);
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

        Assert.AreNotEqual(true, result.IsError);
    }

    [TestMethod]
    [DataRow("standalone-wasm", "index.html")]
    [DataRow("blazor-web-app", "App.razor")]
    public async Task GetBswupSetupGuide_ReturnsTheWiringForAHostingModel(string model, string expected)
    {
        var text = await _server.CallTextAsync("GetBswupSetupGuide", new { hostingModel = model });

        Assert.Contains("## Checklist", text);
        Assert.Contains(expected, text);
    }

    [TestMethod]
    public async Task GetBswupSetupGuide_ForAnUnknownModel_AnswersWithTheOnesItKnows()
    {
        var text = await _server.CallTextAsync("GetBswupSetupGuide", new { hostingModel = "maui-hybrid" });

        Assert.Contains("is not a known hosting model", text);
        Assert.Contains("standalone-wasm", text);
        Assert.Contains("blazor-web-app", text);
    }

    [TestMethod]
    public async Task GetBswupScriptOptions_ReturnsEveryAttributeWithItsDefault()
    {
        var result = await _server.CallAsync("GetBswupScriptOptions");
        var options = McpTestServer.StructuredOf(result, "GetBswupScriptOptions");

        var names = options.EnumerateArray().Select(option => option.GetProperty("name").GetString()).ToArray();

        Assert.Contains("stallTimeout", names);
        Assert.Contains("updateOnVisibility", names);
    }

    [TestMethod]
    public async Task GetBswupServiceWorkerSettings_ReturnsTheSettingsAndTheBuiltInAssetLists()
    {
        var result = await _server.CallAsync("GetBswupServiceWorkerSettings");
        var payload = McpTestServer.StructuredOf(result, "GetBswupServiceWorkerSettings");

        Assert.IsGreaterThan(15, payload.GetProperty("settings").GetArrayLength());
        Assert.IsGreaterThan(10, payload.GetProperty("defaultAssetsInclude").GetArrayLength());
        Assert.IsGreaterThan(0, payload.GetProperty("defaultAssetsExclude").GetArrayLength());

        var notes = string.Join("\n", payload.GetProperty("notes").EnumerateArray().Select(note => note.GetString()));

        Assert.Contains("BEFORE", notes);
        Assert.Contains("service-worker.published.js", notes);
        Assert.Contains("exclude always beats an include", notes);
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

        Assert.AreSequenceEqual(new[] { "NoPrerender", "InitialPrerender", "AlwaysPrerender", "FullOffline" }, names, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public async Task ReferenceTools_AnswerWithOneEntryWhenOneIsAskedFor()
    {
        // The search index hands agents these narrowed calls, and following one has to cost a
        // fraction of the bare call - that is the whole reason the argument exists.
        var whole = await _server.CallTextAsync("GetBswupServiceWorkerSettings");
        var one = await _server.CallTextAsync("GetBswupServiceWorkerSettings", new { name = "self.assetsExclude" });

        Assert.IsLessThan(whole.Length, one.Length * 4, $"narrowing saved nothing: {one.Length} of {whole.Length} characters");

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

        Assert.IsGreaterThan(1, events.GetArrayLength());
        Assert.Contains(message => message.GetProperty("name").GetString() == "updateReady", events.EnumerateArray());
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_ReviewsAFilePassedAsAnArgument()
    {
        var result = await _server.CallAsync("InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.SettingAfterImport });
        var report = McpTestServer.StructuredOf(result, "InspectBswupServiceWorker");

        Assert.IsTrue(report.GetProperty("importsBswup").GetBoolean());

        var problems = string.Join("\n", report.GetProperty("problems").EnumerateArray().Select(problem => problem.GetString()));

        Assert.Contains("AFTER the importScripts line", problems);
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_RefusesAScriptPastTheLengthLimit()
    {
        var result = await _server.CallAsync("InspectBswupServiceWorker", new { script = new string(' ', 64_001) });

        Assert.IsTrue(result.IsError);
        // The reason reaches the model, so it can trim the file and call again - and it says the
        // whole file was refused, not that the first 64,000 characters were read.
        Assert.Contains("over the 64000-character limit, so none of it was inspected", McpTestServer.TextOf(result));
    }

    [TestMethod]
    public void InspectBswupServiceWorker_AdvertisesTheScriptLengthLimitInItsSchema()
    {
        // The refusal above costs a call; the schema is read before the first one, so the limit
        // has to be there too or a model only learns it by hitting it.
        var tool = _tools.Single(tool => tool.Name == "InspectBswupServiceWorker");
        var description = tool.ProtocolTool.InputSchema.GetProperty("properties").GetProperty("script").GetProperty("description").GetString();

        Assert.Contains("at most 64,000 characters", description!);
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_RefusesAssignmentsChainedPastTheLimit()
    {
        var result = await _server.CallAsync("InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.ChainedAssignments });

        Assert.IsTrue(result.IsError);
        Assert.Contains("chained too deeply", McpTestServer.TextOf(result));
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_RefusesInputWithoutLoggingAServerError()
    {
        // A caller's oversized file is the caller's mistake: the SDK's '"{ToolName}" threw an
        // unhandled exception.' error, stack trace and all, is for faults of the server.
        await _server.CallAsync("InspectBswupServiceWorker", new { script = new string(' ', 64_001) });
        await _server.CallAsync("InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.ChainedAssignments });

        Assert.DoesNotContain(exception => exception is ToolInputException, _server.LoggedExceptions, "a refused input was logged as a server error");
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

        Assert.HasCount(4, assets, "newlines, commas and semicolons all separate URLs");
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

        Assert.Contains("Only the first 200 of the 260 URLs", notes);
    }

    [TestMethod]
    public async Task InspectBswupServiceWorker_StopsReadingAUrlListThatRunsPastTheLengthCap()
    {
        // The 200-URL cap applies only after the list is split, so the split itself is bounded by
        // length - otherwise a body of any size is parsed in full before anything is discarded.
        var urls = string.Join("\n", Enumerable.Range(0, 6_000).Select(index => $"_framework/asset{index}.dll"));

        Assert.IsGreaterThan(64_000, urls.Length, "the fixture has to reach the cap for this to test anything");

        var result = await _server.CallAsync("InspectBswupServiceWorker", new { script = ServiceWorkerFixtures.Clean, assetUrls = urls });
        var analysis = McpTestServer.StructuredOf(result, "InspectBswupServiceWorker").GetProperty("assets");

        Assert.AreEqual(200, analysis.GetProperty("assets").GetArrayLength());

        // Cutting mid-URL would invent an entry nobody passed, so every one that came back has to
        // be a whole URL from the list.
        var analyzed = analysis.GetProperty("assets").EnumerateArray().Select(asset => asset.GetProperty("url").GetString()).ToArray();

        CollectionAssert.IsSubsetOf(analyzed, urls.Split('\n'), "a cut inside the cap must land on a separator");

        var notes = string.Join("\n", analysis.GetProperty("notes").EnumerateArray().Select(note => note.GetString()));

        Assert.Contains("ran past 64000 characters", notes);
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

        Assert.Contains("checkForUpdate", names);
        Assert.Contains("forceRefresh", names);
    }

    [TestMethod]
    public async Task GetBswupProgressUI_ReturnsTheParametersAndTheElementIds()
    {
        var result = await _server.CallAsync("GetBswupProgressUI");
        var ui = McpTestServer.StructuredOf(result, "GetBswupProgressUI");

        var autoReload = ui.GetProperty("parameters").EnumerateArray()
                           .Single(parameter => parameter.GetProperty("name").GetString() == "AutoReload");

        Assert.AreEqual("true", autoReload.GetProperty("default").GetString());

        var ids = ui.GetProperty("elements").EnumerateArray().Select(element => element.GetProperty("id").GetString()).ToArray();

        Assert.Contains("bit-bswup-progress-bar", ids);
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

            Assert.Contains($"'{slug}'", description, $"the '{slug}' page is not reachable from the description");
        }
    }

    [TestMethod]
    public async Task TheReadmeIsAResourceRatherThanAPairOfTools()
    {
        // Its one 30,000-character section said what the documentation pages say, so an agent
        // could pay three times over for the same material. It stays readable for a person who
        // wants to pin it; it is no longer something a search can spend a context window on.
        Assert.DoesNotContain(tool => tool.Name.Contains("GuideSection", StringComparison.OrdinalIgnoreCase), _tools,
            string.Join(", ", _tools.Select(tool => tool.Name)));

        var guide = await _server.ReadResourceTextAsync("bswup://guide");

        Assert.IsGreaterThan(1000, guide.Length);
    }

    [TestMethod]
    public async Task GetBswupSourceFiles_ListsFilesThatCanEachBeFetched()
    {
        var result = await _server.CallAsync("GetBswupSourceFiles");
        var files = McpTestServer.StructuredOf(result, "GetBswupSourceFiles");

        Assert.IsGreaterThan(15, files.GetArrayLength());

        // Fetching all of them would be a slow way of re-testing the catalog; the point here is
        // that a path off the listing really is fetchable through the protocol.
        foreach (var path in new[] { "Library/Scripts/bit-bswup.sw.ts", "Demo/Client/wwwroot/service-worker.published.js" })
        {
            var text = await _server.CallTextAsync("GetBswupSourceFile", new { path });

            Assert.IsFalse(text.StartsWith("No source file at", StringComparison.Ordinal), path);
            Assert.IsGreaterThan(100, text.Length, path);
        }
    }

    [TestMethod]
    public async Task GetBswupSourceFile_ForAPartialPath_SuggestsTheMatchesInstead()
    {
        var text = await _server.CallTextAsync("GetBswupSourceFile", new { path = "service-worker.published.js" });

        Assert.Contains("Did you mean", text);
        Assert.Contains("Demo/Client/wwwroot/service-worker.published.js", text);
    }

    [TestMethod]
    public async Task GetBswupSourceFile_ForNothingLikeAPath_PointsAtTheListing()
    {
        var text = await _server.CallTextAsync("GetBswupSourceFile", new { path = "zzz-not-a-file" });

        Assert.Contains("Call GetBswupSourceFiles for the full list", text);
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

        Assert.IsGreaterThan(16_000, BswupSourceCatalog.GetSourceFile(longest.Path)!.Length,
            "no embedded source file is long enough to be windowed - the cap is untested");

        var first = await _server.CallTextAsync("GetBswupSourceFile", new { path = longest.Path });

        Assert.StartsWith("[lines 1-", first);
        Assert.Contains($"startLine: ", first);
        Assert.IsLessThan(17_000, first.Length, $"{longest.Path} came back as {first.Length} characters");

        var next = int.Parse(System.Text.RegularExpressions.Regex.Match(first, @"startLine: (?<line>\d+)").Groups["line"].Value);
        var second = await _server.CallTextAsync("GetBswupSourceFile", new { path = longest.Path, startLine = next });

        Assert.StartsWith($"[lines {next}-", second);
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

        Assert.Contains($"{lines} lines", past);
        Assert.Contains($"no line {lines + 1}", past);
        Assert.Contains($"between 1 and {lines}", past);
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

            Assert.StartsWith(header, last,
                $"the last line of '{file.Path}' does not line up with the {file.Lines} lines the listing advertises");
        }
    }
}
