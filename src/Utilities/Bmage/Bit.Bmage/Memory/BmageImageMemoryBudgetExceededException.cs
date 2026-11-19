namespace Bit.Bmage;

/// <summary>Thrown when an allocation would take a <see cref="BmageBudgetedPixelAllocator"/> past its budget.</summary>
public sealed class BmageImageMemoryBudgetExceededException : BmageImageException
{
    public BmageImageMemoryBudgetExceededException(long requestedBytes, long outstandingBytes, long budgetBytes)
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
