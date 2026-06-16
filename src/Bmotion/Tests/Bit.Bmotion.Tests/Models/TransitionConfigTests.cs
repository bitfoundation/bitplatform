
namespace Bit.Bmotion.Tests.Models;

[TestClass]
public class TransitionConfigTests
{
    // ── Default values ────────────────────────────────────────────────────────

    [TestMethod]
    public void DefaultValues_MatchExpected()
    {
        var config = new BmotionTransitionConfig();

        Assert.AreEqual(BmotionTransitionType.Tween, config.Type);
        Assert.AreEqual(0.3, config.Duration);
        Assert.AreEqual(0.0, config.Delay);
        Assert.AreEqual(BmotionEasing.EaseOut, config.Ease);
        Assert.IsNull(config.EaseCubicBezier);
        Assert.AreEqual(0, config.Repeat);
        Assert.AreEqual(BmotionRepeatType.Loop, config.RepeatType);
        Assert.AreEqual(0.0, config.RepeatDelay);
        Assert.IsNull(config.Times);

        // Spring defaults
        Assert.AreEqual(100, config.Stiffness);
        Assert.AreEqual(10, config.Damping);
        Assert.AreEqual(1, config.Mass);
        Assert.AreEqual(0.0, config.Velocity);
        Assert.AreEqual(0.01, config.RestSpeed);
        Assert.AreEqual(0.01, config.RestDelta);

        // Inertia defaults
        Assert.AreEqual(0.0, config.InertiaVelocity);
        Assert.AreEqual(700, config.TimeConstant);
        Assert.AreEqual(0.8, config.Power);
        Assert.AreEqual(0.5, config.InertiaRestDelta);
        Assert.IsNull(config.InertiaMin);
        Assert.IsNull(config.InertiaMax);

        // Orchestration defaults
        Assert.IsNull(config.StaggerChildren);
        Assert.IsNull(config.DelayChildren);
        Assert.IsNull(config.Properties);
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    [TestMethod]
    public void Tween_DefaultFactory_UsesDefaults()
    {
        var config = BmotionTransitionConfig.Tween();

        Assert.AreEqual(BmotionTransitionType.Tween, config.Type);
        Assert.AreEqual(0.3, config.Duration);
        Assert.AreEqual(BmotionEasing.EaseOut, config.Ease);
    }

    [TestMethod]
    public void Tween_CustomFactory_SetsValues()
    {
        var config = BmotionTransitionConfig.Tween(0.5, BmotionEasing.EaseIn);

        Assert.AreEqual(BmotionTransitionType.Tween, config.Type);
        Assert.AreEqual(0.5, config.Duration);
        Assert.AreEqual(BmotionEasing.EaseIn, config.Ease);
    }

    [TestMethod]
    public void Spring_DefaultFactory_UsesDefaults()
    {
        var config = BmotionTransitionConfig.Spring();

        Assert.AreEqual(BmotionTransitionType.Spring, config.Type);
        Assert.AreEqual(100, config.Stiffness);
        Assert.AreEqual(10, config.Damping);
        Assert.AreEqual(1, config.Mass);
    }

    [TestMethod]
    public void Spring_CustomFactory_SetsValues()
    {
        var config = BmotionTransitionConfig.Spring(stiffness: 200, damping: 25, mass: 2);

        Assert.AreEqual(BmotionTransitionType.Spring, config.Type);
        Assert.AreEqual(200, config.Stiffness);
        Assert.AreEqual(25, config.Damping);
        Assert.AreEqual(2, config.Mass);
    }

    [TestMethod]
    public void Inertia_DefaultFactory_UsesDefaults()
    {
        var config = BmotionTransitionConfig.Inertia();

        Assert.AreEqual(BmotionTransitionType.Inertia, config.Type);
        Assert.AreEqual(0.0, config.InertiaVelocity);
        Assert.AreEqual(700, config.TimeConstant);
    }

    [TestMethod]
    public void Inertia_CustomFactory_SetsValues()
    {
        var config = BmotionTransitionConfig.Inertia(velocity: 500, timeConstant: 1000);

        Assert.AreEqual(BmotionTransitionType.Inertia, config.Type);
        Assert.AreEqual(500, config.InertiaVelocity);
        Assert.AreEqual(1000, config.TimeConstant);
    }

    // ── Repeat / Infinite sentinel ────────────────────────────────────────────

    [TestMethod]
    public void InfiniteRepeat_UsesIntMaxValue()
    {
        var config = new BmotionTransitionConfig { Repeat = int.MaxValue };
        Assert.AreEqual(int.MaxValue, config.Repeat);
    }

    // ── Per-property overrides ────────────────────────────────────────────────

    [TestMethod]
    public void PerPropertyOverrides_CanBeSetAndRetrieved()
    {
        var config = new BmotionTransitionConfig
        {
            Duration = 0.5,
            Properties = new Dictionary<string, BmotionTransitionConfig>
            {
                ["opacity"] = new BmotionTransitionConfig { Duration = 0.1 },
                ["transform"] = BmotionTransitionConfig.Spring(stiffness: 300),
            },
        };

        Assert.IsNotNull(config.Properties);
        Assert.AreEqual(2, config.Properties.Count);
        Assert.AreEqual(0.1, config.Properties["opacity"].Duration);
        Assert.AreEqual(BmotionTransitionType.Spring, config.Properties["transform"].Type);
        Assert.AreEqual(300, config.Properties["transform"].Stiffness);
    }

    // ── Orchestration ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Orchestration_Properties_CanBeSet()
    {
        var config = new BmotionTransitionConfig
        {
            StaggerChildren = 0.05,
            DelayChildren = 0.1,
        };

        Assert.AreEqual(0.05, config.StaggerChildren);
        Assert.AreEqual(0.1, config.DelayChildren);
    }

    // ── Custom cubic-bezier ───────────────────────────────────────────────────

    [TestMethod]
    public void EaseCubicBezier_CanBeSet()
    {
        var config = new BmotionTransitionConfig { EaseCubicBezier = [0.25, 0.1, 0.25, 1.0] };

        Assert.IsNotNull(config.EaseCubicBezier);
        Assert.AreEqual(4, config.EaseCubicBezier.Length);
        Assert.AreEqual(0.25, config.EaseCubicBezier[0]);
    }

    [TestMethod]
    public void EaseCubicBezier_WrongLength_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new BmotionTransitionConfig { EaseCubicBezier = [0.25, 0.1, 0.25] });
    }

    [TestMethod]
    public void EaseCubicBezier_NonFiniteValue_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new BmotionTransitionConfig { EaseCubicBezier = [0.25, 0.1, double.NaN, 1.0] });
    }

    [TestMethod]
    public void EaseCubicBezier_XControlPointOutOfRange_Throws()
    {
        // x1 (index 0) and x2 (index 2) must stay within [0, 1]; only Y may overshoot.
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new BmotionTransitionConfig { EaseCubicBezier = [1.5, 0.1, 0.25, 1.0] });
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new BmotionTransitionConfig { EaseCubicBezier = [0.25, 0.1, -0.2, 1.0] });
    }

    // ── Clone ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Clone_CopiesAllFields()
    {
        var original = new BmotionTransitionConfig
        {
            Type = BmotionTransitionType.Spring,
            Duration = 0.7,
            Delay = 0.2,
            Ease = BmotionEasing.BackInOut,
            EaseCubicBezier = [0.1, 0.2, 0.3, 0.4],
            Repeat = int.MaxValue,
            RepeatType = BmotionRepeatType.Mirror,
            RepeatDelay = 0.15,
            Times = [0, 0.5, 1],
            Stiffness = 321,
            Damping = 12,
            Mass = 3,
            Velocity = 42,
            RestSpeed = 0.002,
            RestDelta = 0.003,
            Bounce = 0.4,
            VisualDuration = 0.9,
            InertiaVelocity = 500,
            TimeConstant = 850,
            Power = 0.6,
            InertiaRestDelta = 0.25,
            InertiaMin = -100,
            InertiaMax = 100,
            StaggerChildren = 0.08,
            DelayChildren = 0.3,
            Properties = new Dictionary<string, BmotionTransitionConfig>
            {
                ["opacity"] = new BmotionTransitionConfig { Duration = 0.1 },
            },
        };

        var clone = original.Clone();

        Assert.AreEqual(original.Type, clone.Type);
        Assert.AreEqual(original.Duration, clone.Duration);
        Assert.AreEqual(original.Delay, clone.Delay);
        Assert.AreEqual(original.Ease, clone.Ease);
        CollectionAssert.AreEqual(original.EaseCubicBezier, clone.EaseCubicBezier);
        Assert.AreEqual(original.Repeat, clone.Repeat);
        Assert.AreEqual(original.RepeatType, clone.RepeatType);
        Assert.AreEqual(original.RepeatDelay, clone.RepeatDelay);
        CollectionAssert.AreEqual(original.Times, clone.Times);
        Assert.AreEqual(original.Stiffness, clone.Stiffness);
        Assert.AreEqual(original.Damping, clone.Damping);
        Assert.AreEqual(original.Mass, clone.Mass);
        Assert.AreEqual(original.Velocity, clone.Velocity);
        Assert.AreEqual(original.RestSpeed, clone.RestSpeed);
        Assert.AreEqual(original.RestDelta, clone.RestDelta);
        Assert.AreEqual(original.Bounce, clone.Bounce);
        Assert.AreEqual(original.VisualDuration, clone.VisualDuration);
        Assert.AreEqual(original.InertiaVelocity, clone.InertiaVelocity);
        Assert.AreEqual(original.TimeConstant, clone.TimeConstant);
        Assert.AreEqual(original.Power, clone.Power);
        Assert.AreEqual(original.InertiaRestDelta, clone.InertiaRestDelta);
        Assert.AreEqual(original.InertiaMin, clone.InertiaMin);
        Assert.AreEqual(original.InertiaMax, clone.InertiaMax);
        Assert.AreEqual(original.StaggerChildren, clone.StaggerChildren);
        Assert.AreEqual(original.DelayChildren, clone.DelayChildren);
        // Properties is deep-copied: equal contents but an independent dictionary instance.
        Assert.AreNotSame(original.Properties, clone.Properties);
        Assert.AreEqual(original.Properties!.Count, clone.Properties!.Count);
        Assert.AreEqual(original.Properties["opacity"].Duration, clone.Properties["opacity"].Duration);
        Assert.AreNotSame(original.Properties["opacity"], clone.Properties["opacity"]);
    }

    [TestMethod]
    public void Clone_IsIndependent_ForScalarsAndArrays()
    {
        var original = new BmotionTransitionConfig
        {
            Duration = 0.3,
            EaseCubicBezier = [0.1, 0.2, 0.3, 0.4],
            Times = [0, 1],
        };

        var clone = original.Clone();
        clone.Duration = 9.9;
        // EaseCubicBezier's getter returns a defensive copy, so mutating the getter result would
        // hit a throwaway array. Assign a new array to mutate the clone's actual stored state.
        clone.EaseCubicBezier = [0.9, 0.2, 0.3, 0.4];
        clone.Times![0] = 99;

        Assert.AreEqual(0.3, original.Duration);              // scalar untouched
        Assert.AreEqual(0.1, original.EaseCubicBezier![0]);    // array deep-copied
        Assert.AreEqual(0.0, original.Times![0]);              // array deep-copied
    }
}
