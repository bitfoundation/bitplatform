using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.Bcrdt.Server;

/// <summary>
/// Decides whether a request may work on a document, before the socket is accepted.
/// </summary>
/// <remarks>
/// Given the <see cref="HttpContext"/> of the upgrade request, so an implementation has the
/// authenticated user, the cookies, the headers and the query string to decide with - everything the
/// connection will ever carry, since a WebSocket presents its credentials once and then only sends
/// frames.
/// </remarks>
/// <param name="context">The upgrade request.</param>
/// <param name="documentId">The document the request is asking for.</param>
/// <returns><see langword="true"/> to accept the socket.</returns>
public delegate ValueTask<bool> BcrdtWebSocketAuthorization(HttpContext context, string documentId);

/// <summary>
/// Decides what a request may do with a document - nothing, read it, or read and change it - before the
/// socket is accepted.
/// </summary>
/// <param name="context">The upgrade request.</param>
/// <param name="documentId">The document the request is asking for.</param>
/// <returns>The access to grant; <see cref="BcrdtDocumentAccess.None"/> refuses the request.</returns>
public delegate ValueTask<BcrdtDocumentAccess> BcrdtWebSocketAccessAuthorization(HttpContext context, string documentId);

/// <summary>Settings for an endpoint mapped with <see cref="BcrdtWebSocketEndpoint.MapBitBcrdtWebSocket"/>.</summary>
public sealed class BcrdtWebSocketEndpointOptions
{
    /// <summary>
    /// Decides what each request may do with a document. Omitted means read-write for every request the
    /// <c>authorize</c> callback admits.
    /// </summary>
    /// <remarks>
    /// Consulted after the <c>authorize</c> callback, when both are given. A read-only connection receives
    /// the document and every change and may publish presence, but every change it sends is dropped. The
    /// client is told what it was granted as soon as its socket opens.
    /// </remarks>
    public BcrdtWebSocketAccessAuthorization? AuthorizeAccess { get; init; }

    /// <summary>
    /// A host that keeps a persisted, always-present replica of every document this endpoint carries,
    /// or <see langword="null"/> to only relay. See <see cref="BcrdtDocumentHost"/>.
    /// </summary>
    /// <remarks>
    /// A host is keyed by document identifier alone, so two endpoints given the same host - this one and a
    /// <see cref="BcrdtHub"/>, say - share its documents, and the host passes changes between their
    /// clients. Give each its own host when their documents must stay apart.
    /// </remarks>
    public BcrdtDocumentHost? DocumentHost { get; init; }

    /// <summary>The default largest message a client may send: 16 MB.</summary>
    public const int DefaultMaxMessageSize = BcrdtWebSocketFraming.DefaultMaxMessageSize;

    /// <summary>
    /// The largest message a client may send, in bytes. A larger one closes that client's socket with
    /// <see cref="WebSocketCloseStatus.MessageTooBig"/> rather than being buffered without bound.
    /// </summary>
    public int MaxMessageSize { get; init; } = DefaultMaxMessageSize;

    /// <summary>
    /// How many messages may be queued for one client before it is disconnected.
    /// </summary>
    /// <remarks>
    /// Generous enough to absorb an ordinary latency spike, and finite because an unbounded queue in
    /// front of a client that never drains simply relocates the problem into the server's memory.
    /// </remarks>
    public int OutboundQueueCapacity { get; init; } = 256;

    /// <summary>
    /// How many bytes may be queued for one client before it is disconnected: 64 MB by default.
    /// </summary>
    /// <remarks>
    /// The message count alone does not bound memory - 256 messages of up to
    /// <see cref="MaxMessageSize"/> each could pin gigabytes behind one client that has stopped
    /// reading. Must be at least <see cref="MaxMessageSize"/>, or a single large message would always
    /// disconnect its recipients.
    /// </remarks>
    public long OutboundQueueByteLimit { get; init; } = 64L * 1024 * 1024;

    /// <summary>
    /// How long, once the host begins shutting down, clients are given to answer the close frame
    /// before their sockets are aborted.
    /// </summary>
    public TimeSpan ShutdownGracePeriod { get; init; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// The server half of the WebSocket transport: relays protocol messages between the clients working
/// on a document.
/// </summary>
/// <remarks>
/// <para>
/// Like the SignalR hub, this is a relay and nothing more - it does not decode a message, hold a
/// replica or know what a state vector is. Rooms are process-local, which is the one thing to know
/// before deploying it behind a load balancer: two clients on the same document must reach the same
/// instance, so either use sticky sessions or put a broker between the instances. The SignalR
/// transport, whose backplane solves that problem already, is the easier answer at that scale.
/// </para>
/// <para>
/// Rooms are also scoped to the mapping: two endpoints mapped with different authorization never
/// share a room, even for the same document identifier, so a client cannot reach a protected
/// endpoint's conversation by connecting through an open one.
/// </para>
/// <para>
/// Every client gets its own outbound queue and its own pump. That is the whole reason this type has
/// any structure at all: relaying inline would mean awaiting a send to each member in turn, so one
/// client on a bad connection - or one that has stopped reading without closing, which a dead
/// laptop lid looks exactly like - would hold up every other client in the document behind its
/// socket buffer.
/// </para>
/// </remarks>
public static class BcrdtWebSocketEndpoint
{
    /// <summary>The route value or query parameter the document identifier is read from.</summary>
    public const string DocumentIdKey = BcrdtWebSocketTransportFactory.DocumentIdQueryKey;

    /// <summary>
    /// Maps an endpoint that accepts collaboration WebSockets and relays between them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The document identifier is taken from the route value named <see cref="DocumentIdKey"/> if the
    /// pattern declares one, and from the query string otherwise - so both
    /// <c>MapBitBcrdtWebSocket("/collab/{documentId}")</c> and
    /// <c>MapBitBcrdtWebSocket("/collab")</c> with <c>?documentId=…</c> work.
    /// </para>
    /// <para>
    /// Requires <c>UseWebSockets()</c> in the pipeline. Without it every request arrives with
    /// <see cref="WebSocketManager.IsWebSocketRequest"/> false and this endpoint answers a flat 400,
    /// which is a confusing way to discover a missing line of startup code - hence the explicit
    /// mention here.
    /// </para>
    /// </remarks>
    /// <param name="endpoints">The route builder to map onto.</param>
    /// <param name="pattern">The route pattern, optionally containing a <c>{documentId}</c> segment.</param>
    /// <param name="authorize">
    /// Consulted before the socket is accepted. Omitted means every request is accepted, which is
    /// right for a demo and wrong for anything reachable from outside. A refusal is answered with 403,
    /// which the client transport reports as a final refusal rather than a failure to retry.
    /// </param>
    /// <param name="options">Message size, queue and shutdown settings; defaults when omitted.</param>
    /// <returns>The mapped endpoint, for further conventions such as authorization policies.</returns>
    public static IEndpointConventionBuilder MapBitBcrdtWebSocket(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        BcrdtWebSocketAuthorization? authorize = null,
        BcrdtWebSocketEndpointOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrEmpty(pattern);

        options ??= new BcrdtWebSocketEndpointOptions();
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxMessageSize, 2, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.OutboundQueueCapacity, 1, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.OutboundQueueByteLimit, options.MaxMessageSize, nameof(options));

        // One relay - and so one set of rooms - per mapping.
        var relay = new Relay(authorize, options);
        return endpoints.Map(pattern, (HttpContext context) => relay.HandleAsync(context));
    }

    private sealed class Relay(BcrdtWebSocketAuthorization? authorize, BcrdtWebSocketEndpointOptions options)
    {
        /// <summary>How long a close frame may take before the socket is abandoned.</summary>
        private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(2);

        private readonly BcrdtRoomRegistry<Member> _rooms = new();

        // This mapping, as a document host tells relays apart: its rooms are its own.
        private readonly string _relayName = "websocket:" + Guid.NewGuid().ToString("N");

        public async Task HandleAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            string? documentId = ResolveDocumentId(context);
            if (string.IsNullOrEmpty(documentId))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // Refused before the upgrade, so the client is told with an HTTP status it can read rather
            // than by a socket that opens and closes again for no stated reason.
            if (authorize is not null && !await authorize(context, documentId).ConfigureAwait(false))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            BcrdtDocumentAccess access = options.AuthorizeAccess is { } authorizeAccess
                ? await authorizeAccess(context, documentId).ConfigureAwait(false)
                : BcrdtDocumentAccess.ReadWrite;

            if (access == BcrdtDocumentAccess.None)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            // The hosted replica is loaded before the upgrade, so it is ready for the handshake the
            // client sends as soon as its socket opens - and a store that cannot load the document is
            // reported as a status the client retries, not as a socket that opens onto nothing.
            var connection = new SocketHostConnection(context.Connection.Id, access, UserIdOf(context), _relayName);
            BcrdtHostSession? session = null;
            if (options.DocumentHost is { } host)
            {
                try
                {
                    session = await host.JoinAsync(documentId, connection, context.RequestAborted).ConfigureAwait(false);
                }
                catch (Exception) when (!context.RequestAborted.IsCancellationRequested)
                {
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    return;
                }
            }

            try
            {
                using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                await RelayAsync(documentId, socket, context, connection, session).ConfigureAwait(false);
            }
            finally
            {
                session?.Dispose();
            }
        }

        private static string? ResolveDocumentId(HttpContext context)
        {
            if (context.Request.RouteValues.TryGetValue(DocumentIdKey, out object? routeValue) &&
                routeValue?.ToString() is { Length: > 0 } fromRoute)
            {
                return fromRoute;
            }

            return context.Request.Query[DocumentIdKey].ToString() is { Length: > 0 } fromQuery ? fromQuery : null;
        }

        /// <summary>The signed-in user, the way SignalR identifies one by default, or null for an anonymous request.</summary>
        private static string? UserIdOf(HttpContext context) =>
            context.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.User.Identity.Name
                : null;

        /// <summary>
        /// Serves one client for the life of its socket: pumping its queue while reading its messages
        /// into the room.
        /// </summary>
        private async Task RelayAsync(
            string documentId,
            WebSocket socket,
            HttpContext context,
            SocketHostConnection connection,
            BcrdtHostSession? session)
        {
            Member member = new(socket, options.OutboundQueueCapacity, options.OutboundQueueByteLimit);
            connection.Attach(member);

            // The grant goes out before anything else, so the client knows whether it may write before
            // it hears - and answers - anybody.
            member.TryEnqueue(BcrdtSyncProtocol.EncodeAuth(connection.Access, null));

            BcrdtRoomRegistry<Member>.Room room = _rooms.Join(documentId, member);

            using CancellationTokenSource shutdown = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            Task pump = member.PumpAsync(shutdown.Token);

            // A graceful host shutdown does not signal RequestAborted until its timeout has run out, so
            // without this every open socket would hold the host up for the whole timeout and then be
            // aborted without a close frame.
            Task? closing = null;
            IHostApplicationLifetime? lifetime = context.RequestServices.GetService<IHostApplicationLifetime>();
            using CancellationTokenRegistration stopping = lifetime?.ApplicationStopping.Register(
                () => Volatile.Write(ref closing, CloseForShutdownAsync(member, pump, socket, shutdown))) ?? default;

            try
            {
                byte[] buffer = new byte[BcrdtWebSocketFraming.DefaultBufferSize];
                using MemoryStream accumulator = new(BcrdtWebSocketFraming.DefaultBufferSize);

                while (socket.State is WebSocketState.Open && !shutdown.IsCancellationRequested && !connection.Refused)
                {
                    byte[]? message = await BcrdtWebSocketFraming
                        .ReceiveMessageAsync(socket, buffer, accumulator, options.MaxMessageSize, shutdown.Token)
                        .ConfigureAwait(false);

                    if (message is null) break;
                    if (message.Length == 0) continue;

                    // Only the server may refuse a client, and this endpoint does so with a 403 before
                    // the upgrade; an Auth frame from a client is a forgery that would disconnect the
                    // whole room for good.
                    if (!BcrdtSyncProtocol.IsRelayable(message)) continue;

                    // What a reader writes reaches nobody.
                    if (connection.Access != BcrdtDocumentAccess.ReadWrite && BcrdtSyncProtocol.IsWrite(message)) continue;

                    // The host sees every message before the room does, so what it refuses - a
                    // malformed message, a change its validator rejected - is never passed on.
                    if (session is not null && !await session.ReceiveAsync(message).ConfigureAwait(false)) continue;

                    Broadcast(room, member, message);
                }
            }
            catch (OperationCanceledException)
            {
                // The client went away, was evicted for falling behind, or the host is shutting down.
            }
            catch (WebSocketException)
            {
                // An abrupt disconnection is the ordinary way a browser tab closes, not an error worth
                // reporting; an oversized message lands here too. The room is tidied below either way.
            }
            finally
            {
                // Order matters: leaving the room first means no further messages are queued for a
                // socket that is about to go, so the pump can drain what it has and stop. An empty
                // room is retired, so a long-lived server does not keep one per document it has seen.
                _rooms.Leave(documentId, room, member);
                member.Complete();

                try
                {
                    await pump.ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // The pump's own failure is why this member is leaving.
                }

                if (Volatile.Read(ref closing) is { } shuttingDown)
                {
                    try
                    {
                        await shuttingDown.ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Best effort, like everything said on a closing socket.
                    }
                }

                if (connection.Refused) await CloseRefusedAsync(socket).ConfigureAwait(false);
                await BcrdtWebSocketFraming.AcknowledgeCloseAsync(socket).ConfigureAwait(false);
            }
        }

        /// <summary>Closes a socket the host refused, once the refusal itself has been sent.</summary>
        private static async Task CloseRefusedAsync(WebSocket socket)
        {
            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    using CancellationTokenSource timeout = new(CloseTimeout);
                    await socket
                        .CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "Refused", timeout.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // The socket is going either way.
            }
        }

        private async Task CloseForShutdownAsync(Member member, Task pump, WebSocket socket, CancellationTokenSource shutdown)
        {
            // Stop queueing, let what is already queued go out, then say goodbye properly so the client
            // reconnects at once instead of waiting to notice a dead socket.
            member.Complete();

            try
            {
                await pump.WaitAsync(CloseTimeout).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // A pump stuck behind a client that is not reading is not allowed to hold the host up.
            }

            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    using CancellationTokenSource timeout = new(CloseTimeout);
                    await socket
                        .CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "Server shutting down", timeout.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                // The socket is going either way.
            }

            try
            {
                // A client that never answers the close frame does not get to hold the host up either.
                shutdown.CancelAfter(options.ShutdownGracePeriod);
            }
            catch (ObjectDisposedException)
            {
                // The relay already finished.
            }
        }

        /// <summary>
        /// Hands a message to every other member's queue, without waiting for any of them.
        /// </summary>
        /// <remarks>
        /// A member whose queue is full is disconnected rather than having the message quietly
        /// dropped. The protocol tolerates loss, but a client that is silently missing updates stays
        /// wrong until something else prompts a resync, whereas one that is disconnected reconnects and
        /// repairs an arbitrary divergence in a single handshake. Disconnecting is the self-healing
        /// choice - which is why it must actually disconnect, not merely stop sending.
        /// </remarks>
        private static void Broadcast(BcrdtRoomRegistry<Member>.Room room, Member sender, byte[] message)
        {
            foreach (KeyValuePair<Member, byte> entry in room.Members)
            {
                Member member = entry.Key;
                if (ReferenceEquals(member, sender)) continue;

                // Safe to share one array between members: nothing downstream writes to it.
                if (!member.TryEnqueue(message) && !member.IsCompleted) member.Evict();
            }
        }
    }

    /// <summary>A socket as the document host sees it.</summary>
    /// <remarks>
    /// Created before the socket is accepted, so the host can be joined - and the document loaded -
    /// first; attached to the socket's member once there is one. Anything sent before that is dropped,
    /// which the client's handshake repairs.
    /// </remarks>
    private sealed class SocketHostConnection(string id, BcrdtDocumentAccess access, string? userId, string relay) : IBcrdtHostConnection
    {
        private Member? _member;
        private int _refused;

        public string Id => id;

        public BcrdtDocumentAccess Access => access;

        public string? UserId => userId;

        public string Relay => relay;

        /// <summary>Whether the host refused this connection, so the relay stops reading from it.</summary>
        public bool Refused => Volatile.Read(ref _refused) != 0;

        public void Attach(Member member) => Volatile.Write(ref _member, member);

        public void Send(byte[] message)
        {
            Member? member = Volatile.Read(ref _member);
            if (member is not null && !member.TryEnqueue(message) && !member.IsCompleted) member.Evict();
        }

        /// <summary>Queues the refusal as the last thing this client hears; the relay then closes it.</summary>
        public void Refuse(string reason)
        {
            if (Interlocked.Exchange(ref _refused, 1) != 0) return;
            if (Volatile.Read(ref _member) is not { } member) return;

            member.TryEnqueue(BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.None, reason));
            member.Complete();
        }
    }

    /// <summary>
    /// One connected client, with the queue that decouples it from the rest of the room.
    /// </summary>
    private sealed class Member
    {
        private readonly WebSocket _socket;
        private readonly Channel<byte[]> _outbound;
        private readonly long _byteLimit;
        private long _queuedBytes;
        private int _completed;

        public Member(WebSocket socket, int capacity, long byteLimit)
        {
            _socket = socket;
            _byteLimit = byteLimit;

            // Single reader because the pump is the only consumer, which is also what satisfies the
            // WebSocket rule that one socket may have only one send in flight.
            _outbound = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                FullMode = BoundedChannelFullMode.Wait,
            });
        }

        /// <summary>Whether no more messages will be queued for this member.</summary>
        public bool IsCompleted => Volatile.Read(ref _completed) != 0;

        /// <summary>
        /// Queues a message, reporting <see langword="false"/> when the client is too far behind - by
        /// message count or by bytes.
        /// </summary>
        public bool TryEnqueue(byte[] message)
        {
            if (Interlocked.Add(ref _queuedBytes, message.Length) > _byteLimit)
            {
                Interlocked.Add(ref _queuedBytes, -message.Length);
                return false;
            }

            if (_outbound.Writer.TryWrite(message)) return true;

            Interlocked.Add(ref _queuedBytes, -message.Length);
            return false;
        }

        /// <summary>Signals that no more messages will be queued, letting the pump finish and return.</summary>
        public void Complete()
        {
            Volatile.Write(ref _completed, 1);
            _outbound.Writer.TryComplete();
        }

        /// <summary>
        /// Disconnects a member that has fallen too far behind.
        /// </summary>
        /// <remarks>
        /// An abort rather than a close frame: a close would queue behind the very sends this member is
        /// failing to drain. Aborting fails its pending receive, so its relay leaves the room, and the
        /// client sees a drop it will reconnect from.
        /// </remarks>
        public void Evict()
        {
            if (Interlocked.Exchange(ref _completed, 1) != 0) return;

            _outbound.Writer.TryComplete();
            _socket.Abort();
        }

        /// <summary>Sends queued messages, one at a time, until the queue is completed or the socket dies.</summary>
        public async Task PumpAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (byte[] message in _outbound.Reader
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    Interlocked.Add(ref _queuedBytes, -message.Length);
                    if (_socket.State is not WebSocketState.Open) return;

                    await _socket
                        .SendAsync(message, WebSocketMessageType.Binary, endOfMessage: true, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // The client left, or the host is shutting down.
            }
            catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException or IOException)
            {
                // This client's socket is gone. Its receive loop reaches the same conclusion and
                // removes it from the room; the other members are unaffected, which is the point of
                // giving each one its own pump.
            }
        }
    }
}
