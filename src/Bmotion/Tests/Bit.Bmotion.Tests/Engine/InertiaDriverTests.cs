
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
        var driver = new BmotionInertiaDriver(0, config, v => lastValue = v);

        driver.Tick(0);
        driver.Cancel();
        bool done = driver.Tick(16);

        // Cancel() freezes in place rather than snapping to the projected target;
        // only completion is guaranteed.
        Assert.IsTrue(done);
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
}
