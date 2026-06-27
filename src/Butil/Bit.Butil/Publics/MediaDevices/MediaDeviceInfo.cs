namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaDeviceInfo">MediaDeviceInfo</see>.
/// </summary>
public class MediaDeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>One of <c>"audioinput"</c>, <c>"audiooutput"</c>, <c>"videoinput"</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
}
