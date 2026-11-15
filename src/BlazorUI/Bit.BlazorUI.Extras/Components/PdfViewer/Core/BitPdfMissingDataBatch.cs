namespace Bit.BlazorUI;

/// <summary>
/// Collects the missing byte ranges of one synchronous Range pass. Independent
/// units of work (one bookmark, one sibling's Count) defer their miss and let the
/// pass continue, so the pass reports every range it discovered at once instead
/// of restarting for each. A deferring unit never returns a partial result: it
/// calls <see cref="ThrowIfDeferred"/> before publishing anything.
/// <para>
/// The batch is an explicit context of one document's xref, never an ambient of
/// the thread: a unit asks the xref it reads through, so a resolver of another
/// document, or of this one outside the pass, finds no batch and lets its miss
/// propagate. It is also bound to the thread that began it, since the pass is
/// synchronous: a read from any other thread (a UI-thread resolver while a
/// background pass runs, or a unit that awaited) is never deferred into it.
/// Without a batch (buffered documents, direct Document access) nothing is
/// deferred.
/// </para>
/// </summary>
internal sealed class BitPdfMissingDataBatch : IDisposable
{
    private readonly BitPdfXRef _xref;
    private readonly BitPdfMissingDataBatch? _previous;
    private readonly int _thread = Environment.CurrentManagedThreadId;
    private readonly long _maxBytes;
    private readonly int _chunkSize;
    private readonly List<(int Offset, int Length)> _ranges = [];
    private readonly HashSet<(int Offset, int Length)> _known = [];
    private readonly HashSet<long> _chunks = [];
    // Every accepted deferral, a range already known included: a unit must learn
    // that it skipped work even when an earlier unit had deferred the same bytes.
    private int _deferrals;
    private bool _disposed;

    private BitPdfMissingDataBatch(BitPdfXRef xref, long maxBytes, int chunkSize)
    {
        _xref = xref;
        _maxBytes = maxBytes;
        _chunkSize = Math.Max(1, chunkSize);
        _previous = xref.MissingDataBatch;
        xref.MissingDataBatch = this;
    }

    /// <summary>Starts collecting for <paramref name="xref"/> on this thread.
    /// <paramref name="maxBytes"/> bounds the chunk-aligned bytes one pass may hand
    /// to the acquisition that follows, so it stays inside the raw-byte budget;
    /// the first miss is always accepted.</summary>
    internal static BitPdfMissingDataBatch Begin(BitPdfXRef xref, long maxBytes, int chunkSize) => new(xref, maxBytes, chunkSize);

    internal int Count => _ranges.Count;

    // The batch a unit reading through xref on this thread may defer into.
    private static BitPdfMissingDataBatch? For(IBitPdfXRef? xref)
        => xref is BitPdfXRef { MissingDataBatch: { _disposed: false } batch } && batch._thread == Environment.CurrentManagedThreadId
            ? batch
            : null;

    internal static int Mark(IBitPdfXRef? xref) => For(xref)?._deferrals ?? 0;

    /// <summary>An exception filter: records the miss and returns <c>true</c> when the
    /// caller may skip its unit and continue, or <c>false</c> when the miss must
    /// propagate (no batch for this xref and thread, or the batch is full).</summary>
    internal static bool Defer(IBitPdfXRef? xref, BitPdfMissingDataException missing)
    {
        var batch = For(xref);
        if (batch is null)
        {
            return false;
        }

        var added = batch.NewChunks(missing.Ranges);
        if (batch._ranges.Count > 0 && !batch.Fits(added))
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
    /// (a <see cref="Mark"/> of the same xref), carrying the batch's ranges.</summary>
    internal static void ThrowIfDeferred(IBitPdfXRef? xref, int mark = 0)
    {
        if (For(xref) is { } batch && batch._deferrals > mark)
        {
            throw new BitPdfMissingDataException([.. batch._ranges]);
        }
    }

    /// <summary>Every deferred range, plus those of <paramref name="missing"/> (the
    /// miss that ended the pass) when they fit the same byte budget. A miss that
    /// does not fit is left for the next pass, which meets it again once the
    /// deferred ranges are cached: one acquisition never exceeds the budget that
    /// <see cref="Defer"/> enforces, and each pass still makes progress, since a
    /// non-empty batch holds only ranges that were missing.</summary>
    internal List<(int Offset, int Length)> Collect(BitPdfMissingDataException? missing = null)
    {
        var ranges = new List<(int Offset, int Length)>(_ranges);
        if (missing is null)
        {
            return ranges;
        }

        var extra = missing.Ranges.Where(range => !_known.Contains(range)).ToList();
        if (ranges.Count == 0 || Fits(NewChunks(extra)))
        {
            ranges.AddRange(extra);
        }

        return ranges;
    }

    private HashSet<long> NewChunks(IEnumerable<(int Offset, int Length)> ranges)
    {
        var added = new HashSet<long>();
        foreach (var range in ranges)
        {
            if (_known.Contains(range))
            {
                continue;
            }

            long end = (long)range.Offset + Math.Max(1, range.Length);
            for (long chunk = range.Offset / _chunkSize; chunk * _chunkSize < end; chunk++)
            {
                if (!_chunks.Contains(chunk))
                {
                    added.Add(chunk);
                }
            }
        }

        return added;
    }

    private bool Fits(HashSet<long> added) => (_chunks.Count + (long)added.Count) * _chunkSize <= _maxBytes;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (ReferenceEquals(_xref.MissingDataBatch, this))
        {
            _xref.MissingDataBatch = _previous;
        }
    }
}
