namespace Bit.Blow.Tests;

/// <summary>
/// The round 15 review of the runner, the registry and the diagram formats: each case asserts the documented behaviour
/// and failed on the code before its fix.
/// </summary>
[TestClass]
public class RoundFifteenReviewTests
{
    private static BlowNode Node(string id, string type = "step") => new() { Id = id, Type = type, Label = id.ToUpperInvariant() };

    private static List<BlowNode> Nodes(params string[] ids) => [.. ids.Select(id => Node(id))];

    private static List<BlowEdge> Edges(params string[] pairs) => [.. pairs.Select(p =>
    {
        var ends = p.Split('>');
        var source = ends[0].Split('.');
        return new BlowEdge { Id = p, Source = source[0], SourceHandle = source.Length > 1 ? source[1] : null, Target = ends[1] };
    })];

    // ---- runner ----

    [TestMethod]
    public async Task Runner_AnInfiniteNodeTimeoutIsNoLimit()
    {
        var runner = new BlowRunner().Handle("step", async ctx =>
        {
            await Task.Delay(20, ctx.CancellationToken);
            return BlowNodeResult.Done(1);
        }, new BlowNodeRunOptions { Timeout = Timeout.InfiniteTimeSpan });

        var result = await runner.RunAsync(Nodes("a"), []);

        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
        Assert.AreEqual(1, result.Nodes["a"].Attempts);
    }

    [TestMethod]
    public async Task Runner_AnInfiniteRunTimeoutIsNoLimit()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { RunTimeout = Timeout.InfiniteTimeSpan }).Handle("step", _ => 1);

        var result = await runner.RunAsync(Nodes("a", "b"), Edges("a>b"));

        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["b"].Status);
    }

    [TestMethod]
    public async Task Runner_TheNodesAfterACancelledOneAreReportedAsStoppedByTheRun()
    {
        using var cts = new CancellationTokenSource();
        var runner = new BlowRunner()
            .Handle("step", _ => 1)
            .Handle("slow", async ctx => { await Task.Delay(Timeout.Infinite, ctx.CancellationToken); return BlowNodeResult.Done(); });
        var nodes = new List<BlowNode> { Node("a"), Node("enrich", "slow"), Node("merge") };

        var result = await runner.RunAsync(nodes, Edges("a>enrich", "enrich>merge", "a>merge"), new BlowRunRequest
        {
            OnEvent = e => { if (e is { Kind: BlowRunEventKind.NodeStarted, NodeId: "enrich" }) cts.Cancel(); },
        }, cts.Token);

        Assert.AreEqual(BlowRunOutcome.Cancelled, result.Outcome);
        Assert.AreEqual(BlowRunStatus.Cancelled, result.Nodes["enrich"].Status);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["merge"].Status);
        Assert.AreEqual("The run stopped before reaching it.", result.Nodes["merge"].Message);
    }

    [TestMethod]
    public async Task Runner_AFailureItsErrorOutputHandledDoesNotFailTheRun()
    {
        var runner = new BlowRunner()
            .Handle("call", _ => throw new BlowNodeException("Down."), new BlowNodeRunOptions { ErrorOutput = "error" })
            .Handle("step", _ => "recovered");
        var nodes = new List<BlowNode> { Node("x", "call"), Node("recover"), Node("after") };

        var result = await runner.RunAsync(nodes, Edges("x.error>recover", "recover>after"));

        Assert.AreEqual(BlowRunStatus.Failed, result.Nodes["x"].Status);
        Assert.IsTrue(result.Nodes["x"].Handled);
        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["after"].Status);
        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
        Assert.IsTrue(result.Succeeded);
    }

    [TestMethod]
    public async Task Runner_AFailureWithNoErrorEdgeStillFailsTheRun()
    {
        var runner = new BlowRunner()
            .Handle("call", _ => throw new BlowNodeException("Down."), new BlowNodeRunOptions { ErrorOutput = "error" });

        var result = await runner.RunAsync([Node("x", "call")], []);

        Assert.IsFalse(result.Nodes["x"].Handled);
        Assert.AreEqual(BlowRunOutcome.Failed, result.Outcome);
    }

    // ---- registry ----

    [TestMethod]
    public void Registry_ALoopOfReplacementsMigratesOnceAndStopsBeforeGoingBack()
    {
        var migrated = new List<string>();
        var registry = new BlowNodeRegistry().Register(
            new BlowNodeDefinition { Type = "a", Deprecated = "x", ReplacedBy = "b", Migrate = _ => migrated.Add("a") },
            new BlowNodeDefinition { Type = "b", Deprecated = "x", ReplacedBy = "a", Migrate = _ => migrated.Add("b") });
        var nodes = new List<BlowNode> { new() { Id = "n", Type = "a" } };

        var changed = registry.Upgrade(nodes);

        Assert.AreEqual("b", nodes[0].Type);
        CollectionAssert.AreEqual(new[] { "a" }, migrated);
        Assert.AreEqual(1, changed.Count);
    }

    // ---- GraphML ----

    [TestMethod]
    public void GraphML_YEdChildrenArePlacedRelativeToTheirGroup()
    {
        const string xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns" xmlns:y="http://www.yworks.com/xml/graphml">
              <key id="d0" for="node" yfiles.type="nodegraphics"/>
              <graph id="G" edgedefault="directed">
                <node id="g"><data key="d0"><y:ProxyAutoBoundsNode><y:Realizers active="0"><y:GroupNode><y:Geometry x="500" y="300" width="200" height="150"/></y:GroupNode></y:Realizers></y:ProxyAutoBoundsNode></data>
                  <graph id="g:" edgedefault="directed">
                    <node id="c"><data key="d0"><y:ShapeNode><y:Geometry x="520" y="340" width="60" height="30"/></y:ShapeNode></data></node>
                  </graph>
                </node>
              </graph>
            </graphml>
            """;

        var doc = BlowGraphMLFormat.Import(xml).Document!;

        Assert.AreEqual(new BlowXYPosition(500, 300), doc.Nodes.Single(n => n.Id == "g").Position);
        var child = doc.Nodes.Single(n => n.Id == "c");
        Assert.AreEqual("g", child.ParentId);
        Assert.AreEqual(new BlowXYPosition(20, 40), child.Position);
    }

    [TestMethod]
    public void GraphML_NodesWithoutAPositionAreLaidOutInAFileThatPlacesOnlySome()
    {
        var nodes = string.Concat(Enumerable.Range(1, 5).Select(i => $"<node id=\"n{i}\"/>"));
        var edges = string.Concat(Enumerable.Range(1, 4).Select(i => $"<edge source=\"n{i}\" target=\"n{i + 1}\"/>"));
        var xml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns">
              <key id="x" for="node" attr.name="x" attr.type="double"/>
              <key id="y" for="node" attr.name="y" attr.type="double"/>
              <graph id="G" edgedefault="directed">
                <node id="placed"><data key="x">100</data><data key="y">50</data></node>
                {nodes}{edges}
              </graph>
            </graphml>
            """;

        var doc = BlowGraphMLFormat.Import(xml).Document!;

        var placed = doc.Nodes.Single(n => n.Id == "placed");
        Assert.AreEqual(new BlowXYPosition(100, 50), placed.Position);
        var rest = doc.Nodes.Where(n => n.Id != "placed").ToList();
        Assert.AreEqual(rest.Count, rest.Select(n => n.Position).Distinct().Count(), "every unplaced node gets a place of its own");
        Assert.IsTrue(rest.All(n => n.Position.Y > placed.Position.Y), "set below the placed node");
    }

    // ---- Mermaid ----

    [TestMethod]
    public void Mermaid_ASubgraphOfTitleWordsKeepsTheWholeTitle()
    {
        const string text = """
            flowchart TD
                subgraph Data ingest
                    A
                end
                subgraph Data export
                    B
                end
            """;

        var doc = BlowMermaidFormat.Import(text).Document!;

        var groups = doc.Nodes.Where(n => n.Type == "group").ToList();
        CollectionAssert.AreEquivalent(new[] { "Data ingest", "Data export" }, groups.Select(g => g.Label).ToArray());
        Assert.AreNotEqual(groups[0].Id, groups[1].Id);
        Assert.AreNotEqual(doc.Nodes.Single(n => n.Id == "A").ParentId, doc.Nodes.Single(n => n.Id == "B").ParentId);
    }

    [TestMethod]
    public void Mermaid_ADoublePercentInsideALabelIsNotAComment()
    {
        var doc = BlowMermaidFormat.Import("flowchart TD\n    A[\"100%% done\"] --> B %% a comment").Document!;

        Assert.AreEqual("100%% done", doc.Nodes.Single(n => n.Id == "A").Label);
        Assert.AreEqual(1, doc.Edges.Count);
    }

    [TestMethod]
    public void Mermaid_ALabelWithADoublePercentRoundTrips()
    {
        var source = new BlowDocument
        {
            Nodes = [new BlowNode { Id = "A", Label = "100%% done" }, new BlowNode { Id = "B", Label = "next" }],
            Edges = [new BlowEdge { Id = "e", Source = "A", Target = "B", Label = "50%% off" }],
        };

        var doc = BlowMermaidFormat.Import(BlowMermaidFormat.Export(source)).Document!;

        Assert.AreEqual("100%% done", doc.Nodes.Single(n => n.Id == "A").Label);
        Assert.AreEqual("50%% off", doc.Edges.Single().Label);
    }
}
