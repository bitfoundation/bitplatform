namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaDeviceInfo">MediaDeviceInfo</see>.
/// </summary>
public class MediaDeviceInfo
{
    /// <summary>The device's id, stable per origin. Empty until the user has granted permission once.</summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>One of <c>"audioinput"</c>, <c>"audiooutput"</c>, <c>"videoinput"</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The human-readable device name. Empty until permission has been granted, since it identifies the user's hardware.</summary>
    public string Label { get; set; } = string.Empty;
    
    /// <summary>Shared by every device on the same physical unit, which is how a headset's microphone and speaker are paired up.</summary>
    public string GroupId { get; set; } = string.Empty;
}
