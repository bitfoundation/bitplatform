namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// <see cref="BmSpring.Analyze"/> measures the engine's own spring driver, and <see cref="BmSprings"/> names springs whose
/// character matches what their names promise.
/// </summary>
[TestClass]
public class SpringAnalysisTests
{
    [TestMethod]
    public void AnUnderdampedSpring_OvershootsAsTheoryPredicts()
    {
        // ζ = 10 / (2·√100) = 0.5: overshoot e^(-ζπ/√(1-ζ²)) ≈ 16.3 %, period 2π/(10·√0.75) ≈ 0.7255 s.
        var a = Bm.Spring(stiffness: 100, damping: 10).Analyze(0, 100);

        Assert.AreEqual(BmSpringDamping.Underdamped, a.DampingKind);
        Assert.AreEqual(0.5, a.DampingRatio, 1e-9);
        Assert.AreEqual(10, a.NaturalFrequency, 1e-9);
        // Measured on 60 Hz frames, as on screen: the frame nearest the peak, integrated in frame-sized steps.
        Assert.AreEqual(Math.Exp(-0.5 * Math.PI / Math.Sqrt(0.75)), a.Overshoot, 0.025);
        Assert.AreEqual(2 * Math.PI / (10 * Math.Sqrt(0.75)), a.OscillationPeriod!.Value, 1e-9);
        // The first peak sits half a period in.
        Assert.AreEqual(a.OscillationPeriod.Value / 2, a.PeakTime, 1.0 / 60);
        Assert.IsTrue(a.TargetCrossings >= 3, $"crossings {a.TargetCrossings}");
        Assert.IsTrue(a.Settled);
        Assert.AreEqual(100, a.FinalValue);
    }

    [TestMethod]
    public void ACriticallyDampedSpring_NeverPassesTheTarget()
    {
        var a = Bm.Spring(stiffness: 100, damping: 20).Analyze(0, 100);

        Assert.AreEqual(BmSpringDamping.CriticallyDamped, a.DampingKind);
        Assert.IsTrue(a.Overshoot < 1e-3, $"overshoot {a.Overshoot}");
        Assert.AreEqual(0, a.TargetCrossings);
        Assert.IsNull(a.OscillationPeriod);
    }

    [TestMethod]
    public void TheSettlingTime_IsWhenTheDriverRests()
    {
        var spring = Bm.Spring(stiffness: 300, damping: 20);
        var a = spring.Analyze(0, 200);

        // Run the same driver on 60 Hz frames: it rests within a frame of the analysis.
        var driver = new BmotionSpringDriver(0, 200, spring.ToConfig(), _ => { });
        double t = 0;
        driver.Tick(0);
        while (!driver.Tick(t += 1000.0 / 60) && t < 10_000) { }
        Assert.AreEqual(a.SettlingTime, t / 1000, 1.0 / 60 + 0.005, $"analysis {a.SettlingTime}, frames {t / 1000}");
    }

    [TestMethod]
    public void TheSamples_TraceTheMotionAt60Hz_FromStartToTarget()
    {
        var a = BmSprings.Bouncy().Analyze(10, 20);

        Assert.AreEqual(60, a.SampleRate);
        Assert.AreEqual(10, a.Samples[0]);
        Assert.AreEqual(20, a.Samples[^1]);
        Assert.AreEqual(a.SettlingTime * 60, a.Samples.Count, 3);
        Assert.IsTrue(a.Samples.Max() > 20, "a bouncy spring passes its target");
    }

    [TestMethod]
    public void ADurationBasedSpring_ReportsTheDerivedPhysics()
    {
        var a = Bm.Spring(bounce: 0.25, duration: 0.5).Analyze();
        var (k, d) = BmotionTransitionConfig.SpringFromBounce(0.5, 0.25);

        Assert.AreEqual(k, a.Stiffness, 1e-9);
        Assert.AreEqual(d, a.Damping, 1e-9);
        Assert.AreEqual(1, a.Mass);
    }

    [TestMethod]
    public void TheRestThresholds_ScaleWithDistance_AsOnScreen()
    {
        var spring = Bm.Spring(stiffness: 170, damping: 26);
        // The thresholds scale with the distance, so a spring settles in about the same time over 1 px and 1000 px.
        Assert.AreEqual(spring.Analyze(0, 1).SettlingTime, spring.Analyze(0, 1000).SettlingTime, 0.05);
    }

    [TestMethod]
    public void AnInitialVelocity_ShowsInThePeakVelocity()
    {
        var still = Bm.Spring(stiffness: 100, damping: 20).Analyze(0, 1);
        var thrown = Bm.Spring(stiffness: 100, damping: 20, velocity: 50).Analyze(0, 1);

        Assert.IsTrue(thrown.PeakVelocity >= 50, $"peak {thrown.PeakVelocity}");
        Assert.IsTrue(thrown.Overshoot > still.Overshoot, "a throw towards the target carries it past");
    }

    [TestMethod]
    public void EqualEnds_HaveNoOvershoot()
    {
        var a = Bm.Spring().Analyze(5, 5);
        Assert.AreEqual(0, a.Overshoot);
        Assert.AreEqual(5, a.FinalValue);
    }

    [TestMethod]
    public void AnUndampedSpring_ReportsThatItNeverSettles()
    {
        var a = Bm.Spring(stiffness: 100, damping: 0).Analyze();

        Assert.IsFalse(a.Settled);
        Assert.AreEqual(60, a.SettlingTime, 0.05);
        Assert.IsTrue(a.Samples.All(double.IsFinite));
    }

    [TestMethod]
    public void ExtremeParameters_StayFinite()
    {
        var a = Bm.Spring(stiffness: 1_000_000, damping: 10_000_000).Analyze(0, 100);

        Assert.IsTrue(a.Settled);
        Assert.IsTrue(a.Samples.All(double.IsFinite));
        Assert.AreEqual(BmSpringDamping.Overdamped, a.DampingKind);
        Assert.AreEqual(0, a.Overshoot);
    }

    [TestMethod]
    public void NonFiniteEnds_AreRejected()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Bm.Spring().Analyze(0, double.NaN));
    }

    [TestMethod]
    public void RepeatsAndDelays_AreNotTheSpringsMotion()
    {
        var plain = Bm.Spring(stiffness: 200, damping: 15).Analyze();
        var repeated = Bm.Spring(stiffness: 200, damping: 15, delay: 1, repeat: BmRepeat.Forever).Analyze();

        Assert.AreEqual(plain.SettlingTime, repeated.SettlingTime);
        Assert.IsTrue(repeated.Settled);
    }

    // ── The named springs ────────────────────────────────────────────────────

    [TestMethod]
    public void TheNamedSprings_AreFreshInstances()
    {
        var a = BmSprings.Snappy();
        a.Delay = 1;
        Assert.AreEqual(0, BmSprings.Snappy().Delay);
    }

    [TestMethod]
    public void TheCalmSprings_DoNotOvershoot()
    {
        foreach (var (name, spring) in new[]
        {
            ("Smooth", BmSprings.Smooth()), ("Stiff", BmSprings.Stiff()),
            ("Responsive", BmSprings.Responsive()), ("Heavy", BmSprings.Heavy()),
        })
        {
            var a = spring.Analyze();
            Assert.IsTrue(a.Overshoot < 1e-3, $"{name} overshoots {a.Overshoot:P2}");
            Assert.IsTrue(a.Settled, $"{name} doesn't settle");
        }
    }

    [TestMethod]
    public void TheLivelySprings_BounceInOrder()
    {
        double snappy = BmSprings.Snappy().Analyze().Overshoot;
        double gentle = BmSprings.Gentle().Analyze().Overshoot;
        double bouncy = BmSprings.Bouncy().Analyze().Overshoot;

        Assert.IsTrue(snappy >= gentle && bouncy > snappy, $"gentle {gentle:P1}, snappy {snappy:P1}, bouncy {bouncy:P1}");
        Assert.IsTrue(bouncy is > 0.08 and < 0.2, $"bouncy {bouncy:P1}");
        Assert.IsTrue(snappy < 0.03, $"snappy {snappy:P1}");
    }

    [TestMethod]
    public void TheNamedSprings_AreOrderedBySpeed()
    {
        double Settle(BmSpring s) => s.Analyze().SettlingTime;

        Assert.IsTrue(Settle(BmSprings.Responsive()) < Settle(BmSprings.Stiff()));
        Assert.IsTrue(Settle(BmSprings.Stiff()) < Settle(BmSprings.Smooth()));
        Assert.IsTrue(Settle(BmSprings.Smooth()) < Settle(BmSprings.Gentle()));
        Assert.IsTrue(Settle(BmSprings.Gentle()) < Settle(BmSprings.Heavy()) + 1, "heavy is the slow one");
    }
}
