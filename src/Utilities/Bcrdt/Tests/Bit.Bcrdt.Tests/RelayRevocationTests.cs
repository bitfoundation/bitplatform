using System.Diagnostics;
using System.Net.WebSockets;
using System.Security.Claims;
using Bit.Bcrdt.Server;
using Bit.Bcrdt.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using LoopbackHost = Bit.Bcrdt.Tests.SyncRegressionTests.LoopbackHost;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Ending and narrowing a grant while the connection is open, through <see cref="BcrdtRelayConnections"/> - over
/// both real relays, with and without a document host.
/// </summary>
[TestClass]
public class RelayRevocationTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(300);

    [TestMethod]
    public async Task A_bare_hub_revokes_a_connection_and_relays_nothing_more_to_or_from_it()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app =>
            {
                SignInFromQuery(app);
                app.MapHub<BcrdtHub>("/hub");
            },
            builder => builder.Services.AddBitBcrdtHub());
        BcrdtRelayConnections connections = host.App.Services.GetRequiredService<BcrdtRelayConnections>();

        await using Participant alice = await Participant.OverSignalRAsync(host, "doc", "alice", 1);
        await using Participant bob = await Participant.OverSignalRAsync(host, "doc", "bob", 2);
        await using Participant carol = await Participant.OverSignalRAsync(host, "doc", "carol", 3);
        await AssertEventuallyAsync(() => alice.Provider.IsSynced && bob.Provider.IsSynced && carol.Provider.IsSynced, "everyone connected");
        await AssertEventuallyAsync(() => connections.GetConnections("doc").Count == 3, "every connection is listed");
        CollectionAssert.AreEquivalent(new[] { "alice", "bob", "carol" }, connections.GetConnections("doc").Select(connection => connection.UserId).ToArray());

        alice.Text.Insert(0, "shared ");
        await AssertEventuallyAsync(() => bob.Body == "shared " && carol.Body == "shared ", "everyone edits together until revoked");

        Assert.AreEqual(1, await connections.RevokeAsync("doc", connection => connection.UserId == "bob", "Removed from the document."));

        await AssertEventuallyAsync(() => bob.Denied == 1, "the revoked client must be told");
        Assert.AreEqual("Removed from the document.", bob.DeniedWith);
        Assert.AreEqual(BcrdtConnectionStatus.Faulted, bob.Provider.Status);
        CollectionAssert.AreEquivalent(new[] { "alice", "carol" }, connections.GetConnections("doc").Select(connection => connection.UserId).ToArray(), "a refused connection is taken off the list at once");
        Assert.AreEqual(0, await connections.RevokeAsync("doc", connection => connection.UserId == "bob"), "revoking twice does nothing");

        alice.Text.Insert(7, "still");
        bob.Text.Insert(0, "too late ");
        await AssertEventuallyAsync(() => carol.Body == "shared still", "the others go on editing");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("shared still", alice.Body, "nothing the revoked client wrote reaches anyone");
        Assert.AreEqual("too late shared ", bob.Body, "and nothing more reaches it");
        Assert.AreEqual(0, alice.Denied + carol.Denied);

        await alice.DisposeAsync();
        await bob.DisposeAsync();
        await carol.DisposeAsync();
        await AssertEventuallyAsync(() => connections.Documents.Count == 0, "the list must empty as clients leave");
    }

    [TestMethod]
    public async Task A_bare_websocket_relay_revokes_connections_and_closes_a_socket_that_ignores_its_refusal()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app =>
            {
                SignInFromQuery(app);
                app.MapBitBcrdtWebSocket("/ws/{documentId}");
            },
            builder => builder.Services.AddBitBcrdtRelayConnections());
        BcrdtRelayConnections connections = host.App.Services.GetRequiredService<BcrdtRelayConnections>();

        await using Participant alice = await Participant.OverWebSocketAsync(host, "doc", "alice", 1);
        await using Participant bob = await Participant.OverWebSocketAsync(host, "doc", "bob", 2);
        await using Participant carol = await Participant.OverWebSocketAsync(host, "doc", "carol", 3);
        await AssertEventuallyAsync(() => alice.Provider.IsSynced && bob.Provider.IsSynced && carol.Provider.IsSynced, "everyone connected");

        // A modified client: it reads what it is sent and pays no attention to a refusal.
        using ClientWebSocket mallory = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/doc?user=mallory");
        await AssertEventuallyAsync(() => connections.GetConnections("doc").Count == 4, "every connection is listed");

        alice.Text.Insert(0, "shared ");
        await AssertEventuallyAsync(() => bob.Body == "shared " && carol.Body == "shared ", "everyone edits together until revoked");

        Assert.AreEqual(2, await connections.RevokeAsync("doc", connection => connection.UserId is "bob" or "mallory"));

        await AssertEventuallyAsync(() => bob.Denied == 1, "the revoked client must be told");
        Assert.AreEqual(BcrdtDocumentHost.DefaultRevocationReason, bob.DeniedWith);
        Assert.AreEqual(BcrdtConnectionStatus.Faulted, bob.Provider.Status);

        BcrdtSyncMessage refusal = await RawSocket.ReceiveAsync(mallory, BcrdtSyncMessageKind.Auth);
        Assert.AreEqual(BcrdtDocumentAccess.None, BcrdtSyncProtocol.DecodeAuthAccess(refusal.Payload).Access);
        Assert.IsTrue(await RawSocket.IsClosedAsync(mallory), "the relay closes a refused socket itself");

        // Whatever it manages to send after that reaches nobody.
        using var forged = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 66 });
        forged.GetText("body").Insert(0, "forged ");
        await RawSocket.TrySendAsync(mallory, BcrdtSyncProtocol.EncodeUpdate(forged.EncodeStateAsUpdate()));

        alice.Text.Insert(7, "still");
        await AssertEventuallyAsync(() => carol.Body == "shared still", "the others go on editing");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("shared still", alice.Body);
        Assert.AreEqual("shared ", bob.Body, "nothing more reaches a revoked client");
        CollectionAssert.AreEquivalent(new[] { "alice", "carol" }, connections.GetConnections("doc").Select(connection => connection.UserId).ToArray());

        await alice.DisposeAsync();
        await bob.DisposeAsync();
        await carol.DisposeAsync();
        await AssertEventuallyAsync(() => connections.Documents.Count == 0, "the list must empty as clients leave");
    }

    [TestMethod]
    public async Task With_a_host_revoking_through_the_list_withdraws_presence_and_revokes_in_the_host_too()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app =>
            {
                SignInFromQuery(app);
                app.MapHub<BcrdtHub>("/hub");
            },
            builder =>
            {
                builder.Services.AddBitBcrdtHub();
                builder.Services.AddBitBcrdtDocumentHost();
            });
        BcrdtRelayConnections connections = host.App.Services.GetRequiredService<BcrdtRelayConnections>();
        BcrdtDocumentHost documentHost = host.App.Services.GetRequiredService<BcrdtDocumentHost>();

        await using Participant alice = await Participant.OverSignalRAsync(host, "doc", "alice", 1);
        await using Participant bob = await Participant.OverSignalRAsync(host, "doc", "bob", 2);
        await using Participant carol = await Participant.OverSignalRAsync(host, "doc", "carol", 3);
        await AssertEventuallyAsync(() => alice.Provider.IsSynced && bob.Provider.IsSynced && carol.Provider.IsSynced, "everyone connected");

        bob.Presence.SetLocalField("name", "Bob");
        await AssertEventuallyAsync(() => alice.Presence.GetField(2, "name") as string == "Bob", "bob is on everyone's participant list");

        alice.Text.Insert(0, "shared ");
        await AssertEventuallyAsync(() => bob.Body == "shared ", "everyone edits together until revoked");

        Assert.AreEqual(1, await connections.RevokeAsync("doc", connection => connection.UserId == "bob"));
        Assert.AreEqual(0, await documentHost.RevokeAsync("doc", connection => connection.UserId == "bob"), "the host has let go of it already");

        await AssertEventuallyAsync(() => bob.Denied == 1, "the revoked client must be told");
        Assert.AreEqual(BcrdtConnectionStatus.Faulted, bob.Provider.Status);
        await AssertEventuallyAsync(() => alice.Presence.GetField(2, "name") is null, "its presence is withdrawn at once, not when it times out");

        alice.Text.Insert(7, "still");
        bob.Text.Insert(0, "too late ");
        await AssertEventuallyAsync(
            async () => await documentHost.ReadAsync("doc", doc => doc.GetText("body").ToString()) == "shared still",
            "the host goes on taking the others' changes");
        await AssertEventuallyAsync(() => carol.Body == "shared still", "and the others go on editing");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("shared still", await documentHost.ReadAsync("doc", doc => doc.GetText("body").ToString()), "nothing the revoked client wrote is kept");
        Assert.AreEqual("too late shared ", bob.Body, "and nothing more reaches it");

        // A refusal that comes from the host takes the connection off the list too.
        Assert.AreEqual(1, await documentHost.RevokeAsync("doc", connection => connection.UserId == "carol"));
        await AssertEventuallyAsync(() => carol.Denied == 1, "a client the host revokes is told");
        CollectionAssert.AreEquivalent(new[] { "alice" }, connections.GetConnections("doc").Select(connection => connection.UserId).ToArray());
    }

    [TestMethod]
    public async Task Revoking_across_every_document_reaches_both_relays_and_their_host()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app =>
            {
                SignInFromQuery(app);
                app.MapHub<BcrdtHub>("/hub");
                app.MapBitBcrdtWebSocket("/ws/{documentId}", options: new BcrdtWebSocketEndpointOptions
                {
                    DocumentHost = app.Services.GetRequiredService<BcrdtDocumentHost>(),
                });
            },
            builder =>
            {
                builder.Services.AddBitBcrdtHub();
                builder.Services.AddBitBcrdtDocumentHost();
            });
        BcrdtRelayConnections connections = host.App.Services.GetRequiredService<BcrdtRelayConnections>();
        BcrdtDocumentHost documentHost = host.App.Services.GetRequiredService<BcrdtDocumentHost>();

        await using Participant aliceOnOne = await Participant.OverWebSocketAsync(host, "one", "alice", 1);
        await using Participant aliceOnTwo = await Participant.OverSignalRAsync(host, "two", "alice", 2);
        await using Participant bobOnOne = await Participant.OverSignalRAsync(host, "one", "bob", 3);
        await using Participant bobOnTwo = await Participant.OverWebSocketAsync(host, "two", "bob", 4);
        await AssertEventuallyAsync(
            () => aliceOnOne.Provider.IsSynced && aliceOnTwo.Provider.IsSynced && bobOnOne.Provider.IsSynced && bobOnTwo.Provider.IsSynced,
            "everyone connected");
        await AssertEventuallyAsync(() => connections.GetConnections("one").Count == 2 && connections.GetConnections("two").Count == 2, "every connection is listed");
        CollectionAssert.AreEquivalent(new[] { "one", "two" }, connections.Documents.ToArray());
        Assert.HasCount(2, connections.GetConnections("one").Select(connection => connection.Relay).Distinct(), "one document, two relays");

        Assert.AreEqual(2, await connections.RevokeAsync((documentId, connection) => connection.UserId == "bob"));

        await AssertEventuallyAsync(() => bobOnOne.Denied == 1 && bobOnTwo.Denied == 1, "both of bob's connections are told");
        CollectionAssert.AreEquivalent(new[] { "alice", "alice" }, connections.Documents.SelectMany(connections.GetConnections).Select(connection => connection.UserId).ToArray());

        aliceOnOne.Text.Insert(0, "one");
        aliceOnTwo.Text.Insert(0, "two");
        await AssertEventuallyAsync(
            async () => await documentHost.ReadAsync("one", doc => doc.GetText("body").ToString()) == "one" &&
                await documentHost.ReadAsync("two", doc => doc.GetText("body").ToString()) == "two",
            "alice goes on editing both");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("", bobOnOne.Body);
        Assert.AreEqual("", bobOnTwo.Body);
    }

    [TestMethod]
    public async Task Narrowing_a_grant_makes_a_hub_connection_read_only_without_disconnecting_it()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app =>
            {
                SignInFromQuery(app);
                app.MapHub<BcrdtHub>("/hub");
            },
            builder => builder.Services.AddBitBcrdtHub());
        BcrdtRelayConnections connections = host.App.Services.GetRequiredService<BcrdtRelayConnections>();

        await using Participant alice = await Participant.OverSignalRAsync(host, "doc", "alice", 1);
        await using Participant bob = await Participant.OverSignalRAsync(host, "doc", "bob", 2);
        await AssertEventuallyAsync(() => alice.Provider.IsSynced && bob.Provider.IsSynced, "both connected");
        int connected = bob.Connections;

        Assert.AreEqual(1, await connections.MakeReadOnlyAsync("doc", connection => connection.UserId == "bob"));
        Assert.AreEqual(0, await connections.MakeReadOnlyAsync("doc", connection => connection.UserId == "bob"), "it is read-only already");

        await AssertEventuallyAsync(() => bob.Provider.Access == BcrdtDocumentAccess.ReadOnly, "the client is told");
        Assert.AreEqual(BcrdtDocumentAccess.ReadOnly, connections.GetConnections("doc").Single(connection => connection.UserId == "bob").Access);
        Assert.AreEqual(BcrdtDocumentAccess.ReadWrite, connections.GetConnections("doc").Single(connection => connection.UserId == "alice").Access);

        alice.Text.Insert(0, "hello");
        await AssertEventuallyAsync(() => bob.Body == "hello", "a reader still receives changes");

        bob.Text.Insert(0, "X");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("hello", alice.Body, "nothing it writes reaches anyone");
        Assert.AreEqual(BcrdtConnectionStatus.Synced, bob.Provider.Status);
        Assert.AreEqual(connected, bob.Connections, "and it stayed connected throughout");
        Assert.AreEqual(0, bob.Denied);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task A_narrowed_socket_that_ignores_its_grant_changes_nothing(bool hosted)
    {
        await using var documentHost = new BcrdtDocumentHost();
        var connections = new BcrdtRelayConnections();
        await using LoopbackHost host = await LoopbackHost.StartAsync(app =>
        {
            SignInFromQuery(app);
            app.MapBitBcrdtWebSocket("/ws/{documentId}", options: new BcrdtWebSocketEndpointOptions
            {
                DocumentHost = hosted ? documentHost : null,
                Connections = connections,
            });
        });

        await using Participant alice = await Participant.OverWebSocketAsync(host, "doc", "alice", 1);
        await AssertEventuallyAsync(() => alice.Provider.IsSynced, "connected");
        using ClientWebSocket mallory = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/doc?user=mallory");
        await AssertEventuallyAsync(() => connections.GetConnections("doc").Count == 2, "both connections are listed");

        Assert.AreEqual(1, await connections.MakeReadOnlyAsync((documentId, connection) => connection.UserId == "mallory"));
        BcrdtSyncMessage grant = await RawSocket.ReceiveAsync(mallory, BcrdtSyncMessageKind.Auth);
        Assert.AreEqual(BcrdtDocumentAccess.ReadOnly, BcrdtSyncProtocol.DecodeAuthAccess(grant.Payload).Access);

        using var forged = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 66 });
        forged.GetText("body").Insert(0, "forged ");
        await RawSocket.TrySendAsync(mallory, BcrdtSyncProtocol.EncodeUpdate(forged.EncodeStateAsUpdate()));

        alice.Text.Insert(0, "kept");
        if (hosted)
        {
            await AssertEventuallyAsync(
                async () => await documentHost.ReadAsync("doc", doc => doc.GetText("body").ToString()) == "kept",
                "the host takes the writer's change");
        }

        await Task.Delay(QuietPeriod);
        Assert.AreEqual("kept", alice.Body, "a narrowed connection's change reaches nobody");
        if (hosted) Assert.AreEqual("kept", await documentHost.ReadAsync("doc", doc => doc.GetText("body").ToString()), "and the host does not apply it");

        BcrdtSyncMessage relayed = await RawSocket.ReceiveAsync(mallory, BcrdtSyncMessageKind.Update);
        using var seen = new BcrdtDocument();
        seen.ApplyUpdate(relayed.Payload);
        Assert.AreEqual("kept", seen.GetText("body").ToString(), "it still receives the others' changes");
    }

    [TestMethod]
    public void The_hub_and_the_explicit_registration_share_one_list()
    {
        var services = new ServiceCollection();
        services.AddBitBcrdtHub();
        services.AddBitBcrdtRelayConnections();
        services.AddBitBcrdtRelayConnections();

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.HasCount(1, provider.GetServices<BcrdtRelayConnections>());
        Assert.AreSame(provider.GetRequiredService<BcrdtRelayConnections>(), provider.GetRequiredService<BcrdtRelayConnections>());
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    /// <summary>Signs every request in as the user named by <c>?user=</c>, so the relays have a user identifier to go by.</summary>
    private static void SignInFromQuery(WebApplication app) => app.Use((context, next) =>
    {
        if (context.Request.Query["user"].ToString() is { Length: > 0 } user)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user)], "query"));
        }
        return next(context);
    });

    private static async Task AssertEventuallyAsync(Func<bool> condition, string because)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.IsTrue(clock.Elapsed < Patience, because);
            await Task.Delay(10);
        }
    }

    private static async Task AssertEventuallyAsync(Func<Task<bool>> condition, string because)
    {
        var clock = Stopwatch.StartNew();
        while (!await condition())
        {
            Assert.IsTrue(clock.Elapsed < Patience, because);
            await Task.Delay(10);
        }
    }

    /// <summary>A signed-in user's replica, presence and provider, connected to one document through one relay.</summary>
    private sealed class Participant : IAsyncDisposable
    {
        private int _denied;
        private int _connections;
        private int _disposed;

        private Participant(ulong clientId, IBcrdtTransport transport)
        {
            Doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
            Presence = new BcrdtAwarenessStore(clientId);
            Transport = transport;
            transport.StateChanged += change =>
            {
                if (change.State == BcrdtTransportState.Connected) Interlocked.Increment(ref _connections);
            };
        }

        public BcrdtDocument Doc { get; }

        public BcrdtAwarenessStore Presence { get; }

        public IBcrdtTransport Transport { get; }

        public BcrdtSyncProvider Provider { get; private set; } = null!;

        public BcrdtText Text => Doc.GetText("body");

        public string Body => Text.ToString();

        public int Denied => Volatile.Read(ref _denied);

        public string? DeniedWith { get; private set; }

        /// <summary>How many times the transport has connected.</summary>
        public int Connections => Volatile.Read(ref _connections);

        public static Task<Participant> OverSignalRAsync(LoopbackHost host, string documentId, string user, ulong clientId) =>
            ConnectAsync(clientId, new BcrdtSignalRTransport(host.BaseUrl + "/hub?user=" + user, documentId));

        public static Task<Participant> OverWebSocketAsync(LoopbackHost host, string documentId, string user, ulong clientId) =>
            ConnectAsync(clientId, new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/" + documentId + "?user=" + user)));

        private static async Task<Participant> ConnectAsync(ulong clientId, IBcrdtTransport transport)
        {
            var participant = new Participant(clientId, transport);

            // Built on the pool, so the provider captures no synchronization context. On a bare relay nobody answers
            // the first one in, so a short handshake timeout is what lets it count as synced.
            participant.Provider = await Task.Run(() => new BcrdtSyncProvider(participant.Doc, transport, participant.Presence, new BcrdtSyncProviderOptions
            {
                HeartbeatInterval = TimeSpan.Zero,
                HandshakeTimeout = TimeSpan.FromMilliseconds(300),
                Reconnect = new BcrdtReconnectPolicy { BaseDelay = TimeSpan.FromMilliseconds(5), MaxDelay = TimeSpan.FromMilliseconds(20) },
            }));
            participant.Provider.AuthorizationDenied += reason =>
            {
                participant.DeniedWith = reason;
                Interlocked.Increment(ref participant._denied);
            };

            await participant.Provider.ConnectAsync();
            return participant;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

            await Provider.DisposeAsync();
            await Transport.DisposeAsync();
            Presence.Dispose();
            Doc.Dispose();
        }
    }

    /// <summary>A bare socket against the WebSocket relay: a client that does only what the test says.</summary>
    private static class RawSocket
    {
        /// <summary>Connects and reads past the read-write grant the relay sends first.</summary>
        public static async Task<ClientWebSocket> ConnectAsync(string address)
        {
            var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(address), CancellationToken.None);
            BcrdtSyncMessage grant = await ReceiveAsync(socket, BcrdtSyncMessageKind.Auth);
            Assert.AreEqual(BcrdtDocumentAccess.ReadWrite, BcrdtSyncProtocol.DecodeAuthAccess(grant.Payload).Access);
            return socket;
        }

        /// <summary>Reads, skipping anything else, until a message of a kind arrives. A socket that outlasts the test's patience is aborted.</summary>
        public static async Task<BcrdtSyncMessage> ReceiveAsync(ClientWebSocket socket, BcrdtSyncMessageKind kind)
        {
            using var patience = new CancellationTokenSource(Patience);
            while (true)
            {
                byte[]? message = await ReadAsync(socket, patience.Token);
                Assert.IsNotNull(message, $"the socket closed before a {kind} message arrived");

                BcrdtSyncMessage decoded = BcrdtSyncProtocol.Decode(message);
                if (decoded.Kind == kind) return decoded;
            }
        }

        /// <summary>Whether the server closes the socket, once whatever was queued before the close has been read.</summary>
        public static async Task<bool> IsClosedAsync(ClientWebSocket socket)
        {
            using var patience = new CancellationTokenSource(Patience);
            try
            {
                while (await ReadAsync(socket, patience.Token) is not null)
                {
                }
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        /// <summary>Sends, if the socket still takes it.</summary>
        public static async Task TrySendAsync(ClientWebSocket socket, byte[] message)
        {
            try
            {
                await socket.SendAsync(message, WebSocketMessageType.Binary, true, CancellationToken.None);
            }
            catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException or InvalidOperationException)
            {
                // Already closed: what it sent cannot reach anyone either way.
            }
        }

        /// <summary>One whole message, or null once the server has closed the socket.</summary>
        private static async Task<byte[]?> ReadAsync(ClientWebSocket socket, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1 << 16];
            using var message = new MemoryStream();
            while (true)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (WebSocketException)
                {
                    // Dropped rather than closed: as closed, for what the test needs to know.
                    return null;
                }

                if (result.MessageType == WebSocketMessageType.Close) return null;
                message.Write(buffer, 0, result.Count);
                if (result.EndOfMessage) return message.ToArray();
            }
        }
    }
}
