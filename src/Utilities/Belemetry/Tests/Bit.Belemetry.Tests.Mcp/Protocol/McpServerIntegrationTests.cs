using System.Text.Json;
using System.Net.Http.Json;
using ModelContextProtocol.Protocol;
using Bit.Belemetry.Tests.Mcp.TestInfra;

namespace Bit.Belemetry.Tests.Mcp.Protocol;

/// <summary>
/// The server as an agent meets it: the real <c>Program.cs</c> hosted in memory, spoken to over the MCP HTTP
/// transport by the real client. A tool that is not registered, a DTO the schema generator cannot describe, an
/// argument name that differs from the parameter name - each passes every method-level test and breaks the server
/// for every client, and each fails here.
/// </summary>
[TestClass]
public class McpServerIntegrationTests
{
    private static BelemetryMcpServerFixture _server = null!;

    [ClassInitialize]
    public static async Task StartServerAsync(TestContext _) => _server = await BelemetryMcpServerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopServerAsync() => await _server.DisposeAsync();

    [TestMethod]
    public void Server_CompletesTheHandshake_AdvertisingToolsPromptsAndResources()
    {
        Assert.IsNotNull(_server.Client.ServerCapabilities);
        Assert.IsNotNull(_server.Client.ServerCapabilities.Tools, "The server does not advertise tools.");
        Assert.IsNotNull(_server.Client.ServerCapabilities.Prompts, "The server does not advertise prompts.");
        Assert.IsNotNull(_server.Client.ServerCapabilities.Resources, "The server does not advertise resources.");
    }

    [TestMethod]
    public async Task ListTools_ExposesExactlyTheToolsTheAttributesDeclare_AndNoMoreThanSeven()
    {
        var overTheWire = (await _server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken)).Select(tool => tool.Name).ToArray();
        var declared = new McpController().GetMcpCatalog().Tools.Select(tool => tool.Name).ToArray();

        Assert.AreSequenceEqual(declared, overTheWire, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, $"Over the wire: {string.Join(", ", overTheWire.Order())}.");
        Assert.IsLessThanOrEqualTo(7, overTheWire.Length, "The tool surface is meant to stay small.");
    }

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
            [nameof(McpController.GetBelemetrySetupGuide)] = ["host"],
            [nameof(McpController.SearchBelemetry)] = ["query", "limit"],
            [nameof(McpController.GetBelemetryApiDetails)] = ["typeName"],
            [nameof(McpController.GenerateBelemetryConfiguration)] = ["host", "exporters", "consent", "features", "transport"],
            [nameof(McpController.ReviewBelemetryConfiguration)] = ["code"],
            [nameof(McpController.LookupBelemetryConventions)] = ["query"],
            [nameof(McpController.GetBelemetrySourceFile)] = ["path", "startLine", "endLine"],
        })
        {
            var properties = tools[name].ProtocolTool.InputSchema.GetProperty("properties");

            foreach (var argument in expected)
            {
                Assert.IsTrue(properties.TryGetProperty(argument, out _), $"'{name}' has no '{argument}' argument in its schema.");
            }
        }
    }

    [TestMethod]
    public async Task CallTool_EveryToolTheServerExposes_AnswersWithoutAnError()
    {
        var arguments = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.Ordinal)
        {
            [nameof(McpController.GetBelemetrySetupGuide)] = new Dictionary<string, object?> { ["host"] = "webassembly" },
            [nameof(McpController.SearchBelemetry)] = new Dictionary<string, object?> { ["query"] = "consent banner", ["limit"] = 5 },
            [nameof(McpController.GetBelemetryApiDetails)] = new Dictionary<string, object?> { ["typeName"] = "BelemetryOptions" },
            [nameof(McpController.GenerateBelemetryConfiguration)] = new Dictionary<string, object?> { ["host"] = "auto", ["exporters"] = "otlp,azure" },
            [nameof(McpController.ReviewBelemetryConfiguration)] = new Dictionary<string, object?> { ["code"] = "options.AddOtlpExporter(\"http://localhost:4318/v1/traces\");" },
            [nameof(McpController.LookupBelemetryConventions)] = new Dictionary<string, object?> { ["query"] = "session.id" },
            [nameof(McpController.GetBelemetrySourceFile)] = new Dictionary<string, object?> { ["path"] = "Demo/Server/Program.cs" },
        };

        var called = 0;

        foreach (var tool in await _server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken))
        {
            var result = await _server.Client.CallToolAsync(tool.Name, arguments.GetValueOrDefault(tool.Name) ?? new Dictionary<string, object?>(), cancellationToken: TestContext.CancellationToken);

            Assert.AreNotEqual(true, result.IsError, $"'{tool.Name}' answered with an error: {Render(result)}");
            Assert.AreNotEqual(string.Empty, Render(result).Trim(), $"'{tool.Name}' answered with empty content.");

            called++;
        }

        Assert.AreEqual(7, called);
    }

    [TestMethod]
    public async Task CallTool_TheReviewer_ReturnsItsFindingsAsData()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.ReviewBelemetryConfiguration),
            new Dictionary<string, object?> { ["code"] = "builder.Services.AddBitBelemetryServices(o =>\n{\n    o.AddOtlpExporter(\"http://localhost:4318/v1/traces\");\n});" }, cancellationToken: TestContext.CancellationToken);

        var findings = Payload(result).GetProperty("findings");

        Assert.AreNotEqual(0, findings.GetArrayLength());
        Assert.AreEqual("BLM002", findings[0].GetProperty("rule").GetString());
        Assert.AreEqual(3, findings[0].GetProperty("line").GetInt32());
    }

    [TestMethod]
    public async Task CallTool_AnUnknownType_NamesWhatDoesExist_InsteadOfFailing()
    {
        var result = await _server.Client.CallToolAsync(nameof(McpController.GetBelemetryApiDetails), new Dictionary<string, object?> { ["typeName"] = "ConsentOptions" }, cancellationToken: TestContext.CancellationToken);

        Assert.AreNotEqual(true, result.IsError);
        Assert.Contains("BelemetryConsentOptions", Render(result));
    }

    [TestMethod]
    public async Task CallTool_AToolThatDoesNotExist_Fails_WithoutTakingTheSessionDown()
    {
        await Assert.ThrowsAsync<ModelContextProtocol.McpException>(
            () => _server.Client.CallToolAsync("GetBelemetryNothing", new Dictionary<string, object?>(), cancellationToken: TestContext.CancellationToken).AsTask());

        Assert.IsNotEmpty(await _server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task ListPrompts_ExposesEveryWorkflow_WithADescription()
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
        var result = await _server.Client.GetPromptAsync("set_up_belemetry", new Dictionary<string, object?> { ["host"] = "hybrid", ["destination"] = "Honeycomb" }, cancellationToken: TestContext.CancellationToken);

        var text = string.Join('\n', result.Messages.Select(message => (message.Content as TextContentBlock)?.Text));

        Assert.Contains("hybrid", text);
        Assert.Contains("Honeycomb", text);
        Assert.Contains(nameof(McpController.ReviewBelemetryConfiguration), text);

        var review = await _server.Client.GetPromptAsync("review_belemetry_setup", new Dictionary<string, object?> { ["code"] = "options.Consent.Default = BelemetryFeatures.All;" }, cancellationToken: TestContext.CancellationToken);
        var expectedValue = string.Join('\n', review.Messages.Select(message => (message.Content as TextContentBlock)?.Text));

        Assert.Contains("BelemetryFeatures.All", expectedValue);

        var banner = await _server.Client.GetPromptAsync("add_consent_banner", cancellationToken: TestContext.CancellationToken);

        Assert.IsNotEmpty(banner.Messages);
    }

    [TestMethod]
    public async Task ListResources_ExposesTheFixedOnes_AndTheTemplatedOne()
    {
        var resources = await _server.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken);
        var templates = await _server.Client.ListResourceTemplatesAsync(cancellationToken: TestContext.CancellationToken);

        var uris = resources.Select(resource => resource.Uri).Concat(templates.Select(template => template.UriTemplate)).ToArray();
        var declared = new McpController().GetMcpCatalog().Resources.Select(resource => resource.Uri).ToArray();

        Assert.AreSequenceEqual(declared, uris, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, $"Over the wire: {string.Join(", ", uris.Order())}.");
    }

    [TestMethod]
    public async Task ReadResource_TheFixedResources_ReturnTheirDocuments()
    {
        foreach (var resource in await _server.Client.ListResourcesAsync(cancellationToken: TestContext.CancellationToken))
        {
            var result = await resource.ReadAsync(cancellationToken: TestContext.CancellationToken);

            var text = (result.Contents[0] as TextResourceContents)?.Text;

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{resource.Uri}' returned empty text.");
        }

        var conventions = await _server.Client.ReadResourceAsync("belemetry://docs/conventions", cancellationToken: TestContext.CancellationToken);

        Assert.Contains("web_vital.name", ((TextResourceContents)conventions.Contents[0]).Text!);
    }

    [TestMethod]
    public async Task ReadResource_ASourceFilePath_SurvivesUriEscaping()
    {
        var result = await _server.Client.ReadResourceAsync($"belemetry://source/{Uri.EscapeDataString("Demo/Server/Program.cs")}", cancellationToken: TestContext.CancellationToken);

        var text = ((TextResourceContents)result.Contents[0]).Text!;

        Assert.Contains("MapMcp", text);
        Assert.AreEqual(BelemetrySourceCatalog.GetSource("Demo/Server/Program.cs"), text);
    }

    [TestMethod]
    public async Task Server_AnswersConcurrentCalls_Independently()
    {
        var calls = new (string Tool, Dictionary<string, object?> Arguments)[]
        {
            (nameof(McpController.SearchBelemetry), new() { ["query"] = "web vitals" }),
            (nameof(McpController.GetBelemetryApiDetails), new() { ["typeName"] = "BelemetryBrowserOptions" }),
            (nameof(McpController.LookupBelemetryConventions), new() { ["query"] = "PageView" }),
            (nameof(McpController.GetBelemetrySetupGuide), new() { ["host"] = "server" }),
            (nameof(McpController.GenerateBelemetryConfiguration), new() { ["host"] = "hybrid" }),
        };

        var results = await Task.WhenAll(calls.Select(call => _server.Client.CallToolAsync(call.Tool, call.Arguments, cancellationToken: TestContext.CancellationToken).AsTask()));

        for (var i = 0; i < results.Length; i++)
        {
            Assert.AreNotEqual(true, results[i].IsError, $"'{calls[i].Tool}' failed under concurrency: {Render(results[i])}");
        }
    }

    /// <summary>
    /// The tools are also plain HTTP GETs under /api/mcp, which is what the demo's MCP page calls. That route is
    /// separate wiring from the MCP transport and fails separately.
    /// </summary>
    [TestMethod]
    public async Task HttpMirror_TheSameToolsAreAlsoReachableOverPlainHttp()
    {
        var catalog = await _server.Http.GetFromJsonAsync<McpCatalogDto>("/api/mcp/GetMcpCatalog", TestContext.CancellationToken);

        Assert.IsNotNull(catalog);
        Assert.HasCount(7, catalog.Tools);
        Assert.HasCount(3, catalog.Prompts);
        Assert.HasCount(3, catalog.Resources);
        Assert.IsTrue(catalog.Tools.Single(t => t.Name == nameof(McpController.GetBelemetryApiDetails)).Parameters.Single().Required);

        var details = await _server.Http.GetFromJsonAsync<BelemetryApiDetailsResultDto>("/api/mcp/GetBelemetryApiDetails?typeName=BelemetryConsentOptions", TestContext.CancellationToken);
        var actualValue = details?.Details;

        Assert.IsNotNull(actualValue);
        Assert.AreEqual("BelemetryFeatures.None", details.Details.Members.Single(m => m.Name == "Default").Default);

        var review = await _server.Http.GetFromJsonAsync<BelemetryReviewDto>($"/api/mcp/ReviewBelemetryConfiguration?code={Uri.EscapeDataString("o.Consent.Default = BelemetryFeatures.All;")}", TestContext.CancellationToken);

        Assert.AreEqual("BLM004", review!.Findings.Single().Rule);

        var guide = await _server.Http.GetStringAsync("/api/mcp/GetBelemetrySetupGuide?host=hybrid", TestContext.CancellationToken);

        Assert.Contains("BelemetryInitializer", guide);
    }

    /// <summary>The answer as a client renders it: every text block the call returned.</summary>
    private static string Render(CallToolResult result) => string.Join('\n', result.Content.OfType<TextContentBlock>().Select(block => block.Text));

    /// <summary>The object a tool returned: structured content when the server sent it, otherwise the JSON in the text block.</summary>
    private static JsonElement Payload(CallToolResult result)
    {
        if (result.StructuredContent is JsonElement structured && structured.ValueKind is not JsonValueKind.Undefined)
        {
            return structured.ValueKind == JsonValueKind.Object && structured.TryGetProperty("result", out var wrapped) ? wrapped : structured;
        }

        return JsonDocument.Parse(Render(result)).RootElement;
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
