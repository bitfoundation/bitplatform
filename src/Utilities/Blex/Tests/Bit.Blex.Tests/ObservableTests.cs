namespace Bit.Blex.Tests;

/// <summary>The <see cref="IObservable{T}"/> views, for code that composes changes with Rx.</summary>
[TestClass]
public class ObservableTests
{
    private sealed class Recorder<T> : IObserver<T>
    {
        public List<T> Values { get; } = [];

        public Exception? Error { get; private set; }

        public bool Completed { get; private set; }

        public void OnNext(T value) => Values.Add(value);

        public void OnError(Exception error) => Error = error;

        public void OnCompleted() => Completed = true;
    }

    [TestMethod]
    public void Observe_delivers_the_current_value_and_then_each_change()
    {
        var store = new CounterStore();
        var recorder = new Recorder<int>();

        using (store.Observe(() => store.Count).Subscribe(recorder))
        {
            store.Increment();
            store.Label = "unrelated";   // the projection did not change: nothing delivered
            store.Add(2);
        }

        store.Increment();   // after disposal

        CollectionAssert.AreEqual(new[] { 0, 1, 3 }, recorder.Values);
        Assert.IsNull(recorder.Error);
    }

    [TestMethod]
    public void Observe_honors_a_comparer()
    {
        var store = new TodoStore();
        var recorder = new Recorder<IEnumerable<string>?>();

        using var sub = store.Observe<IEnumerable<string>?>(() => store.Items.ToList(), BlexComparers.Sequence<string>()).Subscribe(recorder);
        store.Items = [.. store.Items];   // same contents, new list
        store.AddItem("milk");

        Assert.HasCount(2, recorder.Values);
    }

    [TestMethod]
    public void A_selector_that_throws_ends_the_sequence_with_OnError()
    {
        var store = new CounterStore();
        var recorder = new Recorder<int>();

        using var sub = store.Observe(() => store.Count < 2 ? store.Count : throw new InvalidOperationException("too high")).Subscribe(recorder);
        store.Add(5);
        store.Add(-5);   // unsubscribed by the failure: never delivered

        CollectionAssert.AreEqual(new[] { 0 }, recorder.Values);
        Assert.AreEqual("too high", recorder.Error?.Message);
    }

    [TestMethod]
    public void A_selector_that_throws_on_subscribe_reports_rather_than_throws()
    {
        var store = new CounterStore();
        var recorder = new Recorder<int>();

        using var sub = store.Observe<int>(() => throw new InvalidOperationException("nope")).Subscribe(recorder);

        Assert.IsEmpty(recorder.Values);
        Assert.AreEqual("nope", recorder.Error?.Message);
    }

    [TestMethod]
    public void ObserveActions_streams_every_dispatched_action()
    {
        var manager = new BlexManager();
        var orders = new OrderBookStore();
        manager.Register(orders);
        var recorder = new Recorder<BlexActionContext>();

        using (manager.ObserveActions().Subscribe(recorder))
        {
            orders.AddUnits(1);
            orders.Place("pen", 1);
        }

        orders.Clear();

        CollectionAssert.AreEqual(new[] { "AddUnits", "Place" }, recorder.Values.Select(c => c.ActionName).ToArray());
        Assert.IsTrue(OrderBookStore.Actions.Place.Matches(recorder.Values[1]));
    }
}
