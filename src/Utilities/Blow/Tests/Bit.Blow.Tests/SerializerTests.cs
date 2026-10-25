using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Bunit;

namespace Bit.Blow.Tests;

/// <summary>
/// Saving and loading: the schema written, a lossless round trip, determinism, custom data with and without a
/// contract, limits against hostile input, located import errors, application migrations, and the canvas helpers.
/// </summary>
[TestClass]
public class SerializerTests : BunitContext
{
    public SerializerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static BlowDocument Sample() => new()
    {
        Id = "doc-1",
        Metadata = new JsonObject { ["title"] = "Orders" },
        Viewport = new BlowViewport(10, 20, 1.5),
        Nodes =
        [
            new BlowNode { Id = "start", Type = "input", Label = "Start", Position = new(0, 0) },
            new BlowNode
            {
                Id = "check", Label = "Check", Position = new(10.5, -20.25), Width = 180, Height = 60,
                SourcePosition = BlowPosition.Right, TargetPosition = BlowPosition.Left, Hidden = true, Draggable = false,
                Selectable = false, Connectable = false, Deletable = false, Focusable = false, UseCustomDragHandle = true,
                AriaLabel = "Check stock", Class = "task", Style = "color:red", ZIndex = 3, Origin = new(0.5, 0.5),
                Ports =
                [
                    new BlowPort { Id = "in", Type = BlowHandleType.Target, DataType = "order", MinConnections = 1, MaxConnections = 1, Label = "Order", Position = BlowPosition.Left },
                    new BlowPort { Id = "ok", Type = BlowHandleType.Source, Data = "yes" },
                ],
            },
            new BlowNode { Id = "group", Type = "group", Position = new(300, 0), Width = 300, Height = 200 },
            new BlowNode { Id = "child", ParentId = "group", ExtentParent = true, ExpandParent = true, Extent = new BlowRect(0, 0, 100, 50), Position = new(10, 10) },
        ],
        Edges =
        [
            new BlowEdge { Id = "e1", Source = "start", Target = "check", TargetHandle = "in" },
            new BlowEdge
            {
                Id = "e2", Source = "check", SourceHandle = "ok", Target = "child", Type = BlowEdgeType.SmoothStep,
                PathOptions = new BlowEdgePathOptions { BorderRadius = 12, Offset = 30, StepPosition = 0.3, Curvature = 0.5, LoopRadius = 70 },
                Label = "ok", LabelStyle = "fill:red", LabelShowBg = false, LabelBgStyle = "fill:blue", LabelBgPaddingX = 6,
                LabelBgPaddingY = 3, LabelBgBorderRadius = 5, Animated = true, Hidden = true, Selectable = false, Focusable = false,
                Deletable = false, Reconnectable = false, StrokeWidth = 3, Stroke = "#f00", MarkerEnd = false, MarkerStart = true,
                MarkerType = BlowMarkerType.ArrowClosed, MarkerColor = "blue", MarkerWidth = 9, MarkerHeight = 10, Class = "flow",
                Style = "opacity:.5", InteractionWidth = 20, ZIndex = 2, Data = 42,
            },
        ],
    };

    // ---- writing ----

    [TestMethod]
    public void ADocumentSurvivesARoundTripLosslessly()
    {
        var original = Sample();

        var json = BlowSerializer.Serialize(original);
        var result = BlowSerializer.Deserialize(json);

        Assert.IsTrue(result.Succeeded, string.Join("\n", result.Issues));
        var copy = result.Document!;
        Assert.AreEqual(json, BlowSerializer.Serialize(copy), "a second save of the loaded document differs from the first");
        Assert.AreEqual("doc-1", copy.Id);
        Assert.AreEqual("Orders", copy.Metadata!["title"]!.GetValue<string>());
        Assert.AreEqual(new BlowViewport(10, 20, 1.5), copy.Viewport);

        var check = copy.Nodes[1];
        Assert.AreEqual(new BlowXYPosition(10.5, -20.25), check.Position);
        Assert.AreEqual(BlowPosition.Right, check.SourcePosition);
        Assert.IsTrue(check.Hidden);
        Assert.IsFalse(check.Draggable);
        Assert.AreEqual(1, check.FindPort(BlowHandleType.Target, "in")!.MinConnections);
        Assert.AreEqual("order", check.FindPort(BlowHandleType.Target, "in")!.DataType);
        Assert.AreEqual(new BlowRect(0, 0, 100, 50), copy.Nodes[3].Extent);
        Assert.AreEqual(BlowEdgeType.SmoothStep, copy.Edges[1].Type);
        Assert.AreEqual(0.3, copy.Edges[1].PathOptions!.StepPosition);
        Assert.AreEqual(BlowMarkerType.ArrowClosed, copy.Edges[1].MarkerType);
        Assert.AreEqual(42, ((JsonElement)copy.Edges[1].Data!).GetInt32());
    }

    [TestMethod]
    public void TheOutputIsDeterministicAndLeavesDefaultsOut()
    {
        var document = new BlowDocument
        {
            Nodes = [new BlowNode { Id = "a", Position = new(1, 2) }],
            Edges = [new BlowEdge { Id = "e", Source = "a", Target = "a" }],
        };

        var json = BlowSerializer.Serialize(document);

        Assert.AreEqual(
            """{"format":"bit-blow","schemaVersion":1,"version":0,"nodes":[{"id":"a","position":{"x":1,"y":2}}],"edges":[{"id":"e","source":"a","target":"a"}]}""",
            json);
        Assert.AreEqual(json, BlowSerializer.Serialize(document));
    }

    [TestMethod]
    public void SelectionAndMeasuredStateAreNotSavedUnlessAskedFor()
    {
        var node = new BlowNode { Id = "a", Selected = true };
        var document = new BlowDocument { Nodes = [node] };

        Assert.DoesNotContain("selected", BlowSerializer.Serialize(document));
        StringAssert.Contains(BlowSerializer.Serialize(document, new BlowSerializerOptions { IncludeSelection = true }), "\"selected\":true");
    }

    [TestMethod]
    public void NumbersAreWrittenTheSameWayInEveryCulture()
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            StringAssert.Contains(BlowSerializer.Serialize(new BlowDocument { Nodes = [new BlowNode { Id = "a", Position = new(1.5, 2) }] }), "\"x\":1.5");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = culture;
        }
    }

    // ---- data ----

    [TestMethod]
    public void DataWithAContractIsReadBackAsItsType()
    {
        var options = new BlowSerializerOptions().MapNodeData("task", TestJsonContext.Default.TaskData);
        var document = new BlowDocument { Nodes = [new BlowNode { Id = "t", Type = "task", Data = new TaskData { Owner = "ana", Hours = 3 } }] };

        var json = BlowSerializer.Serialize(document, options);
        var loaded = BlowSerializer.Deserialize(json, options).Document!;

        var data = Assert.IsInstanceOfType<TaskData>(loaded.Nodes[0].Data);
        Assert.AreEqual("ana", data.Owner);
        Assert.AreEqual(3, data.Hours);
    }

    [TestMethod]
    public void DataWithoutAContractIsKeptAsJsonAndWrittenBackUnchanged()
    {
        const string json = """{"format":"bit-blow","schemaVersion":1,"version":0,"nodes":[{"id":"t","type":"task","position":{"x":0,"y":0},"data":{"owner":"ana","nested":[1,{"deep":true}]}}],"edges":[]}""";

        var loaded = BlowSerializer.Deserialize(json).Document!;

        Assert.IsInstanceOfType<JsonElement>(loaded.Nodes[0].Data);
        Assert.AreEqual(json, BlowSerializer.Serialize(loaded));
    }

    [TestMethod]
    public void DataOfATypeNoContractCoversCannotBeSavedAndSaysWhichElement()
    {
        var document = new BlowDocument { Nodes = [new BlowNode { Id = "t", Data = new TaskData() }] };

        var ex = Assert.ThrowsExactly<BlowSerializationException>(() => BlowSerializer.Serialize(document));

        StringAssert.Contains(ex.Message, "Node 't'");
        StringAssert.Contains(ex.Message, nameof(TaskData));
    }

    [TestMethod]
    public void DataThatDoesNotFitItsContractIsKeptAsJsonWithAWarning()
    {
        var options = new BlowSerializerOptions().MapNodeData("task", TestJsonContext.Default.TaskData);
        const string json = """{"format":"bit-blow","schemaVersion":1,"nodes":[{"id":"t","type":"task","data":{"hours":"many"}}],"edges":[]}""";

        var result = BlowSerializer.Deserialize(json, options);

        Assert.IsTrue(result.Succeeded);
        Assert.IsInstanceOfType<JsonElement>(result.Document!.Nodes[0].Data);
        var issue = result.Issues.Single();
        Assert.AreEqual(BlowImportCodes.DataNotRead, issue.Code);
        Assert.AreEqual("$.nodes[0].data", issue.Path);
    }

    [TestMethod]
    public void NothingInADocumentNamesAType()
    {
        // A "$type" in data is just data: it is never used to decide what to create.
        const string json = """{"format":"bit-blow","schemaVersion":1,"nodes":[{"id":"t","data":{"$type":"System.Diagnostics.Process"}}],"edges":[]}""";

        var data = BlowSerializer.Deserialize(json).Document!.Nodes[0].Data;

        Assert.IsInstanceOfType<JsonElement>(data);
    }

    // ---- refusing bad input ----

    [TestMethod]
    public void InvalidJsonIsReportedWithWhereItWentWrong()
    {
        var result = BlowSerializer.Deserialize("{\"format\":\"bit-blow\",\n\"nodes\": [ {\"id\": } ]}");

        Assert.IsNull(result.Document);
        var issue = result.Issues.Single();
        Assert.AreEqual(BlowImportCodes.InvalidJson, issue.Code);
        StringAssert.Contains(issue.Message, "line 2");
    }

    [TestMethod]
    public void AnythingThatIsNotABlowDocumentIsRefused()
    {
        Assert.AreEqual(BlowImportCodes.NotADocument, BlowSerializer.Deserialize("""{"nodes":[],"edges":[]}""").Issues.Single().Code);
        Assert.AreEqual(BlowImportCodes.NotADocument, BlowSerializer.Deserialize("[1,2,3]").Issues.Single().Code);
    }

    [TestMethod]
    public void ANewerSchemaIsRefused()
    {
        var result = BlowSerializer.Deserialize("""{"format":"bit-blow","schemaVersion":99,"nodes":[],"edges":[]}""");

        Assert.AreEqual(BlowImportCodes.UnsupportedSchema, result.Issues.Single().Code);
    }

    [TestMethod]
    public void AnOversizedDocumentIsRefusedBeforeItIsParsed()
    {
        var options = new BlowSerializerOptions { Limits = { MaxBytes = 100 } };

        var result = BlowSerializer.Deserialize(new string(' ', 200) + "{}", options);

        Assert.AreEqual(BlowImportCodes.TooLarge, result.Issues.Single().Code);
    }

    [TestMethod]
    public async Task AStreamIsReadNoFurtherThanTheLimit()
    {
        var options = new BlowSerializerOptions { Limits = { MaxBytes = 1000 } };
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(new string(' ', 1_000_000)));

        var result = await BlowSerializer.DeserializeAsync(stream, options);

        Assert.AreEqual(BlowImportCodes.TooLarge, result.Issues.Single().Code);
        Assert.IsLessThan(200_000, stream.Position, "the reader went on reading long after the limit");
    }

    [TestMethod]
    public void DeeplyNestedDataIsRefused()
    {
        var deep = string.Concat(Enumerable.Repeat("[", 500)) + string.Concat(Enumerable.Repeat("]", 500));
        var json = $$"""{"format":"bit-blow","schemaVersion":1,"nodes":[{"id":"a","data":{{deep}}}],"edges":[]}""";

        var result = BlowSerializer.Deserialize(json);

        Assert.AreEqual(BlowImportCodes.TooDeep, result.Issues.Single().Code);
    }

    [TestMethod]
    public void TooManyNodesOrEdgesAreRefusedWholesale()
    {
        var options = new BlowSerializerOptions { Limits = { MaxNodes = 2, MaxEdges = 1 } };
        var nodes = BlowSerializer.Deserialize("""{"format":"bit-blow","schemaVersion":1,"nodes":[{"id":"a"},{"id":"b"},{"id":"c"}],"edges":[]}""", options);
        var edges = BlowSerializer.Deserialize("""{"format":"bit-blow","schemaVersion":1,"nodes":[{"id":"a"}],"edges":[{"id":"1","source":"a","target":"a"},{"id":"2","source":"a","target":"a"}]}""", options);

        Assert.IsNull(nodes.Document);
        Assert.AreEqual("$.nodes", nodes.Issues.Single().Path);
        Assert.IsNull(edges.Document);
        Assert.AreEqual(BlowImportCodes.TooMany, edges.Issues.Single().Code);
    }

    [TestMethod]
    public void AnOverlongStringIsLeftOutAndReported()
    {
        var options = new BlowSerializerOptions { Limits = { MaxStringLength = 10 } };
        var json = $$"""{"format":"bit-blow","schemaVersion":1,"nodes":[{"id":"a","label":"{{new string('x', 50)}}"}],"edges":[]}""";

        var result = BlowSerializer.Deserialize(json, options);

        Assert.IsNull(result.Document!.Nodes[0].Label);
        Assert.AreEqual(BlowImportCodes.StringTooLong, result.Issues.Single().Code);
        Assert.AreEqual("$.nodes[0].label", result.Issues.Single().Path);
    }

    [TestMethod]
    public void BrokenElementsAreLeftOutAndEachProblemIsLocated()
    {
        const string json = """
            {"format":"bit-blow","schemaVersion":1,
             "nodes":[{"id":"a","position":{"x":"far","y":0}},{"label":"no id"},{"id":"b","width":-5,"sourcePosition":"diagonal","shiny":true}],
             "edges":[{"id":"e","source":"a"},{"id":"f","source":"a","target":"b","type":"wavy"}]}
            """;

        var result = BlowSerializer.Deserialize(json);

        Assert.IsFalse(result.Succeeded);
        CollectionAssert.AreEqual(new[] { "a", "b" }, result.Document!.Nodes.Select(n => n.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "f" }, result.Document.Edges.Select(e => e.Id).ToArray());

        var located = result.Issues.Select(i => $"{i.Code}@{i.Path}").ToArray();
        CollectionAssert.Contains(located, "invalid-value@$.nodes[0].position.x");
        CollectionAssert.Contains(located, "missing-property@$.nodes[1]");
        CollectionAssert.Contains(located, "invalid-value@$.nodes[2].width");
        CollectionAssert.Contains(located, "invalid-value@$.nodes[2].sourcePosition");
        CollectionAssert.Contains(located, "unknown-property@$.nodes[2].shiny");
        CollectionAssert.Contains(located, "missing-property@$.edges[0]");
        CollectionAssert.Contains(located, "invalid-value@$.edges[1].type");
        Assert.AreEqual(BlowValidationSeverity.Info, result.Issues.Single(i => i.Code == BlowImportCodes.UnknownProperty).Severity);
    }

    [TestMethod]
    public void GraphProblemsAreWarningsAtTheElementsTheyConcern()
    {
        const string json = """
            {"format":"bit-blow","schemaVersion":1,
             "nodes":[{"id":"a"},{"id":"a"},{"id":"c","parentId":"c"}],
             "edges":[{"id":"e","source":"a","target":"ghost"}]}
            """;

        var result = BlowSerializer.Deserialize(json);

        Assert.IsTrue(result.Succeeded, "a graph the canvas can draw around is still loaded");
        var located = result.Issues.Select(i => $"{i.Code}@{i.Path}").ToArray();
        CollectionAssert.Contains(located, "duplicate-node-id@$.nodes[0]");
        CollectionAssert.Contains(located, "missing-edge-endpoint@$.edges[0]");
        CollectionAssert.Contains(located, "parent-cycle@$.nodes[2]");
        Assert.IsTrue(result.Issues.All(i => i.Severity == BlowValidationSeverity.Warning));
    }

    // ---- versions and migrations ----

    [TestMethod]
    public void AnOlderDocumentIsMigratedStepByStep()
    {
        var options = new BlowSerializerOptions { CurrentVersion = 2 };
        options.Migrations.Add(new BlowMigration(0, doc =>
        {
            // Version 1 renamed node type "task" to "job".
            foreach (var node in doc["nodes"]!.AsArray())
            {
                if ((string?)node!["type"] == "task") node["type"] = "job";
            }
            return doc;
        }));
        options.Migrations.Add(new BlowMigration(1, doc =>
        {
            // Version 2 moved the title into the metadata.
            doc["metadata"] = new JsonObject { ["title"] = doc["title"]?.DeepClone() };
            doc.Remove("title");
            return doc;
        }));
        const string old = """{"format":"bit-blow","schemaVersion":1,"version":0,"title":"Old","nodes":[{"id":"a","type":"task"}],"edges":[]}""";

        var result = BlowSerializer.Deserialize(old, options);

        Assert.IsTrue(result.Succeeded, string.Join("\n", result.Issues));
        Assert.AreEqual(0, result.SourceVersion);
        Assert.AreEqual(2, result.Document!.Version);
        Assert.AreEqual("job", result.Document.Nodes[0].Type);
        Assert.AreEqual("Old", result.Document.Metadata!["title"]!.GetValue<string>());
        StringAssert.Contains(BlowSerializer.Serialize(result.Document, options), "\"version\":2");
    }

    [TestMethod]
    public void AGapInTheMigrationsIsReported()
    {
        var options = new BlowSerializerOptions { CurrentVersion = 3 };
        options.Migrations.Add(new BlowMigration(0, doc => doc));

        var result = BlowSerializer.Deserialize("""{"format":"bit-blow","schemaVersion":1,"version":0,"nodes":[],"edges":[]}""", options);

        Assert.IsNull(result.Document);
        Assert.AreEqual(BlowImportCodes.MissingMigration, result.Issues.Single().Code);
        StringAssert.Contains(result.Issues.Single().Message, "version 1");
    }

    [TestMethod]
    public void AFailingMigrationIsReported()
    {
        var options = new BlowSerializerOptions { CurrentVersion = 1 };
        options.Migrations.Add(new BlowMigration(0, _ => throw new InvalidOperationException("bad data")));

        var result = BlowSerializer.Deserialize("""{"format":"bit-blow","schemaVersion":1,"nodes":[],"edges":[]}""", options);

        Assert.AreEqual(BlowImportCodes.MigrationFailed, result.Issues.Single().Code);
        StringAssert.Contains(result.Issues.Single().Message, "bad data");
    }

    [TestMethod]
    public void ADocumentFromANewerVersionOfTheApplicationIsRefused()
    {
        var result = BlowSerializer.Deserialize("""{"format":"bit-blow","schemaVersion":1,"version":5,"nodes":[],"edges":[]}""");

        Assert.IsNull(result.Document);
        Assert.AreEqual(BlowImportCodes.NewerVersion, result.Issues.Single().Code);
        Assert.AreEqual(5, result.SourceVersion);
    }

    // ---- the canvas ----

    [TestMethod]
    public async Task TheCanvasSavesAndLoadsItsGraph()
    {
        var nodes = new List<BlowNode> { new() { Id = "a", Label = "A" }, new() { Id = "b", Position = new(0, 100) } };
        var edges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes).Add(c => c.Edges, edges).Add(c => c.HistoryEnabled, true));
        var saved = cut.Instance.ToJson();

        await cut.InvokeAsync(() => cut.Instance.DeleteElementsAsync(nodes: [nodes[1]]));
        var result = await cut.InvokeAsync(() => cut.Instance.LoadJsonAsync(saved));

        Assert.IsTrue(result.Succeeded);
        CollectionAssert.AreEqual(new[] { "a", "b" }, nodes.Select(n => n.Id).ToArray(), "the bound list was not reloaded in place");
        Assert.HasCount(1, edges);
        Assert.IsTrue(cut.Instance.CanUndo, "the load is an undo step");
    }

    [TestMethod]
    public async Task ADocumentWithErrorsDoesNotReplaceTheGraph()
    {
        var nodes = new List<BlowNode> { new() { Id = "keep" } };
        var cut = Render<BlowCanvas>(p => p.Add(c => c.Nodes, nodes));

        var result = await cut.InvokeAsync(() => cut.Instance.LoadJsonAsync("""{"format":"bit-blow","schemaVersion":1,"nodes":[{"label":"no id"}],"edges":[]}"""));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual("keep", nodes.Single().Id);
    }
}

/// <summary>A node payload, serialized through a source-generated contract as an application would.</summary>
public sealed class TaskData
{
    public string Owner { get; set; } = "";
    public int Hours { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TaskData))]
internal sealed partial class TestJsonContext : JsonSerializerContext;
