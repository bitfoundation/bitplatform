using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace Bit.Bcrdt.Server;

/// <summary>
/// The server half of the SignalR transport: relays protocol messages between the clients working
/// on a document.
/// </summary>
/// <remarks>
/// <para>
/// The hub is deliberately a relay and nothing more. It does not decode messages, hold a replica or
/// know what a state vector is, which is what keeps the server stateless and horizontally scalable -
/// a SignalR backplane can fan messages out across several instances precisely because no instance
/// owns any document state. A server that also wants to be a replica runs a
/// <c>BcrdtSyncProvider</c> of its own over an ordinary <see cref="IBcrdtTransport"/>; the protocol is
/// symmetric, so nothing here needs to change to support that.
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
/// <see cref="AuthorizeAsync"/> to check the caller against the document.
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
    /// the connection's principal - costs no allocation on a path every client takes.
    /// </remarks>
    /// <param name="documentId">The document the caller is asking for.</param>
    /// <returns><see langword="true"/> to permit the join.</returns>
    protected virtual ValueTask<bool> AuthorizeAsync(string documentId) => ValueTask.FromResult(true);

    /// <summary>
    /// Subscribes the calling connection to a document's messages.
    /// </summary>
    /// <remarks>
    /// A refusal is answered with a <see cref="BcrdtSyncProtocol.EncodeAuth(bool, string?)"/> message
    /// before the connection is aborted, rather than by throwing. A hub exception reaches the client
    /// as a generic invocation failure that its reconnection loop would retry forever; an
    /// <c>Auth</c> message is part of the protocol the client already understands, so it can stop
    /// trying and tell the user why. Aborting afterwards is what makes the refusal binding - without
    /// it a modified client could ignore the message and go on calling
    /// <see cref="Broadcast(string, byte[])"/>.
    /// </remarks>
    /// <param name="documentId">The document to subscribe to.</param>
    public virtual async Task Join(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        if (!await AuthorizeAsync(documentId).ConfigureAwait(false))
        {
            await Clients.Caller
                .SendAsync(ReceiveMethod, BcrdtSyncProtocol.EncodeAuth(false, AccessDeniedReason))
                .ConfigureAwait(false);

            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(documentId)).ConfigureAwait(false);
        JoinedDocuments().TryAdd(documentId, 0);
    }

    /// <summary>
    /// Unsubscribes the calling connection from a document's messages.
    /// </summary>
    /// <remarks>
    /// Only needed by a client that stays connected but stops working on a document - closing a
    /// browser tab needs no call, because SignalR discards a connection's group memberships when it
    /// ends.
    /// </remarks>
    /// <param name="documentId">The document to unsubscribe from.</param>
    public virtual async Task Leave(string documentId)
    {
        ArgumentException.ThrowIfNullOrEmpty(documentId);

        JoinedDocuments().TryRemove(documentId, out _);
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
    /// <see cref="AuthorizeAsync"/> keeps that guarantee without paying for an authorization lookup
    /// on every message.
    /// </para>
    /// <para>
    /// An <c>Auth</c> message is dropped rather than relayed: only the hub may tell a client it has
    /// been refused, and a refusal is final, so one forged by a member of the room would disconnect
    /// everyone else for good. See <see cref="BcrdtSyncProtocol.IsRelayable"/>.
    /// </para>
    /// </remarks>
    /// <param name="documentId">The document the message belongs to.</param>
    /// <param name="message">The encoded protocol message, relayed without being interpreted.</param>
    public virtual async Task Broadcast(string documentId, byte[] message)
    {
        if (string.IsNullOrEmpty(documentId) || message is null || message.Length == 0) return;
        if (!BcrdtSyncProtocol.IsRelayable(message)) return;
        if (!JoinedDocuments().ContainsKey(documentId)) return;

        await Clients.OthersInGroup(GroupName(documentId))
            .SendAsync(ReceiveMethod, message)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// The documents this connection has been admitted to.
    /// </summary>
    /// <remarks>
    /// Kept in <see cref="HubCallerContext.Items"/> rather than in a dictionary of the hub's own,
    /// because SignalR already scopes that bag to the connection and clears it on disconnect - a
    /// static dictionary would need the same lifetime handling and would leak an entry for every
    /// connection that ended unexpectedly. A concurrent set, because an application may raise
    /// <c>MaximumParallelInvocationsPerClient</c> and run a client's invocations side by side.
    /// </remarks>
    private ConcurrentDictionary<string, byte> JoinedDocuments()
    {
        object? existing = Context.Items.TryGetValue(JoinedDocumentsKey, out object? found) ? found : null;
        if (existing is ConcurrentDictionary<string, byte> joined) return joined;

        lock (Context.Items)
        {
            if (Context.Items.TryGetValue(JoinedDocumentsKey, out found) && found is ConcurrentDictionary<string, byte> raced)
            {
                return raced;
            }

            ConcurrentDictionary<string, byte> created = new(StringComparer.Ordinal);
            Context.Items[JoinedDocumentsKey] = created;
            return created;
        }
    }
}
