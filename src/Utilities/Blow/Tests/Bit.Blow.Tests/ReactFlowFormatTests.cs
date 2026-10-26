using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blow.Tests;

/// <summary>Moving flows between React Flow's <c>toObject()</c> JSON and Blow.</summary>
[TestClass]
public class ReactFlowFormatTests
{
    private const string ReactFlowObject = """
        {
          "nodes": [
            { "id": "1", "type": "input", "position": { "x": 250, "y": 5 }, "data": { "label": "Input" },
              "sourcePosition": "right", "className": "start", "style": { "backgroundColor": "#eee", "width": 180 },
              "measured": { "width": 180, "height": 40 }, "selected": true },
            { "id": "2", "position": { "x": 100, "y": 100 }, "data": { "label": "Default", "owner": "ana" },
              "parentId": "g", "extent": "parent", "dragHandle": ".grip" },
            { "id": "g", "type": "group", "position": { "x": 0, "y": 0 }, "data": {}, "width": 400, "height": 300,
              "extent": [[0, 0], [1000, 800]] }
          ],
          "edges": [
            { "id": "e1-2", "source": "1", "target": "2", "animated": true, "label": "go",
              "markerEnd": { "type": "arrowclosed", "color": "#f00", "width": 20 },
              "style": { "stroke": "#333", "strokeWidth": 2, "strokeDasharray": "5 5" } },
            { "id": "e2-3", "source": "2", "sourceHandle": "a", "target": "g", "type": "smoothstep", "reconnectable": "target" },
            { "id": "e3", "source": "g", "target": "1", "type": "buttonedge" }
          ],
          "viewport": { "x": 10, "y": 20, "zoom": 1.25 }
        }
        """;

    [TestMethod]
    public void AReactFlowObjectIsReadIntoBlowsModel()
    {
        var result = BlowReactFlowFormat.Import(ReactFlowObject);

        Assert.IsNotNull(result.Document);
        var doc = result.Document;
        Assert.AreEqual(new BlowViewport(10, 20, 1.25), doc.Viewport);

        var input = doc.Nodes[0];
        Assert.AreEqual("input", input.Type);
        Assert.AreEqual("Input", input.Label);
        Assert.AreEqual(BlowPosition.Right, input.SourcePosition);
        Assert.AreEqual("start", input.Class);
        Assert.AreEqual("background-color:#eee;width:180px;", input.Style);
        Assert.IsTrue(input.Selected);

        var child = doc.Nodes[1];
        Assert.AreEqual("default", child.Type);
        Assert.AreEqual("g", child.ParentId);
        Assert.IsTrue(child.ExtentParent);
        Assert.IsTrue(child.UseCustomDragHandle);
        Assert.AreEqual("ana", ((JsonElement)child.Data!).GetProperty("owner").GetString(), "the data object was not kept");

        Assert.AreEqual(new BlowRect(0, 0, 1000, 800), doc.Nodes[2].Extent);
        Assert.AreEqual(400, doc.Nodes[2].Width);

        var edge = doc.Edges[0];
        Assert.IsTrue(edge.Animated);
        Assert.AreEqual("go", edge.Label);
        Assert.IsTrue(edge.MarkerEnd);
        Assert.AreEqual(BlowMarkerType.ArrowClosed, edge.MarkerType);
        Assert.AreEqual("#f00", edge.MarkerColor);
        Assert.AreEqual(20, edge.MarkerWidth);
        Assert.AreEqual("#333", edge.Stroke);
        Assert.AreEqual(2, edge.StrokeWidth);
        Assert.AreEqual("stroke-dasharray:5 5;", edge.Style);

        Assert.AreEqual(BlowEdgeType.SmoothStep, doc.Edges[1].Type);
        Assert.IsFalse(doc.Edges[1].MarkerEnd, "React Flow draws no arrowhead unless asked for one");
        Assert.IsTrue(doc.Edges[1].Reconnectable);
    }

    [TestMethod]
    public void WhatHasNoCounterpartIsReportedNotGuessed()
    {
        var result = BlowReactFlowFormat.Import(ReactFlowObject);

        var located = result.Issues.Select(i => $"{i.Severity}:{i.Path}").ToArray();
        CollectionAssert.Contains(located, "Warning:$.nodes[1].dragHandle");
        CollectionAssert.Contains(located, "Warning:$.edges[2].type");
        Assert.IsTrue(result.Succeeded, "warnings alone do not fail an import");
    }

    [TestMethod]
    public void ABlowGraphExportsAsAReactFlowObjectAndComesBack()
    {
        var document = new BlowDocument
        {
            Viewport = new BlowViewport(5, 6, 0.5),
            Nodes =
            [
                new BlowNode { Id = "a", Type = "input", Label = "A", Position = new(1, 2), Style = "background-color:#fff;width:120px", Data = JsonDocument.Parse("""{"owner":"ana"}""").RootElement },
                new BlowNode { Id = "b", Label = "B", ParentId = "a", ExtentParent = true, Position = new(3, 4), TargetPosition = BlowPosition.Left },
            ],
            Edges =
            [
                new BlowEdge { Id = "e", Source = "a", Target = "b", Type = BlowEdgeType.Step, Label = "x", MarkerType = BlowMarkerType.ArrowClosed, Stroke = "red", StrokeWidth = 3 },
                new BlowEdge { Id = "f", Source = "b", Target = "a", MarkerEnd = false, Data = 7 },
            ],
        };

        var json = BlowReactFlowFormat.Export(document);
        var parsed = JsonNode.Parse(json)!;

        Assert.AreEqual("A", (string?)parsed["nodes"]![0]!["data"]!["label"]);
        Assert.AreEqual("ana", (string?)parsed["nodes"]![0]!["data"]!["owner"]);
        Assert.AreEqual(120, (double)parsed["nodes"]![0]!["style"]!["width"]!);
        Assert.AreEqual("parent", (string?)parsed["nodes"]![1]!["extent"]);
        Assert.AreEqual("step", (string?)parsed["edges"]![0]!["type"]);
        Assert.AreEqual("arrowclosed", (string?)parsed["edges"]![0]!["markerEnd"]!["type"]);
        Assert.AreEqual(3, (double)parsed["edges"]![0]!["style"]!["strokeWidth"]!);
        Assert.IsNull(parsed["edges"]![1]!["markerEnd"]);
        Assert.AreEqual(7, (int)parsed["edges"]![1]!["data"]!["value"]!);

        var back = BlowReactFlowFormat.Import(json).Document!;
        Assert.AreEqual("A", back.Nodes[0].Label);
        Assert.AreEqual(BlowPosition.Left, back.Nodes[1].TargetPosition);
        Assert.IsTrue(back.Nodes[1].ExtentParent);
        Assert.AreEqual(BlowEdgeType.Step, back.Edges[0].Type);
        Assert.IsTrue(back.Edges[0].MarkerEnd);
        Assert.IsFalse(back.Edges[1].MarkerEnd);
        Assert.AreEqual(new BlowViewport(5, 6, 0.5), back.Viewport);
    }

    [TestMethod]
    public void CustomPropertiesKeepTheirNamesBothWays()
    {
        var imported = BlowReactFlowFormat.Import("""{"nodes":[{"id":"a","style":{"--nodeAccent":"red","borderColor":"var(--nodeAccent)"}}]}""");
        Assert.AreEqual("--nodeAccent:red;border-color:var(--nodeAccent);", imported.Document!.Nodes[0].Style);

        var exported = JsonNode.Parse(BlowReactFlowFormat.Export(imported.Document))!;
        Assert.AreEqual("red", (string?)exported["nodes"]![0]!["style"]!["--nodeAccent"]);
        Assert.AreEqual("var(--nodeAccent)", (string?)exported["nodes"]![0]!["style"]!["borderColor"]);
    }

    [TestMethod]
    public void ImportsAreBoundedLikeBlowDocuments()
    {
        var options = new BlowSerializerOptions { Limits = { MaxNodes = 1 } };

        var tooMany = BlowReactFlowFormat.Import("""{"nodes":[{"id":"a"},{"id":"b"}]}""", options);
        var notReactFlow = BlowReactFlowFormat.Import("""{"cells":[]}""");
        var broken = BlowReactFlowFormat.Import("""{"nodes":[{"id":}]}""");

        Assert.AreEqual(BlowImportCodes.TooMany, tooMany.Issues.Single().Code);
        Assert.AreEqual(BlowImportCodes.NotADocument, notReactFlow.Issues.Single().Code);
        Assert.AreEqual(BlowImportCodes.InvalidJson, broken.Issues.Single().Code);
    }
}
