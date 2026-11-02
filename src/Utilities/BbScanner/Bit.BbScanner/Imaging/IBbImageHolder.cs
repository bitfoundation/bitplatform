namespace Bit.BbScanner;

/// <summary>
/// Something that keeps what it derived from the last image it worked on - a copy, a thresholded form, block
/// statistics, the runs of a scan line - in buffers it reuses for the next one.
/// </summary>
/// <remarks>
/// Reusing the buffers is what keeps a frame from allocating, but it also means the picture lingers in them after the
/// camera that took it is closed. Dropping a buffer does not erase it either: freed memory keeps its contents until it
/// is reused. So the buffers are zeroed when the picture is no longer wanted, and kept for the next one.
/// </remarks>
internal interface IBbImageHolder
{
    /// <summary>Zeroes everything derived from the last image, keeping the buffers.</summary>
    void ForgetImage();
}
