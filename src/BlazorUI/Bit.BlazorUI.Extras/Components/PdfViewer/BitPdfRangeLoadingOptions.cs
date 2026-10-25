namespace Bit.BlazorUI;

/// <summary>Controls opt-in HTTP Range loading of a URL PDF source.</summary>
public sealed class BitPdfRangeLoadingOptions
{
    /// <summary>The raw-byte cache budget, in bytes. Default is 16 MiB.
    /// Includes pinned windows and in-flight buffers; decoded resources and
    /// explicit caller-owned copies are separate allocations.</summary>
    public int CacheSize { get; init; } = 16 * 1024 * 1024;

    /// <summary>The minimum request size and byte alignment. Default is 64 KiB.
    /// Adjacent requests can be combined; the final chunk is clipped at EOF.</summary>
    public int ChunkSize { get; init; } = 64 * 1024;

    /// <summary>The largest permitted buffered fallback, in bytes. Null (the
    /// default) means no additional cap; zero disables fallback. This applies
    /// only before the Range document is published.</summary>
    public long? MaxFallbackSize { get; init; }

    internal void Validate()
    {
        if (ChunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(ChunkSize));
        if (CacheSize < ChunkSize) throw new ArgumentOutOfRangeException(nameof(CacheSize));
        if (MaxFallbackSize < 0) throw new ArgumentOutOfRangeException(nameof(MaxFallbackSize));
    }
}
