// Code-range lookup shared by the CMap readers (cidrange, bfrange).

namespace Bit.BlazorUI;

/// <summary>
/// Flattens a list of code ranges, which may overlap, into sorted and disjoint segments. Each
/// segment is owned by the latest range that covers it - the one left in place by a reader that
/// writes every range into one map in stream order, as pdf.js does, so a later entry corrects an
/// earlier one - and a lookup is a binary search rather than a scan of every range for every code.
/// </summary>
internal static class BitPdfRangeIndex
{
    public static (long Lo, long Hi, int Owner)[] Build(int count, Func<int, (long Lo, long Hi)> rangeAt)
    {
        var points = new List<(long At, int Index, bool Start)>(count * 2);
        for (int i = 0; i < count; i++)
        {
            var (lo, hi) = rangeAt(i);
            if (lo <= hi)
            {
                points.Add((lo, i, true));
                // A range reaching the top code has no code after it to end at: it is left open, and
                // the last segment runs on to the top.
                if (hi < long.MaxValue)
                {
                    points.Add((hi + 1, i, false));
                }
            }
        }
        points.Sort((a, b) => a.At.CompareTo(b.At));

        var active = new PriorityQueue<int, int>();
        var ended = new bool[count];
        var segments = new List<(long Lo, long Hi, int Owner)>();
        int p = 0;
        while (p < points.Count)
        {
            long at = points[p].At;
            for (; p < points.Count && points[p].At == at; p++)
            {
                if (points[p].Start)
                {
                    active.Enqueue(points[p].Index, -points[p].Index); // the latest range on top
                }
                else
                {
                    ended[points[p].Index] = true;
                }
            }
            while (active.TryPeek(out int top, out _) && ended[top])
            {
                active.Dequeue();
            }
            if (active.TryPeek(out int owner, out _))
            {
                segments.Add((at, p < points.Count ? points[p].At - 1 : long.MaxValue, owner));
            }
        }
        return [.. segments];
    }

    /// <summary>The index of the range that owns <paramref name="code"/>, or -1 when no range covers it.</summary>
    public static int Find((long Lo, long Hi, int Owner)[] segments, long code)
    {
        int low = 0, high = segments.Length - 1;
        while (low <= high)
        {
            int mid = (low + high) >>> 1;
            if (segments[mid].Lo <= code)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        return high >= 0 && code <= segments[high].Hi ? segments[high].Owner : -1;
    }
}
