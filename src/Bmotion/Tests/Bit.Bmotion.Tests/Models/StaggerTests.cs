namespace Bit.Bmotion.Tests.Models;

/// <summary>Tests for 1-D, grid and custom staggering (plan item 2.3).</summary>
[TestClass]
public class StaggerTests
{
    private const double Tol = 1e-9;

    // ── 1-D (unchanged behavior) ──────────────────────────────────────────────

    [TestMethod]
    public void OneD_First_IsLinear()
    {
        var s = Bm.Stagger(0.1);
        Assert.AreEqual(0, s.DelayFor(0, 5), Tol);
        Assert.AreEqual(0.2, s.DelayFor(2, 5), Tol);
        Assert.AreEqual(0.4, s.DelayFor(4, 5), Tol);
    }

    [TestMethod]
    public void OneD_Center_RadiatesFromMiddle()
    {
        var s = Bm.Stagger(0.1, BmStaggerFrom.Center);
        Assert.AreEqual(0.2, s.DelayFor(0, 5), Tol);
        Assert.AreEqual(0, s.DelayFor(2, 5), Tol);
        Assert.AreEqual(0.2, s.DelayFor(4, 5), Tol);
    }

    [TestMethod]
    public void OneD_StartDelay_IsAdded()
    {
        var s = Bm.Stagger(0.1, BmStaggerFrom.First, startDelay: 0.5);
        Assert.AreEqual(0.5, s.DelayFor(0, 5), Tol);
        Assert.AreEqual(0.7, s.DelayFor(2, 5), Tol);
    }

    // ── Grid (2-D radial) ─────────────────────────────────────────────────────

    [TestMethod]
    public void Grid_Center_RadiatesRadially()
    {
        // 3x3 grid, radiate from the center cell (1,1).
        var s = Bm.Stagger(0.1, BmStaggerFrom.Center, grid: (3, 3));
        Assert.AreEqual(0, s.DelayFor(4, 9), Tol);                    // center cell
        Assert.AreEqual(0.1, s.DelayFor(1, 9), Tol);                 // one cell up (distance 1)
        Assert.AreEqual(0.1 * Math.Sqrt(2), s.DelayFor(0, 9), Tol);  // corner (distance √2)
    }

    [TestMethod]
    public void Grid_First_RadiatesFromTopLeft()
    {
        var s = Bm.Stagger(0.1, BmStaggerFrom.First, grid: (3, 3));
        Assert.AreEqual(0, s.DelayFor(0, 9), Tol);                    // top-left origin
        Assert.AreEqual(0.1 * Math.Sqrt(8), s.DelayFor(8, 9), Tol);  // bottom-right (√8)
    }

    [TestMethod]
    public void Grid_RejectsNonPositiveDimensions()
        => Assert.ThrowsExactly<ArgumentException>(() => Bm.Stagger(0.1, grid: (0, 3)));

    // ── Custom function ───────────────────────────────────────────────────────

    [TestMethod]
    public void Custom_Function_IsHonored()
    {
        var s = Bm.Stagger((i, total) => i * 0.05 + total * 0.001);
        Assert.AreEqual(0 + 10 * 0.001, s.DelayFor(0, 10), Tol);
        Assert.AreEqual(3 * 0.05 + 10 * 0.001, s.DelayFor(3, 10), Tol);
    }

    [TestMethod]
    public void Custom_NullThrows()
        => Assert.ThrowsExactly<ArgumentNullException>(() => Bm.Stagger((Func<int, int, double>)null!));
}
