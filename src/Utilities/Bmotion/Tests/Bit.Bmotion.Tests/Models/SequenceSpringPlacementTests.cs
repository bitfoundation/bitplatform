namespace Bit.Bmotion.Tests.Models;

/// <summary>A default-positioned segment after a spring starts when that spring's own physics says it is done.</summary>
[TestClass]
public class SequenceSpringPlacementTests
{
    [TestMethod]
    public void AfterAPhysicalSpring_TheNextSegmentWaitsForItsPhysics_NotAFlatGuess()
    {
        // Critically damped, ω₀ = 20: 4 / (ζ·ω₀) = 0.2 s. It used to be placed 0.8 s in, whatever the spring.
        var stiff = new BmSequence()
            .Add("#a", Bm.To(x: 1), Bm.Spring(stiffness: 400, damping: 40))
            .Add("#a", Bm.To(y: 1), Bm.Tween(0.3));
        Assert.AreEqual(0.2, stiff.Segments[1].Start, 1e-9);

        // ζ·ω₀ = 1: a loose spring takes 4 s, and the next segment no longer starts while it is still swinging.
        var loose = new BmSequence()
            .Add("#a", Bm.To(x: 1), Bm.Spring(stiffness: 25, damping: 2))
            .Add("#a", Bm.To(y: 1), Bm.Tween(0.3));
        Assert.AreEqual(4, loose.Segments[1].Start, 1e-9);
    }

    [TestMethod]
    public void TheDefaultSpring_KeepsItsPlacement()
    {
        var seq = new BmSequence()
            .Add("#a", Bm.To(x: 1), Bm.Spring())
            .Add("#a", Bm.To(y: 1));
        Assert.AreEqual(0.8, seq.Segments[1].Start, 1e-9);
    }

    [TestMethod]
    public void ADurationSpring_IsPlacedByItsVisualDuration_AndItsDelay()
    {
        var seq = new BmSequence()
            .Add("#a", Bm.To(x: 1), Bm.Spring(bounce: 0.3, duration: 0.4, delay: 0.1))
            .Add("#a", Bm.To(y: 1))
            .Add("#b", Bm.To(x: 1), Bm.Spring(bounce: 0.3))   // no duration: the 0.5 s default
            .Add("#b", Bm.To(y: 1));
        Assert.AreEqual(0.5, seq.Segments[1].Start, 1e-9);
        Assert.AreEqual(0.8 + 0.5, seq.Segments[3].Start, 1e-9);
    }
}
