using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Full gesture lifecycles driven through the component's [JSInvokable] entry points (the calls the
/// bridge makes) with the engine ticked frame by frame: focus, tap cancel, in-view leave with
/// <c>Once</c>, drag (snap-to-origin, momentum, constraints), pan, the per-frame <c>OnUpdate</c>
/// hook, <c>OnAnimationComplete</c>, the shared-element <c>LayoutId</c> hand-off and
/// <see cref="BmDragControls"/>.
/// </summary>
[TestClass]
public class GestureLifecycleTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    private static Bmotion Events(BmotionTestContext ctx, string id) => (Bmotion)ctx.Interop.EventListenerRefs[id];

    private static IReadOnlyDictionary<string, object?> AttachedFlags(BmotionTestContext ctx, string id)
        => (IReadOnlyDictionary<string, object?>)ctx.Interop.Calls.Last(c => c.Method == "attachEventListeners" && (string?)c.Args[0] == id).Args[1]!;

    private static BmotionElementDiagnostics State(BmotionTestContext ctx, string id)
        => ctx.Engine.GetDiagnostics().Single(d => d.Id == id);

    // ── Focus ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Focus_AppliesWhileFocus_AndRevertsOnFocusOut()
    {
        using var ctx = new BmotionTestContext();
        bool started = false, ended = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileFocus, Bm.To(scale: 1.2))
            .Add(p => p.OnFocusStart, EventCallback.Factory.Create(this, () => started = true))
            .Add(p => p.OnFocusEnd, EventCallback.Factory.Create(this, () => ended = true))
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual(true, AttachedFlags(ctx, "box")["focus"], "focus listeners must be requested");

        await cut.InvokeAsync(() => Events(ctx, "box").OnFocusIn());
        await ctx.RunFramesAsync(0, 1000);
        Assert.IsTrue(started);
        Assert.AreEqual(1.2, State(ctx, "box").Transforms["scale"], 1e-6);

        await cut.InvokeAsync(() => Events(ctx, "box").OnFocusOut());
        await ctx.RunFramesAsync(1016, 2000);
        Assert.IsTrue(ended);
        Assert.AreEqual(1.0, State(ctx, "box").Transforms["scale"], 1e-6, "the focus layer must revert to identity");
    }

    // ── Tap cancel ────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task PointerCancel_CancelsTheTap_AndRevertsWhileTap()
    {
        using var ctx = new BmotionTestContext();
        bool tapStarted = false, tapped = false, cancelled = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileTap, Bm.To(scale: 0.9))
            .Add(p => p.OnTapStart, EventCallback.Factory.Create(this, () => tapStarted = true))
            .Add(p => p.OnTap, EventCallback.Factory.Create(this, () => tapped = true))
            .Add(p => p.OnTapCancel, EventCallback.Factory.Create(this, () => cancelled = true))
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() => Events(ctx, "box").OnPointerDown());
        await ctx.RunFramesAsync(0, 1000);
        Assert.IsTrue(tapStarted);
        Assert.AreEqual(0.9, State(ctx, "box").Transforms["scale"], 1e-6);

        // The browser took the pointer away (scroll, OS gesture): no tap, and the press releases.
        await cut.InvokeAsync(() => Events(ctx, "box").OnPointerCancel());
        await ctx.RunFramesAsync(1016, 2000);
        Assert.IsTrue(cancelled);
        Assert.IsFalse(tapped);
        Assert.AreEqual(1.0, State(ctx, "box").Transforms["scale"], 1e-6);
    }

    // ── In view ───────────────────────────────────────────────────────────────

    private async Task<(double AfterLeave, bool LeaveFired)> EnterThenLeaveViewport(bool once)
    {
        using var ctx = new BmotionTestContext();
        bool left = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Animate, Bm.To(opacity: 0.5))
            .Add(p => p.WhileInView, Bm.To(opacity: 1))
            .Add(p => p.Once, once)
            .Add(p => p.OnViewportLeave, EventCallback.Factory.Create(this, () => left = true))
            .Add(p => p.ChildContent, Div));
        var observed = (BmViewport)ctx.Interop.ArgsOf("observeViewport").Single()[1]!;
        Assert.AreEqual(once, observed.Once, "the bridge must be told whether to keep observing");

        var viewport = (Bmotion)ctx.Interop.ViewportRefs["box"];
        await cut.InvokeAsync(() => viewport.OnIntersect(isIntersecting: true));
        await ctx.RunFramesAsync(0, 1000);
        Assert.AreEqual(1.0, State(ctx, "box").NumericValues["opacity"], 1e-6);

        await cut.InvokeAsync(() => viewport.OnIntersect(isIntersecting: false));
        await ctx.RunFramesAsync(1016, 2000);
        return (State(ctx, "box").NumericValues["opacity"], left);
    }

    [TestMethod]
    public async Task LeavingTheViewport_RevertsWhileInView()
    {
        var (opacity, leaveFired) = await EnterThenLeaveViewport(once: false);
        Assert.AreEqual(0.5, opacity, 1e-6, "leaving reverts to the Animate base");
        Assert.IsTrue(leaveFired);
    }

    [TestMethod]
    public async Task LeavingTheViewport_WithOnce_KeepsTheInViewState_ButStillReportsTheLeave()
    {
        var (opacity, leaveFired) = await EnterThenLeaveViewport(once: true);
        Assert.AreEqual(1.0, opacity, 1e-6, "Once must never deactivate the in-view state");
        Assert.IsTrue(leaveFired, "OnViewportLeave is an event, not part of the Once state");
    }

    // ── Drag ──────────────────────────────────────────────────────────────────

    private IRenderedComponent<Bmotion> RenderDraggable(
        BmotionTestContext ctx, Action<ComponentParameterCollectionBuilder<Bmotion>>? extra, List<string> log)
        => ctx.Render<Bmotion>(ps =>
        {
            ps.Add(p => p.Id, "box")
              .Add(p => p.Drag, BmDrag.Both)
              .Add(p => p.WhileDrag, Bm.To(scale: 1.1))
              .Add(p => p.OnDragStart, EventCallback.Factory.Create(this, () => log.Add("start")))
              .Add(p => p.OnDrag, EventCallback.Factory.Create(this, () => log.Add("move")))
              .Add(p => p.OnDragEnd, EventCallback.Factory.Create(this, () => log.Add("end")))
              .Add(p => p.ChildContent, Div);
            extra?.Invoke(ps);
        });

    // Presses, drags the element to (x, y) the way the bridge reports pointer moves, and releases.
    private static async Task DragTo(IRenderedComponent<Bmotion> cut, BmotionTestContext ctx, double x, double y,
        double velX, double velY, double? boundRight = null)
    {
        var el = Events(ctx, "box");
        await cut.InvokeAsync(el.OnPointerDown_Drag);
        await cut.InvokeAsync(() => el.SetDragPosition(x, y));
        await cut.InvokeAsync(el.OnDragMove);
        Assert.IsTrue(State(ctx, "box").IsDragging, "a pointer move mid-drag marks the element as dragging");
        await cut.InvokeAsync(() => el.OnPointerUp_Drag(velX, velY, boundRight: boundRight));
    }

    [TestMethod]
    public async Task Drag_SnapToOrigin_ReturnsHome_AndRaisesTheLifecycleInOrder()
    {
        using var ctx = new BmotionTestContext();
        var log = new List<string>();
        var cut = RenderDraggable(ctx, ps => ps.Add(p => p.DragSnapToOrigin, true), log);

        await DragTo(cut, ctx, 80, 40, velX: 20, velY: 20);

        Assert.IsFalse(State(ctx, "box").IsDragging);
        await ctx.RunFramesAsync(0, 3000);

        var settled = State(ctx, "box");
        Assert.AreEqual(0, settled.Transforms["x"], 0.5, "snap-to-origin ignores the release velocity");
        Assert.AreEqual(0, settled.Transforms["y"], 0.5);
        Assert.AreEqual(1.0, settled.Transforms["scale"], 1e-6, "the WhileDrag layer is released");
        CollectionAssert.AreEqual(new[] { "start", "move", "end" }, log);
    }

    [TestMethod]
    public async Task Drag_Momentum_CarriesTheElementOnInTheReleaseDirection()
    {
        using var ctx = new BmotionTestContext();
        var log = new List<string>();
        var cut = RenderDraggable(ctx, null, log);

        // A fast horizontal flick; vertical velocity below the inertia threshold.
        await DragTo(cut, ctx, 50, 10, velX: 10, velY: 0);

        CollectionAssert.Contains(State(ctx, "box").ActiveProperties.ToArray(), "x", "momentum coasts the flicked axis");
        CollectionAssert.DoesNotContain(State(ctx, "box").ActiveProperties.ToArray(), "y", "a still axis does not coast");
        await ctx.RunFramesAsync(0, 5000);

        var settled = State(ctx, "box");
        Assert.IsTrue(settled.Transforms["x"] > 50, $"momentum must carry x past the release point: {settled.Transforms["x"]}");
        Assert.AreEqual(10, settled.Transforms["y"], 1e-6, "y stays where it was released");
        Assert.IsFalse(settled.HasActiveAnimations, "the coast comes to rest");
    }

    [TestMethod]
    public async Task Drag_WithoutMomentum_SnapsBackInsideTheReportedBounds()
    {
        using var ctx = new BmotionTestContext();
        var log = new List<string>();
        var cut = RenderDraggable(ctx, ps => ps.Add(p => p.DragMomentum, false), log);

        // Released past the right bound the bridge resolved for this drag.
        await DragTo(cut, ctx, 80, 0, velX: 30, velY: 0, boundRight: 30);
        await ctx.RunFramesAsync(0, 3000);

        Assert.AreEqual(30, State(ctx, "box").Transforms["x"], 0.5);
    }

    // ── Pan ───────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Pan_RaisesStartMoveAndEnd_WithThePointerData()
    {
        using var ctx = new BmotionTestContext();
        var log = new List<string>();
        BmPanInfo? info = null;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.OnPanStart, EventCallback.Factory.Create(this, () => log.Add("start")))
            .Add(p => p.OnPan, EventCallback.Factory.Create<BmPanInfo>(this, i => { info = i; log.Add("pan"); }))
            .Add(p => p.OnPanEnd, EventCallback.Factory.Create(this, () => log.Add("end")))
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual(true, AttachedFlags(ctx, "box")["pan"], "pan listeners must be requested");

        var el = Events(ctx, "box");
        await cut.InvokeAsync(el.OnPanStart_);
        await cut.InvokeAsync(() => el.OnPanMove(100, 200, 3, 4, 30, 40, 500, -250));
        await cut.InvokeAsync(el.OnPanEnd_);

        CollectionAssert.AreEqual(new[] { "start", "pan", "end" }, log);
        Assert.IsNotNull(info);
        Assert.AreEqual((100.0, 200.0), (info.Point.X, info.Point.Y));
        Assert.AreEqual((3.0, 4.0), (info.Delta.X, info.Delta.Y));
        Assert.AreEqual((30.0, 40.0), (info.Offset.X, info.Offset.Y));
        Assert.AreEqual((500.0, -250.0), (info.Velocity.X, info.Velocity.Y));
        Assert.IsFalse(State(ctx, "box").HasActiveAnimations, "panning never moves the element itself");
    }

    // ── OnUpdate / OnAnimationComplete ────────────────────────────────────────

    [TestMethod]
    public async Task OnUpdate_ReceivesEveryFlushedFrame()
    {
        using var ctx = new BmotionTestContext();
        var frames = new List<Dictionary<string, string>>();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(backgroundColor: "#000000"))
            .Add(p => p.Animate, Bm.To(backgroundColor: "#ff0000"))
            .Add(p => p.Transition, Bm.Tween(0.2))
            .Add(p => p.OnUpdate, (Action<IReadOnlyDictionary<string, string>>)(d => frames.Add(new(d))))
            .Add(p => p.ChildContent, Div));

        await ctx.RunFramesAsync(0, 400);

        var colors = frames.Where(f => f.ContainsKey("backgroundColor")).Select(f => f["backgroundColor"]).ToArray();
        Assert.IsTrue(colors.Distinct().Count() >= 3, $"a 200ms tween should report several distinct frames: {string.Join(" | ", colors)}");
        Assert.AreEqual(ctx.Engine.GetCurrentStyles("box")!["backgroundColor"], colors[^1],
            "the last reported frame is what the element settled on");
    }

    [TestMethod]
    public async Task OnAnimationComplete_FiresForTheTargetThatFinished_NotTheOneItSuperseded()
    {
        using var ctx = new BmotionTestContext();
        var started = new List<BmProps?>();
        var completed = new List<BmProps?>();
        var firstCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.OnAnimationStart, EventCallback.Factory.Create<BmProps?>(this, p => started.Add(p)))
            .Add(p => p.OnAnimationComplete, EventCallback.Factory.Create<BmProps?>(this, p =>
            {
                lock (completed) completed.Add(p);
                firstCompletion.TrySetResult();
            }))
            .Add(p => p.ChildContent, Div));

        // Retarget before the first animation finishes: it is superseded, not completed.
        cut.Render(ps => ps.Add(p => p.Animate, Bm.To(opacity: 0.5)));
        await ctx.FinishWaapiAsync("box");
        await firstCompletion.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.AreEqual(2, started.Count);
        lock (completed)
        {
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual(0.5, completed[0]!.Opacity!.Last, "the completion carries the target that actually finished");
        }
    }

    // ── Shared layout ─────────────────────────────────────────────────────────

    private static RenderFragment Shared(string id, string layoutId, string? group = null) => b =>
    {
        RenderFragment element = inner =>
        {
            inner.OpenComponent<Bmotion>(0);
            inner.SetKey(id);
            inner.AddComponentParameter(1, nameof(Bmotion.Id), id);
            inner.AddComponentParameter(2, nameof(Bmotion.LayoutId), layoutId);
            inner.AddComponentParameter(3, nameof(Bmotion.ChildContent), Div);
            inner.CloseComponent();
        };
        if (group is null)
        {
            b.AddContent(0, element);
            return;
        }
        b.OpenComponent<BmotionLayoutGroup>(1);
        b.AddComponentParameter(2, nameof(BmotionLayoutGroup.Name), group);
        b.AddComponentParameter(3, nameof(BmotionLayoutGroup.ChildContent), element);
        b.CloseComponent();
    };

    private static BmotionBoundingRect? Rects(string id) => id switch
    {
        "tab-a" => new BmotionBoundingRect { Left = 0, Top = 0, Width = 100, Height = 4 },
        "tab-b" => new BmotionBoundingRect { Left = 200, Top = 0, Width = 50, Height = 4 },
        _ => null,
    };

    [TestMethod]
    public void LayoutId_NewHolder_FlipsFromWhereThePreviousOneWas()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = Rects;
        var host = ctx.Render<ToggleHost>(ps => ps.Add(p => p.ChildContent, Shared("tab-a", "underline")));
        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiFlip"), "the first holder has nothing to hand off from");

        // The underline moves to another tab: a different element with the same LayoutId mounts.
        host.Render(ps => ps.Add(p => p.ChildContent, Shared("tab-b", "underline")));

        var flip = ctx.Interop.ArgsOf("playWaapiFlip").Single();
        Assert.AreEqual("tab-b", flip[0]);
        Assert.AreEqual(-200, (double)flip[1]!, 1e-9, "starts at the old holder's x");
        Assert.AreEqual(0, (double)flip[2]!, 1e-9);
        Assert.AreEqual(2, (double)flip[3]!, 1e-9, "and at its width (100 / 50)");
        Assert.AreEqual(1, (double)flip[4]!, 1e-9);
    }

    [TestMethod]
    public void LayoutId_InDifferentLayoutGroups_DoesNotHandOff()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = Rects;
        var host = ctx.Render<ToggleHost>(ps => ps.Add(p => p.ChildContent, Shared("tab-a", "underline", group: "left")));

        host.Render(ps => ps.Add(p => p.ChildContent, Shared("tab-b", "underline", group: "right")));

        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiFlip"), "groups namespace LayoutIds");
    }

    // ── Drag controls ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task DragControls_StartTheDragWithThePointersIdAndPosition()
    {
        using var ctx = new BmotionTestContext();
        var controls = new BmDragControls();

        // Not attached to anything yet: a no-op rather than an error.
        await controls.StartAsync(new PointerEventArgs { PointerId = 1 });
        Assert.IsFalse(ctx.Interop.WasCalled("startDrag"));

        var host = ctx.Render<ToggleHost>(ps => ps.Add(p => p.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<Bmotion>(0);
            b.AddComponentParameter(1, nameof(Bmotion.Id), "thumb");
            b.AddComponentParameter(2, nameof(Bmotion.Drag), BmDrag.X);
            b.AddComponentParameter(3, nameof(Bmotion.DragControls), controls);
            b.AddComponentParameter(4, nameof(Bmotion.DragListener), false);
            b.AddComponentParameter(5, nameof(Bmotion.ChildContent), Div);
            b.CloseComponent();
        })));
        Assert.AreEqual(false, AttachedFlags(ctx, "thumb")["dragListener"], "the element itself must not start drags");

        await host.InvokeAsync(() => controls.StartAsync(new PointerEventArgs { PointerId = 7, ClientX = 12.5, ClientY = 30 }).AsTask());

        var call = ctx.Interop.ArgsOf("startDrag").Single();
        CollectionAssert.AreEqual(new object?[] { "thumb", 7L, 12.5, 30.0 }, call);

        // Unmounting detaches the controls again.
        host.Render(ps => ps.Add(p => p.Show, false));
        await controls.StartAsync(new PointerEventArgs { PointerId = 8 });
        Assert.AreEqual(1, ctx.Interop.CountOf("startDrag"));
    }
}
