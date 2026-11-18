using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bit.Bcrdt;

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
    /// right for a demo and wrong for anything reachable from outside. A refused socket is accepted only
    /// to be sent an <c>Auth</c> message granting nothing, and closed: the client's provider reports that
    /// as a final refusal rather than a failure to retry - in a browser too, whose WebSocket never shows
    /// the status of a refused upgrade. A request naming a document identifier longer than
    /// <see cref="BcrdtWebSocketEndpointOptions.MaxDocumentIdLength"/> is refused the same way, before this is consulted.
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
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxDocumentIdLength, 1, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.KeepAliveInterval, TimeSpan.Zero, nameof(options));
        if (options.KeepAliveTimeout != Timeout.InfiniteTimeSpan)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(options.KeepAliveTimeout, TimeSpan.Zero, nameof(options));
        }

        // One relay - and so one set of rooms - per mapping.
        var relay = new Relay(authorize, options, options.Connections ?? endpoints.ServiceProvider.GetService<BcrdtRelayConnections>());
        return endpoints.Map(pattern, (HttpContext context) => relay.HandleAsync(context));
    }

    private sealed class Relay(BcrdtWebSocketAuthorization? authorize, BcrdtWebSocketEndpointOptions options, BcrdtRelayConnections? connections)
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

            // Refused after the upgrade, with an Auth message, the way the hub refuses: a browser's WebSocket
            // never shows the page the status of a failed upgrade, so a 403 there reads as a network failure the
            // client would retry for ever.
            if (BcrdtRelayRefusals.IsTooLong(documentId, options.MaxDocumentIdLength))
            {
                await RefuseAsync(context, BcrdtRelayRefusals.DocumentIdTooLong).ConfigureAwait(false);
                return;
            }

            if (authorize is not null && !await authorize(context, documentId).ConfigureAwait(false))
            {
                await RefuseAsync(context, BcrdtRelayRefusals.AccessDenied).ConfigureAwait(false);
                return;
            }

            BcrdtDocumentAccess access = options.AuthorizeAccess is { } authorizeAccess
                ? await authorizeAccess(context, documentId).ConfigureAwait(false)
                : BcrdtDocumentAccess.ReadWrite;

            if (access == BcrdtDocumentAccess.None)
            {
                await RefuseAsync(context, BcrdtRelayRefusals.AccessDenied).ConfigureAwait(false);
                return;
            }

            // The hosted replica is loaded before the upgrade, so it is ready for the handshake the
            // client sends as soon as its socket opens - and a store that cannot load the document is
            // reported as a status the client retries, not as a socket that opens onto nothing.
            var connection = new SocketHostConnection(documentId, context.Connection.Id, access, UserIdOf(context), _relayName);
            BcrdtHostSession? session = null;
            if (options.DocumentHost is { } host)
            {
                connection.Host = host;
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
                using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync(AcceptContext()).ConfigureAwait(false);
                await RelayAsync(documentId, socket, context, connection, session).ConfigureAwait(false);
            }
            finally
            {
                session?.Dispose();
            }
        }

        /// <summary>How the endpoint accepts a socket: with the keep-alive the options ask for.</summary>
        private WebSocketAcceptContext AcceptContext() => new()
        {
            KeepAliveInterval = options.KeepAliveInterval,
            KeepAliveTimeout = options.KeepAliveTimeout,
        };

        /// <summary>
        /// Accepts the socket only to refuse it: an <c>Auth</c> message granting nothing - which the client's provider
        /// treats as final, in a browser as anywhere - and then a close frame.
        /// </summary>
        /// <remarks>
        /// The whole exchange - the refusal, the close frame and the client's answer to it - shares one
        /// <see cref="CloseTimeout"/>, so a refused request holds its socket no longer than that, however the client
        /// behaves; one that answers at once, as every browser does, holds it for a round trip. A client gone before
        /// the upgrade completed is simply let go.
        /// </remarks>
        private static async Task RefuseAsync(HttpContext context, string reason)
        {
            WebSocket socket;
            try
            {
                socket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // The client dropped during the upgrade: nobody is left to tell, and nothing worth reporting happened.
                return;
            }

            using (socket)
            {
                using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                deadline.CancelAfter(CloseTimeout);

                try
                {
                    await socket
                        .SendAsync(BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.None, reason), WebSocketMessageType.Binary, endOfMessage: true, deadline.Token)
                        .ConfigureAwait(false);

                    await socket
                        .CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "Refused", deadline.Token)
                        .ConfigureAwait(false);

                    // Waits for the client's answer to the close frame, so the socket ends cleanly and the refusal
                    // is read before the connection goes.
                    byte[] buffer = new byte[256];
                    while (socket.State is WebSocketState.CloseSent)
                    {
                        WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, deadline.Token).ConfigureAwait(false);
                        if (result.MessageType == WebSocketMessageType.Close) break;
                    }
                }
                catch (Exception)
                {
                    // The client went away, or did not answer in time: the socket is dropped with the request.
                }
            }
        }

        private static string? ResolveDocumentId(HttpContext context)
        {
            if (context.Request.RouteValues.TryGetValue(DocumentIdKey, out object? routeValue) &&
                routeValue?.ToString() is { Length: > 0 } fromRoute)
            {
                return UnescapeSlashes(context, fromRoute);
            }

            return context.Request.Query[DocumentIdKey].ToString() is { Length: > 0 } fromQuery ? fromQuery : null;
        }

        /// <summary>
        /// The identifier a route value stands for, with the slashes it holds put back.
        /// </summary>
        /// <remarks>
        /// The client escapes a <c>/</c> in an identifier as <c>%2F</c>, and the server decodes every escape of a
        /// path but that one, so the route value of <c>team/notes</c> is <c>team%2Fnotes</c> - a different document
        /// from the one the hub, the host and the application's own code call <c>team/notes</c>. Nor can the route
        /// value simply be unescaped: an identifier that holds the text <c>%2F</c> arrives as the same route value.
        /// So the segment the route captured is read again from the path as the client sent it, and unescaped once.
        /// It is found by its place in the route pattern, not by its value: another segment - a tenant, say - can
        /// decode to the same text, and stands for something else.
        /// </remarks>
        private static string UnescapeSlashes(HttpContext context, string routeValue)
        {
            if (!routeValue.Contains("%2F", StringComparison.OrdinalIgnoreCase)) return routeValue;

            string? rawTarget = context.Features.Get<IHttpRequestFeature>()?.RawTarget;
            if (string.IsNullOrEmpty(rawTarget)) return routeValue;

            // Only a segment that is the parameter and nothing else is the identifier as the client escaped it.
            if (context.GetEndpoint() is not RouteEndpoint { RoutePattern: { } pattern }) return routeValue;
            int index = pattern.PathSegments.ToList().FindIndex(segment =>
                segment.Parts is [RoutePatternParameterPart { IsCatchAll: false } parameter] &&
                string.Equals(parameter.Name, DocumentIdKey, StringComparison.OrdinalIgnoreCase));
            if (index < 0) return routeValue;

            int query = rawTarget.IndexOfAny(['?', '#']);
            string[] rawSegments = (query < 0 ? rawTarget : rawTarget[..query]).Split('/');

            // The raw path is the path base and then the path the pattern matched, segment for segment: the server
            // decodes no "/" within a segment. A path it rewrote - a dot segment resolved, say - is left as routed.
            string pathBase = context.Request.PathBase.Value ?? string.Empty;
            int position = (pathBase.Length == 0 ? 0 : pathBase.Split('/').Length - 1) + index + 1;
            string[] pathSegments = (pathBase + context.Request.Path.Value).Split('/');
            if (rawSegments.Length != pathSegments.Length || position >= rawSegments.Length) return routeValue;

            string segment = rawSegments[position];
            return string.Equals(UnescapeAllButSlashes(segment), routeValue, StringComparison.Ordinal)
                ? Uri.UnescapeDataString(segment)
                : routeValue;

            // What the server makes of a raw segment: every escape decoded but %2F.
            static string UnescapeAllButSlashes(string segment)
            {
                var decoded = new System.Text.StringBuilder(segment.Length);
                int start = 0;
                while (true)
                {
                    int slash = segment.IndexOf("%2F", start, StringComparison.OrdinalIgnoreCase);
                    decoded.Append(Uri.UnescapeDataString(slash < 0 ? segment[start..] : segment[start..slash]));
                    if (slash < 0) return decoded.ToString();
                    decoded.Append(segment, slash, 3);
                    start = slash + 3;
                }
            }
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

            // The grant goes out before anything else, so the client knows whether it may write before
            // it hears - and answers - anybody.
            connection.Attach(member);

            BcrdtRoomRegistry<Member>.Room room = _rooms.Join(documentId, member);

            // Listed once it has a socket to be told on, so a revocation from here on reaches the client.
            connections?.Add(connection);

            using CancellationTokenSource shutdown = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            Task pump = member.PumpAsync(shutdown.Token);
            Task refusal = CloseWhenRefusedAsync(connection, pump, socket, shutdown);

            // A graceful host shutdown does not signal RequestAborted until its timeout has run out, so
            // without this every open socket would hold the host up for the whole timeout and then be
            // aborted without a close frame.
            Task? closing = null;
            IHostApplicationLifetime? lifetime = context.RequestServices.GetService<IHostApplicationLifetime>();
            using CancellationTokenRegistration stopping = lifetime?.ApplicationStopping.Register(
                () => Volatile.Write(ref closing, CloseForShutdownAsync(member, pump, socket, shutdown))) ?? default;

            // The socket is read on its own, so that a receive is pending even while the host handles a message
            // or the answer to one is paced out: a pong is only processed by a pending receive, and with none the
            // keep-alive would give up on a client that is merely waiting for a large answer.
            Inbound inbound = new(options.MaxMessageSize);
            Task reading = ReadAsync(socket, inbound, shutdown.Token);

            try
            {
                while (!connection.Refused && await inbound.ReadAsync(shutdown.Token).ConfigureAwait(false) is { } message)
                {
                    // What a reader writes reaches nobody, nor anything from a connection refused while this
                    // waited for it. A revocation waits for a message that got past here to go where it is going.
                    if (connection.TryBeginRelay(message) is not { } relaying) continue;

                    List<byte[]>? answer = null;
                    try
                    {
                        if (session is null)
                        {
                            Broadcast(room, member, message);
                            continue;
                        }

                        // The host sees every message before the room does, so what it refuses - a
                        // malformed message, a change its validator rejected - is never passed on.
                        bool relay;
                        connection.BeginAnswer();
                        try
                        {
                            relay = await session.ReceiveAsync(message).ConfigureAwait(false);
                        }
                        finally
                        {
                            answer = connection.EndAnswer();
                        }

                        // Revoked while the host was handling it: it was applied, but goes no further.
                        if (relay && !connection.Refused) Broadcast(room, member, message);
                    }
                    finally
                    {
                        relaying.End();
                    }

                    // What the host answered is queued before the next message from this client is handled,
                    // so however often it asks, it has at most one answer waiting on the server.
                    await connection.DeliverAnswerAsync(answer, shutdown.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // The client went away, was evicted for falling behind, or the host is shutting down.
            }
            finally
            {
                // Order matters: leaving the room first means no further messages are queued for a
                // socket that is about to go, so the pump can drain what it has and stop. An empty
                // room is retired, so a long-lived server does not keep one per document it has seen.
                _rooms.Leave(documentId, room, member);
                connection.Unlist();
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

                await refusal.ConfigureAwait(false);

                // A refused client was sent a close frame and is given a moment to answer it; otherwise nothing
                // more from this client is handled, so the read stops now.
                if (!reading.IsCompleted && !connection.Refused) shutdown.Cancel();

                await reading.ConfigureAwait(false);
                await BcrdtWebSocketFraming.AcknowledgeCloseAsync(socket).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Reads the client's messages into <paramref name="inbound"/> until the socket closes, fails or the relay
        /// stops, leaving out what is never handled: empty messages, and <c>Auth</c> frames, which only the server
        /// may send - from a client one is a forgery that would disconnect the whole room for good.
        /// </summary>
        /// <remarks>Never throws: however the read ends, <paramref name="inbound"/> is completed.</remarks>
        private async Task ReadAsync(WebSocket socket, Inbound inbound, CancellationToken cancellationToken)
        {
            try
            {
                byte[] buffer = new byte[BcrdtWebSocketFraming.DefaultBufferSize];
                using MemoryStream accumulator = new(BcrdtWebSocketFraming.DefaultBufferSize);

                while (socket.State is WebSocketState.Open)
                {
                    byte[]? message = await BcrdtWebSocketFraming
                        .ReceiveMessageAsync(socket, buffer, accumulator, options.MaxMessageSize, cancellationToken)
                        .ConfigureAwait(false);

                    if (message is null) break;
                    if (message.Length == 0 || !BcrdtSyncProtocol.IsRelayable(message)) continue;

                    await inbound.WriteAsync(message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or WebSocketException or ObjectDisposedException or IOException)
            {
                // An abrupt disconnection is the ordinary way a browser tab closes, not an error worth reporting;
                // an oversized message, an eviction and a shutdown end here too.
            }
            finally
            {
                inbound.Complete();
            }
        }

        /// <summary>
        /// Closes the socket of a refused client once the refusal itself has gone out, rather than leaving that
        /// to the client: one that ignored the refusal could otherwise hold its socket open for as long as it
        /// liked. A client that does not answer the close frame is dropped shortly after.
        /// </summary>
        private static async Task CloseWhenRefusedAsync(SocketHostConnection connection, Task pump, WebSocket socket, CancellationTokenSource shutdown)
        {
            // A refusal completes the member, so the pump sends what is queued - the refusal last - and stops;
            // otherwise it stops when the relay does.
            try
            {
                await pump.ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Its own failure is the relay's to notice.
            }

            if (!connection.Refused) return;
            await CloseRefusedAsync(socket).ConfigureAwait(false);

            try
            {
                shutdown.CancelAfter(CloseTimeout);
            }
            catch (ObjectDisposedException)
            {
                // The relay already finished.
            }
        }

        /// <summary>Closes a refused socket, once the refusal itself has been sent.</summary>
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

    /// <summary>
    /// A socket as the relay, the document host and the list of connections see it - whether or not there
    /// is a host.
    /// </summary>
    /// <remarks>
    /// Created before the socket is accepted, so the host can be joined - and the document loaded -
    /// first; attached to the socket's member once there is one. Anything sent before that is dropped,
    /// which the client's handshake repairs; a refusal is kept, and is all the client hears.
    /// </remarks>
    private sealed class SocketHostConnection(string documentId, string id, BcrdtDocumentAccess access, string? userId, string relay)
        : BcrdtRelayConnection(documentId, id, access, userId, relay)
    {
        private readonly object _gate = new();
        private Member? _member;
        private string? _refusal;

        // While the host handles a message this client sent, what it sends here - the answer to a
        // handshake, above all - is held, and then queued as the client drains rather than all at once.
        private bool _answering;
        private List<byte[]>? _answer;

        /// <summary>Gives the connection its socket's member, and queues the grant - or the refusal - first.</summary>
        public void Attach(Member member)
        {
            lock (_gate)
            {
                Volatile.Write(ref _member, member);
                if (_refusal is null)
                {
                    member.TryEnqueue(BcrdtSyncProtocol.EncodeAuth(Access, null));
                    return;
                }

                member.TryEnqueue(BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.None, _refusal));
                member.Complete();
            }
        }

        public override void Send(byte[] message)
        {
            lock (_gate)
            {
                if (_answering)
                {
                    (_answer ??= []).Add(message);
                    return;
                }
            }

            Member? member = Volatile.Read(ref _member);
            if (member is not null && !member.TryEnqueue(message) && !member.IsCompleted) member.Evict();
        }

        /// <summary>Starts holding what the host sends, while it handles a message this client sent.</summary>
        public void BeginAnswer()
        {
            lock (_gate) _answering = true;
        }

        /// <summary>Stops holding, and returns what was held for <see cref="DeliverAnswerAsync"/>.</summary>
        public List<byte[]>? EndAnswer()
        {
            lock (_gate)
            {
                List<byte[]>? answer = _answer;
                _answering = false;
                _answer = null;
                return answer;
            }
        }

        /// <summary>
        /// Queues what the host answered, waiting for the client to drain each part rather than
        /// overflowing its queue - which would disconnect the very client that asked, and again each
        /// time it came back and asked once more.
        /// </summary>
        public async Task DeliverAnswerAsync(List<byte[]>? answer, CancellationToken cancellationToken)
        {
            if (answer is null || Volatile.Read(ref _member) is not { } member) return;

            foreach (byte[] message in answer)
            {
                if (!await member.EnqueueAnswerAsync(message, cancellationToken).ConfigureAwait(false)) return;
            }
        }

        /// <summary>Queues the refusal as the last thing this client hears; the relay then closes it.</summary>
        protected override void OnRefused(string reason)
        {
            lock (_gate)
            {
                _refusal = reason;
                if (_member is not { } member) return;

                member.TryEnqueue(BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.None, reason));
                member.Complete();
            }
        }
    }

    /// <summary>
    /// What a client sent and the relay has not handled yet, between the socket's reader and the relay loop.
    /// </summary>
    /// <remarks>
    /// Bounded by bytes: the reader waits once a budget's worth is queued - and with it the socket's receive, so a
    /// client that keeps sending while the host is busy with it is slowed down rather than buffered without bound.
    /// A message is always admitted to an empty queue, whatever its size.
    /// </remarks>
    private sealed class Inbound(long byteLimit)
    {
        private readonly Channel<byte[]> _messages = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = true });

        // Released once for every message taken, so a writer waiting for room looks again.
        private readonly SemaphoreSlim _taken = new(0);
        private long _bytes;

        /// <summary>Queues a message once there is room for it. Called by the reader alone.</summary>
        public async ValueTask WriteAsync(byte[] message, CancellationToken cancellationToken)
        {
            while (Interlocked.Read(ref _bytes) is var queued && queued > 0 && queued + message.Length > byteLimit)
            {
                await _taken.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            Interlocked.Add(ref _bytes, message.Length);
            _messages.Writer.TryWrite(message);
        }

        /// <summary>The next message, or <see langword="null"/> once the reader has finished and nothing is left.</summary>
        public async ValueTask<byte[]?> ReadAsync(CancellationToken cancellationToken)
        {
            while (await _messages.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!_messages.Reader.TryRead(out byte[]? message)) continue;

                Interlocked.Add(ref _bytes, -message.Length);
                _taken.Release();
                return message;
            }

            return null;
        }

        /// <summary>Marks the end of what the client sent.</summary>
        public void Complete() => _messages.Writer.TryComplete();
    }

    /// <summary>
    /// One connected client, with the queue that decouples it from the rest of the room.
    /// </summary>
    private sealed class Member
    {
        private readonly WebSocket _socket;
        private readonly Channel<Outbound> _outbound;
        private readonly int _capacity;
        private readonly long _byteLimit;

        // Two budgets: one for what the room and the host send unasked, which a client that falls behind
        // is disconnected for exceeding, and one for the answer to its own handshake, which is paced.
        private int _queuedCount;
        private long _queuedBytes;
        private int _answerCount;
        private long _answerBytes;

        private TaskCompletionSource? _drained;
        private int _completed;

        public Member(WebSocket socket, int capacity, long byteLimit)
        {
            _socket = socket;
            _capacity = capacity;
            _byteLimit = byteLimit;

            // Unbounded because both budgets are counted here instead. Single reader because the pump is
            // the only consumer, which is also what satisfies the WebSocket rule that one socket may have
            // only one send in flight.
            _outbound = Channel.CreateUnbounded<Outbound>(new UnboundedChannelOptions { SingleReader = true });
        }

        /// <summary>Whether no more messages will be queued for this member.</summary>
        public bool IsCompleted => Volatile.Read(ref _completed) != 0;

        /// <summary>
        /// Queues a message, reporting <see langword="false"/> when the client is too far behind - by
        /// message count or by bytes.
        /// </summary>
        public bool TryEnqueue(byte[] message)
        {
            if (Interlocked.Increment(ref _queuedCount) > _capacity)
            {
                Interlocked.Decrement(ref _queuedCount);
                return false;
            }

            if (Interlocked.Add(ref _queuedBytes, message.Length) <= _byteLimit &&
                _outbound.Writer.TryWrite(new Outbound(message, Answer: false)))
            {
                return true;
            }

            Interlocked.Decrement(ref _queuedCount);
            Interlocked.Add(ref _queuedBytes, -message.Length);
            return false;
        }

        /// <summary>
        /// Queues part of the answer to this client's own handshake once the parts before it have
        /// drained far enough, reporting <see langword="false"/> if the member is gone meanwhile.
        /// </summary>
        /// <remarks>
        /// Called by the member's own relay loop only, one part at a time. A part is admitted alone
        /// whatever its size, so one larger than the whole budget is still sent rather than waited on
        /// for ever.
        /// </remarks>
        public async ValueTask<bool> EnqueueAnswerAsync(byte[] message, CancellationToken cancellationToken)
        {
            while (!IsCompleted && !HasRoomForAnswer(message.Length))
            {
                // Taken before the second look, so a part the pump takes in between still wakes this.
                Task drained = Drained();
                if (IsCompleted || HasRoomForAnswer(message.Length)) break;
                await drained.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (IsCompleted) return false;

            Interlocked.Increment(ref _answerCount);
            Interlocked.Add(ref _answerBytes, message.Length);
            if (_outbound.Writer.TryWrite(new Outbound(message, Answer: true))) return true;

            Interlocked.Decrement(ref _answerCount);
            Interlocked.Add(ref _answerBytes, -message.Length);
            return false;
        }

        private bool HasRoomForAnswer(int length)
        {
            int count = Volatile.Read(ref _answerCount);
            return count == 0 || (count < _capacity && Interlocked.Read(ref _answerBytes) + length <= _byteLimit);
        }

        private Task Drained()
        {
            if (Volatile.Read(ref _drained) is { } current) return current.Task;
            var created = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return (Interlocked.CompareExchange(ref _drained, created, null) ?? created).Task;
        }

        private void SignalDrained() => Interlocked.Exchange(ref _drained, null)?.TrySetResult();

        /// <summary>Signals that no more messages will be queued, letting the pump finish and return.</summary>
        public void Complete()
        {
            Volatile.Write(ref _completed, 1);
            _outbound.Writer.TryComplete();
            SignalDrained();
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
            SignalDrained();
            _socket.Abort();
        }

        /// <summary>Sends queued messages, one at a time, until the queue is completed or the socket dies.</summary>
        public async Task PumpAsync(CancellationToken cancellationToken)
        {
            try
            {
                await foreach (Outbound item in _outbound.Reader
                    .ReadAllAsync(cancellationToken)
                    .ConfigureAwait(false))
                {
                    byte[] message = item.Message;
                    if (item.Answer)
                    {
                        Interlocked.Decrement(ref _answerCount);
                        Interlocked.Add(ref _answerBytes, -message.Length);
                        SignalDrained();
                    }
                    else
                    {
                        Interlocked.Decrement(ref _queuedCount);
                        Interlocked.Add(ref _queuedBytes, -message.Length);
                    }

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
            finally
            {
                // Nothing queued from now on would be sent, and an answer waiting for room must not wait
                // for a pump that has stopped.
                Complete();
            }
        }

        private readonly record struct Outbound(byte[] Message, bool Answer);
    }
}
