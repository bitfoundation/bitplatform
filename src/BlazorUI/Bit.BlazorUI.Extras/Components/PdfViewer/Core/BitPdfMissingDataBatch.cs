namespace Bit.BlazorUI;

/// <summary>
/// Collects the missing byte ranges of one synchronous Range pass. Independent
/// units of work (one bookmark, one sibling's Count) defer their miss and let the
/// pass continue, so the pass reports every range it discovered at once instead
/// of restarting for each. A deferring unit never returns a partial result: it
/// calls <see cref="ThrowIfDeferred"/> before publishing anything. Without an
/// active batch (buffered documents, direct Document access) nothing is deferred.
/// </summary>
internal sealed class BitPdfMissingDataBatch : IDisposable
{
    [ThreadStatic]
    private static BitPdfMissingDataBatch? _current;

    private readonly BitPdfMissingDataBatch? _previous;
    private readonly long _maxBytes;
    private readonly int _chunkSize;
    private readonly List<(int Offset, int Length)> _ranges = [];
    private readonly HashSet<(int Offset, int Length)> _known = [];
    private readonly HashSet<long> _chunks = [];
    // Every accepted deferral, a range already known included: a unit must learn
    // that it skipped work even when an earlier unit had deferred the same bytes.
    private int _deferrals;
    private bool _disposed;

    private BitPdfMissingDataBatch(long maxBytes, int chunkSize)
    {
        _maxBytes = maxBytes;
        _chunkSize = Math.Max(1, chunkSize);
        _previous = _current;
        _current = this;
    }

    /// <summary>Starts collecting on this thread. <paramref name="maxBytes"/> bounds the
    /// chunk-aligned bytes one pass may defer, so the acquisition that follows stays
    /// inside the raw-byte budget; the first miss is always accepted.</summary>
    internal static BitPdfMissingDataBatch Begin(long maxBytes, int chunkSize) => new(maxBytes, chunkSize);

    internal int Count => _ranges.Count;

    internal static int Mark => _current?._deferrals ?? 0;

    /// <summary>An exception filter: records the miss and returns <c>true</c> when the
    /// caller may skip its unit and continue, or <c>false</c> when the miss must
    /// propagate (no batch, or the batch is full).</summary>
    internal static bool Defer(BitPdfMissingDataException missing)
    {
        var batch = _current;
        if (batch is null)
        {
            return false;
        }

        var added = new HashSet<long>();
        foreach (var range in missing.Ranges)
        {
            if (batch._known.Contains(range))
            {
                continue;
            }

            long end = (long)range.Offset + Math.Max(1, range.Length);
            for (long chunk = range.Offset / batch._chunkSize; chunk * batch._chunkSize < end; chunk++)
            {
                if (!batch._chunks.Contains(chunk))
                {
                    added.Add(chunk);
                }
            }
        }

        if (batch._ranges.Count > 0 && (batch._chunks.Count + (long)added.Count) * batch._chunkSize > batch._maxBytes)
        {
            return false;
        }

        foreach (var range in missing.Ranges)
        {
            if (batch._known.Add(range))
            {
                batch._ranges.Add(range);
            }
        }

        batch._chunks.UnionWith(added);
        batch._deferrals++;
        return true;
    }

    /// <summary>Throws when anything was deferred since <paramref name="mark"/>
    /// (a <see cref="Mark"/>), carrying the batch's ranges.</summary>
    internal static void ThrowIfDeferred(int mark = 0)
    {
        if (_current is { } batch && batch._deferrals > mark)
        {
            throw new BitPdfMissingDataException([.. batch._ranges]);
        }
    }

    /// <summary>Every deferred range plus those of <paramref name="missing"/>, if given.</summary>
    internal List<(int Offset, int Length)> Collect(BitPdfMissingDataException? missing = null)
    {
        var ranges = new List<(int Offset, int Length)>(_ranges);
        if (missing is not null)
        {
            foreach (var range in missing.Ranges)
            {
                if (!_known.Contains(range))
                {
                    ranges.Add(range);
                }
            }
        }

        return ranges;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (ReferenceEquals(_current, this))
        {
            _current = _previous;
        }
    }
}
