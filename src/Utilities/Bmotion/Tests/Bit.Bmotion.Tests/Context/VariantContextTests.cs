namespace Bit.Bmotion.Tests.Context;

/// <summary>
/// Tests for <see cref="BmotionVariantContext"/>'s stagger bookkeeping once children unmount: a
/// released slot keeps its index (so the indices other children hold stay valid) but no longer
/// counts - towards a later child's place in the cascade, the stagger's total, or how long an
/// afterChildren parent waits.
/// </summary>
[TestClass]
public class VariantContextTests
{
    // Three children claim slots 0, 1 and 2, each reporting the given animation length.
    private static (BmotionVariantContext Ctx, int A, int B, int C) ThreeChildren(
        double durationA = 1, double durationB = 1, double durationC = 1)
    {
        var ctx = new BmotionVariantContext { StaggerChildren = 0.1 };
        int a = ctx.RegisterChild(() => durationA);
        int b = ctx.RegisterChild(() => durationB);
        int c = ctx.RegisterChild(() => durationC);
        return (ctx, a, b, c);
    }

    [TestMethod]
    public void UnregisterChild_LaterChildrenMoveUpTheCascade()
    {
        var (ctx, a, b, c) = ThreeChildren();

        ctx.UnregisterChild(a);

        // Indices are stable; the delay follows the rank among the children still present.
        Assert.AreEqual(0.0, ctx.GetChildDelay(b), 1e-9);
        Assert.AreEqual(0.1, ctx.GetChildDelay(c), 1e-9);
    }

    [TestMethod]
    public void UnregisterChild_InTheMiddle_OnlyShiftsTheChildrenAfterIt()
    {
        var (ctx, a, b, c) = ThreeChildren();

        ctx.UnregisterChild(b);

        Assert.AreEqual(0.0, ctx.GetChildDelay(a), 1e-9);
        Assert.AreEqual(0.1, ctx.GetChildDelay(c), 1e-9);
    }

    [TestMethod]
    public void TotalChildren_CountsOnlyPresentChildren()
    {
        var (ctx, a, _, c) = ThreeChildren();
        Assert.AreEqual(3, ctx.TotalChildren);

        ctx.UnregisterChild(a);
        ctx.UnregisterChild(c);

        Assert.AreEqual(1, ctx.TotalChildren);
        // The counter handing out indices is unaffected: a newcomer never reuses a released slot.
        Assert.AreEqual(3, ctx.NextChildIndex);
    }

    [TestMethod]
    public void UnregisterChild_ReachesAStaggerThatRadiatesFromTheLastChild()
    {
        // from: Last needs the total: with "c" gone, "b" is the last child and starts first.
        var ctx = new BmotionVariantContext { ChildStagger = Bm.Stagger(0.1, from: BmStaggerFrom.Last) };
        int a = ctx.RegisterChild();
        int b = ctx.RegisterChild();
        int c = ctx.RegisterChild();

        ctx.UnregisterChild(c);

        Assert.AreEqual(0.0, ctx.GetChildDelay(b), 1e-9);
        Assert.AreEqual(0.1, ctx.GetChildDelay(a), 1e-9);
    }

    [TestMethod]
    public void MaxChildFinishSeconds_IgnoresReleasedSlots()
    {
        // The long-running child is the one that leaves: an afterChildren parent must not keep
        // waiting on an animation that will never play.
        var (ctx, a, _, _) = ThreeChildren(durationA: 10, durationB: 0.5, durationC: 0.5);
        Assert.AreEqual(10, ctx.MaxChildFinishSeconds(), 1e-9);

        ctx.UnregisterChild(a);

        // b at rank 0 (0 + 0.5), c at rank 1 (0.1 + 0.5).
        Assert.AreEqual(0.6, ctx.MaxChildFinishSeconds(), 1e-9);
    }

    [TestMethod]
    public void UnregisterChild_OutOfRange_IsIgnored()
    {
        var (ctx, _, _, c) = ThreeChildren();

        ctx.UnregisterChild(-1);
        ctx.UnregisterChild(42);

        Assert.AreEqual(3, ctx.TotalChildren);
        Assert.AreEqual(0.2, ctx.GetChildDelay(c), 1e-9);
    }

    [TestMethod]
    public void SeedFrom_CarriesTheReleasedSlotsOver()
    {
        // A label change publishes a fresh context seeded from the old one: a child that left
        // before the change must stay out of the new cascade too.
        var (previous, a, b, _) = ThreeChildren();
        previous.UnregisterChild(a);

        var next = new BmotionVariantContext { StaggerChildren = 0.1 };
        next.SeedFrom(previous);

        Assert.AreEqual(2, next.TotalChildren);
        Assert.AreEqual(0.0, next.GetChildDelay(b), 1e-9);
    }
}
