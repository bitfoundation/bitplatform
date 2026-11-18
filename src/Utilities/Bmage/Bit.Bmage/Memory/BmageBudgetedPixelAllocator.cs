using System.Buffers;

namespace Bit.Bmage;

/// <summary>
/// An allocator with a hard ceiling: it hands out memory from another allocator while the bytes it has outstanding
/// stay within <see cref="MaxBytes"/>, and refuses with <see cref="BmageImageMemoryBudgetExceededException"/> past that,
/// before anything is allocated. It also records the most it ever had outstanding, which is the honest answer to how
/// much memory a run needs.
/// </summary>
/// <remarks>
/// <para>
/// Every pixel buffer, mask and intermediate an operation makes comes from the allocator in its context, so wrapping
/// one session, request or batch in its own budget bounds what it can take from the process, and a phone gets a clear
/// error instead of a killed tab. Small bookkeeping arrays (a row of scratch, a lookup table) come from the shared
/// array pool and are not counted; they are kilobytes against megabytes.
/// </para>
/// <para>Thread safe: operations running on several threads share one budget.</para>
/// </remarks>
/// <example>
/// <code>
/// var budget = new BmageBudgetedPixelAllocator(256L * 1024 * 1024);
/// var processor = new BmageImageProcessor(BmageImageCodecRegistry.CreateDefault(), budget);
/// var result = await processor.ProcessAsync(upload, pipeline);
/// Console.WriteLine($"Peak: {budget.PeakBytes / 1048576.0:0.0} MB");
/// </code>
/// </example>
public sealed class BmageBudgetedPixelAllocator : IBmagePixelAllocator
{
    private readonly IBmagePixelAllocator _inner;
    private long _current;
    private long _peak;

    /// <param name="maxBytes">The most bytes that may be outstanding at once.</param>
    /// <param name="inner">Where the memory comes from. Defaults to the shared pooled allocator.</param>
    public BmageBudgetedPixelAllocator(long maxBytes, IBmagePixelAllocator? inner = null)
    {
        if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes), "A budget must be positive.");
        MaxBytes = maxBytes;
        _inner = inner ?? BmagePooledPixelAllocator.Shared;
    }

    /// <summary>The most bytes that may be outstanding at once.</summary>
    public long MaxBytes { get; }

    /// <summary>Bytes handed out and not yet returned.</summary>
    public long CurrentBytes => Interlocked.Read(ref _current);

    /// <summary>The most bytes that were ever outstanding at once.</summary>
    public long PeakBytes => Interlocked.Read(ref _peak);

    /// <summary>Bytes that can still be handed out.</summary>
    public long AvailableBytes => Math.Max(0, MaxBytes - CurrentBytes);

    /// <summary>Starts recording the peak afresh from what is outstanding now.</summary>
    public void ResetPeak() => Interlocked.Exchange(ref _peak, CurrentBytes);

    /// <inheritdoc />
    /// <exception cref="BmageImageMemoryBudgetExceededException">Handing out <paramref name="byteCount"/> bytes would exceed the budget.</exception>
    public IMemoryOwner<byte> Rent(int byteCount)
    {
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
        // Reserved only when it fits, so a refused request never shows as in use to another thread renting at once.
        long current;
        while (true)
        {
            var outstanding = Interlocked.Read(ref _current);
            current = outstanding + byteCount;
            if (current > MaxBytes) throw new BmageImageMemoryBudgetExceededException(byteCount, outstanding, MaxBytes);
            if (Interlocked.CompareExchange(ref _current, current, outstanding) == outstanding) break;
        }
        UpdatePeak(current);
        try
        {
            return new Owner(this, _inner.Rent(byteCount), byteCount);
        }
        catch
        {
            Interlocked.Add(ref _current, -byteCount);
            throw;
        }
    }

    private void UpdatePeak(long current)
    {
        var peak = Interlocked.Read(ref _peak);
        while (current > peak)
        {
            var seen = Interlocked.CompareExchange(ref _peak, current, peak);
            if (seen == peak) return;
            peak = seen;
        }
    }

    private sealed class Owner(BmageBudgetedPixelAllocator budget, IMemoryOwner<byte> inner, int byteCount) : IMemoryOwner<byte>
    {
        private IMemoryOwner<byte>? _inner = inner;

        public Memory<byte> Memory => (_inner ?? throw new ObjectDisposedException(nameof(Owner))).Memory;

        public void Dispose()
        {
            var owned = Interlocked.Exchange(ref _inner, null);
            if (owned is null) return;
            owned.Dispose();
            Interlocked.Add(ref budget._current, -byteCount);
        }
    }
}
