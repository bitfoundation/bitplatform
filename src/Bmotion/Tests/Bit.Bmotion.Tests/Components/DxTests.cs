using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>Tests for the DX improvements (plan item 3.5): cancellation and the immutable-id guard.</summary>
[TestClass]
public class DxTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    [TestMethod]
    public void ChangingIdAfterFirstRender_IsIgnored()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.RenderComponent<Bmotion>(ps => ps
            .Add(p => p.Id, "first")
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual("first", cut.Find("div").Id);

        // Changing Id after first render must be ignored: the id is the engine identity.
        cut.SetParametersAndRender(ps => ps.Add(p => p.Id, "second"));
        Assert.AreEqual("first", cut.Find("div").Id);
    }

    [TestMethod]
    public async Task AnimateAsync_PreCancelledToken_DoesNotStartOffload()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.RenderComponent<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.ChildContent, Div));

        var before = ctx.Interop.CountOf("playWaapiAnimation") + ctx.Interop.CountOf("supportsLinearEasing");

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await cut.InvokeAsync(() =>
            cut.Instance.AnimateAsync(Bm.To(opacity: 0.5), Bm.Tween(0.5), cts.Token).AsTask());

        var after = ctx.Interop.CountOf("playWaapiAnimation") + ctx.Interop.CountOf("supportsLinearEasing");
        Assert.AreEqual(before, after, "a pre-cancelled token must not start the animation");
    }

    [TestMethod]
    public async Task AnimateAsync_LiveToken_StartsAnimation()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.RenderComponent<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() =>
            cut.Instance.AnimateAsync(Bm.To(opacity: 0.5), Bm.Tween(0.5), CancellationToken.None).AsTask());

        // An offload-eligible opacity tween probes the compositor path.
        Assert.IsTrue(ctx.Interop.WasCalled("supportsLinearEasing") || ctx.Interop.WasCalled("playWaapiAnimation"));
    }
}
