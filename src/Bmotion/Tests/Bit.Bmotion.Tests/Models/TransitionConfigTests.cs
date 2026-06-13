using Bit.Bmotion.Models;

namespace Bit.Bmotion.Tests.Models;

[TestClass]
public class TransitionConfigTests
{
    // ── Default values ────────────────────────────────────────────────────────

    [TestMethod]
    public void DefaultValues_MatchExpected()
    {
        var config = new TransitionConfig();

        Assert.AreEqual(TransitionType.Tween, config.Type);
        Assert.AreEqual(0.3, config.Duration);
        Assert.AreEqual(0.0, config.Delay);
        Assert.AreEqual(Easing.EaseOut, config.Ease);
        Assert.IsNull(config.EaseCubicBezier);
        Assert.AreEqual(0, config.Repeat);
        Assert.AreEqual(RepeatType.Loop, config.RepeatType);
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
        Assert.AreEqual(WhenType.Default, config.When);
        Assert.IsNull(config.Properties);
    }

    // ── Factory helpers ───────────────────────────────────────────────────────

    [TestMethod]
    public void Tween_DefaultFactory_UsesDefaults()
    {
        var config = TransitionConfig.Tween();

        Assert.AreEqual(TransitionType.Tween, config.Type);
        Assert.AreEqual(0.3, config.Duration);
        Assert.AreEqual(Easing.EaseOut, config.Ease);
    }

    [TestMethod]
    public void Tween_CustomFactory_SetsValues()
    {
        var config = TransitionConfig.Tween(0.5, Easing.EaseIn);

        Assert.AreEqual(TransitionType.Tween, config.Type);
        Assert.AreEqual(0.5, config.Duration);
        Assert.AreEqual(Easing.EaseIn, config.Ease);
    }

    [TestMethod]
    public void Spring_DefaultFactory_UsesDefaults()
    {
        var config = TransitionConfig.Spring();

        Assert.AreEqual(TransitionType.Spring, config.Type);
        Assert.AreEqual(100, config.Stiffness);
        Assert.AreEqual(10, config.Damping);
        Assert.AreEqual(1, config.Mass);
    }

    [TestMethod]
    public void Spring_CustomFactory_SetsValues()
    {
        var config = TransitionConfig.Spring(stiffness: 200, damping: 25, mass: 2);

        Assert.AreEqual(TransitionType.Spring, config.Type);
        Assert.AreEqual(200, config.Stiffness);
        Assert.AreEqual(25, config.Damping);
        Assert.AreEqual(2, config.Mass);
    }

    [TestMethod]
    public void Inertia_DefaultFactory_UsesDefaults()
    {
        var config = TransitionConfig.Inertia();

        Assert.AreEqual(TransitionType.Inertia, config.Type);
        Assert.AreEqual(0.0, config.InertiaVelocity);
        Assert.AreEqual(700, config.TimeConstant);
    }

    [TestMethod]
    public void Inertia_CustomFactory_SetsValues()
    {
        var config = TransitionConfig.Inertia(velocity: 500, timeConstant: 1000);

        Assert.AreEqual(TransitionType.Inertia, config.Type);
        Assert.AreEqual(500, config.InertiaVelocity);
        Assert.AreEqual(1000, config.TimeConstant);
    }

    // ── Repeat / Infinite sentinel ────────────────────────────────────────────

    [TestMethod]
    public void InfiniteRepeat_UsesIntMaxValue()
    {
        var config = new TransitionConfig { Repeat = int.MaxValue };
        Assert.AreEqual(int.MaxValue, config.Repeat);
    }

    // ── Per-property overrides ────────────────────────────────────────────────

    [TestMethod]
    public void PerPropertyOverrides_CanBeSetAndRetrieved()
    {
        var config = new TransitionConfig
        {
            Duration = 0.5,
            Properties = new Dictionary<string, TransitionConfig>
            {
                ["opacity"] = new TransitionConfig { Duration = 0.1 },
                ["transform"] = TransitionConfig.Spring(stiffness: 300),
            },
        };

        Assert.IsNotNull(config.Properties);
        Assert.AreEqual(2, config.Properties.Count);
        Assert.AreEqual(0.1, config.Properties["opacity"].Duration);
        Assert.AreEqual(TransitionType.Spring, config.Properties["transform"].Type);
        Assert.AreEqual(300, config.Properties["transform"].Stiffness);
    }

    // ── Orchestration ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Orchestration_Properties_CanBeSet()
    {
        var config = new TransitionConfig
        {
            StaggerChildren = 0.05,
            DelayChildren = 0.1,
            When = WhenType.BeforeChildren,
        };

        Assert.AreEqual(0.05, config.StaggerChildren);
        Assert.AreEqual(0.1, config.DelayChildren);
        Assert.AreEqual(WhenType.BeforeChildren, config.When);
    }

    // ── Custom cubic-bezier ───────────────────────────────────────────────────

    [TestMethod]
    public void EaseCubicBezier_CanBeSet()
    {
        var config = new TransitionConfig { EaseCubicBezier = [0.25, 0.1, 0.25, 1.0] };

        Assert.IsNotNull(config.EaseCubicBezier);
        Assert.AreEqual(4, config.EaseCubicBezier.Length);
        Assert.AreEqual(0.25, config.EaseCubicBezier[0]);
    }

    // ── Clone ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Clone_CopiesAllFields()
    {
        var original = new TransitionConfig
        {
            Type = TransitionType.Spring,
            Duration = 0.7,
            Delay = 0.2,
            Ease = Easing.BackInOut,
            EaseCubicBezier = [0.1, 0.2, 0.3, 0.4],
            Repeat = int.MaxValue,
            RepeatType = RepeatType.Mirror,
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
            When = WhenType.AfterChildren,
            Properties = new Dictionary<string, TransitionConfig>
            {
                ["opacity"] = new TransitionConfig { Duration = 0.1 },
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
        Assert.AreEqual(original.When, clone.When);
        Assert.AreSame(original.Properties, clone.Properties);
    }

    [TestMethod]
    public void Clone_IsIndependent_ForScalarsAndArrays()
    {
        var original = new TransitionConfig
        {
            Duration = 0.3,
            EaseCubicBezier = [0.1, 0.2, 0.3, 0.4],
            Times = [0, 1],
        };

        var clone = original.Clone();
        clone.Duration = 9.9;
        clone.EaseCubicBezier![0] = 99;
        clone.Times![0] = 99;

        Assert.AreEqual(0.3, original.Duration);              // scalar untouched
        Assert.AreEqual(0.1, original.EaseCubicBezier![0]);    // array deep-copied
        Assert.AreEqual(0.0, original.Times![0]);              // array deep-copied
    }
}
