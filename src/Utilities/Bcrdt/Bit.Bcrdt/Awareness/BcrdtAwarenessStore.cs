namespace Bit.Bcrdt;

/// <summary>What one client is currently broadcasting about itself.</summary>
/// <param name="ClientId">The client session the entry belongs to.</param>
/// <param name="Clock">That client's awareness counter, used to order its own updates.</param>
/// <param name="State">The published state, or <see langword="null"/> if the client has gone.</param>
/// <param name="LastSeen">When this replica last heard from the client.</param>
public sealed record BcrdtAwarenessEntry(
    ulong ClientId,
    long Clock,
    IReadOnlyDictionary<string, object?>? State,
    DateTimeOffset LastSeen);

/// <summary>Which clients appeared, changed or disappeared.</summary>
/// <param name="Added">Clients seen for the first time.</param>
/// <param name="Updated">Clients that published new state.</param>
/// <param name="Removed">Clients that left or timed out.</param>
/// <param name="Origin">What caused the change: <see langword="null"/> for local, otherwise the source.</param>
public readonly record struct BcrdtAwarenessChangedEventArgs(
    IReadOnlyList<ulong> Added,
    IReadOnlyList<ulong> Updated,
    IReadOnlyList<ulong> Removed,
    object? Origin);

/// <summary>
/// The registry of ephemeral, per-client state: cursors, selections, who is online.
/// </summary>
/// <remarks>
/// <para>
/// Awareness is deliberately <b>not</b> a CRDT and deliberately <b>not</b> part of the document.
/// Cursor positions are worthless five seconds later, so paying CRDT costs for them - tombstones,
/// causal history, permanent storage - would be all cost and no benefit. Worse, mixing them into
/// the document would mean every mouse movement grew the document forever.
/// </para>
/// <para>
/// The rules are simpler as a result. Each client owns exactly one entry and may only write its own,
/// so there is nothing to merge: a higher clock from a client simply replaces its previous entry.
/// Entries expire if the client stops refreshing them, which is what makes a crashed browser tab
/// disappear from the participant list without any server bookkeeping or disconnect handling.
/// </para>
/// <para>
/// The store is safe to use from several threads: every read sees a consistent snapshot and every
/// change is applied atomically. Events are raised after the change, outside the store's lock, on
/// the thread that made it.
/// </para>
/// </remarks>
public sealed class BcrdtAwarenessStore : IDisposable
{
    /// <summary>The highest awareness clock accepted from a peer.</summary>
    private const ulong MaxClock = long.MaxValue / 2;

    /// <summary>How far ahead of our own clock a claim about our own entry may be and still be believed.</summary>
    private const long MaxSelfClockJump = 1L << 32;

    private readonly Dictionary<ulong, BcrdtAwarenessEntry> _entries = [];
    private readonly object _gate = new();
    private readonly TimeSpan _timeout;
    private readonly TimeProvider _time;
    private IReadOnlyDictionary<ulong, BcrdtAwarenessEntry>? _snapshot;
    private long _localClock;
    private bool _disposed;

    /// <summary>Creates a store for one local client session.</summary>
    /// <param name="localClientId">
    /// The local session's identity. Use the same value as the document's client identifier so that
    /// awareness entries can be correlated with the edits they accompany.
    /// </param>
    /// <param name="timeout">
    /// How long an entry survives without a refresh. Should be comfortably more than the heartbeat
    /// interval, or clients will flicker in and out of the participant list on a slow network.
    /// </param>
    /// <param name="timeProvider">The clock, overridable so expiry can be tested deterministically.</param>
    public BcrdtAwarenessStore(ulong localClientId, TimeSpan? timeout = null, TimeProvider? timeProvider = null)
    {
        LocalClientId = localClientId;
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <summary>The local client session's identity.</summary>
    public ulong LocalClientId { get; }

    /// <summary>
    /// Every client currently known, including the local one and clients that recently left.
    /// </summary>
    /// <remarks>
    /// A snapshot: it does not change after it has been returned, so it is safe to enumerate while
    /// updates keep arriving. Clients that left are forgotten once they have been gone for longer
    /// than the timeout.
    /// </remarks>
    public IReadOnlyDictionary<ulong, BcrdtAwarenessEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _snapshot ??= new Dictionary<ulong, BcrdtAwarenessEntry>(_entries);
            }
        }
    }

    /// <summary>The clients currently considered online.</summary>
    public IEnumerable<ulong> OnlineClients => Entries.Where(p => p.Value.State is not null).Select(p => p.Key);

    /// <summary>How many clients are currently online.</summary>
    public int OnlineCount => Entries.Count(p => p.Value.State is not null);

    /// <summary>What this client is currently publishing.</summary>
    public IReadOnlyDictionary<string, object?>? LocalState
    {
        get
        {
            lock (_gate)
            {
                return _entries.TryGetValue(LocalClientId, out BcrdtAwarenessEntry? entry) ? entry.State : null;
            }
        }
    }

    /// <summary>Raised whenever a client appears, leaves, or publishes state different from before.</summary>
    /// <remarks>
    /// A refresh that republishes identical state - a heartbeat - is not a change and does not raise
    /// this, so subscribers that re-render on it are not woken every few seconds per participant.
    /// </remarks>
    public event Action<BcrdtAwarenessChangedEventArgs>? Changed;

    /// <summary>
    /// Raised with an encoded update whenever local state changes, ready to broadcast.
    /// </summary>
    /// <remarks>
    /// Carries only the entries that actually changed, so a cursor move costs a few dozen bytes
    /// regardless of how many people are in the room. Also raised for heartbeats, which republish
    /// unchanged state so peers keep the entry alive.
    /// </remarks>
    public event Action<byte[]>? Updated;

    /// <summary>Replaces this client's published state.</summary>
    /// <param name="state">
    /// The new state, or <see langword="null"/> to go offline. Every value must belong to the
    /// collaborative value model (see <see cref="BcrdtAnyCodec"/>). The store keeps a copy in the form
    /// peers decode, so changing the dictionary afterwards changes nothing until it is set again.
    /// </param>
    /// <exception cref="NotSupportedException">
    /// A value is outside the collaborative value model. Nothing is published in that case.
    /// </exception>
    public void SetLocalState(IReadOnlyDictionary<string, object?>? state) => UpdateLocal(_ => state);

    /// <summary>Updates a single field of this client's published state.</summary>
    /// <remarks>
    /// A value outside the collaborative value model - an application record, say - is projected onto
    /// it first, the same way <see cref="BcrdtAnyCodec.FromObject"/> projects a value stored in a document.
    /// </remarks>
    public void SetLocalField(string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        object? projected = value is null ? null : ToValueModel(value);
        UpdateLocal(current =>
        {
            var next = current is null
                ? new Dictionary<string, object?>(StringComparer.Ordinal)
                : new Dictionary<string, object?>(current, StringComparer.Ordinal);

            if (value is null) next.Remove(key);
            else next[key] = projected;
            return next;
        });
    }

    /// <summary>Reads a field from a client's published state.</summary>
    public object? GetField(ulong clientId, string key) =>
        Entries.TryGetValue(clientId, out BcrdtAwarenessEntry? entry) && entry.State is not null
            ? entry.State.GetValueOrDefault(key)
            : null;

    /// <summary>
    /// Refreshes the local entry and expires clients that have gone quiet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this on a timer at roughly a third of the timeout. Refreshing our own entry tells peers
    /// we are still here; expiring theirs is how a client that crashed, slept or lost its network
    /// leaves the participant list without ever sending a goodbye. Clients that have been gone for a
    /// whole timeout are forgotten entirely, so a long-lived store does not accumulate every session
    /// it has ever seen.
    /// </para>
    /// <para>
    /// The whole pass is one atomic step: a client that refreshes concurrently is either seen as
    /// fresh or expired, never expired after it refreshed, and the local entry is republished as it
    /// stands at that moment, so a concurrent change to it is never overwritten by an older copy.
    /// </para>
    /// </remarks>
    public void Heartbeat()
    {
        List<ulong> expired = [];
        byte[]? removal = null;
        byte[]? refresh = null;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            DateTimeOffset now = _time.GetUtcNow();
            List<ulong>? forgotten = null;

            foreach ((ulong clientId, BcrdtAwarenessEntry entry) in _entries)
            {
                if (clientId == LocalClientId || now - entry.LastSeen <= _timeout) continue;
                if (entry.State is null) (forgotten ??= []).Add(clientId);
                else expired.Add(clientId);
            }

            if (forgotten is not null)
            {
                foreach (ulong clientId in forgotten) _entries.Remove(clientId);
            }

            // Removed at their current clock, which only they may advance: their own next update
            // must still supersede the removal. See RemoveStates.
            foreach (ulong clientId in expired)
            {
                _entries[clientId] = _entries[clientId] with { State = null, LastSeen = now };
            }

            if (expired.Count > 0) removal = EncodeLocked(expired);

            // Republishing our own entry both refreshes peers' timers and repairs any update they
            // lost. Its state was encodable when it was set, so this cannot fail.
            if (_entries.TryGetValue(LocalClientId, out BcrdtAwarenessEntry? local) && local.State is not null)
            {
                BcrdtAwarenessEntry republished = local with { Clock = ++_localClock, LastSeen = now };
                _entries[LocalClientId] = republished;
                refresh = Encode([republished]);
            }

            _snapshot = null;
        }

        if (removal is not null)
        {
            Changed?.Invoke(new BcrdtAwarenessChangedEventArgs([], [], expired, "timeout"));
            Updated?.Invoke(removal);
        }

        if (refresh is not null) Updated?.Invoke(refresh);
    }

    /// <summary>Marks clients as gone.</summary>
    /// <remarks>
    /// The removal is broadcast at the client's current clock. It does not advance a remote client's
    /// clock, because only that client may do so: its own next update must still supersede the
    /// removal, or a client that was merely slow would stay invisible after it recovered.
    /// </remarks>
    public void RemoveStates(IReadOnlyList<ulong> clients, object? origin = null)
    {
        ArgumentNullException.ThrowIfNull(clients);

        List<ulong> removed = [];
        byte[] update;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            DateTimeOffset now = _time.GetUtcNow();

            foreach (ulong clientId in clients)
            {
                if (!_entries.TryGetValue(clientId, out BcrdtAwarenessEntry? entry) || entry.State is null) continue;

                long clock = clientId == LocalClientId ? ++_localClock : entry.Clock;
                _entries[clientId] = entry with { State = null, Clock = clock, LastSeen = now };
                removed.Add(clientId);
            }

            if (removed.Count == 0) return;
            _snapshot = null;
            update = EncodeLocked(removed);
        }

        Changed?.Invoke(new BcrdtAwarenessChangedEventArgs([], [], removed, origin));
        Updated?.Invoke(update);
    }

    /// <summary>Encodes the entries for specific clients.</summary>
    public byte[] EncodeUpdate(IReadOnlyList<ulong> clients)
    {
        ArgumentNullException.ThrowIfNull(clients);
        lock (_gate)
        {
            return EncodeLocked(clients);
        }
    }

    /// <summary>
    /// Encodes every online client and the local entry, for bringing a newly connected peer up to date.
    /// </summary>
    public byte[] EncodeAll()
    {
        lock (_gate)
        {
            List<ulong> clients = [];
            foreach ((ulong clientId, BcrdtAwarenessEntry entry) in _entries)
            {
                if (entry.State is not null || clientId == LocalClientId) clients.Add(clientId);
            }
            return EncodeLocked(clients);
        }
    }

    /// <summary>
    /// Merges an awareness update from a peer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An entry is accepted when it carries a higher clock than what is already held, which makes the
    /// operation idempotent and order-insensitive in the same way document updates are. At an equal
    /// clock the only accepted change is a removal: a peer that timed a client out announces it at
    /// that client's clock, and every replica must agree that the removal wins regardless of the
    /// order messages arrive in. The client's own next update then brings it back.
    /// </para>
    /// <para>
    /// An entry for the local client with a higher clock than ours means another replica holds a
    /// version of our entry we have not superseded - from an earlier session, or from another session
    /// using our identity - so we step our clock past it. A removal of our entry at our current clock
    /// means a peer timed us out while we are in fact still here; we step past it and republish at
    /// once, so peers do not show us as gone until the next heartbeat.
    /// </para>
    /// <para>
    /// The whole update is decoded before anything is applied, so a malformed message changes nothing.
    /// </para>
    /// </remarks>
    public void ApplyUpdate(ReadOnlyMemory<byte> update, object? origin = null)
    {
        var reader = new BcrdtBufferReader(update);
        int count = reader.ValidateCount(reader.ReadVarUInt32(), "awareness entry");

        var decoded = new List<(ulong ClientId, long Clock, IReadOnlyDictionary<string, object?>? State)>(count);
        for (int i = 0; i < count; i++)
        {
            ulong clientId = reader.ReadVarUInt();
            ulong rawClock = reader.ReadVarUInt();

            // Far below the top of the range: a clock is stepped past (+1) when it is ours, and a
            // value at the very top would overflow into one every peer rejects from then on.
            if (rawClock > MaxClock) throw new BcrdtFormatException("Awareness clock out of range.");

            object? value = BcrdtAnyCodec.Read(ref reader);
            decoded.Add((clientId, (long)rawClock, value as IReadOnlyDictionary<string, object?>));
        }

        List<ulong> added = [];
        List<ulong> updated = [];
        List<ulong> removed = [];
        byte[]? republish = null;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            DateTimeOffset now = _time.GetUtcNow();
            bool reclaim = false;

            foreach ((ulong clientId, long clock, IReadOnlyDictionary<string, object?>? state) in decoded)
            {
                _entries.TryGetValue(clientId, out BcrdtAwarenessEntry? existing);

                if (clientId == LocalClientId)
                {
                    // Our own entry echoed back at our current clock carries nothing new.
                    bool removedUs = state is null && existing?.State is not null;
                    if (clock < _localClock || (clock == _localClock && !removedUs)) continue;

                    // A claim further ahead than any session could have published is not an earlier
                    // session of ours; it is forged or corrupt. Stepping past it would hand our clock to
                    // whoever chose the number, so it is ignored, and peers that believed it time it out.
                    if (clock - _localClock > MaxSelfClockJump) continue;

                    _localClock = clock + 1;
                    _entries[clientId] = existing is null
                        ? new BcrdtAwarenessEntry(clientId, _localClock, null, now)
                        : existing with { Clock = _localClock, LastSeen = now };

                    // A peer announced us as gone - typically it timed us out - while we are still
                    // here: say so at once rather than staying invisible until the next heartbeat.
                    // A newer non-empty entry under our identity is only stepped past, not answered,
                    // so two sessions sharing an identity do not ping-pong updates at each other.
                    if (removedUs) reclaim = true;
                    continue;
                }

                if (existing is not null)
                {
                    if (clock < existing.Clock) continue;

                    bool removal = state is null && existing.State is not null;
                    if (clock == existing.Clock && !removal)
                    {
                        // The same entry again. It carries no new information, but it is proof that the
                        // peer is still there - which is the only thing keeping it in the participant
                        // list. Refresh the liveness timer without raising a change.
                        _entries[clientId] = existing with { LastSeen = now };
                        continue;
                    }
                }

                bool wasOnline = existing?.State is not null;
                bool isOnline = state is not null;

                _entries[clientId] = new BcrdtAwarenessEntry(clientId, clock, state, now);

                if (!wasOnline && isOnline) added.Add(clientId);
                else if (wasOnline && isOnline && !BcrdtAnyCodec.DeepEquals(existing!.State, state)) updated.Add(clientId);
                else if (wasOnline && !isOnline) removed.Add(clientId);
            }

            _snapshot = null;
            if (reclaim) republish = EncodeLocked([LocalClientId]);
        }

        if (added.Count > 0 || updated.Count > 0 || removed.Count > 0)
        {
            Changed?.Invoke(new BcrdtAwarenessChangedEventArgs(added, updated, removed, origin));
        }

        if (republish is not null) Updated?.Invoke(republish);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
        }

        // Announce departure before shutting down, not after: peers should learn immediately that
        // this client has gone rather than waiting out the timeout and showing a ghost participant
        // in the meantime. The disposed flag is therefore set only once the goodbye has gone out.
        if (LocalState is not null) SetLocalState(null);

        lock (_gate)
        {
            _disposed = true;
            _entries.Clear();
            _snapshot = null;
        }

        Changed = null;
        Updated = null;
    }

    private void UpdateLocal(Func<IReadOnlyDictionary<string, object?>?, IReadOnlyDictionary<string, object?>?> transform)
    {
        BcrdtAwarenessChangedEventArgs? change = null;
        byte[] update;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            _entries.TryGetValue(LocalClientId, out BcrdtAwarenessEntry? previous);

            // A copy in wire form, never the caller's dictionary: a caller mutating its own instance
            // and setting it again would otherwise compare the state with itself and announce nothing,
            // and the local entry would hold values - an int, say - that no peer ever decodes.
            IReadOnlyDictionary<string, object?>? state = transform(previous?.State) is { } next
                ? (IReadOnlyDictionary<string, object?>)BcrdtAnyCodec.Canonicalize(next)!
                : null;

            long clock = _localClock + 1;
            var entry = new BcrdtAwarenessEntry(LocalClientId, clock, state, _time.GetUtcNow());

            // Encode before committing: a value that cannot be sent must not become local state, or
            // every later publish and heartbeat would fail on it too.
            update = Encode([entry]);

            _localClock = clock;
            _entries[LocalClientId] = entry;
            _snapshot = null;

            bool wasOnline = previous?.State is not null;
            bool isOnline = state is not null;
            ulong[] self = [LocalClientId];

            if (!wasOnline && isOnline) change = new BcrdtAwarenessChangedEventArgs(self, [], [], null);
            else if (wasOnline && !isOnline) change = new BcrdtAwarenessChangedEventArgs([], [], self, null);
            else if (wasOnline && isOnline && !BcrdtAnyCodec.DeepEquals(previous!.State, state))
            {
                change = new BcrdtAwarenessChangedEventArgs([], self, [], null);
            }
        }

        if (change is { } args) Changed?.Invoke(args);
        Updated?.Invoke(update);
    }

    private static object? ToValueModel(object value)
    {
        object? normalized = BcrdtAnyCodec.Normalize(value);
        try
        {
            BcrdtAnyCodec.Encode(normalized);
            return normalized;
        }
        catch (NotSupportedException)
        {
            // Not part of the value model as it stands; flatten it the way documents do.
            return BcrdtAnyCodec.FromObject(value);
        }
    }

    private byte[] EncodeLocked(IReadOnlyList<ulong> clients)
    {
        var entries = new List<BcrdtAwarenessEntry>(clients.Count);
        foreach (ulong clientId in clients)
        {
            entries.Add(_entries.TryGetValue(clientId, out BcrdtAwarenessEntry? entry)
                ? entry
                : new BcrdtAwarenessEntry(clientId, 0, null, default));
        }
        return Encode(entries);
    }

    /// <summary>
    /// Reads who an awareness update speaks for, without applying it: each entry's client, clock, and
    /// whether it publishes state or announces a departure.
    /// </summary>
    /// <remarks>For a server that polices which connection may speak for which client.</remarks>
    /// <exception cref="BcrdtFormatException">The update is malformed.</exception>
    internal static List<(ulong ClientId, long Clock, bool Online)> ReadEntries(ReadOnlyMemory<byte> update)
    {
        var reader = new BcrdtBufferReader(update);
        int count = reader.ValidateCount(reader.ReadVarUInt32(), "awareness entry");

        var entries = new List<(ulong, long, bool)>(count);
        for (int i = 0; i < count; i++)
        {
            ulong clientId = reader.ReadVarUInt();
            ulong rawClock = reader.ReadVarUInt();
            if (rawClock > MaxClock) throw new BcrdtFormatException("Awareness clock out of range.");

            object? state = BcrdtAnyCodec.Read(ref reader);
            entries.Add((clientId, (long)rawClock, state is not null));
        }
        return entries;
    }

    /// <summary>Encodes departures: each client gone, at the clock it was last seen with.</summary>
    /// <remarks>
    /// A removal at a client's own current clock supersedes its entry everywhere, and the client's own
    /// next update - should it in fact still be there - supersedes the removal.
    /// </remarks>
    internal static byte[] EncodeRemovals(IReadOnlyList<(ulong ClientId, long Clock)> clients)
    {
        var entries = new List<BcrdtAwarenessEntry>(clients.Count);
        foreach ((ulong clientId, long clock) in clients) entries.Add(new BcrdtAwarenessEntry(clientId, clock, null, default));
        return Encode(entries);
    }

    private static byte[] Encode(IReadOnlyList<BcrdtAwarenessEntry> entries)
    {
        var writer = new BcrdtBufferWriter(64);
        try
        {
            writer.WriteVarUInt((ulong)entries.Count);
            foreach (BcrdtAwarenessEntry entry in entries)
            {
                writer.WriteVarUInt(entry.ClientId);
                writer.WriteVarUInt((ulong)entry.Clock);
                BcrdtAnyCodec.Write(ref writer, entry.State);
            }
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }
}
