using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Tests;

/// <summary>Effects and actions whose runs overlap, parked on gates the test opens.</summary>
[BlexStoreAttribute(Name = "overlap")]
public partial class OverlapStore
{
    [BlexStateAttribute] private int _count;

    public Queue<TaskCompletionSource> Gates { get; } = new();

    private Task NextGate() => Gates.Count > 0 ? Gates.Dequeue().Task : Task.CompletedTask;

    public TaskCompletionSource Gate()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Gates.Enqueue(gate);
        return gate;
    }

    /// <summary>Parallel: every run overlaps the others freely.</summary>
    [BlexEffectAttribute]
    private async Task OnRun(int amount)
    {
        await NextGate();
        Count += amount;
    }

    /// <summary>Changes state on both sides of an await.</summary>
    [BlexEffectAttribute]
    private async Task OnStepAround()
    {
        Count += 1;
        await NextGate();
        Count += 1;
    }

    [BlexActionAttribute] private void OnBump() => Count += 100;

    /// <summary>An action of its own body, after an await: part of this one.</summary>
    [BlexEffectAttribute]
    private async Task OnBumpInside()
    {
        await NextGate();
        Bump();
        Count += 1;
    }

    /// <summary>Starts an effect and does not wait for it: the effect outlives this action.</summary>
    [BlexActionAttribute]
    private void OnKickOff()
    {
        Count += 1000;
        _ = Run(5);
    }
}

[BlexStoreAttribute(Name = "strictOverlap", EnforceActions = true)]
public partial class StrictOverlapStore
{
    [BlexStateAttribute] private int _value;

    public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    [BlexEffectAttribute]
    private async Task OnLoad()
    {
        await Gate.Task;
        Value = 1;
    }

    /// <summary>The store's own code changing state outside any action.</summary>
    public void AssignOutsideAnAction() => Value = 5;
}

/// <summary>
/// An action records what its own flow changed. Actions that overlap -- two runs of a Parallel effect, a click
/// handled while an effect awaits -- are each an action of their own in the log, middleware, DevTools and undo,
/// rather than folded into whichever happened to end last; an action dispatched from inside another one's body
/// is part of it.
/// </summary>
[TestClass]
public class OverlappingActionTests
{
    private static (BlexManager Manager, OverlapStore Store, List<BlexActionContext> Recorded) Setup()
    {
        var manager = new BlexManager();
        var store = new OverlapStore();
        manager.Register(store);
        var recorded = new List<BlexActionContext>();
        manager.Subscribe(recorded.Add);
        return (manager, store, recorded);
    }

    [TestMethod]
    public async Task Two_overlapping_runs_are_two_actions()
    {
        var (_, store, recorded) = Setup();
        var first = store.Gate();
        var second = store.Gate();

        var a = store.Run(1);
        var b = store.Run(2);
        second.SetResult();
        await b;
        first.SetResult();
        await a;

        Assert.AreEqual(3, store.Count);
        Assert.AreSequenceEqual(["overlap/Run", "overlap/Run"], recorded.Select(c => c.QualifiedName).ToArray());
        Assert.AreSequenceEqual([2, 1], recorded.Select(c => (int)c.Args.Single().Value!).ToArray(), "each with its own arguments, in the order they ended");
    }

    [TestMethod]
    public async Task An_action_handled_while_an_effect_awaits_is_recorded_on_its_own_and_first()
    {
        var (manager, store, recorded) = Setup();
        var history = new BlexHistory(manager);
        history.Start();
        var gate = store.Gate();

        var run = store.StepAround();   // +1, then parks
        store.Bump();                   // +100, from elsewhere

        Assert.AreSequenceEqual(["overlap/Bump"], recorded.Select(c => c.QualifiedName).ToArray());

        gate.SetResult();
        await run;

        Assert.AreSequenceEqual(["overlap/Bump", "overlap/StepAround"], recorded.Select(c => c.QualifiedName).ToArray());
        Assert.AreEqual(102, store.Count);
        Assert.AreEqual(2, history.UndoCount, "two actions, two steps");
    }

    [TestMethod]
    public async Task An_action_dispatched_from_inside_an_async_body_is_part_of_it()
    {
        var (_, store, recorded) = Setup();
        var gate = store.Gate();

        var run = store.BumpInside();
        gate.SetResult();
        await run;

        Assert.AreEqual(101, store.Count);
        var action = Assert.ContainsSingle(recorded);
        Assert.AreEqual("overlap/BumpInside", action.QualifiedName);
    }

    [TestMethod]
    public async Task An_effect_started_and_not_awaited_by_an_action_records_what_it_changes_after_that_action()
    {
        var (_, store, recorded) = Setup();
        var gate = store.Gate();

        store.KickOff();
        Assert.AreSequenceEqual(["overlap/KickOff"], recorded.Select(c => c.QualifiedName).ToArray(), "the action records as it ends");
        Assert.AreEqual(1000, store.Count);

        gate.SetResult();
        await Task.Delay(50);   // the effect resumes on its own

        Assert.AreSequenceEqual(["overlap/KickOff", "overlap/Run"], recorded.Select(c => c.QualifiedName).ToArray());
        Assert.IsTrue(recorded[1].IsNested, "started from inside KickOff: a replay of KickOff runs it again");
        Assert.AreEqual(1005, store.Count);
    }

    [TestMethod]
    public async Task An_assignment_made_elsewhere_while_an_effect_awaits_is_its_own_vetoable_set()
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices(options => options.UseFilter(ctx => ctx.ActionName != "Set Count"));
        services.AddBitBlexStore<OverlapStore>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<OverlapStore>();
        var gate = store.Gate();

        var run = store.Run(1);
        store.Count = 50;   // not the effect's doing, so not waved through as part of it

        Assert.AreEqual(0, store.Count);
        gate.SetResult();
        await run;
        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public async Task EnforceActions_rejects_an_assignment_outside_an_action_even_while_an_effect_awaits()
    {
        var manager = new BlexManager();
        var store = new StrictOverlapStore();
        manager.Register(store);

        var load = store.Load();
        Assert.ThrowsExactly<InvalidOperationException>(store.AssignOutsideAnAction);

        store.Gate.SetResult();
        await load;
        Assert.AreEqual(1, store.Value, "the effect's own assignment, after its await, is still inside it");
    }
}
