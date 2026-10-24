namespace Bit.Blex.Tests;

/// <summary>The classic mistake, both ways: a list mutated in place, and one replaced.</summary>
[BlexStoreAttribute(Name = "basket")]
public partial class BasketStore
{
    [BlexStateAttribute] private List<string> _items = [];
    [BlexStateAttribute] private int _version;

    /// <summary>Wrong: nothing is assigned, so nothing hears about it.</summary>
    [BlexActionAttribute]
    private void OnAddInPlace(string item) => Items.Add(item);

    /// <summary>Right: a new list is assigned.</summary>
    [BlexActionAttribute]
    private void OnAdd(string item) => Items = [.. Items, item];

    /// <summary>Wrong, asynchronously.</summary>
    [BlexActionAttribute]
    private async Task OnAddInPlaceLater(string item)
    {
        await Task.Yield();
        Items.Add(item);
    }

    /// <summary>Mutates one field in place but assigns another, so the change is announced anyway.</summary>
    [BlexActionAttribute]
    private void OnAddAndBump(string item)
    {
        Items.Add(item);
        Version++;
    }

    [BlexActionAttribute]
    private void OnNothing()
    {
    }
}

/// <summary>
/// <see cref="BlexManager.DetectInPlaceMutations"/>: an action that changed state without assigning a
/// property -- which no subscriber, DevTools entry, undo step or storage write ever sees -- is reported.
/// </summary>
[TestClass]
public class InPlaceMutationTests
{
    private static (BasketStore Store, List<BlexError> Errors) Setup(bool detect = true)
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add, DetectInPlaceMutations = detect };
        var store = new BasketStore();
        manager.Register(store);
        return (store, errors);
    }

    [TestMethod]
    public void An_in_place_change_is_reported_with_the_fix()
    {
        var (store, errors) = Setup();

        store.AddInPlace("apple");

        var error = Assert.ContainsSingle(errors);
        Assert.AreEqual("mutation", error.Source);
        Assert.AreEqual("basket/AddInPlace", error.Detail);
        Assert.Contains("Items = [.. Items, item]", error.Exception.Message);
    }

    [TestMethod]
    public async Task An_async_action_is_checked_too()
    {
        var (store, errors) = Setup();

        await store.AddInPlaceLater("pear");

        Assert.ContainsSingle(errors);
    }

    [TestMethod]
    public void A_proper_assignment_and_an_action_that_changes_nothing_are_not_reported()
    {
        var (store, errors) = Setup();

        store.Add("apple");
        store.Nothing();
        store.AddAndBump("pear");   // announced through Version

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void It_is_off_by_default()
    {
        var (store, errors) = Setup(detect: false);

        store.AddInPlace("apple");

        Assert.IsEmpty(errors);
        Assert.IsFalse(new BlexManager().DetectInPlaceMutations);
    }
}

/// <summary><see cref="BlexManager.WhenAsync"/>: waiting for a condition on the state, as a workflow step.</summary>
[TestClass]
public class WhenAsyncTests
{
    [TestMethod]
    public async Task It_completes_once_a_change_makes_the_condition_true()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var when = manager.WhenAsync(() => counter.Count >= 2);
        counter.Increment();
        Assert.IsFalse(when.IsCompleted);

        counter.Increment();
        await when.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsFalse(manager.HasObservers, "the waiter lets go of the pipeline once done");
    }

    [TestMethod]
    public void It_completes_at_once_when_the_condition_already_holds()
        => Assert.IsTrue(new BlexManager().WhenAsync(() => true).IsCompletedSuccessfully);

    [TestMethod]
    public async Task It_sees_an_effect_finish()
    {
        var manager = new BlexManager();
        var store = new GatedStore();
        manager.Register(store);
        var run = store.Slow();

        var settled = manager.WhenAsync(() => !store.SlowIsLoading);
        store.Gate.SetResult();

        await settled.WaitAsync(TimeSpan.FromSeconds(5));
        await run;
    }

    [TestMethod]
    public async Task It_sees_a_restore()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);
        counter.Add(5);
        var snapshot = manager.CaptureGlobalState();
        counter.Reset();

        var restored = manager.WhenAsync(() => counter.Count == 5);
        manager.RestoreGlobalState(snapshot);

        await restored.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task It_is_cancelled_with_its_token_and_lets_go()
    {
        var manager = new BlexManager();
        manager.Register(new CounterStore());
        using var cts = new CancellationTokenSource();

        var when = manager.WhenAsync(() => false, cts.Token);
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => when);
        Assert.IsFalse(manager.HasObservers);
    }

    [TestMethod]
    public async Task A_throwing_condition_faults_the_task()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var when = manager.WhenAsync(() => counter.Count > 0 ? throw new InvalidOperationException("bad") : false);
        counter.Increment();

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => when);
        Assert.AreEqual("bad", error.Message);
    }
}
