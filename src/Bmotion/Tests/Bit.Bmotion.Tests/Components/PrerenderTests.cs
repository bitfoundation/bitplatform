using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <see cref="BmPrerenderMode"/>: what an element renders in a pass with no browser
/// attached, and how the interactive runtime takes over an element that was prerendered at rest.
/// </summary>
[TestClass]
public class PrerenderTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    private static void UseStaticRender(BmotionTestContext ctx)
    {
        ctx.Interop.IsPrerendering = true;
#if NET9_0_OR_GREATER
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
#endif
    }

    private static void UseInteractiveRender(BmotionTestContext ctx)
    {
#if NET9_0_OR_GREATER
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
#endif
    }

    private static IRenderedComponent<Bmotion> RenderFadeUp(BmotionTestContext ctx, Action<ComponentParameterCollectionBuilder<Bmotion>>? extra = null)
        => ctx.Render<Bmotion>(ps =>
        {
            ps.Add(p => p.Id, "box")
              .Add(p => p.Initial, Bm.To(opacity: 0, y: 20))
              .Add(p => p.Animate, Bm.To(opacity: 1, y: 0))
              .Add(p => p.Transition, Bm.Tween(0.5, ease: BmEase.Linear))
              .Add(p => p.ChildContent, Div);
            extra?.Invoke(ps);
        });

    private static string? StyleOf(IRenderedComponent<Bmotion> cut) => cut.Find("div").GetAttribute("style");

    private static RenderFragment StyledDiv(string style) => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "style", style);
        b.CloseElement();
    };

    /// <summary>Marks the element as part of an interactive island, which the prerendering pass persists entries for.</summary>
    private static void InInteractiveIsland(ComponentParameterCollectionBuilder<Bmotion> ps)
    {
#if NET9_0_OR_GREATER
        ps.SetAssignedRenderMode(RenderMode.InteractiveWebAssembly);
#endif
    }

    /// <summary>What the prerendering pass persists for elements resting at <paramref name="settledStyle"/>, whose markup carried <paramref name="ids"/>.</summary>
    private static void PersistPrerendered(BunitPersistentComponentState state, BmPrerenderMode mode, string settledStyle, params string[] ids)
    {
        var signature = BmotionPrerenderState.Signature(mode, settledStyle);
        for (int i = 0; i < ids.Length; i++)
            state.Persist(BmotionPrerenderState.EntryKey(signature, i), ids[i]);
        state.Persist(BmotionPrerenderState.CountKey(signature), ids.Length);
    }

    // ── Default mode ───────────────────────────────────────────────────────────

    [TestMethod]
    public void InitialMode_RendersTheInitialState_EvenWhenStatic()
    {
        using var ctx = new BmotionTestContext();
        UseStaticRender(ctx);

        var cut = RenderFadeUp(ctx);

        Assert.AreEqual("transform:translate(0px,20px);opacity:0;", StyleOf(cut));
    }

    // ── The prerendering pass ──────────────────────────────────────────────────

    [TestMethod]
    public void Static_Final_RendersAtRest_WithoutAnimation()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseStaticRender(ctx);

        var cut = RenderFadeUp(ctx);

        Assert.AreEqual("opacity:1;", StyleOf(cut));
    }

    [TestMethod]
    public void Static_Animate_PlaysTheEnterAnimationInCss()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        UseStaticRender(ctx);

        var style = StyleOf(RenderFadeUp(ctx));

        StringAssert.StartsWith(style, "opacity:1;;--bm-a:");
        StringAssert.Contains(style, "bm-transform-2 500ms linear 0ms 1 normal backwards");
        StringAssert.Contains(style, "bm-opacity-2 500ms linear 0ms 1 normal backwards");
        StringAssert.Contains(style, ";--bm-transform-0:translate(0px,20px);--bm-transform-1:none");
        Assert.IsTrue(style!.EndsWith(';'), "the consumer's style is appended straight after");
    }

    [TestMethod]
    public void PrerenderParameter_OverridesTheOption()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        UseStaticRender(ctx);

        var cut = RenderFadeUp(ctx, ps => ps.Add(p => p.Prerender, BmPrerenderMode.Initial));

        Assert.AreEqual("transform:translate(0px,20px);opacity:0;", StyleOf(cut));
    }

    [TestMethod]
    public void Static_Animate_HonoursTheReducedMotionPolicy()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        ctx.Options.ReducedMotion = BmReducedMotionMode.User;
        UseStaticRender(ctx);

        var style = StyleOf(RenderFadeUp(ctx));

        // The OS preference is left to the stylesheet's media query: transforms snap, opacity fades.
        StringAssert.Contains(style, ";--bm-ar:bm-opacity-2 500ms linear 0ms 1 normal backwards");
    }

    [TestMethod]
    public void Static_Final_KeepsTheConsumersStyleLast()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        UseStaticRender(ctx);

        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Initial, Bm.To(opacity: 0))
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.ChildContent, (RenderFragment)(b =>
            {
                b.OpenElement(0, "div");
                b.AddAttribute(1, "style", "color:red");
                b.CloseElement();
            })));

        StringAssert.EndsWith(StyleOf(cut), ";color:red");
    }

    [TestMethod]
    public void Static_StaggeredVariantChildren_GetTheirDelays()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        UseStaticRender(ctx);
        var states = new BmVariants { ["hidden"] = Bm.To(opacity: 0), ["visible"] = Bm.To(opacity: 1) };

        var cut = ctx.Render(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Variants), states);
            b.AddComponentParameter(2, nameof(Bmotion.InitialState), "hidden");
            b.AddComponentParameter(3, nameof(Bmotion.State), "visible");
            b.AddComponentParameter(4, nameof(Bmotion.Transition),
                Bm.Tween(0.3, childStagger: Bm.Stagger(0.1, from: BmStaggerFrom.Last)));
            b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(inner =>
            {
                inner.OpenElement(0, "ul");
                for (int i = 0; i < 3; i++)
                {
                    inner.OpenComponent<Bmotion>(1);
                    inner.SetKey(i);
                    inner.AddComponentParameter(2, nameof(Bmotion.Variants), states);
                    inner.AddComponentParameter(3, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
                    {
                        leaf.OpenElement(0, "li");
                        leaf.CloseElement();
                    }));
                    inner.CloseComponent();
                }
                inner.CloseElement();
            }));
            b.CloseComponent();
        });

        var items = cut.FindAll("li").Select(li => li.GetAttribute("style")!).ToArray();
        // Radiating from the last child needs the full child count, known only once all have mounted.
        StringAssert.Contains(items[0], " 200ms 1 normal backwards");
        StringAssert.Contains(items[1], " 100ms 1 normal backwards");
        StringAssert.Contains(items[2], " 0ms 1 normal backwards");
        StringAssert.StartsWith(items[0], "opacity:1;");
    }

    [TestMethod]
    public void Static_WhileInView_ShowsTheInViewStateWithoutScripting()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseStaticRender(ctx);

        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Initial, Bm.To(opacity: 0))
            .Add(p => p.WhileInView, Bm.To(opacity: 1))
            .Add(p => p.ChildContent, Div));

        // No Animate target: the element rests at Initial, and the stylesheet reveals it only under
        // @media (scripting: none).
        Assert.AreEqual("opacity:0;;--bm-nj-opacity:1;", StyleOf(cut));
    }

    [TestMethod]
    public void Static_PersistsAnEntryPerElement_WithTheIdItsMarkupCarries()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseStaticRender(ctx);

        RenderFadeUp(ctx, InInteractiveIsland);
        ctx.Render<Bmotion>(ps =>
        {
            ps.Add(p => p.Id, "other")
              .Add(p => p.Initial, Bm.To(opacity: 0))
              .Add(p => p.Animate, Bm.To(opacity: 1))
              .Add(p => p.ChildContent, Div);
            InInteractiveIsland(ps);
        });
        state.TriggerOnPersisting();

        // Each element registers its own callback, so each entry goes to that element's runtime.
        var signature = BmotionPrerenderState.Signature(BmPrerenderMode.Final, "opacity:1;");
        Assert.IsTrue(state.TryTake<int>(BmotionPrerenderState.CountKey(signature), out var count));
        Assert.AreEqual(2, count);
        Assert.IsTrue(state.TryTake<string>(BmotionPrerenderState.EntryKey(signature, 0), out var first));
        Assert.AreEqual("box", first);
        Assert.IsTrue(state.TryTake<string>(BmotionPrerenderState.EntryKey(signature, 1), out var second));
        Assert.AreEqual("other", second);
    }

#if NET9_0_OR_GREATER
    [TestMethod]
    public void Static_OutsideAnyInteractiveIsland_PersistsNothing()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseStaticRender(ctx);

        // No runtime ever renders static SSR content again: an entry for it could only be claimed
        // by some other, newer element.
        RenderFadeUp(ctx);
        state.TriggerOnPersisting();

        var signature = BmotionPrerenderState.Signature(BmPrerenderMode.Final, "opacity:1;");
        Assert.IsFalse(state.TryTake<int>(BmotionPrerenderState.CountKey(signature), out _));
    }
#endif

    [TestMethod]
    public void Static_InitialMode_PersistsNothing()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        UseStaticRender(ctx);

        RenderFadeUp(ctx, InInteractiveIsland);
        state.TriggerOnPersisting();

        foreach (var mode in new[] { BmPrerenderMode.Initial, BmPrerenderMode.Final })
            Assert.IsFalse(state.TryTake<int>(BmotionPrerenderState.CountKey(BmotionPrerenderState.Signature(mode, "opacity:1;")), out _));
    }

    [TestMethod]
    public void Static_ReducedList_LeavesOutTheStaggerDelay()
    {
        using var ctx = new BmotionTestContext();
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        ctx.Options.ReducedMotion = BmReducedMotionMode.User;
        UseStaticRender(ctx);
        var states = new BmVariants { ["hidden"] = Bm.To(opacity: 0), ["visible"] = Bm.To(opacity: 1) };

        var cut = ctx.Render(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Variants), states);
            b.AddComponentParameter(2, nameof(Bmotion.InitialState), "hidden");
            b.AddComponentParameter(3, nameof(Bmotion.State), "visible");
            b.AddComponentParameter(4, nameof(Bmotion.Transition), Bm.Tween(0.3, staggerChildren: 0.1));
            b.AddComponentParameter(5, nameof(Bmotion.ChildContent), (RenderFragment)(inner =>
            {
                inner.OpenElement(0, "ul");
                for (int i = 0; i < 2; i++)
                {
                    inner.OpenComponent<Bmotion>(1);
                    inner.SetKey(i);
                    inner.AddComponentParameter(2, nameof(Bmotion.Variants), states);
                    inner.AddComponentParameter(3, nameof(Bmotion.ChildContent), (RenderFragment)(leaf =>
                    {
                        leaf.OpenElement(0, "li");
                        leaf.CloseElement();
                    }));
                    inner.CloseComponent();
                }
                inner.CloseElement();
            }));
            b.CloseComponent();
        });

        var second = cut.FindAll("li")[1].GetAttribute("style")!;
        StringAssert.Contains(second, "--bm-af:bm-opacity-2 300ms ease-out 100ms");
        StringAssert.Contains(second, "--bm-ar:bm-opacity-2 300ms ease-out 0ms");
    }

    // ── The runtime taking over ────────────────────────────────────────────────

    [TestMethod]
    public void TakeOver_Final_StartsAtRest_AndSkipsTheEnter()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        PersistPrerendered(state, BmPrerenderMode.Final, "opacity:1;", "box");
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseInteractiveRender(ctx);
        int starts = 0;

        var cut = RenderFadeUp(ctx, ps => ps.Add(p => p.OnAnimationStart, (BmProps? _) => starts++));

        Assert.AreEqual("opacity:1;", StyleOf(cut));
        Assert.AreEqual(0, starts, "the enter animation must not replay");
        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiAnimation"));
        Assert.AreEqual("1", ctx.Engine.GetCurrentStyles("box")!["opacity"]);
    }

    [TestMethod]
    public void TakeOver_GestureRevertsToTheAnimateTarget()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        PersistPrerendered(state, BmPrerenderMode.Final, "opacity:1;", "box");
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseInteractiveRender(ctx);

        var cut = RenderFadeUp(ctx, ps => ps.Add(p => p.WhileHover, Bm.To(scale: 1.1)));
        cut.InvokeAsync(() => cut.Instance.OnPointerEnter()).GetAwaiter().GetResult();
        cut.InvokeAsync(() => cut.Instance.OnPointerLeave()).GetAwaiter().GetResult();

        // The base the hover layer reverts to is the Animate target, as if it had been played.
        Assert.AreEqual("1", ctx.Engine.GetCurrentStyles("box")!["opacity"]);
    }

    [TestMethod]
    public void TakeOver_Animate_ContinuesTheCssAnimation()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        PersistPrerendered(state, BmPrerenderMode.Animate, "opacity:1;", "bm-prerendered");
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        ctx.Interop.PrerenderedAnimationElapsedMs = 200;
        UseInteractiveRender(ctx);

        var style = StyleOf(RenderFadeUp(ctx));

        StringAssert.Contains(style, "bm-opacity-2 500ms linear -200ms 1 normal backwards");
        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiAnimation"));
        // How far in the animation is gets read off the prerendered markup it replaces.
        CollectionAssert.AreEqual(new[] { "bm-prerendered" }, ctx.Interop.PrerenderedAnimationQueries);
    }

    [TestMethod]
    public void TakeOver_Animate_ConsumerStyleChange_DropsTheContinuedAnimation()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        PersistPrerendered(state, BmPrerenderMode.Animate, "opacity:1;", "box");
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        ctx.Interop.PrerenderedAnimationElapsedMs = 200;
        UseInteractiveRender(ctx);

        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(opacity: 0))
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.Transition, Bm.Tween(0.5, ease: BmEase.Linear))
            .Add(p => p.ChildContent, StyledDiv("color:red")));
        StringAssert.Contains(StyleOf(cut), "--bm-a:");

        // An unchanged attribute isn't written again, so the animation keeps playing...
        cut.Render(ps => ps.Add(p => p.ChildContent, StyledDiv("color:red")));
        StringAssert.Contains(StyleOf(cut), "--bm-a:");

        // ...but a rewritten one must not restart it from where it was at the first render.
        cut.Render(ps => ps.Add(p => p.ChildContent, StyledDiv("color:blue")));
        Assert.AreEqual("opacity:1;color:blue", StyleOf(cut));
    }

    [TestMethod]
    public void TakeOver_Animate_WithoutElapsedTime_LandsAtRest()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        PersistPrerendered(state, BmPrerenderMode.Animate, "opacity:1;", "box");
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        UseInteractiveRender(ctx);

        Assert.AreEqual("opacity:1;", StyleOf(RenderFadeUp(ctx)));
    }

    [TestMethod]
    public void TakeOver_OnlyCoversTheFirstRender()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        PersistPrerendered(state, BmPrerenderMode.Final, "opacity:1;", "box");
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseInteractiveRender(ctx);

        RenderFadeUp(ctx);
        var later = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "later")
            .Add(p => p.Initial, Bm.To(opacity: 0))
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.ChildContent, Div));

        // Mounted after the prerendered page was replaced: new content, so it enters normally.
        Assert.AreEqual("opacity:0;", StyleOf(later));
    }

    [TestMethod]
    public void TakeOver_ContentThatWasNotPrerendered_EntersNormally()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        PersistPrerendered(state, BmPrerenderMode.Final, "opacity:1;", "box");
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseInteractiveRender(ctx);

        // Both mount in the first render, but only one was in the prerendered markup.
        var cut = ctx.Render(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Initial), (BmTarget?)Bm.To(opacity: 0));
            b.AddComponentParameter(2, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 0.8));
            b.AddComponentParameter(3, nameof(Bmotion.ChildContent), (RenderFragment)(inner =>
            {
                inner.OpenElement(0, "span");
                inner.CloseElement();
            }));
            b.CloseComponent();
            b.OpenComponent<Bmotion>(4);
            b.AddComponentParameter(5, nameof(Bmotion.Initial), (BmTarget?)Bm.To(opacity: 0));
            b.AddComponentParameter(6, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 1));
            b.AddComponentParameter(7, nameof(Bmotion.ChildContent), Div);
            b.CloseComponent();
        });

        Assert.AreEqual("opacity:0;", cut.Find("span").GetAttribute("style"));
        Assert.AreEqual("opacity:1;", cut.Find("div").GetAttribute("style"));
    }

    [TestMethod]
    public void TakeOver_EachEntryIsClaimedOnce()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        PersistPrerendered(state, BmPrerenderMode.Final, "opacity:1;", "a");
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseInteractiveRender(ctx);

        var cut = ctx.Render(b =>
        {
            for (int i = 0; i < 2; i++)
            {
                b.OpenComponent<Bmotion>(0);
                b.SetKey(i);
                b.AddComponentParameter(1, nameof(Bmotion.Initial), (BmTarget?)Bm.To(opacity: 0));
                b.AddComponentParameter(2, nameof(Bmotion.Animate), (BmTarget?)Bm.To(opacity: 1));
                b.AddComponentParameter(3, nameof(Bmotion.ChildContent), Div);
                b.CloseComponent();
            }
        });

        // One element was prerendered at rest; the second is new and enters.
        var styles = cut.FindAll("div").Select(d => d.GetAttribute("style")).ToArray();
        CollectionAssert.AreEqual(new[] { "opacity:1;", "opacity:0;" }, styles);
    }

    [TestMethod]
    public void TakeOver_SkipsEntriesPersistedForAnotherRuntime()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        // Two elements rested alike, but the first one's entry went to another runtime's state.
        var signature = BmotionPrerenderState.Signature(BmPrerenderMode.Final, "opacity:1;");
        state.Persist(BmotionPrerenderState.EntryKey(signature, 1), "box");
        state.Persist(BmotionPrerenderState.CountKey(signature), 2);
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseInteractiveRender(ctx);

        Assert.AreEqual("opacity:1;", StyleOf(RenderFadeUp(ctx)));
    }

    [TestMethod]
    public void Interactive_WithoutAPrerender_EntersNormally()
    {
        using var ctx = new BmotionTestContext();
        ctx.AddBunitPersistentComponentState();
        ctx.Options.Prerender = BmPrerenderMode.Final;
        UseInteractiveRender(ctx);
        int starts = 0;

        var cut = RenderFadeUp(ctx, ps => ps.Add(p => p.OnAnimationStart, (BmProps? _) => starts++));

        Assert.AreEqual("transform:translate(0px,20px);opacity:0;", StyleOf(cut));
        Assert.AreEqual(1, starts);
    }

    [TestMethod]
    public void TakeOver_EndlessAnimation_Restarts()
    {
        using var ctx = new BmotionTestContext();
        var state = ctx.AddBunitPersistentComponentState();
        var settled = BmotionPrerenderCss.Compile(
            Bm.To(rotate: 0).ToJsDictionary(), Bm.To(rotate: 360).ToJsDictionary(),
            Bm.Tween(1, ease: BmEase.Linear, repeat: BmRepeat.Forever).ToConfig(), null, null)!.SettledStyle;
        PersistPrerendered(state, BmPrerenderMode.Animate, settled, "bm-prerendered");
        ctx.Options.Prerender = BmPrerenderMode.Animate;
        UseInteractiveRender(ctx);
        int starts = 0;

        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Initial, Bm.To(rotate: 0))
            .Add(p => p.Animate, Bm.To(rotate: 360))
            .Add(p => p.Transition, Bm.Tween(1, ease: BmEase.Linear, repeat: BmRepeat.Forever))
            .Add(p => p.OnAnimationStart, (BmProps? _) => starts++)
            .Add(p => p.ChildContent, Div));

        // It never comes to rest, so there is no resting state to take over at.
        Assert.IsNull(StyleOf(cut));
        Assert.AreEqual(1, starts);
    }

    // ── Capabilities ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Capabilities_ReportPrerendering()
    {
        var interop = new FakeBmotionInterop { IsInProcess = false, IsPrerendering = true };
        Assert.IsTrue(new BmotionCapabilities(interop).IsPrerendering);
        interop.IsPrerendering = false;
        Assert.IsFalse(new BmotionCapabilities(interop).IsPrerendering);
    }
}
