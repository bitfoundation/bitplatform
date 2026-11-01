namespace Boilerplate.WorkbenchTests.Features.Scenario;

/// <summary>
/// The data states are what make a page's every state reproducible: each load has to behave the same way every time.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchScenarioStateTests
{
    public TestContext TestContext { get; set; } = default!;

    private static WorkbenchScenarioState In(WorkbenchDataState data)
    {
        var state = new WorkbenchScenarioState();
        state.Set(new() { Data = data });
        return state;
    }

    [TestMethod]
    public async Task Live_Should_ReturnTheLiveData()
    {
        Assert.AreEqual("live", await In(WorkbenchDataState.Live).Load("source", () => "live", () => "empty", TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Empty_Should_ReturnTheEmptyData()
    {
        Assert.AreEqual("empty", await In(WorkbenchDataState.Empty).Load("source", () => "live", () => "empty", TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Error_Should_FailTheFirstLoadAndLetARetrySucceed()
    {
        var state = In(WorkbenchDataState.Error);

        await Assert.ThrowsExactlyAsync<WorkbenchSimulatedException>(() => state.Load("source", () => "live", () => "empty", TestContext.CancellationToken));

        Assert.AreEqual("live", await state.Load("source", () => "live", () => "empty", TestContext.CancellationToken));

        await Assert.ThrowsExactlyAsync<WorkbenchSimulatedException>(() => state.Load("another source", () => "live", () => "empty", TestContext.CancellationToken), "Each source fails on its own first load.");
    }

    [TestMethod]
    public async Task SwitchingTheScenario_Should_MakeAnErrorSourceFailAgain()
    {
        var state = In(WorkbenchDataState.Error);

        await Assert.ThrowsExactlyAsync<WorkbenchSimulatedException>(() => state.Load("source", () => "live", () => "empty", TestContext.CancellationToken));
        Assert.AreEqual("live", await state.Load("source", () => "live", () => "empty", TestContext.CancellationToken));

        state.Set(new() { Data = WorkbenchDataState.Error });

        await Assert.ThrowsExactlyAsync<WorkbenchSimulatedException>(() => state.Load("source", () => "live", () => "empty", TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task Loading_Should_NeverFinishUntilCancelled()
    {
        var state = In(WorkbenchDataState.Loading);
        using var cancellation = new CancellationTokenSource();

        var load = state.Load("source", () => "live", () => "empty", cancellation.Token);

        await Task.Delay(300, TestContext.CancellationToken);

        Assert.IsFalse(load.IsCompleted);
        Assert.IsFalse(state.IsSettled, "A load in flight keeps the view from counting as settled.");

        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => load);
        Assert.IsTrue(state.IsSettled);
    }

    [TestMethod]
    public async Task Slow_Should_ReturnTheLiveDataAfterMoreThanASecond()
    {
        var state = In(WorkbenchDataState.Slow);

        var stopwatch = Stopwatch.StartNew();
        var result = await state.Load("source", () => "live", () => "empty", TestContext.CancellationToken);

        Assert.AreEqual("live", result);
        Assert.IsGreaterThanOrEqualTo(1100, stopwatch.ElapsedMilliseconds);
    }

    [TestMethod]
    public void Mixed_Should_GiveTheSourcesErrorEmptyAndLiveInTurnTheSameOnEveryRun()
    {
        var state = In(WorkbenchDataState.Mixed);
        var again = In(WorkbenchDataState.Mixed);

        var resolved = PosWidgetSources.Select(state.ResolveState).ToArray();

        CollectionAssert.AreEqual(resolved, PosWidgetSources.Select(again.ResolveState).ToArray(), "A page asking for the same sources in the same order gets the same states.");
        CollectionAssert.AreEqual(resolved, PosWidgetSources.Select(state.ResolveState).ToArray(), "A source keeps its state when it loads again.");
        CollectionAssert.AreEqual(new[] { WorkbenchDataState.Error, WorkbenchDataState.Empty, WorkbenchDataState.Live }, resolved[..3]);

        foreach (var kind in new[] { WorkbenchDataState.Error, WorkbenchDataState.Empty, WorkbenchDataState.Live })
        {
            Assert.AreEqual(PosWidgetSources.Length / 3, resolved.Count(s => s == kind), $"The POS dashboard shows {kind} on a third of its widgets.");
        }
    }

    [TestMethod]
    [DataRow(WorkbenchDataState.Live)]
    [DataRow(WorkbenchDataState.Loading)]
    [DataRow(WorkbenchDataState.Empty)]
    [DataRow(WorkbenchDataState.Error)]
    [DataRow(WorkbenchDataState.Slow)]
    public void EveryStateButMixed_Should_ApplyToEverySource(WorkbenchDataState data)
    {
        var state = In(data);

        foreach (var source in PosWidgetSources)
        {
            Assert.AreEqual(data, state.ResolveState(source));
        }
    }

    [TestMethod]
    public async Task Track_Should_CountALoadThePageMakesItself()
    {
        var state = new WorkbenchScenarioState();
        var load = new TaskCompletionSource<int>();

        var tracked = state.Track(load.Task);

        Assert.IsFalse(state.IsSettled);

        load.SetResult(42);

        Assert.AreEqual(42, await tracked);
        Assert.IsTrue(state.IsSettled);
    }

    [TestMethod]
    public void Set_Should_RaiseChangedAndRecordActivity()
    {
        var state = new WorkbenchScenarioState();
        var before = state.LastActivity;
        var raised = 0;

        state.Changed += () => raised++;
        state.Set(new() { Theme = WorkbenchThemeMode.Dark });

        Assert.AreEqual(1, raised);
        Assert.AreEqual(WorkbenchThemeMode.Dark, state.Scenario.Theme);
        Assert.IsGreaterThanOrEqualTo(before, state.LastActivity);
    }

    /// <summary>The sources of the POS dashboard's widgets.</summary>
    private static readonly string[] PosWidgetSources =
    [
        "pos.net-sales", "pos.transactions", "pos.average-sale", "pos.refunds", "pos.sales-by-hour", "pos.payment-methods",
        "pos.top-products", "pos.low-stock", "pos.recent-orders", "pos.staff", "pos.daily-goal", "pos.peak-hours"
    ];
}
