using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Exercises the JS→C# gesture/viewport callback paths (plan item 0.1 breadth) by invoking the
/// component's [JSInvokable] entry points on the DotNetObjectReference the fake interop captured -
/// the same call a real browser event would make.
/// </summary>
[TestClass]
public class GestureCallbackTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    private static Bmotion EventRef(BmotionTestContext ctx, string id) => (Bmotion)ctx.Interop.EventListenerRefs[id];
    private static Bmotion ViewportRef(BmotionTestContext ctx, string id) => (Bmotion)ctx.Interop.ViewportRefs[id];

    [TestMethod]
    public async Task PointerEnter_FiresHoverStart()
    {
        using var ctx = new BmotionTestContext();
        var started = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileHover, Bm.To(scale: 1.1))
            .Add(p => p.OnHoverStart, EventCallback.Factory.Create(this, () => started = true))
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() => EventRef(ctx, "box").OnPointerEnter());
        Assert.IsTrue(started);
    }

    [TestMethod]
    public async Task PointerEnterThenLeave_FiresBothHoverEvents()
    {
        using var ctx = new BmotionTestContext();
        bool started = false, ended = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileHover, Bm.To(scale: 1.1))
            .Add(p => p.OnHoverStart, EventCallback.Factory.Create(this, () => started = true))
            .Add(p => p.OnHoverEnd, EventCallback.Factory.Create(this, () => ended = true))
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() => EventRef(ctx, "box").OnPointerEnter());
        await cut.InvokeAsync(() => EventRef(ctx, "box").OnPointerLeave());
        Assert.IsTrue(started && ended);
    }

    [TestMethod]
    public async Task PointerUp_InsideElement_FiresTap_NotCancel()
    {
        using var ctx = new BmotionTestContext();
        bool tapped = false, cancelled = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileTap, Bm.To(scale: 0.95))
            .Add(p => p.OnTap, EventCallback.Factory.Create(this, () => tapped = true))
            .Add(p => p.OnTapCancel, EventCallback.Factory.Create(this, () => cancelled = true))
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() => EventRef(ctx, "box").OnPointerDown());
        await cut.InvokeAsync(() => EventRef(ctx, "box").OnPointerUp(isInsideElement: true));
        Assert.IsTrue(tapped);
        Assert.IsFalse(cancelled);
    }

    [TestMethod]
    public async Task PointerUp_OutsideElement_FiresCancel_NotTap()
    {
        using var ctx = new BmotionTestContext();
        bool tapped = false, cancelled = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileTap, Bm.To(scale: 0.95))
            .Add(p => p.OnTap, EventCallback.Factory.Create(this, () => tapped = true))
            .Add(p => p.OnTapCancel, EventCallback.Factory.Create(this, () => cancelled = true))
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() => EventRef(ctx, "box").OnPointerDown());
        // Released outside the element (pointer left before release) ⇒ tap cancelled, not fired.
        await cut.InvokeAsync(() => EventRef(ctx, "box").OnPointerUp(isInsideElement: false));
        Assert.IsTrue(cancelled);
        Assert.IsFalse(tapped);
    }

    [TestMethod]
    public async Task Intersect_FiresViewportEnter()
    {
        using var ctx = new BmotionTestContext();
        var entered = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.WhileInView, Bm.To(opacity: 1))
            .Add(p => p.OnViewportEnter, EventCallback.Factory.Create(this, () => entered = true))
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() => ViewportRef(ctx, "box").OnIntersect(isIntersecting: true));
        Assert.IsTrue(entered);
    }
}
