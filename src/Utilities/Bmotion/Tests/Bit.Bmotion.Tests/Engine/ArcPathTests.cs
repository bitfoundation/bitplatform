namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for <see cref="BmArc"/> - the curve an element travels along instead of the straight line
/// between two points - and the coupled driver behind it.
/// </summary>
[TestClass]
public class ArcPathTests
{
    // Perpendicular distance from the straight line P0→P1 to the point the curve reaches at t.
    private static double DeviationFromChord(BmArcCurve curve, double t)
    {
        var (x, y) = curve.PointAt(t);
        double dx = curve.ToX - curve.FromX, dy = curve.ToY - curve.FromY;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-9) return 0;
        // |cross product| / |chord| is the perpendicular distance from the line.
        return Math.Abs((x - curve.FromX) * dy - (y - curve.FromY) * dx) / length;
    }

    // ── The curve ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Curve_StartsAndEndsExactlyOnTheEndpoints()
    {
        var curve = Bm.Arc(strength: 0.9).BuildCurve(10, 20, 210, 120);

        var (startX, startY) = curve.PointAt(0);
        var (endX, endY) = curve.PointAt(1);

        Assert.AreEqual(10, startX, 1e-9);
        Assert.AreEqual(20, startY, 1e-9);
        Assert.AreEqual(210, endX, 1e-9);
        Assert.AreEqual(120, endY, 1e-9);
    }

    [TestMethod]
    public void Curve_LeavesTheStraightLineInBetween()
    {
        var curve = Bm.Arc(strength: 0.5).BuildCurve(0, 0, 200, 0);

        Assert.IsTrue(DeviationFromChord(curve, 0.5) > 1, "an arc must not trace the chord it is bending away from");
    }

    [TestMethod]
    public void Strength_ScalesHowFarTheCurveBends()
    {
        double gentle = DeviationFromChord(Bm.Arc(strength: 0.2).BuildCurve(0, 0, 200, 0), 0.5);
        double strong = DeviationFromChord(Bm.Arc(strength: 0.9).BuildCurve(0, 0, 200, 0), 0.5);

        Assert.IsTrue(strong > gentle, $"strength must deepen the bend ({strong} vs {gentle})");
    }

    [TestMethod]
    public void StrengthOne_PeaksARoughlyFullTravelDistanceOffTheLine()
    {
        // The documented meaning of strength: 1 - the peak sits a whole journey-length off the chord.
        var curve = Bm.Arc(strength: 1).BuildCurve(0, 0, 200, 0);

        Assert.AreEqual(200, DeviationFromChord(curve, 0.5), 1);
    }

    [TestMethod]
    public void StrengthZero_TracesTheStraightLine()
    {
        var curve = Bm.Arc(strength: 0).BuildCurve(0, 0, 200, 100);

        for (double t = 0; t <= 1.0001; t += 0.25)
            Assert.AreEqual(0, DeviationFromChord(curve, t), 1e-9);
    }

    [TestMethod]
    public void Peak_MovesTheApexAlongTheJourney()
    {
        // A quadratic Bézier always reaches its furthest point from the chord at t = 0.5; `peak`
        // moves where that point sits *in space* along the chord, which is what a viewer reads as
        // the arc cresting early or late.
        double early = Bm.Arc(strength: 0.6, peak: 0.15).BuildCurve(0, 0, 200, 0).PointAt(0.5).X;
        double middle = Bm.Arc(strength: 0.6, peak: 0.5).BuildCurve(0, 0, 200, 0).PointAt(0.5).X;
        double late = Bm.Arc(strength: 0.6, peak: 0.85).BuildCurve(0, 0, 200, 0).PointAt(0.5).X;

        Assert.IsTrue(early < middle, $"a low peak must crest nearer the start ({early} vs {middle})");
        Assert.IsTrue(late > middle, $"a high peak must crest nearer the end ({late} vs {middle})");
    }

    [TestMethod]
    public void Direction_ChoosesTheSideTheArcBulgesTowards()
    {
        var cw = Bm.Arc(strength: 0.5, direction: BmArcDirection.Clockwise).BuildCurve(0, 0, 200, 0);
        var ccw = Bm.Arc(strength: 0.5, direction: BmArcDirection.CounterClockwise).BuildCurve(0, 0, 200, 0);

        double cwY = cw.PointAt(0.5).Y;
        double ccwY = ccw.PointAt(0.5).Y;

        Assert.IsTrue(Math.Sign(cwY) != Math.Sign(ccwY) && cwY != 0,
            $"the two directions must bulge to opposite sides (got {cwY} and {ccwY})");
    }

    [TestMethod]
    public void Auto_BulgesUpwardForAHorizontalJourney()
    {
        // Screen coordinates: negative y is up, which is what a thrown object looks like.
        var curve = Bm.Arc(strength: 0.5).BuildCurve(0, 0, 200, 0);

        Assert.IsTrue(curve.PointAt(0.5).Y < 0);
    }

    [TestMethod]
    public void Auto_IsStableAcrossRepeatedCalls()
    {
        var first = Bm.Arc(strength: 0.5).BuildCurve(0, 0, 120, 80);
        var second = Bm.Arc(strength: 0.5).BuildCurve(0, 0, 120, 80);

        Assert.AreEqual(first.PointAt(0.5).Y, second.PointAt(0.5).Y, 1e-9);
    }

    [TestMethod]
    public void ZeroLengthJourney_ProducesNoCurveAndNoNaN()
    {
        var curve = Bm.Arc(strength: 1, rotate: 1).BuildCurve(50, 50, 50, 50);

        var (x, y) = curve.PointAt(0.5);
        Assert.AreEqual(50, x, 1e-9);
        Assert.AreEqual(50, y, 1e-9);
        Assert.AreEqual(0, curve.RotationAt(0.5), 1e-9);
    }

    [TestMethod]
    public void NonFiniteOptions_AreSanitisedRatherThanPropagatingNaN()
    {
        var curve = Bm.Arc(strength: double.NaN, peak: double.PositiveInfinity, rotate: double.NaN)
            .BuildCurve(0, 0, 100, 100);

        var (x, y) = curve.PointAt(0.5);
        Assert.IsTrue(double.IsFinite(x) && double.IsFinite(y));
        Assert.AreEqual(0, curve.RotationAt(0.5), 1e-9);
    }

    // ── Tangent following ─────────────────────────────────────────────────────

    [TestMethod]
    public void Rotate_Zero_KeepsTheElementUpright()
    {
        var curve = Bm.Arc(strength: 0.8).BuildCurve(0, 0, 200, 0);

        Assert.AreEqual(0, curve.RotationAt(0.25), 1e-9);
        Assert.AreEqual(0, curve.RotationAt(0.75), 1e-9);
    }

    [TestMethod]
    public void Rotate_TurnsTheElementAlongTheCurve()
    {
        var curve = Bm.Arc(strength: 0.8, rotate: 1).BuildCurve(0, 0, 200, 0);

        double entering = curve.RotationAt(0.05);
        double leaving = curve.RotationAt(0.95);

        // Rising then falling: the tangent starts pointing up-and-along and ends down-and-along.
        Assert.IsTrue(entering < 0, $"expected an upward tangent on the way out, got {entering}");
        Assert.IsTrue(leaving > 0, $"expected a downward tangent on the way in, got {leaving}");
    }

    [TestMethod]
    public void Rotate_Fraction_ScalesTheTurn()
    {
        var full = Bm.Arc(strength: 0.8, rotate: 1).BuildCurve(0, 0, 200, 0);
        var half = Bm.Arc(strength: 0.8, rotate: 0.5).BuildCurve(0, 0, 200, 0);

        Assert.AreEqual(full.RotationAt(0.2) * 0.5, half.RotationAt(0.2), 1e-9);
    }

    [TestMethod]
    public void FollowsTangent_ReflectsWhetherRotationIsOn()
    {
        Assert.IsFalse(Bm.Arc().FollowsTangent);
        Assert.IsFalse(Bm.Arc(rotate: 0).FollowsTangent);
        Assert.IsTrue(Bm.Arc(rotate: 0.3).FollowsTangent);
    }

    // ── The driver ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Driver_MovesAlongTheCurveAndSettlesOnTheTarget()
    {
        var curve = Bm.Arc(strength: 0.8).BuildCurve(0, 0, 200, 0);
        var points = new List<(double X, double Y)>();
        var driver = new BmotionArcDriver(curve, Bm.Tween(0.2, BmEase.Linear).ToConfig(),
            (x, y, _) => points.Add((x, y)));

        bool done = false;
        for (double ts = 0; ts <= 1000 && !done; ts += 16) done = driver.Tick(ts);

        Assert.IsTrue(done, "the arc should settle");
        Assert.AreEqual(200, points[^1].X, 0.5);
        Assert.AreEqual(0, points[^1].Y, 0.5);
        Assert.IsTrue(points.Any(p => Math.Abs(p.Y) > 1),
            "the journey must actually leave the straight line, not just end in the right place");
    }

    [TestMethod]
    public void Driver_ReportsRotationOnlyWhenTheArcTurnsTheElement()
    {
        var upright = Bm.Arc(strength: 0.8).BuildCurve(0, 0, 200, 0);
        var turning = Bm.Arc(strength: 0.8, rotate: 1).BuildCurve(0, 0, 200, 0);

        double? uprightRotation = 0, turningRotation = null;
        new BmotionArcDriver(upright, Bm.Tween(0.2).ToConfig(), (_, _, r) => uprightRotation = r).Tick(0);
        new BmotionArcDriver(turning, Bm.Tween(0.2).ToConfig(), (_, _, r) => turningRotation = r).Tick(0);

        Assert.IsNull(uprightRotation, "an arc that doesn't turn the element must not claim its rotate");
        Assert.IsNotNull(turningRotation);
    }

    [TestMethod]
    public void Driver_Complete_LandsExactlyOnTheTarget()
    {
        var curve = Bm.Arc(strength: 0.8).BuildCurve(0, 0, 200, 60);
        (double X, double Y) last = default;
        var driver = new BmotionArcDriver(curve, Bm.Tween(5).ToConfig(), (x, y, _) => last = (x, y));

        driver.Tick(0);
        driver.Complete();

        Assert.AreEqual(200, last.X, 1e-6);
        Assert.AreEqual(60, last.Y, 1e-6);
    }

    // ── When an arc applies ───────────────────────────────────────────────────

    [TestMethod]
    public void Applies_NeedsBothAxesAsSingleValues()
    {
        Assert.IsTrue(BmotionArcTargets.Applies(new() { ["x"] = 100.0, ["y"] = 50.0 }));
        Assert.IsFalse(BmotionArcTargets.Applies(new() { ["x"] = 100.0 }), "one axis doesn't define a curve");
        Assert.IsFalse(BmotionArcTargets.Applies(new() { ["x"] = 100.0, ["y"] = null }));
    }

    [TestMethod]
    public void Applies_IsFalseForKeyframeSequences()
    {
        // A keyframe array already describes its own path; a curve must not override it.
        Assert.IsFalse(BmotionArcTargets.Applies(
            new() { ["x"] = new double[] { 0, 100 }, ["y"] = 50.0 }));
    }

    [TestMethod]
    public void Applies_IsFalseForNonNumericOrNonFiniteValues()
    {
        Assert.IsFalse(BmotionArcTargets.Applies(new() { ["x"] = "50%", ["y"] = 10.0 }));
        Assert.IsFalse(BmotionArcTargets.Applies(new() { ["x"] = double.NaN, ["y"] = 10.0 }));
    }

    // ── Transition plumbing ───────────────────────────────────────────────────

    [TestMethod]
    public void Path_SurvivesLoweringCloningAndWithDelay()
    {
        var transition = Bm.Tween(0.5, path: Bm.Arc(strength: 0.7));

        Assert.IsNotNull(transition.ToConfig().Path);
        Assert.IsNotNull(transition.ToConfig().Clone().Path);
        Assert.IsNotNull(transition.WithDelay(0.2).Path);
    }

    [TestMethod]
    public void Path_ParticipatesInTransitionEquality()
    {
        Assert.IsFalse(BmTransition.AreEquivalent(Bm.Tween(0.5), Bm.Tween(0.5, path: Bm.Arc())));
        Assert.IsFalse(BmTransition.AreEquivalent(
            Bm.Tween(0.5, path: Bm.Arc(strength: 0.2)),
            Bm.Tween(0.5, path: Bm.Arc(strength: 0.9))));
        // Recreated inline with the same options: must not read as a change.
        Assert.IsTrue(BmTransition.AreEquivalent(
            Bm.Tween(0.5, path: Bm.Arc(strength: 0.6, rotate: 1)),
            Bm.Tween(0.5, path: Bm.Arc(strength: 0.6, rotate: 1))));
    }
}
