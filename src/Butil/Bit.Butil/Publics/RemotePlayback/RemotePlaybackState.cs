namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/RemotePlayback/state">RemotePlayback.state</see>.
/// </summary>
public enum RemotePlaybackState
{
    /// <summary>The element is playing in the page, on this device.</summary>
    Disconnected,

    /// <summary>A device was picked and the connection is being set up.</summary>
    Connecting,

    /// <summary>
    /// The media is playing on the remote device. The element in the page keeps its current time and
    /// its controls, but decodes nothing.
    /// </summary>
    Connected
}
