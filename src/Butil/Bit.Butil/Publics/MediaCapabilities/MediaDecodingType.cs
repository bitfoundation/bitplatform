namespace Bit.Butil;

/// <summary>
/// How the media being asked about reaches the decoder - the <c>type</c> member of
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaCapabilities/decodingInfo">MediaCapabilities.decodingInfo()</see>.
/// </summary>
public enum MediaDecodingType
{
    /// <summary>A plain media element source - a progressive file.</summary>
    File,

    /// <summary>Buffers appended through Media Source Extensions - the adaptive-streaming case.</summary>
    MediaSource,

    /// <summary>A real-time stream received over WebRTC.</summary>
    WebRtc
}
