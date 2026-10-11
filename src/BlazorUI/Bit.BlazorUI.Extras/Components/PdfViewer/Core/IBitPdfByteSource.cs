using System;

namespace Bit.BlazorUI;

/// <summary>
/// Internal random access to original PDF bytes, independent of a read cursor.
/// Offsets are absolute and remain int-sized, as in the existing engine.
/// Reads are synchronous; this contract does not perform asynchronous I/O.
/// </summary>
/// <remarks>
/// Borrowed storage must not be mutated or recycled while a consumer retains a
/// view. The caller of a memory source must keep its input bytes stable during
/// parsing. A future bounded source must account for held/pinned views and
/// in-flight buffers, not just the entries remaining in its cache.
/// HTTP support will use a dedicated missing-data exception and asynchronous
/// acquisition outside the engine. Every malformed-content catch-all must let
/// that signal and cancellation escape; neither may initiate recovery scanning.
/// The exception, catch filters and retry coordinator belong to that later stage.
/// </remarks>
internal interface IBitPdfByteSource
{
    /// <summary>The complete, fixed byte length of the source.</summary>
    int Length { get; }

    /// <summary>
    /// Reports an already available complete, stable buffer of exactly Length
    /// bytes. This capability check must not read or materialize the source.
    /// </summary>
    bool TryGetContiguous(out ReadOnlyMemory<byte> memory);

    /// <summary>
    /// Borrows a read-only view of an exact range, without copying. The entire
    /// range must be within the source. The backing storage belongs to the source.
    /// </summary>
    ReadOnlyMemory<byte> GetMemory(int offset, int length);

    /// <summary>
    /// Creates an independent cursor over an exact range, or the remainder when
    /// length is null. The window shares the source's storage; the parser attaches
    /// the PDF dictionary after creating it.
    /// </summary>
    BitPdfBaseStream CreateStream(int start = 0, int? length = null);
}
