namespace Bit.Butil;

/// <summary>Whether a WebTransport session is still usable.</summary>
public enum WebTransportState
{
    /// <summary>The session is established and can carry streams and datagrams.</summary>
    Open,

    /// <summary>
    /// The session is over - closed by either side, or lost. Also what a disposed handle, and one
    /// for a session the browser no longer knows about, reports.
    /// </summary>
    Closed
}
