using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Blow.Tests;

/// <summary>
/// The round 14 review of the runner, the registry, the diff and the diagram formats: each case asserts the documented
/// behaviour and failed on the code before its fix - hostile imports that hung or crashed the process, callbacks and
/// timeouts that threw out of RunAsync, handlers that ignored their token, a partial run that ran nothing, events off
/// the caller's context, and the smaller contract breaks.
/// </summary>
[TestClass]
public class RoundFourteenReviewTests
{
    private static BlowNode Node(string id, string type = "step") => new() { Id = id, Type = type, Label = id.ToUpperInvariant() };

    private static List<BlowNode> Nodes(params string[] ids) => [.. ids.Select(id => Node(id))];

    private static List<BlowEdge> Edges(params string[] pairs) => [.. pairs.Select(p =>
    {
        var ends = p.Split('>');
        return new BlowEdge { Id = p, Source = ends[0], Target = ends[1] };
    })];

    // ---- runner: RunAsync must not throw for node-level problems ----

    [TestMethod]
    public async Task Runner_ATimeoutLongerThanATimerTakesDoesNotThrowOutOfRunAsync()
    {
        // NodeOptions is documented as "a timeout read from the node's data, say": 60 days is a plausible value.
        var runner = new BlowRunner(new BlowRunnerOptions { NodeOptions = _ => new BlowNodeRunOptions { Timeout = TimeSpan.FromDays(60) } })
            .Handle("step", _ => 1);

        Exception? thrown = null;
        BlowRunResult? result = null;
        try { result = await runner.RunAsync(Nodes("a"), []); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"RunAsync threw {thrown?.GetType().Name}: {thrown?.Message}");
        Assert.AreEqual(BlowRunOutcome.Succeeded, result!.Outcome, $"outcome {result.Outcome}");
    }

    [TestMethod]
    public async Task Runner_ANegativeTimeoutDoesNotThrowOutOfRunAsync()
    {
        var runner = new BlowRunner().Handle("step", _ => 1, new BlowNodeRunOptions { Timeout = TimeSpan.FromSeconds(-5) });

        Exception? thrown = null;
        try { await runner.RunAsync(Nodes("a"), []); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"RunAsync threw {thrown?.GetType().Name}: {thrown?.Message}");
    }

    [TestMethod]
    public async Task Runner_ARunTimeoutOfSixtyDaysDoesNotThrow()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { RunTimeout = TimeSpan.FromDays(60) }).Handle("step", _ => 1);

        Exception? thrown = null;
        try { await runner.RunAsync(Nodes("a"), []); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"RunAsync threw {thrown?.GetType().Name}: {thrown?.Message}");
    }

    [TestMethod]
    public async Task Runner_AShouldRetryThatThrowsFailsTheNodeNotTheRun()
    {
        var runner = new BlowRunner().Handle("step", ctx => ctx.Node.Id == "a" ? throw new IOException() : 1,
            new BlowNodeRunOptions { Retry = new BlowRetryPolicy { MaxAttempts = 2, Delay = TimeSpan.Zero, ShouldRetry = _ => throw new InvalidOperationException("policy bug") } });

        Exception? thrown = null;
        BlowRunResult? result = null;
        try { result = await runner.RunAsync(Nodes("a", "b"), []); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"RunAsync threw {thrown?.GetType().Name}: {thrown?.Message}");
        Assert.AreEqual(BlowRunStatus.Succeeded, result!.Nodes["b"].Status);
    }

    [TestMethod]
    public async Task Runner_ARetryDelayLongerThanATimerTakesDoesNotThrow()
    {
        var calls = 0;
        var runner = new BlowRunner().Handle("step", _ => ++calls == 1 ? throw new IOException() : 1,
            new BlowNodeRunOptions { Retry = new BlowRetryPolicy { MaxAttempts = 2, Delay = TimeSpan.FromDays(60), MaxDelay = TimeSpan.FromDays(60) } });
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        Exception? thrown = null;
        BlowRunResult? result = null;
        try { result = await runner.RunAsync(Nodes("a"), [], cancellationToken: cts.Token); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"RunAsync threw {thrown?.GetType().Name}: {thrown?.Message}");
        Assert.AreEqual(BlowRunOutcome.Cancelled, result!.Outcome, $"outcome {result.Outcome}");
    }

    [TestMethod]
    public async Task Runner_ANodeOptionsCallbackThatThrowsDoesNotThrowOutOfRunAsync()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { NodeOptions = n => n.Id == "a" ? throw new FormatException("bad data") : null })
            .Handle("step", _ => 1);

        Exception? thrown = null;
        try { await runner.RunAsync(Nodes("a", "b"), Edges("a>b")); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"RunAsync threw {thrown?.GetType().Name}: {thrown?.Message}");
    }

    // ---- runner: timeouts and handlers that ignore cancellation ----

    [TestMethod]
    public async Task Runner_ARunTimeoutEndsARunWhoseHandlerIgnoresCancellation()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { RunTimeout = TimeSpan.FromMilliseconds(150) })
            .Handle("step", async _ => { await Task.Delay(2500); return BlowNodeResult.Done(1); });

        var watch = Stopwatch.StartNew();
        var result = await runner.RunAsync(Nodes("a"), []);
        watch.Stop();

        Assert.IsTrue(watch.ElapsedMilliseconds < 1500, $"the run took {watch.ElapsedMilliseconds} ms with a 150 ms RunTimeout; outcome {result.Outcome}, a {result.Nodes["a"].Status}");
    }

    [TestMethod]
    public async Task Runner_AnAttemptSlowerThanItsTimeoutCountsAsAFailureEvenIfItIgnoresTheToken()
    {
        var runner = new BlowRunner()
            .Handle("step", async _ => { await Task.Delay(400); return BlowNodeResult.Done(1); },
                new BlowNodeRunOptions { Timeout = TimeSpan.FromMilliseconds(50) });

        var result = await runner.RunAsync(Nodes("a"), []);

        Assert.AreEqual(BlowRunStatus.Failed, result.Nodes["a"].Status, $"status {result.Nodes["a"].Status}, outcome {result.Outcome}");
    }

    // ---- runner: partial runs ----

    [TestMethod]
    public async Task Runner_StartAtWithoutACheckpointRunsTheStartNode()
    {
        var calls = new List<string>();
        var runner = new BlowRunner().Handle("step", ctx => { calls.Add(ctx.Node.Id); return 1; });

        var result = await runner.RunAsync(Nodes("a", "b", "c"), Edges("a>b", "b>c"), new BlowRunRequest { StartAt = ["b"] });

        Assert.IsTrue(calls.Contains("b"),
            $"ran [{string.Join(",", calls)}]; b {result.Nodes["b"].Status} '{result.Nodes["b"].Message}', c {result.Nodes["c"].Status}; outcome {result.Outcome}");
    }

    // ---- runner: the observer's context ----

    [TestMethod]
    public async Task Runner_AReportFromTaskRunStillReachesTheObserverOnTheCallersContext()
    {
        var context = new SingleThreadContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var offContext = 0;
            var runner = new BlowRunner().Handle("step", async ctx =>
            {
                // "Handlers that do heavy work off that context use Task.Run themselves."
                await Task.Run(() => ctx.Report("half way", 0.5));
                return BlowNodeResult.Done();
            });

            var run = runner.RunAsync(Nodes("a"), [], new BlowRunRequest
            {
                OnEvent = _ => { if (SynchronizationContext.Current != context) Interlocked.Increment(ref offContext); },
            });
            await run.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.AreEqual(0, offContext, $"{offContext} event(s) reached OnEvent off the caller's context");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private sealed class SingleThreadContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback, object?)> _queue = [];

        public SingleThreadContext()
        {
            var thread = new Thread(() =>
            {
                SetSynchronizationContext(this);
                foreach (var (callback, state) in _queue.GetConsumingEnumerable()) callback(state);
            }) { IsBackground = true };
            thread.Start();
        }

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));
    }

    // ---- runner: sub-runs ----

    [TestMethod]
    public async Task Runner_ASubRunsIdempotencyKeysAreTheSameAcrossTheOuterNodesRetries()
    {
        var innerKeys = new List<string>();
        var attempts = 0;
        var runner = new BlowRunner();
        runner.Handle("inner", ctx => { innerKeys.Add(ctx.IdempotencyKey); return 1; });
        runner.Handle("outer", async ctx =>
        {
            await ctx.RunSubgraphAsync([new BlowNode { Id = "i", Type = "inner" }], []);
            if (++attempts == 1) throw new IOException();
            return BlowNodeResult.Done();
        }, new BlowNodeRunOptions { Retry = new BlowRetryPolicy { MaxAttempts = 2, Delay = TimeSpan.Zero } });

        await runner.RunAsync([new BlowNode { Id = "o", Type = "outer" }], []);

        Assert.AreEqual(2, innerKeys.Count);
        Assert.AreEqual(innerKeys[0], innerKeys[1], $"inner keys: {string.Join(" | ", innerKeys)}");
    }

    // ---- registry ----

    [TestMethod]
    public void Registry_UpgradeSetsThePortsFromTheNewDefinition()
    {
        var registry = new BlowNodeRegistry().Register(
            new BlowNodeDefinition
            {
                Type = "old",
                Deprecated = "use new",
                ReplacedBy = "new",
                Ports = [new BlowPort { Type = BlowHandleType.Target, Id = "legacyIn" }, new BlowPort { Type = BlowHandleType.Source, Id = "legacyOut" }],
            },
            new BlowNodeDefinition
            {
                Type = "new",
                Ports = [new BlowPort { Type = BlowHandleType.Target, Id = "in" }, new BlowPort { Type = BlowHandleType.Source, Id = "out" }],
            });
        var node = registry.Create("old");

        registry.Upgrade([node]);

        var ids = string.Join(",", node.Ports!.Select(p => p.Id));
        Assert.AreEqual("in,out", ids, $"ports after upgrade: {ids}");
    }

    // ---- diff ----

    [TestMethod]
    public void Diff_JsonElementAndJsonNodeWithTheSameContentAreNotAChange()
    {
        var element = JsonDocument.Parse("{\"a\":1}").RootElement;
        var node = JsonNode.Parse("{\"a\":1}");
        var before = new List<BlowNode> { new() { Id = "n", Data = element } };
        var after = new List<BlowNode> { new() { Id = "n", Data = node } };

        var diff = BlowGraphDiff.Compare(before, [], after, []);

        Assert.IsTrue(diff.IsEmpty, $"changed: {string.Join(",", diff.ChangedNodes.SelectMany(c => c.Properties))}");
    }

    // ---- Mermaid ----

    [TestMethod]
    public void Mermaid_ANodeWhoseIdStartsWithClickIsImported()
    {
        var result = BlowMermaidFormat.Import("graph TD\nclickHandler --> done");

        var ids = string.Join(",", result.Document!.Nodes.Select(n => n.Id));
        Assert.AreEqual("clickHandler,done", ids,
            $"nodes [{ids}]; issues: {string.Join(" | ", result.Issues.Select(i => i.Message))}");
    }

    [TestMethod]
    public void Mermaid_ALimitStopsTheRestOfTheLineAndDoesNotFloodTheIssues()
    {
        var line = string.Concat(Enumerable.Repeat("a-->b;", 5000));
        var result = BlowMermaidFormat.Import("graph TD\n" + line, new BlowDiagramImportOptions { Limits = new BlowImportLimits { MaxEdges = 10 }, Layout = null });

        Assert.IsTrue(result.Issues.Count < 50, $"{result.Issues.Count} issues for one line past the edge limit; edges {result.Document?.Edges.Count}");
    }

    [TestMethod]
    public void Mermaid_ACrossProductPastTheLimitDoesNotFloodTheIssues()
    {
        var left = string.Join(" & ", Enumerable.Range(0, 300).Select(i => $"a{i}"));
        var right = string.Join(" & ", Enumerable.Range(0, 300).Select(i => $"b{i}"));
        var text = $"graph TD\n{left} --> {right}";

        var result = BlowMermaidFormat.Import(text, new BlowDiagramImportOptions { Limits = new BlowImportLimits { MaxEdges = 100 }, Layout = null });

        Assert.IsTrue(result.Issues.Count < 50, $"{result.Issues.Count} issues from {text.Length} characters");
    }

    [TestMethod]
    public void Mermaid_ALinkTextFollowedBySpacesIsReadInLinearTime()
    {
        var text = "graph TD\nA -- x" + new string(' ', 30_000) + "y";

        var watch = Stopwatch.StartNew();
        BlowMermaidFormat.Import(text, new BlowDiagramImportOptions { Layout = null });
        watch.Stop();

        Assert.IsTrue(watch.ElapsedMilliseconds < 1000, $"{watch.ElapsedMilliseconds} ms for one {text.Length}-character statement");
    }

    [TestMethod]
    public void Mermaid_ASubgraphTitleFollowedBySpacesIsReadInLinearTime()
    {
        var text = "graph TD\nsubgraph x [" + new string(' ', 30_000) + "a";

        var watch = Stopwatch.StartNew();
        BlowMermaidFormat.Import(text, new BlowDiagramImportOptions { Layout = null });
        watch.Stop();

        Assert.IsTrue(watch.ElapsedMilliseconds < 1000, $"{watch.ElapsedMilliseconds} ms for one {text.Length}-character statement");
    }

    [TestMethod]
    public void Mermaid_ALongChainIsReadInLinearTime()
    {
        var statement = "a" + string.Concat(Enumerable.Repeat("-->a", 15_000)); // ~60 KB, under MaxStringLength
        var text = "graph TD\n" + string.Join("\n", Enumerable.Repeat(statement, 4));

        var watch = Stopwatch.StartNew();
        BlowMermaidFormat.Import(text, new BlowDiagramImportOptions { Layout = null });
        watch.Stop();

        Assert.IsTrue(watch.ElapsedMilliseconds < 2000, $"{watch.ElapsedMilliseconds} ms for {text.Length} characters");
    }

    [TestMethod]
    public void Mermaid_SubgraphNestingIsBoundedByMaxDepth()
    {
        var depth = 500;
        var text = "graph TD\n" + string.Concat(Enumerable.Range(0, depth).Select(i => $"subgraph g{i}\n")) + "leaf\n" + string.Concat(Enumerable.Repeat("end\n", depth));

        var result = BlowMermaidFormat.Import(text, new BlowDiagramImportOptions { Layout = null });

        var byId = result.Document!.Nodes.ToDictionary(n => n.Id);
        var levels = 0;
        for (var n = byId["leaf"]; n.ParentId is { } p; n = byId[p]) levels++;
        Assert.IsTrue(levels <= new BlowImportLimits().MaxDepth || result.Issues.Any(i => i.Code == BlowImportCodes.TooDeep),
            $"imported {levels} levels of nesting with MaxDepth {new BlowImportLimits().MaxDepth}, no too-deep issue");
    }

    /// <summary>
    /// Scaling, not a deadline: four times the groups and four times the edges take about four times as long when the
    /// work is linear, and sixteen times when every group scans every edge - whatever the machine's speed.
    /// </summary>
    [TestMethod]
    public void Mermaid_ArrangingManyGroupsWithManyEdgesIsNotQuadratic()
    {
        static string Text(int groups, int edges)
        {
            var text = new StringBuilder("graph TD\n");
            for (var i = 0; i < groups; i++) text.Append("subgraph g").Append(i).Append("\nx").Append(i).Append("\nend\n");
            for (var i = 0; i < edges; i++) text.Append("p-->q\n");
            return text.ToString();
        }

        // The best of a few runs, so a pause of the machine's own does not count as the import's.
        static double Time(string text)
        {
            var options = new BlowDiagramImportOptions { Layout = new BlowLayeredLayoutOptions() };
            BlowMermaidFormat.Import(text, options);
            var best = double.MaxValue;
            for (var run = 0; run < 3; run++)
            {
                var watch = Stopwatch.StartNew();
                BlowMermaidFormat.Import(text, options);
                best = Math.Min(best, watch.Elapsed.TotalMilliseconds);
            }
            return best;
        }

        var small = Time(Text(1000, 12_500));
        var large = Time(Text(4000, 50_000));

        Assert.IsTrue(large < small * 10, $"{small:0} ms for 1,000 groups and 12,500 edges, {large:0} ms for four times both");
    }

    // ---- GraphML ----

    [TestMethod]
    public void GraphML_ExportDoesNotThrowOnAControlCharacterInAnId()
    {
        var doc = new BlowDocument { Nodes = [new BlowNode { Id = "a\u0001b" }] };

        Exception? thrown = null;
        try { BlowGraphMLFormat.Export(doc); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"Export threw {thrown?.GetType().Name}: {thrown?.Message}");
    }

    [TestMethod]
    public void GraphML_ExportDoesNotThrowOnALabelMermaidImportProduced()
    {
        // #65534; is U+FFFE, which XML 1.0 cannot carry; char.IsControl is false for it.
        var doc = BlowMermaidFormat.Import("graph TD\nA[\"x #65534; y\"] --> B").Document!;

        Exception? thrown = null;
        try { BlowGraphMLFormat.Export(doc); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"Export threw {thrown?.GetType().Name}: {thrown?.Message}; label '{doc.Nodes[0].Label}'");
    }

    [TestMethod]
    public void GraphML_ExportDoesNotThrowOnALoneSurrogate()
    {
        var doc = new BlowDocument { Nodes = [new BlowNode { Id = "a", Label = "x\uD800y" }] };

        Exception? thrown = null;
        try { BlowGraphMLFormat.Export(doc); }
        catch (Exception ex) { thrown = ex; }

        Assert.IsNull(thrown, $"Export threw {thrown?.GetType().Name}: {thrown?.Message}");
    }

    [TestMethod]
    public void GraphML_AGeneratedEdgeIdDoesNotRepeatAnExplicitOne()
    {
        var xml = """
            <graphml xmlns="http://graphml.graphdrawing.org/xmlns"><graph edgedefault="directed">
              <node id="a"/><node id="b"/>
              <edge id="e1-a-b" source="a" target="b"/>
              <edge source="a" target="b"/>
            </graph></graphml>
            """;

        var doc = BlowGraphMLFormat.Import(xml).Document!;

        var ids = doc.Edges.Select(e => e.Id).ToList();
        Assert.AreEqual(ids.Count, ids.Distinct().Count(), $"edge ids: {string.Join(",", ids)}");
    }

    [TestMethod]
    public void GraphML_PortsStayWithinMaxPortsPerNode()
    {
        var ports = string.Concat(Enumerable.Range(0, 10).Select(i => $"<port name=\"p{i}\"/>"));
        var xml = $"<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\"><graph><node id=\"a\">{ports}</node></graph></graphml>";

        var doc = BlowGraphMLFormat.Import(xml, new BlowDiagramImportOptions { Limits = new BlowImportLimits { MaxPortsPerNode = 4 } }).Document!;

        Assert.IsTrue(doc.Nodes[0].Ports!.Count <= 4, $"{doc.Nodes[0].Ports!.Count} ports with MaxPortsPerNode 4");
    }

    [TestMethod]
    public void GraphML_AnEdgeToAMissingNodeIsReported()
    {
        var xml = """<graphml xmlns="http://graphml.graphdrawing.org/xmlns"><graph><node id="a"/><edge source="a" target="ghost"/></graph></graphml>""";

        var result = BlowGraphMLFormat.Import(xml);

        Assert.IsTrue(result.Issues.Any(i => i.Severity >= BlowValidationSeverity.Warning),
            $"edges {result.Document!.Edges.Count}, issues: {string.Join(" | ", result.Issues.Select(i => i.Code))}");
    }

    /// <summary>XmlNode.InnerText recurses once per level; a deep tree is refused before it is built.</summary>
    [TestMethod]
    public void GraphML_DeeplyNestedDataDoesNotOverflowTheStack()
    {
        const int depth = 200_000; // ~1.4 MB of text, far under MaxBytes
        var xml = "<graphml xmlns=\"http://graphml.graphdrawing.org/xmlns\"><graph><node id=\"a\"><data key=\"k\">"
                  + string.Concat(Enumerable.Repeat("<x>", depth)) + "t" + string.Concat(Enumerable.Repeat("</x>", depth))
                  + "</data></node></graph></graphml>";

        var result = BlowGraphMLFormat.Import(xml);

        Assert.AreEqual(BlowImportCodes.TooDeep, result.Issues.Single().Code);
    }

    /// <summary>The exporters walk groups with a stack of their own, so a chain of any depth is written.</summary>
    [TestMethod]
    public void Mermaid_ExportOfADeepGroupChainDoesNotOverflowTheStack()
    {
        const int depth = 100_000;
        var nodes = Enumerable.Range(0, depth).Select(i => new BlowNode { Id = $"g{i}", ParentId = i == 0 ? null : $"g{i - 1}" }).ToList();

        var document = new BlowDocument { Nodes = nodes };

        var text = BlowMermaidFormat.Export(document);
        var dot = BlowDotFormat.Export(document);
        var xml = BlowGraphMLFormat.Export(document);

        Assert.AreEqual(depth - 1, text.Split('\n').Count(l => l.Trim() == "end"));
        Assert.AreEqual(depth - 1, dot.Split('\n').Count(l => l.Trim().StartsWith("subgraph", StringComparison.Ordinal)));
        Assert.IsTrue(xml.Length > depth * 10);
        // The indentation is capped: the text grows with the depth, not its square.
        Assert.IsTrue(text.Length < depth * 200, $"{text.Length} characters");
    }

    [TestMethod]
    public void NodesOnAParentCycleAreStillExported()
    {
        var nodes = new List<BlowNode>
        {
            new() { Id = "a", ParentId = "b", Label = "A" }, new() { Id = "b", ParentId = "a", Label = "B" }, new() { Id = "c", Label = "C" },
        };
        var document = new BlowDocument { Nodes = nodes, Edges = [new BlowEdge { Id = "e", Source = "a", Target = "c" }] };

        var mermaid = BlowMermaidFormat.Import(BlowMermaidFormat.Export(document)).Document!;
        var graphml = BlowGraphMLFormat.Import(BlowGraphMLFormat.Export(document)).Document!;

        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, mermaid.Nodes.Select(n => n.Id).ToArray());
        CollectionAssert.AreEquivalent(new[] { "a", "b", "c" }, graphml.Nodes.Select(n => n.Id).ToArray());
    }

    [TestMethod]
    public void DotPointsAnEdgeToAGroupAtItsCluster()
    {
        var document = new BlowDocument
        {
            Nodes = [new() { Id = "g", Label = "G" }, new() { Id = "in", ParentId = "g" }, new() { Id = "x" }],
            Edges = [new BlowEdge { Id = "e", Source = "x", Target = "g" }],
        };

        var dot = BlowDotFormat.Export(document);

        StringAssert.Contains(dot, "compound=true;");
        StringAssert.Contains(dot, "\"x\" -> \"in\" [lhead=\"cluster_0\"]");
        Assert.IsFalse(dot.Contains("\"g\" ["), "the group is drawn as a cluster only");
    }

    [TestMethod]
    public async Task AnErrorOutputWithNoEdgeDoesNotSwallowTheFailure()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { StopOnFailure = true, MaxConcurrency = 1 })
            .Handle("step", ctx => ctx.Node.Id == "bad" ? throw new BlowNodeException("No.") : 1, new BlowNodeRunOptions { ErrorOutput = "error" });

        var result = await runner.RunAsync(Nodes("bad", "after", "other"), Edges("bad>after"));

        Assert.AreEqual("An earlier step failed.", result.Nodes["after"].Message);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["other"].Status, "StopOnFailure stopped the rest");
    }

    [TestMethod]
    public async Task ACancellationAfterEveryNodeSettledDoesNotMakeTheRunCancelled()
    {
        using var cts = new CancellationTokenSource();
        var runner = new BlowRunner().Handle("step", _ => 1);

        var result = await runner.RunAsync(Nodes("a"), [], new BlowRunRequest
        {
            OnEvent = e => { if (e.Kind == BlowRunEventKind.NodeSucceeded) cts.Cancel(); },
        }, cts.Token);

        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
    }

    [TestMethod]
    public async Task AReportAfterTheAttemptEndedChangesNothing()
    {
        BlowNodeRunContext? kept = null;
        var runner = new BlowRunner().Handle("step", ctx => { kept = ctx; return 1; });

        var result = await runner.RunAsync(Nodes("a"), []);
        var events = result.Events.Count;
        kept!.Report("Late.");

        Assert.IsNull(result.Nodes["a"].Message);
        Assert.AreEqual(events, result.Events.Count);
    }
}
