namespace Bit.Butil;

/// <summary>
/// The <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaRecorder/MediaRecorder#options">MediaRecorder constructor options</see>.
/// </summary>
/// <remarks>
/// Members left null are not sent, so the browser picks its own default. An unsupported
/// <see cref="MimeType"/> makes <see cref="MediaRecorder.Start"/> return null rather than throw -
/// probe with <see cref="MediaRecorder.GetSupportedTypes"/> first.
/// </remarks>
public class MediaRecorderOptions
{
    /// <summary>The container, optionally with codecs, e.g. <c>"video/webm;codecs=vp9,opus"</c>.</summary>
    public string? MimeType { get; set; }

    /// <summary>Target audio bitrate in bits per second.</summary>
    public long? AudioBitsPerSecond { get; set; }

    /// <summary>Target video bitrate in bits per second.</summary>
    public long? VideoBitsPerSecond { get; set; }
}
