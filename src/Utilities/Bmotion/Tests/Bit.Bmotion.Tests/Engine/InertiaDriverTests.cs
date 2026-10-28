
namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class InertiaDriverTests
{
    // ── Motion ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_MovesTowardProjectedTarget()
    {
        var values = new List<double>();
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = 1000, // px/s
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
        };
        var driver = new BmotionInertiaDriver(0, config, v => values.Add(v));

        driver.Tick(0);   // pos = 0 (no elapsed time yet)
        driver.Tick(100); // ~64ms capped → pos > 0

        Assert.IsTrue(values.Count >= 2);
        Assert.IsTrue(values[1] > values[0], "Inertia should move toward projected target");
    }

    [TestMethod]
    public void Tick_EventuallySettlesAtProjectedTarget()
    {
        double lastValue = 0;
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = 500,
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
        };
        var driver = new BmotionInertiaDriver(0, config, v => lastValue = v);

        bool done = false;
        double ts = 0;
        while (!done && ts < 10_000)
        {
            ts += 16.67;
            done = driver.Tick(ts);
        }

        Assert.IsTrue(done, "Inertia should settle");
        double projected = 0 + 0.8 * 500;
        Assert.AreEqual(projected, lastValue, 1e-5); // snaps to _projected exactly on settle
    }

    // ── Bounds clamping ───────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_ClampsToMaxBound()
    {
        double lastValue = 0;
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = 100_000, // projected would be huge
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
            InertiaMax = 100.0,
        };
        var driver = new BmotionInertiaDriver(0, config, v => lastValue = v);

        bool done = false;
        double ts = 0;
        while (!done && ts < 10_000)
        {
            ts += 16.67;
            done = driver.Tick(ts);
        }

        Assert.IsTrue(done);
        Assert.AreEqual(100.0, lastValue, 1e-5);
    }

    [TestMethod]
    public void Tick_ClampsToMinBound()
    {
        double lastValue = 0;
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = -100_000, // large negative velocity
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
            InertiaMin = -100.0,
        };
        var driver = new BmotionInertiaDriver(0, config, v => lastValue = v);

        bool done = false;
        double ts = 0;
        while (!done && ts < 10_000)
        {
            ts += 16.67;
            done = driver.Tick(ts);
        }

        Assert.IsTrue(done);
        Assert.AreEqual(-100.0, lastValue, 1e-5);
    }

    // ── Delay ─────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_DuringDelay_HoldsAtStart()
    {
        var values = new List<double>();
        var config = new BmotionTransitionConfig
        {
            Delay = 0.3, // 300 ms
            InertiaVelocity = 1000,
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
        };
        var driver = new BmotionInertiaDriver(0, config, v => values.Add(v));

        driver.Tick(0);
        driver.Tick(100); // still within 300 ms delay

        Assert.AreEqual(0.0, values[0], 1e-5);
        Assert.AreEqual(0.0, values[1], 1e-5);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Cancel_CompletesImmediately()
    {
        double lastValue = 0;
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = 1000,
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
        };
        int emitted = 0;
        var driver = new BmotionInertiaDriver(0, config, v => { lastValue = v; emitted++; });

        driver.Tick(0);
        int emittedAfterFirstTick = emitted;
        double valueAtCancel = lastValue;
        driver.Cancel();
        bool done = driver.Tick(16);

        // Cancel() freezes in place rather than snapping to the projected target;
        // only completion is guaranteed.
        Assert.IsTrue(done);
        // A cancelled Tick must exit before applying, so the frozen value is never overwritten.
        Assert.AreEqual(emittedAfterFirstTick, emitted);
        Assert.AreEqual(valueAtCancel, lastValue);
    }

    // ── ModifyTarget (snap-to-grid) ───────────────────────────────────────────

    private static double RunToRest(BmotionTransitionConfig config, double from = 0)
    {
        double lastValue = from;
        var driver = new BmotionInertiaDriver(from, config, v => lastValue = v);
        bool done = false;
        double ts = 0;
        while (!done && ts < 20_000)
        {
            ts += 16.67;
            done = driver.Tick(ts);
        }
        Assert.IsTrue(done, "Inertia should settle");
        return lastValue;
    }

    [TestMethod]
    public void ModifyTarget_SnapsProjectedTargetToGrid()
    {
        // Unmodified the coast would rest at 0.8 * 530 = 424; snapping to a 100px grid lands on 400.
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = 530,
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
            ModifyTarget = Bm.SnapTo(100),
        };

        Assert.AreEqual(400.0, RunToRest(config), 1e-5);
    }

    [TestMethod]
    public void ModifyTarget_RunsBeforeBoundsClamping()
    {
        // The snap would overshoot to 500, but the Max bound still wins - a snapped target must
        // never escape the drag constraints.
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = 600,
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
            InertiaMax = 300.0,
            ModifyTarget = _ => 500.0,
        };

        Assert.AreEqual(300.0, RunToRest(config), 1e-5);
    }

    [TestMethod]
    public void ModifyTarget_NonFiniteResult_IsIgnored()
    {
        // A consumer's snap function returning NaN would otherwise poison the decay math and make
        // the rest test unsatisfiable (the driver would tick forever).
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = 500,
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
            ModifyTarget = _ => double.NaN,
        };

        Assert.AreEqual(400.0, RunToRest(config), 1e-5);
    }

    [TestMethod]
    public void Clone_CarriesModifyTarget()
    {
        var config = new BmotionTransitionConfig { ModifyTarget = Bm.SnapTo(50) };

        Assert.AreSame(config.ModifyTarget, config.Clone().ModifyTarget);
    }

    // ── Zero velocity ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_ZeroVelocity_CompletesImmediately()
    {
        var values = new List<double>();
        var config = new BmotionTransitionConfig
        {
            InertiaVelocity = 0,
            Power = 0.8,
            TimeConstant = 700,
            InertiaRestDelta = 0.5,
        };
        var driver = new BmotionInertiaDriver(0, config, v => values.Add(v));

        // projected = 0 + 0.8*0 = 0; |projected - pos| = 0 < 0.5 → done on first non-zero elapsed tick
        driver.Tick(0);
        bool done = driver.Tick(16);

        Assert.IsTrue(done);
        Assert.AreEqual(0.0, values[^1], 1e-5);
    }

    // ── Bouncing off the bounds ───────────────────────────────────────────────

    // Every value the driver shows, one 60 Hz frame at a time, until it settles.
    private static List<double> Run(double from, BmotionTransitionConfig config)
    {
        var values = new List<double>();
        var driver = new BmotionInertiaDriver(from, config, values.Add);
        bool done = false;
        for (double ts = 0; !done && ts < 10_000; ts += 1000.0 / 60) done = driver.Tick(ts);
        Assert.IsTrue(done, "the inertia never settled");
        return values;
    }

    [TestMethod]
    public void AThrowTowardsABound_KeepsItsSpeedUntilItGetsThere()
    {
        // motion.dev semantics: the coast heads for where the throw would come to rest, and the bound is enforced
        // when it gets there - not by braking from the start to stop on it.
        var free = Run(0, new BmotionTransitionConfig { InertiaVelocity = 1000 });
        var bounded = Run(0, new BmotionTransitionConfig { InertiaVelocity = 1000, InertiaMax = 300 });

        for (int i = 0; i < 10; i++)
            Assert.AreEqual(free[i], bounded[i], 1e-9, $"frame {i}: the bound slowed the throw down before it was reached");
    }

    [TestMethod]
    public void AThrowPastABound_BouncesOffIt_AndSettlesOnIt()
    {
        var values = Run(0, new BmotionTransitionConfig { InertiaVelocity = 1000, InertiaMax = 300 });

        Assert.IsTrue(values.Max() > 300.5, $"it stopped dead at the bound (max {values.Max():0.##})");
        Assert.AreEqual(300, values[^1], 1e-9, "and settles on it");
    }

    [TestMethod]
    public void AStiffCriticallyDampedWall_StopsNearTheBound_WithoutBouncing()
    {
        // ζ = 1: the spring brings it straight back - within the couple of pixels a 60 Hz step of so stiff a spring
        // rings by - rather than bouncing.
        var values = Run(0, new BmotionTransitionConfig
        {
            InertiaVelocity = 1000, InertiaMax = 300, BounceStiffness = 20_000, BounceDamping = 2 * Math.Sqrt(20_000),
        });

        int peak = values.IndexOf(values.Max());
        Assert.IsTrue(values.Max() < 300 + 15, $"it went {values.Max() - 300:0.##}px past a stiff wall");
        for (int i = peak + 1; i < values.Count; i++)
            Assert.IsTrue(Math.Abs(values[i] - 300) <= 2, $"frame {i}: it bounced ({values[i]:0.###})");
        Assert.AreEqual(300, values[^1], 1e-9);
    }

    [TestMethod]
    public void AVeryHeavilyDampedWall_StaysFinite()
    {
        // Explicit damping this heavy would flip and amplify the velocity every step until it is NaN.
        var values = new List<double>();
        var driver = new BmotionInertiaDriver(0, new BmotionTransitionConfig
        {
            InertiaVelocity = 1000, InertiaMax = 300, BounceStiffness = 1_000_000, BounceDamping = 10_000_000,
        }, values.Add);
        for (double ts = 0; ts < 3000; ts += 1000.0 / 60) driver.Tick(ts);

        Assert.IsTrue(values.All(double.IsFinite));
        Assert.IsTrue(values.Max() < 330, $"it went {values.Max() - 300:0.##}px past the wall");
    }

    [TestMethod]
    public void ReleasedPastABound_ComesBack_FromWhereItIs()
    {
        // A drag pulled 10px past its constraint and let go moving fast further out: it carries on a little, turns, and
        // settles on the bound - never jumps to it.
        var values = Run(310, new BmotionTransitionConfig { InertiaVelocity = 3000, InertiaMax = 300 });

        Assert.IsTrue(Math.Abs(values[0] - 310) < 1e-9, "it starts where it was let go");
        Assert.IsTrue(values.Skip(1).Take(3).Max() > 315, "the outward velocity carries it on for a moment");
        Assert.AreEqual(300, values[^1], 1e-9);
        for (int i = 1; i < values.Count; i++)
            Assert.IsTrue(Math.Abs(values[i] - values[i - 1]) < 3000.0 / 60 + 5, $"frame {i}: jumped {values[i] - values[i - 1]:0.##}px");
    }

    [TestMethod]
    public void ABounceSettles_AtThePixelScale_NotTheDistanceScale()
    {
        // The spring scales its rest thresholds by the distance it covers; released 200px out, an unscaled 0.5px
        // threshold would become 100px and it would snap home from far away.
        var values = Run(500, new BmotionTransitionConfig { InertiaVelocity = 0, InertiaMax = 300 });

        Assert.AreEqual(300, values[^1], 1e-9);
        Assert.IsTrue(Math.Abs(values[^2] - 300) < 1, $"it snapped home from {values[^2] - 300:0.##}px away");
    }

    [TestMethod]
    public void Complete_LandsOnTheBound()
    {
        double last = double.NaN;
        var driver = new BmotionInertiaDriver(0, new BmotionTransitionConfig { InertiaVelocity = 1000, InertiaMax = 300 }, v => last = v);
        driver.Complete();
        Assert.AreEqual(300, last);
    }

    [TestMethod]
    public void BmInertia_CarriesItsBounce_IntoTheConfig()
    {
        var inertia = new BmInertia { BounceStiffness = 250, BounceDamping = 30 };
        var config = inertia.ToConfig();

        Assert.AreEqual(250, config.BounceStiffness);
        Assert.AreEqual(30, config.BounceDamping);
        Assert.AreEqual(250, config.Clone().BounceStiffness);
        Assert.IsNull(new BmInertia().ToConfig().BounceStiffness, "unset means the context's default");
    }
}
