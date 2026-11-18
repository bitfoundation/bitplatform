namespace Bit.Bcrdt;

/// <summary>
/// An in-process message bus that connects replicas without touching the network.
/// </summary>
/// <remarks>
/// <para>
/// Replicas join a named room and every message one sends is delivered to the others. That is
/// enough to exercise the entire collaboration stack - handshake, updates, presence, reconnection -
/// with no server and no sockets, which makes it the right default for tests and for a demo that
/// needs to show several clients side by side.
/// </para>
/// <para>
/// It is also genuinely useful in production, for a desktop application syncing several windows or
/// a single process holding several views of one document.
/// </para>
/// <para>
/// Delivery is synchronous: a send invokes each receiver's handler on the sender's thread before it
/// returns. The <c>BcrdtSyncProvider</c> marshals what it receives onto its own synchronization context,
/// so replicas owned by different threads stay confined to their own.
/// </para>
/// </remarks>
public sealed class BcrdtInMemoryNetwork
{
    private readonly BcrdtRoomRegistry<BcrdtInMemoryTransport> _rooms = new();

    /// <summary>A shared network, convenient when one process only needs one.</summary>
    public static BcrdtInMemoryNetwork Shared { get; } = new();

    /// <summary>Raised for every message that crosses the network, for tracing and assertions.</summary>
    public event Action<string, ReadOnlyMemory<byte>>? MessageSent;

    /// <summary>Creates a transport that will join a room when connected.</summary>
    public BcrdtInMemoryTransport CreateTransport(string room)
    {
        ArgumentException.ThrowIfNullOrEmpty(room);
        return new BcrdtInMemoryTransport(this, room);
    }

    /// <summary>How many transports are currently connected to a room.</summary>
    public int MemberCount(string room) =>
        _rooms.TryGet(room, out BcrdtRoomRegistry<BcrdtInMemoryTransport>.Room? existing) ? existing.Members.Count : 0;

    /// <summary>
    /// How many rooms currently have members. Empty rooms are retired, so a long-lived network -
    /// <see cref="Shared"/> in particular - does not hold an entry for every room name it has seen.
    /// </summary>
    internal int RoomCount => _rooms.Count;

    /// <summary>Adds a member to a room, creating it if this is the first arrival.</summary>
    internal void Join(string room, BcrdtInMemoryTransport transport) => _rooms.Join(room, transport);

    /// <summary>Removes a member, and the room itself once it is empty.</summary>
    internal void Leave(string room, BcrdtInMemoryTransport transport) => _rooms.Leave(room, transport);

    internal void Broadcast(string room, BcrdtInMemoryTransport sender, ReadOnlyMemory<byte> message)
    {
        if (!_rooms.TryGet(room, out BcrdtRoomRegistry<BcrdtInMemoryTransport>.Room? existing)) return;

        MessageSent?.Invoke(room, message);

        // Copy before handing out: a receiver may hold the buffer beyond the call, and callers are
        // entitled to reuse theirs the moment Send returns.
        byte[] payload = message.ToArray();

        foreach (KeyValuePair<BcrdtInMemoryTransport, byte> member in existing.Members)
        {
            if (ReferenceEquals(member.Key, sender)) continue;
            member.Key.Deliver(payload);
        }
    }
}
