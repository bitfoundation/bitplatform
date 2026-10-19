namespace Bit.Bcrdt;

/// <summary>The kinds of message peers exchange.</summary>
/// <remarks>These values are on the wire; never renumber an existing entry.</remarks>
public enum BcrdtSyncMessageKind : byte
{
    /// <summary>"Here is everything I have; send me what I am missing." Opens a handshake.</summary>
    SyncStep1 = 0,
    /// <summary>"Here is what you were missing."</summary>
    SyncStep2 = 1,
    /// <summary>An incremental change.</summary>
    Update = 2,
    /// <summary>Ephemeral presence state.</summary>
    AwarenessUpdate = 3,
    /// <summary>"Tell me who is here."</summary>
    AwarenessQuery = 4,
    /// <summary>An authentication or authorization result.</summary>
    Auth = 5,
    /// <summary>A liveness probe.</summary>
    Ping = 6,
    /// <summary>A liveness response.</summary>
    Pong = 7,
    /// <summary>
    /// "Having answered your <see cref="SyncStep1"/>, here is what I have." Answered with
    /// <see cref="SyncStep2"/> only, which is what makes the exchange terminate.
    /// </summary>
    SyncStep1Reply = 8,
}

/// <summary>A decoded protocol message.</summary>
/// <param name="Kind">What the message is.</param>
/// <param name="Payload">Its body, which is empty for the kinds that carry none.</param>
public readonly record struct BcrdtSyncMessage(BcrdtSyncMessageKind Kind, ReadOnlyMemory<byte> Payload);

/// <summary>
/// The wire protocol peers speak to each other.
/// </summary>
/// <remarks>
/// <para>
/// The protocol is <b>symmetric</b>: there is no client role and no server role, only peers. A
/// browser tab, a server process and a peer-to-peer connection all run the same state machine, and
/// a server is simply a peer that happens to be well connected and always present. That symmetry is
/// what lets the same code serve client-server, peer-to-peer and local multi-tab topologies.
/// </para>
/// <para>
/// The handshake reconciles arbitrary divergence, in both directions, in one round trip each way:
/// </para>
/// <list type="number">
/// <item><description>A peer that connects (or resyncs) announces its state vector with
/// <see cref="BcrdtSyncMessageKind.SyncStep1"/>.</description></item>
/// <item><description>Every peer that hears it answers with <see cref="BcrdtSyncMessageKind.SyncStep2"/> - the
/// blocks the announcer lacks plus the complete delete set - and with a
/// <see cref="BcrdtSyncMessageKind.SyncStep1Reply"/> stating its own state vector and a digest of its
/// deletions.</description></item>
/// <item><description>A peer that hears a <see cref="BcrdtSyncMessageKind.SyncStep1Reply"/> answers with
/// <see cref="BcrdtSyncMessageKind.SyncStep2"/> only, and only when it holds something the replier lacks -
/// blocks beyond the replier's state vector, or deletions whose digest differs.</description></item>
/// </list>
/// <para>
/// The reply is unconditional because a state vector cannot describe deletions: a replica returning
/// with nothing but offline deletions has the same state vector as everyone else, and only the
/// digest in its reply reveals that it holds something they do not. Replies are never answered with
/// another reply, so the exchange always terminates.
/// </para>
/// </remarks>
public static class BcrdtSyncProtocol
{
    /// <summary>The protocol version this build speaks.</summary>
    public const byte Version = 1;

    /// <summary>Encodes "here is what I have; send me the rest".</summary>
    public static byte[] EncodeSyncStep1(BcrdtStateVector stateVector)
    {
        ArgumentNullException.ThrowIfNull(stateVector);
        return Encode(BcrdtSyncMessageKind.SyncStep1, stateVector.Encode());
    }

    /// <summary>Encodes the answer to a <see cref="BcrdtSyncMessageKind.SyncStep1"/>: what this replica has.</summary>
    /// <param name="stateVector">This replica's state vector.</param>
    /// <param name="deletionDigest">
    /// <see cref="ComputeDeletionDigest(BcrdtDeleteSet)"/> of this replica's delete set, or zero if unknown.
    /// </param>
    public static byte[] EncodeSyncStep1Reply(BcrdtStateVector stateVector, ulong deletionDigest)
    {
        ArgumentNullException.ThrowIfNull(stateVector);

        var writer = new BcrdtBufferWriter(stateVector.Count * 6 + 16);
        try
        {
            writer.WriteByte(Version);
            writer.WriteByte((byte)BcrdtSyncMessageKind.SyncStep1Reply);
            stateVector.Write(ref writer);
            writer.WriteVarUInt(deletionDigest);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>Reads a <see cref="BcrdtSyncMessageKind.SyncStep1Reply"/> payload.</summary>
    /// <returns>The replier's state vector, and its deletion digest (zero when absent).</returns>
    /// <exception cref="BcrdtFormatException">The payload is malformed.</exception>
    public static (BcrdtStateVector StateVector, ulong DeletionDigest) DecodeSyncStep1Reply(ReadOnlyMemory<byte> payload)
    {
        var reader = new BcrdtBufferReader(payload);
        BcrdtStateVector vector = BcrdtStateVector.Read(ref reader);
        ulong digest = reader.IsAtEnd ? 0 : reader.ReadVarUInt();
        return (vector, digest);
    }

    /// <summary>
    /// A 64-bit fingerprint of a delete set that is equal for equal sets regardless of the order in
    /// which their clients happen to be stored.
    /// </summary>
    /// <remarks>
    /// Used to decide whether a peer might be missing deletions, which its state vector cannot show.
    /// A false "different" costs one redundant <see cref="BcrdtSyncMessageKind.SyncStep2"/>; a false
    /// "equal" would require a 64-bit collision. Never returns zero, which means "unknown" on the wire.
    /// </remarks>
    public static ulong ComputeDeletionDigest(BcrdtDeleteSet deleteSet)
    {
        ArgumentNullException.ThrowIfNull(deleteSet);

        const ulong offset = 14695981039346656037;
        const ulong prime = 1099511628211;

        ulong digest = 0;
        foreach ((ulong client, List<BcrdtDeleteRange> ranges) in deleteSet.Clients)
        {
            if (ranges.Count == 0) continue;

            // Ranges are sorted and coalesced, so a client's sequence is canonical. Clients are
            // combined with addition, which does not care about the dictionary's order.
            ulong hash = offset;
            hash = (hash ^ client) * prime;
            foreach (BcrdtDeleteRange range in ranges)
            {
                hash = (hash ^ (ulong)range.Clock) * prime;
                hash = (hash ^ (ulong)range.Length) * prime;
            }
            digest += hash;
        }

        return digest == 0 ? 1 : digest;
    }

    /// <summary>Encodes the delta a peer was missing.</summary>
    public static byte[] EncodeSyncStep2(ReadOnlySpan<byte> update) => Encode(BcrdtSyncMessageKind.SyncStep2, update);

    /// <summary>Encodes an incremental change.</summary>
    public static byte[] EncodeUpdate(ReadOnlySpan<byte> update) => Encode(BcrdtSyncMessageKind.Update, update);

    /// <summary>Encodes ephemeral presence state.</summary>
    public static byte[] EncodeAwarenessUpdate(ReadOnlySpan<byte> awareness) =>
        Encode(BcrdtSyncMessageKind.AwarenessUpdate, awareness);

    /// <summary>Encodes a request for the current participant list.</summary>
    public static byte[] EncodeAwarenessQuery() => Encode(BcrdtSyncMessageKind.AwarenessQuery, default);

    /// <summary>Encodes an authentication or authorization result.</summary>
    public static byte[] EncodeAuth(bool permitted, string? reason)
    {
        var writer = new BcrdtBufferWriter(32);
        try
        {
            writer.WriteByte(Version);
            writer.WriteByte((byte)BcrdtSyncMessageKind.Auth);
            writer.WriteByte(permitted ? (byte)1 : (byte)0);
            writer.WriteString(reason ?? string.Empty);
            return writer.ToArray();
        }
        finally
        {
            writer.Dispose();
        }
    }

    /// <summary>Encodes a liveness probe.</summary>
    public static byte[] EncodePing() => Encode(BcrdtSyncMessageKind.Ping, default);

    /// <summary>Encodes a liveness response.</summary>
    public static byte[] EncodePong() => Encode(BcrdtSyncMessageKind.Pong, default);

    /// <summary>Reads an authentication result from a decoded message payload.</summary>
    public static (bool Permitted, string Reason) DecodeAuth(ReadOnlyMemory<byte> payload)
    {
        var reader = new BcrdtBufferReader(payload);
        bool permitted = reader.ReadByte() != 0;
        return (permitted, reader.ReadString());
    }

    /// <summary>
    /// Whether a relay may pass a message it received from one client on to the others.
    /// </summary>
    /// <remarks>
    /// Everything may be relayed except <see cref="BcrdtSyncMessageKind.Auth"/>. That message is the
    /// server's verdict on a connection, and a provider treats a refusal as final: it stops and never
    /// reconnects. A relay that forwarded one from a client would let any member of a room forge a
    /// refusal and permanently disconnect everybody else. Only the kind byte is inspected, so this
    /// costs nothing on the hot path and holds whatever the rest of the message contains.
    /// </remarks>
    /// <param name="message">The encoded message, as a client sent it.</param>
    public static bool IsRelayable(ReadOnlySpan<byte> message) =>
        message.Length < 2 || message[1] != (byte)BcrdtSyncMessageKind.Auth;

    private static byte[] Encode(BcrdtSyncMessageKind kind, ReadOnlySpan<byte> payload)
    {
        byte[] message = new byte[2 + payload.Length];
        message[0] = Version;
        message[1] = (byte)kind;
        payload.CopyTo(message.AsSpan(2));
        return message;
    }

    /// <summary>
    /// Decodes a message received from a peer.
    /// </summary>
    /// <exception cref="BcrdtFormatException">
    /// The message is too short, speaks a version this build does not understand, or names a kind
    /// this build does not know. All three are rejected rather than guessed at, so that a future
    /// peer talking a newer dialect produces a clear failure instead of silent misinterpretation.
    /// </exception>
    public static BcrdtSyncMessage Decode(ReadOnlyMemory<byte> message)
    {
        if (message.Length < 2) throw new BcrdtFormatException("Protocol message is too short.");

        ReadOnlySpan<byte> span = message.Span;
        if (span[0] != Version)
        {
            throw new BcrdtFormatException(
                $"Protocol version {span[0]} is not supported; this build speaks version {Version}.");
        }

        byte kind = span[1];
        if (!Enum.IsDefined((BcrdtSyncMessageKind)kind))
        {
            throw BcrdtFormatException.UnknownTag("protocol message", kind);
        }

        return new BcrdtSyncMessage((BcrdtSyncMessageKind)kind, message[2..]);
    }
}
