namespace Bit.BlazorUI;

/// <summary>A synchronous read of a Range-backed document needs bytes which
/// are not currently cached. Use the viewer's asynchronous operations to acquire
/// their dependencies; directly accessing Document never starts HTTP requests.</summary>
public sealed class BitPdfMissingDataException : Exception
{
    /// <summary>The document-relative offset of the required byte range.</summary>
    public int Offset { get; }

    /// <summary>The number of bytes required by the read.</summary>
    public int Length { get; }

    // Every range a pass found missing; the first one is Offset/Length.
    internal IReadOnlyList<(int Offset, int Length)> Ranges { get; }

    internal BitPdfMissingDataException(int offset, int length)
        : this([(offset, length)])
    {
    }

    internal BitPdfMissingDataException(IReadOnlyList<(int Offset, int Length)> ranges)
        : base($"PDF bytes [{ranges[0].Offset}, {(long)ranges[0].Offset + ranges[0].Length}) are not cached. Use an asynchronous PDF viewer operation.")
    {
        Offset = ranges[0].Offset;
        Length = ranges[0].Length;
        Ranges = ranges;
    }
}
