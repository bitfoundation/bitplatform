using System.Text.Json.Nodes;

namespace Bit.Blow.Tests;

/// <summary>
/// Comparing two versions of a graph and patching one into the other; Mermaid, DOT and GraphML out and in, including
/// input written to break out of the text or the XML.
/// </summary>
[TestClass]
public class DiffAndInterchangeTests
{
    private static BlowDocument Sample() => new()
    {
        Nodes =
        [
            new BlowNode { Id = "start", Label = "Start", Type = "input", Position = new(0, 0), SourcePosition = BlowPosition.Right },
            new BlowNode { Id = "group", Label = "Work", Type = "group", Position = new(200, 0), Width = 300, Height = 200, SourcePosition = BlowPosition.Right },
            new BlowNode { Id = "step", Label = "Do \"it\" <now> #1", ParentId = "group", Position = new(20, 40), SourcePosition = BlowPosition.Right },
            new BlowNode { Id = "end", Label = "End", Type = "output", Position = new(600, 0), SourcePosition = BlowPosition.Right },
        ],
        Edges =
        [
            new BlowEdge { Id = "a", Source = "start", Target = "step", Label = "go" },
            new BlowEdge { Id = "b", Source = "step", Target = "end", Animated = true },
        ],
    };

    // ---- diff ----

    [TestMethod]
    public void ADiffNamesWhatWasAddedRemovedAndChanged()
    {
        var before = Sample();
        var after = Sample();
        after.Nodes.RemoveAt(3);
        after.Edges.RemoveAt(1);
        after.Nodes[0].Position = new(10, 0);
        after.Nodes[0].Label = "Begin";
        after.Nodes.Add(new BlowNode { Id = "new", Label = "New" });
        after.Edges[0].Target = "new";

        var diff = BlowGraphDiff.Compare(before, after);

        CollectionAssert.AreEqual(new[] { "new" }, diff.AddedNodes.Select(n => n.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "end" }, diff.RemovedNodes.Select(n => n.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "Position", "Label" }, diff.ChangedNodes.Single().Properties.ToArray());
        CollectionAssert.AreEqual(new[] { "b" }, diff.RemovedEdges.Select(e => e.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "Target" }, diff.ChangedEdges.Single().Properties.ToArray());
        Assert.AreEqual(5, diff.Count);
    }

    [TestMethod]
    public void SelectionTinyMovesAndEqualJsonAreNotChanges()
    {
        var before = Sample();
        var after = Sample();
        after.Nodes[0].Selected = true;
        after.Nodes[1].Position = new(200.4, 0);
        before.Nodes[2].Data = JsonNode.Parse("""{"a":[1,2]}""");
        after.Nodes[2].Data = JsonNode.Parse("""{"a":[1,2]}""");

        Assert.IsTrue(BlowGraphDiff.Compare(before, after, new BlowGraphDiffOptions { PositionTolerance = 0.5 }).IsEmpty);
        Assert.AreEqual(2, BlowGraphDiff.Compare(before, after, new BlowGraphDiffOptions { IncludeSelection = true }).ChangedNodes.Count);
        Assert.IsTrue(BlowGraphDiff.Compare(before, after, new BlowGraphDiffOptions { Ignore = new HashSet<string> { "Position" } }).IsEmpty);
    }

    [TestMethod]
    public void ADiffAppliedToTheFirstGraphGivesTheSecondAndItsInverseGoesBack()
    {
        var before = Sample();
        var after = Sample();
        after.Nodes.RemoveAt(3);
        after.Edges.RemoveAt(1);
        after.Nodes[0].Label = "Begin";
        after.Nodes.Add(new BlowNode { Id = "new" });
        after.Edges.Add(new BlowEdge { Id = "c", Source = "start", Target = "new" });
        var diff = BlowGraphDiff.Compare(before, after);

        var nodes = before.Nodes.Select(n => n.Clone()).ToList();
        var edges = before.Edges.Select(e => e.Clone()).ToList();
        diff.ApplyTo(nodes, edges);

        Assert.IsTrue(BlowGraphDiff.Compare(nodes, edges, after.Nodes, after.Edges).IsEmpty);

        diff.Invert().ApplyTo(nodes, edges);
        var back = BlowGraphDiff.Compare(before.Nodes, before.Edges, nodes, edges);
        Assert.IsTrue(back.IsEmpty, string.Join(", ", back.ChangedNodes.SelectMany(c => c.Properties)));
    }

    [TestMethod]
    public void ApplyingADiffRemovesTheEdgesOfANodeItRemoves()
    {
        var before = Sample();
        var after = Sample();
        after.Nodes.RemoveAt(3); // "end", leaving the edge into it dangling in "after"
        var nodes = before.Nodes.ToList();
        var edges = before.Edges.ToList();

        BlowGraphDiff.Compare(before, after).ApplyTo(nodes, edges);

        Assert.IsFalse(edges.Any(e => e.Target == "end"));
    }

    // ---- Mermaid ----

    [TestMethod]
    public void MermaidExportWritesNodesGroupsAndLinks()
    {
        var text = BlowMermaidFormat.Export(Sample());

        StringAssert.StartsWith(text, "flowchart LR\n");
        StringAssert.Contains(text, "start([\"Start\"])");
        StringAssert.Contains(text, "subgraph group [\"Work\"]");
        StringAssert.Contains(text, "step[\"Do #quot;it#quot; #lt;now#gt; #35;1\"]");
        StringAssert.Contains(text, "start -->|\"go\"| step");
        StringAssert.Contains(text, "step -.-> n0");
    }

    [TestMethod]
    public void MermaidExportReplacesIdsMermaidCannotTake()
    {
        var doc = new BlowDocument
        {
            Nodes = [new BlowNode { Id = "a b\"]; click x", Label = "x" }, new BlowNode { Id = "end", Label = "y" }],
            Edges = [new BlowEdge { Id = "e", Source = "a b\"]; click x", Target = "end" }],
        };

        var text = BlowMermaidFormat.Export(doc);

        Assert.IsFalse(text.Contains("click"));
        StringAssert.Contains(text, "n0 --> n1");
    }

    [TestMethod]
    public void MermaidImportReadsTheCommonFlowchartSyntax()
    {
        const string text = """
            flowchart LR
                %% a comment
                A[Start] --> B{Is it?}
                B -->|Yes| C(["Do #quot;this#quot;"])
                B -- No --> D[(Store)]
                C & D --> E((End)); E -.-> F
                subgraph S [Side work]
                    G --- H
                end
                click A "javascript:alert(1)"
                style A fill:#f00
            """;

        var result = BlowMermaidFormat.Import(text);

        Assert.IsTrue(result.Succeeded, string.Join("; ", result.Issues.Select(i => i.Message)));
        var doc = result.Document!;
        CollectionAssert.AreEquivalent(new[] { "A", "B", "C", "D", "E", "F", "S", "G", "H" }, doc.Nodes.Select(n => n.Id).ToArray());
        Assert.AreEqual("Is it?", doc.Nodes.Single(n => n.Id == "B").Label);
        Assert.AreEqual("Do \"this\"", doc.Nodes.Single(n => n.Id == "C").Label);
        Assert.AreEqual("Yes", doc.Edges.Single(e => e.Source == "B" && e.Target == "C").Label);
        Assert.AreEqual("No", doc.Edges.Single(e => e.Source == "B" && e.Target == "D").Label);
        Assert.AreEqual(2, doc.Edges.Count(e => e.Target == "E"));
        Assert.IsTrue(doc.Edges.Single(e => e.Source == "E").Animated);
        Assert.IsFalse(doc.Edges.Single(e => e.Source == "G").MarkerEnd);
        Assert.AreEqual("group", doc.Nodes.Single(n => n.Id == "S").Type);
        Assert.AreEqual("S", doc.Nodes.Single(n => n.Id == "G").ParentId);
        Assert.AreEqual(BlowPosition.Right, doc.Nodes[0].SourcePosition);
        // Laid out: not everything at the origin, and left to right.
        Assert.IsTrue(doc.Nodes.Single(n => n.Id == "E").Position.X > doc.Nodes.Single(n => n.Id == "A").Position.X);
        Assert.IsTrue(result.Issues.Any(i => i.Message.Contains("click")));
    }

    [TestMethod]
    public void MermaidRoundTripsThroughExportAndImport()
    {
        var result = BlowMermaidFormat.Import(BlowMermaidFormat.Export(Sample()));

        Assert.IsTrue(result.Succeeded);
        var doc = result.Document!;
        Assert.AreEqual("Do \"it\" <now> #1", doc.Nodes.Single(n => n.Id == "step").Label);
        Assert.AreEqual("group", doc.Nodes.Single(n => n.Id == "step").ParentId);
        Assert.AreEqual("go", doc.Edges.Single(e => e.Source == "start").Label);
        Assert.IsTrue(doc.Edges.Single(e => e.Source == "step").Animated);
    }

    [TestMethod]
    public void MermaidImportRefusesWhatIsNotAFlowchartAndStaysWithinItsLimits()
    {
        Assert.AreEqual(BlowImportCodes.NotADocument, BlowMermaidFormat.Import("sequenceDiagram\n A->>B: hi").Issues[0].Code);
        Assert.AreEqual(BlowImportCodes.NotADocument, BlowMermaidFormat.Import("").Issues[0].Code);

        var many = "graph TD\n" + string.Join("\n", Enumerable.Range(0, 50).Select(i => $"n{i} --> n{i + 1}"));
        var limited = BlowMermaidFormat.Import(many, new BlowDiagramImportOptions { Limits = new BlowImportLimits { MaxNodes = 10 } });

        Assert.AreEqual(10, limited.Document!.Nodes.Count);
        Assert.IsTrue(limited.Issues.Any(i => i.Code == BlowImportCodes.TooMany));
        Assert.AreEqual(BlowImportCodes.TooLarge,
            BlowMermaidFormat.Import(many, new BlowDiagramImportOptions { Limits = new BlowImportLimits { MaxBytes = 10 } }).Issues[0].Code);
    }

    [TestMethod]
    public void MermaidImportReportsWhatItDoesNotUnderstand()
    {
        var result = BlowMermaidFormat.Import("graph TD\nA --> B\n???\nend");

        Assert.AreEqual(2, result.Document!.Nodes.Count);
        Assert.IsTrue(result.Issues.Any(i => i.Path == "line 3"));
        Assert.IsTrue(result.Issues.Any(i => i.Path == "line 4" && i.Message.Contains("closes no subgraph")));
    }

    // ---- DOT ----

    [TestMethod]
    public void DotExportQuotesEverything()
    {
        var doc = Sample();
        doc.Nodes[0].Label = "a\"; b -> c [color=red] \\ \n x";

        var text = BlowDotFormat.Export(doc, new BlowDotExportOptions { IncludePositions = true });

        StringAssert.StartsWith(text, "digraph \"G\" {\n    rankdir=LR;");
        StringAssert.Contains(text, "\"start\" [label=\"a\\\"; b -> c [color=red] \\\\ \\n x\", pos=");
        StringAssert.Contains(text, "subgraph \"cluster_0\" {");
        StringAssert.Contains(text, "\"start\" -> \"step\" [label=\"go\"];");
        StringAssert.Contains(text, "\"step\" -> \"end\" [style=dashed];");
        // Every line of the label stays inside its quotes: the text has no raw newline inside an attribute.
        Assert.IsFalse(text.Split('\n').Any(l => l.Trim() == "x\", pos="));
    }

    // ---- GraphML ----

    [TestMethod]
    public void GraphMLRoundTripsNodesGroupsEdgesAndData()
    {
        var doc = Sample();
        doc.Nodes[0].Data = JsonNode.Parse("""{"retries":3}""");
        doc.Nodes[0].Ports = [new BlowPort { Id = "out", Type = BlowHandleType.Source }];
        doc.Edges[0].SourceHandle = "out";

        var xml = BlowGraphMLFormat.Export(doc);
        var result = BlowGraphMLFormat.Import(xml);

        StringAssert.StartsWith(xml, "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        Assert.IsTrue(result.Succeeded, string.Join("; ", result.Issues.Select(i => i.Message)));
        var back = result.Document!;
        var diff = BlowGraphDiff.Compare(doc.Nodes, doc.Edges, back.Nodes, back.Edges,
            new BlowGraphDiffOptions { Ignore = new HashSet<string> { "SourcePosition", "Ports", "Animated" } });
        Assert.IsTrue(diff.IsEmpty, string.Join(", ", diff.ChangedNodes.Select(c => c.After.Id + ":" + string.Join("/", c.Properties))
            .Concat(diff.ChangedEdges.Select(c => c.After.Id + ":" + string.Join("/", c.Properties)))));
        Assert.AreEqual(3, back.Nodes[0].Data is JsonObject o ? (int)o["retries"]! : 0);
    }

    [TestMethod]
    public void GraphMLImportReadsYEdGeometryAndLabels()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns" xmlns:y="http://www.yworks.com/xml/graphml">
              <key id="d0" for="node" yfiles.type="nodegraphics"/>
              <key id="d1" for="edge" yfiles.type="edgegraphics"/>
              <key id="d2" for="node" attr.name="owner" attr.type="string"/>
              <graph id="G" edgedefault="directed">
                <node id="n0"><data key="d0"><y:ShapeNode><y:Geometry x="10" y="20" width="90" height="30"/><y:NodeLabel>First</y:NodeLabel></y:ShapeNode></data><data key="d2">ops</data></node>
                <node id="n1"><data key="d0"><y:ShapeNode><y:Geometry x="200" y="20" width="90" height="30"/><y:NodeLabel>Second</y:NodeLabel></y:ShapeNode></data></node>
                <edge id="e0" source="n0" target="n1"><data key="d1"><y:PolyLineEdge><y:EdgeLabel>next</y:EdgeLabel></y:PolyLineEdge></data></edge>
              </graph>
            </graphml>
            """;

        var doc = BlowGraphMLFormat.Import(xml).Document!;

        Assert.AreEqual("First", doc.Nodes[0].Label);
        Assert.AreEqual(new BlowXYPosition(10, 20), doc.Nodes[0].Position);
        Assert.AreEqual(90, doc.Nodes[0].Width);
        Assert.AreEqual("ops", (string)((JsonObject)doc.Nodes[0].Data!)["owner"]!);
        Assert.AreEqual("next", doc.Edges[0].Label);
    }

    [TestMethod]
    public void GraphMLImportRefusesDtdsAndExternalEntities()
    {
        const string xml = """
            <?xml version="1.0"?>
            <!DOCTYPE graphml [<!ENTITY xxe SYSTEM "file:///c:/windows/win.ini">]>
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns"><graph><node id="a"><data key="label">&xxe;</data></node></graph></graphml>
            """;

        var result = BlowGraphMLFormat.Import(xml);

        Assert.IsNull(result.Document);
        Assert.AreEqual(BlowImportCodes.InvalidXml, result.Issues[0].Code);
    }

    [TestMethod]
    public void GraphMLImportLaysOutNodesWithoutPositions()
    {
        const string xml = """
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns"><graph edgedefault="undirected">
              <node id="a"/><node id="b"/><node id="c"/><edge source="a" target="b"/><edge source="b" target="c"/>
            </graph></graphml>
            """;

        var result = BlowGraphMLFormat.Import(xml);
        var doc = result.Document!;

        Assert.AreEqual(3, doc.Nodes.Select(n => n.Position).Distinct().Count());
        Assert.IsTrue(doc.Edges.All(e => !e.MarkerEnd));
        Assert.IsTrue(result.Issues.Any(i => i.Message.Contains("undirected")));
    }

    [TestMethod]
    public void GraphMLExportKeepsControlCharactersOutOfTheXml()
    {
        var doc = new BlowDocument { Nodes = [new BlowNode { Id = "a", Label = "bad\u0001label </data><node id=\"x\"/>" }] };

        var xml = BlowGraphMLFormat.Export(doc);
        var back = BlowGraphMLFormat.Import(xml).Document!;

        Assert.AreEqual(1, back.Nodes.Count);
        Assert.AreEqual("badlabel </data><node id=\"x\"/>", back.Nodes[0].Label);
    }
}
