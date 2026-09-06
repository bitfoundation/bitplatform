namespace Bit.Butil;

/// <summary>
/// The JSON shape <c>BitButil.streams.fromResponse</c> answers with, before the stream handle is
/// attached to it. Internal because a caller sees <see cref="StreamedResponse"/> instead - this
/// exists only because the handle cannot cross the interop boundary.
/// </summary>
internal class StreamedResponseDto
{
    public bool Ok { get; set; }
    public int Status { get; set; }
    public string? StatusText { get; set; }
    public string? Url { get; set; }
    public long? TotalBytes { get; set; }
    public string? Error { get; set; }
}
