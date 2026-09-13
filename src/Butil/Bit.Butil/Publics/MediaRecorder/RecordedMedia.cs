namespace Bit.Butil;

/// <summary>
/// The result of a finished recording: either the bytes themselves, or a <c>blob:</c> URL that
/// keeps them inside the browser.
/// </summary>
/// <remarks>
/// Exactly one of <see cref="Data"/> and <see cref="ObjectUrl"/> is populated, depending on which
/// stop overload was used. <see cref="Size"/> is filled in either way, so a caller can decide
/// whether a take is worth pulling across the interop boundary at all.
/// </remarks>
public class RecordedMedia
{
    /// <summary>The recorded bytes, or null when the recording was stopped as an object URL.</summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// A <c>blob:</c> URL a <c>&lt;video&gt;</c> or <c>&lt;audio&gt;</c> element can play directly,
    /// or null when the recording was stopped as bytes. Release it with
    /// <see cref="MediaRecordingHandle.RevokeObjectUrl"/> when you're done - it pins the recording
    /// in memory until you do.
    /// </summary>
    public string? ObjectUrl { get; set; }

    /// <summary>The container the browser actually produced, e.g. <c>"video/webm;codecs=vp9,opus"</c>.</summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>The recording's size in bytes.</summary>
    public long Size { get; set; }
}
