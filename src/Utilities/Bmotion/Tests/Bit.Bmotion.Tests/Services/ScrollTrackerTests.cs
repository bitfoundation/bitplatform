using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Services;

/// <summary>
/// Tests for <see cref="BmotionScrollTracker"/> (motion.dev's <c>useScroll</c>): the bridge's
/// <c>OnScroll</c> callback updates the progress properties and their motion values, and disposal
/// releases every JS subscription and the reference JS held.
/// </summary>
[TestClass]
public class ScrollTrackerTests
{
    // What the bridge would report a third of the way down a 3000px page in a 1000px viewport.
    private static BmScrollInfo Info(double? targetProgress = null) => new()
    {
        ScrollX = 0,
        ScrollY = 666,
        ProgressX = 0,
        ProgressY = 1.0 / 3,
        ScrollHeight = 3000,
        ClientHeight = 1000,
        TargetProgress = targetProgress,
    };

    // The tracker as the bridge sees it: the object behind the reference it was handed.
    private static BmotionScrollTracker FromBridge(FakeBmotionInterop interop, string key)
        => (BmotionScrollTracker)interop.ScrollRefs[key];

    [TestMethod]
    public async Task OnScroll_UpdatesTheProgressProperties_AndCallsTheObserver()
    {
        var interop = new FakeBmotionInterop();
        await using var tracker = new BmotionScrollTracker(interop);
        BmScrollInfo? observed = null;

        await tracker.ObserveAsync(info => observed = info);
        var key = (string)interop.ArgsOf("observeScroll").Single()[2]!;

        var info = Info(targetProgress: 0.25);
        await FromBridge(interop, key).OnScroll(info);

        Assert.AreSame(info, observed);
        Assert.AreEqual(1.0 / 3, tracker.ProgressY, 1e-12);
        Assert.AreEqual(666, tracker.ScrollY);
        Assert.AreEqual(0, tracker.ProgressX);
        Assert.AreEqual(0.25, tracker.TargetProgress);
    }

    [TestMethod]
    public async Task OnScroll_PushesIntoTheMaterializedMotionValues()
    {
        var interop = new FakeBmotionInterop();
        await using var tracker = new BmotionScrollTracker(interop);
        // Touch the values first: they are created lazily and only update once they exist.
        var progressY = tracker.ProgressYValue;
        var scrollY = tracker.ScrollYValue;
        var target = tracker.TargetProgressValue;
        var derived = progressY.Transform([0, 1], [0, 100]);
        await tracker.ObserveAsync(_ => { });

        await FromBridge(interop, "scroll-key-1").OnScroll(Info(targetProgress: 0.8));

        Assert.AreEqual(1.0 / 3, progressY.Value, 1e-12);
        Assert.AreEqual(666, scrollY.Value);
        Assert.AreEqual(0.8, target.Value);
        Assert.AreEqual(100.0 / 3, derived.Value, 1e-9, "values derived from a scroll value follow it");

        // No tracked target any more: the motion value falls back to 0 rather than keeping 0.8.
        await FromBridge(interop, "scroll-key-1").OnScroll(Info(targetProgress: null));
        Assert.AreEqual(0, target.Value);
        Assert.IsNull(tracker.TargetProgress);
    }

    [TestMethod]
    public async Task OnScroll_AThrowingObserver_DoesNotFaultTheBridgeCallback()
    {
        var interop = new FakeBmotionInterop();
        await using var tracker = new BmotionScrollTracker(interop);
        await tracker.ObserveAsync((Func<BmScrollInfo, Task>)(_ => throw new InvalidOperationException("boom")));

        await FromBridge(interop, "scroll-key-1").OnScroll(Info());

        Assert.AreEqual(666, tracker.ScrollY, "state is still updated");
    }

    [TestMethod]
    public async Task ObserveAgain_ReplacesTheEarlierSubscription()
    {
        var interop = new FakeBmotionInterop();
        await using var tracker = new BmotionScrollTracker(interop);

        await tracker.ObserveAsync(_ => { });
        await tracker.ObserveAsync(new BmScrollOptions { ContainerId = "list" }, _ => { });

        CollectionAssert.AreEqual(new[] { "scroll-key-1" }, interop.ArgsOf("unobserveScroll").Select(a => (string)a[0]!).ToArray());
        Assert.AreEqual("list", interop.ArgsOf("observeScroll")[1][0], "the new subscription watches the container");
    }

    [TestMethod]
    public async Task DisposeAsync_UnobservesEveryKey_AndReleasesTheReferenceJsHeld()
    {
        var interop = new FakeBmotionInterop();
        var tracker = new BmotionScrollTracker(interop);
        await tracker.ObserveAsync(_ => { });
        await tracker.ObserveAsync("list", _ => { });

        await tracker.DisposeAsync();

        var observed = interop.ArgsOf("observeScroll").Select(a => (string)a[2]!).ToArray();
        var unobserved = interop.ArgsOf("unobserveScroll").Select(a => (string)a[0]!).ToArray();
        CollectionAssert.AreEquivalent(observed, unobserved, "every subscription ever opened must be closed");
        Assert.IsTrue(FakeBmotionInterop.IsDisposed(interop.ScrollReferences["scroll-key-2"]),
            "a scroll event arriving after disposal must not reach the tracker");

        await tracker.DisposeAsync(); // idempotent
        Assert.AreEqual(unobserved.Length, interop.CountOf("unobserveScroll"));
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => tracker.ObserveAsync(_ => { }));
    }

    [TestMethod]
    public void IsRegisteredTransient_SoEveryConsumerOwnsItsOwnTracker()
    {
        using var ctx = new BmotionTestContext();
        var first = ctx.Services.GetService(typeof(BmotionScrollTracker));
        var second = ctx.Services.GetService(typeof(BmotionScrollTracker));

        Assert.IsNotNull(first);
        Assert.AreNotSame(first, second);
    }
}
