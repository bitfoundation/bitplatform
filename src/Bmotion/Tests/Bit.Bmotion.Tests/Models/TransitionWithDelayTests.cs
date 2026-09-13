namespace Bit.Bmotion.Tests.Models;

/// <summary>
/// Tests for <see cref="BmTransition.WithDelay"/>: it must produce an independent copy that keeps
/// every other knob (including the per-type ones) so a shared transition can drive staggered
/// elements without them fighting over its delay.
/// </summary>
[TestClass]
public class TransitionWithDelayTests
{
    [TestMethod]
    public void WithDelay_DoesNotMutateTheOriginal()
    {
        var original = Bm.Tween(0.4, delay: 0.1);
        var delayed = original.WithDelay(0.9);

        Assert.AreEqual(0.1, original.Delay);
        Assert.AreEqual(0.9, delayed.Delay);
        Assert.AreNotSame(original, delayed);
    }

    [TestMethod]
    public void WithDelay_PreservesTweenSpecifics()
    {
        var original = new BmTween
        {
            Duration = 1.25,
            Ease = BmEase.CircInOut,
            Bezier = [0.1, 0.2, 0.3, 0.4],
            Steps = 4,
            StepJump = BmStepJump.Start,
            Times = [0, 0.5, 1],
            Eases = [BmEase.BackOut],
        };

        var copy = (BmTween)original.WithDelay(0.5);

        Assert.AreEqual(1.25, copy.Duration);
        Assert.AreEqual(BmEase.CircInOut, copy.Ease);
        CollectionAssert.AreEqual(original.Bezier, copy.Bezier);
        Assert.AreEqual(4, copy.Steps);
        Assert.AreEqual(BmStepJump.Start, copy.StepJump);
        CollectionAssert.AreEqual(original.Times, copy.Times);
        CollectionAssert.AreEqual(original.Eases, copy.Eases);
    }

    [TestMethod]
    public void WithDelay_PreservesSpringSpecifics()
    {
        var original = new BmSpring
        {
            Stiffness = 321,
            Damping = 21,
            Mass = 2,
            Velocity = 7,
            RestSpeed = 0.5,
            RestDelta = 0.25,
            Bounce = 0.4,
            Duration = 0.75,
        };

        var copy = (BmSpring)original.WithDelay(0.2);

        Assert.AreEqual(321, copy.Stiffness);
        Assert.AreEqual(21, copy.Damping);
        Assert.AreEqual(2, copy.Mass);
        Assert.AreEqual(7, copy.Velocity);
        Assert.AreEqual(0.5, copy.RestSpeed);
        Assert.AreEqual(0.25, copy.RestDelta);
        Assert.AreEqual(0.4, copy.Bounce);
        Assert.AreEqual(0.75, copy.Duration);
    }

    [TestMethod]
    public void WithDelay_PreservesInertiaSpecifics()
    {
        var original = new BmInertia
        {
            Velocity = 500,
            TimeConstant = 350,
            Power = 0.6,
            RestDelta = 1.5,
            Min = -10,
            Max = 10,
        };

        var copy = (BmInertia)original.WithDelay(0.3);

        Assert.AreEqual(500, copy.Velocity);
        Assert.AreEqual(350, copy.TimeConstant);
        Assert.AreEqual(0.6, copy.Power);
        Assert.AreEqual(1.5, copy.RestDelta);
        Assert.AreEqual(-10, copy.Min);
        Assert.AreEqual(10, copy.Max);
    }

    [TestMethod]
    public void WithDelay_PreservesBaseFields()
    {
        var original = new BmTween
        {
            ColorSpace = BmColorSpace.Oklab,
            Repeat = BmRepeat.Mirror(),
            StaggerChildren = 0.05,
            DelayChildren = 0.2,
            Properties = new() { ["opacity"] = Bm.Tween(0.1) },
        };

        var copy = original.WithDelay(0.4);

        Assert.AreEqual(BmColorSpace.Oklab, copy.ColorSpace);
        Assert.AreEqual(original.Repeat, copy.Repeat);
        Assert.AreEqual(0.05, copy.StaggerChildren);
        Assert.AreEqual(0.2, copy.DelayChildren);
        Assert.IsNotNull(copy.Properties);
        Assert.IsTrue(copy.Properties!.ContainsKey("opacity"));

        // The copied map is independent: adding to it must not reach back into the original.
        copy.Properties["x"] = Bm.Tween(0.2);
        Assert.AreEqual(1, original.Properties!.Count);
    }

    [TestMethod]
    public void WithDelay_SurvivesLoweringToTheEngineConfig()
    {
        var config = Bm.Spring(stiffness: 250, damping: 22).WithDelay(0.6).ToConfig();

        Assert.AreEqual(0.6, config.Delay);
        Assert.AreEqual(250, config.Stiffness);
        Assert.AreEqual(22, config.Damping);
        Assert.AreEqual(BmotionTransitionType.Spring, config.Type);
    }
}
