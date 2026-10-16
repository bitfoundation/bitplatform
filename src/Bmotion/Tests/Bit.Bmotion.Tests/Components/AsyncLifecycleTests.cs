using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// <see cref="Bmotion"/> across awaits that outlive the caller's interest: a cancellation token
/// that fires while the animation it started is still playing, and an element unmounted while
/// its initialisation is still waiting on the browser (a presence list, a filter, a page left at
/// once). The fake bridge's gates hold those round trips open so the unmount lands mid-way.
/// </summary>
[TestClass]
public class AsyncLifecycleTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.CloseElement();
    };

    private static BmotionElementDiagnostics State(BmotionTestContext ctx, string id)
        => ctx.Engine.GetDiagnostics().Single(d => d.Id == id);

    // ── AnimateAsync with a cancellation token ────────────────────────────────

    private static IRenderedComponent<Bmotion> RenderBlack(BmotionTestContext ctx)
        => ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(backgroundColor: "#000000"))
            .Add(p => p.ChildContent, Div));

    [TestMethod]
    public async Task AnimateAsync_TokenCancelledMidFlight_StopsTheAnimationWhereItIs()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderBlack(ctx);
        using var cts = new CancellationTokenSource();

        // AnimateAsync returns once the animation has started; the token has to outlive that.
        await cut.InvokeAsync(async () =>
            await cut.Instance.AnimateAsync(Bm.To(backgroundColor: "#ffffff"), Bm.Tween(1, ease: BmEase.Linear), cts.Token));
        await ctx.RunFramesAsync(0, 300);
        var midway = State(ctx, "box").StringValues["backgroundColor"];

        await cut.InvokeAsync(() => cts.Cancel());
        await ctx.RunFramesAsync(316, 1500);

        var stopped = State(ctx, "box");
        Assert.IsFalse(stopped.HasActiveAnimations, "cancelling the token must stop the animation");
        Assert.AreEqual(midway, stopped.StringValues["backgroundColor"], "frozen where it was, not run to its end");
    }

    [TestMethod]
    public async Task AnimateAsync_TokenCancelledAfterTheAnimationSettled_LeavesLaterAnimationsAlone()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderBlack(ctx);
        using var cts = new CancellationTokenSource();
        await cut.InvokeAsync(async () =>
            await cut.Instance.AnimateAsync(Bm.To(backgroundColor: "#ffffff"), Bm.Tween(0.2), cts.Token));
        await ctx.RunFramesAsync(0, 400);
        await cut.InvokeAsync(() => { }); // the settled animation's clean-up runs on the dispatcher

        // A later animation of the same property, started without the token.
        await cut.InvokeAsync(async () =>
            await cut.Instance.AnimateAsync(Bm.To(backgroundColor: "#ff0000"), Bm.Tween(1, ease: BmEase.Linear)));
        await ctx.RunFramesAsync(416, 600);
        var whenCancelled = State(ctx, "box").StringValues["backgroundColor"];

        // The first animation's registration went with it: the token no longer reaches the element.
        await cut.InvokeAsync(() => cts.Cancel());
        Assert.IsTrue(State(ctx, "box").HasActiveAnimations, "the newer animation must keep playing");
        await ctx.RunFramesAsync(616, 1600);
        Assert.AreNotEqual(whenCancelled, State(ctx, "box").StringValues["backgroundColor"]);
        Assert.IsFalse(State(ctx, "box").HasActiveAnimations);
    }

    // ── Unmounted while initialising ──────────────────────────────────────────

    private static IRenderedComponent<ToggleHost> RenderInteractive(BmotionTestContext ctx)
        => ctx.Render<ToggleHost>(ps => ps.Add(p => p.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Id), "box");
            b.AddComponentParameter(2, nameof(Bmotion.Initial), (BmTarget?)Bm.To(opacity: 0));
            b.AddComponentParameter(3, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 1));
            b.AddComponentParameter(4, nameof(Bmotion.WhileHover), (BmTarget?)Bm.To(scale: 1.1));
            b.AddComponentParameter(5, nameof(Bmotion.WhileInView), (BmTarget?)Bm.To(y: 0));
            b.AddComponentParameter(6, nameof(Bmotion.ChildContent), Div);
            b.CloseComponent();
        })));

    private static void AssertNothingHappenedAfterTheUnmount(BmotionTestContext ctx)
    {
        Assert.IsFalse(ctx.Engine.IsRegistered("box"), "a disposed element must not be (re-)registered in the engine");
        Assert.IsFalse(ctx.Interop.WasCalled("attachEventListeners"), "nor hand the bridge its disposed object reference");
        Assert.IsFalse(ctx.Interop.WasCalled("observeViewport"));
        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiAnimation"), "nor start its enter animation");
        Assert.IsFalse(ctx.Renderer.UnhandledException.IsCompleted, "and nothing may throw");
    }

    [TestMethod]
    public async Task UnmountedWhileProbingReducedMotion_NeverRegisters()
    {
        using var ctx = new BmotionTestContext();
        // The OS preference matters under User, so initialisation starts with the probe round trip.
        ctx.Options.ReducedMotion = BmReducedMotionMode.User;
        var probe = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ctx.Interop.PrefersReducedMotionGate = probe.Task;
        var host = RenderInteractive(ctx);
        Assert.IsTrue(ctx.Interop.WasCalled("prefersReducedMotion"));

        host.Render(ps => ps.Add(p => p.Show, false));
        await host.InvokeAsync(probe.SetResult);
        await host.InvokeAsync(() => { }); // let the resumed initialisation run its course

        AssertNothingHappenedAfterTheUnmount(ctx);
        Assert.IsFalse(ctx.Interop.WasCalled("registerElement"), "the bridge is never asked to find the element");
    }

    [TestMethod]
    public async Task UnmountedWhileTheBridgeLooksForTheElement_StopsThere()
    {
        using var ctx = new BmotionTestContext();
        var lookup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ctx.Interop.RegisterElementGate = lookup.Task;
        var host = RenderInteractive(ctx);
        Assert.IsTrue(ctx.Engine.IsRegistered("box"), "the engine registration precedes the bridge round trip");

        host.Render(ps => ps.Add(p => p.Show, false));
        Assert.IsFalse(ctx.Engine.IsRegistered("box"), "the unmount releases it");

        await host.InvokeAsync(lookup.SetResult);
        await host.InvokeAsync(() => { });

        AssertNothingHappenedAfterTheUnmount(ctx);
    }
}
