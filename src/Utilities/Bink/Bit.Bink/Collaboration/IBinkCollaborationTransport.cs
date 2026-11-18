namespace Bit.Bink;

/// <summary>
/// Moves messages between participants. Implement this over SignalR, WebSockets, WebRTC or anything else; the
/// session makes no assumptions beyond eventual delivery.
/// </summary>
/// <remarks>
/// The transport is responsible for delivery and for excluding the sender from its own broadcasts. It must
/// deliver each sender's messages in order; a message that arrives after a later one from the same sender is
/// discarded. Duplicates are harmless. Ordering between different senders does not matter.
/// </remarks>
public interface IBinkCollaborationTransport
{
    /// <summary>True when the transport is connected and messages will be delivered.</summary>
    bool IsConnected { get; }

    /// <summary>Sends a message to the other participants.</summary>
    Task SendAsync(BinkCollaborationMessage message, CancellationToken cancellationToken = default);

    /// <summary>Raised for each message received from another participant.</summary>
    event Func<BinkCollaborationMessage, Task>? MessageReceived;

    /// <summary>Raised when the connection state changes. The session flushes its queue when it becomes true.</summary>
    event Action<bool>? ConnectionChanged;
}
