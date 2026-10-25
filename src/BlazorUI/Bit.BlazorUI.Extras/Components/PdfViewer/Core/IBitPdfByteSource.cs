using System;

namespace Bit.BlazorUI;

/// <summary>
/// Internal random access to original PDF bytes, independent of a read cursor.
/// Offsets are absolute and remain int-sized, as in the existing engine.
/// Reads are synchronous; this contract does not perform asynchronous I/O.
/// </summary>
/// <remarks>
/// Borrowed windows must be disposed after their last use; their storage must not
/// be mutated or recycled while the window is held. The caller of a memory source
/// must keep its input bytes stable during parsing. Bounded sources account for
/// held/pinned views, in-flight buffers and cross-block copies, including storage
/// retained after eviction or disposal until its final lease is released.
/// HTTP support will use a dedicated missing-data exception and asynchronous
/// acquisition outside the engine. Every malformed-content catch-all must let
/// that signal, source I/O failures, disposal and cancellation escape; neither may initiate recovery scanning.
/// The missing-data exception and retry coordinator belong to that later stage.
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
    /// Borrows a scoped read-only window of an exact range. The entire
    /// range must be within the source. The backing storage belongs to the source.
    /// </summary>
    BitPdfByteSourceWindow GetMemory(int offset, int length);

    /// <summary>
    /// Creates an independent cursor over an exact range, or the remainder when
    /// length is null. The window shares the source's storage; the parser attaches
    /// the PDF dictionary after creating it.
    /// </summary>
    BitPdfBaseStream CreateStream(int start = 0, int? length = null);
}
