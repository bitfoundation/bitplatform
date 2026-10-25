using Bit.Bmotion.Tests.TestInfra;

namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// The engine's synchronous entry points that have to get a value onto the element by themselves:
/// a drag position on an otherwise idle element, <c>Complete</c> for an element released straight
/// after, and a frame whose per-element callback mounts or unmounts elements while the frame is
/// still iterating them.
/// </summary>
[TestClass]
public class EngineFlushTests
{
    private static (BmotionAnimationEngine Engine, FakeBmotionInterop Interop) NewEngine(bool inProcess = true)
    {
        var interop = new FakeBmotionInterop { IsInProcess = inProcess };
        return (new BmotionAnimationEngine(interop), interop);
    }

    private static Dictionary<string, string>[] StylesWrittenTo(FakeBmotionInterop interop, string id)
        => interop.ArgsOf("applyStyles").Where(a => (string?)a[0] == id).Select(a => (Dictionary<string, string>)a[1]!).ToArray();

    private static Dictionary<string, object?> Values(string key, object value) => new() { [key] = value };

    // ── Drag position ─────────────────────────────────────────────────────────

    [TestMethod]
    public async Task SetDragPosition_OnAnIdleElement_StartsTheFrameLoop()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        Assert.AreEqual(0, interop.CountOf("startRafLoop"), "nothing to animate yet");

        // Nothing else is animating, so without a kick no frame would ever write the drag.
        engine.SetDragPosition("el", 30, 10);

        Assert.AreEqual(1, interop.CountOf("startRafLoop"));
        var frame = engine.ComputeFrame(0);
        StringAssert.Contains(frame!["el"]["transform"], "30px");

        // While the loop runs, every frame picks the position up by itself - no restart per move.
        engine.SetDragPosition("el", 45, 10);
        Assert.AreEqual(1, interop.CountOf("startRafLoop"));
        StringAssert.Contains(engine.ComputeFrame(16)!["el"]["transform"], "45px");
    }

    [TestMethod]
    public async Task SetDragPosition_WithoutAFrameLoop_FlushesEachPositionToTheElement()
    {
        var (engine, interop) = NewEngine(inProcess: false);
        await using var _ = engine;
        engine.RegisterElement("el");

        engine.SetDragPosition("el", 30, 10);
        engine.SetDragPosition("el", 45, 10);

        Assert.IsFalse(interop.WasCalled("startRafLoop"), "Blazor Server has no frame loop to start");
        var written = StylesWrittenTo(interop, "el");
        Assert.AreEqual(2, written.Length, "one write per reported position");
        StringAssert.Contains(written[0]["transform"], "30px");
        StringAssert.Contains(written[1]["transform"], "45px");
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Complete_OfACompositorAnimation_WritesTheTarget_EvenIfTheElementIsReleasedAtOnce()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el");
        await engine.AnimateToAsync("el", Values("opacity", 0.25), Bm.Tween(1).ToConfig());
        Assert.IsTrue(interop.WasCalled("playWaapiAnimation"));

        // What a programmatic animation's controls do: complete, then release the element - before
        // any frame (or flush) could have found it.
        engine.Complete("el");
        engine.UnregisterElement("el");

        // The browser animation is cancelled without committing, so the final value has to have
        // been written by Complete itself.
        Assert.IsTrue(interop.ArgsOf("cancelWaapiAnimation").All(a => (bool)a[2]! == false));
        Assert.AreEqual("0.25", StylesWrittenTo(interop, "el").Last()["opacity"]);
    }

    [TestMethod]
    public async Task Complete_OfAFrameLoopAnimation_WritesTheTarget_EvenIfTheElementIsReleasedAtOnce()
    {
        var (engine, interop) = NewEngine();
        await using var _ = engine;
        engine.RegisterElement("el", Values("backgroundColor", "#000000"));
        await engine.AnimateToAsync("el", Values("backgroundColor", "#ffffff"), Bm.Tween(1).ToConfig());
        engine.ComputeFrame(0);

        engine.Complete("el");
        engine.UnregisterElement("el");

        Assert.IsNull(engine.ComputeFrame(16), "nothing is left for a frame to write");
        Assert.AreEqual("#ffffff", StylesWrittenTo(interop, "el").Last()["backgroundColor"]);
    }

    // ── Frames that change the element set ────────────────────────────────────

    [TestMethod]
    public async Task ComputeFrame_SurvivesAnOnFrameCallbackThatMountsAndUnmountsElements()
    {
        var (engine, _) = NewEngine();
        await using var __ = engine;
        foreach (var id in new[] { "a", "b" })
        {
            engine.RegisterElement(id, Values("backgroundColor", "#000000"));
            await engine.AnimateToAsync(id, Values("backgroundColor", "#ffffff"), Bm.Tween(1).ToConfig());
        }

        // A per-frame callback whose re-render (synchronous on WebAssembly) mounts one element and
        // disposes another - in the middle of the frame that is iterating them.
        bool fired = false;
        engine.SetOnFrame("a", _ =>
        {
            if (fired) return;
            fired = true;
            engine.RegisterElement("c");
            engine.UnregisterElement("b");
        });

        var frame = engine.ComputeFrame(0);

        Assert.IsTrue(fired);
        Assert.IsTrue(frame!.ContainsKey("a"));
        Assert.IsFalse(frame.ContainsKey("b"), "an element disposed earlier in the frame is not ticked");
        Assert.IsTrue(engine.IsRegistered("a"), "the element whose callback did it is not evicted for it");
        Assert.IsTrue(engine.IsRegistered("c"));
        Assert.IsFalse(engine.IsRegistered("b"));

        // And the next frame carries on as usual.
        Assert.IsTrue(engine.ComputeFrame(16)!.ContainsKey("a"));
    }
}
