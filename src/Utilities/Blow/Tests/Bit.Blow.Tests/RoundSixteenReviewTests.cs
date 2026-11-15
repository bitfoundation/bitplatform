using System.Text.Json.Nodes;

namespace Bit.Blow.Tests;

/// <summary>
/// The round 16 review of the diff, the runner and the diagram formats: each case asserts the documented behaviour
/// and failed on the code before its fix.
/// </summary>
[TestClass]
public class RoundSixteenReviewTests
{
    private static BlowNode Node(string id, string type = "step") => new() { Id = id, Type = type, Label = id.ToUpperInvariant() };

    private static List<BlowNode> Nodes(params string[] ids) => [.. ids.Select(id => Node(id))];

    private static List<BlowEdge> Edges(params string[] pairs) => [.. pairs.Select(p =>
    {
        var ends = p.Split('>');
        return new BlowEdge { Id = p, Source = ends[0], Target = ends[1] };
    })];

    // ---- diff ----

    [TestMethod]
    public void Diff_AnEdgeMovedOffARemovedNodeIsKept()
    {
        var beforeNodes = Nodes("a", "b", "c");
        var beforeEdges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "b" } };
        var afterNodes = Nodes("a", "c");
        var afterEdges = new List<BlowEdge> { new() { Id = "e", Source = "a", Target = "c" } };

        BlowGraphDiff.Compare(beforeNodes, beforeEdges, afterNodes, afterEdges).ApplyTo(beforeNodes, beforeEdges);

        Assert.AreEqual("c", beforeEdges.Single().Target);
        CollectionAssert.AreEqual(new[] { "a", "c" }, beforeNodes.Select(n => n.Id).ToArray());
    }

    [TestMethod]
    public void Diff_AnEdgeOfARemovedNodeTheDiffDoesNotNameStillGoes()
    {
        var beforeNodes = Nodes("a", "b");
        var beforeEdges = Edges("a>b");

        BlowGraphDiff.Compare(beforeNodes, beforeEdges, Nodes("a"), Edges("a>b")).ApplyTo(beforeNodes, beforeEdges);

        Assert.AreEqual(0, beforeEdges.Count);
    }

    [TestMethod]
    public void Diff_MarkerAndLabelChangesAreNamedByTheirProperty()
    {
        var before = new BlowEdge { Id = "e", Source = "a", Target = "b" };
        var after = new BlowEdge { Id = "e", Source = "a", Target = "b", MarkerStart = true, LabelBgPaddingX = 9 };

        var diff = BlowGraphDiff.Compare([], [before], [], [after]);
        var ignored = BlowGraphDiff.Compare([], [before], [], [after], new BlowGraphDiffOptions { Ignore = new HashSet<string> { "MarkerStart" } });

        CollectionAssert.AreEqual(new[] { "MarkerStart", "LabelBgPaddingX" }, diff.ChangedEdges.Single().Properties.ToArray());
        CollectionAssert.AreEqual(new[] { "LabelBgPaddingX" }, ignored.ChangedEdges.Single().Properties.ToArray());
    }

    // ---- runner ----

    [TestMethod]
    public async Task Runner_AnEdgeToANodeLeftOutByIncludeDoesNotRefuseTheRun()
    {
        var runner = new BlowRunner().Handle("step", _ => 1);
        var validator = BlowGraphValidator.CreateDefault();

        var included = await runner.RunAsync(Nodes("a", "b", "x"), Edges("a>b", "x>a"), new BlowRunRequest { Include = n => n.Id != "x", Validator = validator });
        var dangling = await runner.RunAsync(Nodes("a", "b"), Edges("a>b", "a>ghost"), new BlowRunRequest { Validator = validator });

        Assert.AreEqual(BlowRunOutcome.Succeeded, included.Outcome, string.Join(" | ", included.Problems.Select(p => p.Message)));
        Assert.AreEqual(BlowRunOutcome.Refused, dangling.Outcome);
    }

    [TestMethod]
    public async Task Runner_TheValidatorSeesARepeatedNodeId()
    {
        var runner = new BlowRunner().Handle("step", _ => 1);

        var result = await runner.RunAsync(Nodes("a", "a"), [], new BlowRunRequest { Validator = BlowGraphValidator.CreateDefault() });

        Assert.AreEqual(BlowRunOutcome.Refused, result.Outcome);
        Assert.IsTrue(result.Problems.Any(p => p.Code == BlowValidationCodes.DuplicateNodeId));
    }

    [TestMethod]
    public async Task Runner_InputsAreInTheOrderOfTheIncomingEdges()
    {
        string[]? order = null;
        var runner = new BlowRunner()
            .Handle("step", async ctx =>
            {
                if (ctx.Node.Id == "slow") await Task.Delay(100);
                if (ctx.Node.Id == "join") order = [.. ctx.Inputs.Select(i => i.Edge.Source)];
                return BlowNodeResult.Done(ctx.Node.Id);
            });

        await runner.RunAsync(Nodes("slow", "fast", "join"), Edges("slow>join", "fast>join"));

        CollectionAssert.AreEqual(new[] { "slow", "fast" }, order);
    }

    [TestMethod]
    public async Task Runner_AnEdgeReconnectedDuringTheRunIsRunAsItWasAtTheStart()
    {
        var edges = Edges("a>b");
        var runner = new BlowRunner().Handle("step", ctx =>
        {
            if (ctx.Node.Id == "a") edges[0].Target = "c";
            return ctx.Node.Id;
        });

        var result = await runner.RunAsync(Nodes("a", "b", "c"), edges);

        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["b"].Status, $"b {result.Nodes["b"].Status} '{result.Nodes["b"].Message}'");
        Assert.AreEqual("a", result.Nodes["b"].Inputs.Single().Value);
    }

    [TestMethod]
    public async Task Runner_DistinctSubRunPathsGiveDistinctIdempotencyKeys()
    {
        var keys = new List<string>();
        var runner = new BlowRunner(new BlowRunnerOptions { MaxConcurrency = 1 });
        runner.Handle("leaf", ctx => { lock (keys) keys.Add(ctx.IdempotencyKey); return 1; });
        runner.Handle("wrapB", async ctx =>
        {
            await ctx.RunSubgraphAsync([new BlowNode { Id = "x", Type = "leaf" }], []);
            return BlowNodeResult.Done();
        });
        runner.Handle("wrapA", async ctx =>
        {
            await ctx.RunSubgraphAsync([new BlowNode { Id = "b", Type = "wrapB" }], []);
            return BlowNodeResult.Done();
        });

        // a's sub-run 1 runs b, whose sub-run 1 runs x; the node "a/1/b" runs x in its sub-run 1 as well.
        await runner.RunAsync([new BlowNode { Id = "a", Type = "wrapA" }, new BlowNode { Id = "a/1/b", Type = "wrapB" }], [], new BlowRunRequest { RunId = "R" });

        Assert.AreEqual(2, keys.Distinct().Count(), string.Join(" | ", keys));
    }

    [TestMethod]
    public async Task Runner_SubRunsShareTheRunsMaxConcurrency()
    {
        var working = 0;
        var most = 0;
        var runner = new BlowRunner(new BlowRunnerOptions { MaxConcurrency = 2 });
        runner.Handle("leaf", async _ =>
        {
            var now = Interlocked.Increment(ref working);
            lock (runner) most = Math.Max(most, now);
            await Task.Delay(40);
            Interlocked.Decrement(ref working);
            return BlowNodeResult.Done();
        });
        runner.Handle("outer", async ctx =>
        {
            var inner = Enumerable.Range(0, 3).Select(i => new BlowNode { Id = $"i{i}", Type = "leaf" }).ToList();
            var result = await ctx.RunSubgraphAsync(inner, []);
            return result.Succeeded ? BlowNodeResult.Done() : throw new InvalidOperationException(result.Outcome.ToString());
        });

        var run = await runner.RunAsync([new BlowNode { Id = "p1", Type = "outer" }, new BlowNode { Id = "p2", Type = "outer" }], [])
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual(BlowRunOutcome.Succeeded, run.Outcome);
        Assert.IsTrue(most <= 2, $"{most} inner nodes worked at once with MaxConcurrency 2");
    }

    [TestMethod]
    public async Task Runner_ASubRunWithOneSlotDoesNotDeadlock()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { MaxConcurrency = 1 });
        runner.Handle("leaf", _ => 1);
        runner.Handle("outer", async ctx =>
        {
            var result = await ctx.RunSubgraphAsync([new BlowNode { Id = "i", Type = "leaf" }, new BlowNode { Id = "j", Type = "leaf" }], Edges("i>j"));
            return result.Succeeded ? BlowNodeResult.Done() : throw new InvalidOperationException(result.Outcome.ToString());
        });

        var run = await runner.RunAsync([new BlowNode { Id = "o", Type = "outer" }, new BlowNode { Id = "p", Type = "outer" }], [])
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreEqual(BlowRunOutcome.Succeeded, run.Outcome, string.Join(" | ", run.Nodes.Values.Select(r => $"{r.NodeId} {r.Status} {r.Message}")));
    }

    // ---- import issues ----

    [TestMethod]
    public void Import_AnErrorPastTheIssueCapStillFailsTheImport()
    {
        var text = "graph TD\n???\n???\n" + new string('a', 50);

        var result = BlowMermaidFormat.Import(text, new BlowDiagramImportOptions { MaxIssues = 1, Limits = new BlowImportLimits { MaxStringLength = 20 }, Layout = null });

        Assert.AreEqual(2, result.Issues.Count);
        Assert.IsFalse(result.Succeeded, string.Join(" | ", result.Issues.Select(i => $"{i.Severity} {i.Message}")));
    }

    // ---- GraphML ----

    [TestMethod]
    public void GraphML_DataLongerThanMaxStringLengthStillParses()
    {
        var json = "{\"text\":\"" + new string('x', 100) + "\"}";
        var xml = "<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\"><key id=\"d\" for=\"node\" attr.name=\"data\"/><graph>"
                  + $"<node id=\"a\"><data key=\"d\">{json}</data></node></graph></graphml>";

        var node = BlowGraphMLFormat.Import(xml, new BlowDiagramImportOptions { Limits = new BlowImportLimits { MaxStringLength = 20 } }).Document!.Nodes[0];

        Assert.AreEqual(new string('x', 100), ((JsonObject)node.Data!)["text"]!.GetValue<string>());
    }

    [TestMethod]
    public void GraphML_ExportNamesEachPortOnce()
    {
        var doc = new BlowDocument
        {
            Nodes = [new BlowNode { Id = "a", Ports = [new BlowPort { Id = "p", Type = BlowHandleType.Target }, new BlowPort { Id = "p", Type = BlowHandleType.Source }] }],
        };

        var xml = BlowGraphMLFormat.Export(doc);

        Assert.AreEqual(1, xml.Split("<port ").Length - 1, xml);
    }

    [TestMethod]
    public void GraphML_NumbersAndBooleansAreWrittenAndOtherPayloadsAreSaidToBeLeftOut()
    {
        var doc = new BlowDocument { Nodes = [new BlowNode { Id = "n", Data = 42 }, new BlowNode { Id = "b", Data = true }, new BlowNode { Id = "o", Data = new object() }] };

        var back = BlowGraphMLFormat.Import(BlowGraphMLFormat.Export(doc)).Document!.Nodes;

        Assert.AreEqual(42, ((JsonValue)back[0].Data!).GetValue<int>());
        Assert.IsTrue(((JsonValue)back[1].Data!).GetValue<bool>());
        Assert.IsNull(back[2].Data);
        StringAssert.Contains(BlowGraphMLFormat.Export(doc), "<!-- data not written: a Object is not JSON");
    }

    [TestMethod]
    public void GraphML_AnEdgeWithoutAnArrowheadComesBackWithoutOne()
    {
        var doc = new BlowDocument { Nodes = Nodes("a", "b"), Edges = [new BlowEdge { Id = "u", Source = "a", Target = "b", MarkerEnd = false }, new BlowEdge { Id = "d", Source = "b", Target = "a" }] };

        var edges = BlowGraphMLFormat.Import(BlowGraphMLFormat.Export(doc)).Document!.Edges;

        Assert.IsFalse(edges.Single(e => e.Id == "u").MarkerEnd);
        Assert.IsTrue(edges.Single(e => e.Id == "d").MarkerEnd);
    }

    [TestMethod]
    public void GraphML_ANestedGraphReadsItsOwnEdgeDefault()
    {
        var xml = """
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns"><graph edgedefault="directed">
              <node id="g"><graph id="g:" edgedefault="undirected"><node id="a"/><node id="b"/><edge source="a" target="b"/></graph></node>
              <node id="c"/><edge source="g" target="c"/>
            </graph></graphml>
            """;

        var edges = BlowGraphMLFormat.Import(xml).Document!.Edges;

        Assert.IsFalse(edges.Single(e => e.Source == "a").MarkerEnd);
        Assert.IsTrue(edges.Single(e => e.Source == "g").MarkerEnd);
    }

    [TestMethod]
    public void GraphML_TextNamingAnEncodingIsLeftAlone()
    {
        var doc = new BlowDocument { Nodes = [new BlowNode { Id = "a", Label = "encoding=\"utf-16\"" }] };

        var xml = BlowGraphMLFormat.Export(doc);

        StringAssert.StartsWith(xml, "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        Assert.AreEqual("encoding=\"utf-16\"", BlowGraphMLFormat.Import(xml).Document!.Nodes[0].Label);
    }

    [TestMethod]
    public void GraphML_BlowsOwnPositionWinsOverYEdGeometry()
    {
        var xml = """
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns" xmlns:y="http://www.yworks.com/xml/graphml">
              <key id="x" for="node" attr.name="x"/><key id="y" for="node" attr.name="y"/><key id="d0" for="node" yfiles.type="nodegraphics"/>
              <graph edgedefault="directed">
                <node id="g"><data key="d0"><y:ShapeNode><y:Geometry x="500" y="300" width="200" height="150"/></y:ShapeNode></data>
                  <graph id="g:" edgedefault="directed">
                    <node id="c"><data key="x">10</data><data key="y">20</data><data key="d0"><y:ShapeNode><y:Geometry x="900" y="900" width="60" height="30"/></y:ShapeNode></data></node>
                  </graph>
                </node>
              </graph>
            </graphml>
            """;

        var child = BlowGraphMLFormat.Import(xml).Document!.Nodes.Single(n => n.Id == "c");

        Assert.AreEqual(new BlowXYPosition(10, 20), child.Position);
    }

    // ---- Mermaid ----

    [TestMethod]
    public void Mermaid_ANodeNamedBeforeItsSubgraphJoinsIt()
    {
        var doc = BlowMermaidFormat.Import("graph TD\nA --> B\nsubgraph G\nB\nend\nsubgraph H\nB\nend", new BlowDiagramImportOptions { Layout = null }).Document!;

        Assert.IsNull(doc.Nodes.Single(n => n.Id == "A").ParentId);
        Assert.AreEqual("G", doc.Nodes.Single(n => n.Id == "B").ParentId, "the first subgraph listing it keeps it");
    }

    [TestMethod]
    public void Mermaid_ASubgraphNamedInsideItselfIsNotItsOwnParent()
    {
        var doc = BlowMermaidFormat.Import("graph TD\nsubgraph G\nG --> x\nend", new BlowDiagramImportOptions { Layout = null }).Document!;

        Assert.IsNull(doc.Nodes.Single(n => n.Id == "G").ParentId);
    }

    [TestMethod]
    public void Mermaid_ExportKeepsAnArrowheadAtTheStart()
    {
        var doc = new BlowDocument
        {
            Nodes = Nodes("s", "t", "u"),
            Edges =
            [
                new BlowEdge { Id = "1", Source = "s", Target = "t", MarkerStart = true, MarkerEnd = false },
                new BlowEdge { Id = "2", Source = "s", Target = "u", MarkerStart = true, Animated = true },
            ],
        };

        var text = BlowMermaidFormat.Export(doc);

        StringAssert.Contains(text, "t --> s");
        StringAssert.Contains(text, "s <-.-> u");
        var back = BlowMermaidFormat.Import(text, new BlowDiagramImportOptions { Layout = null }).Document!.Edges.Single(e => e.Target == "u");
        Assert.IsTrue(back.MarkerStart && back.MarkerEnd && back.Animated);
    }

    // ---- DOT ----

    [TestMethod]
    public void Dot_PositionsOfChildrenIncludeTheirParentsOffset()
    {
        var doc = new BlowDocument
        {
            Nodes =
            [
                new BlowNode { Id = "g", Position = new(100, 100), Width = 300, Height = 200 },
                new BlowNode { Id = "c", ParentId = "g", Position = new(10, 10), Width = 100, Height = 40 },
            ],
        };

        var dot = BlowDotFormat.Export(doc, new BlowDotExportOptions { IncludePositions = true });

        StringAssert.Contains(dot, "pos=\"120,-97.5!\"");
    }

    [TestMethod]
    public void Dot_AnEdgeIsNotAttachedToAPortTheBoxDoesNotDeclare()
    {
        var doc = new BlowDocument { Nodes = Nodes("a", "b"), Edges = [new BlowEdge { Id = "e", Source = "a", SourceHandle = "out", Target = "b", TargetHandle = "in" }] };

        var dot = BlowDotFormat.Export(doc);

        StringAssert.Contains(dot, "\"a\" -> \"b\"");
        Assert.IsFalse(dot.Contains("\"out\"") || dot.Contains("\"in\""), dot);
    }
}
