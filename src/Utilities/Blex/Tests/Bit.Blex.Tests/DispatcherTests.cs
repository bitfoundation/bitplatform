using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// <see cref="BlexManager.Dispatcher"/>: stores are not synchronized, so every change has to happen on
/// the dispatcher their state belongs to. <see cref="BlexManager.InvokeAsync(System.Action)"/> gets code
/// that is elsewhere there, and a change made elsewhere anyway is reported rather than left to corrupt
/// state silently.
/// </summary>
[TestClass]
public class DispatcherTests
{
    /// <summary>A dispatcher whose context is "inside" only while it runs a work item.</summary>
    private sealed class FakeDispatcher : IBlexDispatcher
    {
        public bool Inside { get; set; }

        public int Invocations { get; private set; }

        public bool CheckAccess() => Inside;

        public async Task InvokeAsync(Func<Task> workItem)
        {
            Invocations++;
            var outer = Inside;
            Inside = true;
            try
            {
                await workItem();
            }
            finally
            {
                Inside = outer;
            }
        }
    }

    private static (BlexManager Manager, CounterStore Store, FakeDispatcher Dispatcher, List<BlexError> Errors) Setup()
    {
        var errors = new List<BlexError>();
        var dispatcher = new FakeDispatcher();
        var manager = new BlexManager { OnError = errors.Add, Dispatcher = dispatcher };
        var store = new CounterStore();
        manager.Register(store);
        return (manager, store, dispatcher, errors);
    }

    [TestMethod]
    public async Task InvokeAsync_MarshalsToTheDispatcherFromOutsideIt()
    {
        var (manager, store, dispatcher, errors) = Setup();

        await manager.InvokeAsync(store.Increment);

        Assert.AreEqual(1, dispatcher.Invocations);
        Assert.AreEqual(1, store.Count);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task InvokeAsync_RunsInlineWhenAlreadyOnTheDispatcher()
    {
        var (manager, store, dispatcher, errors) = Setup();
        dispatcher.Inside = true;

        var task = manager.InvokeAsync(store.Increment);

        Assert.IsTrue(task.IsCompletedSuccessfully, "work already on the dispatcher must not be queued behind itself");
        Assert.AreEqual(1, store.Count);
        Assert.AreEqual(0, dispatcher.Invocations);
        await task;
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task InvokeAsync_RunsInlineWithoutADispatcher()
    {
        var manager = new BlexManager();
        var store = new CounterStore();
        manager.Register(store);

        await manager.InvokeAsync(store.Increment);
        await manager.InvokeAsync(() => store.LoadData());

        Assert.AreEqual(42, store.Count);
    }

    [TestMethod]
    public async Task InvokeAsync_FaultsTheTaskWhenTheWorkThrows()
    {
        var manager = new BlexManager();

        var task = manager.InvokeAsync(() => throw new InvalidOperationException("boom"));

        Assert.IsTrue(task.IsFaulted, "the inline path must fault the task, as the marshalled one does");
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => task);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => manager.InvokeAsync(() => Task.FromException(new InvalidOperationException("async boom"))));
    }

    [TestMethod]
    public async Task InvokeAsync_RunsAsyncWorkOnTheDispatcher()
    {
        var (manager, store, dispatcher, errors) = Setup();

        await manager.InvokeAsync(async () =>
        {
            await Task.Yield();
            store.Increment();
        });

        Assert.AreEqual(1, dispatcher.Invocations);
        Assert.AreEqual(1, store.Count);
        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void AChangeOutsideTheDispatcher_IsReportedAndStillApplied()
    {
        var (_, store, _, errors) = Setup();

        store.Increment();

        Assert.AreEqual(1, store.Count, "the change has already happened by the time it can be noticed");
        var error = Assert.ContainsSingle(errors);
        Assert.AreEqual("dispatcher", error.Source);
        Assert.AreEqual("counter", error.Detail);
        Assert.Contains("InvokeAsync", error.Exception.Message);
    }

    [TestMethod]
    public void AChangeOnTheDispatcher_IsNotReported()
    {
        var (_, store, dispatcher, errors) = Setup();
        dispatcher.Inside = true;

        store.Increment();
        store.Label = "direct";

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public async Task AnEffectsLifecycleOutsideTheDispatcher_IsReported()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add, Dispatcher = new FakeDispatcher() };
        var store = new FailingStore();
        manager.Register(store);

        await store.Load("x");   // IsLoading flips and Error is set, all outside the dispatcher

        Assert.IsNotEmpty(errors);
        Assert.IsTrue(errors.TrueForAll(e => e.Source == "dispatcher"));
    }

    [TestMethod]
    public void TheReportCanBeTurnedOff()
    {
        var (manager, store, _, errors) = Setup();
        manager.ReportOffDispatcherChanges = false;

        store.Increment();

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void NothingIsReportedWithoutABoundDispatcher()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new CounterStore();
        manager.Register(store);

        store.Increment();

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void TheOptionReachesTheManager()
    {
        var services = new ServiceCollection();
        services.AddBitBlexServices(o => o.ReportOffDispatcherChanges = false);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsFalse(scope.ServiceProvider.GetRequiredService<BlexManager>().ReportOffDispatcherChanges);
        Assert.IsTrue(new BlexManager().ReportOffDispatcherChanges);
    }
}
