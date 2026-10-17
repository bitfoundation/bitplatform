using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Momentum without a frame loop (Blazor Server, where every animation collapses to an instant
/// change): an inertia can't coast there, but it still has a resting place - the one
/// <see cref="BmotionInertiaDriver.ProjectRest"/> computes, snapped by <c>ModifyTarget</c> and
/// clamped to the drag bounds - and the element lands on it at once instead of staying wherever
/// it was released.
/// </summary>
[TestClass]
public class ForceInstantInertiaTests
{
    private static (BmotionAnimationEngine Engine, FakeBmotionInterop Interop) NewServerEngine()
    {
        var interop = new FakeBmotionInterop { IsInProcess = false };
        return (new BmotionAnimationEngine(interop), interop);
    }

    // Releases a drag at x = 30 with a horizontal flick of 2 px/frame. The engine scales that by 50
    // into the inertia velocity, so the decay projects 30 + 0.8 (default power) * 100 = 110.
    private static async Task<double> ReleaseAt30(
        BmotionAnimationEngine engine, BmInertia momentum, BmDragConstraints? constraints = null)
    {
        engine.RegisterElement("el");
        engine.SetDragPosition("el", 30, 0);
        await engine.EndDragAsync("el", velX: 2, velY: 0, momentum: true, constraints, axis: "x", momentum.ToConfig());
        return engine.GetDiagnostics().Single(d => d.Id == "el").Transforms["x"];
    }

    private static string LastTransform(FakeBmotionInterop interop)
        => interop.ArgsOf("applyStyles").Select(a => (Dictionary<string, string>)a[1]!).Last(s => s.ContainsKey("transform"))["transform"];

    [TestMethod]
    public async Task Release_LandsOnTheProjectedRest()
    {
        var (engine, interop) = NewServerEngine();
        await using var _ = engine;

        double x = await ReleaseAt30(engine, Bm.Inertia());

        Assert.AreEqual(110, x, 1e-9, "the decay's resting place, not the release point");
        StringAssert.Contains(LastTransform(interop), "110px", "and it is written to the element");
        Assert.IsFalse(engine.GetDiagnostics().Single(d => d.Id == "el").HasActiveAnimations);
    }

    [TestMethod]
    public async Task Release_SnapsTheRestWithModifyTarget()
    {
        var (engine, interop) = NewServerEngine();
        await using var _ = engine;

        double x = await ReleaseAt30(engine, Bm.Inertia(modifyTarget: Bm.SnapTo(100)));

        Assert.AreEqual(100, x, 1e-9, "110 snaps to the 100px grid");
        StringAssert.Contains(LastTransform(interop), "100px");
    }

    [TestMethod]
    public async Task Release_ClampsTheSnappedRestToTheBounds()
    {
        var (engine, _) = NewServerEngine();
        await using var __ = engine;

        double x = await ReleaseAt30(engine, Bm.Inertia(modifyTarget: Bm.SnapTo(100)),
            new BmDragConstraints { Left = 0, Right = 60 });

        Assert.AreEqual(60, x, 1e-9, "the snapped 100 is clamped to the right bound");
    }

    [TestMethod]
    public void ProjectRest_MatchesWhereTheDriverComesToRest()
    {
        // The instant landing and the coasting driver must agree on where the element ends up.
        var config = Bm.Inertia(velocity: 300, power: 0.5, min: -40, max: 120, modifyTarget: Bm.SnapTo(25)).ToConfig();
        double rest = BmotionInertiaDriver.ProjectRest(10, config);
        double last = double.NaN;
        var driver = new BmotionInertiaDriver(10, config, v => last = v);
        for (double t = 0; t < 20_000 && !driver.Tick(t); t += 16) { }

        Assert.AreEqual(120, rest, 1e-9, "10 + 0.5 * 300 = 160, snapped to 150, clamped to 120");
        Assert.AreEqual(rest, last, 1e-6);
    }
}
