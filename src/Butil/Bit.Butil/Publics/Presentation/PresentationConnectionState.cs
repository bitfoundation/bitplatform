namespace Bit.Butil;

/// <summary>
/// Mirrors <see href="https://developer.mozilla.org/en-US/docs/Web/API/PresentationConnection/state">PresentationConnection.state</see>.
/// </summary>
public enum PresentationConnectionState
{
    /// <summary>The receiving page is being opened; messages sent now are rejected.</summary>
    Connecting,

    /// <summary>Open in both directions - messages can be sent and received.</summary>
    Connected,

    /// <summary>
    /// This page let go of the connection, but the presentation is still running on the other screen
    /// and can be picked up again with <see cref="Presentation.Reconnect"/>.
    /// </summary>
    Closed,

    /// <summary>The presentation itself is over. Nothing is running on the other screen any more.</summary>
    Terminated
}
