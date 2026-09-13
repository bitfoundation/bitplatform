using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>Tests for the drag-edge improvements (plan item 2.5): the DragPropagation flag wiring.</summary>
[TestClass]
public class DragEdgesTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    private static IReadOnlyDictionary<string, object?> AttachedFlags(FakeBmotionInterop interop)
    {
        var call = interop.Calls.First(c => c.Method == "attachEventListeners");
        return (IReadOnlyDictionary<string, object?>)call.Args[1]!;
    }

    [TestMethod]
    public void DragPropagation_True_FlagsPropagation()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Drag, BmDrag.Both)
            .Add(p => p.DragPropagation, true)
            .Add(p => p.ChildContent, Div));

        var flags = AttachedFlags(ctx.Interop);
        Assert.AreEqual(true, flags["drag"]);
        Assert.AreEqual(true, flags["dragPropagation"]);
    }

    [TestMethod]
    public void DragPropagation_DefaultFalse_OmitsFlag()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Drag, BmDrag.Both)
            .Add(p => p.ChildContent, Div));

        var flags = AttachedFlags(ctx.Interop);
        Assert.IsFalse(flags.ContainsKey("dragPropagation"),
            "propagation must default off (nested drags isolated) and not emit the flag");
    }

    // ── Direction lock reporting ──────────────────────────────────────────────

    [TestMethod]
    public async Task OnDirectionLocked_ReportsTheResolvedAxis()
    {
        using var ctx = new BmotionTestContext();
        BmDragAxis? locked = null;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Drag, BmDrag.Both)
            .Add(p => p.DragDirectionLock, true)
            .Add(p => p.OnDirectionLock, EventCallback.Factory.Create<BmDragAxis>(this, a => locked = a))
            .Add(p => p.ChildContent, Div));

        var element = (Bmotion)ctx.Interop.EventListenerRefs["box"];

        await cut.InvokeAsync(() => element.OnDirectionLocked("y"));
        Assert.AreEqual(BmDragAxis.Y, locked);

        await cut.InvokeAsync(() => element.OnDirectionLocked("x"));
        Assert.AreEqual(BmDragAxis.X, locked);
    }

    [TestMethod]
    public async Task OnDirectionLocked_UnknownAxis_FallsBackToX_InsteadOfThrowing()
    {
        using var ctx = new BmotionTestContext();
        BmDragAxis? locked = null;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Drag, BmDrag.Both)
            .Add(p => p.DragDirectionLock, true)
            .Add(p => p.OnDirectionLock, EventCallback.Factory.Create<BmDragAxis>(this, a => locked = a))
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() => ((Bmotion)ctx.Interop.EventListenerRefs["box"]).OnDirectionLocked("?"));

        Assert.AreEqual(BmDragAxis.X, locked);
    }

    [TestMethod]
    public async Task OnDirectionLocked_WithoutAHandler_IsANoOp()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Drag, BmDrag.Both)
            .Add(p => p.DragDirectionLock, true)
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() => ((Bmotion)ctx.Interop.EventListenerRefs["box"]).OnDirectionLocked("x"));
    }

    // ── Gesture propagation (tap / pan) ───────────────────────────────────────

    [TestMethod]
    public void GesturePropagation_DefaultsToTrue_AndOmitsTheFlag()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileTap, Bm.To(scale: 0.95))
            .Add(p => p.ChildContent, Div));

        Assert.IsFalse(AttachedFlags(ctx.Interop).ContainsKey("gesturePropagation"),
            "bubbling is the platform default, so the common case must not emit the flag");
    }

    [TestMethod]
    public void GesturePropagation_False_FlagsTheIsolationForJs()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileTap, Bm.To(scale: 0.95))
            .Add(p => p.GesturePropagation, false)
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual(false, AttachedFlags(ctx.Interop)["gesturePropagation"]);
    }

    [TestMethod]
    public void GesturePropagation_ChangingIt_ReattachesTheListeners()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileTap, Bm.To(scale: 0.95))
            .Add(p => p.ChildContent, Div));

        int before = ctx.Interop.CountOf("attachEventListeners");
        cut.Render(ps => ps.Add(p => p.GesturePropagation, false));

        Assert.AreEqual(before + 1, ctx.Interop.CountOf("attachEventListeners"),
            "listeners are wired once, so a changed flag has to re-wire them to take effect");
    }

    [TestMethod]
    public void DragDirectionLock_True_FlagsTheLockForJs()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Drag, BmDrag.Both)
            .Add(p => p.DragDirectionLock, true)
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual(true, AttachedFlags(ctx.Interop)["dragDirectionLock"]);
    }
}
