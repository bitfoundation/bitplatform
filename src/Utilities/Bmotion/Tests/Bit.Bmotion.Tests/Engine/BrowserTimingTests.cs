namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for <see cref="BmotionBrowserTiming"/>: the rules the compositor offload and prerendered CSS
/// animations share for what the browser can play.
/// </summary>
[TestClass]
public class BrowserTimingTests
{
    [TestMethod]
    public void CanPlay_EvenlySpacedTimes()
    {
        Assert.IsTrue(BmotionBrowserTiming.CanPlay(Bm.Tween(0.5, times: [0, 0.5, 1]).ToConfig()));
        Assert.IsFalse(BmotionBrowserTiming.CanPlay(Bm.Tween(0.5, times: [0, 0.2, 1]).ToConfig()));
    }

    [TestMethod]
    public void CanPlay_RejectsWhatTheBrowserCantReproduce()
    {
        Assert.IsFalse(BmotionBrowserTiming.CanPlay(Bm.Inertia(velocity: 500).ToConfig()));
        Assert.IsFalse(BmotionBrowserTiming.CanPlay(Bm.Tween(0).ToConfig()));
        Assert.IsFalse(BmotionBrowserTiming.CanPlay(Bm.Tween(0.5, repeat: BmRepeat.Reverse()).ToConfig()));
        Assert.IsTrue(BmotionBrowserTiming.CanPlay(Bm.Spring(stiffness: 300, damping: 20).ToConfig()));
    }

    [TestMethod]
    public void SpringSamples_AreSharedBetweenIdenticalSprings()
    {
        var a = BmotionBrowserTiming.SampleSpringProgress(Bm.Spring(stiffness: 321, damping: 17).ToConfig());
        var b = BmotionBrowserTiming.SampleSpringProgress(Bm.Spring(stiffness: 321, damping: 17).ToConfig());

        Assert.IsNotNull(a);
        Assert.AreSame(a.Value.Samples, b!.Value.Samples);
    }

    [TestMethod]
    public void Easing_PrefersAnExactKeyword_OnlyWhenAsked()
    {
        var config = Bm.Tween(0.5, ease: BmEase.Linear).ToConfig();
        var samples = BmotionBrowserTiming.SampleProgress(config)!.Value.Samples;

        Assert.AreEqual("linear", BmotionBrowserTiming.ChooseEasing(config, samples, linearSupported: true, preferKeyword: true));
        StringAssert.StartsWith(BmotionBrowserTiming.ChooseEasing(config, samples, linearSupported: true, preferKeyword: false), "linear(");
        Assert.AreEqual("linear", BmotionBrowserTiming.ChooseEasing(config, samples, linearSupported: false, preferKeyword: false));
    }
}
