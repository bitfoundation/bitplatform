namespace Bit.Bmotion.Tests.Models;

/// <summary><see cref="Bm.Combine"/> and <see cref="BmValue{T}.Previous"/>.</summary>
[TestClass]
public class MotionValueCombineTests
{
    [TestMethod]
    public void ACombinedValue_FollowsEveryInput()
    {
        var x = Bm.Value(3.0);
        var y = Bm.Value(4.0);
        using var distance = Bm.Combine(() => Math.Sqrt((x.Value * x.Value) + (y.Value * y.Value)), x, y);

        Assert.AreEqual(5, distance.Value, 1e-12);
        x.Value = 6;
        y.Value = 8;
        Assert.AreEqual(10, distance.Value, 1e-12);
    }

    [TestMethod]
    public void ACombinedValue_NotifiesItsOwnSubscribers_AndChains()
    {
        var a = Bm.Value(1.0);
        var b = Bm.Value(2.0);
        using var sum = Bm.Combine(() => a.Value + b.Value, a, b);
        using var mapped = sum.Transform([0, 10], [0, 100]);
        var seen = new List<double>();
        using var _ = sum.Subscribe(seen.Add);

        a.Value = 4;
        b.Value = 6;

        CollectionAssert.AreEqual(new[] { 6.0, 10.0 }, seen);
        Assert.AreEqual(100, mapped.Value, 1e-12);
    }

    [TestMethod]
    public void ACombinedValue_TracksItsOwnVelocity()
    {
        var a = Bm.Value(0.0);
        using var doubled = Bm.Combine(() => a.Value * 2, a);
        long now = 1000;
        doubled.TimeSource = () => now;

        a.Value = 10;   // first change: no interval yet
        now += 100;
        a.Value = 20;   // doubled 20 → 40 in 100 ms

        Assert.AreEqual(200, doubled.GetVelocity(), 1e-9);
    }

    [TestMethod]
    public void DisposingACombinedValue_DetachesItFromItsInputs()
    {
        var a = Bm.Value(1.0);
        var combined = Bm.Combine(() => a.Value * 10, a);
        combined.Dispose();

        a.Value = 5;

        Assert.AreEqual(10, combined.Value, "a disposed value no longer follows");
    }

    [TestMethod]
    public void ANullInput_IsRejected()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Bm.Combine(() => 0, Bm.Value(0.0), null!));
    }

    [TestMethod]
    public void Previous_IsTheValueBeforeTheLatestChange()
    {
        var v = Bm.Value(1.0);
        Assert.AreEqual(1, v.Previous, "the initial value until the first change");

        v.Value = 2;
        Assert.AreEqual(1, v.Previous);
        v.Jump(5);
        Assert.AreEqual(2, v.Previous);
        _ = v.SetAsync(7);
        Assert.AreEqual(5, v.Previous);
    }

    [TestMethod]
    public void Previous_IsReadableInsideASubscriber()
    {
        var v = Bm.Value(10.0);
        bool? movedUp = null;
        using var _ = v.Subscribe(now => movedUp = now > v.Previous);

        v.Value = 4;
        Assert.IsFalse(movedUp);
        v.Value = 12;
        Assert.IsTrue(movedUp);
    }

    [TestMethod]
    public void NonDoubleNumericValues_StillTrackVelocity()
    {
        var v = Bm.Value(0);
        long now = 0;
        v.TimeSource = () => now;
        v.Value = 1;
        now += 50;
        v.Value = 11;

        Assert.AreEqual(200, v.GetVelocity(), 1e-9);
    }
}
