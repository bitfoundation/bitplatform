using System;

namespace Bit.BlazorUI;

/// <summary>
/// Internal random access to original PDF bytes, independent of a read cursor.
/// Offsets are absolute and remain int-sized, as in the existing engine.
/// Reads are synchronous; this contract does not perform asynchronous I/O.
/// </summary>
internal interface IBitPdfByteSource
{
    /// <summary>The complete, fixed byte length of the source.</summary>
    int Length { get; }

    /// <summary>
    /// Reads a byte, or -1 at exactly Length. Offsets outside [0, Length] throw.
    /// </summary>
    int ReadByte(int offset);

    /// <summary>
    /// Copies as many bytes as fit in the destination, clamped at EOF, and
    /// returns the number copied. Offsets outside [0, Length] throw.
    /// </summary>
    int Read(int offset, Span<byte> destination);

    /// <summary>
    /// Borrows a read-only view of an exact range, without copying. The entire
    /// range must be within the source. The backing storage belongs to the source.
    /// </summary>
    ReadOnlyMemory<byte> GetMemory(int offset, int length);

    /// <summary>
    /// Creates an independent cursor over an exact range, or the remainder when
    /// length is null. The window shares the source's storage and optional dictionary.
    /// </summary>
    BitPdfBaseStream CreateStream(int start = 0, int? length = null, BitPdfDict? dict = null);
}
