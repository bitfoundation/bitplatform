using System.Text.Json;
using System.Net.Http.Json;
using ModelContextProtocol.Protocol;
using Bit.Bmotion.Tests.Mcp.TestInfra;

namespace Bit.Bmotion.Tests.Mcp.Protocol;

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
    private static BmotionMcpServerFixture _server = null!;

    [ClassInitialize]
    public static async Task StartServerAsync(TestContext _) => _server = await BmotionMcpServerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopServerAsync() => await _server.DisposeAsync();

    /// <summary>
    /// That the client connected at all is the handshake. What it negotiated is the interesting
    /// part: a server advertising no capability for one of the three halves has that half wired but
    /// unreachable, which looks from the outside like the feature simply not existing.
    /// </summary>
    [TestMethod]
    public void Server_CompletesTheHandshake_AdvertisingAllThreeHalvesOfItsSurface()
    {
        Assert.IsNotNull(_server.Client.ServerInfo);
        Assert.IsNotNull(_server.Client.ServerCapabilities);

        Assert.IsNotNull(_server.Client.ServerCapabilities.Tools, "The server does not advertise tools.");
        Assert.IsNotNull(_server.Client.ServerCapabilities.Prompts, "The server does not advertise prompts.");
        Assert.IsNotNull(_server.Client.ServerCapabilities.Resources, "The server does not advertise resources.");
    }

    /// <summary>
    /// What the server registers over the protocol has to be what the demo page - and this suite -
    /// reads off the attributes. A tool missing its registration is invisible here and present
    /// everywhere else.
    /// </summary>
    [TestMethod]
    public async Task ListTools_ExposesExactlyTheToolsTheAttributesDeclare()
    {
        var overTheWire = (await _server.Client.ListToolsAsync()).Select(tool => tool.Name).ToArray();
        var declared = new McpController().GetMcpCatalog().Tools.Select(tool => tool.Name).ToArray();

        CollectionAssert.AreEquivalent(declared, overTheWire,
                                       $"Over the wire: {string.Join(", ", overTheWire.Order())}.");
    }

    /// <summary>
    /// The description and the input schema are the entire basis on which a model chooses a tool and
    /// fills in its arguments. A DTO the generator cannot describe shows up as a missing schema here.
    /// </summary>
    [TestMethod]
    public async Task ListTools_EveryTool_ArrivesWithADescriptionAndAnInputSchema()
    {
        foreach (var tool in await _server.Client.ListToolsAsync())
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
        var tools = (await _server.Client.ListToolsAsync()).ToDictionary(tool => tool.Name, StringComparer.Ordinal);

        foreach (var (name, expected) in new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            [nameof(McpController.SearchBmotion)] = ["query", "limit"],
            [nameof(McpController.GetBmotionSetupGuide)] = ["renderMode"],
            [nameof(McpController.GetBmotionRecipe)] = ["id"],
            [nameof(McpController.SimulateBmotionTransition)] = ["transition", "from", "to"],
            [nameof(McpController.AnalyzeBmotionAnimation)] = ["properties", "transition"],
            [nameof(McpController.ReviewBmotionCode)] = ["code"],
            [nameof(McpController.GetBmotionGuideSection)] = ["heading"],
            [nameof(McpController.GetBmotionApiDetails)] = ["typeName"],
            [nameof(McpController.GetBmotionSourceFile)] = ["path"],
        })
        {
            var properties = tools[name].ProtocolTool.InputSchema.GetProperty("properties");

            foreach (var argument in expected)
            {
                Assert.IsTrue(properties.TryGetProperty(argument, out _),
                              $"'{name}' has no '{argument}' argument in its schema.");
            }
        }
    }

    /// <summary>
    /// Every tool, called for real. Serialisation of the answer happens inside the call, so a DTO
    /// that will not round-trip fails here and nowhere else in this suite.
    /// </summary>
    [TestMethod]
    public async Task CallTool_EveryToolTheServerExposes_AnswersWithoutAnError()
    {
        var arguments = new Dictionary<string, IReadOnlyDictionary<string, object?>>(StringComparer.Ordinal)
        {
            [nameof(McpController.SearchBmotion)] = new Dictionary<string, object?> { ["query"] = "staggered list", ["limit"] = 5 },
            [nameof(McpController.GetBmotionSetupGuide)] = new Dictionary<string, object?> { ["renderMode"] = "wasm" },
            [nameof(McpController.GetBmotionRecipe)] = new Dictionary<string, object?> { ["id"] = "staggered-list" },
            [nameof(McpController.SimulateBmotionTransition)] = new Dictionary<string, object?> { ["transition"] = "spring(stiffness: 260, damping: 12)" },
            [nameof(McpController.CompareBmotionTransitions)] = new Dictionary<string, object?> { ["transitions"] = "tween(0.3); spring(bounce: 0.2, duration: 0.4)" },
            [nameof(McpController.AnalyzeBmotionAnimation)] = new Dictionary<string, object?> { ["properties"] = "x, opacity", ["transition"] = "tween(0.4)" },
            [nameof(McpController.ReviewBmotionCode)] = new Dictionary<string, object?> { ["code"] = "<Bmotion Animate=\"Bm.To(x: 100)\"><div /></Bmotion>" },
            [nameof(McpController.GetBmotionGuideSection)] = new Dictionary<string, object?> { ["heading"] = "Installation" },
            [nameof(McpController.GetBmotionApiDetails)] = new Dictionary<string, object?> { ["typeName"] = "BmSpring" },
            [nameof(McpController.GetBmotionSourceFile)] = new Dictionary<string, object?> { ["path"] = "Demo/Server/Program.cs" },
        };

        var called = 0;

        foreach (var tool in await _server.Client.ListToolsAsync())
        {
            var result = await _server.Client.CallToolAsync(
                tool.Name,
                arguments.GetValueOrDefault(tool.Name) ?? new Dictionary<string, object?>());

            Assert.IsFalse(result.IsError is true, $"'{tool.Name}' answered with an error: {Render(result)}");
            Assert.AreNotEqual(0, result.Content.Count, $"'{tool.Name}' answered with nothing at all.");
            Assert.AreNotEqual(string.Empty, Render(result).Trim(), $"'{tool.Name}' answered with empty content.");

            called++;
        }

        Assert.IsTrue(called >= 15, $"Only {called} tools were called.");
    }

    /// <summary>
    /// The answers that are objects come back as structured content, not only as a rendered string -
    /// which is what lets a client read SettleSeconds as a number rather than parsing prose.
    /// </summary>
    [TestMethod]
    public async Task CallTool_AMeasuringTool_ReturnsItsNumbersAsStructuredData()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.SimulateBmotionTransition),
            new Dictionary<string, object?> { ["transition"] = "spring(stiffness: 100, damping: 20)", ["from"] = 0, ["to"] = 100 });

        Assert.IsFalse(result.IsError is true, Render(result));

        var payload = Payload(result);

        Assert.AreEqual("Spring", payload.GetProperty("kind").GetString());
        Assert.IsTrue(payload.GetProperty("settleSeconds").GetDouble() > 0);
        Assert.IsTrue(payload.GetProperty("overshootPercent").GetDouble() < 0.5, "A critically damped spring measured as overshooting.");
        Assert.AreNotEqual(0, payload.GetProperty("samples").GetArrayLength());
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.GetProperty("reading").GetString()));
    }

    [TestMethod]
    public async Task CallTool_TheDoubleArguments_SurviveTheJsonRoundTrip()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.SimulateBmotionTransition),
            new Dictionary<string, object?> { ["transition"] = "tween(0.5, Linear)", ["from"] = 12.5, ["to"] = 87.5 });

        var payload = Payload(result);

        Assert.AreEqual(12.5, payload.GetProperty("from").GetDouble());
        Assert.AreEqual(87.5, payload.GetProperty("to").GetDouble());
    }

    [TestMethod]
    public async Task CallTool_TheCodeReview_ReturnsItsFindingsAsData()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.ReviewBmotionCode),
            new Dictionary<string, object?>
            {
                ["code"] = "@if (show)\n{\n    <Bmotion Exit=\"Bm.To(opacity: 0)\">\n        <div />\n    </Bmotion>\n}",
            });

        var payload = Payload(result);

        Assert.IsFalse(payload.GetProperty("passed").GetBoolean());

        var findings = payload.GetProperty("findings");

        Assert.AreNotEqual(0, findings.GetArrayLength());
        Assert.AreEqual("exit-without-presence", findings[0].GetProperty("rule").GetString());
        Assert.AreEqual(3, findings[0].GetProperty("line").GetInt32());
    }

    /// <summary>
    /// An unreadable transition is a correctable mistake, not a failed call. Thrown, the protocol
    /// reduces it to "an error occurred invoking SimulateBmotionTransition" and the caller learns
    /// nothing; returned as data, the explanation survives.
    /// </summary>
    [TestMethod]
    public async Task CallTool_AnUnreadableArgument_ComesBackAsAnExplanation_NotAsAProtocolError()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.SimulateBmotionTransition),
            new Dictionary<string, object?> { ["transition"] = "swoosh(0.4)" });

        Assert.IsFalse(result.IsError is true, "A correctable mistake was reported as a failed call.");

        var payload = Payload(result);

        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.GetProperty("error").GetString()));
        StringAssert.Contains(payload.GetProperty("error").GetString()!, "spring");
    }

    [TestMethod]
    public async Task CallTool_AMissingId_NamesWhatDoesExist()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.GetBmotionRecipe),
            new Dictionary<string, object?> { ["id"] = "teleport" });

        Assert.IsFalse(result.IsError is true);
        StringAssert.Contains(Render(result), "staggered-list");
    }

    [TestMethod]
    public async Task CallTool_AnOptionalArgumentOmitted_UsesTheDeclaredDefault()
    {
        var result = await _server.Client.CallToolAsync(
            nameof(McpController.SimulateBmotionTransition),
            new Dictionary<string, object?> { ["transition"] = "tween(0.3)" });

        var payload = Payload(result);

        Assert.AreEqual(0, payload.GetProperty("from").GetDouble());
        Assert.AreEqual(100, payload.GetProperty("to").GetDouble());
    }

    [TestMethod]
    public async Task CallTool_AToolThatDoesNotExist_Fails_WithoutTakingTheSessionDown()
    {
        var error = await Assert.ThrowsAsync<ModelContextProtocol.McpException>(
            () => _server.Client.CallToolAsync("GetBmotionNothing", new Dictionary<string, object?>()).AsTask());

        StringAssert.Contains(error.Message, "GetBmotionNothing");

        // The session is still usable afterwards, which is what makes a bad call recoverable.
        Assert.AreNotEqual(0, (await _server.Client.ListToolsAsync()).Count);
    }

    [TestMethod]
    public async Task ListPrompts_ExposesEveryWorkflow_WithItsArguments()
    {
        var prompts = await _server.Client.ListPromptsAsync();

        var declared = new McpController().GetMcpCatalog().Prompts.Select(prompt => prompt.Name).ToArray();

        CollectionAssert.AreEquivalent(declared, prompts.Select(prompt => prompt.Name).ToArray());

        foreach (var prompt in prompts)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(prompt.Description), $"'{prompt.Name}' has no description.");
        }
    }

    [TestMethod]
    public async Task GetPrompt_RendersTheWorkflowWithTheArgumentsItWasGiven()
    {
        var result = await _server.Client.GetPromptAsync(
            "animate-with-bmotion",
            new Dictionary<string, object?>
            {
                ["request"] = "the cards should appear one after another",
                ["renderMode"] = "server",
            });

        Assert.AreNotEqual(0, result.Messages.Count);

        var text = string.Join('\n', result.Messages.Select(message => (message.Content as TextContentBlock)?.Text));

        StringAssert.Contains(text, "the cards should appear one after another");
        StringAssert.Contains(text, "server");
        StringAssert.Contains(text, nameof(McpController.AnalyzeBmotionAnimation));
    }

    [TestMethod]
    public async Task ListResources_ExposesTheFixedOnes_AndTheTemplatedOnes()
    {
        var resources = await _server.Client.ListResourcesAsync();
        var templates = await _server.Client.ListResourceTemplatesAsync();

        var uris = resources.Select(resource => resource.Uri)
            .Concat(templates.Select(template => template.UriTemplate))
            .ToArray();

        var declared = new McpController().GetMcpCatalog().Resources.Select(resource => resource.Name).ToArray();

        CollectionAssert.AreEquivalent(declared, uris, $"Over the wire: {string.Join(", ", uris.Order())}.");
    }

    [TestMethod]
    public async Task ReadResource_TheFixedResources_ReturnTheirDocuments()
    {
        foreach (var resource in await _server.Client.ListResourcesAsync())
        {
            var result = await resource.ReadAsync();

            Assert.AreNotEqual(0, result.Contents.Count, $"'{resource.Uri}' returned nothing.");

            var text = (result.Contents[0] as TextResourceContents)?.Text;

            Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"'{resource.Uri}' returned empty text.");
        }
    }

    [TestMethod]
    public async Task ReadResource_ATemplatedResource_ResolvesItsArgument()
    {
        var guide = await _server.Client.ReadResourceAsync("bmotion://guide/Installation");

        StringAssert.Contains(((TextResourceContents)guide.Contents[0]).Text!, "## Installation");

        var type = await _server.Client.ReadResourceAsync("bmotion://api/BmSpring");

        StringAssert.Contains(((TextResourceContents)type.Contents[0]).Text!, "Stiffness");

        var setup = await _server.Client.ReadResourceAsync("bmotion://setup/server");

        StringAssert.Contains(((TextResourceContents)setup.Contents[0]).Text!, "Blazor Server");
    }

    /// <summary>
    /// A path is a URI segment here, so the slashes in it have to be escaped and unescaped again.
    /// Getting that wrong makes every demo page unreadable through the resource while every one of
    /// them stays readable through the tool.
    /// </summary>
    [TestMethod]
    public async Task ReadResource_ASourceFilePath_SurvivesUriEscaping()
    {
        var result = await _server.Client.ReadResourceAsync($"bmotion://source/{Uri.EscapeDataString("Demo/Server/Program.cs")}");

        var text = ((TextResourceContents)result.Contents[0]).Text!;

        StringAssert.Contains(text, "MapMcp");
        Assert.AreEqual(BmotionSourceCatalog.GetSourceFile("Demo/Server/Program.cs"), text);
    }

    /// <summary>
    /// Several agents share one server. The catalogs are built lazily and once, and the simulations
    /// each build their own engine; nothing here may serialise on the others or answer differently
    /// because it was asked at the same time as something else.
    /// </summary>
    [TestMethod]
    public async Task Server_AnswersConcurrentCalls_Independently()
    {
        var calls = new (string Tool, Dictionary<string, object?> Arguments)[]
        {
            (nameof(McpController.SimulateBmotionTransition), new() { ["transition"] = "spring(stiffness: 260, damping: 12)" }),
            (nameof(McpController.SearchBmotion), new() { ["query"] = "drag constraints" }),
            (nameof(McpController.GetBmotionEasings), new()),
            (nameof(McpController.GetBmotionAnimatableProperties), new()),
            (nameof(McpController.AnalyzeBmotionAnimation), new() { ["properties"] = "width" }),
            (nameof(McpController.GetBmotionApiList), new()),
            (nameof(McpController.SimulateBmotionTransition), new() { ["transition"] = "tween(0.4, BackOut)" }),
            (nameof(McpController.GetBmotionRecipes), new()),
        };

        var results = await Task.WhenAll(calls.Select(call =>
            _server.Client.CallToolAsync(call.Tool, call.Arguments).AsTask()));

        for (int i = 0; i < results.Length; i++)
        {
            Assert.IsFalse(results[i].IsError is true, $"'{calls[i].Tool}' failed under concurrency: {Render(results[i])}");
        }

        // And the same question asked in that crowd still gives the answer it gives alone.
        var alone = await _server.Client.CallToolAsync(
            nameof(McpController.SimulateBmotionTransition),
            new Dictionary<string, object?> { ["transition"] = "spring(stiffness: 260, damping: 12)" });

        Assert.AreEqual(Payload(results[0]).GetProperty("settleSeconds").GetDouble(),
                        Payload(alone).GetProperty("settleSeconds").GetDouble());
    }

    /// <summary>
    /// The tools are also plain HTTP GETs, which is what the demo's own /mcp page calls to show them
    /// working. That route is separate wiring from the MCP transport and fails separately.
    /// </summary>
    [TestMethod]
    public async Task Server_TheSameToolsAreAlsoReachableOverPlainHttp()
    {
        using var factory = new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>();
        using var http = factory.CreateClient();

        var catalog = await http.GetFromJsonAsync<BmotionMcpCatalogDto>("/api/Mcp/GetMcpCatalog");

        Assert.IsNotNull(catalog);
        Assert.AreNotEqual(0, catalog.Tools.Length);

        var simulation = await http.GetFromJsonAsync<BmotionSimulationDto>(
            "/api/Mcp/SimulateBmotionTransition?transition=spring(stiffness:%20260,%20damping:%2012)");

        Assert.IsNotNull(simulation);
        Assert.AreEqual("Spring", simulation.Kind);
        Assert.IsTrue(simulation.SettleSeconds > 0);
    }

    /// <summary>The answer as a client renders it: every text block the call returned.</summary>
    private static string Render(CallToolResult result)
    {
        return string.Join('\n', result.Content.OfType<TextContentBlock>().Select(block => block.Text));
    }

    /// <summary>
    /// The object a tool returned. Structured content when the server sent it, and otherwise the
    /// JSON in the text block - which is how a tool returning a bare object arrives.
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
}
