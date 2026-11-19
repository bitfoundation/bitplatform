namespace Bit.Bcrdt;

/// <summary>What handling one document-synchronization message did.</summary>
/// <param name="Handled">Whether the message was a document message at all.</param>
/// <param name="AppliedDelta">Whether it carried a <see cref="BcrdtSyncMessageKind.SyncStep2"/> delta that was applied.</param>
/// <param name="PeerStateVector">
/// The state vector a peer stated in a <see cref="BcrdtSyncMessageKind.SyncStep1Reply"/>, which is what lets
/// a replica decide whether it has caught up with that peer.
/// </param>
public readonly record struct BcrdtSyncExchangeResult(bool Handled, bool AppliedDelta, BcrdtStateVector? PeerStateVector)
{
    /// <summary>
    /// The deletion digest of a peer whose answer was held back, or zero. Set only when the caller asked
    /// for deferral: the peer holds every block this replica does, and only deletions might differ.
    /// </summary>
    internal ulong DeferredDigest { get; init; }
}
