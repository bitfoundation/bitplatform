using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Services;

/// <summary>Tests for the View Transitions API wrapper (plan item 3.3).</summary>
[TestClass]
public class ViewTransitionTests
{
    [TestMethod]
    public async Task StartAsync_RunsUpdate_AndReportsNativeSupport()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true };
        await using var vt = new BmotionViewTransition(interop);

        var ran = false;
        var usedNative = await vt.StartAsync(() => { ran = true; return Task.CompletedTask; });

        Assert.IsTrue(ran, "the DOM-update callback must run");
        Assert.IsTrue(usedNative);
        Assert.IsTrue(interop.WasCalled("startViewTransition"));
    }

    [TestMethod]
    public async Task StartAsync_Unsupported_StillRunsUpdate_ReturnsFalse()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = false };
        await using var vt = new BmotionViewTransition(interop);

        var ran = false;
        var usedNative = await vt.StartAsync(() => ran = true);

        Assert.IsTrue(ran, "the callback must run even without native support");
        Assert.IsFalse(usedNative);
    }

    // ── Reduced motion and types ──────────────────────────────────────────────

    private static BmotionViewTransition WithPolicy(FakeBmotionInterop interop, BmReducedMotionMode mode)
        => new(interop, new BmotionAnimationEngine(interop), new BitBmotionOptions { ReducedMotion = mode });

    [TestMethod]
    public async Task UnderTheUserPolicy_AnOsPreferenceForLessMotion_SkipsTheTransition()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true, PrefersReducedMotion = true };
        await using var vt = WithPolicy(interop, BmReducedMotionMode.User);

        var ran = false;
        var usedNative = await vt.StartAsync(() => ran = true);

        Assert.IsTrue(ran, "the update still runs - only the animation is skipped");
        Assert.IsFalse(usedNative);
        Assert.IsFalse(interop.WasCalled("startViewTransition"));
    }

    [TestMethod]
    public async Task UnderTheUserPolicy_WithoutThePreference_TheTransitionPlays()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true, PrefersReducedMotion = false };
        await using var vt = WithPolicy(interop, BmReducedMotionMode.User);

        Assert.IsTrue(await vt.StartAsync(() => { }));
    }

    [TestMethod]
    public async Task TheBackCompatDefault_IgnoresTheOsPreference()
    {
        // A service sits outside every <BmotionConfig>, and IgnoreUnlessConfigured only honours the OS inside one.
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true, PrefersReducedMotion = true };
        await using var vt = WithPolicy(interop, BmReducedMotionMode.IgnoreUnlessConfigured);

        Assert.IsTrue(await vt.StartAsync(() => { }));
    }

    [TestMethod]
    public async Task APerCallOverride_WinsOverThePolicy()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true };
        await using var vt = WithPolicy(interop, BmReducedMotionMode.Always);

        Assert.IsFalse(await vt.StartAsync(() => { }), "Always reduces");
        Assert.IsTrue(await vt.StartAsync(() => { }, new BmViewTransitionOptions { ReduceMotion = false }),
            "a transition whose stylesheet honours the preference itself can opt back in");
        Assert.IsFalse(await WithPolicy(interop, BmReducedMotionMode.Never).StartAsync(() => { }, new BmViewTransitionOptions { ReduceMotion = true }));
    }

    [TestMethod]
    public async Task Types_ReachTheBrowser()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true };
        await using var vt = new BmotionViewTransition(interop);

        await vt.StartAsync(() => { }, new BmViewTransitionOptions { Types = ["forward", "slide"] });

        CollectionAssert.AreEqual(new[] { "forward", "slide" }, interop.ViewTransitionTypes);
    }

    [TestMethod]
    public async Task StartAsync_NullUpdate_Throws()
    {
        var interop = new FakeBmotionInterop();
        await using var vt = new BmotionViewTransition(interop);
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await vt.StartAsync((Func<Task>)null!));
    }

    [TestMethod]
    public async Task StartAsync_WhileInProgress_Throws()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true };
        await using var vt = new BmotionViewTransition(interop);

        // A re-entrant StartAsync (from inside the running update callback) must be rejected rather
        // than clobber the in-flight _pending callback.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await vt.StartAsync(async () => await vt.StartAsync(() => Task.CompletedTask)));
    }

    [TestMethod]
    public async Task StartAsync_AThrowingUpdate_PropagatesToTheCaller_AndReleasesTheGuard()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true };
        await using var vt = new BmotionViewTransition(interop);

        // The DOM update failing is the caller's error to see - swallowing it would leave the page
        // half-updated with no signal.
        var thrown = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            await vt.StartAsync((Func<Task>)(() => throw new InvalidOperationException("update failed"))));
        Assert.AreEqual("update failed", thrown.Message);

        // And the in-progress guard must not stay latched by the failure.
        var ran = false;
        await vt.StartAsync(() => ran = true);
        Assert.IsTrue(ran);
    }

    [TestMethod]
    public async Task StartAsync_HandsJsACallbackItCanActuallyInvoke()
    {
        var interop = new FakeBmotionInterop();
        await using var vt = new BmotionViewTransition(interop);

        await vt.StartAsync(() => { });

        // The bridge invokes the callback by its [JSInvokable] identifier through the reference it
        // was given; the fake resolves it exactly that way, so reaching here proves the pairing.
        Assert.AreEqual("RunUpdateAsync", interop.ArgsOf("startViewTransition").Single()[0]);
        Assert.AreSame(vt, ((Microsoft.JSInterop.DotNetObjectReference<BmotionViewTransition>)interop.ViewTransitionReference!).Value);
    }

    [TestMethod]
    public async Task DisposeAsync_ReleasesTheReferenceJsHeld()
    {
        var interop = new FakeBmotionInterop();
        var vt = new BmotionViewTransition(interop);
        await vt.StartAsync(() => { });

        await vt.DisposeAsync();

        Assert.IsTrue(FakeBmotionInterop.IsDisposed(interop.ViewTransitionReference!));
    }

    [TestMethod]
    public async Task StartAsync_AfterCompletion_CanStartAgain()
    {
        var interop = new FakeBmotionInterop { SupportsViewTransitions = true };
        await using var vt = new BmotionViewTransition(interop);

        // The in-progress guard must reset after each transition settles.
        await vt.StartAsync(() => Task.CompletedTask);
        var ran = false;
        await vt.StartAsync(() => { ran = true; return Task.CompletedTask; });
        Assert.IsTrue(ran);
    }
}
