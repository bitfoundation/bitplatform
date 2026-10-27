using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Models;

/// <summary>
/// Tests for the standalone value utilities on <see cref="Bm"/> (clamp / wrap / mix / range
/// mapping / velocity) and the non-clamping range map on <see cref="BmValue{T}"/>.
/// </summary>
[TestClass]
public class ValueUtilityTests
{
    [TestMethod]
    public void Clamp_ConstrainsToTheInclusiveRange()
    {
        Assert.AreEqual(0, Bm.Clamp(0, 10, -5));
        Assert.AreEqual(10, Bm.Clamp(0, 10, 42));
        Assert.AreEqual(4, Bm.Clamp(0, 10, 4));
    }

    [TestMethod]
    public void Wrap_LoopsInsteadOfClamping()
    {
        Assert.AreEqual(0, Bm.Wrap(0, 10, 10));    // the upper bound wraps to the lower one
        Assert.AreEqual(2, Bm.Wrap(0, 10, 12));
        Assert.AreEqual(8, Bm.Wrap(0, 10, -2));    // negatives wrap round rather than mirroring
        Assert.AreEqual(-4, Bm.Wrap(-5, 5, 6));
    }

    [TestMethod]
    public void Wrap_ZeroWidthRange_ReturnsTheBound_InsteadOfDividingByZero()
    {
        Assert.AreEqual(3, Bm.Wrap(3, 3, 99));
    }

    [TestMethod]
    public void Mix_InterpolatesLinearly()
    {
        Assert.AreEqual(0, Bm.Mix(0, 100, 0));
        Assert.AreEqual(50, Bm.Mix(0, 100, 0.5));
        Assert.AreEqual(100, Bm.Mix(0, 100, 1));
        Assert.AreEqual(150, Bm.Mix(0, 100, 1.5)); // progress is not clamped
    }

    [TestMethod]
    public void MapRange_ClampsByDefault()
    {
        Assert.AreEqual(0, Bm.MapRange(-10, [0, 100], [0, 1]));
        Assert.AreEqual(1, Bm.MapRange(200, [0, 100], [0, 1]));
        Assert.AreEqual(0.25, Bm.MapRange(25, [0, 100], [0, 1]), 1e-9);
    }

    [TestMethod]
    public void MapRange_WithoutClamp_ExtrapolatesTheOutermostSegments()
    {
        Assert.AreEqual(-0.1, Bm.MapRange(-10, [0, 100], [0, 1], clamp: false), 1e-9);
        Assert.AreEqual(2, Bm.MapRange(200, [0, 100], [0, 1], clamp: false), 1e-9);
    }

    [TestMethod]
    public void MapRange_MultiPointRange_UsesTheContainingSegment()
    {
        double[] input = [0, 50, 100];
        double[] output = [0, 10, 0];
        Assert.AreEqual(5, Bm.MapRange(25, input, output), 1e-9);
        Assert.AreEqual(10, Bm.MapRange(50, input, output), 1e-9);
        Assert.AreEqual(5, Bm.MapRange(75, input, output), 1e-9);
    }

    [TestMethod]
    public void MapRange_RejectsMalformedRanges()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Bm.MapRange(0, [0, 1], [0]));
        Assert.ThrowsExactly<ArgumentException>(() => Bm.MapRange(0, [0], [0]));
        Assert.ThrowsExactly<ArgumentException>(() => Bm.MapRange(0, [1, 0], [0, 1]));
    }

    [TestMethod]
    public void Transform_NonClamping_ExtrapolatesOnTheDerivedValue()
    {
        var source = Bm.Value(0.0);
        var derived = source.Transform([0, 100], [0, 1], clamp: false);

        source.SetSync(200);
        Assert.AreEqual(2, derived.Value, 1e-9);

        source.SetSync(-50);
        Assert.AreEqual(-0.5, derived.Value, 1e-9);
    }

    [TestMethod]
    public void Transform_ClampingRemainsTheDefault()
    {
        var source = Bm.Value(0.0);
        var derived = source.Transform([0, 100], [0, 1]);

        source.SetSync(200);
        Assert.AreEqual(1, derived.Value, 1e-9);
    }

    [TestMethod]
    public void Velocity_TracksTheSourcesRateOfChange()
    {
        var source = Bm.Value(0.0);
        long now = 0;
        source.TimeSource = () => now;

        var velocity = Bm.Velocity(source);

        now = 0; source.SetSync(0);       // seeds the clock
        now = 100; source.SetSync(10);    // +10 units over 0.1s ⇒ 100 units/sec
        Assert.AreEqual(100, velocity.Value, 1e-6);

        now = 200; source.SetSync(10);    // no movement ⇒ zero velocity
        Assert.AreEqual(0, velocity.Value, 1e-6);
    }

    [TestMethod]
    public void Velocity_SettlesToZero_OnceTheSourceStopsChanging()
    {
        // A scroll that stops sends no "zero" event: the last change is the last thing the source reports.
        // The velocity must not hold that speed forever (a scroll-driven skew would stay leaning).
        var context = new ManualSynchronizationContext();
        var source = Bm.Value(0.0);
        long now = 0;
        source.TimeSource = () => now;
        var velocity = Bm.Velocity(source);

        ChangeUnder(context, () =>
        {
            now = 0; source.SetSync(0);
            now = 16; source.SetSync(40);
            now = 32; source.SetSync(80);
        });
        Assert.AreEqual(2500, velocity.Value, 1e-6);

        now = 10_000;   // long still - the settle check that the last change scheduled finds it so
        var settled = SpinWait.SpinUntil(() => { context.Pump(); return velocity.Value == 0; }, TimeSpan.FromSeconds(2));

        Assert.IsTrue(settled, $"velocity still {velocity.Value} after the source stopped");
    }

    [TestMethod]
    public void Velocity_Settles_WhenItsCheckFindsTheSourceNotYetStill()
    {
        // The check's timer and the source's clock are different clocks (and a test's clock is a third): one that runs
        // ahead finds the source not yet still, and must look again later rather than give up with the speed held.
        var context = new ManualSynchronizationContext();
        var source = Bm.Value(0.0);
        long now = 0;
        source.TimeSource = () => now;
        var velocity = Bm.Velocity(source);
        ChangeUnder(context, () =>
        {
            now = 0; source.SetSync(0);
            now = 16; source.SetSync(40);
            now = 32; source.SetSync(80);
        });

        // By the source's clock no time passes: the first check finds it still moving.
        Assert.IsTrue(SpinWait.SpinUntil(() => context.Posted > 0, TimeSpan.FromSeconds(2)), "the check never came");
        context.Pump();
        Assert.AreEqual(2500, velocity.Value, 1e-6);

        now = 10_000;
        var settled = SpinWait.SpinUntil(() => { context.Pump(); return velocity.Value == 0; }, TimeSpan.FromSeconds(2));

        Assert.IsTrue(settled, $"velocity still {velocity.Value} after the source stopped");
    }

    [TestMethod]
    public void Velocity_DoesNotSettle_WhileTheSourceKeepsChanging()
    {
        var context = new ManualSynchronizationContext();
        var source = Bm.Value(0.0);
        long now = 0;
        source.TimeSource = () => now;
        var velocity = Bm.Velocity(source);

        for (int i = 0; i <= 20; i++)
        {
            ChangeUnder(context, () => { now = i * 16; source.SetSync(i * 40.0); });
            context.Pump();
            Thread.Sleep(5);   // well inside the window a 16 ms pace allows
        }

        Assert.AreEqual(2500, velocity.Value, 1e-6);
    }

    [TestMethod]
    public void Velocity_ChangedFromAThreadWithNoWayBack_IsNotSettledFromAnother()
    {
        // No synchronization context, off WebAssembly: a check would run on a timer's thread, racing the thread that
        // changes the source. Nothing is scheduled; the velocity keeps its speed until the source changes again.
        var source = Bm.Value(0.0);
        long now = 0;
        source.TimeSource = () => now;
        var velocity = Bm.Velocity(source);
        ChangeUnder(null, () =>
        {
            now = 0; source.SetSync(0);
            now = 16; source.SetSync(40);
            now = 32; source.SetSync(80);
        });

        now = 10_000;
        Thread.Sleep(150);   // three times the window a 16 ms pace allows

        Assert.AreEqual(2500, velocity.Value, 1e-6);
        Assert.AreEqual(0, source.GetVelocity(), "the source itself reads as still");
    }

    // Runs the source changes in change with context as the current one: where whatever they schedule runs.
    private static void ChangeUnder(SynchronizationContext? context, Action change)
        => ManualSynchronizationContext.RunUnder(context, () => { change(); return 0; });

    [TestMethod]
    public void Velocity_RejectsNullSource()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Bm.Velocity(null!));
    }

    // ── SnapTo (inertia ModifyTarget helpers) ─────────────────────────────────

    [TestMethod]
    public void SnapTo_Step_RoundsToTheNearestMultiple()
    {
        var snap = Bm.SnapTo(100);

        Assert.AreEqual(0, snap(49));
        Assert.AreEqual(100, snap(51));
        Assert.AreEqual(-100, snap(-51));
        Assert.AreEqual(100, snap(50));    // exact halves round away from zero, not to even
        Assert.AreEqual(-100, snap(-50));
    }

    [TestMethod]
    public void SnapTo_Step_AnchorsOnTheOrigin()
    {
        var snap = Bm.SnapTo(100, origin: 20);

        Assert.AreEqual(20, snap(50));
        Assert.AreEqual(120, snap(80));
    }

    [TestMethod]
    public void SnapTo_Step_RejectsNonPositiveOrNonFiniteArguments()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Bm.SnapTo(0));
        Assert.ThrowsExactly<ArgumentException>(() => Bm.SnapTo(-10));
        Assert.ThrowsExactly<ArgumentException>(() => Bm.SnapTo(double.NaN));
        Assert.ThrowsExactly<ArgumentException>(() => Bm.SnapTo(100, origin: double.PositiveInfinity));
    }

    [TestMethod]
    public void SnapTo_Stops_PicksTheNearestStop()
    {
        var snap = Bm.SnapTo([0, -320, -640]);

        Assert.AreEqual(0, snap(-100));
        Assert.AreEqual(-320, snap(-200));
        Assert.AreEqual(-640, snap(-900));   // beyond the last stop still clamps onto it
    }

    [TestMethod]
    public void SnapTo_Stops_IsUnaffectedByLaterMutationOfTheCallersArray()
    {
        var stops = new[] { 0.0, 100.0 };
        var snap = Bm.SnapTo(stops);

        stops[1] = 9999;

        Assert.AreEqual(100, snap(80));
    }

    [TestMethod]
    public void SnapTo_Stops_RejectsEmptyOrNonFiniteStops()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Bm.SnapTo([]));
        Assert.ThrowsExactly<ArgumentException>(() => Bm.SnapTo([0, double.NaN]));
        Assert.ThrowsExactly<ArgumentNullException>(() => Bm.SnapTo(null!));
    }
}
