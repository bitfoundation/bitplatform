namespace Bit.Butil;

/// <summary>
/// How an <see cref="SourceBufferHandle.Append"/> ended. The distinction matters: one of these
/// outcomes is routine and recoverable, and treating it like the others is what makes a player stall.
/// </summary>
public enum SourceBufferAppendStatus
{
    /// <summary>The segment was accepted and is now part of the buffered ranges.</summary>
    Success,

    /// <summary>
    /// The buffer is full (<c>QuotaExceededError</c>). This is expected during normal playback of a
    /// long stream, and the fix is to <see cref="SourceBufferHandle.Remove"/> the ranges that have
    /// already been played and append the same segment again - not to fail the playback.
    /// </summary>
    QuotaExceeded,

    /// <summary>The bytes were rejected: a segment that doesn't match the buffer's type, or one appended before its initialization segment.</summary>
    Failed,

    /// <summary>An <see cref="SourceBufferHandle.Abort"/> - or a quality switch - cancelled the append while it was running.</summary>
    Aborted,

    /// <summary>The buffer or its media source is gone: removed, disposed, or detached from the element.</summary>
    Closed
}
