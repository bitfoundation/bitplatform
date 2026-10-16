using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// <c>Initial="false"</c> - motion.dev's <c>initial={false}</c>: the element mounts already at its
/// <c>Animate</c> target, plays no enter animation and reports none, and only a later change of the
/// target animates. Pinned against the fake bridge: the inline style the first render injects, the
/// values the engine is seeded with, and the absence of any animation for the mount.
/// </summary>
[TestClass]
public class InitialFalseTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    private static BmotionElementDiagnostics State(BmotionTestContext ctx, string id)
        => ctx.Engine.GetDiagnostics().Single(d => d.Id == id);

    private IRenderedComponent<Bmotion> RenderAtRest(BmotionTestContext ctx, List<BmProps?> started, List<BmProps?> completed)
        => ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, (BmTarget)false)
            .Add(p => p.Animate, Bm.To(opacity: 0.5, x: 40))
            .Add(p => p.Transition, Bm.Tween(0.3))
            .Add(p => p.OnAnimationStart, EventCallback.Factory.Create<BmProps?>(this, p => started.Add(p)))
            .Add(p => p.OnAnimationComplete, EventCallback.Factory.Create<BmProps?>(this, p => { lock (completed) completed.Add(p); }))
            .Add(p => p.ChildContent, Div));

    [TestMethod]
    public void Mount_InjectsTheAnimateTarget_AndSeedsTheEngineWithIt()
    {
        using var ctx = new BmotionTestContext();
        var cut = RenderAtRest(ctx, [], []);

        // The first paint already shows the target - there is no Initial state to flash first.
        var style = cut.Find("div").GetAttribute("style")!;
        StringAssert.Contains(style, "opacity:0.5");
        StringAssert.Contains(style, "40px");

        // And the engine starts from those same values, so the next animation leaves from them.
        var state = State(ctx, "box");
        Assert.AreEqual(0.5, state.NumericValues["opacity"], 1e-9);
        Assert.AreEqual(40, state.Transforms["x"], 1e-9);
    }

    [TestMethod]
    public async Task Mount_StartsNoEnterAnimation_AndReportsNone()
    {
        using var ctx = new BmotionTestContext();
        var started = new List<BmProps?>();
        var completed = new List<BmProps?>();
        RenderAtRest(ctx, started, completed);

        // Neither a compositor animation nor a frame-loop driver: the element is simply at rest.
        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiAnimation"), "the mount must not play an enter animation");
        Assert.IsFalse(State(ctx, "box").HasActiveAnimations);

        // Give any stray driver or completion every chance to surface.
        await ctx.RunFramesAsync(0, 1000);
        await ctx.FinishWaapiAsync();
        Assert.AreEqual(0, started.Count, "an enter that never plays must not be reported as started");
        lock (completed) Assert.AreEqual(0, completed.Count, "...or as completed");
    }

    [TestMethod]
    public async Task ALaterAnimateChange_AnimatesFromTheMountedTarget_AndReportsIt()
    {
        using var ctx = new BmotionTestContext();
        var started = new List<BmProps?>();
        var completed = new List<BmProps?>();
        var cut = RenderAtRest(ctx, started, completed);

        cut.Render(ps => ps.Add(p => p.Animate, Bm.To(opacity: 1, x: 40)));

        // Only the change animates, and it leaves from the values the element mounted at.
        var play = ctx.Interop.ArgsOf("playWaapiAnimation").Single();
        var keyframes = (Dictionary<string, object>[])play[2]!;
        Assert.AreEqual("0.5", keyframes[0]["opacity"]);
        Assert.AreEqual("1", keyframes[^1]["opacity"]);
        Assert.AreEqual(1, started.Count);

        await ctx.FinishWaapiAsync("box");
        cut.WaitForAssertion(() => { lock (completed) Assert.AreEqual(1, completed.Count); });
        Assert.AreEqual(1.0, State(ctx, "box").NumericValues["opacity"], 1e-9);
    }
}
