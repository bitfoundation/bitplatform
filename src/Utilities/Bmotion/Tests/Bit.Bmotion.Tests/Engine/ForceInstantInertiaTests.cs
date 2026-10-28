using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Momentum without a frame loop (Blazor Server): the release coasts in the browser - the inertia is recorded on
/// the server and played as a Web Animation - and comes to rest where <see cref="BmotionInertiaDriver.ProjectRest"/>
/// says, snapped by <c>ModifyTarget</c> and clamped to the drag bounds, rather than staying where it was released.
/// Where even that is impossible (a browser without Web Animations) the element still lands on that resting place
/// at once.
/// </summary>
[TestClass]
public class ForceInstantInertiaTests
{
    private static (BmotionAnimationEngine Engine, FakeBmotionInterop Interop) NewServerEngine(bool webAnimations = true)
    {
        var interop = new FakeBmotionInterop { IsInProcess = false, WebAnimationsSupported = webAnimations };
        return (new BmotionAnimationEngine(interop), interop);
    }

    // Releases a drag at x = 30 with a horizontal flick of 2 px/frame. The engine scales that by 50
    // into the inertia velocity, so the decay projects 30 + 0.8 (default power) * 100 = 110.
    private static async Task ReleaseAt30(
        BmotionAnimationEngine engine, BmInertia momentum, BmDragConstraints? constraints = null)
    {
        engine.RegisterElement("el");
        engine.SetDragPosition("el", 30, 0);
        await engine.EndDragAsync("el", velX: 2, velY: 0, momentum: true, constraints, axis: "x", momentum.ToConfig());
    }

    private static double X(BmotionAnimationEngine engine) => engine.GetDiagnostics().Single(d => d.Id == "el").Transforms["x"];

    private static string[] CoastTransforms(FakeBmotionInterop interop)
        => ((Dictionary<string, object>[])interop.ArgsOf("playWaapiAnimation").Single()[2]!)
            .Select(k => (string)k["transform"]).ToArray();

    private static double TranslateX(string transform)
        => double.Parse(transform[(transform.IndexOf('(') + 1)..transform.IndexOf("px", StringComparison.Ordinal)],
            System.Globalization.CultureInfo.InvariantCulture);

    [TestMethod]
    public async Task Release_CoastsInTheBrowser_ToTheProjectedRest()
    {
        var (engine, interop) = NewServerEngine();
        await using var _ = engine;

        await ReleaseAt30(engine, Bm.Inertia());

        var frames = CoastTransforms(interop);
        Assert.IsTrue(frames.Length > 2, "a coast, not a jump");
        var xs = frames.Select(TranslateX).ToArray();
        for (int i = 1; i < xs.Length; i++) Assert.IsTrue(xs[i] >= xs[i - 1] - 1e-9, "decelerating forwards, never back");
        Assert.AreEqual(110, xs[^1], 1e-6, "the decay's resting place, not the release point");

        interop.FinishWaapi();
        Assert.AreEqual(110, X(engine), 1e-6);
    }

    [TestMethod]
    public async Task Release_SnapsTheRestWithModifyTarget()
    {
        var (engine, interop) = NewServerEngine();
        await using var _ = engine;

        await ReleaseAt30(engine, Bm.Inertia(modifyTarget: Bm.SnapTo(100)));

        Assert.AreEqual(100, TranslateX(CoastTransforms(interop)[^1]), 1e-6, "110 snaps to the 100px grid");
    }

    [TestMethod]
    public async Task Release_ClampsTheSnappedRestToTheBounds()
    {
        var (engine, interop) = NewServerEngine();
        await using var _ = engine;

        await ReleaseAt30(engine, Bm.Inertia(modifyTarget: Bm.SnapTo(100)), new BmDragConstraints { Left = 0, Right = 60 });

        Assert.AreEqual(60, TranslateX(CoastTransforms(interop)[^1]), 1e-6, "the snapped 100 is clamped to the right bound");
    }

    [TestMethod]
    public async Task WithoutWebAnimations_TheReleaseLandsOnTheProjectedRestAtOnce()
    {
        var (engine, interop) = NewServerEngine(webAnimations: false);
        await using var _ = engine;

        await ReleaseAt30(engine, Bm.Inertia(modifyTarget: Bm.SnapTo(100)));

        Assert.AreEqual(100, X(engine), 1e-9);
        var written = interop.ArgsOf("applyStyles").Select(a => (Dictionary<string, string>)a[1]!).Last(s => s.ContainsKey("transform"));
        StringAssert.Contains(written["transform"], "100px");
        Assert.IsFalse(engine.GetDiagnostics().Single(d => d.Id == "el").HasActiveAnimations);
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
