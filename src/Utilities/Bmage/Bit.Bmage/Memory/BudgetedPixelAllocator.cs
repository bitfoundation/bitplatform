using System.Buffers;

namespace Bit.Bmage.Memory;

/// <summary>
/// An allocator with a hard ceiling: it hands out memory from another allocator while the bytes it has outstanding
/// stay within <see cref="MaxBytes"/>, and refuses with <see cref="ImageMemoryBudgetExceededException"/> past that,
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
/// var budget = new BudgetedPixelAllocator(256L * 1024 * 1024);
/// var processor = new ImageProcessor(ImageCodecRegistry.CreateDefault(), budget);
/// var result = await processor.ProcessAsync(upload, pipeline);
/// Console.WriteLine($"Peak: {budget.PeakBytes / 1048576.0:0.0} MB");
/// </code>
/// </example>
public sealed class BudgetedPixelAllocator : IPixelAllocator
{
    private readonly IPixelAllocator _inner;
    private long _current;
    private long _peak;

    /// <param name="maxBytes">The most bytes that may be outstanding at once.</param>
    /// <param name="inner">Where the memory comes from. Defaults to the shared pooled allocator.</param>
    public BudgetedPixelAllocator(long maxBytes, IPixelAllocator? inner = null)
    {
        if (maxBytes <= 0) throw new ArgumentOutOfRangeException(nameof(maxBytes), "A budget must be positive.");
        MaxBytes = maxBytes;
        _inner = inner ?? PooledPixelAllocator.Shared;
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
    /// <exception cref="ImageMemoryBudgetExceededException">Handing out <paramref name="byteCount"/> bytes would exceed the budget.</exception>
    public IMemoryOwner<byte> Rent(int byteCount)
    {
        if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
        // Reserved only when it fits, so a refused request never shows as in use to another thread renting at once.
        long current;
        while (true)
        {
            var outstanding = Interlocked.Read(ref _current);
            current = outstanding + byteCount;
            if (current > MaxBytes) throw new ImageMemoryBudgetExceededException(byteCount, outstanding, MaxBytes);
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

    private sealed class Owner(BudgetedPixelAllocator budget, IMemoryOwner<byte> inner, int byteCount) : IMemoryOwner<byte>
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

/// <summary>Thrown when an allocation would take a <see cref="BudgetedPixelAllocator"/> past its budget.</summary>
public sealed class ImageMemoryBudgetExceededException : ImageException
{
    public ImageMemoryBudgetExceededException(long requestedBytes, long outstandingBytes, long budgetBytes)
        : base($"Allocating {Megabytes(requestedBytes)} would exceed the memory budget of {Megabytes(budgetBytes)}, of which {Megabytes(outstandingBytes)} is in use. Process a smaller image, cap its size first with MaxSize, or raise the budget.")
    {
        RequestedBytes = requestedBytes;
        OutstandingBytes = outstandingBytes;
        BudgetBytes = budgetBytes;
    }

    /// <summary>The allocation that was refused.</summary>
    public long RequestedBytes { get; }

    /// <summary>What was already in use.</summary>
    public long OutstandingBytes { get; }

    /// <summary>The budget.</summary>
    public long BudgetBytes { get; }

    private static string Megabytes(long bytes) => $"{bytes / (1024.0 * 1024.0):0.#} MB";
}
