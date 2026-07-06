
namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class ColorTweenDriverTests
{
    // ── Basic interpolation ───────────────────────────────────────────────────

    [TestMethod]
    public void Tick_FirstTick_AppliesFromColor()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear },
            v => lastValue = v);

        driver.Tick(0); // t=0 → from color

        Assert.AreEqual("rgba(0,0,0,1)", lastValue);
    }

    [TestMethod]
    public void Tick_AtMidpoint_InterpolatesColor()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear },
            v => lastValue = v);

        driver.Tick(0);
        driver.Tick(150); // t=0.5 → rgba(128,128,128,1)

        Assert.AreEqual("rgba(128,128,128,1)", lastValue);
    }

    [TestMethod]
    public void Tick_AtEnd_AppliesTargetColorAndReturnsTrue()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear },
            v => lastValue = v);

        driver.Tick(0);
        bool done = driver.Tick(300); // t=1.0

        Assert.AreEqual("rgba(255,255,255,1)", lastValue);
        Assert.IsTrue(done);
    }

    [TestMethod]
    public void Tick_ZeroDuration_CompletesImmediately()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig { Duration = 0 },
            v => lastValue = v);

        bool done = driver.Tick(0);

        Assert.AreEqual("rgba(255,255,255,1)", lastValue);
        Assert.IsTrue(done);
    }

    // ── Delay ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_DuringDelay_AppliesFromColor()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig { Duration = 0.3, Delay = 0.2, Ease = BmEase.Linear },
            v => lastValue = v);

        driver.Tick(0);   // seeds startTime = 200
        driver.Tick(100); // still in delay

        // During delay the raw from-string is applied (not the interpolated form)
        Assert.AreEqual("#000000", lastValue);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Cancel_CompletesImmediately()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig { Duration = 0.3 },
            v => lastValue = v);

        driver.Tick(0);
        driver.Cancel();
        bool done = driver.Tick(100);

        // Cancel() freezes in place rather than snapping to the target string;
        // only completion is guaranteed.
        Assert.IsTrue(done);
    }

    // ── Repeat / Mirror ───────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_RepeatOnce_PlaysAnimationTwice()
    {
        var log = new List<string>();
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear, Repeat = 1 },
            v => log.Add(v));

        driver.Tick(0);
        bool done1 = driver.Tick(300); // end of first pass
        bool done2 = driver.Tick(600); // end of second pass

        Assert.IsFalse(done1);
        Assert.IsTrue(done2);
    }

    [TestMethod]
    public void Tick_MirrorRepeat_SecondPassReturnsToFirstColor()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Mirror,
            },
            v => lastValue = v);

        driver.Tick(0);   // value → rgba(0,0,0,1) at t=0
        driver.Tick(300); // end of first pass → rgba(255,255,255,1), mirrors
        driver.Tick(600); // end of reversed pass → rgba(0,0,0,1)

        Assert.AreEqual("rgba(0,0,0,1)", lastValue);
    }

    // ── Complete() terminal state ─────────────────────────────────────────────

    [TestMethod]
    public void Complete_MirrorRepeatOnce_SnapsBackToFirstColor()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Mirror,
            },
            v => lastValue = v);

        // Two passes (forward + mirrored) end back on the start; Complete() snaps there directly.
        driver.Complete();

        Assert.AreEqual("#000000", lastValue);
    }

    [TestMethod]
    public void Complete_ReverseNoRepeat_SnapsToTargetColor()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                RepeatType = BmRepeatType.Reverse,
            },
            v => lastValue = v);

        // A single forward pass ends on the target colour.
        driver.Complete();

        Assert.AreEqual("#ffffff", lastValue);
    }

    [TestMethod]
    public void Complete_ReverseRepeatOnce_SnapsBackToFirstColor()
    {
        string? lastValue = null;
        var driver = new BmotionColorTweenDriver(
            "#000000", "#ffffff",
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Reverse,
            },
            v => lastValue = v);

        // Forward once then replayed reversed → ends on the start colour.
        driver.Complete();

        Assert.AreEqual("#000000", lastValue);
    }
}
