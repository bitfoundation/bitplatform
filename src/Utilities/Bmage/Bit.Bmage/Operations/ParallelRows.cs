using System.Runtime.ExceptionServices;

namespace Bit.Bmage.Operations;

/// <summary>Processes rows <paramref name="start"/> (inclusive) to <paramref name="end"/> (exclusive) of an image.</summary>
/// <param name="start">The first row of the band.</param>
/// <param name="end">The row after the last row of the band.</param>
/// <param name="context">
/// The context to check cancellation on and report progress through, as a fraction of the whole image. Inside a band
/// that runs beside others it reports nothing, since the bands finish in no particular order; the bands' completion is
/// reported instead.
/// </param>
internal delegate void RowBand(int start, int end, OperationContext context);

/// <summary>
/// Spreads the rows of an operation over <see cref="OperationContext.MaxDegreeOfParallelism"/> threads, for operations
/// whose rows can be computed independently of one another.
/// </summary>
/// <remarks>
/// <para>
/// Every row is computed by exactly the code that would compute it on one thread, from the same inputs, so the pixels
/// are identical however many threads run. Each band brings its own scratch rows, rented from the pool, so the extra
/// memory is a few rows per thread rather than anything proportional to the image.
/// </para>
/// <para>
/// With one thread allowed, or too little work for handing it out to pay, the body runs once on the caller's thread
/// with the caller's context, which is exactly the sequential loop it replaced.
/// </para>
/// </remarks>
internal static class ParallelRows
{
    /// <summary>Below this many pixels a band costs more to hand to another thread than it saves.</summary>
    internal const long MinimumPixelsPerBand = 1 << 15;

    /// <summary>
    /// Runs <paramref name="body"/> over rows <paramref name="top"/> to <paramref name="bottom"/>, in bands of whole rows.
    /// <paramref name="pixelsPerRow"/> is a measure of the work in one row, used to decide whether bands are worth it.
    /// </summary>
    public static void For(OperationContext context, int top, int bottom, long pixelsPerRow, RowBand body)
    {
        var rows = bottom - top;
        if (rows <= 0) return;
        var bands = BandCount(context.MaxDegreeOfParallelism, rows, pixelsPerRow);
        if (bands <= 1)
        {
            body(top, bottom, context);
            return;
        }

        // A band beside others must not report progress itself, and must not fan out again.
        var bandContext = new OperationContext(context.Allocator, context.Limits, context.CancellationToken, progress: null, context.CanMutateSource, maxDegreeOfParallelism: 1);
        var progress = context.Progress is null ? null : new BandProgress(context, rows);
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(context.MaxDegreeOfParallelism, bands),
            CancellationToken = context.CancellationToken,
        };
        try
        {
            Parallel.For(0, bands, options, band =>
            {
                // Bands of nearly equal height, the first rows - rows % bands of them one row taller.
                var start = top + (int)((long)rows * band / bands);
                var end = top + (int)((long)rows * (band + 1) / bands);
                body(start, end, bandContext);
                progress?.Completed(end - start);
            });
        }
        catch (AggregateException ex)
        {
            context.ThrowIfCancellationRequested();
            // Rethrow what the band threw, so callers see the same exception the sequential loop would raise.
            var inner = ex.Flatten().InnerExceptions;
            ExceptionDispatchInfo.Capture(inner.FirstOrDefault(e => e is not OperationCanceledException) ?? inner[0]).Throw();
        }
    }

    /// <summary>
    /// How many bands to split the rows into: at most twice the threads, so a band that happens to be slow (rows mostly
    /// outside a rotated image are cheap, rows through it are not) does not leave the others idle, and never so many
    /// that a band holds less than <see cref="MinimumPixelsPerBand"/>.
    /// </summary>
    internal static int BandCount(int maxDegreeOfParallelism, int rows, long pixelsPerRow)
    {
        if (maxDegreeOfParallelism <= 1 || rows <= 1) return 1;
        var byWork = (long)rows * Math.Max(1, pixelsPerRow) / MinimumPixelsPerBand;
        return (int)Math.Max(1, Math.Min(Math.Min(rows, 2L * maxDegreeOfParallelism), byWork));
    }

    /// <summary>Reports the share of rows whose bands have finished, never going backwards.</summary>
    private sealed class BandProgress(OperationContext context, int rows)
    {
        private readonly object _lock = new();
        private int _done;

        public void Completed(int count)
        {
            lock (_lock)
            {
                _done += count;
                context.ReportProgress(_done / (double)rows);
            }
        }
    }
}
