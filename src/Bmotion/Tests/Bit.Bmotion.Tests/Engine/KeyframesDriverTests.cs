
namespace Bit.Bmotion.Tests.Engine;

[TestClass]
public class NumericKeyframesDriverTests
{
    // ── Two frames ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_TwoFrames_LinearEase_InterpolatesCorrectly()
    {
        var log = new List<double>();
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear },
            v => log.Add(v));

        driver.Tick(0);   // t=0   → 0
        driver.Tick(150); // t=0.5 → 50
        driver.Tick(300); // t=1.0 → 100

        Assert.AreEqual(0.0, log[0], 1e-5);
        Assert.AreEqual(50.0, log[1], 1e-1);
        Assert.AreEqual(100.0, log[2], 1e-5);
    }

    [TestMethod]
    public void Tick_TwoFrames_AtEnd_ReturnsTrue()
    {
        var log = new List<double>();
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear },
            v => log.Add(v));

        driver.Tick(0);
        bool done = driver.Tick(300);

        Assert.IsTrue(done);
        Assert.AreEqual(100.0, log[^1], 1e-5);
    }

    // ── Three frames (even distribution) ──────────────────────────────────────

    [TestMethod]
    public void Tick_ThreeFrames_EvenTimes_CorrectSegmentInterpolation()
    {
        var log = new List<double>();
        // times automatically: [0, 0.5, 1.0]
        var driver = new BmotionNumericKeyframesDriver(
            [0, 50, 100],
            new BmotionTransitionConfig { Duration = 0.4, Ease = BmEase.Linear },
            v => log.Add(v));

        driver.Tick(0);   // t=0   → frame[0] = 0
        driver.Tick(200); // t=0.5 → boundary between segment 0 and 1 → 50
        driver.Tick(400); // t=1.0 → frame[2] = 100

        Assert.AreEqual(0.0, log[0], 1e-5);
        Assert.AreEqual(50.0, log[1], 1e-1);
        Assert.AreEqual(100.0, log[2], 1e-5);
    }

    // ── Custom times ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_CustomTimes_RespectsKeyframePlacement()
    {
        var log = new List<double>();
        // First segment covers t=0..0.8, second 0.8..1.0
        var driver = new BmotionNumericKeyframesDriver(
            [0, 80, 100],
            new BmotionTransitionConfig { Duration = 0.4, Ease = BmEase.Linear, Times = [0.0, 0.8, 1.0] },
            v => log.Add(v));

        driver.Tick(0);   // t=0   → 0
        driver.Tick(320); // t=0.8 → frame[1] = 80
        driver.Tick(400); // t=1.0 → frame[2] = 100

        Assert.AreEqual(0.0, log[0], 1e-5);
        Assert.AreEqual(80.0, log[1], 1e-1);
        Assert.AreEqual(100.0, log[2], 1e-5);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Cancel_CompletesImmediately()
    {
        var log = new List<double>();
        var driver = new BmotionNumericKeyframesDriver(
            [0, 50, 100],
            new BmotionTransitionConfig { Duration = 0.3 },
            v => log.Add(v));

        driver.Tick(0);
        driver.Cancel();
        bool done = driver.Tick(100);

        // Cancel() freezes in place rather than snapping to the last frame;
        // only completion is guaranteed.
        Assert.IsTrue(done);
    }

    // ── Repeat / Mirror ───────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_RepeatOnce_PlaysAnimationTwice()
    {
        var log = new List<double>();
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear, Repeat = 1 },
            v => log.Add(v));

        driver.Tick(0);
        bool done1 = driver.Tick(300); // end of first pass
        bool done2 = driver.Tick(600); // end of second pass

        Assert.IsFalse(done1);
        Assert.IsTrue(done2);
    }

    [TestMethod]
    public void Tick_MirrorRepeat_SecondPassIsReversed()
    {
        var log = new List<double>();
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Mirror,
            },
            v => log.Add(v));

        driver.Tick(0);   // → 0
        driver.Tick(300); // → 100, mirrors
        driver.Tick(450); // midpoint of reversed pass → ≈ 50
        driver.Tick(600); // end of reversed pass → 0

        Assert.AreEqual(0.0, log[^1], 1e-1);
    }

    [TestMethod]
    public void Tick_ReverseRepeat_PlaysForwardThenReversed()
    {
        var log = new List<double>();
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Reverse,
            },
            v => log.Add(v));

        driver.Tick(0);   // → 0
        driver.Tick(300); // forward pass end → 100, reverses
        driver.Tick(450); // midpoint of reversed pass → ≈ 50
        driver.Tick(600); // reversed pass end → 0

        Assert.AreEqual(50.0, log[^2], 1e-1);
        Assert.AreEqual(0.0, log[^1], 1e-1);
    }

    [TestMethod]
    public void Complete_ReverseRepeatOnce_ReturnsToStartValue()
    {
        var log = new List<double>();
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Reverse,
            },
            v => log.Add(v));

        // Forward once then replayed reversed → terminal value is the start frame.
        driver.Complete();

        Assert.AreEqual(0.0, log[^1], 1e-5);
    }
}

[TestClass]
public class NumericKeyframesDriverValidationTests
{
    // ── Constructor input guards ──────────────────────────────────────────────

    [TestMethod]
    public void Ctor_TimesLengthMismatch_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BmotionNumericKeyframesDriver(
            [0, 50, 100],
            new BmotionTransitionConfig { Duration = 0.3, Times = [0.0, 1.0] }, // 2 times, 3 frames
            _ => { }));
    }

    [TestMethod]
    public void Ctor_TimesOutOfRange_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig { Duration = 0.3, Times = [0.0, 1.5] }, // 1.5 > 1
            _ => { }));
    }

    [TestMethod]
    public void Ctor_TimesNonFinite_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig { Duration = 0.3, Times = [0.0, double.NaN] },
            _ => { }));
    }

    [TestMethod]
    public void Ctor_NonFiniteDuration_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig { Duration = double.PositiveInfinity },
            _ => { }));
    }

    [TestMethod]
    public void Ctor_NegativeDuration_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BmotionNumericKeyframesDriver(
            [0, 100],
            new BmotionTransitionConfig { Duration = -0.1 },
            _ => { }));
    }
}

[TestClass]
public class ColorKeyframesDriverTests
{
    // ── Interpolation ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_TwoColorFrames_AtMidpoint_InterpolatesCorrectly()
    {
        string? lastValue = null;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#ffffff"],
            new BmotionTransitionConfig { Duration = 0.3, Ease = BmEase.Linear },
            v => lastValue = v);

        driver.Tick(0);
        driver.Tick(150); // t=0.5 → rgba(128,128,128,1)

        Assert.AreEqual("rgba(128,128,128,1)", lastValue);
    }

    [TestMethod]
    public void Tick_ThreeColorFrames_AtEnd_ReturnsLastFrame()
    {
        string? lastValue = null;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#ff0000", "#0000ff"],
            new BmotionTransitionConfig { Duration = 0.4, Ease = BmEase.Linear },
            v => lastValue = v);

        driver.Tick(0);
        driver.Tick(400); // t=1.0 → last frame is blue

        Assert.AreEqual("rgba(0,0,255,1)", lastValue);
    }

    [TestMethod]
    public void Tick_AtEnd_ReturnsTrue()
    {
        bool done = false;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#ffffff"],
            new BmotionTransitionConfig { Duration = 0.3 },
            _ => { });

        driver.Tick(0);
        done = driver.Tick(300);

        Assert.IsTrue(done);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Cancel_CompletesImmediately()
    {
        string? lastValue = null;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#ff0000"],
            new BmotionTransitionConfig { Duration = 0.3 },
            v => lastValue = v);

        driver.Tick(0);
        driver.Cancel();
        bool done = driver.Tick(50);

        // Cancel() freezes in place rather than snapping to the last frame;
        // only completion is guaranteed.
        Assert.IsTrue(done);
    }

    // ── Mirror repeat ─────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_MirrorRepeat_SecondPassGoesBackToFirstFrame()
    {
        string? lastValue = null;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#ffffff"],
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Mirror,
            },
            v => lastValue = v);

        driver.Tick(0);   // → rgba(0,0,0,1)
        driver.Tick(300); // → rgba(255,255,255,1), mirrors
        driver.Tick(600); // reversed pass end → rgba(0,0,0,1)

        Assert.AreEqual("rgba(0,0,0,1)", lastValue);
    }

    // ── Reverse repeat ────────────────────────────────────────────────────────

    [TestMethod]
    public void Tick_ReverseRepeat_SecondPassGoesBackToFirstFrame()
    {
        string? lastValue = null;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#ffffff"],
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Reverse,
            },
            v => lastValue = v);

        driver.Tick(0);   // → rgba(0,0,0,1)
        driver.Tick(300); // forward pass end → rgba(255,255,255,1), reverses
        driver.Tick(600); // reversed pass end → rgba(0,0,0,1)

        Assert.AreEqual("rgba(0,0,0,1)", lastValue);
    }

    [TestMethod]
    public void Complete_ReverseNoRepeat_SnapsToLastFrame()
    {
        string? lastValue = null;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#ffffff"],
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                RepeatType = BmRepeatType.Reverse,
            },
            v => lastValue = v);

        // A single forward pass ends on the last frame.
        driver.Complete();

        Assert.AreEqual("#ffffff", lastValue);
    }

    [TestMethod]
    public void Complete_ReverseRepeatOnce_SnapsBackToFirstFrame()
    {
        string? lastValue = null;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#ffffff"],
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.Linear,
                Repeat = 1,
                RepeatType = BmRepeatType.Reverse,
            },
            v => lastValue = v);

        // Forward once then replayed reversed → terminal value is the first frame.
        driver.Complete();

        Assert.AreEqual("#000000", lastValue);
    }

    // ── Per-segment easing (Eases) ────────────────────────────────────────────

    [TestMethod]
    public void Tick_PerSegmentEases_OverridesGlobalEasePerSegment()
    {
        var log = new List<double>();
        // Global ease is BackOut (overshoots), but per-segment eases force both segments linear.
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100, 200],
            new BmotionTransitionConfig
            {
                Duration = 0.4,
                Ease = BmEase.BackOut,
                Eases = [BmEase.Linear, BmEase.Linear],
            },
            v => log.Add(v));

        driver.Tick(0);   // t=0    → 0
        driver.Tick(100); // t=0.25 → segment 0 at segT 0.5, linear → 50
        driver.Tick(300); // t=0.75 → segment 1 at segT 0.5, linear → 150

        Assert.AreEqual(0.0, log[0], 1e-5);
        Assert.AreEqual(50.0, log[1], 1e-1);
        Assert.AreEqual(150.0, log[2], 1e-1);
    }

    [TestMethod]
    public void Tick_PerSegmentEases_ShorterArray_RepeatsLastEntry()
    {
        var log = new List<double>();
        // One entry for two segments: the Linear entry repeats onto segment 1 as well.
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100, 200],
            new BmotionTransitionConfig
            {
                Duration = 0.4,
                Ease = BmEase.BackOut,
                Eases = [BmEase.Linear],
            },
            v => log.Add(v));

        driver.Tick(0);
        driver.Tick(300); // t=0.75 → segment 1 at segT 0.5, linear → 150

        Assert.AreEqual(150.0, log[^1], 1e-1);
    }

    [TestMethod]
    public void Tick_PerSegmentEases_MirrorRepeat_KeepsEasePairedWithSegment()
    {
        var log = new List<double>();
        // Segment 0 linear, segment 1 heavily eased. After the mirror both frames AND eases
        // reverse, so the (100 → 0) segment of the mirrored pass must still be linear.
        var driver = new BmotionNumericKeyframesDriver(
            [0, 100, 200],
            new BmotionTransitionConfig
            {
                Duration = 0.4,
                Ease = BmEase.Linear,
                Eases = [BmEase.Linear, BmEase.CircIn],
                Repeat = 1,
                RepeatType = BmRepeatType.Mirror,
            },
            v => log.Add(v));

        driver.Tick(0);
        driver.Tick(400); // forward pass ends at 200, mirrors (frames + eases reversed)
        driver.Tick(700); // mirrored t=0.75 → now the 100→0 segment at segT 0.5, linear → 50

        Assert.AreEqual(50.0, log[^1], 1e-1);
    }

    [TestMethod]
    public void Tick_ColorDriver_PerSegmentEases_AppliesLinearSegment()
    {
        string? lastValue = null;
        var driver = new BmotionColorKeyframesDriver(
            ["#000000", "#646464"],
            new BmotionTransitionConfig
            {
                Duration = 0.3,
                Ease = BmEase.BackOut,
                Eases = [BmEase.Linear],
            },
            v => lastValue = v);

        driver.Tick(0);
        driver.Tick(150); // linear midpoint of 0x00 → 0x64 (100) = 50

        Assert.AreEqual("rgba(50,50,50,1)", lastValue);
    }
}

