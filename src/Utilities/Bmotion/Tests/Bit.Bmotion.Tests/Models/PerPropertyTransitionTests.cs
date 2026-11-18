namespace Bit.Bmotion.Tests.Models;

/// <summary>
/// Verifies per-property transitions flow to the engine config for transform components
/// (plan item 2.1, step 1 - independent transforms via <see cref="BmotionTransition.Properties"/>).
/// </summary>
[TestClass]
public class PerPropertyTransitionTests
{
    [TestMethod]
    public void TransformComponents_CanCarryIndependentTransitions()
    {
        var transition = new BmotionTween
        {
            Duration = 0.3,
            Properties = new()
            {
                ["x"] = Bm.Spring(stiffness: 300),
                ["scale"] = Bm.Tween(1.2, BmotionEase.BounceOut),
            },
        };

        var config = transition.ToConfig();
        Assert.IsNotNull(config.Properties);
        Assert.AreEqual(BmotionTransitionType.Spring, config.Properties!["x"].Type);
        Assert.AreEqual(BmotionEase.BounceOut, config.Properties["scale"].Ease);
        // The base transition still applies to any component without an override (e.g. rotate).
        Assert.AreEqual(0.3, config.Duration);
    }

    [TestMethod]
    public void PerPropertyTransition_IncludedInValueEquality()
    {
        var a = new BmotionTween { Properties = new() { ["x"] = Bm.Spring(stiffness: 300) } };
        var b = new BmotionTween { Properties = new() { ["x"] = Bm.Spring(stiffness: 300) } };
        var c = new BmotionTween { Properties = new() { ["x"] = Bm.Spring(stiffness: 100) } };

        Assert.IsTrue(BmotionTransition.AreEquivalent(a, b));
        Assert.IsFalse(BmotionTransition.AreEquivalent(a, c));
    }
}
