namespace Bit.Bcrdt;

/// <summary>What handling one document-synchronization message did.</summary>
/// <param name="Handled">Whether the message was a document message at all.</param>
/// <param name="AppliedDelta">Whether it carried a <see cref="BcrdtSyncMessageKind.SyncStep2"/> delta that was applied.</param>
/// <param name="PeerStateVector">
/// The state vector a peer stated in a <see cref="BcrdtSyncMessageKind.SyncStep1Reply"/>, which is what lets
/// a replica decide whether it has caught up with that peer.
/// </param>
public readonly record struct BcrdtSyncExchangeResult(bool Handled, bool AppliedDelta, BcrdtStateVector? PeerStateVector);

/// <summary>
/// The document half of the handshake: what a replica owes in answer to each message.
/// </summary>
/// <remarks>
/// Kept separate from <see cref="BcrdtSyncProvider"/> so that anything speaking the protocol - the
/// provider, a deterministic network simulator, a server-side replica - runs exactly the same
/// decisions rather than a hand-maintained copy of them. See <see cref="BcrdtSyncProtocol"/> for the
/// shape of the exchange.
/// </remarks>
public static class BcrdtSyncExchange
{
    /// <summary>The message that opens a handshake: this replica's state vector.</summary>
    public static byte[] CreateAnnouncement(BcrdtDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        return BcrdtSyncProtocol.EncodeSyncStep1(doc.GetStateVector());
    }

    /// <summary>The deletion digest of a document, as carried in a <see cref="BcrdtSyncMessageKind.SyncStep1Reply"/>.</summary>
    public static ulong ComputeDeletionDigest(BcrdtDocument doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        return BcrdtSyncProtocol.ComputeDeletionDigest(doc.GetDeleteSet());
    }

    /// <summary>
    /// Handles a document message, applying whatever it carries and collecting the replies it calls for.
    /// </summary>
    /// <param name="doc">The local replica.</param>
    /// <param name="message">A decoded message. Kinds other than the four document kinds are ignored.</param>
    /// <param name="origin">The origin to apply remote changes with, so they are not echoed back.</param>
    /// <param name="replies">Receives the messages owed in answer, in the order they should be sent.</param>
    /// <exception cref="BcrdtFormatException">The payload is malformed.</exception>
    /// <remarks>
    /// A reply larger than <see cref="BcrdtSyncProtocol.DefaultMaxMessageSize"/> is sent as several
    /// messages.
    /// </remarks>
    public static BcrdtSyncExchangeResult Handle(
        BcrdtDocument doc,
        BcrdtSyncMessage message,
        object? origin,
        ICollection<byte[]> replies) =>
        Handle(doc, message, origin, replies, BcrdtSyncProtocol.DefaultMaxMessageSize);

    /// <summary>
    /// Handles a document message, applying whatever it carries and collecting the replies it calls for,
    /// none of them larger than <paramref name="maxMessageSize"/> where the content can be divided.
    /// </summary>
    /// <param name="doc">The local replica.</param>
    /// <param name="message">A decoded message. Kinds other than the four document kinds are ignored.</param>
    /// <param name="origin">The origin to apply remote changes with, so they are not echoed back.</param>
    /// <param name="replies">Receives the messages owed in answer, in the order they should be sent.</param>
    /// <param name="maxMessageSize">The most one reply may take, or zero for no limit.</param>
    /// <exception cref="BcrdtFormatException">The payload is malformed.</exception>
    public static BcrdtSyncExchangeResult Handle(
        BcrdtDocument doc,
        BcrdtSyncMessage message,
        object? origin,
        ICollection<byte[]> replies,
        int maxMessageSize)
    {
        ArgumentNullException.ThrowIfNull(doc);
        ArgumentNullException.ThrowIfNull(replies);

        switch (message.Kind)
        {
            case BcrdtSyncMessageKind.SyncStep1:
            {
                // An announcement: state what we have, so that the announcer can answer in turn, and
                // send what it lacks. The reply goes out even when our state vector is behind on
                // nothing, because only the deletion digest can reveal deletions it holds. It goes
                // first because it is small: on a slow link a large delta can take longer than the
                // announcer's handshake timeout, and hearing the reply tells it somebody is here and
                // what to wait for.
                BcrdtStateVector remote = BcrdtStateVector.Decode(message.Payload);
                replies.Add(BcrdtSyncProtocol.EncodeSyncStep1Reply(doc.GetStateVector(), ComputeDeletionDigest(doc)));
                AddDelta(replies, doc.EncodeStateAsUpdate(remote), maxMessageSize);
                return new BcrdtSyncExchangeResult(true, false, null);
            }

            case BcrdtSyncMessageKind.SyncStep1Reply:
            {
                // Answered with a delta only - never another reply - which is what terminates the
                // exchange. Skipped when the replier provably holds everything we hold.
                (BcrdtStateVector remote, ulong remoteDigest) = BcrdtSyncProtocol.DecodeSyncStep1Reply(message.Payload);
                bool replierIsComplete =
                    remoteDigest != 0 &&
                    remote.IsSupersetOf(doc.GetStateVector()) &&
                    remoteDigest == ComputeDeletionDigest(doc);

                if (!replierIsComplete)
                {
                    AddDelta(replies, doc.EncodeStateAsUpdate(remote), maxMessageSize);
                }
                return new BcrdtSyncExchangeResult(true, false, remote);
            }

            case BcrdtSyncMessageKind.SyncStep2:
                doc.ApplyUpdate(message.Payload, origin);
                return new BcrdtSyncExchangeResult(true, true, null);

            case BcrdtSyncMessageKind.Update:
                doc.ApplyUpdate(message.Payload, origin);
                return new BcrdtSyncExchangeResult(true, false, null);

            default:
                return default;
        }
    }

    private static void AddDelta(ICollection<byte[]> replies, byte[] update, int maxMessageSize)
    {
        foreach (byte[] message in BcrdtSyncProtocol.EncodeDelta(BcrdtSyncMessageKind.SyncStep2, update, maxMessageSize))
        {
            replies.Add(message);
        }
    }
}
