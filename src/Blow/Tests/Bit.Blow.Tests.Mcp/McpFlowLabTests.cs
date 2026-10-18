using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Bit.Blow.Demo.Server.Dtos;

namespace Bit.Blow.Tests.Mcp;

/// <summary>
/// InspectBlowFlow on the flows an application actually meets: one it saved itself, one built by
/// hand, one with every mistake BlowErrorCode names, one with the mistakes the canvas does not report,
/// and input that is not a flow at all. The flows are built with the library's own types and edited as
/// JSON, so each test changes exactly the one thing it is about.
/// </summary>
[TestClass]
public class McpFlowLabTests
{
    private static readonly JsonSerializerOptions _web = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [TestMethod]
    public async Task Without_a_flow_it_hands_out_a_sample_the_library_reads()
    {
        var report = await InspectAsync(null);

        Assert.IsFalse(report.Valid);
        Assert.IsNotNull(report.Sample);

        var sample = JsonSerializer.Deserialize<BlowSnapshot>(report.Sample, _web)!;

        Assert.HasCount(6, sample.Nodes);
        Assert.HasCount(4, sample.Edges);
        Assert.AreEqual(BlowViewport.Identity, sample.Viewport);
    }

    [TestMethod]
    public async Task The_sample_is_reported_node_by_node()
    {
        var report = await InspectAsync((await InspectAsync(null)).Sample);

        Assert.IsTrue(report.Valid);
        Assert.AreEqual(6, report.Nodes);
        Assert.AreEqual(4, report.Edges);
        Assert.AreEqual(3, Count(report.NodeTypes, "default"));
        Assert.AreEqual(1, Count(report.NodeTypes, "group"));
        Assert.AreEqual(1, Count(report.EdgeTypes, nameof(BlowEdgeType.SmoothStep)));
        Assert.IsEmpty(report.Problems!);
        Assert.AreEqual(1, report.MaxDepth);

        var group = report.SubFlows!.Single();

        Assert.AreEqual("checks", group.ParentId);
        Assert.AreEqual("group", group.ParentType);
        CollectionAssert.AreEqual(new[] { "lint", "test" }, group.Children);

        // The group is 200 x 150 at (220, 80) and its children sit inside it, so the flow's right edge is
        // the group's - which only holds if the children were resolved against their parent.
        Assert.AreEqual(new BlowRectDto { X = 0, Y = 0, Width = 420, Height = 340 }, report.Bounds);
    }

    [TestMethod]
    public async Task A_flow_written_by_JsonSerializer_with_its_defaults_is_read_back()
    {
        // PascalCase names, enums as numbers, and the members the canvas maintains - what an application
        // gets from JsonSerializer.Serialize(canvas.ToObject()) without configuring anything.
        var snapshot = new BlowSnapshot(
            [new BlowNode { Id = "a", Type = "input" }, new BlowNode { Id = "b", Position = new(0, 100), SourcePosition = BlowPosition.Right }],
            [new BlowEdge { Id = "a-b", Source = "a", Target = "b", Type = BlowEdgeType.Step }],
            new BlowViewport(10, 20, 1.5));

        var json = JsonSerializer.Serialize(snapshot);

        var report = await InspectAsync(json);

        Assert.IsTrue(report.Valid, report.Error);
        Assert.AreEqual(1, Count(report.EdgeTypes, nameof(BlowEdgeType.Step)));
        Assert.AreEqual(new BlowViewportDto { X = 10, Y = 20, Zoom = 1.5 }, report.Viewport);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("never reads back", StringComparison.Ordinal)), "The members JsonSerializer writes but never reads should be pointed out.");
    }

    [TestMethod]
    public async Task Every_problem_BlowErrorCode_names_is_raised_by_the_canvas()
    {
        var flow = new JsonObject
        {
            ["nodes"] = new JsonArray(
                Node("a"),
                Node("a"),
                Node("orphan", parent: "nowhere"),
                Node("ports", handles: new JsonArray(Handle("out", "source", "right"), Handle("out", "source", "bottom")))),
            ["edges"] = new JsonArray(Edge("e1", "a", "missing"), Edge("e1", "a", "ports"))
        };

        var report = await InspectAsync(flow.ToJsonString());

        Assert.IsTrue(report.Valid, report.Error);

        CollectionAssert.AreEquivalent(
            new[] { BlowErrorCode.DuplicateNodeId, BlowErrorCode.DuplicateEdgeId, BlowErrorCode.MissingEdgeEndpoint, BlowErrorCode.MissingParentNode, BlowErrorCode.DuplicateHandleId },
            report.Problems!.Select(problem => problem.Code).ToArray());

        Assert.Contains("'missing'", report.Problems!.Single(problem => problem.Code == BlowErrorCode.MissingEdgeEndpoint).Message);
        Assert.Contains("'nowhere'", report.Problems!.Single(problem => problem.Code == BlowErrorCode.MissingParentNode).Message);

        // One note per kind of problem, saying what the canvas does about it.
        Assert.IsTrue(report.Notes.Any(note => note.Contains("keeps the first node", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("laid out as a root", StringComparison.Ordinal) || note.Contains("lays each of them out as a root", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_handle_written_the_way_JsonSerializer_writes_BlowNode_Handles_is_measured()
    {
        // The dictionary shape: keyed by BlowHandleBounds.Key, holding a BlowHandleBounds.
        var handles = new JsonObject
        {
            [BlowHandleBounds.Key(BlowHandleType.Source, "yes")] = JsonSerializer.SerializeToNode(new BlowHandleBounds("yes", BlowHandleType.Source, BlowPosition.Right, 150, 20)),
            [BlowHandleBounds.Key(BlowHandleType.Target, null)] = JsonSerializer.SerializeToNode(new BlowHandleBounds(string.Empty, BlowHandleType.Target, BlowPosition.Left, 0, 20)),
        };

        var flow = new JsonObject
        {
            ["nodes"] = new JsonArray(Node("decision", handles: handles), Node("next")),
            ["edges"] = new JsonArray(Edge("ok", "decision", "next", sourceHandle: "yes"), Edge("bad", "decision", "next", sourceHandle: "no"))
        };

        var report = await InspectAsync(flow.ToJsonString());

        Assert.IsEmpty(report.Problems!);

        var note = report.Notes.Single(note => note.Contains("name a handle the node does not have", StringComparison.Ordinal));

        Assert.Contains("'bad'", note);
        Assert.DoesNotContain("'ok'", note);
    }

    [TestMethod]
    public async Task What_the_canvas_does_not_report_is_pointed_out()
    {
        var flow = new JsonObject
        {
            ["nodes"] = new JsonArray(
                Node("p", parent: "q"),
                Node("q", parent: "p"),
                Node("self", parent: "self"),
                Node("start", type: "input"),
                Node("task", type: "task")),
            ["edges"] = new JsonArray(Edge("into-input", "task", "start"), Edge("twice-1", "start", "task"), Edge("twice-2", "start", "task"))
        };

        var report = await InspectAsync(flow.ToJsonString());

        Assert.IsEmpty(report.Problems!, "None of these is a BlowErrorCode, which is why the tool has to say them.");
        Assert.IsTrue(report.Notes.Any(note => note.Contains("parent cycle", StringComparison.Ordinal) && note.Contains("'p'", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("own parent", StringComparison.Ordinal) && note.Contains("'self'", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'into-input'", StringComparison.Ordinal) && note.Contains("no target handle", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'task'", StringComparison.Ordinal) && note.Contains("NodeTemplate", StringComparison.Ordinal)));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("start -> task", StringComparison.Ordinal) && note.Contains("PreventDuplicateEdges", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_measured_size_in_the_flow_is_the_size_the_canvas_uses()
    {
        var measured = Node("wide");
        measured["measured"] = new JsonObject { ["width"] = 300, ["height"] = 120 };

        var report = await InspectAsync(new JsonObject { ["nodes"] = new JsonArray(measured, Node("plain", x: 0, y: 200)) }.ToJsonString());

        // 300 wide from its measurement; the other node falls back to BlowNode.DefaultHeight below it.
        Assert.AreEqual(new BlowRectDto { X = 0, Y = 0, Width = 300, Height = 200 + BlowNode.DefaultHeight }, report.Bounds);
    }

    [TestMethod]
    public async Task Hidden_nodes_are_counted_left_out_of_the_bounds_and_left_where_they_are()
    {
        var hidden = Node("hidden", x: 5000, y: 5000);
        hidden["hidden"] = true;

        var report = await InspectAsync(new JsonObject
        {
            ["nodes"] = new JsonArray(Node("a"), Node("b", x: 0, y: 100), hidden),
            ["edges"] = new JsonArray(Edge("a-b", "a", "b"), Edge("b-hidden", "b", "hidden"))
        }.ToJsonString(), layout: "grid");

        Assert.AreEqual(1, report.HiddenNodes);
        Assert.AreEqual(1, report.HiddenEdges, "An edge to a hidden node is not drawn.");
        Assert.IsLessThan(1000, report.Bounds!.Width);
        Assert.IsFalse(report.Layout!.Positions.Any(position => position.Id == "hidden"));
    }

    [TestMethod]
    public async Task A_layout_places_every_visible_node_set_by_set()
    {
        var report = await InspectAsync((await InspectAsync(null)).Sample, layout: "layered-left-to-right");

        var layout = report.Layout!;

        Assert.AreEqual("layered-left-to-right", layout.Algorithm);
        Assert.AreEqual(2, layout.Groups, "The roots, and the group's children.");
        CollectionAssert.AreEquivalent(new[] { "start", "review", "checks", "lint", "test", "ship" }, layout.Positions.Select(position => position.Id).ToArray());
        Assert.IsTrue(layout.Positions.All(position => position.SourcePosition == nameof(BlowPosition.Right) && position.TargetPosition == nameof(BlowPosition.Left)));
        Assert.AreEqual("checks", layout.Positions.Single(position => position.Id == "lint").ParentId);
        Assert.Contains("BlowLayoutDirection.LeftToRight", layout.Code);
        Assert.Contains("UpdateNodeInternals", layout.Code);
        Assert.IsNotNull(layout.Bounds);
    }

    [TestMethod]
    public async Task A_layout_name_past_the_schema_is_answered_with_the_list()
    {
        var report = await InspectAsync((await InspectAsync(null)).Sample, layout: "force-directed");

        Assert.IsNull(report.Layout);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("'force-directed' is not a layout", StringComparison.Ordinal) && note.Contains("layered", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_viewport_outside_the_default_zoom_range_is_pointed_out()
    {
        var report = await InspectAsync(new JsonObject { ["nodes"] = new JsonArray(Node("a")), ["viewport"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["zoom"] = 9 } }.ToJsonString());

        Assert.IsTrue(report.Notes.Any(note => note.Contains("MinZoom..MaxZoom", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Input_that_is_not_json_is_rejected_with_the_parsers_own_message()
    {
        var report = await InspectAsync("{ nodes: [");

        Assert.IsFalse(report.Valid);
        Assert.IsNotNull(report.Error);
    }

    [TestMethod]
    public async Task Json_that_is_not_a_flow_says_what_a_flow_is()
    {
        var report = await InspectAsync("[1, 2, 3]");

        Assert.IsFalse(report.Valid);
        Assert.Contains("\"nodes\" array", report.Error!);
    }

    [TestMethod]
    public async Task A_node_without_an_id_is_refused_the_way_JsonSerializer_refuses_it()
    {
        var missing = await InspectAsync("""{ "nodes": [ { "label": "no id" } ] }""");
        var nulled = await InspectAsync("""{ "nodes": [ { "id": null } ] }""");

        Assert.IsFalse(missing.Valid);
        Assert.Contains("required", missing.Error!);
        Assert.IsFalse(nulled.Valid);
        Assert.Contains("null", nulled.Error!);
    }

    [TestMethod]
    public async Task A_flow_past_the_tool_limit_is_refused_with_the_reason()
    {
        var huge = new string(' ', 2 * 1024 * 1024 + 1) + """{ "nodes": [] }""";

        var report = await InspectAsync(huge);

        Assert.IsFalse(report.Valid);
        Assert.Contains("2097152 bytes", report.Error!);
    }

    internal static Task<BlowFlowReportDto> InspectAsync(string? json, string? layout = null)
    {
        var arguments = new Dictionary<string, object?>();
        if (json is not null) arguments["json"] = json;
        if (layout is not null) arguments["layout"] = layout;

        return McpCall.StructuredAsync<BlowFlowReportDto>("InspectBlowFlow", arguments);
    }

    private static JsonObject Node(string id, string? type = null, string? parent = null, double x = 0, double y = 0, JsonNode? handles = null)
    {
        var node = new JsonObject { ["id"] = id, ["position"] = new JsonObject { ["x"] = x, ["y"] = y } };

        if (type is not null) node["type"] = type;
        if (parent is not null) node["parentId"] = parent;
        if (handles is not null) node["handles"] = handles;

        return node;
    }

    private static JsonObject Handle(string id, string type, string position) => new() { ["id"] = id, ["type"] = type, ["position"] = position };

    private static JsonObject Edge(string id, string source, string target, string? sourceHandle = null)
    {
        var edge = new JsonObject { ["id"] = id, ["source"] = source, ["target"] = target };

        if (sourceHandle is not null) edge["sourceHandle"] = sourceHandle;

        return edge;
    }

    private static int Count(BlowCountDto[]? counts, string name) => counts?.SingleOrDefault(count => count.Name == name)?.Count ?? 0;
}
