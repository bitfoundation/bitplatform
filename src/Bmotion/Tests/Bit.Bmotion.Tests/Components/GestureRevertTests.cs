using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// What a gesture layer leaves behind when it lets go (motion.dev semantics): only the properties
/// the gesture changed move back, a keyframe resting target settles on its resting frame instead of
/// replaying, a property the resting target doesn't define returns to what it showed before the
/// gesture rather than to an identity value, and an orchestration delay that timed the element's
/// entrance does not time every later hover-out.
/// <para>
/// Gesture layers always animate on the frame loop, so the tests tick it by hand and read the
/// element's values through the engine's diagnostics.
/// </para>
/// </summary>
[TestClass]
public class GestureRevertTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.CloseElement();
    };

    private static Bmotion Events(BmotionTestContext ctx, string id) => (Bmotion)ctx.Interop.EventListenerRefs[id];

    private static BmotionElementDiagnostics State(BmotionTestContext ctx, string id)
        => ctx.Engine.GetDiagnostics().Single(d => d.Id == id);

    // Ticks the frame loop like RunFramesAsync does, sampling one value of the element per frame.
    private static async Task<List<double>> SampleFramesAsync(
        BmotionTestContext ctx, string id, Func<BmotionElementDiagnostics, double> read, double fromMs, double toMs)
    {
        var samples = new List<double>();
        for (double t = fromMs; t <= toMs; t += 16)
        {
            var timestamp = t;
            await ctx.Renderer.Dispatcher.InvokeAsync(() => ctx.Engine.ComputeFrame(timestamp));
            samples.Add(read(State(ctx, id)));
        }
        return samples;
    }

    [TestMethod]
    public async Task HoverEnd_LeavesAnInFlightEnterOnAnUnrelatedPropertyAlone()
    {
        using var ctx = new BmotionTestContext();
        var enterCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        // A colour enter (frame loop) still playing while a scale hover comes and goes.
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(backgroundColor: "#000000"))
            .Add(p => p.Animate, Bm.To(backgroundColor: "#ffffff"))
            .Add(p => p.Transition, Bm.Tween(1, ease: BmEase.Linear))
            .Add(p => p.WhileHover, Bm.To(scale: 1.2, transition: Bm.Tween(0.1)))
            .Add(p => p.OnAnimationComplete, EventCallback.Factory.Create<BmProps?>(this, _ => enterCompleted.TrySetResult()))
            .Add(p => p.ChildContent, Div));
        await ctx.RunFramesAsync(0, 200);

        await cut.InvokeAsync(Events(ctx, "box").OnPointerEnter);
        await ctx.RunFramesAsync(216, 400);
        await cut.InvokeAsync(Events(ctx, "box").OnPointerLeave);

        // The hover-out moves scale back and nothing else: the enter keeps playing undisturbed and
        // still reports its completion - retargeting it would have superseded it, completion and all.
        await ctx.RunFramesAsync(416, 1500);
        await enterCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var settled = State(ctx, "box");
        Assert.IsFalse(settled.HasActiveAnimations);
        Assert.AreEqual(1.0, settled.Transforms["scale"], 1e-6);
    }

    [TestMethod]
    public async Task HoverEnd_OverAKeyframeRestingTarget_SettlesOnItsLastFrame_InsteadOfReplaying()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Animate, Bm.To(opacity: new double[] { 0, 1 }))
            .Add(p => p.Transition, Bm.Tween(0.2, ease: BmEase.Linear))
            .Add(p => p.WhileHover, Bm.To(opacity: 0.5))
            .Add(p => p.ChildContent, Div));
        await ctx.FinishWaapiAsync("box"); // the [0, 1] fade-in has played

        await cut.InvokeAsync(Events(ctx, "box").OnPointerEnter);
        await ctx.RunFramesAsync(0, 500);
        Assert.AreEqual(0.5, State(ctx, "box").NumericValues["opacity"], 1e-6);

        await cut.InvokeAsync(Events(ctx, "box").OnPointerLeave);
        var opacities = await SampleFramesAsync(ctx, "box", s => s.NumericValues["opacity"], 516, 1000);

        // Straight from 0.5 up to the sequence's resting frame - never back through its first frame.
        Assert.IsTrue(opacities.Min() >= 0.5 - 1e-9, $"the fade-in must not replay: {string.Join(", ", opacities)}");
        Assert.AreEqual(1.0, opacities[^1], 1e-9);
    }

    [TestMethod]
    public async Task LeavingTheViewport_RestoresAnInitialOnlyValue_NotTheIdentityValue()
    {
        using var ctx = new BmotionTestContext();
        // opacity has no resting target (no Animate): before the in-view gesture it showed the
        // Initial 0.2, and that - not opacity's identity value 1 - is what it goes back to.
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(opacity: 0.2))
            .Add(p => p.WhileInView, Bm.To(opacity: 1))
            .Add(p => p.Transition, Bm.Tween(0.2))
            .Add(p => p.ChildContent, Div));
        var viewport = (Bmotion)ctx.Interop.ViewportRefs["box"];

        await cut.InvokeAsync(() => viewport.OnIntersect(isIntersecting: true));
        await ctx.RunFramesAsync(0, 500);
        Assert.AreEqual(1.0, State(ctx, "box").NumericValues["opacity"], 1e-6);

        await cut.InvokeAsync(() => viewport.OnIntersect(isIntersecting: false));
        await ctx.RunFramesAsync(516, 1000);
        Assert.AreEqual(0.2, State(ctx, "box").NumericValues["opacity"], 1e-6);
    }

    [TestMethod]
    public async Task HoverEnd_OnAVariantChild_RevertsWithoutItsEntranceDelay()
    {
        using var ctx = new BmotionTestContext();
        var states = new BmVariants { ["hidden"] = Bm.To(opacity: 0), ["visible"] = Bm.To(opacity: 1) };
        // The parent holds its children back by a second; the child's own transition is quick.
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "parent")
            .Add(p => p.Variants, states)
            .Add(p => p.InitialState, "hidden")
            .Add(p => p.State, "visible")
            .Add(p => p.Transition, Bm.Tween(0.3, delayChildren: 1))
            .Add(p => p.ChildContent, (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.OpenComponent<Bmotion>(1);
                b.AddComponentParameter(2, nameof(Bmotion.Id), "child");
                b.AddComponentParameter(3, nameof(Bmotion.Transition), (BmTransition?)Bm.Tween(0.2, ease: BmEase.Linear));
                b.AddComponentParameter(4, nameof(Bmotion.WhileHover), (BmTarget?)Bm.To(opacity: 0.5));
                b.AddComponentParameter(5, nameof(Bmotion.ChildContent), Div);
                b.CloseComponent();
                b.CloseElement();
            })));
        var enter = ctx.Interop.ArgsOf("playWaapiAnimation").Single(a => (string?)a[0] == "child");
        Assert.AreEqual(1000.0, (double)((Dictionary<string, object?>)enter[3]!)["delay"]!, 1e-6,
            "the baseline: the child's entrance waits for the parent's delayChildren");
        await ctx.FinishWaapiAsync();

        var child = Events(ctx, "child");
        await cut.InvokeAsync(child.OnPointerEnter);
        await ctx.RunFramesAsync(0, 400);
        await cut.InvokeAsync(child.OnPointerLeave);

        // 0.2s after the hover ends the element is back at its variant - a second's delay would
        // still be holding it at the hover value.
        await ctx.RunFramesAsync(416, 700);
        Assert.AreEqual(1.0, State(ctx, "child").NumericValues["opacity"], 1e-6);
    }

    [TestMethod]
    public async Task HoverEnd_OnAVariantChildWithNoTransitionAnywhere_RevertsWithoutItsEntranceDelay()
    {
        using var ctx = new BmotionTestContext();
        var states = new BmVariants { ["hidden"] = Bm.To(opacity: 0), ["visible"] = Bm.To(opacity: 1) };
        // As above, but the child has no transition of its own (nor a config default): its resting
        // transition is the engine default, and only its entrance - not the revert - may inherit
        // the parent's second of delayChildren.
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "parent")
            .Add(p => p.Variants, states)
            .Add(p => p.InitialState, "hidden")
            .Add(p => p.State, "visible")
            .Add(p => p.Transition, Bm.Tween(0.3, delayChildren: 1))
            .Add(p => p.ChildContent, (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.OpenComponent<Bmotion>(1);
                b.AddComponentParameter(2, nameof(Bmotion.Id), "child");
                b.AddComponentParameter(3, nameof(Bmotion.WhileHover), (BmTarget?)Bm.To(opacity: 0.5));
                b.AddComponentParameter(4, nameof(Bmotion.ChildContent), Div);
                b.CloseComponent();
                b.CloseElement();
            })));
        await ctx.FinishWaapiAsync();

        var child = Events(ctx, "child");
        await cut.InvokeAsync(child.OnPointerEnter);
        await ctx.RunFramesAsync(0, 600);
        Assert.AreEqual(0.5, State(ctx, "child").NumericValues["opacity"], 1e-6);
        await cut.InvokeAsync(child.OnPointerLeave);

        // The default 0.3s tween is over by 400ms after the hover ends; the entrance delay would
        // still be holding the element at the hover value.
        await ctx.RunFramesAsync(616, 1000);
        Assert.AreEqual(1.0, State(ctx, "child").NumericValues["opacity"], 1e-6);
    }
}
