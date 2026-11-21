using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// A property only a gesture sets goes back to what it showed before the gesture took it - however quickly the
/// gesture comes and goes (#15405: a hover flicked in and out came to rest part-way, at the scale it had shown
/// mid-flight).
/// </summary>
[TestClass]
public class GestureOriginTests
{
    private static BmotionAnimationEngine NewEngine()
        => new(new FakeBmotionInterop { IsInProcess = true, WebAnimationsSupported = false });

    private static Dictionary<string, object?> Values(params (string Key, object Value)[] values)
        => values.ToDictionary(v => v.Key, v => (object?)v.Value);

    private static double Scale(BmotionAnimationEngine engine)
        => engine.GetDiagnostics().Single(d => d.Id == "el").Transforms.GetValueOrDefault("scale", 1);

    private static double Run(BmotionAnimationEngine engine, ref double now, double ms)
    {
        for (var end = now + ms; now < end;) engine.ComputeFrame(now += 16);
        return Scale(engine);
    }

    [TestMethod]
    public async Task AGestureTakingAPropertyAgainOnItsWayBack_StillReturnsItToWhereItRests()
    {
        var engine = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        var tween = Bm.Tween(0.3).ToConfig();
        double now = 0;
        engine.ComputeFrame(now);

        for (var flick = 0; flick < 5; flick++)
        {
            await engine.ActivateGestureLayerAsync("el", "hover", Values(("scale", 1.5)), tween);
            Run(engine, ref now, 100);
            await engine.DeactivateGestureLayerAsync("el", "hover");
            var partWay = Run(engine, ref now, 50);
            Assert.IsTrue(partWay > 1.01, $"still on its way back when the pointer comes in again: {partWay}");
        }

        Assert.AreEqual(1, Run(engine, ref now, 1_000), 1e-6, "back to the scale it had before the first hover");
    }

    [TestMethod]
    public async Task AGestureTakingAPropertyAgainOnceItIsBack_ReturnsItToWhereItRestsNow()
    {
        var engine = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        var tween = Bm.Tween(0.3).ToConfig();
        double now = 0;
        engine.ComputeFrame(now);

        await engine.ActivateGestureLayerAsync("el", "hover", Values(("scale", 1.5)), tween);
        Run(engine, ref now, 500);
        await engine.DeactivateGestureLayerAsync("el", "hover");
        Assert.AreEqual(1, Run(engine, ref now, 500), 1e-6);

        // Something other than a gesture moves it since: that is where it rests now.
        await engine.AnimateToAsync("el", Values(("scale", 2.0)), tween);
        Assert.AreEqual(2, Run(engine, ref now, 500), 1e-6);

        await engine.ActivateGestureLayerAsync("el", "hover", Values(("scale", 1.5)), tween);
        Run(engine, ref now, 500);
        await engine.DeactivateGestureLayerAsync("el", "hover");
        Assert.AreEqual(2, Run(engine, ref now, 500), 1e-6);
    }

    [TestMethod]
    public async Task APropertyTakenOverOnItsWayBack_RestsWhereTheNewAnimationPutsIt()
    {
        var engine = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        var tween = Bm.Tween(0.3).ToConfig();
        double now = 0;
        engine.ComputeFrame(now);

        await engine.ActivateGestureLayerAsync("el", "hover", Values(("scale", 1.5)), tween);
        Run(engine, ref now, 500);
        await engine.DeactivateGestureLayerAsync("el", "hover");
        Run(engine, ref now, 50);

        // Taken over part-way back: the origin the hover captured is no longer where it rests.
        await engine.AnimateToAsync("el", Values(("scale", 0.5)), tween);
        Assert.AreEqual(0.5, Run(engine, ref now, 500), 1e-6);

        await engine.ActivateGestureLayerAsync("el", "hover", Values(("scale", 1.5)), tween);
        Run(engine, ref now, 500);
        await engine.DeactivateGestureLayerAsync("el", "hover");
        Assert.AreEqual(0.5, Run(engine, ref now, 500), 1e-6);
    }
}
