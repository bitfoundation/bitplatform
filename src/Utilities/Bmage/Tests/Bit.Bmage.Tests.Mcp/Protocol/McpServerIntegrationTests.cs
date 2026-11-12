using System.Text.Json;
using System.Net.Http.Json;
using ModelContextProtocol.Protocol;
using Bit.Bmage.Tests.Mcp.TestInfra;

namespace Bit.Bmage.Tests.Mcp.Protocol;

/// <summary>
/// The server as an agent meets it: the real <c>Program.cs</c> hosted in memory, spoken to over the
/// MCP HTTP transport by the real client.
/// <para>
/// This is the layer where production failures actually live. A tool that is not decorated is not
/// registered; a DTO the schema generator cannot describe fails at <c>tools/list</c>; an argument
/// name that differs from the parameter name arrives as null; a returned object that will not
/// serialise fails inside the call rather than in the method. Every one of those passes the
/// method-level tests in this suite and breaks the server for every client.
/// </para>
/// </summary>
[TestClass]
public class McpServerIntegrationTests
{
    private static BmageMcpServerFixture _server = null!;

    [ClassInitialize]
    public static async Task StartServerAsync(TestContext _) => _server = await BmageMcpServerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopServerAsync() => await _server.DisposeAsync();

    /// <summary>
    /// What the client negotiated is the interesting part of the handshake: a server advertising no
    /// capability for one of its halves has that half wired but unreachable.
    /// </summary>
    [TestMethod]
    public void Server_CompletesTheHandshake_AdvertisingEveryHalfOfItsSurface()
    {
        Assert.IsNotNull(_server.Client.ServerCapabilities);

        Assert.IsNotNull(_server.Client.ServerCapabilities.Tools, "The server does not advertise tools.");
        Assert.IsNotNull(_server.Client.ServerCapabilities.Prompts, "The server does not advertise prompts.");
        Assert.IsNotNull(_server.Client.ServerCapabilities.Resources, "The server does not advertise resources.");
        Assert.IsNotNull(_server.Client.ServerCapabilities.Completions, "The server does not advertise completions.");
    }

    /// <summary>
    /// The name a person sees in their client and the version they would quote in a bug report are
    /// the library's - and the instructions reach the model before any tool is called.
    /// </summary>
    [TestMethod]
    public void Server_IdentifiesItselfAsBmage_AndHandsOverItsWorkingRules()
    {
        var info = _server.Client.ServerInfo;

        Assert.AreEqual("bit-bmage", info.Name);
        Assert.AreEqual(BmageServerInstructions.BmageVersion, info.Version);

        var instructions = _server.Client.ServerInstructions;

        Assert.IsFalse(string.IsNullOrWhiteSpace(instructions), "The server sends no instructions.");
        Assert.Contains(nameof(McpController.SearchBmage), instructions);
        Assert.Contains("AddBitBmageHeadlessServices", instructions);
        Assert.Contains("OnAfterRenderAsync", instructions);
        Assert.Contains(BmageServerInstructions.BmageVersion, instructions);
    }

    /// <summary>
    /// What the server registers over the protocol has to be what the demo page - and this suite -
    /// reads off the attributes. A tool missing its registration is invisible here and present
    /// everywhere else.
    /// </summary>
    [TestMethod]
    public async Task ListTools_ExposesExactlyTheToolsTheAttributesDeclare()
    {
        var overTheWire = (await _server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).Select(tool => tool.Name).ToArray();
        var declared = new McpController().GetMcpCatalog().Tools.Select(tool => tool.Name).ToArray();

        Assert.AreSequenceEqual(declared, overTheWire, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, $"Over the wire: {string.Join(", ", overTheWire.Order())}.");
    }

    /// <summary>The description and the input schema are the entire basis on which a model chooses a tool.</summary>
    [TestMethod]
    public async Task ListTools_EveryTool_ArrivesWithADescriptionAndAnInputSchema()
    {
        foreach (var tool in await _server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken))
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Description), $"'{tool.Name}' arrived with no description.");

            var schema = tool.ProtocolTool.InputSchema;

            Assert.AreEqual(JsonValueKind.Object, schema.ValueKind, $"'{tool.Name}' has no input schema.");
            Assert.AreEqual("object", schema.GetProperty("type").GetString());
        }
    }

    [TestMethod]
    public async Task ListTools_TheArgumentNames_AreTheOnesTheMethodsDeclare()
    {
        var tools = (await _server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).ToDictionary(tool => tool.Name, StringComparer.Ordinal);

        foreach (var (name, expected) in new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(McpController.SearchBmage)] = ["query", "limit"],
            [nameof(McpController.GetBmageSetupGuide)] = ["renderMode"],
            [nameof(McpController.GetBmageRecipe)] = ["id"],
            [nameof(McpController.GetBmageDocument)] = ["name", "section"],
            [nameof(McpController.GetBmageApiList)] = ["filter"],
            [nameof(McpController.GetBmageApiDetails)] = ["typeName"],
            [nameof(McpController.GetBmageSourceFiles)] = ["filter"],
            [nameof(McpController.GetBmageSourceFile)] = ["path", "fromLine", "toLine"],
            [nameof(McpController.ReviewBmageCode)] = ["code"],
            [nameof(McpController.RunBmagePipeline)] = ["pipeline", "source", "width", "height", "formats", "qualities", "maxFileSize"],
            [nameof(McpController.CompareBmageMetadataPolicies)] = ["format"],
        })
        {
            var properties = tools[name].ProtocolTool.InputSchema.GetProperty("properties");

            foreach (var argument in expected)
            {
                Assert.IsTrue(properties.TryGetProperty(argument, out _), $"'{name}' has no '{argument}' argument in its schema.");
            }

            // The cancellation token is the transport's to supply; offered as an argument, a model would fill it in.
            Assert.IsFalse(properties.TryGetProperty("cancellationToken", out _), $"'{name}' exposes its CancellationToken as an argument.");
        }
    }

    /// <summary>
    /// Every tool, called for real. Serialisation of the answer happens inside the call, so a DTO that
    /// will not round-trip fails here and nowhere else in this suite.
    /// </summary>
    [TestMethod]
    public async Task CallTool_EveryToolTheServerExposes_AnswersWithoutAnError()
    {
        var arguments = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.Ordinal)
        {
            [nameof(McpController.SearchBmage)] = new Dictionary<string, object?> { ["query"] = "avatar size budget", ["limit"] = 5 },
            [nameof(McpController.GetBmageSetupGuide)] = new Dictionary<string, object?> { ["renderMode"] = "wasm" },
            [nameof(McpController.GetBmageRecipe)] = new Dictionary<string, object?> { ["id"] = "avatar-crop" },
            [nameof(McpController.GetBmageDocument)] = new Dictionary<string, object?> { ["name"] = "pipelines", ["section"] = "The optimiser" },
            [nameof(McpController.GetBmageApiDetails)] = new Dictionary<string, object?> { ["typeName"] = "ImageExportOptions" },
            [nameof(McpController.GetBmageSourceFile)] = new Dictionary<string, object?> { ["path"] = "Demo/Server/Program.cs" },
            [nameof(McpController.ReviewBmageCode)] = new Dictionary<string, object?> { ["code"] = "@inject ImageProcessor Processor" },
            [nameof(McpController.RunBmagePipeline)] = new Dictionary<string, object?> { ["pipeline"] = "MaxSize(320, 320)", ["width"] = 640, ["height"] = 480 },
            [nameof(McpController.CompareBmageMetadataPolicies)] = new Dictionary<string, object?> { ["format"] = "jpeg" },
        };

        var called = 0;

        foreach (var tool in await _server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken))
        {
            var result = await _server.Client.CallToolAsync(tool.Name, arguments.GetValueOrDefault(tool.Name) ?? new Dictionary<string, object?>(), cancellationToken: TestContext.CancellationToken);

            Assert.AreNotEqual(true, result.IsError, $"'{tool.Name}' answered with an error: {Render(result)}");
            Assert.IsNotEmpty(result.Content, $"'{tool.Name}' answered with nothing at all.");
            Assert.AreNotEqual(string.Empty, Render(result).Trim(), $"'{tool.Name}' answered with empty content.");

            called++;
        }

        Assert.IsGreaterThanOrEqualTo(14, called, $"Only {called} tools were called.");
    }

    /// <summary>
    /// The engine tool's answer is data, not prose: a client reads Bytes and OutputWidth as numbers.
    /// </summary>
    [TestMethod]
    public async Task CallTool_RunBmagePipeline_ReturnsItsMeasurementsAsStructuredData()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.RunBmagePipeline),
            new Dictionary<string, object?>
            {
                ["pipeline"] = "ImagePipeline.Create().AutoOrient().MaxSize(400, 400).Sharpen(0.3f)",
                ["source"] = "photo",
                ["width"] = 800,
                ["height"] = 600,
                ["formats"] = "jpeg, png",
                ["qualities"] = "0.6, 0.9",
                ["maxFileSize"] = 1_000_000,
            }, cancellationToken: TestContext.CancellationToken);

        Assert.AreNotEqual(true, result.IsError, Render(result));

        var payload = Payload(result);

        Assert.AreEqual(400, payload.GetProperty("outputWidth").GetInt32());
        Assert.AreEqual(300, payload.GetProperty("outputHeight").GetInt32());

        var encodings = payload.GetProperty("encodings");
        Assert.AreEqual(3, encodings.GetArrayLength(), "Two JPEG qualities and one PNG were asked for.");

        foreach (var encoding in encodings.EnumerateArray())
        {
            Assert.IsGreaterThan(0, encoding.GetProperty("bytes").GetInt64());
        }

        Assert.IsTrue(payload.GetProperty("budgets")[0].GetProperty("met").GetBoolean());
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.GetProperty("reading").GetString()));
    }

    [TestMethod]
    public async Task CallTool_TheNumericArguments_SurviveTheJsonRoundTrip()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.RunBmagePipeline),
            new Dictionary<string, object?> { ["pipeline"] = "Crop(0, 0, 100, 50)", ["width"] = 321, ["height"] = 123, ["formats"] = "png" }, cancellationToken: TestContext.CancellationToken);

        var payload = Payload(result);

        Assert.AreEqual(321, payload.GetProperty("sourceWidth").GetInt32());
        Assert.AreEqual(123, payload.GetProperty("sourceHeight").GetInt32());
        Assert.AreEqual(100, payload.GetProperty("outputWidth").GetInt32());
        Assert.AreEqual(50, payload.GetProperty("outputHeight").GetInt32());
    }

    [TestMethod]
    public async Task CallTool_TheCodeReview_ReturnsItsFindingsAsData()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.ReviewBmageCode),
            new Dictionary<string, object?>
            {
                ["code"] = "@page \"/x\"\n@inject ImageProcessor Processor\n\n<p>hi</p>",
            }, cancellationToken: TestContext.CancellationToken);

        var payload = Payload(result);

        Assert.IsFalse(payload.GetProperty("passed").GetBoolean());

        var findings = payload.GetProperty("findings");

        Assert.AreNotEqual(0, findings.GetArrayLength());
        Assert.AreEqual("processor-injected-in-browser", findings[0].GetProperty("rule").GetString());
        Assert.AreEqual(2, findings[0].GetProperty("line").GetInt32());
    }

    [TestMethod]
    public async Task CallTool_TheMetadataComparison_ReportsTheLocationOnlyUnderPreserve()
    {
        var result = await _server.Client.CallToolAsync(nameof(McpController.CompareBmageMetadataPolicies), new Dictionary<string, object?> { ["format"] = "jpeg" }, cancellationToken: TestContext.CancellationToken);

        var policies = Payload(result).GetProperty("policies").EnumerateArray()
            .ToDictionary(policy => policy.GetProperty("policy").GetString()!, policy => policy.GetProperty("leaksLocation").GetBoolean());

        Assert.IsFalse(policies["Remove"]);
        Assert.IsFalse(policies["StripSensitive"]);
        Assert.IsTrue(policies["Preserve"]);
    }

    /// <summary>
    /// An unreadable pipeline is a correctable mistake, not a failed call. Thrown, the protocol reduces
    /// it to "an error occurred invoking RunBmagePipeline" and the caller learns nothing; returned as
    /// data, the explanation survives.
    /// </summary>
    [TestMethod]
    public async Task CallTool_AnUnreadableArgument_ComesBackAsAnExplanation_NotAsAProtocolError()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.RunBmagePipeline),
            new Dictionary<string, object?> { ["pipeline"] = "ImagePipeline.Create().Teleport(3)" }, cancellationToken: TestContext.CancellationToken);

        Assert.AreNotEqual(true, result.IsError, "A correctable mistake was reported as a failed call.");

        var error = Payload(result).GetProperty("error").GetString();

        Assert.IsFalse(string.IsNullOrWhiteSpace(error));
        Assert.Contains("Teleport", error);
        Assert.Contains("MaxSize", error, "The explanation does not say what can be called instead.");
    }

    [TestMethod]
    public async Task CallTool_AMissingId_NamesWhatDoesExist()
    {
        var result = await _server.Client.CallToolAsync(nameof(McpController.GetBmageRecipe), new Dictionary<string, object?> { ["id"] = "teleport" }, cancellationToken: TestContext.CancellationToken);

        Assert.AreNotEqual(true, result.IsError);
        Assert.Contains("avatar-crop", Render(result));
    }

    [TestMethod]
    public async Task CallTool_AnOptionalArgumentOmitted_UsesTheDeclaredDefault()
    {
        var result = await _server.Client.CallToolAsync(nameof(McpController.RunBmagePipeline), new Dictionary<string, object?> { ["pipeline"] = "MaxSize(100, 100)" }, cancellationToken: TestContext.CancellationToken);

        var payload = Payload(result);

        Assert.AreEqual(BmagePipelineLab.DefaultWidth, payload.GetProperty("sourceWidth").GetInt32());
        Assert.AreEqual(BmagePipelineLab.DefaultHeight, payload.GetProperty("sourceHeight").GetInt32());
        Assert.AreEqual("photo", payload.GetProperty("source").GetString());
    }

    [TestMethod]
    public async Task CallTool_AToolThatDoesNotExist_Fails_WithoutTakingTheSessionDown()
    {
        var error = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(
            () => _server.Client.CallToolAsync("GetBmageNothing", new Dictionary<string, object?>(), cancellationToken: TestContext.CancellationToken).AsTask());

        Assert.Contains("GetBmageNothing", error.Message);

        Assert.IsNotEmpty(await _server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ListPrompts_ExposesEveryWorkflow_WithItsArguments()
    {
        var prompts = await _server.Client.ListPromptsAsync(cancellationToken: TestContext.CancellationToken);

        var declared = new McpController().GetMcpCatalog().Prompts.Select(prompt => prompt.Name).ToArray();

        Assert.AreSequenceEqual(declared, prompts.Select(prompt => prompt.Name).ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);

        foreach (var prompt in prompts)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(prompt.Description), $"'{prompt.Name}' has no description.");
        }
    }

    [TestMethod]
    public async Task GetPrompt_RendersTheWorkflowWithTheArgumentsItWasGiven()
    {
        var result = await _server.Client.GetPromptAsync(
            "build-with-bmage",
            new Dictionary<string, object?>
            {
                ["request"] = "let users crop an avatar to a circle",
                ["renderMode"] = "server",
            }, cancellationToken: TestContext.CancellationToken);

        Assert.IsNotEmpty(result.Messages);

        var text = string.Join('\n', result.Messages.Select(message => (message.Content as TextContentBlock)?.Text));

        Assert.Contains("let users crop an avatar to a circle", text);
        Assert.Contains("server", text);
        Assert.Contains(nameof(McpController.ReviewBmageCode), text);
        Assert.Contains(nameof(McpController.RunBmagePipeline), text);
    }

    [TestMethod]
    public async Task ListResources_ExposesTheFixedOnes_AndTheTemplatedOnes()
    {
        var resources = await _server.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken);
        var templates = await _server.Client.ListResourceTemplatesAsync(cancellationToken: TestContext.CancellationToken);

        var uris = resources.Select(resource => resource.Uri)
            .Concat(templates.Select(template => template.UriTemplate))
            .ToArray();

        var declared = new McpController().GetMcpCatalog().Resources.Select(resource => resource.Name).ToArray();

        Assert.AreSequenceEqual(declared, uris, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, $"Over the wire: {string.Join(", ", uris.Order())}.");
    }

    [TestMethod]
    public async Task ReadResource_TheFixedResources_ReturnTheirDocuments()
    {
        foreach (var resource in await _server.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken))
        {
            var result = await resource.ReadAsync(cancellationToken: TestContext.CancellationToken);

            Assert.IsNotEmpty(result.Contents, $"'{resource.Uri}' returned nothing.");

            var text = (result.Contents[0] as TextResourceContents)?.Text;

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{resource.Uri}' returned empty text.");
        }
    }

    [TestMethod]
    public async Task ReadResource_ATemplatedResource_ResolvesItsArgument()
    {
        var document = await _server.Client.ReadResourceAsync("bmage://docs/pipelines", cancellationToken: TestContext.CancellationToken);
        Assert.Contains("## The optimiser", ((TextResourceContents)document.Contents[0]).Text!);

        var type = await _server.Client.ReadResourceAsync("bmage://api/ImageExportOptions", cancellationToken: TestContext.CancellationToken);
        Assert.Contains("MaxFileSize", ((TextResourceContents)type.Contents[0]).Text!);

        var setup = await _server.Client.ReadResourceAsync("bmage://setup/headless", cancellationToken: TestContext.CancellationToken);
        Assert.Contains("AddBitBmageHeadlessServices", ((TextResourceContents)setup.Contents[0]).Text!);

        var recipe = await _server.Client.ReadResourceAsync("bmage://recipes/strip-location", cancellationToken: TestContext.CancellationToken);
        Assert.Contains("MetadataPolicy.StripSensitive", ((TextResourceContents)recipe.Contents[0]).Text!);
    }

    /// <summary>
    /// A path is a URI segment here, so the slashes in it have to be escaped and unescaped again.
    /// Getting that wrong makes every demo page unreadable through the resource while every one of
    /// them stays readable through the tool.
    /// </summary>
    [TestMethod]
    public async Task ReadResource_ASourceFilePath_SurvivesUriEscaping()
    {
        var result = await _server.Client.ReadResourceAsync($"bmage://source/{Uri.EscapeDataString("Demo/Server/Program.cs")}", cancellationToken: TestContext.CancellationToken);

        var text = ((TextResourceContents)result.Contents[0]).Text!;

        Assert.Contains("MapMcp", text);
        Assert.AreEqual(BmageSourceCatalog.GetSourceFile("Demo/Server/Program.cs"), text);
    }

    /// <summary>
    /// A template placeholder is a key into a closed set; the completion handler is what lets a client
    /// offer its values instead of a blank box.
    /// </summary>
    [TestMethod]
    public async Task Complete_ATemplatePlaceholder_OffersTheValuesThatExist()
    {
        var types = await _server.Client.CompleteAsync(new ResourceTemplateReference { Uri = "bmage://api/{typeName}" }, "typeName", "ImageExp", cancellationToken: TestContext.CancellationToken);

        Assert.Contains("ImageExportOptions", types.Completion.Values.ToArray());
        Assert.Contains("ImageExporter", types.Completion.Values.ToArray());

        var recipes = await _server.Client.CompleteAsync(new PromptReference { Name = "build-with-bmage" }, "renderMode", "head", cancellationToken: TestContext.CancellationToken);

        Assert.AreSequenceEqual(new[] { "headless" }, recipes.Completion.Values.ToArray());
    }

    /// <summary>
    /// Several agents share one server. The catalogs are built lazily and once, and the engine runs are
    /// gated; nothing may fail or answer differently because it was asked at the same time as
    /// something else.
    /// </summary>
    [TestMethod]
    public async Task Server_AnswersConcurrentCalls_Independently()
    {
        var calls = new (string Tool, Dictionary<string, object?> Arguments)[]
        {
            (nameof(McpController.RunBmagePipeline), new() { ["pipeline"] = "MaxSize(300, 300)", ["width"] = 600, ["height"] = 400, ["formats"] = "png" }),
            (nameof(McpController.SearchBmage), new() { ["query"] = "strip gps" }),
            (nameof(McpController.GetBmageApiList), new()),
            (nameof(McpController.CompareBmageMetadataPolicies), new()),
            (nameof(McpController.ReviewBmageCode), new() { ["code"] = "MetadataPolicy.Preserve" }),
            (nameof(McpController.RunBmagePipeline), new() { ["pipeline"] = "Cover(200, 200)", ["width"] = 640, ["height"] = 480, ["formats"] = "jpeg" }),
            (nameof(McpController.GetBmageRecipes), new()),
            (nameof(McpController.RunBmagePipeline), new() { ["pipeline"] = "MaxSize(300, 300)", ["width"] = 600, ["height"] = 400, ["formats"] = "png" }),
        };

        var results = await Task.WhenAll(calls.Select(call => _server.Client.CallToolAsync(call.Tool, call.Arguments, cancellationToken: TestContext.CancellationToken).AsTask()));

        for (int i = 0; i < results.Length; i++)
        {
            Assert.AreNotEqual(true, results[i].IsError, $"'{calls[i].Tool}' failed under concurrency: {Render(results[i])}");
        }

        // The same run asked twice in that crowd encodes to the same bytes: the engine is deterministic.
        Assert.AreEqual(Payload(results[0]).GetProperty("encodings")[0].GetProperty("bytes").GetInt64(),
                        Payload(results[7]).GetProperty("encodings")[0].GetProperty("bytes").GetInt64());
    }

    /// <summary>
    /// The tools are also plain HTTP GETs, which is what the demo's own /mcp-server page calls. That
    /// route is separate wiring from the MCP transport and fails separately - but it is the same host,
    /// so it is reached through the fixture rather than a second startup.
    /// </summary>
    [TestMethod]
    public async Task Server_TheSameToolsAreAlsoReachableOverPlainHttp()
    {
        var http = _server.Http;

        var catalog = await http.GetFromJsonAsync<BmageMcpCatalogDto>("/api/Mcp/GetMcpCatalog", TestContext.CancellationToken);

        Assert.IsNotNull(catalog);
        Assert.IsNotEmpty(catalog.Tools);
        Assert.DoesNotContain(parameter => parameter.Contains("cancellationToken", StringComparison.Ordinal), catalog.Tools.SelectMany(tool => tool.Parameters));

        var run = await http.GetFromJsonAsync<BmagePipelineRunDto>(
            $"/api/Mcp/RunBmagePipeline?pipeline={Uri.EscapeDataString("MaxSize(200, 200)")}&width=400&height=300&formats=jpeg", TestContext.CancellationToken);

        Assert.IsNotNull(run);
        Assert.AreEqual(200, run.OutputWidth);
        Assert.IsNull(run.Error);

        var review = await http.GetFromJsonAsync<BmageReviewDto>($"/api/Mcp/ReviewBmageCode?code={Uri.EscapeDataString("MetadataPolicy.Preserve")}", TestContext.CancellationToken);

        Assert.IsNotNull(review);
        Assert.AreEqual("metadata-preserve-keeps-location", review.Findings.Single().Rule);

        // The page runs in the browser, which reads the same JSON: nulls are left out, as the MCP transport leaves them out.
        var raw = await http.GetStringAsync($"/api/Mcp/GetBmageRecipe?id=nothing", TestContext.CancellationToken);
        Assert.IsFalse(raw.Contains("\"recipe\":null", StringComparison.Ordinal), raw);
    }

    /// <summary>The answer as a client renders it: every text block the call returned.</summary>
    private static string Render(CallToolResult result)
        => string.Join('\n', result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    /// <summary>
    /// The object a tool returned. Structured content when the server sent it, and otherwise the JSON
    /// in the text block - which is how a tool returning a bare object arrives.
    /// </summary>
    private static JsonElement Payload(CallToolResult result)
    {
        if (result.StructuredContent is JsonElement structured && structured.ValueKind is not JsonValueKind.Undefined)
        {
            // A tool returning a single value is wrapped under "result" by the schema generator.
            return structured.ValueKind == JsonValueKind.Object && structured.TryGetProperty("result", out var wrapped)
                ? wrapped
                : structured;
        }

        return JsonDocument.Parse(Render(result)).RootElement;
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
