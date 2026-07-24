namespace Bit.Bmotion.Tests.Models;

/// <summary>
/// Verifies per-property transitions flow to the engine config for transform components
/// (plan item 2.1, step 1 - independent transforms via <see cref="BmTransition.Properties"/>).
/// </summary>
[TestClass]
public class PerPropertyTransitionTests
{
    [TestMethod]
    public void TransformComponents_CanCarryIndependentTransitions()
    {
        var transition = new BmTween
        {
            Duration = 0.3,
            Properties = new()
            {
                ["x"] = Bm.Spring(stiffness: 300),
                ["scale"] = Bm.Tween(1.2, BmEase.BounceOut),
            },
        };

        var config = transition.ToConfig();
        Assert.IsNotNull(config.Properties);
        Assert.AreEqual(BmotionTransitionType.Spring, config.Properties!["x"].Type);
        Assert.AreEqual(BmEase.BounceOut, config.Properties["scale"].Ease);
        // The base transition still applies to any component without an override (e.g. rotate).
        Assert.AreEqual(0.3, config.Duration);
    }

    [TestMethod]
    public void PerPropertyTransition_IncludedInValueEquality()
    {
        var a = new BmTween { Properties = new() { ["x"] = Bm.Spring(stiffness: 300) } };
        var b = new BmTween { Properties = new() { ["x"] = Bm.Spring(stiffness: 300) } };
        var c = new BmTween { Properties = new() { ["x"] = Bm.Spring(stiffness: 100) } };

        Assert.IsTrue(BmTransition.AreEquivalent(a, b));
        Assert.IsFalse(BmTransition.AreEquivalent(a, c));
    }
}
