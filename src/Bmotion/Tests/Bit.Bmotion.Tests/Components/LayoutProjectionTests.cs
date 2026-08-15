using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for the layout projection options - the anchor a FLIP is measured from, the coordinate
/// space it is measured in, the dependency that gates re-measuring, and the start/complete callbacks.
/// </summary>
[TestClass]
public class LayoutProjectionTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    // Before: 100x100 at (0,0). After: 200x200 at (0,0) - a pure in-place growth, so a top-left
    // anchor sees no movement at all while a centre anchor sees the box grow around its middle.
    private static Func<string, BmotionBoundingRect?> GrowingRect()
    {
        int call = 0;
        return _ => call++ == 0
            ? new BmotionBoundingRect { Left = 0, Top = 0, Width = 100, Height = 100 }
            : new BmotionBoundingRect { Left = 0, Top = 0, Width = 200, Height = 200 };
    }

    private static FakeBmotionInterop.Call? LastFlip(BmotionTestContext ctx)
        => ctx.Interop.Calls.LastOrDefault(c => c.Method == "playWaapiFlip");

    // ── LayoutAnchor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void DefaultAnchor_PinsTheTopLeftCorner()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = GrowingRect();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box").Add(p => p.Layout, BmLayout.Full).Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        var call = LastFlip(ctx)!;
        Assert.AreEqual(0, (double)call.Args[1]!, 1e-9, "a top-left-anchored in-place growth doesn't translate");
        Assert.AreEqual(0, (double)call.Args[2]!, 1e-9);
        Assert.AreEqual(0, (double)call.Args[8]!, 1e-9, "and the transform origin is the top-left corner");
        Assert.AreEqual(0, (double)call.Args[9]!, 1e-9);
    }

    [TestMethod]
    public void CenterAnchor_ProjectsFromTheMiddle()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = GrowingRect();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, BmLayout.Full)
            .Add(p => p.LayoutAnchor, BmLayoutAnchor.Center)
            .Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        var call = LastFlip(ctx)!;
        // Old centre (50,50) vs new centre (100,100): the box must appear to grow around its middle.
        Assert.AreEqual(-50, (double)call.Args[1]!, 1e-9);
        Assert.AreEqual(-50, (double)call.Args[2]!, 1e-9);
        Assert.AreEqual(0.5, (double)call.Args[8]!, 1e-9);
        Assert.AreEqual(0.5, (double)call.Args[9]!, 1e-9);
    }

    [TestMethod]
    public void Anchor_OutOfRangeOrNonFinite_IsClamped()
    {
        var sanitized = new BmLayoutAnchor(double.NaN, 5).Sanitized();

        Assert.AreEqual(0, sanitized.X);
        Assert.AreEqual(1, sanitized.Y);
    }

    // ── LayoutScroll / LayoutRoot ─────────────────────────────────────────────

    [TestMethod]
    public void MeasureOptions_DefaultToPlainDocumentCoordinates()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = GrowingRect();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box").Add(p => p.Layout, BmLayout.Full).Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        var measure = (BmotionMeasureOptions)ctx.Interop.Calls
            .Last(c => c.Method == "getBoundingRect").Args[1]!;
        Assert.IsTrue(measure.IsDefault);
    }

    [TestMethod]
    public void LayoutScroll_AsksForTheContainersScrollOffset()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = GrowingRect();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, BmLayout.Full)
            .Add(p => p.LayoutScroll, true)
            .Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        // Every measurement must use the same space, or the two would not be comparable.
        foreach (var call in ctx.Interop.Calls.Where(c => c.Method == "getBoundingRect"))
            Assert.IsTrue(((BmotionMeasureOptions)call.Args[1]!).TrackScroll);
    }

    [TestMethod]
    public void LayoutRoot_AsksForViewportCoordinates()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = GrowingRect();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, BmLayout.Full)
            .Add(p => p.LayoutRoot, true)
            .Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        foreach (var call in ctx.Interop.Calls.Where(c => c.Method == "getBoundingRect"))
            Assert.IsTrue(((BmotionMeasureOptions)call.Args[1]!).FixedRoot);
    }

    // ── LayoutDependency ──────────────────────────────────────────────────────

    [TestMethod]
    public void WithoutADependency_EveryRenderMeasures()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = _ => new BmotionBoundingRect { Width = 100, Height = 100 };
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box").Add(p => p.Layout, BmLayout.Full).Add(p => p.ChildContent, Div));

        int before = ctx.Interop.CountOf("getBoundingRect");
        cut.Render(ps => ps.Add(p => p.Custom, "a"));

        Assert.IsTrue(ctx.Interop.CountOf("getBoundingRect") > before);
    }

    [TestMethod]
    public void UnchangedDependency_SkipsTheMeasurement()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = _ => new BmotionBoundingRect { Width = 100, Height = 100 };
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, BmLayout.Full)
            .Add(p => p.LayoutDependency, "same")
            .Add(p => p.ChildContent, Div));

        int before = ctx.Interop.CountOf("getBoundingRect");
        cut.Render(ps => ps.Add(p => p.Custom, "an unrelated change"));

        Assert.AreEqual(before, ctx.Interop.CountOf("getBoundingRect"),
            "an unrelated re-render must not pay for a forced reflow");
    }

    [TestMethod]
    public void ChangedDependency_MeasuresAgain()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = _ => new BmotionBoundingRect { Width = 100, Height = 100 };
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, BmLayout.Full)
            .Add(p => p.LayoutDependency, "first")
            .Add(p => p.ChildContent, Div));

        int before = ctx.Interop.CountOf("getBoundingRect");
        cut.Render(ps => ps.Add(p => p.LayoutDependency, "second"));

        Assert.IsTrue(ctx.Interop.CountOf("getBoundingRect") > before);
    }

    // ── Layout animation callbacks ────────────────────────────────────────────

    [TestMethod]
    public void OnLayoutAnimationStart_FiresWhenTheFlipPlays()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = GrowingRect();
        bool started = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, BmLayout.Full)
            .Add(p => p.OnLayoutAnimationStart, EventCallback.Factory.Create(this, () => started = true))
            .Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        Assert.IsTrue(started);
    }

    [TestMethod]
    public void OnLayoutAnimationStart_DoesNotFireWhenNothingMoved()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = _ => new BmotionBoundingRect { Width = 100, Height = 100 };
        bool started = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, BmLayout.Full)
            .Add(p => p.OnLayoutAnimationStart, EventCallback.Factory.Create(this, () => started = true))
            .Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        Assert.IsFalse(started, "an unchanged layout has no animation to report");
    }

    [TestMethod]
    public async Task OnLayoutAnimationComplete_FiresFromTheJsCallback()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = GrowingRect();
        bool completed = false;
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, BmLayout.Full)
            .Add(p => p.OnLayoutAnimationComplete, EventCallback.Factory.Create(this, () => completed = true))
            .Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        await cut.InvokeAsync(() => ((Bmotion)ctx.Interop.FlipRefs["box"]).OnLayoutAnimationCompleted());

        Assert.IsTrue(completed);
    }

    [TestMethod]
    public void NoCompletionHandler_SendsNoCallbackRefToJs()
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = GrowingRect();
        var cut = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box").Add(p => p.Layout, BmLayout.Full).Add(p => p.ChildContent, Div));
        cut.Render(ps => ps.Add(p => p.Custom, "changed"));

        Assert.IsFalse(ctx.Interop.FlipRefs.ContainsKey("box"),
            "the common case must not pay for a JS→.NET callback after every layout animation");
    }
}
