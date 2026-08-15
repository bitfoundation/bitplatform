namespace Bit.Bmotion.Tests.Engine;

/// <summary>
/// Tests for the timeline playhead behind <see cref="BmSequence"/>. The point of running segment
/// starts off the engine clock instead of the wall clock is that a sequence's playback controls
/// govern the gaps between segments too, so that is what these assert.
/// </summary>
[TestClass]
public class SequenceSchedulerTests
{
    // Ticks at 16ms steps (a realtime frame) up to `throughMs`, so the caps and per-frame
    // accumulation are exercised the same way the rAF loop exercises them.
    private static void Advance(BmotionSequenceScheduler scheduler, double fromMs, double throughMs)
    {
        for (double t = fromMs; t <= throughMs; t += 16)
            scheduler.Tick(t);
    }

    private static (BmotionSequenceScheduler Scheduler, List<string> Fired) Build(
        params (double At, string Name)[] entries)
    {
        var fired = new List<string>();
        var scheduler = new BmotionSequenceScheduler(
            entries.Select(e => (e.At, (Action)(() => fired.Add(e.Name)))));
        return (scheduler, fired);
    }

    [TestMethod]
    public void FirstTick_OnlyEstablishesTheClockOrigin()
    {
        var (scheduler, fired) = Build((0.5, "a"));

        // A scheduler created mid-session must not inherit the whole page-load elapsed time.
        scheduler.Tick(900_000);

        CollectionAssert.AreEqual(Array.Empty<string>(), fired);
    }

    [TestMethod]
    public void ZeroOffsetEntries_FireOnTheFirstAdvancingTick()
    {
        var (scheduler, fired) = Build((0, "a"));

        scheduler.Tick(0);
        scheduler.Tick(16);

        CollectionAssert.AreEqual(new[] { "a" }, fired);
    }

    [TestMethod]
    public void Entries_FireInTimelineOrder_EvenWhenSuppliedOutOfOrder()
    {
        var (scheduler, fired) = Build((0.4, "third"), (0.1, "first"), (0.2, "second"));

        Advance(scheduler, 0, 600);

        CollectionAssert.AreEqual(new[] { "first", "second", "third" }, fired);
    }

    [TestMethod]
    public void Entry_DoesNotFireBeforeItsTimelinePosition()
    {
        var (scheduler, fired) = Build((0.5, "a"));

        Advance(scheduler, 0, 300);

        CollectionAssert.AreEqual(Array.Empty<string>(), fired);
        Assert.IsFalse(scheduler.IsFinished);
    }

    [TestMethod]
    public void RateZero_HoldsTheTimeline()
    {
        var (scheduler, fired) = Build((0.2, "a"));
        scheduler.Rate = 0;

        Advance(scheduler, 0, 2000);

        CollectionAssert.AreEqual(Array.Empty<string>(), fired,
            "a paused sequence must not keep starting its later segments");
    }

    [TestMethod]
    public void RaisingTheRate_CompressesTheGapsToo()
    {
        var (fast, fastFired) = Build((0.4, "a"));
        fast.Rate = 4;
        Advance(fast, 0, 150);

        var (realtime, realtimeFired) = Build((0.4, "a"));
        Advance(realtime, 0, 150);

        CollectionAssert.AreEqual(new[] { "a" }, fastFired);
        CollectionAssert.AreEqual(Array.Empty<string>(), realtimeFired,
            "at realtime the same 150ms must not have reached a 0.4s offset");
    }

    [TestMethod]
    public void ResumingAfterAPause_ContinuesFromWhereItHeld()
    {
        var (scheduler, fired) = Build((0.3, "a"));

        Advance(scheduler, 0, 160);       // ~0.16s of the 0.3s elapsed
        scheduler.Rate = 0;
        Advance(scheduler, 176, 5000);    // held: no progress at all
        CollectionAssert.AreEqual(Array.Empty<string>(), fired);

        scheduler.Rate = 1;
        Advance(scheduler, 5016, 5200);   // the remaining ~0.14s

        CollectionAssert.AreEqual(new[] { "a" }, fired);
    }

    [TestMethod]
    public void LongFrameGap_IsCapped_SoABackgroundedTabDoesNotFlushTheWholeTimeline()
    {
        var (scheduler, fired) = Build((0.1, "a"), (5.0, "b"));

        scheduler.Tick(0);
        scheduler.Tick(30_000);   // tab was backgrounded for 30 seconds

        CollectionAssert.AreEqual(Array.Empty<string>(), fired,
            "the step is capped at one frame's worth, so 30s of real time cannot flush the timeline");

        // Resuming normally, the timeline picks up from ~0.064s and only the imminent segment runs.
        Advance(scheduler, 30_016, 30_200);
        CollectionAssert.AreEqual(new[] { "a" }, fired);
    }

    [TestMethod]
    public void Cancel_AbandonsEverythingUnfired()
    {
        var (scheduler, fired) = Build((0.1, "a"), (0.2, "b"));

        Advance(scheduler, 0, 120);
        scheduler.Cancel();
        Advance(scheduler, 136, 1000);

        CollectionAssert.AreEqual(new[] { "a" }, fired);
        Assert.IsTrue(scheduler.IsFinished);
    }

    [TestMethod]
    public void Tick_ReportsFinished_OnceEveryEntryHasFired()
    {
        var (scheduler, _) = Build((0.1, "a"));

        scheduler.Tick(0);
        bool finished = false;
        for (double t = 16; t <= 400 && !finished; t += 16) finished = scheduler.Tick(t);

        Assert.IsTrue(finished, "the engine drops the scheduler on this signal");
    }

    [TestMethod]
    public void FaultingEntry_DoesNotStrandTheRestOfTheTimeline()
    {
        var fired = new List<string>();
        var scheduler = new BmotionSequenceScheduler(
        [
            (0.1, () => throw new InvalidOperationException("segment blew up")),
            (0.2, () => fired.Add("b")),
        ]);

        Advance(scheduler, 0, 400);

        CollectionAssert.AreEqual(new[] { "b" }, fired);
    }

    [TestMethod]
    public void Rate_CoercesNegativeAndNonFiniteValuesToZero()
    {
        var (scheduler, _) = Build((1.0, "a"));

        scheduler.Rate = -2;
        Assert.AreEqual(0, scheduler.Rate);

        scheduler.Rate = double.NaN;
        Assert.AreEqual(0, scheduler.Rate);

        scheduler.Rate = double.PositiveInfinity;
        Assert.AreEqual(0, scheduler.Rate);
    }
}
