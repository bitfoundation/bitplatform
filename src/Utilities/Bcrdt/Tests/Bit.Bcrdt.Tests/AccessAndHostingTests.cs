using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using Bit.Bcrdt.Server;
using Bit.Bcrdt.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using LoopbackHost = Bit.Bcrdt.Tests.SyncRegressionTests.LoopbackHost;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Read-only access, the handshake's completion rule, reconnection pacing, and the server-side
/// document host - over the real relays where the behaviour lives in them.
/// </summary>
[TestClass]
public class AccessAndHostingTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(300);

    private static BcrdtSyncProviderOptions Options(TimeSpan? handshakeTimeout = null, BcrdtReconnectPolicy? reconnect = null) => new()
    {
        HeartbeatInterval = TimeSpan.Zero,
        HandshakeTimeout = handshakeTimeout ?? TimeSpan.FromMinutes(5),
        Reconnect = reconnect ?? new BcrdtReconnectPolicy { BaseDelay = TimeSpan.FromMilliseconds(5), MaxDelay = TimeSpan.FromMilliseconds(20) },
    };

    /// <summary>Built on the pool, so the provider captures no synchronization context.</summary>
    private static Task<BcrdtSyncProvider> ProviderAsync(
        BcrdtDocument doc,
        IBcrdtTransport transport,
        BcrdtAwarenessStore? awareness = null,
        BcrdtSyncProviderOptions? options = null) =>
        Task.Run(() => new BcrdtSyncProvider(doc, transport, awareness, options ?? Options()));

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

    // ---------------------------------------------------------------------------------------------
    // Protocol
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_read_only_grant_is_still_a_permission_to_a_client_that_predates_it()
    {
        BcrdtSyncMessage grant = BcrdtSyncProtocol.Decode(BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.ReadOnly, null));

        Assert.IsTrue(BcrdtSyncProtocol.DecodeAuth(grant.Payload).Permitted);
        Assert.AreEqual(BcrdtDocumentAccess.ReadOnly, BcrdtSyncProtocol.DecodeAuthAccess(grant.Payload).Access);

        // A grant from a newer server that this build does not know is read narrowly, never as a refusal.
        byte[] unknown = BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.ReadWrite, "x");
        unknown[2] = 9;
        Assert.AreEqual(BcrdtDocumentAccess.ReadOnly, BcrdtSyncProtocol.DecodeAuthAccess(BcrdtSyncProtocol.Decode(unknown).Payload).Access);
        Assert.IsTrue(BcrdtSyncProtocol.IsWrite(BcrdtSyncProtocol.EncodeUpdate([0, 0])));
        Assert.IsTrue(BcrdtSyncProtocol.IsWrite(BcrdtSyncProtocol.EncodeSyncStep2([0, 0])));
        Assert.IsFalse(BcrdtSyncProtocol.IsWrite(BcrdtSyncProtocol.EncodeSyncStep1(new BcrdtStateVector())));
        Assert.IsFalse(BcrdtSyncProtocol.IsWrite(BcrdtSyncProtocol.EncodeAwarenessQuery()));
    }

    // ---------------------------------------------------------------------------------------------
    // The provider
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_read_only_provider_answers_nobody_and_keeps_its_local_edits_to_itself()
    {
        var network = new BcrdtInMemoryNetwork();
        RawPeer server = await RawPeer.JoinAsync(network, "room");

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        doc.GetText("body").Insert(0, "mine");
        await using BcrdtSyncProvider provider = await ProviderAsync(doc, network.CreateTransport("room"));
        var errors = new ConcurrentQueue<Exception>();
        provider.Error += errors.Enqueue;
        int accessChanges = 0;
        provider.AccessChanged += _ => Interlocked.Increment(ref accessChanges);

        await provider.ConnectAsync();
        await AssertEventuallyAsync(() => server.Received.Any(m => m.Kind == BcrdtSyncMessageKind.SyncStep1), "the provider should be in the room");
        await server.SendAsync(BcrdtSyncProtocol.EncodeAuth(BcrdtDocumentAccess.ReadOnly, null));
        await AssertEventuallyAsync(() => provider.Access == BcrdtDocumentAccess.ReadOnly, "the grant should be taken");
        Assert.AreEqual(1, accessChanges);

        // Asked what it has, it answers nothing: its content would be dropped, and a statement of what it
        // holds would promise content nobody will ever receive.
        server.Received.Clear();
        await server.SendAsync(BcrdtSyncProtocol.EncodeSyncStep1(new BcrdtStateVector()));
        await server.SendAsync(BcrdtSyncProtocol.EncodeSyncStep1Reply(new BcrdtStateVector(), 1));

        // A local edit stays local, and the application hears about it.
        doc.GetText("body").Insert(0, "!");
        await Task.Delay(QuietPeriod);

        Assert.IsFalse(server.Received.Any(m => m.Kind is BcrdtSyncMessageKind.Update or BcrdtSyncMessageKind.SyncStep2 or BcrdtSyncMessageKind.SyncStep1Reply));
        Assert.IsTrue(errors.Any(e => e is InvalidOperationException), "a local edit on a read-only connection must be reported");
    }

    [TestMethod]
    public async Task A_reply_without_its_delta_does_not_complete_the_handshake()
    {
        var network = new BcrdtInMemoryNetwork();
        RawPeer peer = await RawPeer.JoinAsync(network, "room");

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using BcrdtSyncProvider provider = await ProviderAsync(doc, network.CreateTransport("room"));
        await provider.ConnectAsync();
        await AssertEventuallyAsync(() => peer.Received.Any(m => m.Kind == BcrdtSyncMessageKind.SyncStep1), "the announcement should arrive");

        // The reply travels first and is small; on a slow link the delta behind it is still on its way.
        await peer.SendAsync(BcrdtSyncProtocol.EncodeSyncStep1Reply(new BcrdtStateVector(), 1));
        await Task.Delay(QuietPeriod);
        Assert.AreEqual(BcrdtConnectionStatus.Handshaking, provider.Status, "synced before the peer's data arrived");

        await peer.SendAsync(BcrdtSyncProtocol.EncodeSyncStep2(BcrdtUpdateEncoder.Empty));
        await AssertEventuallyAsync(() => provider.IsSynced, "the delta completes the handshake");
    }

    [TestMethod]
    public async Task A_connection_dropped_as_soon_as_it_opens_is_retried_with_a_growing_delay()
    {
        await using var transport = new FlappingTransport();
        using var doc = new BcrdtDocument();
        await using BcrdtSyncProvider provider = await ProviderAsync(doc, transport, options: Options(
            reconnect: new BcrdtReconnectPolicy { BaseDelay = TimeSpan.FromMilliseconds(100), MaxDelay = TimeSpan.FromSeconds(10) }));

        await provider.ConnectAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
        await provider.DisconnectAsync();

        // Exponential from 100 ms fits half a dozen attempts in two seconds; retrying at the first
        // delay forever fits forty.
        Assert.IsLessThan(15, transport.ConnectCalls, $"{transport.ConnectCalls} attempts in two seconds");
    }

    [TestMethod]
    public async Task A_document_larger_than_the_relays_message_limit_still_reaches_a_newcomer()
    {
        // A relay that drops anybody sending more than 64 KB at once, and a document of several times that.
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket(
            "/ws/{documentId}",
            options: new BcrdtWebSocketEndpointOptions { MaxMessageSize = 64 * 1024 }));

        var options = new BcrdtSyncProviderOptions
        {
            HeartbeatInterval = TimeSpan.Zero,
            HandshakeTimeout = TimeSpan.FromMilliseconds(500),
            MaxMessageSize = 32 * 1024,
        };

        using var holder = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        holder.GetText("body").Insert(0, string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog. ", 6_000)));
        await using var holderTransport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/big"));
        await using BcrdtSyncProvider holderProvider = await ProviderAsync(holder, holderTransport, options: options);
        await holderProvider.ConnectAsync();
        await AssertEventuallyAsync(() => holderProvider.IsSynced, "alone in the room");

        using var newcomer = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using var newcomerTransport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/big"));
        await using BcrdtSyncProvider newcomerProvider = await ProviderAsync(newcomer, newcomerTransport, options: options);
        await newcomerProvider.ConnectAsync();

        await AssertEventuallyAsync(() => newcomer.GetText("body").Count == holder.GetText("body").Count, "the whole document should arrive");
        await AssertEventuallyAsync(() => newcomerProvider.IsSynced, "and the newcomer should be synced");
        Assert.AreEqual(BcrdtTransportState.Connected, holderTransport.State, "the holder must not have been disconnected for answering");

        // A single paste larger than the limit goes through the same way.
        holder.GetText("body").Insert(0, new string('x', 200_000));
        await AssertEventuallyAsync(() => newcomer.GetText("body").Count == holder.GetText("body").Count, "the paste should arrive");
        Assert.AreEqual(BcrdtTransportState.Connected, holderTransport.State);
    }

    // ---------------------------------------------------------------------------------------------
    // Read-only access through the relays
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task The_websocket_relay_carries_nothing_a_reader_writes()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket(
            "/ws/{documentId}",
            options: new BcrdtWebSocketEndpointOptions { AuthorizeAccess = AccessFromQuery }));

        using var writerDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var readerDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using var writerTransport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/doc"));
        await using var readerTransport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/doc?role=reader"));
        await using BcrdtSyncProvider writer = await ProviderAsync(writerDoc, writerTransport, options: Options(TimeSpan.FromMilliseconds(300)));
        await using BcrdtSyncProvider reader = await ProviderAsync(readerDoc, readerTransport, options: Options(TimeSpan.FromMilliseconds(300)));

        await writer.ConnectAsync();
        await reader.ConnectAsync();
        await AssertEventuallyAsync(() => reader.Access == BcrdtDocumentAccess.ReadOnly, "the reader should be told");
        Assert.AreEqual(BcrdtDocumentAccess.ReadWrite, writer.Access);

        writerDoc.GetText("body").Insert(0, "hello");
        await AssertEventuallyAsync(() => readerDoc.GetText("body").ToString() == "hello", "a reader still receives changes");

        readerDoc.GetText("body").Insert(0, "X");
        await writer.ResyncAsync();
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("hello", writerDoc.GetText("body").ToString());

        // Even a client that ignores its grant gets nothing through.
        using ClientWebSocket rogue = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/doc?role=reader", BcrdtDocumentAccess.ReadOnly);
        using var forged = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 });
        forged.GetText("body").Insert(0, "forged ");
        await rogue.SendAsync(BcrdtSyncProtocol.EncodeUpdate(forged.EncodeStateAsUpdate()), WebSocketMessageType.Binary, true, CancellationToken.None);
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("hello", writerDoc.GetText("body").ToString());
    }

    [TestMethod]
    public async Task The_hub_carries_nothing_a_reader_writes()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app => app.MapHub<ReaderAwareHub>("/hub"),
            builder => builder.Services.AddBitBcrdtHub<ReaderAwareHub>());

        using var writerDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var readerDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using var writerTransport = new BcrdtSignalRTransport(host.BaseUrl + "/hub", "doc");
        await using var readerTransport = new BcrdtSignalRTransport(host.BaseUrl + "/hub?role=reader", "doc");
        await using BcrdtSyncProvider writer = await ProviderAsync(writerDoc, writerTransport, options: Options(TimeSpan.FromMilliseconds(300)));
        await using BcrdtSyncProvider reader = await ProviderAsync(readerDoc, readerTransport, options: Options(TimeSpan.FromMilliseconds(300)));

        await writer.ConnectAsync();
        await reader.ConnectAsync();
        await AssertEventuallyAsync(() => reader.Access == BcrdtDocumentAccess.ReadOnly, "the reader should be told");

        writerDoc.GetText("body").Insert(0, "hello");
        await AssertEventuallyAsync(() => readerDoc.GetText("body").ToString() == "hello", "a reader still receives changes");

        readerDoc.GetText("body").Insert(0, "X");
        await writer.ResyncAsync();
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("hello", writerDoc.GetText("body").ToString());
    }

    // ---------------------------------------------------------------------------------------------
    // The document host
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_hosted_document_outlives_its_last_client_and_its_host()
    {
        var store = new BcrdtInMemoryDocumentStore();
        await using (var documentHost = new BcrdtDocumentHost(new BcrdtDocumentHostOptions { Store = store, UnloadDelay = TimeSpan.Zero }))
        {
            await using LoopbackHost host = await HostedWebSocketAsync(documentHost);

            // The author edits, then leaves before anybody else arrives.
            using (var author = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
            {
                await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/notes"));
                await using BcrdtSyncProvider provider = await ProviderAsync(author, transport);
                await provider.ConnectAsync();
                await AssertEventuallyAsync(() => provider.IsSynced, "the host answers the handshake");

                author.GetText("body").Insert(0, "kept by the server");
                await AssertEventuallyAsync(
                    async () => await documentHost.ReadAsync("notes", doc => doc.GetText("body").ToString()) == "kept by the server",
                    "the host should hold the change");
            }

            // A newcomer to the empty room is given the document by the host alone.
            using var newcomer = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
            await using var newcomerTransport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/notes"));
            await using BcrdtSyncProvider newcomerProvider = await ProviderAsync(newcomer, newcomerTransport);
            await newcomerProvider.ConnectAsync();

            await AssertEventuallyAsync(() => newcomerProvider.IsSynced, "the newcomer should be synced");
            Assert.AreEqual("kept by the server", newcomer.GetText("body").ToString());
        }

        // And a new host on the same store - a restarted server - has it too.
        await using var restarted = new BcrdtDocumentHost(new BcrdtDocumentHostOptions { Store = store });
        Assert.AreEqual("kept by the server", await restarted.ReadAsync("notes", doc => doc.GetText("body").ToString()));
    }

    [TestMethod]
    public async Task A_client_returning_from_offline_hands_its_work_to_the_host()
    {
        await using var documentHost = new BcrdtDocumentHost();
        await using LoopbackHost host = await HostedWebSocketAsync(documentHost);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        doc.GetText("body").Insert(0, "written offline");
        doc.GetText("body").Delete(0, 8);

        await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/offline"));
        await using BcrdtSyncProvider provider = await ProviderAsync(doc, transport);
        await provider.ConnectAsync();

        await AssertEventuallyAsync(
            async () => await documentHost.ReadAsync("offline", hosted => hosted.GetText("body").ToString()) == "offline",
            "the host should collect the offline work, deletions included");
    }

    [TestMethod]
    public async Task A_change_made_on_the_server_reaches_connected_clients()
    {
        await using var documentHost = new BcrdtDocumentHost();
        await using LoopbackHost host = await HostedWebSocketAsync(documentHost);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/board"));
        await using BcrdtSyncProvider provider = await ProviderAsync(doc, transport);
        await provider.ConnectAsync();
        await AssertEventuallyAsync(() => provider.IsSynced, "connected");

        await documentHost.EditAsync("board", hosted => hosted.GetMap("").Set("status", "approved"));

        await AssertEventuallyAsync(() => doc.GetMap("").Get("status") as string == "approved", "the server's change should arrive");
    }

    [TestMethod]
    public async Task A_change_the_host_rejects_refuses_its_sender_and_reaches_nobody()
    {
        await using var documentHost = new BcrdtDocumentHost(new BcrdtDocumentHostOptions
        {
            // The rule is checked against what the document would become, on a scratch copy.
            ValidateUpdate = context =>
            {
                using var probe = new BcrdtDocument();
                probe.ApplyUpdate(context.Document.EncodeStateAsUpdate());
                probe.ApplyUpdate(context.Update);
                return ValueTask.FromResult(!probe.GetText("body").ToString().Contains("forbidden", StringComparison.Ordinal));
            },
        });
        await using LoopbackHost host = await HostedWebSocketAsync(documentHost);

        using var goodDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var badDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using var goodTransport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/rules"));
        await using var badTransport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/rules"));
        await using BcrdtSyncProvider good = await ProviderAsync(goodDoc, goodTransport);
        await using BcrdtSyncProvider bad = await ProviderAsync(badDoc, badTransport);
        int denied = 0;
        bad.AuthorizationDenied += _ => Interlocked.Increment(ref denied);

        await good.ConnectAsync();
        await bad.ConnectAsync();
        await AssertEventuallyAsync(() => good.IsSynced && bad.IsSynced, "both connected");

        goodDoc.GetText("body").Insert(0, "fine ");
        await AssertEventuallyAsync(() => badDoc.GetText("body").ToString() == "fine ", "an accepted change goes through");

        badDoc.GetText("body").Insert(5, "forbidden");

        await AssertEventuallyAsync(() => denied == 1, "the sender should be refused");
        Assert.AreEqual(BcrdtConnectionStatus.Faulted, bad.Status);
        await Task.Delay(QuietPeriod);
        Assert.AreEqual("fine ", goodDoc.GetText("body").ToString());
        Assert.AreEqual("fine ", await documentHost.ReadAsync("rules", doc => doc.GetText("body").ToString()));
    }

    [TestMethod]
    public async Task With_a_host_a_joiner_is_answered_by_the_host_alone()
    {
        await using var documentHost = new BcrdtDocumentHost();
        await documentHost.EditAsync("quiet", doc => doc.GetText("body").Insert(0, "abc"));
        await using LoopbackHost host = await HostedWebSocketAsync(documentHost);

        using ClientWebSocket bystander = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/quiet");
        using ClientWebSocket joiner = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/quiet");
        Task<BcrdtSyncMessage?> bystanderHears = RawSocket.ReceiveAsync(bystander, QuietPeriod * 2);

        await joiner.SendAsync(BcrdtSyncProtocol.EncodeSyncStep1(new BcrdtStateVector()), WebSocketMessageType.Binary, true, CancellationToken.None);

        BcrdtSyncMessage? first = await RawSocket.ReceiveAsync(joiner, Patience);
        BcrdtSyncMessage? second = await RawSocket.ReceiveAsync(joiner, Patience);
        Assert.AreEqual(BcrdtSyncMessageKind.SyncStep1Reply, first?.Kind);
        Assert.AreEqual(BcrdtSyncMessageKind.SyncStep2, second?.Kind);

        using var check = new BcrdtDocument();
        check.ApplyUpdate(second!.Value.Payload);
        Assert.AreEqual("abc", check.GetText("body").ToString());

        Assert.IsNull(await bystanderHears, "the other clients need not hear a question the host answered");
    }

    [TestMethod]
    public async Task With_a_host_presence_cannot_be_published_under_someone_elses_identity()
    {
        await using var documentHost = new BcrdtDocumentHost();
        await using LoopbackHost host = await HostedWebSocketAsync(documentHost);

        using ClientWebSocket ada = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/presence");
        using ClientWebSocket mallory = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/presence");
        using ClientWebSocket observer = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/presence");

        using var adaPresence = new BcrdtAwarenessStore(10);
        byte[]? adaUpdate = null;
        adaPresence.Updated += update => adaUpdate = update;
        adaPresence.SetLocalField("name", "Ada");
        await ada.SendAsync(BcrdtSyncProtocol.EncodeAwarenessUpdate(adaUpdate!), WebSocketMessageType.Binary, true, CancellationToken.None);

        var observed = new BcrdtAwarenessStore(99);
        BcrdtSyncMessage? heard = await RawSocket.ReceiveAsync(observer, Patience);
        Assert.AreEqual(BcrdtSyncMessageKind.AwarenessUpdate, heard?.Kind);
        observed.ApplyUpdate(heard!.Value.Payload);
        Assert.AreEqual("Ada", observed.GetField(10, "name"));

        // The same identity, a later clock, a different name - from another connection.
        using var forger = new BcrdtAwarenessStore(10);
        byte[]? forged = null;
        forger.Updated += update => forged = update;
        for (int i = 0; i < 5; i++) forger.SetLocalField("name", "Mallory");
        await mallory.SendAsync(BcrdtSyncProtocol.EncodeAwarenessUpdate(forged!), WebSocketMessageType.Binary, true, CancellationToken.None);
        Assert.IsNull(await RawSocket.ReceiveAsync(observer, QuietPeriod), "the forgery must not be relayed");

        // When Ada's connection goes without a goodbye, the host says it for her.
        ada.Abort();
        heard = await RawSocket.ReceiveAsync(observer, Patience);
        Assert.AreEqual(BcrdtSyncMessageKind.AwarenessUpdate, heard?.Kind);
        observed.ApplyUpdate(heard!.Value.Payload);
        Assert.DoesNotContain(10UL, observed.OnlineClients);
    }

    [TestMethod]
    public async Task With_a_host_a_malformed_message_is_not_relayed()
    {
        await using var documentHost = new BcrdtDocumentHost();
        await using LoopbackHost host = await HostedWebSocketAsync(documentHost);

        using ClientWebSocket sender = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/garbage");
        using ClientWebSocket receiver = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/garbage");

        await sender.SendAsync(BcrdtSyncProtocol.EncodeUpdate([0xFF, 0xFF, 0xFF]), WebSocketMessageType.Binary, true, CancellationToken.None);
        Assert.IsNull(await RawSocket.ReceiveAsync(receiver, QuietPeriod));

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 5 });
        doc.GetText("body").Insert(0, "ok");
        await sender.SendAsync(BcrdtSyncProtocol.EncodeUpdate(doc.EncodeStateAsUpdate()), WebSocketMessageType.Binary, true, CancellationToken.None);
        Assert.AreEqual(BcrdtSyncMessageKind.Update, (await RawSocket.ReceiveAsync(receiver, Patience))?.Kind);
    }

    [TestMethod]
    public async Task The_host_asks_once_for_what_a_change_builds_on_and_does_not_keep_asking()
    {
        await using var documentHost = new BcrdtDocumentHost();
        await using LoopbackHost host = await HostedWebSocketAsync(documentHost);
        using ClientWebSocket client = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/pending");

        // A change by client 7 at clock 5: clocks 0-4 never arrive, so it waits for ever.
        using (var author = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 7 }))
        {
            author.GetText("body").Insert(0, "abcde");
            BcrdtStateVector before = author.GetStateVector();
            author.GetText("body").Insert(5, "f");
            byte[] orphan = author.EncodeStateAsUpdate(before);
            await client.SendAsync(BcrdtSyncProtocol.EncodeUpdate(orphan), WebSocketMessageType.Binary, true, CancellationToken.None);
        }

        // Asked once for the rest.
        Assert.AreEqual(BcrdtSyncMessageKind.SyncStep1, (await RawSocket.ReceiveAsync(client, Patience))?.Kind);

        // The client answers with what it has - not what is missing - and the host does not ask again.
        await client.SendAsync(BcrdtSyncProtocol.EncodeSyncStep2(BcrdtUpdateEncoder.Empty), WebSocketMessageType.Binary, true, CancellationToken.None);
        Assert.IsNull(await RawSocket.ReceiveAsync(client, QuietPeriod), "an answer must not prompt another question");

        // Nor does an unrelated change, while the old one is still waiting.
        using var other = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 11 });
        other.GetText("body").Insert(0, "x");
        await client.SendAsync(BcrdtSyncProtocol.EncodeUpdate(other.EncodeStateAsUpdate()), WebSocketMessageType.Binary, true, CancellationToken.None);
        Assert.IsNull(await RawSocket.ReceiveAsync(client, QuietPeriod), "nothing new is waiting");
    }

    [TestMethod]
    public async Task A_reconnecting_client_takes_its_presence_back_from_its_silent_old_connection()
    {
        await using var documentHost = new BcrdtDocumentHost(new BcrdtDocumentHostOptions { PresenceTakeoverAfter = TimeSpan.FromMilliseconds(200) });
        await using LoopbackHost host = await HostedWebSocketAsync(documentHost);

        using ClientWebSocket stale = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/takeover");
        using ClientWebSocket observer = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/takeover");

        using var presence = new BcrdtAwarenessStore(42);
        byte[]? update = null;
        presence.Updated += published => update = published;
        presence.SetLocalField("name", "Ada");
        await stale.SendAsync(BcrdtSyncProtocol.EncodeAwarenessUpdate(update!), WebSocketMessageType.Binary, true, CancellationToken.None);
        Assert.AreEqual(BcrdtSyncMessageKind.AwarenessUpdate, (await RawSocket.ReceiveAsync(observer, Patience))?.Kind);

        // The old connection goes quiet without closing - a network blip the server has not noticed -
        // and the same client comes back on a new one.
        using ClientWebSocket fresh = await RawSocket.ConnectAsync(host.WebSocketBase + "/ws/takeover");
        await Task.Delay(400);
        presence.SetLocalField("name", "Ada again");
        await fresh.SendAsync(BcrdtSyncProtocol.EncodeAwarenessUpdate(update!), WebSocketMessageType.Binary, true, CancellationToken.None);

        BcrdtSyncMessage? heard = await RawSocket.ReceiveAsync(observer, Patience);
        Assert.AreEqual(BcrdtSyncMessageKind.AwarenessUpdate, heard?.Kind, "the client's presence must not be dropped as a forgery");
        var observed = new BcrdtAwarenessStore(99);
        observed.ApplyUpdate(heard!.Value.Payload);
        Assert.AreEqual("Ada again", observed.GetField(42, "name"));
    }

    [TestMethod]
    public async Task One_host_behind_both_relays_carries_changes_between_their_clients()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app =>
            {
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

        using var signalRDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var socketDoc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using var signalR = new BcrdtSignalRTransport(host.BaseUrl + "/hub", "shared");
        await using var socket = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/shared"));
        await using BcrdtSyncProvider overSignalR = await ProviderAsync(signalRDoc, signalR);
        await using BcrdtSyncProvider overSocket = await ProviderAsync(socketDoc, socket);
        await overSignalR.ConnectAsync();
        await overSocket.ConnectAsync();
        await AssertEventuallyAsync(() => overSignalR.IsSynced && overSocket.IsSynced, "both connected");

        signalRDoc.GetText("body").Insert(0, "from SignalR. ");
        await AssertEventuallyAsync(() => socketDoc.GetText("body").ToString() == "from SignalR. ", "the WebSocket client should see it live");

        socketDoc.GetText("body").Insert(14, "From WebSocket.");
        await AssertEventuallyAsync(() => signalRDoc.GetText("body").ToString() == "from SignalR. From WebSocket.", "and the SignalR client the other way");
    }

    [TestMethod]
    public async Task A_hub_with_a_registered_host_keeps_documents_after_everyone_leaves()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app => app.MapHub<BcrdtHub>("/hub"),
            builder =>
            {
                builder.Services.AddBitBcrdtHub();
                builder.Services.AddBitBcrdtDocumentHost();
            });

        using (var author = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        {
            await using var transport = new BcrdtSignalRTransport(host.BaseUrl + "/hub", "notes");
            await using BcrdtSyncProvider provider = await ProviderAsync(author, transport);
            await provider.ConnectAsync();
            await AssertEventuallyAsync(() => provider.IsSynced, "the host answers the handshake");
            author.GetText("body").Insert(0, "kept");

            BcrdtDocumentHost documentHost = host.App.Services.GetRequiredService<BcrdtDocumentHost>();
            await AssertEventuallyAsync(
                async () => await documentHost.ReadAsync("notes", doc => doc.GetText("body").ToString()) == "kept",
                "the host should hold the change");
        }

        using var newcomer = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using var newcomerTransport = new BcrdtSignalRTransport(host.BaseUrl + "/hub", "notes");
        await using BcrdtSyncProvider newcomerProvider = await ProviderAsync(newcomer, newcomerTransport);
        await newcomerProvider.ConnectAsync();

        await AssertEventuallyAsync(() => newcomer.GetText("body").ToString() == "kept", "the host should hand the document over");
        await AssertEventuallyAsync(() => newcomerProvider.IsSynced, "and the newcomer should be synced");
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static ValueTask<BcrdtDocumentAccess> AccessFromQuery(HttpContext context, string documentId) =>
        ValueTask.FromResult(context.Request.Query["role"].ToString() == "reader" ? BcrdtDocumentAccess.ReadOnly : BcrdtDocumentAccess.ReadWrite);

    private static Task<LoopbackHost> HostedWebSocketAsync(BcrdtDocumentHost documentHost) =>
        LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket(
            "/ws/{documentId}",
            options: new BcrdtWebSocketEndpointOptions { DocumentHost = documentHost }));

    /// <summary>A participant in an in-memory room that speaks raw protocol, with no provider.</summary>
    private sealed class RawPeer
    {
        private readonly BcrdtInMemoryTransport _transport;

        private RawPeer(BcrdtInMemoryTransport transport)
        {
            _transport = transport;
            transport.MessageReceived += message => Received.Enqueue(BcrdtSyncProtocol.Decode(message.ToArray()));
        }

        public ConcurrentQueue<BcrdtSyncMessage> Received { get; } = new();

        public static async Task<RawPeer> JoinAsync(BcrdtInMemoryNetwork network, string room)
        {
            BcrdtInMemoryTransport transport = network.CreateTransport(room);
            var peer = new RawPeer(transport);
            await transport.ConnectAsync();
            return peer;
        }

        public ValueTask SendAsync(byte[] message) => _transport.SendAsync(message);
    }

    /// <summary>Bare sockets against a relay.</summary>
    private static class RawSocket
    {
        /// <summary>Connects and reads past the access grant every relay sends first.</summary>
        public static async Task<ClientWebSocket> ConnectAsync(string address, BcrdtDocumentAccess expected = BcrdtDocumentAccess.ReadWrite)
        {
            var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(address), CancellationToken.None);
            BcrdtSyncMessage? grant = await ReceiveAsync(socket, Patience);
            Assert.AreEqual(BcrdtSyncMessageKind.Auth, grant?.Kind);
            Assert.AreEqual(expected, BcrdtSyncProtocol.DecodeAuthAccess(grant!.Value.Payload).Access);
            return socket;
        }

        // A receive that outlived its caller's patience is still waiting on the socket; the next call
        // picks it up rather than starting a second one, which would both fail and lose a message.
        private static readonly ConditionalWeakTable<ClientWebSocket, Pending> Outstanding = [];

        private sealed record Pending(Task<WebSocketReceiveResult> Receive, byte[] Buffer);

        /// <summary>The next message, or null if none arrives in time. Never cancels the receive itself.</summary>
        public static async Task<BcrdtSyncMessage?> ReceiveAsync(ClientWebSocket socket, TimeSpan limit)
        {
            if (!Outstanding.TryGetValue(socket, out Pending? pending))
            {
                byte[] buffer = new byte[1 << 16];
                pending = new Pending(socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None), buffer);
                Outstanding.AddOrUpdate(socket, pending);
            }

            if (await Task.WhenAny(pending.Receive, Task.Delay(limit)) != pending.Receive) return null;
            Outstanding.Remove(socket);

            WebSocketReceiveResult result = await pending.Receive;
            if (result.MessageType == WebSocketMessageType.Close) return null;
            return BcrdtSyncProtocol.Decode(pending.Buffer.AsMemory(0, result.Count).ToArray());
        }
    }

    /// <summary>A transport whose every connection is lost the moment it is made.</summary>
    private sealed class FlappingTransport : IBcrdtTransport
    {
        private int _state = (int)BcrdtTransportState.Disconnected;
        private int _connectCalls;

        public int ConnectCalls => Volatile.Read(ref _connectCalls);

        public BcrdtTransportState State => (BcrdtTransportState)Volatile.Read(ref _state);

        public string Description => "flapping";

        public event Action<ReadOnlyMemory<byte>>? MessageReceived { add { } remove { } }

        public event Action<BcrdtTransportStateChange>? StateChanged;

        public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _connectCalls);
            Raise(BcrdtTransportState.Connected, null);
            _ = Task.Run(() => Raise(BcrdtTransportState.Faulted, new IOException("dropped")));
            return ValueTask.CompletedTask;
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            Raise(BcrdtTransportState.Disconnected, null);
            return ValueTask.CompletedTask;
        }

        private void Raise(BcrdtTransportState state, Exception? error)
        {
            if (Interlocked.Exchange(ref _state, (int)state) == (int)state) return;
            StateChanged?.Invoke(new BcrdtTransportStateChange(state, error));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

/// <summary>A hub that makes a connection read-only when it asks with <c>?role=reader</c>.</summary>
public sealed class ReaderAwareHub : BcrdtHub
{
    protected override ValueTask<BcrdtDocumentAccess> AuthorizeAccessAsync(string documentId) =>
        ValueTask.FromResult(Context.GetHttpContext()?.Request.Query["role"].ToString() == "reader"
            ? BcrdtDocumentAccess.ReadOnly
            : BcrdtDocumentAccess.ReadWrite);
}
