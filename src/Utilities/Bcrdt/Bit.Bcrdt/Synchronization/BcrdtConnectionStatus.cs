namespace Bit.Bcrdt;

/// <summary>The connection lifecycle as an application sees it.</summary>
public enum BcrdtConnectionStatus
{
    /// <summary>Not connected and not trying.</summary>
    Disconnected,
    /// <summary>Establishing a connection.</summary>
    Connecting,
    /// <summary>Connected, exchanging state with the peer.</summary>
    Handshaking,
    /// <summary>Connected and up to date.</summary>
    Synced,
    /// <summary>Connection lost; waiting to retry.</summary>
    Reconnecting,
    /// <summary>Gave up, or was refused.</summary>
    Faulted,
}
