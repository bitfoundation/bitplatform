namespace Bit.Blow.Tests;

/// <summary>
/// The headless runner: values along edges, branches and joins, failures and what they skip, retries, timeouts,
/// cancellation, pausing, checkpoints, partial runs, sub-graphs and the event stream - with no canvas anywhere.
/// </summary>
[TestClass]
public class RuntimeTests
{
    private static BlowNode Node(string id, string type = "step") => new() { Id = id, Type = type, Label = id.ToUpperInvariant() };

    private static List<BlowNode> Nodes(params string[] ids) => [.. ids.Select(id => Node(id))];

    /// <summary>Edges as "a>b", or "a.handle>b.handle" for named handles.</summary>
    private static List<BlowEdge> Edges(params string[] pairs) => [.. pairs.Select(p =>
    {
        var ends = p.Split('>');
        var (s, sh) = Split(ends[0]);
        var (t, th) = Split(ends[1]);
        return new BlowEdge { Id = p, Source = s, SourceHandle = sh, Target = t, TargetHandle = th };
    })];

    private static (string, string?) Split(string end) => end.Split('.') is [var id, var handle] ? (id, handle) : (end, null);

    private static BlowRunner Summing() => new BlowRunner().Handle("step", ctx => ctx.GetInputs<int>().Sum() + 1);

    // ---- values, order and records ----

    [TestMethod]
    public async Task ValuesFlowAlongEdgesInDependencyOrder()
    {
        var order = new List<string>();
        var runner = new BlowRunner(new BlowRunnerOptions { MaxConcurrency = 1 })
            .Handle("step", ctx => { order.Add(ctx.Node.Id); return ctx.GetInputs<int>().Sum() + 1; });

        var result = await runner.RunAsync(Nodes("d", "c", "b", "a"), Edges("a>b", "b>c", "a>c", "c>d"));

        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, order);
        Assert.AreEqual(1, result.ValueOf<int>("a"));
        Assert.AreEqual(2, result.ValueOf<int>("b"));
        Assert.AreEqual(4, result.ValueOf<int>("c")); // a (1) + b (2) + 1
        Assert.AreEqual(5, result.ValueOf<int>("d"));
        Assert.IsTrue(result.Nodes.Values.All(r => r.Status == BlowRunStatus.Succeeded && r.Attempts == 1 && r.Duration is not null));
        CollectionAssert.AreEquivalent(new[] { "a>b", "b>c", "a>c", "c>d" }, result.ActiveEdges.ToArray());
    }

    [TestMethod]
    public async Task InputsAreReadByTargetHandle()
    {
        var runner = new BlowRunner()
            .Handle("number", ctx => ctx.Node.Id == "x" ? 6 : 2)
            .Handle("divide", ctx => ctx.GetInput<int>("dividend") / ctx.GetInput<int>("divisor"));
        var nodes = new List<BlowNode> { Node("x", "number"), Node("y", "number"), Node("q", "divide") };

        var result = await runner.RunAsync(nodes, Edges("x>q.dividend", "y>q.divisor"));

        Assert.AreEqual(3, result.ValueOf<int>("q"));
        Assert.AreEqual(2, result.Nodes["q"].Inputs.Count);
    }

    [TestMethod]
    public async Task TheRunInputReachesEveryNode()
    {
        var runner = new BlowRunner().Handle("step", ctx => (int)ctx.RunInput! + ctx.GetInputs<int>().Sum());

        var result = await runner.RunAsync(Nodes("a", "b"), Edges("a>b"), new BlowRunRequest { Input = 10 });

        Assert.AreEqual(20, result.ValueOf<int>("b"));
    }

    // ---- branches and joins ----

    [TestMethod]
    public async Task ABranchRunsOnlyThePathItChose()
    {
        var runner = new BlowRunner()
            .Handle("if", ctx => BlowNodeResult.Branch((int)ctx.RunInput! > 0 ? "yes" : "no", ctx.RunInput))
            .Handle("step", ctx => ctx.Node.Id);
        var nodes = new List<BlowNode> { Node("check", "if"), Node("positive"), Node("negative"), Node("after") };
        var edges = Edges("check.yes>positive", "check.no>negative", "positive>after", "negative>after");

        var result = await runner.RunAsync(nodes, edges, new BlowRunRequest { Input = 5 });

        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["positive"].Status);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["negative"].Status);
        Assert.AreEqual("No input reached it.", result.Nodes["negative"].Message);
        // The join after the branch runs on the one path that reached it.
        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["after"].Status);
        Assert.IsFalse(result.ActiveEdges.Contains("negative>after"));
    }

    [TestMethod]
    public async Task AnAllJoinIsSkippedWhenAnInputCarriedNothing()
    {
        var runner = new BlowRunner()
            .Handle("if", _ => BlowNodeResult.Branch("yes"))
            .Handle("step", _ => 1)
            .Handle("join", _ => 1, new BlowNodeRunOptions { Join = BlowJoinMode.All });
        var nodes = new List<BlowNode> { Node("check", "if"), Node("a"), Node("b"), Node("both", "join") };

        var result = await runner.RunAsync(nodes, Edges("check.yes>a", "check.no>b", "a>both", "b>both"));

        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["both"].Status);
        Assert.AreEqual("Not every input reached it.", result.Nodes["both"].Message);
    }

    [TestMethod]
    public async Task StopPassesNothingOn()
    {
        var runner = new BlowRunner().Handle("filter", _ => BlowNodeResult.Stop()).Handle("step", _ => 1);

        var result = await runner.RunAsync([Node("f", "filter"), Node("next")], Edges("f>next"));

        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["f"].Status);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["next"].Status);
        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
    }

    // ---- failures ----

    [TestMethod]
    public async Task AFailureSkipsWhatDependsOnItAndIndependentBranchesGoOn()
    {
        var runner = new BlowRunner().Handle("step", ctx => ctx.Node.Id == "bad" ? throw new InvalidOperationException("secret=hunter2") : 1);

        var result = await runner.RunAsync(Nodes("start", "bad", "after", "end", "other"),
            Edges("start>bad", "bad>after", "after>end", "start>other"));

        Assert.AreEqual(BlowRunOutcome.Failed, result.Outcome);
        Assert.AreEqual(BlowRunStatus.Failed, result.Nodes["bad"].Status);
        Assert.IsInstanceOfType<InvalidOperationException>(result.Nodes["bad"].Error);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["after"].Status);
        // The reason reaches the end of the chain.
        Assert.AreEqual("An earlier step failed.", result.Nodes["end"].Message);
        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["other"].Status);
    }

    [TestMethod]
    public async Task AnExceptionMessageIsNotShownUnlessTheApplicationSaysSo()
    {
        var failing = (BlowRunnerOptions o) => new BlowRunner(o).Handle("step", _ => throw new InvalidOperationException("password=hunter2"));

        var hidden = await failing(new BlowRunnerOptions()).RunAsync(Nodes("a"), []);
        var shown = await failing(new BlowRunnerOptions { DescribeError = e => e.Message }).RunAsync(Nodes("a"), []);
        var own = await new BlowRunner().Handle("step", _ => throw new BlowNodeException("The service said no.")).RunAsync(Nodes("a"), []);

        Assert.AreEqual("Failed.", hidden.Nodes["a"].Message);
        Assert.IsFalse(hidden.Events.Any(e => e.Message?.Contains("hunter2") == true));
        Assert.IsFalse(hidden.ToRunStates()["a"].Message!.Contains("hunter2"));
        Assert.AreEqual("password=hunter2", shown.Nodes["a"].Message);
        Assert.AreEqual("The service said no.", own.Nodes["a"].Message);
    }

    [TestMethod]
    public async Task AnErrorOutputCarriesTheFailureToItsHandler()
    {
        var runner = new BlowRunner()
            .Handle("call", _ => throw new BlowNodeException("Down."), new BlowNodeRunOptions { ErrorOutput = "error" })
            .Handle("step", ctx => ctx.GetInput<BlowRunError>()?.Message ?? "ok");
        var nodes = new List<BlowNode> { Node("call", "call"), Node("onSuccess"), Node("onError") };

        var result = await runner.RunAsync(nodes, Edges("call>onSuccess", "call.error>onError"));

        Assert.AreEqual(BlowRunStatus.Failed, result.Nodes["call"].Status);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["onSuccess"].Status);
        Assert.AreEqual("No input reached it.", result.Nodes["onSuccess"].Message);
        Assert.AreEqual("Down.", result.ValueOf<string>("onError"));
    }

    [TestMethod]
    public async Task StopOnFailureCancelsTheRest()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { StopOnFailure = true, MaxConcurrency = 2 })
            .Handle("fail", _ => throw new BlowNodeException("No."))
            .Handle("slow", async ctx => { await Task.Delay(Timeout.Infinite, ctx.CancellationToken); return BlowNodeResult.Done(); });
        var nodes = new List<BlowNode> { Node("slow", "slow"), Node("fail", "fail"), Node("later", "fail") };

        var result = await runner.RunAsync(nodes, Edges("slow>later"));

        Assert.AreEqual(BlowRunOutcome.Failed, result.Outcome);
        Assert.AreEqual(BlowRunStatus.Cancelled, result.Nodes["slow"].Status);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["later"].Status);
    }

    // ---- retries and timeouts ----

    [TestMethod]
    public async Task AFailingNodeIsTriedAgainAsItsPolicySays()
    {
        var calls = 0;
        var runner = new BlowRunner().Handle("step", _ => ++calls < 3 ? throw new IOException() : calls,
            new BlowNodeRunOptions { Retry = new BlowRetryPolicy { MaxAttempts = 4, Delay = TimeSpan.FromMilliseconds(1) } });

        var result = await runner.RunAsync(Nodes("a"), []);

        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["a"].Status);
        Assert.AreEqual(3, result.Nodes["a"].Attempts);
        Assert.AreEqual(2, result.Events.Count(e => e.Kind == BlowRunEventKind.NodeRetrying));
        Assert.AreEqual(3, result.Events.Count(e => e.Kind == BlowRunEventKind.NodeStarted));
        Assert.AreEqual("Failed. Trying again (attempt 2).", result.Events.First(e => e.Kind == BlowRunEventKind.NodeRetrying).Message);
    }

    [TestMethod]
    public async Task AFailureThatIsNotTransientIsNotRetried()
    {
        var calls = 0;
        var runner = new BlowRunner().Handle("step", _ => { calls++; throw new BlowNodeException("Bad input.") { IsTransient = false }; },
            new BlowNodeRunOptions { Retry = new BlowRetryPolicy { MaxAttempts = 5, Delay = TimeSpan.Zero } });

        var result = await runner.RunAsync(Nodes("a"), []);

        Assert.AreEqual(1, calls);
        Assert.AreEqual(BlowRunStatus.Failed, result.Nodes["a"].Status);
    }

    [TestMethod]
    public void BackoffGrowsByItsFactorUpToTheMaximum()
    {
        var policy = new BlowRetryPolicy { Delay = TimeSpan.FromSeconds(1), BackoffFactor = 2, MaxDelay = TimeSpan.FromSeconds(5) };

        Assert.AreEqual(TimeSpan.Zero, policy.DelayBefore(1));
        Assert.AreEqual(TimeSpan.FromSeconds(1), policy.DelayBefore(2));
        Assert.AreEqual(TimeSpan.FromSeconds(2), policy.DelayBefore(3));
        Assert.AreEqual(TimeSpan.FromSeconds(4), policy.DelayBefore(4));
        Assert.AreEqual(TimeSpan.FromSeconds(5), policy.DelayBefore(5));
        Assert.AreEqual(TimeSpan.FromSeconds(5), policy.DelayBefore(5000));
    }

    [TestMethod]
    public async Task ASlowAttemptTimesOut()
    {
        var runner = new BlowRunner().Handle("step", async ctx =>
        {
            await Task.Delay(Timeout.Infinite, ctx.CancellationToken);
            return BlowNodeResult.Done();
        }, new BlowNodeRunOptions { Timeout = TimeSpan.FromMilliseconds(30) });

        var result = await runner.RunAsync(Nodes("a"), []);

        Assert.AreEqual(BlowRunStatus.Failed, result.Nodes["a"].Status);
        Assert.AreEqual("Timed out after 30 ms.", result.Nodes["a"].Message);
        Assert.IsInstanceOfType<TimeoutException>(result.Nodes["a"].Error);
    }

    [TestMethod]
    public async Task NodeOptionsOverrideTheTypes()
    {
        var calls = 0;
        var runner = new BlowRunner(new BlowRunnerOptions
        {
            NodeOptions = n => n.Id == "b" ? new BlowNodeRunOptions { Retry = new BlowRetryPolicy { MaxAttempts = 2, Delay = TimeSpan.Zero } } : null,
        }).Handle("step", ctx => { calls++; throw new IOException(); });

        await runner.RunAsync(Nodes("a", "b"), []);

        Assert.AreEqual(3, calls);
    }

    // ---- cancelling, timing out and pausing a run ----

    [TestMethod]
    public async Task CancellingARunCancelsWhatIsWorkingAndSkipsTheRest()
    {
        using var cts = new CancellationTokenSource();
        var runner = new BlowRunner().Handle("step", async ctx =>
        {
            cts.Cancel();
            await Task.Delay(Timeout.Infinite, ctx.CancellationToken);
            return BlowNodeResult.Done();
        });

        var result = await runner.RunAsync(Nodes("a", "b"), Edges("a>b"), null, cts.Token);

        Assert.AreEqual(BlowRunOutcome.Cancelled, result.Outcome);
        Assert.AreEqual(BlowRunStatus.Cancelled, result.Nodes["a"].Status);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["b"].Status);
        Assert.AreEqual(BlowRunEventKind.RunFinished, result.Events[^1].Kind);
    }

    [TestMethod]
    public async Task ARunLongerThanItsTimeoutTimesOut()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { RunTimeout = TimeSpan.FromMilliseconds(30) })
            .Handle("step", async ctx => { await Task.Delay(Timeout.Infinite, ctx.CancellationToken); return BlowNodeResult.Done(); });

        var result = await runner.RunAsync(Nodes("a"), []);

        Assert.AreEqual(BlowRunOutcome.TimedOut, result.Outcome);
    }

    [TestMethod]
    public async Task APausedRunStartsNothingUntilResumed()
    {
        var control = new BlowRunControl();
        control.Pause();
        var ran = 0;
        var runner = Summing().Handle("step", _ => ++ran);

        var run = runner.RunAsync(Nodes("a", "b"), Edges("a>b"), new BlowRunRequest { Control = control });
        await Task.Delay(50);
        Assert.AreEqual(0, ran);
        Assert.IsFalse(run.IsCompleted);

        control.Resume();
        var result = await run;

        Assert.AreEqual(2, ran);
        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
    }

    [TestMethod]
    public async Task CancellingAPausedRunEndsIt()
    {
        var control = new BlowRunControl();
        control.Pause();
        using var cts = new CancellationTokenSource(30);

        var result = await Summing().RunAsync(Nodes("a"), [], new BlowRunRequest { Control = control }, cts.Token);

        Assert.AreEqual(BlowRunOutcome.Cancelled, result.Outcome);
        Assert.AreEqual(BlowRunStatus.Skipped, result.Nodes["a"].Status);
    }

    [TestMethod]
    public async Task AWaitingNodeIsShownAsWaiting()
    {
        var approval = new TaskCompletionSource<bool>();
        var states = new BlowRunStates();
        var runner = new BlowRunner().Handle("approve", async ctx =>
            await ctx.WaitAsync(approval.Task, "Waiting for a manager.") ? BlowNodeResult.Branch("approved") : BlowNodeResult.Branch("rejected"));

        var run = runner.RunAsync([Node("ok", "approve")], [], new BlowRunRequest { OnEvent = states.Apply });
        while (!states.TryGetValue("ok", out var s) || s.Status != BlowRunStatus.Waiting) await Task.Delay(5);
        Assert.AreEqual("Waiting for a manager.", states["ok"].Message);

        approval.SetResult(true);
        var result = await run;

        Assert.AreEqual(BlowRunStatus.Succeeded, result.Nodes["ok"].Status);
        Assert.IsTrue(result.ResultOf("ok")!.TryGetOutput("approved", out _));
    }

    // ---- concurrency ----

    [TestMethod]
    public async Task IndependentNodesRunAtOnceUpToTheLimit()
    {
        var working = 0;
        var most = 0;
        var runner = new BlowRunner(new BlowRunnerOptions { MaxConcurrency = 2 }).Handle("step", async ctx =>
        {
            most = Math.Max(most, Interlocked.Increment(ref working));
            await Task.Delay(20);
            Interlocked.Decrement(ref working);
            return BlowNodeResult.Done();
        });

        var result = await runner.RunAsync(Nodes("a", "b", "c", "d", "e"), []);

        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
        Assert.AreEqual(2, most);
    }

    // ---- refusing ----

    [TestMethod]
    public async Task ACycleIsRefusedBeforeAnythingRuns()
    {
        var ran = false;
        var runner = new BlowRunner().Handle("step", _ => ran = true);

        var result = await runner.RunAsync(Nodes("a", "b"), Edges("a>b", "b>a"));

        Assert.AreEqual(BlowRunOutcome.Refused, result.Outcome);
        Assert.IsFalse(ran);
        Assert.AreEqual(BlowValidationCodes.Cycle, result.Problems[0].Code);
    }

    [TestMethod]
    public async Task ANodeWithoutAHandlerIsRefusedPassedThroughOrSkipped()
    {
        var nodes = new List<BlowNode> { Node("a"), Node("note", "sticky"), Node("b") };
        var edges = Edges("a>note", "note>b");
        BlowRunner With(BlowMissingHandlerBehavior b) => new BlowRunner(new BlowRunnerOptions { MissingHandler = b })
            .Handle("step", ctx => ctx.GetInputs<int>().Sum() + 1);

        var refused = await With(BlowMissingHandlerBehavior.Refuse).RunAsync(nodes, edges);
        var passed = await With(BlowMissingHandlerBehavior.PassThrough).RunAsync(nodes, edges);
        var skipped = await With(BlowMissingHandlerBehavior.Skip).RunAsync(nodes, edges);

        Assert.AreEqual(BlowRunOutcome.Refused, refused.Outcome);
        Assert.AreEqual(BlowRunCodes.MissingHandler, refused.Problems.Single().Code);
        Assert.AreEqual("note", refused.Problems.Single().NodeId);
        Assert.AreEqual(2, passed.ValueOf<int>("b"));
        Assert.AreEqual(BlowRunStatus.Skipped, skipped.Nodes["note"].Status);
        Assert.AreEqual(BlowRunStatus.Skipped, skipped.Nodes["b"].Status);
    }

    [TestMethod]
    public async Task IncludeLeavesNodesOutOfTheRun()
    {
        var nodes = new List<BlowNode> { Node("frame", "group"), Node("a"), Node("b") };

        var result = await Summing().RunAsync(nodes, Edges("a>b"), new BlowRunRequest { Include = n => n.Type != "group" });

        Assert.AreEqual(BlowRunOutcome.Succeeded, result.Outcome);
        Assert.IsFalse(result.Nodes.ContainsKey("frame"));
    }

    [TestMethod]
    public async Task AValidatorErrorRefusesTheRun()
    {
        var validator = new BlowGraphValidator(BlowValidationRules.ForNodes("needs-label", n => n.Label is null ? "No label." : null));
        var nodes = new List<BlowNode> { new() { Id = "a", Type = "step" } };

        var result = await Summing().RunAsync(nodes, [], new BlowRunRequest { Validator = validator });

        Assert.AreEqual(BlowRunOutcome.Refused, result.Outcome);
        Assert.AreEqual("needs-label", result.Problems.Single().Code);
        Assert.AreEqual("needs-label", Summing().Check(nodes, [], new BlowRunRequest { Validator = validator }).Single().Code);
    }

    // ---- checkpoints and partial runs ----

    [TestMethod]
    public async Task AResumedRunDoesNotRunAgainWhatSucceeded()
    {
        var calls = new List<string>();
        var failC = true;
        var runner = new BlowRunner().Handle("step", ctx =>
        {
            calls.Add(ctx.Node.Id);
            if (ctx.Node.Id == "c" && failC) throw new BlowNodeException("Not yet.");
            return ctx.GetInputs<int>().Sum() + 1;
        });
        var nodes = Nodes("a", "b", "c");
        var edges = Edges("a>b", "b>c");

        var first = await runner.RunAsync(nodes, edges);
        failC = false;
        calls.Clear();
        var second = await runner.RunAsync(nodes, edges, new BlowRunRequest { Resume = first.ToCheckpoint() });

        Assert.AreEqual(BlowRunOutcome.Failed, first.Outcome);
        CollectionAssert.AreEqual(new[] { "c" }, calls);
        Assert.AreEqual(BlowRunOutcome.Succeeded, second.Outcome);
        Assert.AreEqual(3, second.ValueOf<int>("c"));
        Assert.IsTrue(second.Nodes["a"].Restored);
        Assert.AreEqual(first.RunId, second.RunId);
    }

    [TestMethod]
    public async Task StartAtRunsANodeAndWhatDependsOnItAgain()
    {
        var calls = new List<string>();
        var runner = new BlowRunner().Handle("step", ctx => { calls.Add(ctx.Node.Id); return ctx.GetInputs<int>().Sum() + 1; });
        var nodes = Nodes("a", "b", "c", "side");
        var edges = Edges("a>b", "b>c", "a>side");
        var first = await runner.RunAsync(nodes, edges);
        calls.Clear();

        var again = await runner.RunAsync(nodes, edges, new BlowRunRequest { StartAt = ["b"], Resume = first.ToCheckpoint() });

        CollectionAssert.AreEquivalent(new[] { "b", "c" }, calls);
        Assert.AreEqual(3, again.ValueOf<int>("c"));
        Assert.IsTrue(again.Nodes["a"].Restored);
        Assert.IsTrue(again.Nodes["side"].Restored);
    }

    [TestMethod]
    public async Task AnUnknownStartNodeIsRefused()
    {
        var result = await Summing().RunAsync(Nodes("a"), [], new BlowRunRequest { StartAt = ["nope"] });

        Assert.AreEqual(BlowRunCodes.UnknownStart, result.Problems.Single().Code);
    }

    [TestMethod]
    public async Task TheIdempotencyKeyIsTheSameAcrossRetriesAndResumes()
    {
        var keys = new List<string>();
        var calls = 0;
        var runner = new BlowRunner().Handle("step", ctx => { keys.Add(ctx.IdempotencyKey); return ++calls < 2 ? throw new IOException() : 1; },
            new BlowNodeRunOptions { Retry = new BlowRetryPolicy { MaxAttempts = 2, Delay = TimeSpan.Zero } });

        var first = await runner.RunAsync(Nodes("a"), [], new BlowRunRequest { RunId = "run-1" });

        CollectionAssert.AreEqual(new[] { "run-1:a", "run-1:a" }, keys);
        Assert.AreEqual("run-1", first.RunId);
    }

    // ---- sub-graphs and limits ----

    [TestMethod]
    public async Task ANodeCanRunAGraphOfItsOwnAsALoop()
    {
        var inner = Nodes("double");
        var runner = new BlowRunner()
            .Handle("step", ctx => (int)ctx.RunInput! * 2)
            .Handle("loop", async ctx =>
            {
                var total = 0;
                foreach (var item in new[] { 1, 2, 3 })
                {
                    var run = await ctx.RunSubgraphAsync(inner, [], item);
                    total += run.ValueOf<int>("double");
                }
                return BlowNodeResult.Done(total);
            });

        var result = await runner.RunAsync([Node("each", "loop")], []);

        Assert.AreEqual(12, result.ValueOf<int>("each"));
    }

    [TestMethod]
    public async Task RunsNestedTooDeeplyAreRefused()
    {
        var graph = new List<BlowNode> { Node("self", "recurse") };
        var deepest = BlowRunOutcome.Succeeded;
        var runner = new BlowRunner(new BlowRunnerOptions { MaxDepth = 3 });
        runner.Handle("recurse", async ctx =>
        {
            var inner = await ctx.RunSubgraphAsync(graph, []);
            if (inner.Outcome == BlowRunOutcome.Refused) deepest = inner.Outcome;
            return BlowNodeResult.Done();
        });

        await runner.RunAsync(graph, []);

        Assert.AreEqual(BlowRunOutcome.Refused, deepest);
    }

    [TestMethod]
    public async Task ARunStopsAtItsExecutionLimit()
    {
        var runner = new BlowRunner(new BlowRunnerOptions { MaxExecutions = 2, MaxConcurrency = 1 }).Handle("step", _ => 1);

        var result = await runner.RunAsync(Nodes("a", "b", "c"), []);

        Assert.AreEqual(BlowRunStatus.Failed, result.Nodes["c"].Status);
        Assert.AreEqual("The run reached its limit of steps.", result.Nodes["c"].Message);
    }

    [TestMethod]
    public async Task AFailureAtTheStartOfALongChainSkipsItWithoutRecursing()
    {
        const int n = 20_000;
        var nodes = Enumerable.Range(0, n).Select(i => Node($"n{i}")).ToList();
        var edges = Enumerable.Range(1, n - 1).Select(i => new BlowEdge { Id = $"e{i}", Source = $"n{i - 1}", Target = $"n{i}" }).ToList();
        var runner = new BlowRunner().Handle("step", _ => throw new BlowNodeException("No."));

        var result = await runner.RunAsync(nodes, edges);

        Assert.AreEqual(n - 1, result.Nodes.Values.Count(r => r.Status == BlowRunStatus.Skipped));
    }

    // ---- events, states and playback ----

    [TestMethod]
    public async Task EventsComeInOrderAndStatesFollowThem()
    {
        var states = new BlowRunStates();
        var kinds = new List<BlowRunEventKind>();
        var runner = new BlowRunner().Handle("step", ctx => { ctx.Report("Halfway.", 0.5); return 1; });

        var result = await runner.RunAsync(Nodes("a", "b"), Edges("a>b"), new BlowRunRequest
        {
            OnEvent = e => { kinds.Add(e.Kind); states.Apply(e); },
        });

        Assert.AreEqual(BlowRunEventKind.RunStarted, kinds[0]);
        Assert.AreEqual(BlowRunEventKind.RunFinished, kinds[^1]);
        CollectionAssert.AreEqual(result.Events.Select(e => e.Kind).ToList(), kinds);
        Assert.AreEqual(0.5, result.Events.First(e => e.Kind == BlowRunEventKind.NodeProgress).Progress);
        Assert.AreEqual(BlowRunStatus.Succeeded, states["b"].Status);
        Assert.IsTrue(states.ActiveEdges.Contains("a>b"));
        CollectionAssert.AreEquivalent(result.ToRunStates().ToList(), states.ToList());
    }

    [TestMethod]
    public async Task ARunCanBePlayedBack()
    {
        var result = await Summing().RunAsync(Nodes("a", "b"), Edges("a>b"));
        var afterA = result.Events.ToList().FindIndex(e => e.Kind == BlowRunEventKind.NodeSucceeded && e.NodeId == "a") + 1;

        var start = result.StatesAt(1);
        var middle = result.StatesAt(afterA);
        var end = result.StatesAt(int.MaxValue);

        Assert.AreEqual(0, start.Count);
        Assert.AreEqual(BlowRunStatus.Succeeded, middle["a"].Status);
        Assert.AreEqual(BlowRunStatus.Queued, middle["b"].Status);
        Assert.AreEqual(BlowRunStatus.Succeeded, end["b"].Status);
    }

    [TestMethod]
    public async Task AnObserverThatThrowsStopsTheRunAndSurfaces()
    {
        var runner = Summing();

        await Assert.ThrowsExactlyAsync<InvalidTimeZoneException>(() => runner.RunAsync(Nodes("a", "b"), Edges("a>b"), new BlowRunRequest
        {
            OnEvent = e => { if (e.Kind == BlowRunEventKind.NodeSucceeded) throw new InvalidTimeZoneException(); },
        }));
    }

    [TestMethod]
    public async Task TheRunnerStaysOnTheCallersContext()
    {
        var context = new CountingContext();
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var seen = new List<SynchronizationContext?>();
            var runner = new BlowRunner().Handle("step", async ctx => { await Task.Delay(1); seen.Add(SynchronizationContext.Current); return BlowNodeResult.Done(); });

            var run = runner.RunAsync(Nodes("a", "b"), Edges("a>b"), new BlowRunRequest { OnEvent = _ => seen.Add(SynchronizationContext.Current) });
            await run.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.IsTrue(seen.All(c => c == context), "every event and handler continuation ran on the caller's context");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    /// <summary>A single-threaded context, as a Blazor renderer's is.</summary>
    private sealed class CountingContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback, object?)> _queue = [];

        public CountingContext()
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
}
