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

    internal BitPdfMissingDataException(int offset, int length)
        : base($"PDF bytes [{offset}, {(long)offset + length}) are not cached. Use an asynchronous PDF viewer operation.")
    {
        Offset = offset;
        Length = length;
    }
}
