using System.Reflection;
using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// A compositor (WAAPI) animation owns the whole <c>transform</c> property, so a new animation on
/// <c>y</c> has to cancel one running on <c>x</c>. These tests pin what happens to the <c>x</c>:
/// it carries on along the very same curve on the frame loop (a
/// <see cref="BmotionWaapiContinuationDriver"/>) and lands on its target, and the completion of the
/// animation it belonged to settles - as superseded - only once it has.
/// <para>
/// The continuations of the engine's completions run on the caller's context; the tests start
/// animations under a <see cref="ManualSynchronizationContext"/> and pump it, so whether a
/// completion handler ran is a fact at every step rather than a race.
/// </para>
/// </summary>
[TestClass]
public class WaapiContinuationTests
{
    private static (BmotionAnimationEngine Engine, FakeBmotionInterop Interop) NewEngine()
    {
        var interop = new FakeBmotionInterop { IsInProcess = true, SupportsLinearEasing = true };
        return (new BmotionAnimationEngine(interop), interop);
    }

    private static Dictionary<string, object?> Values(params (string Key, object Value)[] values)
        => values.ToDictionary(v => v.Key, v => (object?)v.Value);

    private static BmotionElementDiagnostics State(BmotionAnimationEngine engine) => engine.GetDiagnostics().Single(d => d.Id == "el");

    private static object? DriverFor(BmotionAnimationEngine engine, string key)
    {
        // The driver type is the point of the hand-off, and only the state's private map shows it.
        var drivers = (System.Collections.IDictionary)typeof(BmotionElementAnimationState)
            .GetField("_activeAnims", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(engine.GetState("el"))!;
        return drivers[key];
    }

    [TestMethod]
    public async Task InterruptedForAnotherTransformKey_ThePlanCarriesOnAlongItsCurve_AndSettlesWhenDone()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");

        // An x plan exactly halfway through on the wall clock the mirror samples: a very long linear
        // animation started half of it ago, so the jitter between here and the interrupt is a
        // negligible fraction of it. The completion is the one its starter would be awaiting.
        const double durationMs = 1_000_000;
        var completion = new TaskCompletionSource<bool>();
        var plan = new BmotionElementAnimationState.WaapiPlan
        {
            Token = 4242,
            StartMs = Environment.TickCount64 - (long)(durationMs / 2),
            DelayMs = 0,
            DurationMs = durationMs,
            Progress = [0, 1],
            Values = new() { ["x"] = (0, 100) },
            Iterations = 0,
            Mirror = false,
            Completion = completion,
        };
        engine.GetState("el")!.AddWaapiPlan(plan);

        await engine.AnimateToAsync("el", Values(("y", 50.0)), Bm.Tween(0.3).ToConfig());

        // The browser animation had to go (y takes the transform property)...
        Assert.AreEqual(4242, interop.ArgsOf("cancelWaapiAnimation").Single()[1]);
        // ...but x was handed to the frame loop rather than frozen, and y waits its turn there too.
        Assert.IsTrue(plan.HandedOff);
        Assert.IsInstanceOfType<BmotionWaapiContinuationDriver>(DriverFor(engine, "x"));
        CollectionAssert.IsSubsetOf(new[] { "x", "y" }, State(engine).ActiveProperties.ToArray());

        engine.ComputeFrame(0);
        Assert.AreEqual(50, State(engine).Transforms["x"], 0.5, "it resumes from where the compositor was");

        engine.ComputeFrame(250_000);
        Assert.AreEqual(75, State(engine).Transforms["x"], 0.5, "and follows the same linear curve");
        Assert.AreEqual(50, State(engine).Transforms["y"], 1e-9, "the interruption itself is long done");
        Assert.IsFalse(completion.Task.IsCompleted, "x's animation is not over while x is still moving");

        engine.ComputeFrame(500_100);
        Assert.AreEqual(100, State(engine).Transforms["x"], 1e-9, "it lands exactly on its target");
        Assert.IsTrue(completion.Task.IsCompleted);
        Assert.IsFalse(completion.Task.Result, "partly superseded, so it settles as superseded, not completed");
    }

    [TestMethod]
    public async Task InterruptedForAnotherTransformKey_TheStartersCompletionWaitsForTheContinuedKey()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        var context = new ManualSynchronizationContext();
        bool settled = false, completed = false;

        // Started through the regular entry point, as a <Bmotion> does: onSettled hears however it
        // ends, onComplete only a natural finish.
        await ManualSynchronizationContext.RunUnder(context, () => engine.AnimateToAsync("el",
            Values(("x", 100.0)), Bm.Tween(1, ease: BmEase.Linear).ToConfig(),
            onComplete: () => { completed = true; return Task.CompletedTask; },
            onSettled: () => settled = true));
        Assert.AreEqual(1, interop.CountOf("playWaapiAnimation"));

        await ManualSynchronizationContext.RunUnder(context, () => engine.AnimateToAsync("el",
            Values(("y", 50.0)), Bm.Tween(0.3).ToConfig()));
        engine.ComputeFrame(0);
        engine.ComputeFrame(500);
        context.Pump();

        double midway = State(engine).Transforms["x"];
        Assert.IsTrue(midway > 40 && midway < 60, $"x keeps going on the frame loop: {midway}");
        Assert.IsFalse(settled, "the animation is not over while its x is still moving");

        engine.ComputeFrame(1100);
        context.Pump();

        Assert.AreEqual(100, State(engine).Transforms["x"], 1e-9);
        Assert.IsTrue(settled);
        Assert.IsFalse(completed, "a superseded animation must not report a natural completion");
    }

    [TestMethod]
    public async Task TransformAfter_SamplesTheCompositorPlanWhereItWillBeThen_NotAtItsEnd()
    {
        // What a FLIP ends on: it masks the compositor animation for its own length, so it must land
        // on the frame that animation will be showing when the FLIP lifts - a plan a quarter through
        // its linear second, asked about a quarter second ahead, is halfway; asked about long after,
        // it is at its end.
        var (engine, _) = NewEngine();
        await using var __ = engine;
        engine.RegisterElement("el");
        const double durationMs = 1_000_000;
        var plan = new BmotionElementAnimationState.WaapiPlan
        {
            Token = 7,
            StartMs = Environment.TickCount64 - (long)(durationMs / 4),
            DelayMs = 0,
            DurationMs = durationMs,
            Progress = [0, 1],
            Values = new() { ["x"] = (0, 100) },
            Iterations = 0,
            Mirror = false,
        };
        engine.GetState("el")!.AddWaapiPlan(plan);

        Assert.AreEqual(50, engine.GetState("el")!.TransformsAfter(durationMs / 4)["x"], 0.5);
        Assert.AreEqual(100, engine.GetState("el")!.TransformsAfter(durationMs * 2)["x"], 1e-9);
        StringAssert.Contains(engine.GetTransformStringAfter("el", durationMs / 4)!, "50");
    }

    [TestMethod]
    public async Task EveryKeySuperseded_NothingIsContinued_AndTheOldCompletionIsNotRaised()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        var context = new ManualSynchronizationContext();
        bool settled = false, completed = false;

        await ManualSynchronizationContext.RunUnder(context, () => engine.AnimateToAsync("el",
            Values(("x", 100.0)), Bm.Tween(1).ToConfig(),
            onComplete: () => { completed = true; return Task.CompletedTask; },
            onSettled: () => settled = true));

        // The new target takes x as well: there is nothing of the old animation left to carry on.
        await ManualSynchronizationContext.RunUnder(context, () => engine.AnimateToAsync("el",
            Values(("x", 50.0), ("y", 20.0)), Bm.Tween(1).ToConfig()));
        context.Pump();

        Assert.IsTrue(settled, "the superseded animation settles at once");
        Assert.IsFalse(completed);
        Assert.AreEqual(2, interop.CountOf("playWaapiAnimation"), "with no continuation in the way, the new target offloads too");
        Assert.IsFalse(State(engine).HasActiveAnimations, "no frame-loop driver was left behind");

        // The new animation finishing is its own completion, never the old one's.
        interop.FinishWaapi();
        context.Pump();
        Assert.IsFalse(completed);
        Assert.AreEqual(50, State(engine).Transforms["x"], 1e-9);
    }
}
