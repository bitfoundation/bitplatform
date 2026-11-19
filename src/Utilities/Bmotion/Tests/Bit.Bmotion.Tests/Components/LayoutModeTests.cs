using Bit.Bmotion.Tests.TestInfra;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Tests.Components;

/// <summary>
/// Tests for the FLIP layout modes: Full animates position + size, Position translates only, and
/// Size scales only (the element snaps to its new spot). The assertions read the dx/dy/sx/sy the
/// component hands to <c>playWaapiFlip</c>, which is where each mode's decision actually lands.
/// </summary>
[TestClass]
public class LayoutModeTests
{
    private static RenderFragment Div => b =>
    {
        b.OpenElement(0, "div");
        b.AddAttribute(1, "class", "box");
        b.CloseElement();
    };

    // The element starts at (0,0) 100x100 and, on the second render, is measured at (50,80) 200x50.
    // OnParametersSetAsync snapshots before the re-render and OnAfterRenderAsync measures after, so
    // the provider hands out the "before" rect for the first call and the "after" rect from then on.
    private static Func<string, BmotionBoundingRect?> MovingRect()
    {
        int call = 0;
        return _ => call++ == 0
            ? new BmotionBoundingRect { Left = 0, Top = 0, Width = 100, Height = 100 }
            : new BmotionBoundingRect { Left = 50, Top = 80, Width = 200, Height = 50 };
    }

    private static (double dx, double dy, double sx, double sy) PlayFlip(BmotionLayout layout)
    {
        using var ctx = new BmotionTestContext();
        ctx.Interop.BoundingRectProvider = MovingRect();

        var component = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, layout)
            .Add(p => p.ChildContent, Div));

        // A second render with a changed parameter is what triggers the snapshot → measure → FLIP.
        component.Render(ps => ps.Add(p => p.Custom, "changed"));

        var call = ctx.Interop.Calls.Last(c => c.Method == "playWaapiFlip");
        return ((double)call.Args[1]!, (double)call.Args[2]!, (double)call.Args[3]!, (double)call.Args[4]!);
    }

    [TestMethod]
    public void Full_AnimatesBothPositionAndSize()
    {
        var (dx, dy, sx, sy) = PlayFlip(BmotionLayout.Full);

        Assert.AreEqual(-50, dx, 1e-9);
        Assert.AreEqual(-80, dy, 1e-9);
        Assert.AreEqual(0.5, sx, 1e-9);   // 100 old / 200 new
        Assert.AreEqual(2.0, sy, 1e-9);   // 100 old / 50 new
    }

    [TestMethod]
    public void Position_TranslatesOnly()
    {
        var (dx, dy, sx, sy) = PlayFlip(BmotionLayout.Position);

        Assert.AreEqual(-50, dx, 1e-9);
        Assert.AreEqual(-80, dy, 1e-9);
        Assert.AreEqual(1.0, sx, 1e-9);
        Assert.AreEqual(1.0, sy, 1e-9);
    }

    [TestMethod]
    public void Size_ScalesOnly_AndSnapsToTheNewPosition()
    {
        var (dx, dy, sx, sy) = PlayFlip(BmotionLayout.Size);

        Assert.AreEqual(0, dx, 1e-9);
        Assert.AreEqual(0, dy, 1e-9);
        Assert.AreEqual(0.5, sx, 1e-9);
        Assert.AreEqual(2.0, sy, 1e-9);
    }

    // ── Holding the element through the render on Blazor Server ───────────────

    private static BmotionTestContext RenderAndMove(bool inProcess, BmotionLayout layout, Func<string, BmotionBoundingRect?> rects)
    {
        var ctx = new BmotionTestContext();
        ctx.Interop.IsInProcess = inProcess;
        ctx.Interop.BoundingRectProvider = rects;
        var component = ctx.Render<Bmotion>(ps => ps
            .Add(p => p.Id, "box")
            .Add(p => p.Layout, layout)
            .Add(p => p.LayoutAnchor, BmotionLayoutAnchor.Center)
            .Add(p => p.ChildContent, Div));
        component.Render(ps => ps.Add(p => p.Custom, "changed"));
        return ctx;
    }

    [TestMethod]
    public void OnServer_TheSnapshotBeforeARender_HoldsTheElementThroughIt_InTheFlipsTerms()
    {
        // The FLIP is a round trip behind the render there: until it comes, the element is held where it was.
        using var ctx = RenderAndMove(inProcess: false, BmotionLayout.Position, MovingRect());

        var hold = ctx.Interop.ArgsOf("measureAndHoldLayout").Single();
        Assert.AreEqual("box", hold[0]);
        Assert.AreEqual(true, hold[2], "translates, as Position does");
        Assert.AreEqual(false, hold[3], "but doesn't scale");
        Assert.AreEqual(0.5, (double)hold[4]!, 1e-9, "about the same anchor");
        Assert.AreEqual(0.5, (double)hold[5]!, 1e-9);
        Assert.AreEqual(1, ctx.Interop.CountOf("playWaapiFlip"), "the FLIP takes it over");
        Assert.IsFalse(ctx.Interop.WasCalled("releaseLayoutHold"));
    }

    [TestMethod]
    public void OnServer_ARenderThatDoesntMoveTheElement_LetsGoOfTheHold()
    {
        var still = new BmotionBoundingRect { Left = 10, Top = 10, Width = 100, Height = 100 };
        using var ctx = RenderAndMove(inProcess: false, BmotionLayout.Full, _ => still);

        Assert.AreEqual(1, ctx.Interop.CountOf("measureAndHoldLayout"));
        Assert.IsFalse(ctx.Interop.WasCalled("playWaapiFlip"));
        Assert.AreEqual("box", (string?)ctx.Interop.ArgsOf("releaseLayoutHold").Single()[0]);
    }

    [TestMethod]
    public void OnWebAssembly_NothingIsHeld()
    {
        // The FLIP plays before the browser paints there.
        using var ctx = RenderAndMove(inProcess: true, BmotionLayout.Full, MovingRect());

        Assert.IsFalse(ctx.Interop.WasCalled("measureAndHoldLayout"));
        Assert.AreEqual(1, ctx.Interop.CountOf("playWaapiFlip"));
    }

    [TestMethod]
    public void Size_IsEnabledAndDistinctFromTheOtherModes()
    {
        Assert.IsTrue(BmotionLayout.Size.Enabled);
        Assert.AreEqual(BmotionLayoutMode.Size, BmotionLayout.Size.Mode);
        Assert.AreNotEqual(BmotionLayout.Full, BmotionLayout.Size);
        Assert.AreNotEqual(BmotionLayout.Position, BmotionLayout.Size);
    }
}
