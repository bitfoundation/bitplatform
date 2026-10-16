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
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "first")
            .Add(p => p.Animate, Bm.To(opacity: 1))
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual("first", cut.Find("div").Id);

        // Changing Id after first render must be ignored: the id is the engine identity.
        cut.Render(ps => ps.Add(p => p.Id, "second"));
        Assert.AreEqual("first", cut.Find("div").Id);
    }

    [TestMethod]
    public async Task AnimateAsync_PreCancelledToken_DoesNotStartOffload()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.ChildContent, Div));

        var callsBefore = ctx.Interop.Calls.Count;

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        // A color target would run on the rAF path and an opacity one would offload; use both so
        // neither path can slip through unnoticed.
        await cut.InvokeAsync(() =>
            cut.Instance.AnimateAsync(Bm.To(opacity: 0.5, backgroundColor: "#ff0000"), Bm.Tween(0.5), cts.Token).AsTask());

        Assert.AreEqual(callsBefore, ctx.Interop.Calls.Count, "a pre-cancelled token must not reach the bridge at all");
        var el = ctx.Engine.GetDiagnostics().Single(d => d.Id == "box");
        Assert.IsFalse(el.HasActiveAnimations, "a pre-cancelled token must not start a driver");
        Assert.IsFalse(el.NumericValues.ContainsKey("opacity"));
        Assert.IsFalse(el.StringValues.ContainsKey("backgroundColor"));
    }

    [TestMethod]
    public async Task AnimateAsync_CancelledMidFlight_StopsOnlyItsOwnProperties()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.ChildContent, Div));

        // Two independent rAF animations on the same element (colors never offload).
        await cut.InvokeAsync(() => cut.Instance.AnimateAsync(Bm.To(color: "#00ff00"), Bm.Tween(1)).AsTask());
        using var cts = new CancellationTokenSource();
        var cancellable = cut.InvokeAsync(() =>
            cut.Instance.AnimateAsync(Bm.To(backgroundColor: "#ff0000"), Bm.Tween(1), cts.Token).AsTask());
        await cancellable;
        CollectionAssert.AreEquivalent(new[] { "color", "backgroundColor" },
            ctx.Engine.GetDiagnostics().Single(d => d.Id == "box").ActiveProperties.ToArray());

        cts.Cancel();

        // The cancellation stops just the properties that call animated, not the whole element.
        CollectionAssert.AreEquivalent(new[] { "color" },
            ctx.Engine.GetDiagnostics().Single(d => d.Id == "box").ActiveProperties.ToArray());
    }

    [TestMethod]
    public async Task AnimateAsync_LiveToken_StartsAnimation()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.ChildContent, Div));

        await cut.InvokeAsync(() =>
            cut.Instance.AnimateAsync(Bm.To(opacity: 0.5), Bm.Tween(0.5), CancellationToken.None).AsTask());

        // An offload-eligible opacity tween is handed to the compositor: exactly one animation for
        // this element, heading for the requested value over the requested duration.
        var play = ctx.Interop.ArgsOf("playWaapiAnimation").Single();
        Assert.AreEqual("box", play[0]);
        var keyframes = (Dictionary<string, object>[])play[2]!;
        Assert.AreEqual("0.5", keyframes[^1]["opacity"]);
        Assert.AreEqual(500.0, (double)((Dictionary<string, object?>)play[3]!)["duration"]!, 1e-9);
    }
}
