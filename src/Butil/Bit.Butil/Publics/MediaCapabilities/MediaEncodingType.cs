namespace Bit.Butil;

/// <summary>
/// What the media being asked about is encoded for - the <c>type</c> member of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilities/encodingInfo">MediaCapabilities.encodingInfo()</see>.
/// </summary>
public enum MediaEncodingType
{
    /// <summary>Recording to a file, as <c>MediaRecorder</c> does.</summary>
    Record,

    /// <summary>Encoding for a real-time stream sent over WebRTC.</summary>
    WebRtc
}
