namespace Bit.Bonboarding.Tests;

[TestClass]
public class PlacementEngineTests
{
    private static BonboardingPlacementRequest Request(
        BonboardingRect target,
        BonboardingSize popover,
        BonboardingPlacement preferred = BonboardingPlacement.Auto,
        BonboardingRect? viewport = null,
        bool rtl = false,
        IReadOnlyList<BonboardingPlacement>? fallbacks = null) => new()
        {
            Target = target,
            Popover = popover,
            Viewport = viewport ?? new BonboardingRect(0, 0, 1000, 800),
            Preferred = preferred,
            Fallbacks = fallbacks,
            Offset = 12,
            ViewportPadding = 16,
            ArrowSize = 10,
            CornerRadius = 12,
            RightToLeft = rtl,
            HasTarget = true,
        };

    [TestMethod]
    public void Centers_When_There_Is_No_Target()
    {
        var request = Request(BonboardingRect.Empty, new BonboardingSize(300, 200)) with { HasTarget = false };

        var result = BonboardingPlacementEngine.Place(request);

        Assert.AreEqual(BonboardingPlacementSide.Center, result.Side);
        Assert.IsFalse(result.ShowArrow);
        Assert.AreEqual(350, result.Popover.X);
        Assert.AreEqual(300, result.Popover.Y);
    }

    [TestMethod]
    public void Honours_An_Explicit_Placement_That_Fits()
    {
        var result = BonboardingPlacementEngine.Place(Request(new BonboardingRect(400, 400, 100, 40), new BonboardingSize(300, 150), BonboardingPlacement.Top));

        Assert.AreEqual(BonboardingPlacementSide.Top, result.Side);
        Assert.IsTrue(result.UsedPreferred);
        // Bottom edge of the popover sits one offset above the target.
        Assert.AreEqual(400 - 12 - 150, result.Popover.Y);
        // Centre-aligned on the target.
        Assert.AreEqual(450 - 150, result.Popover.X);
    }

    [TestMethod]
    public void Flips_To_The_Opposite_Side_When_The_Preferred_One_Does_Not_Fit()
    {
        // Target near the top: there is no room above for a 300px tall popover.
        var result = BonboardingPlacementEngine.Place(Request(new BonboardingRect(400, 20, 100, 40), new BonboardingSize(300, 300), BonboardingPlacement.Top));

        Assert.AreEqual(BonboardingPlacementSide.Bottom, result.Side);
        Assert.IsFalse(result.UsedPreferred);
        Assert.AreEqual(0, result.Overflow);
    }

    [TestMethod]
    public void Falls_Back_To_A_Perpendicular_Side_When_Neither_Vertical_Side_Fits()
    {
        // A tall popover in a short viewport: only an inline side can work, and Right is tried
        // before Left.
        var request = Request(
            new BonboardingRect(700, 300, 60, 60),
            new BonboardingSize(240, 560),
            BonboardingPlacement.Bottom,
            viewport: new BonboardingRect(0, 0, 1200, 640));

        var result = BonboardingPlacementEngine.Place(request);

        Assert.AreEqual(BonboardingPlacementSide.Right, result.Side);
        Assert.AreEqual(0, result.Overflow);
    }

    [TestMethod]
    public void Shifts_Along_The_Cross_Axis_Rather_Than_Flipping()
    {
        // Target hard against the left edge; a centred popover would hang off it. Sliding it right
        // keeps the requested side, which is much less disruptive than moving it beside the target.
        var result = BonboardingPlacementEngine.Place(Request(new BonboardingRect(10, 300, 40, 40), new BonboardingSize(320, 160), BonboardingPlacement.Bottom));

        Assert.AreEqual(BonboardingPlacementSide.Bottom, result.Side);
        Assert.IsTrue(result.UsedPreferred);
        Assert.AreEqual(16, result.Popover.Left);
        Assert.IsTrue(result.Popover.Right <= 1000 - 16);
        Assert.AreEqual(0, result.Overflow);
    }

    [TestMethod]
    public void Uses_An_Explicit_Fallback_Order()
    {
        var result = BonboardingPlacementEngine.Place(Request(
            new BonboardingRect(400, 20, 100, 40),
            new BonboardingSize(300, 300),
            BonboardingPlacement.Top,
            fallbacks: [BonboardingPlacement.Right]));

        Assert.AreEqual(BonboardingPlacementSide.Right, result.Side);
    }

    [TestMethod]
    public void Auto_Picks_The_Side_With_The_Most_Room()
    {
        // Target low in the viewport, so above has far more space than below.
        var result = BonboardingPlacementEngine.Place(Request(new BonboardingRect(450, 700, 100, 40), new BonboardingSize(200, 120)));

        Assert.AreEqual(BonboardingPlacementSide.Top, result.Side);
    }

    [TestMethod]
    public void Arrow_Points_At_The_Target_Centre()
    {
        var result = BonboardingPlacementEngine.Place(Request(new BonboardingRect(400, 400, 100, 40), new BonboardingSize(300, 150), BonboardingPlacement.Bottom));

        Assert.IsTrue(result.ShowArrow);
        // The popover is centred, so the arrow sits at its midpoint.
        Assert.AreEqual(150, result.ArrowOffset, 0.05);
    }

    [TestMethod]
    public void Arrow_Stays_Inside_The_Popover_Corners_For_An_Edge_Target()
    {
        var result = BonboardingPlacementEngine.Place(Request(new BonboardingRect(4, 300, 24, 24), new BonboardingSize(320, 160), BonboardingPlacement.Bottom));

        Assert.IsTrue(result.ShowArrow);
        // Corner radius plus half the arrow keeps it off the rounded corner.
        Assert.IsTrue(result.ArrowOffset >= 12 + 5, $"Arrow offset {result.ArrowOffset} overlaps the corner.");
        Assert.IsTrue(result.ArrowOffset <= 320 - 12 - 5);
    }

    [TestMethod]
    public void Arrow_Is_Hidden_When_The_Popover_Does_Not_Overlap_The_Target()
    {
        // A wide popover under a tiny corner target is shifted clear of it entirely, so an arrow
        // would point at empty space.
        var result = BonboardingPlacementEngine.Place(Request(new BonboardingRect(0, 0, 10, 10), new BonboardingSize(400, 200), BonboardingPlacement.Bottom));

        Assert.AreEqual(BonboardingPlacementSide.Bottom, result.Side);
        Assert.AreEqual(16, result.Popover.Left);
        Assert.IsFalse(result.ShowArrow);
    }

    [TestMethod]
    public void Arrow_Is_Hidden_When_Its_Size_Is_Zero()
    {
        var request = Request(new BonboardingRect(400, 400, 100, 40), new BonboardingSize(300, 150), BonboardingPlacement.Bottom) with { ArrowSize = 0 };

        Assert.IsFalse(BonboardingPlacementEngine.Place(request).ShowArrow);
    }

    [TestMethod]
    public void Mirrors_Start_And_End_Alignment_In_RightToLeft()
    {
        var ltr = BonboardingPlacementEngine.Place(Request(new BonboardingRect(400, 300, 200, 40), new BonboardingSize(120, 100), BonboardingPlacement.BottomStart));
        var rtl = BonboardingPlacementEngine.Place(Request(new BonboardingRect(400, 300, 200, 40), new BonboardingSize(120, 100), BonboardingPlacement.BottomStart, rtl: true));

        Assert.AreEqual(BonboardingPlacementAlign.Start, ltr.Align);
        Assert.AreEqual(400, ltr.Popover.X);

        Assert.AreEqual(BonboardingPlacementAlign.End, rtl.Align);
        Assert.AreEqual(600 - 120, rtl.Popover.X);
    }

    [TestMethod]
    public void Clamps_A_Popover_Larger_Than_The_Viewport_To_The_Safe_Area()
    {
        var request = Request(
            new BonboardingRect(100, 100, 40, 40),
            new BonboardingSize(900, 900),
            BonboardingPlacement.Bottom,
            viewport: new BonboardingRect(0, 0, 400, 400));

        var result = BonboardingPlacementEngine.Place(request);

        // The start of the content stays reachable rather than being scrolled off the top-left.
        Assert.AreEqual(16, result.Popover.X);
        Assert.AreEqual(16, result.Popover.Y);
    }

    [TestMethod]
    public void Survives_A_Viewport_Smaller_Than_Its_Own_Padding()
    {
        var request = Request(
            new BonboardingRect(5, 5, 10, 10),
            new BonboardingSize(50, 50),
            BonboardingPlacement.Bottom,
            viewport: new BonboardingRect(0, 0, 20, 20));

        var result = BonboardingPlacementEngine.Place(request);

        Assert.IsFalse(double.IsNaN(result.Popover.X));
        Assert.IsFalse(double.IsNaN(result.Popover.Y));
    }

    [TestMethod]
    public void Placement_Is_Deterministic()
    {
        var request = Request(new BonboardingRect(123.456, 234.567, 89.1, 33.3), new BonboardingSize(301.7, 155.2));

        var first = BonboardingPlacementEngine.Place(request);
        var second = BonboardingPlacementEngine.Place(request);

        Assert.AreEqual(first, second);
    }

    [TestMethod]
    [DataRow(BonboardingPlacement.Top, BonboardingPlacementSide.Top)]
    [DataRow(BonboardingPlacement.TopStart, BonboardingPlacementSide.Top)]
    [DataRow(BonboardingPlacement.BottomEnd, BonboardingPlacementSide.Bottom)]
    [DataRow(BonboardingPlacement.LeftStart, BonboardingPlacementSide.Left)]
    [DataRow(BonboardingPlacement.Right, BonboardingPlacementSide.Right)]
    public void Every_Placement_Resolves_To_Its_Own_Side_When_There_Is_Room(BonboardingPlacement placement, BonboardingPlacementSide expected)
    {
        var result = BonboardingPlacementEngine.Place(Request(new BonboardingRect(480, 380, 40, 40), new BonboardingSize(160, 100), placement));

        Assert.AreEqual(expected, result.Side);
    }
}
