namespace Bit.Bcrdt;

/// <summary>Where a transport currently stands.</summary>
public enum BcrdtTransportState
{
    /// <summary>Not connected, and not trying to be.</summary>
    Disconnected,
    /// <summary>Attempting to establish a connection.</summary>
    Connecting,
    /// <summary>Connected and able to carry messages.</summary>
    Connected,
    /// <summary>Failed in a way the transport itself cannot recover from.</summary>
    Faulted,
}
