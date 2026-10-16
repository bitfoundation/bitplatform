namespace Bit.Bmotion.Tests.Models;

/// <summary>
/// Tests for <see cref="BmValue{T}"/> (motion.dev's <c>MotionValue</c>): async subscriber
/// notification, unsubscription, disposal ending a derived chain, and the general
/// <see cref="BmValue{T}.Transform{TOut}(Func{T, TOut})"/> projection.
/// </summary>
[TestClass]
public class BmValueTests
{
    [TestMethod]
    public async Task SetAsync_AwaitsEverySubscriberInOrder()
    {
        var value = Bm.Value(0.0);
        var log = new List<string>();
        // Explicitly async (Func<T, Task>) subscribers, not async-void Actions SetAsync couldn't await.
        value.Subscribe((Func<double, Task>)(async v =>
        {
            await Task.Yield();
            log.Add($"first {v}");
        }));
        value.Subscribe((Func<double, Task>)(async v =>
        {
            await Task.Delay(1);
            log.Add($"second {v}");
        }));

        await value.SetAsync(5);

        // Both asynchronous subscribers have finished by the time SetAsync returns, in subscription order.
        CollectionAssert.AreEqual(new[] { "first 5", "second 5" }, log);
        Assert.AreEqual(5, value.Value);
    }

    [TestMethod]
    public async Task SetAsync_AFailingSubscriber_DoesNotStopTheOthers()
    {
        var value = Bm.Value("a");
        var seen = new List<string>();
        value.Subscribe((Func<string, Task>)(_ => throw new InvalidOperationException("sync throw")));
        value.Subscribe((Func<string, Task>)(async _ => { await Task.Yield(); throw new InvalidOperationException("async throw"); }));
        value.Subscribe(v => seen.Add(v));

        await value.SetAsync("b");

        CollectionAssert.AreEqual(new[] { "b" }, seen);
    }

    [TestMethod]
    public void Subscribe_DisposingTheSubscription_Unsubscribes()
    {
        var value = Bm.Value(0);
        var seen = new List<int>();
        var subscription = value.Subscribe(v => seen.Add(v));

        value.SetSync(1);
        subscription.Dispose();
        value.SetSync(2);
        subscription.Dispose(); // idempotent

        CollectionAssert.AreEqual(new[] { 1 }, seen);
    }

    [TestMethod]
    public void Transform_ProjectsIntoAnotherType_AndFollowsTheSource()
    {
        var value = Bm.Value(10.0);
        var css = value.Transform(v => $"{v}px");

        Assert.AreEqual("10px", css.Value, "the projection is evaluated up front");

        value.SetSync(25);
        Assert.AreEqual("25px", css.Value);
    }

    [TestMethod]
    public void Transform_Chains_AndDisposingTheMiddleEndsTheChain()
    {
        var source = Bm.Value(1.0);
        var doubled = source.Transform(v => v * 2);
        var label = doubled.Transform(v => $"x{v}");

        source.SetSync(3);
        Assert.AreEqual("x6", label.Value);

        doubled.Dispose();
        source.SetSync(4);

        Assert.AreEqual(6, doubled.Value, "a disposed derived value no longer follows its source");
        Assert.AreEqual("x6", label.Value, "and nothing downstream of it does either");
    }

    [TestMethod]
    public void Dispose_TheSource_DropsItsSubscribers()
    {
        var source = Bm.Value(1.0);
        var derived = source.Transform(v => v + 1);
        var seen = new List<double>();
        source.Subscribe(v => seen.Add(v));

        source.Dispose();
        source.SetSync(5);

        Assert.AreEqual(5, source.Value, "the value itself still holds what it was set to");
        Assert.AreEqual(2, derived.Value, "but no longer notifies anyone");
        Assert.AreEqual(0, seen.Count);
    }

    [TestMethod]
    public void Value_Setter_NotifiesLikeSetSync()
    {
        var value = Bm.Value(0.0);
        double? seen = null;
        value.Subscribe(v => seen = v);

        value.Value = 7;

        Assert.AreEqual(7, seen);
    }
}
