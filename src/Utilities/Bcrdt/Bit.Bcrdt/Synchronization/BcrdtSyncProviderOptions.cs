namespace Bit.Bcrdt;

/// <summary>Settings for a <see cref="BcrdtSyncProvider"/>.</summary>
public sealed class BcrdtSyncProviderOptions
{
    /// <summary>
    /// How often to refresh this replica's presence and expire participants that have gone quiet.
    /// </summary>
    /// <remarks>
    /// This is a presence heartbeat, not a connection probe: detecting a dead connection is the
    /// transport's job (SignalR's server timeout, the WebSocket transport's keep-alive). Probing at
    /// this layer would be broadcast through any relay to every peer in the room, and every peer's
    /// answer broadcast back again - traffic that grows with the square of the room's size.
    /// </remarks>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// How often to re-run the handshake even while apparently connected.
    /// </summary>
    /// <remarks>
    /// Off by default, because the protocol repairs itself on reconnect. Worth enabling over a
    /// transport that can silently drop a message without dropping the connection, where a lost
    /// update would otherwise go unnoticed until the next disconnection. Runs on its own schedule,
    /// whether or not <see cref="HeartbeatInterval"/> is enabled and whichever of the two is shorter.
    /// </remarks>
    public TimeSpan ResyncInterval { get; init; } = TimeSpan.Zero;

    /// <summary>
    /// How long to wait for any peer to answer the opening handshake before concluding that none is
    /// present and treating this replica as synced.
    /// </summary>
    /// <remarks>
    /// A replica that is first into a room behind a relay - the SignalR hub and the WebSocket endpoint
    /// both relay without holding state - has nobody to answer it, and without this would stay in
    /// <see cref="BcrdtConnectionStatus.Handshaking"/> until someone else arrived. If a peer does answer
    /// but its state has not been fully received by then, the handshake is repeated instead.
    /// <see cref="TimeSpan.Zero"/> disables the fallback.
    /// </remarks>
    public TimeSpan HandshakeTimeout { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>How reconnection is paced.</summary>
    public BcrdtReconnectPolicy Reconnect { get; init; } = new();

    /// <summary>Whether presence is exchanged over the same connection, in both directions.</summary>
    public bool SyncAwareness { get; init; } = true;

    /// <summary>
    /// The largest message the provider sends, in bytes; a larger change or handshake answer goes out as
    /// several. Zero sends every message whole.
    /// </summary>
    /// <remarks>
    /// Relays limit what a client may send - a message over the limit gets the client disconnected - and
    /// a document's full state, or one large paste, can exceed any limit. Keep this below the relay's.
    /// Defaults to <see cref="BcrdtSyncProtocol.DefaultMaxMessageSize"/>.
    /// </remarks>
    public int MaxMessageSize { get; init; } = BcrdtSyncProtocol.DefaultMaxMessageSize;

    private Func<BcrdtUpdateEventArgs, bool>? _shouldForward;

    /// <summary>
    /// Which of the document's changes the provider sends on to its peers. <see langword="null"/> - the default -
    /// sends every change but the ones this provider received itself, which are never sent back whatever this says.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Worth setting when one document has two providers, each over its own transport to its own peers - a
    /// server, and the other tabs of the same browser. A change one of them received is a change the other's peers
    /// may well have received too: the other tabs are in the server's room as well. Sent on, every tab would hand
    /// the server back each change another tab made, multiplying its traffic by the number of tabs. Excluding the
    /// other provider (<c>args =&gt; args.Origin != otherProvider</c>) leaves each provider sending this replica's
    /// own changes; what it misses meanwhile, the next handshake on either side brings. The collaboration service
    /// does exactly that when cross-tab sync is on: see <see cref="BcrdtCollaborationBuilder.UseCrossTabSync"/>.
    /// </para>
    /// <para>
    /// It runs on the document's thread for every change, so keep it cheap; it never affects the handshake, which
    /// always reconciles everything.
    /// </para>
    /// </remarks>
    public Func<BcrdtUpdateEventArgs, bool>? ShouldForward { get => _shouldForward; init => _shouldForward = value; }

    /// <summary>A copy of these settings that also leaves out the changes <paramref name="filter"/> refuses.</summary>
    /// <remarks>
    /// A member-wise copy, so a setting added later is carried over without being listed here.
    /// </remarks>
    internal BcrdtSyncProviderOptions AndForwardOnly(Func<BcrdtUpdateEventArgs, bool> filter)
    {
        var copy = (BcrdtSyncProviderOptions)MemberwiseClone();
        Func<BcrdtUpdateEventArgs, bool>? own = _shouldForward;
        copy._shouldForward = own is null ? filter : args => own(args) && filter(args);
        return copy;
    }
}
