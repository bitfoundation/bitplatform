using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using Bit.Bcrdt.Server;
using Bit.Bcrdt.SignalR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Regression tests for the synchronization, transport and testing layers: one or more per defect
/// found in review, each failing against the code as it was before the fix.
/// </summary>
/// <remarks>
/// Waits poll a condition with a generous ceiling rather than sleeping a fixed time. The few
/// assertions that something did <i>not</i> happen necessarily wait a short, fixed interval after
/// a positive signal that the thing would have happened by then.
/// </remarks>
[TestClass]
public class SyncRegressionTests
{
    [TestMethod]
    [DataRow("/hub/collaboration")]
    [DataRow("hub/collaboration")]
    [DataRow("ws://localhost/hub")]
    public void A_hub_url_the_signalr_client_cannot_use_is_rejected_when_configured(string hubUrl)
    {
        // The client needs an absolute http(s) URL. Without the early check a relative one surfaced
        // as a UriFormatException from deep inside SignalR on the first connection attempt.
        Assert.ThrowsExactly<ArgumentException>(() => new BcrdtSignalRTransportFactory(hubUrl));
        Assert.ThrowsExactly<ArgumentException>(() => new BcrdtSignalRTransport(hubUrl, "doc"));
        _ = new BcrdtSignalRTransportFactory("https://localhost/hub/collaboration");
    }

    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);

    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(300);

    // ---------------------------------------------------------------------------------------------
    // BcrdtSyncProvider: threading
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Inbound_messages_and_status_events_run_on_the_providers_synchronization_context()
    {
        using var context = new SingleThreadContext();
        var network = new BcrdtInMemoryNetwork();
        using var docA = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var docB = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        ConcurrentBag<int> applyThreads = [];
        ConcurrentBag<int> statusThreads = [];
        docA.Updated += args =>
        {
            if (args.Origin is BcrdtSyncProvider) applyThreads.Add(Environment.CurrentManagedThreadId);
        };

        BcrdtSyncProvider providerA = await context.InvokeAsync(() =>
        {
            var provider = new BcrdtSyncProvider(docA, network.CreateTransport("room"), null, Options());
            provider.StatusChanged += _ => statusThreads.Add(Environment.CurrentManagedThreadId);
            return provider;
        });
        await context.InvokeAsync(() => providerA.ConnectAsync().AsTask());

        // B lives on the thread pool, with no context at all, and edits from there.
        BcrdtSyncProvider providerB = await Task.Run(() => new BcrdtSyncProvider(docB, network.CreateTransport("room"), null, Options()));
        await Task.Run(() => providerB.ConnectAsync().AsTask());
        await Task.Run(() => docB.GetText("t").Insert(0, "from B"));

        await AssertEventuallyAsync(
            async () => await context.InvokeAsync(() => docA.GetText("t").ToString()) == "from B",
            "the edit should reach A");

        Assert.IsNotEmpty(applyThreads);
        foreach (int thread in applyThreads) Assert.AreEqual(context.ThreadId, thread);
        Assert.IsNotEmpty(statusThreads);
        foreach (int thread in statusThreads) Assert.AreEqual(context.ThreadId, thread);

        await providerB.DisposeAsync();
        await context.InvokeAsync(() => providerA.DisposeAsync().AsTask());
    }

    // ---------------------------------------------------------------------------------------------
    // BcrdtSyncProvider: reconnection and cancellation
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_reconnect_attempt_that_throws_OperationCanceledException_is_retried()
    {
        // SignalR reports a handshake timeout as an OperationCanceledException nobody asked for.
        var transport = new ScriptedTransport
        {
            OnConnect = (attempt, _) => attempt <= 3
                ? Task.FromException(new OperationCanceledException("The handshake timed out."))
                : Task.CompletedTask,
        };
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);

        await provider.ConnectAsync();

        await AssertEventuallyAsync(
            () => transport.State == BcrdtTransportState.Connected,
            "the provider should keep retrying past transport-raised cancellations");
        Assert.AreEqual(4, transport.ConnectCalls);
    }

    [TestMethod]
    public async Task Cancelling_ConnectAsync_throws_and_leaves_the_provider_disconnected()
    {
        var transport = new ScriptedTransport { OnConnect = (_, token) => Task.Delay(Timeout.Infinite, token) };
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAsync<OperationCanceledException>(() => provider.ConnectAsync(cancellation.Token).AsTask());

        Assert.AreEqual(BcrdtConnectionStatus.Disconnected, provider.Status);
        await Task.Delay(QuietPeriod);
        Assert.AreEqual(1, transport.ConnectCalls);
    }

    [TestMethod]
    public async Task A_transport_refusal_is_final_and_reported()
    {
        var transport = new ScriptedTransport
        {
            OnConnect = (_, _) => Task.FromException(new BcrdtTransportAuthorizationException("No entry.")),
        };
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);
        int denials = 0;
        provider.AuthorizationDenied += _ => Interlocked.Increment(ref denials);

        await provider.ConnectAsync();

        await AssertEventuallyAsync(() => denials == 1, "the refusal should be reported");
        Assert.AreEqual(BcrdtConnectionStatus.Faulted, provider.Status);
        await Task.Delay(QuietPeriod);
        Assert.AreEqual(1, transport.ConnectCalls);
        Assert.AreEqual(1, denials);
    }

    [TestMethod]
    public async Task A_drop_with_reconnection_disabled_reports_disconnected()
    {
        var network = new BcrdtInMemoryNetwork();
        BcrdtInMemoryTransport transport = network.CreateTransport("room");
        await using BcrdtSyncProvider provider = await CreateProviderAsync(
            new BcrdtDocument(), transport, options: Options(handshakeTimeout: TimeSpan.FromMilliseconds(10), reconnect: BcrdtReconnectPolicy.None));

        await provider.ConnectAsync();
        await AssertEventuallyAsync(() => provider.Status == BcrdtConnectionStatus.Synced, "a lone replica should settle");

        transport.SimulateDrop();

        await AssertEventuallyAsync(
            () => provider.Status == BcrdtConnectionStatus.Disconnected,
            "a dropped connection must not go on reporting Synced");
    }

    [TestMethod]
    public async Task Disconnecting_cancels_a_pending_reconnect_attempt()
    {
        int cancelled = 0;
        var transport = new ScriptedTransport
        {
            OnConnect = async (attempt, token) =>
            {
                if (attempt == 1) return;
                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelled);
                    throw;
                }
            },
        };
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);

        await provider.ConnectAsync();
        transport.Raise(BcrdtTransportState.Faulted, new IOException("Dropped."));
        await AssertEventuallyAsync(() => transport.ConnectCalls == 2, "a reconnect attempt should be under way");

        await provider.DisconnectAsync();

        await AssertEventuallyAsync(() => cancelled == 1, "the pending attempt should be cancelled");
        Assert.AreEqual(BcrdtConnectionStatus.Disconnected, provider.Status);
        await Task.Delay(QuietPeriod);
        Assert.AreNotEqual(BcrdtTransportState.Connected, transport.State);
        Assert.AreEqual(2, transport.ConnectCalls);
    }

    [TestMethod]
    public async Task One_loss_reported_twice_starts_a_single_reconnect_loop()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new ScriptedTransport
        {
            OnConnect = (attempt, token) => attempt == 1 ? Task.CompletedTask : release.Task.WaitAsync(token),
        };
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);

        await provider.ConnectAsync();
        transport.Raise(BcrdtTransportState.Faulted, new IOException("Send failed."));
        transport.Raise(BcrdtTransportState.Disconnected);

        await AssertEventuallyAsync(() => transport.ConnectCalls == 2, "a reconnect attempt should start");
        await Task.Delay(QuietPeriod);

        Assert.AreEqual(2, transport.ConnectCalls);
        Assert.AreEqual(1, transport.MaxConcurrentConnects);
        release.TrySetResult();
    }

    // ---------------------------------------------------------------------------------------------
    // BcrdtSyncProvider: status and the handshake
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_refusal_from_the_peer_leaves_the_status_faulted_and_drops_the_connection()
    {
        var network = new BcrdtInMemoryNetwork();
        RawPeer peer = await RawPeer.JoinAsync(network, "room");
        BcrdtInMemoryTransport transport = network.CreateTransport("room");
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);
        string? reason = null;
        provider.AuthorizationDenied += denied => reason = denied;

        await provider.ConnectAsync();
        await peer.SendAsync(BcrdtSyncProtocol.EncodeAuth(false, "Not you."));

        await AssertEventuallyAsync(
            () => reason == "Not you." && transport.State == BcrdtTransportState.Disconnected,
            "the refusal should be reported and the connection closed");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual(BcrdtConnectionStatus.Faulted, provider.Status);
    }

    [TestMethod]
    public async Task Connecting_an_already_connected_provider_does_not_lose_its_status()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtSyncProvider provider = await CreateProviderAsync(
            new BcrdtDocument(), network.CreateTransport("room"), options: Options(handshakeTimeout: TimeSpan.FromMilliseconds(10)));

        await provider.ConnectAsync();
        await AssertEventuallyAsync(() => provider.Status == BcrdtConnectionStatus.Synced, "a lone replica should settle");

        await provider.ConnectAsync();

        Assert.AreEqual(BcrdtConnectionStatus.Synced, provider.Status);
    }

    [TestMethod]
    public async Task A_provider_over_an_already_connected_transport_runs_the_handshake()
    {
        var network = new BcrdtInMemoryNetwork();
        RawPeer peer = await RawPeer.JoinAsync(network, "room");
        BcrdtInMemoryTransport transport = network.CreateTransport("room");
        await transport.ConnectAsync();

        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);
        await provider.ConnectAsync();

        await AssertEventuallyAsync(
            () => peer.Received.Any(message => message.Kind == BcrdtSyncMessageKind.SyncStep1),
            "the handshake should open even though nothing announced the connection");
        Assert.AreEqual(BcrdtConnectionStatus.Handshaking, provider.Status);
    }

    [TestMethod]
    public async Task A_replica_alone_in_a_room_reaches_synced_once_nobody_answers()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtSyncProvider provider = await CreateProviderAsync(
            new BcrdtDocument(), network.CreateTransport("room"), options: Options(handshakeTimeout: TimeSpan.FromMilliseconds(50)));
        int synced = 0;
        provider.Synced += () => Interlocked.Increment(ref synced);

        await provider.ConnectAsync();

        await AssertEventuallyAsync(() => provider.IsSynced && synced == 1, "nobody to be behind means synced");
    }

    [TestMethod]
    public async Task A_late_joiner_is_synced_by_the_peers_reply()
    {
        var network = new BcrdtInMemoryNetwork();
        using var docA = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        docA.GetText("body").Insert(0, "already here");
        await using BcrdtSyncProvider providerA = await CreateProviderAsync(docA, network.CreateTransport("room"));
        await providerA.ConnectAsync();

        using var docB = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using BcrdtSyncProvider providerB = await CreateProviderAsync(docB, network.CreateTransport("room"));
        await providerB.ConnectAsync();

        // The handshake timeout is minutes: only the reply can have done this.
        await AssertEventuallyAsync(() => providerB.IsSynced, "the reply proves B holds everything A does");
        Assert.AreEqual("already here", docB.GetText("body").ToString());
    }

    [TestMethod]
    public async Task Relayed_updates_and_other_peers_deltas_do_not_mark_a_replica_synced()
    {
        var network = new BcrdtInMemoryNetwork();
        RawPeer peer = await RawPeer.JoinAsync(network, "room");
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using BcrdtSyncProvider provider = await CreateProviderAsync(doc, network.CreateTransport("room"));
        await provider.ConnectAsync();

        using var elsewhere = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 7 });
        elsewhere.GetText("t").Insert(0, "abc");
        await peer.SendAsync(BcrdtSyncProtocol.EncodeUpdate(elsewhere.EncodeStateAsUpdate()));
        elsewhere.GetText("t").Insert(3, "def");
        await peer.SendAsync(BcrdtSyncProtocol.EncodeSyncStep2(elsewhere.EncodeStateAsUpdate()));

        await AssertEventuallyAsync(() => doc.GetText("t").ToString() == "abcdef", "both messages should be applied");
        Assert.AreEqual(BcrdtConnectionStatus.Handshaking, provider.Status);

        // A reply stating what the peer holds - all of which this replica now has - is what counts.
        await peer.SendAsync(BcrdtSyncProtocol.EncodeSyncStep1Reply(elsewhere.GetStateVector(), BcrdtSyncExchange.ComputeDeletionDigest(elsewhere)));
        await AssertEventuallyAsync(() => provider.IsSynced, "a covered reply completes the handshake");
    }

    [TestMethod]
    public async Task Offline_deletions_reach_the_room_on_reconnect()
    {
        var network = new BcrdtInMemoryNetwork();
        using var docA = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var docB = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using BcrdtSyncProvider providerA = await CreateProviderAsync(docA, network.CreateTransport("room"));
        await using BcrdtSyncProvider providerB = await CreateProviderAsync(docB, network.CreateTransport("room"));
        await providerA.ConnectAsync();
        await providerB.ConnectAsync();

        docA.GetText("body").Insert(0, "hello");
        await AssertEventuallyAsync(() => docB.GetText("body").ToString() == "hello", "the insert should arrive");

        await providerA.DisconnectAsync();

        // Deletions only: the state vector does not move, so it cannot reveal them.
        docA.GetText("body").Delete(1, 3);
        Assert.AreEqual("ho", docA.GetText("body").ToString());

        await providerA.ConnectAsync();

        await AssertEventuallyAsync(() => docB.GetText("body").ToString() == "ho", "the offline deletion should be collected");
        BcrdtConvergenceHarness.AssertStructurallyIdentical(docA, docB);
    }

    [TestMethod]
    public void A_handshake_reply_is_answered_with_a_delta_only_and_only_when_one_is_needed()
    {
        using var holder = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var joiner = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        holder.GetText("t").Insert(0, "abc");

        List<byte[]> fromHolder = [];
        BcrdtSyncExchange.Handle(holder, BcrdtSyncProtocol.Decode(BcrdtSyncExchange.CreateAnnouncement(joiner)), "holder", fromHolder);
        Assert.AreSequenceEqual(
            [BcrdtSyncMessageKind.SyncStep2, BcrdtSyncMessageKind.SyncStep1Reply],
            fromHolder.Select(message => BcrdtSyncProtocol.Decode(message).Kind));

        // Applied in order, the joiner holds everything the holder does: nothing is owed back.
        List<byte[]> fromJoiner = [];
        foreach (byte[] message in fromHolder)
        {
            BcrdtSyncExchange.Handle(joiner, BcrdtSyncProtocol.Decode(message), "joiner", fromJoiner);
        }
        Assert.IsEmpty(fromJoiner);

        // A deletion the holder lacks changes nothing a state vector can see, but the digest shows it.
        joiner.GetText("t").Delete(0, 1);
        BcrdtSyncExchange.Handle(joiner, BcrdtSyncProtocol.Decode(fromHolder[1]), "joiner", fromJoiner);
        Assert.AreSequenceEqual([BcrdtSyncMessageKind.SyncStep2], fromJoiner.Select(message => BcrdtSyncProtocol.Decode(message).Kind));
    }

    // ---------------------------------------------------------------------------------------------
    // BcrdtSyncProvider: heartbeat and presence
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task The_heartbeat_survives_a_throwing_presence_subscriber()
    {
        var network = new BcrdtInMemoryNetwork();
        using var awareness = new BcrdtAwarenessStore(1);
        awareness.SetLocalState(new Dictionary<string, object?> { ["name"] = "Ada" });

        // A heartbeat republishes the local entry without changing it, so it raises Updated but not
        // Changed; the subscriber that throws therefore listens to Updated. It is armed only once the
        // connection is up, so the failure lands on a heartbeat tick rather than on the handshake.
        int armed = 0;
        int failures = 0;
        int republished = 0;
        awareness.Updated += _ =>
        {
            Interlocked.Increment(ref republished);
            if (Volatile.Read(ref armed) == 1 && Interlocked.Increment(ref failures) == 1)
            {
                throw new InvalidOperationException("Subscriber failure.");
            }
        };

        await using BcrdtSyncProvider provider = await CreateProviderAsync(
            new BcrdtDocument(), network.CreateTransport("room"), awareness, Options(heartbeat: TimeSpan.FromMilliseconds(20)));
        int errors = 0;
        provider.Error += _ => Interlocked.Increment(ref errors);
        await provider.ConnectAsync();

        int republishedBefore = Volatile.Read(ref republished);
        Volatile.Write(ref armed, 1);

        await AssertEventuallyAsync(
            () => errors >= 1 && republished >= republishedBefore + 3,
            "later ticks should keep republishing");
    }

    [TestMethod]
    public async Task The_heartbeat_does_not_broadcast_pings()
    {
        var network = new BcrdtInMemoryNetwork();
        ConcurrentBag<BcrdtSyncMessageKind> kinds = [];
        network.MessageSent += (_, message) => kinds.Add(BcrdtSyncProtocol.Decode(message.ToArray()).Kind);

        using var awareness = new BcrdtAwarenessStore(1);
        awareness.SetLocalState(new Dictionary<string, object?> { ["name"] = "Ada" });
        int republished = 0;
        awareness.Updated += _ => Interlocked.Increment(ref republished);

        RawPeer peer = await RawPeer.JoinAsync(network, "room");
        await using BcrdtSyncProvider provider = await CreateProviderAsync(
            new BcrdtDocument(), network.CreateTransport("room"), awareness, Options(heartbeat: TimeSpan.FromMilliseconds(20)));
        await provider.ConnectAsync();

        await AssertEventuallyAsync(() => republished >= 3, "several heartbeats should pass");
        Assert.DoesNotContain(BcrdtSyncMessageKind.Ping, kinds);
    }

    [TestMethod]
    public async Task A_deliberate_disconnect_hides_presence_and_reconnecting_restores_it()
    {
        var network = new BcrdtInMemoryNetwork();
        using var awarenessA = new BcrdtAwarenessStore(1);
        using var awarenessB = new BcrdtAwarenessStore(2);
        awarenessA.SetLocalState(new Dictionary<string, object?> { ["name"] = "Ada" });

        await using BcrdtSyncProvider providerA = await CreateProviderAsync(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }), network.CreateTransport("room"), awarenessA);
        await using BcrdtSyncProvider providerB = await CreateProviderAsync(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 }), network.CreateTransport("room"), awarenessB);
        await providerB.ConnectAsync();
        await providerA.ConnectAsync();
        await AssertEventuallyAsync(() => Equals(awarenessB.GetField(1, "name"), "Ada"), "B should see Ada");

        await providerA.DisconnectAsync();
        await AssertEventuallyAsync(() => awarenessB.GetField(1, "name") is null, "B should see Ada leave");

        await providerA.ConnectAsync();
        await AssertEventuallyAsync(() => Equals(awarenessB.GetField(1, "name"), "Ada"), "Ada should be back, name and all");
        Assert.IsNotNull(awarenessA.LocalState);
    }

    [TestMethod]
    public async Task Disposing_a_connected_provider_announces_departure()
    {
        var network = new BcrdtInMemoryNetwork();
        using var awarenessA = new BcrdtAwarenessStore(1);
        using var awarenessB = new BcrdtAwarenessStore(2);
        awarenessA.SetLocalState(new Dictionary<string, object?> { ["name"] = "Ada" });

        BcrdtSyncProvider providerA = await CreateProviderAsync(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }), network.CreateTransport("room"), awarenessA);
        await using BcrdtSyncProvider providerB = await CreateProviderAsync(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 }), network.CreateTransport("room"), awarenessB);
        await providerB.ConnectAsync();
        await providerA.ConnectAsync();
        await AssertEventuallyAsync(() => Equals(awarenessB.GetField(1, "name"), "Ada"), "B should see Ada");

        await providerA.DisposeAsync();

        await AssertEventuallyAsync(() => awarenessB.GetField(1, "name") is null, "B should not be left with a ghost");
    }

    [TestMethod]
    public async Task Awareness_queries_are_answered_with_the_local_entry_only()
    {
        var network = new BcrdtInMemoryNetwork();
        using var awareness = new BcrdtAwarenessStore(1);
        awareness.SetLocalState(new Dictionary<string, object?> { ["name"] = "Ada" });

        // A participant this replica merely heard about - and that may long since have gone.
        using var ghost = new BcrdtAwarenessStore(99);
        ghost.SetLocalState(new Dictionary<string, object?> { ["name"] = "Ghost" });
        awareness.ApplyUpdate(ghost.EncodeAll());

        RawPeer peer = await RawPeer.JoinAsync(network, "room");
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), network.CreateTransport("room"), awareness);
        await provider.ConnectAsync();
        await AssertEventuallyAsync(() => peer.Received.Any(message => message.Kind == BcrdtSyncMessageKind.AwarenessQuery), "the handshake should settle");
        peer.Received.Clear();

        await peer.SendAsync(BcrdtSyncProtocol.EncodeAwarenessQuery());

        await AssertEventuallyAsync(() => peer.Received.Any(message => message.Kind == BcrdtSyncMessageKind.AwarenessUpdate), "the query should be answered");
        using var decoded = new BcrdtAwarenessStore(12345);
        foreach (BcrdtSyncMessage message in peer.Received.Where(message => message.Kind == BcrdtSyncMessageKind.AwarenessUpdate))
        {
            decoded.ApplyUpdate(message.Payload);
        }
        Assert.AreSequenceEqual([1UL], decoded.Entries.Keys.Order());
    }

    [TestMethod]
    public async Task With_awareness_sync_off_remote_presence_is_not_applied()
    {
        var network = new BcrdtInMemoryNetwork();
        using var awareness = new BcrdtAwarenessStore(1);
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        RawPeer peer = await RawPeer.JoinAsync(network, "room");
        await using BcrdtSyncProvider provider = await CreateProviderAsync(doc, network.CreateTransport("room"), awareness, Options(syncAwareness: false));
        await provider.ConnectAsync();

        using var remote = new BcrdtAwarenessStore(77);
        remote.SetLocalState(new Dictionary<string, object?> { ["name"] = "Remote" });
        await peer.SendAsync(BcrdtSyncProtocol.EncodeAwarenessUpdate(remote.EncodeAll()));

        // A sentinel after it, so that once the sentinel lands the presence message has been handled.
        using var sentinel = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 9 });
        sentinel.GetMap("m").Set("k", 1L);
        await peer.SendAsync(BcrdtSyncProtocol.EncodeUpdate(sentinel.EncodeStateAsUpdate()));

        await AssertEventuallyAsync(() => doc.GetMap("m").ContainsKey("k"), "the sentinel should arrive");
        Assert.IsFalse(awareness.Entries.ContainsKey(77));
    }

    [TestMethod]
    public void A_reconnect_policy_is_safe_to_share_between_threads()
    {
        var policy = new BcrdtReconnectPolicy { BaseDelay = TimeSpan.FromMilliseconds(100), MaxDelay = TimeSpan.FromSeconds(5) };
        ConcurrentBag<TimeSpan> delays = [];

        Parallel.For(0, 20_000, _ => delays.Add(policy.NextDelay(6)));

        foreach (TimeSpan delay in delays) Assert.IsInRange(TimeSpan.Zero, policy.MaxDelay, delay);
        Assert.IsTrue(delays.Distinct().Count() > 1000, "Concurrent use should still jitter.");
    }

    // ---------------------------------------------------------------------------------------------
    // In-memory transport
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task An_in_memory_room_is_retired_when_its_last_member_leaves()
    {
        var network = new BcrdtInMemoryNetwork();
        BcrdtInMemoryTransport first = network.CreateTransport("room");
        BcrdtInMemoryTransport second = network.CreateTransport("room");
        await first.ConnectAsync();
        await second.ConnectAsync();
        Assert.AreEqual(1, network.RoomCount);

        await first.DisconnectAsync();
        Assert.AreEqual(1, network.RoomCount);
        second.SimulateDrop();

        Assert.AreEqual(0, network.RoomCount);
        await first.ConnectAsync();
        Assert.AreEqual(1, network.MemberCount("room"));
    }

    // ---------------------------------------------------------------------------------------------
    // WebSocket transport and relay
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task A_relay_member_that_falls_behind_is_disconnected()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket(
            "/ws/{documentId}",
            options: new BcrdtWebSocketEndpointOptions { OutboundQueueCapacity = 4 }));

        using var slow = new ClientWebSocket();
        slow.Options.KeepAliveInterval = TimeSpan.Zero;
        await slow.ConnectAsync(new Uri(host.WebSocketBase + "/ws/flood"), CancellationToken.None);
        using var fast = new ClientWebSocket();
        await fast.ConnectAsync(new Uri(host.WebSocketBase + "/ws/flood"), CancellationToken.None);

        // The slow client reads nothing while the fast one floods the room.
        byte[] chunk = BcrdtSyncProtocol.EncodeUpdate(new byte[128 * 1024]);
        for (int i = 0; i < 300; i++)
        {
            await fast.SendAsync(chunk, WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None);
        }

        Assert.IsTrue(
            await ReadsToAnEndAsync(slow, Patience),
            "a member that cannot keep up must be disconnected, not silently cut off");
    }

    [TestMethod]
    public async Task A_relay_member_whose_queue_exceeds_its_byte_budget_is_disconnected()
    {
        // The count limit is far away; only the byte budget can trip.
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket(
            "/ws/{documentId}",
            options: new BcrdtWebSocketEndpointOptions
            {
                MaxMessageSize = 1024 * 1024,
                OutboundQueueCapacity = 100_000,
                OutboundQueueByteLimit = 1024 * 1024,
            }));

        using var slow = new ClientWebSocket();
        slow.Options.KeepAliveInterval = TimeSpan.Zero;
        await slow.ConnectAsync(new Uri(host.WebSocketBase + "/ws/flood"), CancellationToken.None);
        using var fast = new ClientWebSocket();
        await fast.ConnectAsync(new Uri(host.WebSocketBase + "/ws/flood"), CancellationToken.None);

        byte[] chunk = BcrdtSyncProtocol.EncodeUpdate(new byte[256 * 1024]);
        for (int i = 0; i < 300; i++)
        {
            await fast.SendAsync(chunk, WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None);
        }

        Assert.IsTrue(
            await ReadsToAnEndAsync(slow, Patience),
            "a member holding more queued bytes than its budget must be disconnected");
    }

    [TestMethod]
    public async Task An_oversized_message_closes_the_senders_socket()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket(
            "/ws/{documentId}",
            options: new BcrdtWebSocketEndpointOptions { MaxMessageSize = 64 * 1024 }));

        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri(host.WebSocketBase + "/ws/doc"), CancellationToken.None);

        // One byte over: the server reads all of it before refusing, so nothing is left unread to
        // turn its close into a connection reset.
        await client.SendAsync(new byte[(64 * 1024) + 1], WebSocketMessageType.Binary, endOfMessage: true, CancellationToken.None);

        WebSocketReceiveResult result = await client
            .ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None)
            .WaitAsync(Patience);

        Assert.AreEqual(WebSocketMessageType.Close, result.MessageType);
        Assert.AreEqual(WebSocketCloseStatus.MessageTooBig, result.CloseStatus);
    }

    [TestMethod]
    public async Task An_oversized_message_faults_the_receiving_client_transport()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket("/ws/{documentId}"));
        await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/doc")) { MaxMessageSize = 16 * 1024 };
        await transport.ConnectAsync();

        using var sender = new ClientWebSocket();
        await sender.ConnectAsync(new Uri(host.WebSocketBase + "/ws/doc"), CancellationToken.None);
        await sender.SendAsync(BcrdtSyncProtocol.EncodeUpdate(new byte[64 * 1024]), WebSocketMessageType.Binary, true, CancellationToken.None);

        await AssertEventuallyAsync(() => transport.State == BcrdtTransportState.Faulted, "an oversized message must not be buffered");
    }

    [TestMethod]
    public async Task Rooms_are_not_shared_between_endpoint_mappings()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(app =>
        {
            app.MapBitBcrdtWebSocket("/public/{documentId}");
            app.MapBitBcrdtWebSocket(
                "/private/{documentId}",
                (context, _) => ValueTask.FromResult(context.Request.Headers["X-Key"] == "letmein"));
        });

        using var insider = await ConnectRawAsync(host.WebSocketBase + "/private/payroll", key: "letmein");
        using var colleague = await ConnectRawAsync(host.WebSocketBase + "/private/payroll", key: "letmein");
        using var outsider = await ConnectRawAsync(host.WebSocketBase + "/public/payroll");

        Task<WebSocketReceiveResult> colleagueReceives = colleague.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);
        Task<WebSocketReceiveResult> outsiderReceives = outsider.ReceiveAsync(new ArraySegment<byte>(new byte[1024]), CancellationToken.None);

        // Repeated until the colleague hears it, in case its join had not landed by the first send.
        var clock = Stopwatch.StartNew();
        while (!colleagueReceives.IsCompleted)
        {
            Assert.IsTrue(clock.Elapsed < Patience, "a client of the same protected endpoint should hear it");
            await insider.SendAsync(BcrdtSyncProtocol.EncodeUpdate([42]), WebSocketMessageType.Binary, true, CancellationToken.None);
            await Task.WhenAny(colleagueReceives, Task.Delay(200));
        }

        await Task.Delay(QuietPeriod);
        Assert.IsFalse(outsiderReceives.IsCompleted, "a client of the open endpoint must not hear the protected one");
    }

    [TestMethod]
    public async Task A_socket_closed_right_after_the_upgrade_is_never_left_reported_as_connected()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.Map("/close-at-once", async (HttpContext context) =>
        {
            using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            await socket.CloseOutputAsync(WebSocketCloseStatus.PolicyViolation, "Go away.", CancellationToken.None);
            try
            {
                await socket.ReceiveAsync(new ArraySegment<byte>(new byte[64]), CancellationToken.None);
            }
            catch (Exception)
            {
                // Whatever the client does next is fine.
            }
        }));

        // The race is narrow, so take several runs at it.
        for (int run = 0; run < 15; run++)
        {
            await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/close-at-once"));
            try
            {
                await transport.ConnectAsync();
            }
            catch (Exception)
            {
                // Failing to connect at all is an honest outcome too.
            }

            await AssertEventuallyAsync(
                () => transport.State is BcrdtTransportState.Disconnected or BcrdtTransportState.Faulted,
                $"run {run}: a closed socket must not be reported as connected");
        }
    }

    [TestMethod]
    public async Task Connecting_to_a_server_that_never_answers_times_out_and_can_be_abandoned()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        ConcurrentBag<Socket> held = [];
        _ = Task.Run(async () =>
        {
            try
            {
                while (true) held.Add(await listener.AcceptSocketAsync());
            }
            catch (Exception)
            {
                // The listener stopped.
            }
        });
        var address = new Uri($"ws://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/ws/doc");

        await using (var transport = new BcrdtWebSocketTransport(address) { ConnectTimeout = TimeSpan.FromMilliseconds(300) })
        {
            await Assert.ThrowsExactlyAsync<TimeoutException>(() => transport.ConnectAsync().AsTask().WaitAsync(Patience));
            Assert.AreEqual(BcrdtTransportState.Disconnected, transport.State);
        }

        var slowTransport = new BcrdtWebSocketTransport(address) { ConnectTimeout = TimeSpan.FromSeconds(30) };
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), slowTransport);
        Task connecting = provider.ConnectAsync().AsTask();

        await provider.DisconnectAsync().AsTask().WaitAsync(Patience);
        await connecting.WaitAsync(Patience);
        Assert.AreEqual(BcrdtConnectionStatus.Disconnected, provider.Status);

        await slowTransport.DisposeAsync();
        listener.Stop();
        foreach (Socket socket in held) socket.Dispose();
    }

    [TestMethod]
    public async Task An_aborted_socket_is_reported_as_a_lost_connection()
    {
        // Cancelling a send part-way through aborts the socket; so does a keep-alive timeout. Either
        // way the receive loop sees an OperationCanceledException nobody asked for.
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket("/ws/{documentId}"));
        await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/doc"));
        await transport.ConnectAsync();

        transport.CurrentSocket!.Abort();

        await AssertEventuallyAsync(() => transport.State == BcrdtTransportState.Faulted, "an aborted socket is a dead connection");
    }

    [TestMethod]
    public async Task A_403_is_reported_as_a_refusal_and_not_retried()
    {
        int requests = 0;
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.MapBitBcrdtWebSocket(
            "/ws/{documentId}",
            (_, _) =>
            {
                Interlocked.Increment(ref requests);
                return ValueTask.FromResult(false);
            }));

        await using (var direct = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/doc")))
        {
            await Assert.ThrowsExactlyAsync<BcrdtTransportAuthorizationException>(() => direct.ConnectAsync().AsTask());
        }

        await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/doc"));
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);
        int denials = 0;
        provider.AuthorizationDenied += _ => Interlocked.Increment(ref denials);
        await provider.ConnectAsync();

        await AssertEventuallyAsync(() => denials == 1 && provider.Status == BcrdtConnectionStatus.Faulted, "the refusal should be final");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual(2, requests);
    }

    [TestMethod]
    public async Task Disconnecting_sends_a_close_frame()
    {
        var observed = new TaskCompletionSource<WebSocketCloseStatus?>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using LoopbackHost host = await LoopbackHost.StartAsync(app => app.Map("/record-close", async (HttpContext context) =>
        {
            using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            byte[] buffer = new byte[1024];
            try
            {
                while (true)
                {
                    WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType != WebSocketMessageType.Close) continue;

                    observed.TrySetResult(result.CloseStatus);
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                    return;
                }
            }
            catch (Exception)
            {
                observed.TrySetResult(null);
            }
        }));

        await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/record-close"));
        await transport.ConnectAsync();
        await transport.DisconnectAsync();

        Assert.AreEqual(WebSocketCloseStatus.NormalClosure, await observed.Task.WaitAsync(Patience));
    }

    [TestMethod]
    public async Task Shutting_down_the_host_closes_relay_sockets_promptly()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app => app.MapBitBcrdtWebSocket("/ws/{documentId}"),
            builder => builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(30)));

        await using var transport = new BcrdtWebSocketTransport(new Uri(host.WebSocketBase + "/ws/doc"));
        await transport.ConnectAsync();

        var clock = Stopwatch.StartNew();
        await host.App.StopAsync();

        Assert.IsTrue(clock.Elapsed < TimeSpan.FromSeconds(10), $"Shutdown took {clock.Elapsed}.");
        await AssertEventuallyAsync(() => transport.State == BcrdtTransportState.Disconnected, "the client should be told, not aborted");
    }

    [TestMethod]
    public void BuildUri_drops_a_fragment_rather_than_hiding_the_document_inside_it()
    {
        Uri uri = BcrdtWebSocketTransportFactory.BuildUri("wss://example.com/collab#section", "doc 1");

        Assert.AreEqual("documentId=doc%201", uri.Query.TrimStart('?'));
        Assert.AreEqual(string.Empty, uri.Fragment);
        Assert.AreEqual(
            "wss://example.com/collab?a=1&documentId=x",
            BcrdtWebSocketTransportFactory.BuildUri("https://example.com/collab?a=1&", "x").AbsoluteUri);
    }

    // ---------------------------------------------------------------------------------------------
    // SignalR transport and hub
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void Hub_method_names_match_the_client_transport()
    {
        // Both sides are constants, so the analyzer sees these as always true - which is the point:
        // renaming either side alone turns this test red.
#pragma warning disable MSTEST0032
        Assert.AreEqual(BcrdtSignalRTransport.JoinMethod, nameof(BcrdtHub.Join));
        Assert.AreEqual(BcrdtSignalRTransport.LeaveMethod, nameof(BcrdtHub.Leave));
        Assert.AreEqual(BcrdtSignalRTransport.BroadcastMethod, nameof(BcrdtHub.Broadcast));
        Assert.AreEqual(BcrdtSignalRTransport.ReceiveMethod, BcrdtHub.ReceiveMethod);
#pragma warning restore MSTEST0032
    }

    [TestMethod]
    public async Task AddCollaborationHub_lets_a_large_handshake_reply_through()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app => app.MapHub<BcrdtHub>("/hub"),
            builder => builder.Services.AddBitBcrdtHub());

        await using var sender = new BcrdtSignalRTransport(host.BaseUrl + "/hub", "doc");
        await using var receiver = new BcrdtSignalRTransport(host.BaseUrl + "/hub", "doc");
        int received = 0;
        receiver.MessageReceived += message =>
        {
            if (message.Length > 100 * 1024) Interlocked.Increment(ref received);
        };
        await sender.ConnectAsync();
        await receiver.ConnectAsync();

        // Far beyond SignalR's 32 KB default, before base64 inflates it by another third.
        await sender.SendAsync(BcrdtSyncProtocol.EncodeSyncStep2(new byte[200 * 1024]));

        await AssertEventuallyAsync(() => received == 1, "the large message should be relayed");
        Assert.AreEqual(BcrdtTransportState.Connected, sender.State);
    }

    [TestMethod]
    public async Task A_connection_lost_during_join_is_reported_only_by_the_exception()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app => app.MapHub<DropDuringJoinHub>("/drop"),
            builder => builder.Services.AddBitBcrdtHub<DropDuringJoinHub>());

        await using var transport = new BcrdtSignalRTransport(host.BaseUrl + "/drop", "doc");
        ConcurrentQueue<BcrdtTransportState> transitions = [];
        transport.StateChanged += change => transitions.Enqueue(change.State);

        await Assert.ThrowsAsync<Exception>(() => transport.ConnectAsync().AsTask().WaitAsync(Patience));
        await Task.Delay(QuietPeriod);

        Assert.AreSequenceEqual([BcrdtTransportState.Connecting], transitions);
        Assert.AreEqual(BcrdtTransportState.Disconnected, transport.State);
    }

    [TestMethod]
    public async Task A_refused_join_leaves_the_provider_faulted()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app => app.MapHub<DenyHub>("/deny"),
            builder => builder.Services.AddBitBcrdtHub<DenyHub>());

        await using var transport = new BcrdtSignalRTransport(host.BaseUrl + "/deny", "secret");
        await using BcrdtSyncProvider provider = await CreateProviderAsync(new BcrdtDocument(), transport);
        int denials = 0;
        provider.AuthorizationDenied += _ => Interlocked.Increment(ref denials);

        await provider.ConnectAsync();

        await AssertEventuallyAsync(() => denials == 1, "the refusal should be reported");
        await AssertEventuallyAsync(() => transport.State == BcrdtTransportState.Disconnected, "the connection should be closed");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual(BcrdtConnectionStatus.Faulted, provider.Status);
        Assert.AreEqual(1, denials);
    }

    [TestMethod]
    public async Task A_document_named_like_an_application_group_cannot_reach_that_group()
    {
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app => app.MapHub<BcrdtHub>("/hub"),
            builder => builder.Services.AddBitBcrdtHub());
        IHubContext<BcrdtHub> hub = host.App.Services.GetRequiredService<IHubContext<BcrdtHub>>();

        await using HubConnection admin = new HubConnectionBuilder().WithUrl(host.BaseUrl + "/hub").Build();
        await using HubConnection intruder = new HubConnectionBuilder().WithUrl(host.BaseUrl + "/hub").Build();
        int adminReceived = 0;
        int intruderReceived = 0;
        admin.On<byte[]>(BcrdtHub.ReceiveMethod, _ => Interlocked.Increment(ref adminReceived));
        intruder.On<byte[]>(BcrdtHub.ReceiveMethod, _ => Interlocked.Increment(ref intruderReceived));
        await admin.StartAsync();
        await intruder.StartAsync();

        // The application's own group, and an intruder who joins a "document" of the same name.
        await hub.Groups.AddToGroupAsync(admin.ConnectionId!, "admins");
        await intruder.InvokeAsync(BcrdtSignalRTransport.JoinMethod, "admins");

        await hub.Clients.Group("admins").SendAsync(BcrdtHub.ReceiveMethod, BcrdtSyncProtocol.EncodePing());

        await AssertEventuallyAsync(() => adminReceived == 1, "the application's group should receive it");
        await Task.Delay(QuietPeriod);
        Assert.AreEqual(0, intruderReceived);
    }

    // ---------------------------------------------------------------------------------------------
    // Testing package
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void AssertStructurallyIdentical_sees_a_deletion_the_state_vector_cannot()
    {
        using var a = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var b = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        a.GetArray("items").Push(1L, 2L, 3L);
        b.ApplyUpdate(a.EncodeStateAsUpdate());
        BcrdtConvergenceHarness.AssertStructurallyIdentical(a, b);

        b.GetArray("items").Delete(1, 1);

        Assert.IsTrue(a.GetStateVector().StructurallyEquals(b.GetStateVector()));
        Assert.ThrowsExactly<BcrdtConvergenceException>(() => BcrdtConvergenceHarness.AssertStructurallyIdentical(a, b));
    }

    [TestMethod]
    public void AssertConverged_treats_declared_and_undeclared_roots_alike()
    {
        using var typed = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var untyped = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        typed.GetText("body").Insert(0, "abc");
        typed.GetText("body").Delete(0, 3);
        typed.GetArray("never-written");
        untyped.ApplyUpdate(typed.EncodeStateAsUpdate());

        BcrdtConvergenceHarness.AssertConverged(typed, untyped);
        BcrdtConvergenceHarness.AssertConverged(untyped, typed);
    }

    [TestMethod]
    public void The_simulator_delivers_in_send_order_without_latency()
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = 3 });
        BcrdtSimulatedPeer[] peers = network.AddPeers(2);
        List<int> deliveredLengths = [];
        network.Delivered += (target, message) =>
        {
            if (ReferenceEquals(target, peers[1])) deliveredLengths.Add(message.Length);
        };

        for (int i = 1; i <= 5; i++) peers[0].Doc.GetText("t").Insert(0, new string('x', i * 10));
        network.Step();

        Assert.AreEqual(5, deliveredLengths.Count);
        Assert.IsTrue(deliveredLengths.SequenceEqual(deliveredLengths.Order()), "later, longer inserts were sent later");
    }

    [TestMethod]
    public void Simulated_presence_expires_on_the_simulated_clock()
    {
        using var network = new BcrdtSimulatedNetwork();
        BcrdtSimulatedPeer[] peers = network.AddPeers(2);
        peers[0].Awareness.SetLocalState(new Dictionary<string, object?> { ["name"] = "Ada" });
        network.Flush();
        Assert.AreEqual("Ada", peers[1].Awareness.GetField(peers[0].Awareness.LocalClientId, "name"));

        peers[0].GoOffline();
        network.AdvanceTime(TimeSpan.FromSeconds(29));
        peers[1].Awareness.Heartbeat();
        Assert.AreEqual("Ada", peers[1].Awareness.GetField(peers[0].Awareness.LocalClientId, "name"));

        network.AdvanceTime(TimeSpan.FromSeconds(2));
        peers[1].Awareness.Heartbeat();
        Assert.IsNull(peers[1].Awareness.GetField(peers[0].Awareness.LocalClientId, "name"));
    }

    [TestMethod]
    public void A_simulated_replica_returning_with_only_deletions_converges_on_its_own_handshake()
    {
        using var network = new BcrdtSimulatedNetwork(new BcrdtSimulatedNetworkOptions { Seed = 11 });
        BcrdtSimulatedPeer[] peers = network.AddPeers(3);
        peers[0].Doc.GetText("body").Insert(0, "collaborative");
        network.Flush();

        peers[0].GoOffline();
        peers[0].Doc.GetText("body").Delete(0, 6);
        peers[0].GoOnline();
        network.Flush();

        BcrdtConvergenceHarness.AssertStructurallyIdentical([.. network.Peers.Select(peer => peer.Doc)]);
        Assert.AreEqual("orative", peers[2].Doc.GetText("body").ToString());
    }

    // ---------------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------------

    private static BcrdtSyncProviderOptions Options(
        TimeSpan? handshakeTimeout = null,
        BcrdtReconnectPolicy? reconnect = null,
        TimeSpan? heartbeat = null,
        bool syncAwareness = true) => new()
        {
            HeartbeatInterval = heartbeat ?? TimeSpan.Zero,
            HandshakeTimeout = handshakeTimeout ?? TimeSpan.FromMinutes(5),
            Reconnect = reconnect ?? new BcrdtReconnectPolicy
            {
                BaseDelay = TimeSpan.FromMilliseconds(5),
                MaxDelay = TimeSpan.FromMilliseconds(20),
            },
            SyncAwareness = syncAwareness,
        };

    /// <summary>
    /// Builds a provider on the thread pool, so it captures no synchronization context whatever the
    /// test runner has installed, and serializes its own work under its lock.
    /// </summary>
    private static Task<BcrdtSyncProvider> CreateProviderAsync(
        BcrdtDocument doc,
        IBcrdtTransport transport,
        BcrdtAwarenessStore? awareness = null,
        BcrdtSyncProviderOptions? options = null) =>
        Task.Run(() => new BcrdtSyncProvider(doc, transport, awareness, options ?? Options()));

    private static async Task<bool> EventuallyAsync(Func<bool> condition, TimeSpan? timeout = null)
    {
        TimeSpan limit = timeout ?? Patience;
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.Elapsed > limit) return condition();
            await Task.Delay(10);
        }
        return true;
    }

    private static async Task AssertEventuallyAsync(Func<bool> condition, string because, TimeSpan? timeout = null) =>
        Assert.IsTrue(await EventuallyAsync(condition, timeout), because);

    private static async Task AssertEventuallyAsync(Func<Task<bool>> condition, string because)
    {
        var clock = Stopwatch.StartNew();
        while (!await condition())
        {
            Assert.IsTrue(clock.Elapsed < Patience, because);
            await Task.Delay(10);
        }
    }

    private static async Task<ClientWebSocket> ConnectRawAsync(string address, string? key = null)
    {
        var socket = new ClientWebSocket();
        if (key is not null) socket.Options.SetRequestHeader("X-Key", key);
        await socket.ConnectAsync(new Uri(address), CancellationToken.None);
        return socket;
    }

    /// <summary>Reads until the server closes or aborts the socket, or the limit passes in silence.</summary>
    private static async Task<bool> ReadsToAnEndAsync(ClientWebSocket socket, TimeSpan limit)
    {
        byte[] buffer = new byte[1 << 20];
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < limit)
        {
            // Never cancel the receive itself: cancelling would abort the socket from this side.
            Task<WebSocketReceiveResult> receive = socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (await Task.WhenAny(receive, Task.Delay(limit - clock.Elapsed)) != receive) return false;

            try
            {
                if ((await receive).MessageType == WebSocketMessageType.Close) return true;
            }
            catch (Exception)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>A transport whose connection attempts a test scripts, and whose events it raises.</summary>
    private sealed class ScriptedTransport : IBcrdtTransport
    {
        private int _state = (int)BcrdtTransportState.Disconnected;
        private int _concurrentConnects;
        private int _connectCalls;
        private int _maxConcurrentConnects;

        public Func<int, CancellationToken, Task>? OnConnect { get; init; }

        public int ConnectCalls => Volatile.Read(ref _connectCalls);

        public int MaxConcurrentConnects => Volatile.Read(ref _maxConcurrentConnects);

        public BcrdtTransportState State => (BcrdtTransportState)Volatile.Read(ref _state);

        public string Description => "scripted";

        public event Action<ReadOnlyMemory<byte>>? MessageReceived;

        public event Action<BcrdtTransportStateChange>? StateChanged;

        public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            int attempt = Interlocked.Increment(ref _connectCalls);
            int concurrent = Interlocked.Increment(ref _concurrentConnects);
            int observed;
            while (concurrent > (observed = Volatile.Read(ref _maxConcurrentConnects)) &&
                   Interlocked.CompareExchange(ref _maxConcurrentConnects, concurrent, observed) != observed)
            {
            }

            try
            {
                if (OnConnect is not null) await OnConnect(attempt, cancellationToken);
                Raise(BcrdtTransportState.Connected);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentConnects);
            }
        }

        public ValueTask SendAsync(ReadOnlyMemory<byte> message, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask DisconnectAsync(CancellationToken cancellationToken = default)
        {
            Raise(BcrdtTransportState.Disconnected);
            return ValueTask.CompletedTask;
        }

        public void Raise(BcrdtTransportState state, Exception? error = null)
        {
            if (Interlocked.Exchange(ref _state, (int)state) == (int)state) return;
            StateChanged?.Invoke(new BcrdtTransportStateChange(state, error));
        }

        public void Deliver(byte[] message) => MessageReceived?.Invoke(message);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

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

    /// <summary>A synchronization context backed by one dedicated thread, like a UI thread.</summary>
    private sealed class SingleThreadContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];
        private readonly Thread _thread;

        public SingleThreadContext()
        {
            _thread = new Thread(Run) { IsBackground = true, Name = "single-thread-context" };
            _thread.Start();
        }

        public int ThreadId => _thread.ManagedThreadId;

        public override void Post(SendOrPostCallback d, object? state)
        {
            try
            {
                _queue.Add((d, state));
            }
            catch (InvalidOperationException)
            {
                // Shut down; like a closed window, it no longer runs work.
            }
        }

        public override void Send(SendOrPostCallback d, object? state) => throw new NotSupportedException();

        public Task<T> InvokeAsync<T>(Func<T> work)
        {
            var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            Post(_ =>
            {
                try
                {
                    completion.SetResult(work());
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                }
            }, null);
            return completion.Task;
        }

        public Task InvokeAsync(Func<Task> work) => InvokeAsync<Task>(work).Unwrap();

        public void Dispose() => _queue.CompleteAdding();

        private void Run()
        {
            SetSynchronizationContext(this);
            foreach ((SendOrPostCallback callback, object? state) in _queue.GetConsumingEnumerable())
            {
                callback(state);
            }
        }
    }

    /// <summary>A Kestrel host on a loopback port chosen by the operating system.</summary>
    private sealed class LoopbackHost : IAsyncDisposable
    {
        private LoopbackHost(WebApplication app)
        {
            App = app;
            BaseUrl = app.Urls.First();
        }

        public WebApplication App { get; }

        public string BaseUrl { get; }

        public string WebSocketBase => BaseUrl.Replace("http://", "ws://", StringComparison.Ordinal);

        public static async Task<LoopbackHost> StartAsync(Action<WebApplication> map, Action<WebApplicationBuilder>? configure = null)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = AppContext.BaseDirectory,
            });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            configure?.Invoke(builder);

            WebApplication app = builder.Build();
            app.UseWebSockets();
            map(app);
            await app.StartAsync();
            return new LoopbackHost(app);
        }

        public async ValueTask DisposeAsync()
        {
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }
}

/// <summary>A hub that refuses everyone.</summary>
public sealed class DenyHub : BcrdtHub
{
    protected override ValueTask<bool> AuthorizeAsync(string documentId) => ValueTask.FromResult(false);
}

/// <summary>A hub whose connection drops while a join is still in flight.</summary>
public sealed class DropDuringJoinHub : BcrdtHub
{
    public override async Task Join(string documentId)
    {
        await Task.Delay(50);
        Context.Abort();
        await Task.Delay(50);
    }
}
