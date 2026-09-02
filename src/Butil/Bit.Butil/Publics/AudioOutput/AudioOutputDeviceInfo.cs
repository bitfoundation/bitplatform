namespace Bit.Butil;

/// <summary>
/// One audio output device - a speaker, a headset, an HDMI sink.
/// <br/>
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/MediaDeviceInfo">MediaDeviceInfo</see>
/// </summary>
public class AudioOutputDeviceInfo
{
    /// <summary>
    /// The device's id, stable per origin. This is what <see cref="AudioOutput.SetSinkId"/> takes;
    /// an empty string means the system default.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// The human-readable device name. Empty until the origin has been granted a device permission
    /// once - <see cref="AudioOutput.SelectDevice"/> is the way to get a label without asking for a
    /// microphone.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Shared by every device on the same physical unit, which is how a headset's speaker and microphone are paired up.</summary>
    public string GroupId { get; set; } = string.Empty;
}
