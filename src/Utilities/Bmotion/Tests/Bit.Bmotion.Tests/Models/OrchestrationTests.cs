namespace Bit.Bmotion.Tests.Models;

/// <summary>
/// Tests for variant orchestration: the <c>when</c> ordering flag and the duration estimate it is
/// built on (a spring has no true end, so <c>beforeChildren</c> needs one).
/// </summary>
[TestClass]
public class OrchestrationTests
{
    // ── EstimatedDurationSeconds ──────────────────────────────────────────────

    [TestMethod]
    public void Tween_ReportsItsDurationExactly()
    {
        Assert.AreEqual(0.4, Bm.Tween(0.4).ToConfig().EstimatedDurationSeconds(), 1e-9);
    }

    [TestMethod]
    public void Tween_ZeroDuration_ReportsZero()
    {
        Assert.AreEqual(0, Bm.Tween(0).ToConfig().EstimatedDurationSeconds(), 1e-9);
    }

    [TestMethod]
    public void Tween_IgnoresDelayAndRepeats()
    {
        // The estimate is the length of one play - a container's delay already shifts its children
        // through the normal transition, and repeats have no bearing on when children may start.
        var config = Bm.Tween(0.5, delay: 2, repeat: BmRepeat.Loop(4)).ToConfig();

        Assert.AreEqual(0.5, config.EstimatedDurationSeconds(), 1e-9);
    }

    [TestMethod]
    public void DurationBasedSpring_ReportsItsVisualDuration()
    {
        Assert.AreEqual(0.6, Bm.Spring(bounce: 0.3, duration: 0.6).ToConfig().EstimatedDurationSeconds(), 1e-9);
    }

    [TestMethod]
    public void PhysicsSpring_EstimatesFromTheDecayEnvelope()
    {
        // stiffness 100, damping 10, mass 1 → ω₀ = 10, ζ = 0.5, so 4/(ζω₀) = 0.8s.
        var config = Bm.Spring(stiffness: 100, damping: 10, mass: 1).ToConfig();

        Assert.AreEqual(0.8, config.EstimatedDurationSeconds(), 1e-9);
    }

    [TestMethod]
    public void StifferSpring_EstimatesShorter()
    {
        double loose = Bm.Spring(stiffness: 100, damping: 10).ToConfig().EstimatedDurationSeconds();
        double stiff = Bm.Spring(stiffness: 400, damping: 40).ToConfig().EstimatedDurationSeconds();

        Assert.IsTrue(stiff < loose, $"a snappier spring must estimate shorter ({stiff} vs {loose})");
    }

    [TestMethod]
    public void UndampedSpring_IsCappedRatherThanInfinite()
    {
        // Zero damping never settles; without the cap this would be infinity and would push every
        // child's delay out to never.
        var config = Bm.Spring(stiffness: 100, damping: 0).ToConfig();

        double estimate = config.EstimatedDurationSeconds();
        Assert.IsTrue(double.IsFinite(estimate) && estimate <= 10, $"expected a finite cap, got {estimate}");
    }

    [TestMethod]
    public void Inertia_EstimatesFromItsTimeConstant()
    {
        // 700ms time constant, ~98% decayed after four of them.
        Assert.AreEqual(2.8, Bm.Inertia(timeConstant: 700).ToConfig().EstimatedDurationSeconds(), 1e-9);
    }

    // ── when ──────────────────────────────────────────────────────────────────

    [TestMethod]
    public void When_DefaultsToTogether()
    {
        Assert.AreEqual(BmWhen.Together, Bm.Tween(0.3).When);
        Assert.AreEqual(BmWhen.Together, Bm.Spring().When);
        Assert.AreEqual(BmWhen.Together, Bm.Tween(0.3).ToConfig().When);
    }

    [TestMethod]
    public void When_SurvivesLoweringToTheEngineConfig()
    {
        var config = Bm.Tween(0.3, when: BmWhen.BeforeChildren).ToConfig();

        Assert.AreEqual(BmWhen.BeforeChildren, config.When);
    }

    [TestMethod]
    public void When_SurvivesWithDelayAndClone()
    {
        var transition = Bm.Spring(when: BmWhen.BeforeChildren);

        Assert.AreEqual(BmWhen.BeforeChildren, transition.WithDelay(0.5).When);
        Assert.AreEqual(BmWhen.BeforeChildren, transition.ToConfig().Clone().When);
    }

    [TestMethod]
    public void When_ParticipatesInTransitionEquality()
    {
        // Otherwise flipping the flag would not read as a parameter change on re-render.
        Assert.IsFalse(BmTransition.AreEquivalent(
            Bm.Tween(0.3),
            Bm.Tween(0.3, when: BmWhen.BeforeChildren)));

        Assert.IsTrue(BmTransition.AreEquivalent(
            Bm.Tween(0.3, when: BmWhen.BeforeChildren),
            Bm.Tween(0.3, when: BmWhen.BeforeChildren)));
    }
}
