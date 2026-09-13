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

    private static (double dx, double dy, double sx, double sy) PlayFlip(BmLayout layout)
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
        var (dx, dy, sx, sy) = PlayFlip(BmLayout.Full);

        Assert.AreEqual(-50, dx, 1e-9);
        Assert.AreEqual(-80, dy, 1e-9);
        Assert.AreEqual(0.5, sx, 1e-9);   // 100 old / 200 new
        Assert.AreEqual(2.0, sy, 1e-9);   // 100 old / 50 new
    }

    [TestMethod]
    public void Position_TranslatesOnly()
    {
        var (dx, dy, sx, sy) = PlayFlip(BmLayout.Position);

        Assert.AreEqual(-50, dx, 1e-9);
        Assert.AreEqual(-80, dy, 1e-9);
        Assert.AreEqual(1.0, sx, 1e-9);
        Assert.AreEqual(1.0, sy, 1e-9);
    }

    [TestMethod]
    public void Size_ScalesOnly_AndSnapsToTheNewPosition()
    {
        var (dx, dy, sx, sy) = PlayFlip(BmLayout.Size);

        Assert.AreEqual(0, dx, 1e-9);
        Assert.AreEqual(0, dy, 1e-9);
        Assert.AreEqual(0.5, sx, 1e-9);
        Assert.AreEqual(2.0, sy, 1e-9);
    }

    [TestMethod]
    public void Size_IsEnabledAndDistinctFromTheOtherModes()
    {
        Assert.IsTrue(BmLayout.Size.Enabled);
        Assert.AreEqual(BmLayoutMode.Size, BmLayout.Size.Mode);
        Assert.AreNotEqual(BmLayout.Full, BmLayout.Size);
        Assert.AreNotEqual(BmLayout.Position, BmLayout.Size);
    }
}
