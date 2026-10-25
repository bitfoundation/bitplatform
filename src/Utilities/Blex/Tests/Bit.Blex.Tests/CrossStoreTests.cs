using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class CrossStoreTests
{
    [TestMethod]
    public void GetStore_ReturnsRegisteredStoreByType()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        Assert.AreSame(counter, manager.GetStore<CounterStore>());
        Assert.IsNull(manager.GetStore<TodoStore>());
    }

    [TestMethod]
    public void SubscribeTo_FiresOnlyForMatchingStoreType()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var todo = new TodoStore();
        manager.Register(counter);
        manager.Register(todo);

        var hits = 0;
        using var sub = manager.SubscribeTo<CounterStore>(_ => hits++);

        counter.Increment();
        todo.AddItem("x"); // different store, should not fire

        Assert.AreEqual(1, hits);
    }

    [TestMethod]
    public void SubscribeToAction_MatchesBareAndQualifiedNames()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var bare = 0;
        var qualified = 0;
        using var s1 = manager.SubscribeToAction("Increment", _ => bare++);
        using var s2 = manager.SubscribeToAction("counter/Increment", _ => qualified++);

        counter.Increment();

        Assert.AreEqual(1, bare);
        Assert.AreEqual(1, qualified);
    }

    [TestMethod]
    public void CrossStore_OneStoreReactsToAnother()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var todo = new TodoStore();
        manager.Register(counter);
        manager.Register(todo);

        // Every 3rd increment adds a todo.
        using var sub = manager.SubscribeTo<CounterStore>(ctx =>
        {
            if (((CounterStore)ctx.Store).Count % 3 == 0)
                todo.AddItem($"milestone {((CounterStore)ctx.Store).Count}");
        });

        counter.Increment(); // 1
        counter.Increment(); // 2
        counter.Increment(); // 3 -> adds todo

        Assert.ContainsSingle(todo.Items);
        Assert.AreEqual("milestone 3", todo.Items[0]);
    }

    [TestMethod]
    public void Subscribe_Dispose_StopsReacting()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var hits = 0;
        var sub = manager.Subscribe(_ => hits++);
        counter.Increment();
        Assert.AreEqual(1, hits);

        sub.Dispose();
        counter.Increment();
        Assert.AreEqual(1, hits);
    }

    [TestMethod]
    public async Task SubscribeAsync_RunsAsyncReactor()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        var data = new DataStore();
        manager.Register(counter);
        manager.Register(data);

        var tcs = new TaskCompletionSource();
        using var sub = manager.SubscribeAsync(async _ =>
        {
            await data.Load("reacted");
            tcs.TrySetResult();
        });

        counter.Increment();
        await tcs.Task;

        Assert.AreEqual("reacted", data.Value);
    }

    [TestMethod]
    public void SubscriberException_DoesNotBreakDispatch()
    {
        var manager = new BlexManager();
        var counter = new CounterStore();
        manager.Register(counter);

        var second = 0;
        using var bad = manager.Subscribe(_ => throw new System.Exception("nope"));
        using var good = manager.Subscribe(_ => second++);

        counter.Increment(); // must not throw

        Assert.AreEqual(1, second);
    }
}
