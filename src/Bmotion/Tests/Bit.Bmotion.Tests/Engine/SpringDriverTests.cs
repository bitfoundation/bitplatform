
namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class SpringDriverTests
{
    // ── Settling ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_EventuallySettlesAtTarget()
    {
        double lastValue = double.NaN;
        var config = new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 20,
            Mass = 1,
            RestSpeed = 0.01,
            RestDelta = 0.01,
        };
        var driver = new BmotionSpringDriver(0, 100, config, v => lastValue = v);

        bool done = false;
        double ts = 0;
        while (!done && ts < 10_000)
        {
            ts += 16.67; // ~60 fps
            done = driver.Tick(ts);
        }

        Assert.IsTrue(done, "Spring did not settle within 10 s");
        Assert.AreEqual(100.0, lastValue, 1e-2);
    }

    [TestMethod]
    public void Tick_OverdampedSpring_DoesNotOvershoot()
    {
        // With damping >> critical damping the position should be monotonically increasing
        double prevValue = -1;
        var config = new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 100,
            Mass = 1,
        };
        var driver = new BmotionSpringDriver(0, 100, config, v =>
        {
            // Allow a tiny floating-point tolerance
            Assert.IsTrue(v >= prevValue - 1e-6, $"Overshoot detected: {v} < {prevValue}");
            prevValue = v;
        });

        double ts = 0;
        while (ts < 5_000)
        {
            ts += 16.67;
            if (driver.Tick(ts)) break;
        }

        Assert.IsTrue(prevValue > 0, "Spring never moved");
    }

    [TestMethod]
    public void Tick_UnderdampedSpring_OscillatesAndSettles()
    {
        double lastValue = 0;
        var config = new BmotionTransitionConfig
        {
            Stiffness = 200,
            Damping = 5, // very low damping → will oscillate
            Mass = 1,
            RestSpeed = 0.01,
            RestDelta = 0.01,
        };
        var driver = new BmotionSpringDriver(0, 100, config, v => lastValue = v);

        bool done = false;
        double ts = 0;
        while (!done && ts < 30_000)
        {
            ts += 16.67;
            done = driver.Tick(ts);
        }

        Assert.IsTrue(done, "Under-damped spring did not settle");
        Assert.AreEqual(100.0, lastValue, 1e-2);
    }

    // ── Delay ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_DuringDelay_HoldsAtFromValue()
    {
        var values = new List<double>();
        var config = new BmotionTransitionConfig
        {
            Delay = 0.2,  // 200 ms
            Stiffness = 100,
            Damping = 20,
            Mass = 1,
        };
        var driver = new BmotionSpringDriver(0, 100, config, v => values.Add(v));

        driver.Tick(0);   // startTs = 0; elapsed=0 < 200 ms delay
        driver.Tick(100); // still in delay

        Assert.AreEqual(0.0, values[0], 1e-5);
        Assert.AreEqual(0.0, values[1], 1e-5);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Cancel_CompletesImmediately()
    {
        double lastValue = 0;
        var driver = new BmotionSpringDriver(0, 100, new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 10,
            Mass = 1,
        }, v => lastValue = v);

        driver.Tick(0);
        driver.Tick(16);
        driver.Cancel();
        bool done = driver.Tick(32);

        // Cancel() freezes the animation in place; it does not snap to the target. Only
        // completion is guaranteed (Complete() is the operation that writes the end value).
        Assert.IsTrue(done);
    }

    // ── Initial velocity ──────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_PositiveInitialVelocity_MovesImmediatelyTowardTarget()
    {
        var values = new List<double>();
        var config = new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 10,
            Mass = 1,
            Velocity = 500, // toward target (0 → 100)
        };
        var driver = new BmotionSpringDriver(0, 100, config, v => values.Add(v));

        driver.Tick(0);
        driver.Tick(16);

        Assert.IsTrue(values[1] > values[0], "Spring with positive velocity should move toward target immediately");
    }

    // ── Repeat ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_NoRepeat_CompletesAfterSingleSettle()
    {
        // Regression guard: Repeat = 0 must still finish on the first settle.
        var config = new BmotionTransitionConfig { Stiffness = 100, Damping = 20, Mass = 1 };
        var driver = new BmotionSpringDriver(0, 100, config, _ => { });

        int settleTicks = RunUntilComplete(driver);

        Assert.IsTrue(settleTicks > 0 && settleTicks < 600);
    }

    [TestMethod]
    public void Tick_RepeatLoop_ReplaysFromOriginBeforeCompleting()
    {
        var values = new List<double>();
        var config = new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 20,
            Mass = 1,
            Repeat = 1,
            RepeatType = BmotionRepeatType.Loop,
        };
        var driver = new BmotionSpringDriver(0, 100, config, v => values.Add(v));

        bool done = false;
        bool reachedTarget = false;
        bool resetAfterTarget = false;
        double ts = 0;
        while (!done && ts < 30_000)
        {
            ts += 16.67;
            done = driver.Tick(ts);
            double v = values[^1];
            if (!reachedTarget && v >= 99) reachedTarget = true;
            else if (reachedTarget && v <= 50) resetAfterTarget = true; // Loop snapped back to origin
        }

        Assert.IsTrue(done, "Repeating spring never completed");
        Assert.IsTrue(resetAfterTarget, "Loop repeat should replay from the origin (value dropped back toward 0)");
        Assert.AreEqual(100.0, values[^1], 1e-2); // final cycle still settles at the target
    }

    [TestMethod]
    public void Tick_RepeatMirror_PingPongsBackToStart()
    {
        var config = new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 20,
            Mass = 1,
            Repeat = 1,
            RepeatType = BmotionRepeatType.Mirror,
        };
        double lastValue = double.NaN;
        var driver = new BmotionSpringDriver(0, 100, config, v => lastValue = v);

        bool done = false;
        double ts = 0;
        while (!done && ts < 30_000)
        {
            ts += 16.67;
            done = driver.Tick(ts);
        }

        Assert.IsTrue(done, "Mirror-repeating spring never completed");
        // 0 → 100 (cycle 1) then mirrored 100 → 0 (cycle 2): settles back at the origin.
        Assert.AreEqual(0.0, lastValue, 1e-2);
    }

    [TestMethod]
    public void Tick_InfiniteRepeat_NeverCompletes()
    {
        var config = new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 20,
            Mass = 1,
            RepeatInfinite = true,
            RepeatType = BmotionRepeatType.Loop,
        };
        var driver = new BmotionSpringDriver(0, 100, config, _ => { });

        bool done = false;
        double ts = 0;
        for (int i = 0; i < 2000 && !done; i++) // ~33 s of frames
        {
            ts += 16.67;
            done = driver.Tick(ts);
        }

        Assert.IsFalse(done, "Infinite-repeat spring should never report completion");
    }

    // ── Constructor safety guards (non-terminating spring prevention) ─────────

    [TestMethod]
    public void Tick_NonPositiveMass_StillTerminatesAtTarget()
    {
        // A Mass <= 0 would divide the acceleration into NaN/Infinity and trap the spring forever.
        // The constructor falls back to a positive mass, so the spring must still settle.
        double lastValue = double.NaN;
        var config = new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 20,
            Mass = 0, // non-positive: must be guarded
            RestSpeed = 0.01,
            RestDelta = 0.01,
        };
        var driver = new BmotionSpringDriver(0, 100, config, v => lastValue = v);

        int ticks = RunUntilComplete(driver);

        Assert.IsTrue(ticks > 0, "Spring with non-positive Mass never terminated");
        Assert.AreEqual(100.0, lastValue, 1e-2);
    }

    [TestMethod]
    public void Tick_NonPositiveRestThresholds_StillTerminates()
    {
        // Non-positive RestSpeed/RestDelta would make the at-rest gate unsatisfiable, leaving the
        // spring ticking forever. The constructor clamps them to a positive floor, so it must end.
        double lastValue = double.NaN;
        var config = new BmotionTransitionConfig
        {
            Stiffness = 100,
            Damping = 20,
            Mass = 1,
            RestSpeed = 0,  // non-positive: must be guarded
            RestDelta = 0,  // non-positive: must be guarded
        };
        var driver = new BmotionSpringDriver(0, 100, config, v => lastValue = v);

        int ticks = RunUntilComplete(driver);

        Assert.IsTrue(ticks > 0, "Spring with non-positive rest thresholds never terminated");
        Assert.AreEqual(100.0, lastValue, 1e-1);
    }

    private static int RunUntilComplete(BmotionSpringDriver driver, double maxMs = 30_000)
    {
        int ticks = 0;
        double ts = 0;
        while (ts < maxMs)
        {
            ts += 16.67;
            ticks++;
            if (driver.Tick(ts)) return ticks;
        }
        return -1;
    }
}
