
namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class TweenDriverTests
{
    private static BmotionTweenDriver Create(double from, double to, BmotionTransitionConfig config, List<double> log)
        => new(from, to, config, v => log.Add(v));

    // ── Basic interpolation ───────────────────────────────────────────────────

    [TestMethod]
    public void Tick_FirstTick_AppliesFromValue()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear }, log);

        driver.Tick(0);

        Assert.AreEqual(0.0, log[0], 1e-5);
    }

    [TestMethod]
    public void Tick_MidAnimation_AppliesInterpolatedValue()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear }, log);

        driver.Tick(0);   // seeds startTime = 0
        driver.Tick(150); // elapsed = 150ms, t = 0.5 → value = 50

        Assert.AreEqual(50.0, log[1], 1e-1);
    }

    [TestMethod]
    public void Tick_AtDurationEnd_AppliesTargetAndReturnsTrue()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear }, log);

        driver.Tick(0);
        bool done = driver.Tick(300); // t = 1.0

        Assert.AreEqual(100.0, log[^1], 1e-5);
        Assert.IsTrue(done);
    }

    [TestMethod]
    public void Tick_ZeroDuration_CompletesImmediately()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0 }, log);

        bool done = driver.Tick(0);

        Assert.AreEqual(100.0, log[0], 1e-5);
        Assert.IsTrue(done);
    }

    [TestMethod]
    public void Tick_BeyondDuration_StillReturnsDone()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0.3 }, log);

        driver.Tick(0);
        bool done = driver.Tick(1000); // well past end

        Assert.IsTrue(done);
        Assert.AreEqual(100.0, log[^1], 1e-5);
    }

    // ── Delay ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_DuringDelay_AppliesFromValue()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0.3, Delay = 0.2 }, log);

        driver.Tick(0);   // seeds startTime = 200
        driver.Tick(100); // timestamp 100 < startTime 200 → still in delay

        Assert.AreEqual(0.0, log[0], 1e-5);
        Assert.AreEqual(0.0, log[1], 1e-5);
    }

    [TestMethod]
    public void Tick_AfterDelay_CompletesAtExpectedTime()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0.3, Delay = 0.2, Ease = BmEase.Linear }, log);

        driver.Tick(0);   // startTime = 200
        driver.Tick(200); // elapsed = 0 → value ≈ 0
        bool done = driver.Tick(500); // elapsed = 300ms, t = 1.0

        Assert.IsTrue(done);
        Assert.AreEqual(100.0, log[^1], 1e-5);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Cancel_CompletesImmediately()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0.3 }, log);

        driver.Tick(0);
        int logCountAfterFirstTick = log.Count;
        driver.Cancel();
        bool done = driver.Tick(150);

        // Cancel() freezes the animation in place rather than snapping to the target;
        // only completion is guaranteed.
        Assert.IsTrue(done);
        // A cancelled Tick must exit before _apply, so no new value is emitted after Cancel().
        Assert.AreEqual(logCountAfterFirstTick, log.Count);
    }

    // ── Repeat ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_RepeatOnce_PlaysAnimationTwiceBeforeFinishing()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear, Repeat = 1 }, log);

        driver.Tick(0);
        bool done1 = driver.Tick(300); // end of first pass → repeat, returns false
        bool done2 = driver.Tick(600); // end of second pass → done, returns true

        Assert.IsFalse(done1);
        Assert.IsTrue(done2);
    }

    [TestMethod]
    public void Tick_MirrorRepeat_SecondPassIsReversed()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig
        {
            Duration = 0.3,
            Ease = BmEase.Linear,
            Repeat = 1,
            RepeatType = BmRepeatType.Mirror,
        }, log);

        driver.Tick(0);   // value = 0
        driver.Tick(300); // value = 100, mirrors (from↔to swapped)
        driver.Tick(450); // midpoint of reversed pass: value ≈ 50
        driver.Tick(600); // end of reversed pass: value = 0

        Assert.AreEqual(0.0, log[^1], 1e-1);
    }

    [TestMethod]
    public void Tick_InfiniteRepeat_NeverReturnsDone()
    {
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig
        {
            Duration = 0.3,
            Repeat = int.MaxValue,
        }, log);

        driver.Tick(0);
        for (int i = 1; i <= 10; i++)
        {
            bool done = driver.Tick(i * 300.0);
            Assert.IsFalse(done, $"Unexpected completion after iteration {i}");
        }
    }

    [TestMethod]
    public void Tick_RepeatInfiniteFlag_NeverReturnsDone()
    {
        // Exercises the preferred RepeatInfinite flag directly rather than the legacy
        // Repeat = int.MaxValue sentinel covered above.
        var log = new List<double>();
        var driver = Create(0, 100, new BmotionTransitionConfig
        {
            Duration = 0.3,
            RepeatInfinite = true,
        }, log);

        driver.Tick(0);
        for (int i = 1; i <= 10; i++)
        {
            bool done = driver.Tick(i * 300.0);
            Assert.IsFalse(done, $"Unexpected completion after iteration {i}");
        }
    }
}
