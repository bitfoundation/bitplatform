using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Memory over time: the heap read once a second, each full collection a timeline event with the heap after it, and a
/// finding when what survives full collections keeps climbing. Fed readings, so no real collection decides the outcome.
/// </summary>
[TestClass]
public class MemoryTrendTests
{
    private const long MB = 1024 * 1024;

    private static BevMemoryReading Reading(long heapMb, int full, long index = 0, long afterMb = 0, double? pause = null, long? linear = null) =>
        new(heapMb * MB, full, pause ?? 0, index, afterMb * MB, pause, linear);

    [TestMethod]
    public void Readings_are_taken_at_most_once_a_second_and_a_full_collection_is_a_timeline_event()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var memory = session.Memory;
            var t0 = DateTimeOffset.UtcNow;
            Assert.IsTrue(memory.Sample(session, t0, () => Reading(40, 3, index: 3, afterMb: 30)));
            Assert.IsFalse(memory.Sample(session, t0.AddMilliseconds(400), () => Reading(41, 3)), "within the second");
            Assert.IsTrue(memory.Sample(session, t0.AddSeconds(1), () => Reading(25, 4, index: 4, afterMb: 22, pause: 12)));
            Assert.IsTrue(memory.Sample(session, t0.AddSeconds(2), () => Reading(28, 5, index: 4)));

            Assert.HasCount(3, memory.Samples);
            var collections = memory.FullCollections;
            Assert.HasCount(2, collections);
            Assert.AreEqual(22 * MB, collections[0].HeapAfterBytes);
            Assert.IsTrue(collections[0].Exact);
            Assert.AreEqual(12, collections[0].PauseMs);
            Assert.IsFalse(collections[1].Exact, "the runtime reported no new collection: the heap at the next reading stands for it");
            Assert.AreEqual(28 * MB, collections[1].HeapAfterBytes);

            var events = session.Timeline.Snapshot().Where(e => e.Category == "gc").ToArray();
            Assert.HasCount(2, events);
            Assert.AreEqual("Full garbage collection: heap 40 MB → 22 MB", events[0].Title);
            Assert.AreEqual(12, events[0].DurationMs);
            StringAssert.StartsWith(events[1].Detail, "the heap at the first reading after it");
        }
    }

    [TestMethod]
    public void A_cleared_trend_starts_over_and_tells_collections_against_readings_made_after_it()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var memory = session.Memory;
            var t0 = DateTimeOffset.UtcNow;
            Assert.IsTrue(memory.Sample(session, t0, () => Reading(40, 3, index: 3, afterMb: 30)));
            session.ClearAll();

            // Within the second of the last reading, and with collections since it: taken, and no collection put down to
            // a reading the clear threw away.
            Assert.IsTrue(memory.Sample(session, t0.AddMilliseconds(300), () => Reading(25, 4, index: 4, afterMb: 22)));
            var sample = Assert.ContainsSingle(memory.Samples);
            Assert.AreEqual(25 * MB, sample.HeapBytes);
            Assert.AreEqual(t0.AddMilliseconds(300), sample.At);
            Assert.IsEmpty(memory.FullCollections);
            Assert.IsFalse(session.Timeline.Snapshot().Any(e => e.Category == "gc"));

            // The next one is told against that first reading after the clear.
            Assert.IsTrue(memory.Sample(session, t0.AddMilliseconds(1300), () => Reading(20, 5, index: 5, afterMb: 18, pause: 6)));
            var collection = Assert.ContainsSingle(memory.FullCollections);
            Assert.AreEqual(18 * MB, collection.HeapAfterBytes);
            var gc = Assert.ContainsSingle(e => e.Category == "gc", session.Timeline.Snapshot());
            Assert.AreEqual("Full garbage collection: heap 25 MB → 18 MB", gc.Title);
        }
    }

    [TestMethod]
    public void Paused_the_trend_takes_no_readings_and_no_collections()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var memory = session.Memory;
            var t0 = DateTimeOffset.UtcNow;
            memory.Sample(session, t0, () => Reading(40, 3, index: 3, afterMb: 30));
            session.PauseCapture();
            memory.Sample(session, t0.AddSeconds(1), () => Reading(30, 4, index: 4, afterMb: 25));
            Assert.ContainsSingle(memory.Samples);
            Assert.IsEmpty(memory.FullCollections);

            session.ResumeCapture();
            memory.Sample(session, t0.AddSeconds(2), () => Reading(35, 4, index: 4, afterMb: 25));
            Assert.HasCount(2, memory.Samples);
            Assert.IsEmpty(memory.FullCollections, "the collection made while paused is not put down to the first reading after it");
        }
    }

    [TestMethod]
    public void Exports_keep_at_most_120_readings()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var t0 = DateTimeOffset.UtcNow.AddMinutes(-5);
            for (var i = 0; i < 200; i++)
            {
                session.Memory.Sample(session, t0.AddSeconds(i), () => Reading(40, 1));
            }

            using var json = JsonDocument.Parse(BevSessionExporter.ToJson(session, new BevSessionExportOptions { Sections = new HashSet<string> { "runtime" } }));
            var count = json.RootElement.GetProperty("memory").GetProperty("samples").GetArrayLength();
            Assert.IsTrue(count is > 0 and <= 120, "readings: " + count);
        }
    }

    [TestMethod]
    public void A_heap_that_climbs_over_full_collections_is_a_finding_and_one_that_returns_is_not()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var t0 = DateTimeOffset.UtcNow.AddMinutes(-6);
            long[] climbing = [30, 38, 47, 55, 66, 80];
            session.Memory.Sample(session, t0, () => Reading(30, 0));
            for (var i = 0; i < climbing.Length; i++)
            {
                var after = climbing[i];
                session.Memory.Sample(session, t0.AddMinutes(i + 0.5), () => Reading(after + 5, i + 1, index: i + 1, afterMb: after));
            }

            session.Diagnostics.Evaluate(force: true);
            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "memory.growth", session.Diagnostics.Findings).Diagnostic;
            Assert.AreEqual("The heap after full collections grew from 30 MB to 80 MB over 6 collections", finding.Title);
        }

        (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var t0 = DateTimeOffset.UtcNow.AddMinutes(-6);
            long[] sawtooth = [30, 60, 31, 70, 32, 80];
            session.Memory.Sample(session, t0, () => Reading(30, 0));
            for (var i = 0; i < sawtooth.Length; i++)
            {
                var after = sawtooth[i];
                session.Memory.Sample(session, t0.AddMinutes(i + 0.5), () => Reading(after + 5, i + 1, index: i + 1, afterMb: after));
            }

            session.Diagnostics.Evaluate(force: true);
            Assert.DoesNotContain(f => f.Diagnostic.RuleId == "memory.growth", session.Diagnostics.Findings, "back near where it started: what is retained is released again");
        }
    }

    [TestMethod]
    public void Exports_carry_the_readings_and_the_collections()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var t0 = DateTimeOffset.UtcNow;
            session.Memory.Sample(session, t0, () => Reading(40, 1, linear: 80 * MB));
            session.Memory.Sample(session, t0.AddSeconds(1), () => Reading(30, 2, index: 9, afterMb: 25, pause: 4, linear: 96 * MB));

            using var json = JsonDocument.Parse(BevSessionExporter.ToJson(session, new BevSessionExportOptions { Sections = new HashSet<string> { "runtime" } }));
            var memory = json.RootElement.GetProperty("memory");
            Assert.AreEqual(2, memory.GetProperty("samples").GetArrayLength());
            Assert.AreEqual(96 * MB, memory.GetProperty("samples")[1].GetProperty("linearMemoryBytes").GetInt64());
            var collection = memory.GetProperty("fullCollections")[0];
            Assert.AreEqual(25 * MB, collection.GetProperty("heapAfterBytes").GetInt64());
            Assert.IsTrue(collection.GetProperty("exact").GetBoolean());
        }
    }

    [TestMethod]
    public void The_runtime_reads_its_own_counters()
    {
        var reading = BevMemoryReading.Now(null);
        Assert.IsTrue(reading.HeapBytes > 0);
        Assert.IsTrue(reading.FullCollections >= 0);
    }
}

[TestClass]
public class MemoryTrendPanelTests : BunitContext
{
    public MemoryTrendPanelTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.Ui.RefreshIntervalMs = 600_000;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("WebAssembly");
    }

    [TestMethod]
    public void The_runtime_panel_draws_the_heap_marks_full_collections_and_shows_linear_memory()
    {
        var session = Services.GetRequiredService<BevToolsSession>();
        var t0 = DateTimeOffset.UtcNow.AddSeconds(-30);
        session.Memory.Sample(session, t0, () => new BevMemoryReading(40L << 20, 1, 0, 0, 0, null, 80L << 20));
        session.Memory.Sample(session, t0.AddSeconds(10), () => new BevMemoryReading(30L << 20, 2, 3, 2, 25L << 20, 3, 96L << 20));

        var panel = Render<BevToolsPanel>();
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Runtime", StringComparison.Ordinal)).Click();
        var text = panel.Find(".bev-body").TextContent;
        StringAssert.Contains(text, "Memory over time");
        StringAssert.Contains(text, "WebAssembly memory");
        StringAssert.Contains(text, "96 MB");
        StringAssert.Contains(text, "80 MB at the start of the window");
        StringAssert.Contains(text, "heap after the last: 25 MB");
        Assert.IsNotNull(panel.Find(".bev-body svg polyline"));
        Assert.HasCount(1, panel.FindAll(".bev-body svg line"));
    }
}
