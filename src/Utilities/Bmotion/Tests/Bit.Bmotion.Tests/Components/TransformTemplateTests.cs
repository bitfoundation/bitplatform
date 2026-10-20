using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for <c>TransformTemplate</c> - the consumer's rewrite of the composed transform string.
/// The point of the parameter is that it applies to <em>every</em> path that writes a transform, so
/// the element never flickers between a templated and an untemplated one; that is what these check.
/// </summary>
[TestClass]
public class TransformTemplateTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.CloseElement();
    };

    private static readonly BmTransformTemplate _centred =
        (_, generated) => $"translate(-50%, -50%) {generated}";

    // ── The pre-first-paint inline style ──────────────────────────────────────

    [TestMethod]
    public void InitialInlineStyle_IsTemplated()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(x: 40))
            .Add(p => p.TransformTemplate, _centred)
            .Add(p => p.ChildContent, Div));

        var style = cut.Find("div").GetAttribute("style") ?? "";
        Assert.IsTrue(style.Contains("translate(-50%, -50%)"),
            $"the very first painted frame must already be templated; got: {style}");
    }

    [TestMethod]
    public void WithoutATemplate_TheTransformIsUnchanged()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(x: 40))
            .Add(p => p.ChildContent, Div));

        var style = cut.Find("div").GetAttribute("style") ?? "";
        Assert.IsTrue(style.Contains("transform:translate(40px,0px)"), $"got: {style}");
    }

    // ── The engine's live values ──────────────────────────────────────────────

    [TestMethod]
    public void EngineComposedTransform_IsTemplated()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(x: 40))
            .Add(p => p.TransformTemplate, _centred)
            .Add(p => p.ChildContent, Div));

        var transform = ctx.Engine.GetCurrentTransformString("box");
        Assert.IsNotNull(transform);
        StringAssert.StartsWith(transform, "translate(-50%, -50%)");
    }

    [TestMethod]
    public void Template_ReceivesTheLiveComponentsAndTheGeneratedString()
    {
        using var ctx = new BmotionTestContext();
        IReadOnlyDictionary<string, double>? seenComponents = null;
        string? seenGenerated = null;

        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(x: 40, scale: 2))
            .Add(p => p.TransformTemplate, new BmTransformTemplate((components, generated) =>
            {
                seenComponents = components;
                seenGenerated = generated;
                return generated;
            }))
            .Add(p => p.ChildContent, Div));

        ctx.Engine.GetCurrentTransformString("box");

        Assert.IsNotNull(seenComponents);
        Assert.AreEqual(40, seenComponents!.GetValueOrDefault("x"));
        Assert.AreEqual(2, seenComponents.GetValueOrDefault("scale"));
        StringAssert.Contains(seenGenerated!, "translate(40px,0px)");
    }

    [TestMethod]
    public void ReturningTheGeneratedString_IsTheSameAsNoTemplate()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(x: 40))
            .Add(p => p.TransformTemplate, new BmTransformTemplate((_, generated) => generated))
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual("translate(40px,0px)", ctx.Engine.GetCurrentTransformString("box"));
    }

    // ── Robustness ────────────────────────────────────────────────────────────

    [TestMethod]
    public void ThrowingTemplate_FallsBackToTheGeneratedString()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(x: 40))
            .Add(p => p.TransformTemplate, new BmTransformTemplate((_, _) => throw new InvalidOperationException()))
            .Add(p => p.ChildContent, Div));

        // This runs inside the rAF tick, where an exception would evict the element from the engine.
        Assert.AreEqual("translate(40px,0px)", ctx.Engine.GetCurrentTransformString("box"));
    }

    [TestMethod]
    public void NullReturningTemplate_FallsBackToTheGeneratedString()
    {
        using var ctx = new BmotionTestContext();
        ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(x: 40))
            .Add(p => p.TransformTemplate, new BmTransformTemplate((_, _) => null!))
            .Add(p => p.ChildContent, Div));

        Assert.AreEqual("translate(40px,0px)", ctx.Engine.GetCurrentTransformString("box"));
    }

    [TestMethod]
    public void Template_CanBeChangedAfterFirstRender()
    {
        using var ctx = new BmotionTestContext();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Initial, Bm.To(x: 40))
            .Add(p => p.ChildContent, Div));

        cut.Render(ps => ps.Add(p => p.TransformTemplate, _centred));

        StringAssert.StartsWith(ctx.Engine.GetCurrentTransformString("box")!, "translate(-50%, -50%)");
    }
}
