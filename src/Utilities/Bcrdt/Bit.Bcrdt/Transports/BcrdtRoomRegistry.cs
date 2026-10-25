using System.Collections.Concurrent;

namespace Bit.Bcrdt;

/// <summary>
/// Named rooms of members, created by the first arrival and retired when the last member leaves.
/// </summary>
/// <remarks>
/// Shared by <see cref="BcrdtInMemoryNetwork"/> and the WebSocket relay in <c>Bit.Bcrdt.Server</c>,
/// because the retirement protocol is race-sensitive and the two must not drift apart: a fix that
/// reached only one of them would leave the other able to strand a member in a room nobody else
/// can find.
/// </remarks>
/// <typeparam name="TMember">What occupies a room - a transport, a socket.</typeparam>
internal sealed class BcrdtRoomRegistry<TMember>
    where TMember : notnull
{
    private readonly ConcurrentDictionary<string, Room> _rooms = new(StringComparer.Ordinal);

    /// <summary>How many rooms currently have members.</summary>
    public int Count => _rooms.Count;

    /// <summary>The room with a name, if it currently has members.</summary>
    public bool TryGet(string name, [NotNullWhen(true)] out Room? room) => _rooms.TryGetValue(name, out room);

    /// <summary>
    /// Adds a member to a room, creating the room if this is the first arrival.
    /// </summary>
    /// <remarks>
    /// The retry exists because a room is retired when its last member leaves. Without it, a member
    /// arriving at the same moment could be added to a room that has just been taken out of the
    /// dictionary, and would then sit in a room nobody else could ever find.
    /// </remarks>
    /// <returns>The room the member joined, to pass back to <see cref="Leave(string, Room, TMember)"/>.</returns>
    public Room Join(string name, TMember member)
    {
        while (true)
        {
            Room room = _rooms.GetOrAdd(name, static _ => new Room());

            lock (room.Gate)
            {
                if (room.Abandoned) continue;

                room.Members.TryAdd(member, 0);
                return room;
            }
        }
    }

    /// <summary>Removes a member from the room it joined under a name, if that room still exists.</summary>
    public void Leave(string name, TMember member)
    {
        if (_rooms.TryGetValue(name, out Room? room)) Leave(name, room, member);
    }

    /// <summary>
    /// Removes a member, and the room itself once it is empty.
    /// </summary>
    /// <remarks>
    /// Empty rooms are removed so that a long-lived registry does not accumulate an entry for every
    /// name it has ever seen. The value-matching removal is what keeps that safe: it cannot delete a
    /// replacement room that a new member created in the meantime.
    /// </remarks>
    public void Leave(string name, Room room, TMember member)
    {
        lock (room.Gate)
        {
            room.Members.TryRemove(member, out _);
            if (!room.Members.IsEmpty) return;

            room.Abandoned = true;
        }

        _rooms.TryRemove(new KeyValuePair<string, Room>(name, room));
    }

    /// <summary>The members currently in one room.</summary>
    internal sealed class Room
    {
        /// <summary>The members, as keys. Enumerating it is lock-free and safe alongside joins and leaves.</summary>
        public readonly ConcurrentDictionary<TMember, byte> Members = new();

        /// <summary>Guards the transition to <see cref="Abandoned"/> against a concurrent arrival.</summary>
        public readonly object Gate = new();

        /// <summary>Set once the room has been retired, so a joiner knows to ask for a fresh one.</summary>
        public bool Abandoned;
    }
}
