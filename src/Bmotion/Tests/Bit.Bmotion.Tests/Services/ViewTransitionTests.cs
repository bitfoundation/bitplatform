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

    [TestMethod]
    public async Task StartAsync_NullUpdate_Throws()
    {
        var interop = new FakeBmotionInterop();
        await using var vt = new BmotionViewTransition(interop);
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await vt.StartAsync((Func<Task>)null!));
    }
}
