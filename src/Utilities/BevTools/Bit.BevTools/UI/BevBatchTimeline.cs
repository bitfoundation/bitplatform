using System.Diagnostics;

namespace Bit.BevTools;

/// <summary>One render on a batch's time axis: when it started after the batch's first render, how long it took, how deep it is.</summary>
internal sealed record BevTimelineBar(long InstanceId, string Name, int Depth, double StartMs, double DurationMs);

/// <summary>
/// What one render batch rendered, in time: every render the components still hold for it, placed at the moment it
/// started (each sample's own timestamp) and on the row of its depth among what the batch rendered - Blazor renders a
/// batch's components one after another, so the chart reads left to right in the order the renderer took them.
/// </summary>
internal sealed class BevBatchTimeline
{
    public static readonly BevBatchTimeline Empty = new([], 0, 0, 0);

    private BevBatchTimeline(IReadOnlyList<BevTimelineBar> bars, double spanMs, int depth, int omitted)
    {
        Bars = bars;
        SpanMs = spanMs;
        Depth = depth;
        Omitted = omitted;
    }

    public IReadOnlyList<BevTimelineBar> Bars { get; }

    /// <summary>From the first render's start to the last render's end.</summary>
    public double SpanMs { get; }

    /// <summary>The deepest row used, plus one.</summary>
    public int Depth { get; }

    /// <summary>Renders past <c>maxBars</c>, the latest left out.</summary>
    public int Omitted { get; }

    public static BevBatchTimeline Build(IEnumerable<BevComponentRecord> records, long batchId, int maxBars)
    {
        var samples = new List<(BevComponentRecord Record, BevRenderSample Sample)>();
        var rendered = new HashSet<long>();
        foreach (var record in records)
        {
            if (record.IsBevTools || record.IsHidden)
            {
                continue;
            }

            foreach (var sample in record.Renders)
            {
                if (sample.BatchId == batchId)
                {
                    samples.Add((record, sample));
                    rendered.Add(record.InstanceId);
                }
            }
        }

        if (samples.Count == 0)
        {
            return Empty;
        }

        samples.Sort((a, b) => a.Sample.StartTicks.CompareTo(b.Sample.StartTicks));
        var first = samples[0].Sample.StartTicks;
        // A render's row: how many of its ancestors rendered in the same batch. Remembered for every component walked,
        // so a deep tree is walked once, without recursion.
        var above = new Dictionary<long, int>();
        int DepthOf(BevComponentRecord record)
        {
            var path = new List<BevComponentRecord>();
            var count = 0;
            for (var current = record; current is not null; current = current.Parent)
            {
                if (above.TryGetValue(current.InstanceId, out var known))
                {
                    // What is above the known one, and the known one itself when it rendered too.
                    count = known + (rendered.Contains(current.InstanceId) ? 1 : 0);
                    break;
                }

                path.Add(current);
            }

            for (var i = path.Count - 1; i >= 0; i--)
            {
                above[path[i].InstanceId] = count;
                if (rendered.Contains(path[i].InstanceId))
                {
                    count++;
                }
            }

            return above[record.InstanceId];
        }

        var bars = new List<BevTimelineBar>(Math.Min(samples.Count, maxBars));
        var span = 0d;
        foreach (var (record, sample) in samples.Take(maxBars))
        {
            var start = Stopwatch.GetElapsedTime(first, sample.StartTicks).TotalMilliseconds;
            bars.Add(new BevTimelineBar(record.InstanceId, record.DisplayName, DepthOf(record), start, sample.DurationMs));
            span = Math.Max(span, start + sample.DurationMs);
        }

        return new BevBatchTimeline(bars, span, bars.Max(b => b.Depth) + 1, Math.Max(0, samples.Count - maxBars));
    }
}
