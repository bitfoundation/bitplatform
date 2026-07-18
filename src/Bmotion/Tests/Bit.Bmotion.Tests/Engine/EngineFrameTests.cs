using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Drives the engine's <see cref="BmotionAnimationEngine.ComputeFrame"/> tick directly with
/// synthetic timestamps (plan item 0.1 - engine concurrency) to pin the highest-risk behaviours:
/// per-frame progression, natural completion, and interruption folding a running animation back
/// into state without raising its completion callback.
/// </summary>
[TestClass]
public class EngineFrameTests
{
    private static BmotionAnimationEngine NewEngine()
        => new(new FakeBmotionInterop { IsInProcess = true });

    // Colors never offload to the compositor, so they stay on the rAF path we can tick by hand.
    private static readonly Dictionary<string, object?> ToRed = new() { ["backgroundColor"] = "#ff0000" };

    private static string? ColorOf(Dictionary<string, Dictionary<string, string>>? frame, string id)
        => frame is not null && frame.TryGetValue(id, out var s) && s.TryGetValue("backgroundColor", out var c) ? c : null;

    [TestMethod]
    public async Task Frame_ProgressesAColorTween_AndCompletesNaturally()
    {
        var engine = NewEngine();
        engine.RegisterElement("el", new Dictionary<string, object?> { ["backgroundColor"] = "#000000" });

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await engine.AnimateToAsync("el", ToRed, Bm.Tween(0.1).ToConfig(),
            onComplete: () => { done.TrySetResult(); return Task.CompletedTask; });

        engine.ComputeFrame(0);                    // seeds start time, applies the from-color
        var mid = engine.ComputeFrame(50);         // ~halfway
        Assert.IsNotNull(ColorOf(mid, "el"), "the color tween should emit a frame value");

        engine.ComputeFrame(100);                  // reaches the target
        engine.ComputeFrame(101);                  // settles / resolves the completion batch

        await done.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(done.Task.IsCompletedSuccessfully, "a tween that runs to its end completes naturally");
    }

    [TestMethod]
    public async Task Interruption_FoldsRunningAnimation_WithoutRaisingItsCompletion()
    {
        var engine = NewEngine();
        engine.RegisterElement("el", new Dictionary<string, object?> { ["backgroundColor"] = "#000000" });

        var firstCompleted = false;
        await engine.AnimateToAsync("el", ToRed, Bm.Tween(0.5).ToConfig(),
            onComplete: () => { firstCompleted = true; return Task.CompletedTask; });

        engine.ComputeFrame(0);
        engine.ComputeFrame(100); // 20% through the first animation

        // Interrupt with a new animation on the same property before the first finishes.
        await engine.AnimateToAsync("el",
            new Dictionary<string, object?> { ["backgroundColor"] = "#00ff00" },
            Bm.Tween(0.1).ToConfig());

        engine.ComputeFrame(150);
        engine.ComputeFrame(260); // let the second animation finish

        await Task.Delay(50); // give any (incorrect) continuation a chance to run
        Assert.IsFalse(firstCompleted, "a superseded animation must not raise OnAnimationComplete");
    }

    [TestMethod]
    public async Task Frame_IsIdle_WhenNothingIsAnimating_AndAfterCompletion()
    {
        var engine = NewEngine();
        engine.RegisterElement("el", null); // no seeded values ⇒ nothing to flush

        // No animation started → an idle tick produces no updates.
        Assert.IsNull(engine.ComputeFrame(0));

        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await engine.AnimateToAsync("el", ToRed, Bm.Tween(0.05).ToConfig(),
            onComplete: () => { done.TrySetResult(); return Task.CompletedTask; });

        engine.ComputeFrame(0);
        engine.ComputeFrame(60); // finishes
        await done.Task.WaitAsync(TimeSpan.FromSeconds(2));

        // The element is no longer animating once the tween has settled.
        Assert.IsFalse(engine.GetDiagnostics().Single(d => d.Id == "el").HasActiveAnimations);
    }

    [TestMethod]
    public async Task Stop_HaltsAnAnimationMidFlight()
    {
        var engine = NewEngine();
        engine.RegisterElement("el", new Dictionary<string, object?> { ["backgroundColor"] = "#000000" });

        await engine.AnimateToAsync("el", ToRed, Bm.Tween(1.0).ToConfig());
        engine.ComputeFrame(0);
        engine.ComputeFrame(100);

        engine.Stop("el", null);
        var after = engine.GetDiagnostics().Single(d => d.Id == "el");
        Assert.IsFalse(after.HasActiveAnimations, "Stop() must halt the running driver");
    }
}
