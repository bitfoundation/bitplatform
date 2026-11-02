using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bcrdt.Server;

/// <summary>
/// The server half of the SignalR transport: relays protocol messages between the clients working
/// on a document.
/// </summary>
/// <remarks>
/// <para>
/// On its own the hub is a relay and nothing more. It does not decode messages, hold a replica or
/// know what a state vector is, which is what keeps the server stateless and horizontally scalable -
/// a SignalR backplane can fan messages out across several instances precisely because no instance
/// owns any document state.
/// </para>
/// <para>
/// Register a <see cref="BcrdtDocumentHost"/> (<c>AddBitBcrdtDocumentHost()</c>) and the hub also keeps
/// a persisted replica of every document it carries: one more peer, always present, that answers every
/// handshake with the whole document, writes every change to a store and can validate changes against
/// server-side rules before anyone else receives them. The relaying itself is unchanged. Override
/// <see cref="DocumentHost"/> to use a different host, or none, for a particular hub.
/// </para>
/// <para>
/// One group per document is what makes this scale: a client subscribed to one document never
/// receives another's traffic, and the fan-out cost of an update is proportional to the number of
/// people editing that document rather than the number of people connected to the server. Document
/// groups are namespaced (see <see cref="GroupName"/>), so a subclass that uses SignalR groups for
/// purposes of its own cannot have them joined or written to by naming them as a document. Server
/// code that sends to a document's clients through <c>IHubContext</c> must therefore address
/// <c>Clients.Group(BcrdtHub.GroupName(documentId))</c>, not the bare identifier.
/// </para>
/// <para>
/// Register the hub with <c>AddBitBcrdtHub()</c> - or, for a subclass, with
/// <c>AddBitBcrdtHub&lt;YourHub&gt;()</c>, because SignalR applies hub options per hub type.
/// SignalR's default 32 KB receive limit is smaller than the handshake reply for any sizeable
/// document, and a client that exceeds it is disconnected.
/// </para>
/// <para>
/// <b>Authorization lives here, not in the transport.</b> A client-side transport can only ever ask
/// itself whether it is allowed in, which is worth nothing against a client that has been modified
/// or a request that never came from the application at all. The hub is the first point in the path
/// that the server controls, and it is also the only one holding the caller's authenticated
/// <see cref="HubCallerContext"/>, so it is the only place a refusal is meaningful. Override
/// <see cref="AuthorizeAccessAsync"/> to grant read-write or read-only access per document, or
/// <see cref="AuthorizeAsync"/> for a plain yes or no.
/// </para>
/// <para>
/// A subclass that overrides <see cref="OnDisconnectedAsync"/> must call the base implementation, which
/// releases the connection's place in every hosted document it joined.
/// </para>
/// </remarks>
public class BcrdtHub : Hub
{
    /// <summary>The client method this hub calls to deliver a message. Must match the client transport.</summary>
    public const string ReceiveMethod = "Receive";

    /// <summary>The prefix that keeps document groups apart from any other group the application uses.</summary>
    public const string GroupPrefix = "bit-bcrdt:document:";

    private const string JoinedDocumentsKey = "Bit.Bcrdt.JoinedDocuments";

    /// <summary>The reason sent to a client whose join was refused.</summary>
    /// <remarks>
    /// Deliberately vague by default: a message that explains whether a document exists, or why
    /// exactly access was denied, tells an unauthorized caller more than it tells a legitimate one.
    /// </remarks>
    protected virtual string AccessDeniedReason => "Not authorized to join this document.";

    /// <summary>
    /// The host that keeps a replica of the documents this hub carries, or <see langword="null"/> to
    /// only relay.
    /// </summary>
    /// <remarks>Defaults to the <see cref="BcrdtDocumentHost"/> registered in the container, if any.</remarks>
    protected virtual BcrdtDocumentHost? DocumentHost =>
        Context.GetHttpContext()?.RequestServices.GetService<BcrdtDocumentHost>();

    /// <summary>The SignalR group a document's clients belong to.</summary>
    /// <param name="documentId">The document identifier, as the client named it.</param>
    public static string GroupName(string documentId) => GroupPrefix + documentId;

    /// <summary>
    /// Decides whether the caller may work on a document.
    /// </summary>
    /// <remarks>
    /// Defaults to permitting everything, which is right for a demo and wrong for anything else.
    /// Returning <see langword="false"/> refuses the join, and the base implementation is a
    /// <see cref="ValueTask{TResult}"/> so that the common synchronous decision - a claim already on
    /// the connection's principal - costs no allocation on a path every client takes. Consulted by the
    /// default <see cref="AuthorizeAccessAsync"/>; override that one instead to grant read-only access.
    /// </remarks>
    /// <param name="documentId">The document the caller is asking for.</param>
    /// <returns><see langword="true"/> to permit the join.</returns>
    protected virtual ValueTask<bool> AuthorizeAsync(string documentId) => ValueTask.FromResult(true);

    /// <summary>
    /// Decides what the caller may do with a document: nothing, read it, or read and change it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <see cref="BcrdtDocumentAccess.ReadWrite"/> when <see cref="AuthorizeAsync"/> permits the
    /// join and <see cref="BcrdtDocumentAccess.None"/> when it does not.
    /// </para>
    /// <para>
    /// A read-only connection receives the document and every change to it, and may publish presence -
    /// a reader's cursor is still worth showing - but the hub drops every change it sends, so nothing it
    /// writes reaches anyone else. The client is told on joining, and the built-in components stop
    /// offering edits.
    /// </para>
    /// </remarks>
    /// <param name="documentId">The document the caller is asking for.</param>
    protected virtual async ValueTask<BcrdtDocumentAccess> AuthorizeAccessAsync(string documentId) =>
        await AuthorizeAsync(documentId).ConfigureAwait(false) ? BcrdtDocumentAccess.ReadWrite : BcrdtDocumentAccess.None;

    /// <summary>
    /// Subscribes the calling connection to a document's messages.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A refusal is answered with a <see cref="BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess, string?)"/>
    /// refusal before the connection is aborted, rather than by throwing. A hub exception reaches the
    /// client as a generic invocation failure that its reconnection loop would retry forever; an
    /// <c>Auth</c> message is part of the protocol the client already understands, so it can stop
    /// trying and tell the user why. Aborting afterwards is what makes the refusal binding - without
    /// it a modified client could ignore the message and go on calling
    /// <see cref="Broadcast(string, byte[])"/>.
    /// </para>
    /// <para>
    /// An admitted client is told what it was granted the same way, before the join completes, so it
    /// knows whether it may write before it sends anything.
    /// </para>
    /// </remarks>
    /// <param name="documentId">The document to subscribe to.</param>
    public virtual async Task Join(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        BcrdtDocumentAccess access = await AuthorizeAccessAsync(documentId).ConfigureAwait(false);
        if (access == BcrdtDocumentAccess.None)
        {
            await Clients.Caller
                .SendAsync(ReceiveMethod, BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.None, AccessDeniedReason))
                .ConfigureAwait(false);

            Context.Abort();
            return;
        }

        ConcurrentDictionary<string, JoinedDocument> joined = JoinedDocuments();
        if (joined.ContainsKey(documentId)) return;

        // Loaded before the client is admitted, so the host is ready to answer the handshake the client
        // sends as soon as this returns. A failure here fails the join, and the client retries it.
        BcrdtHostSession? session = null;
        HubHostConnection? connection = null;
        if (DocumentHost is { } host)
        {
            // Every hub type is a relay of its own: SignalR keeps groups per hub.
            connection = new HubHostConnection(Context, Clients.Client(Context.ConnectionId), access, "signalr:" + GetType().FullName);
            session = await host.JoinAsync(documentId, connection, Context.ConnectionAborted).ConfigureAwait(false);
        }

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(documentId)).ConfigureAwait(false);
            if (!joined.TryAdd(documentId, new JoinedDocument(access, session, connection)))
            {
                session?.Dispose();
                return;
            }
        }
        catch
        {
            session?.Dispose();
            throw;
        }

        await Clients.Caller.SendAsync(ReceiveMethod, BcrdtSyncProtocol.EncodeAuth(access, null)).ConfigureAwait(false);
    }

    /// <summary>
    /// Unsubscribes the calling connection from a document's messages.
    /// </summary>
    /// <remarks>
    /// Only needed by a client that stays connected but stops working on a document - closing a
    /// browser tab needs no call, because SignalR discards a connection's group memberships when it
    /// ends, and <see cref="OnDisconnectedAsync"/> releases its hosted documents.
    /// </remarks>
    /// <param name="documentId">The document to unsubscribe from.</param>
    public virtual async Task Leave(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        if (JoinedDocuments().TryRemove(documentId, out JoinedDocument? left)) left.Session?.Dispose();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(documentId)).ConfigureAwait(false);
    }

    /// <summary>
    /// Relays a message to the document's other clients.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sent to the others in the group rather than to everyone, because a peer that received its own
    /// update back would have to recognise and discard it on every keystroke, and the sender already
    /// has the change by definition.
    /// </para>
    /// <para>
    /// The membership check is not redundant with <see cref="Join"/>. A hub method is callable the
    /// moment a connection is established, so a client that never joined - or that was refused -
    /// could otherwise name any document identifier and have the server relay whatever it liked into
    /// a conversation it has no access to. Checking the recorded membership rather than re-running
    /// <see cref="AuthorizeAccessAsync"/> keeps that guarantee without paying for an authorization lookup
    /// on every message.
    /// </para>
    /// <para>
    /// An <c>Auth</c> message is dropped rather than relayed: only the hub may tell a client it has
    /// been refused, and a refusal is final, so one forged by a member of the room would disconnect
    /// everyone else for good. See <see cref="BcrdtSyncProtocol.IsRelayable"/>. A change from a read-only
    /// connection is dropped too (<see cref="BcrdtSyncProtocol.IsWrite"/>), and with a
    /// <see cref="DocumentHost"/> so is anything the host refuses.
    /// </para>
    /// </remarks>
    /// <param name="documentId">The document the message belongs to.</param>
    /// <param name="message">The encoded protocol message, relayed without being interpreted.</param>
    public virtual async Task Broadcast(string documentId, byte[] message)
    {
        if (string.IsNullOrEmpty(documentId) || message is null || message.Length == 0) return;
        if (!BcrdtSyncProtocol.IsRelayable(message)) return;
        if (!JoinedDocuments().TryGetValue(documentId, out JoinedDocument? joined)) return;
        if (joined.Access != BcrdtDocumentAccess.ReadWrite && BcrdtSyncProtocol.IsWrite(message)) return;

        // Refused by the host and about to be aborted: nothing more it sends counts, least of all changes
        // built on the one that was rejected.
        if (joined.Connection is { Refused: true }) return;
        if (joined.Session is { } session && !await session.ReceiveAsync(message).ConfigureAwait(false)) return;

        await Clients.OthersInGroup(GroupName(documentId))
            .SendAsync(ReceiveMethod, message)
            .ConfigureAwait(false);
    }

    /// <inheritdoc/>
    /// <remarks>Releases the connection's place in every hosted document it joined. Call it from an override.</remarks>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue(JoinedDocumentsKey, out object? found) &&
            found is ConcurrentDictionary<string, JoinedDocument> joined)
        {
            foreach (string documentId in joined.Keys)
            {
                if (joined.TryRemove(documentId, out JoinedDocument? left)) left.Session?.Dispose();
            }
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    /// <summary>
    /// The documents this connection has been admitted to, with what it was granted.
    /// </summary>
    /// <remarks>
    /// Kept in <see cref="HubCallerContext.Items"/> rather than in a dictionary of the hub's own,
    /// because SignalR already scopes that bag to the connection and clears it on disconnect - a
    /// static dictionary would need the same lifetime handling and would leak an entry for every
    /// connection that ended unexpectedly. A concurrent dictionary, because an application may raise
    /// <c>MaximumParallelInvocationsPerClient</c> and run a client's invocations side by side.
    /// </remarks>
    private ConcurrentDictionary<string, JoinedDocument> JoinedDocuments()
    {
        object? existing = Context.Items.TryGetValue(JoinedDocumentsKey, out object? found) ? found : null;
        if (existing is ConcurrentDictionary<string, JoinedDocument> joined) return joined;

        lock (Context.Items)
        {
            if (Context.Items.TryGetValue(JoinedDocumentsKey, out found) && found is ConcurrentDictionary<string, JoinedDocument> raced)
            {
                return raced;
            }

            ConcurrentDictionary<string, JoinedDocument> created = new(StringComparer.Ordinal);
            Context.Items[JoinedDocumentsKey] = created;
            return created;
        }
    }

    private sealed record JoinedDocument(BcrdtDocumentAccess Access, BcrdtHostSession? Session, HubHostConnection? Connection);

    /// <summary>A SignalR connection as the document host sees it.</summary>
    /// <remarks>
    /// Holds the client proxy rather than the hub, which SignalR creates per invocation and disposes
    /// after it: the proxy addresses the connection through the hub lifetime manager, so it stays
    /// valid - across a backplane too - for as long as the connection does.
    /// </remarks>
    private sealed class HubHostConnection(HubCallerContext context, IClientProxy client, BcrdtDocumentAccess access, string relay)
        : IBcrdtHostConnection
    {
        private int _refused;

        public string Id => context.ConnectionId;

        public BcrdtDocumentAccess Access => access;

        public string? UserId => context.UserIdentifier;

        public string Relay => relay;

        /// <summary>Whether the host refused the connection; set before the refusal is even sent.</summary>
        public bool Refused => Volatile.Read(ref _refused) != 0;

        public void Send(byte[] message) => _ = SendQuietlyAsync(message);

        public void Refuse(string reason)
        {
            if (Interlocked.Exchange(ref _refused, 1) != 0) return;
            _ = RefuseAsync(reason);
        }

        private async Task RefuseAsync(string reason)
        {
            await SendQuietlyAsync(BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.None, reason)).ConfigureAwait(false);
            context.Abort();
        }

        private async Task SendQuietlyAsync(byte[] message)
        {
            try
            {
                await client.SendAsync(ReceiveMethod, message).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The connection is gone; the protocol repairs a lost message on the next handshake.
            }
        }
    }
}
