using System.Diagnostics;
using Bit.Bcrdt.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using LoopbackHost = Bit.Bcrdt.Tests.SyncRegressionTests.LoopbackHost;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Cross-tab sync: the tabs of one browser keeping each other up to date over a channel of their own, next to the
/// server - without handing the server every change once per tab.
/// </summary>
/// <remarks>
/// A tab is a collaboration service whose documents have two providers: one in the server's room, where a plain
/// provider stands for the server, and one in the tabs' room, where the browser's <c>BroadcastChannel</c> would be.
/// Both rooms are an in-memory network, which counts what crosses it.
/// </remarks>
[TestClass]
public class CrossTabTests
{
    private const string DocumentId = "doc";
    private const string ServerRoom = "server:" + DocumentId;
    private const string TabsRoom = "tabs:" + DocumentId;

    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);

    public static IEnumerable<object[]> Seeds
    {
        get
        {
            for (int seed = 1; seed <= 5; seed++) yield return [seed];
        }
    }

    private static BcrdtSyncProviderOptions SyncOptions(TimeSpan? handshakeTimeout = null) => new()
    {
        HeartbeatInterval = TimeSpan.Zero,
        HandshakeTimeout = handshakeTimeout ?? TimeSpan.FromMilliseconds(200),
        Reconnect = new BcrdtReconnectPolicy { BaseDelay = TimeSpan.FromMilliseconds(5), MaxDelay = TimeSpan.FromMilliseconds(20) },
    };

    private sealed class Rooms(BcrdtInMemoryNetwork network, string prefix) : IBcrdtTransportFactory
    {
        public IBcrdtTransport Create(string documentId) => network.CreateTransport(prefix + documentId);
    }

    /// <summary>One browser tab: its documents meet the server in its room and the other tabs in theirs.</summary>
    private static BcrdtCollaborationService Tab(BcrdtInMemoryNetwork network, bool withServer = true) => new(
        new BcrdtCollaborationOptions { Sync = SyncOptions(), CreateUndoManager = false },
        withServer ? new Rooms(network, "server:") : null,
        store: null,
        new Rooms(network, "tabs:"));

    /// <summary>A peer in the server's room - the server, or another person's browser.</summary>
    private sealed class Peer : IAsyncDisposable
    {
        private Peer(BcrdtDocument doc, BcrdtSyncProvider provider)
        {
            Doc = doc;
            Provider = provider;
        }

        public BcrdtDocument Doc { get; }

        public BcrdtSyncProvider Provider { get; }

        /// <summary>Built on the pool, so the provider captures no synchronization context.</summary>
        public static async Task<Peer> JoinAsync(BcrdtInMemoryNetwork network)
        {
            var doc = new BcrdtDocument();
            BcrdtSyncProvider provider = await Task.Run(() => new BcrdtSyncProvider(doc, network.CreateTransport(ServerRoom), null, SyncOptions()));
            await provider.ConnectAsync();
            await AssertEventuallyAsync(() => provider.IsSynced, "the peer should settle in its room");
            return new Peer(doc, provider);
        }

        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            Doc.Dispose();
        }
    }

    /// <summary>Counts the messages of each kind that cross each room.</summary>
    private sealed class Traffic
    {
        private readonly List<(string Room, BcrdtSyncMessageKind Kind)> _messages = [];

        public Traffic(BcrdtInMemoryNetwork network) => network.MessageSent += (room, message) =>
        {
            BcrdtSyncMessageKind kind = BcrdtSyncProtocol.Decode(message).Kind;
            lock (_messages) _messages.Add((room, kind));
        };

        public int Count(string room, params BcrdtSyncMessageKind[] kinds)
        {
            lock (_messages) return _messages.Count(message => message.Room == room && kinds.Contains(message.Kind));
        }

        public int Count(string room)
        {
            lock (_messages) return _messages.Count(message => message.Room == room);
        }

        public void Clear()
        {
            lock (_messages) _messages.Clear();
        }
    }

    private static async Task AssertEventuallyAsync(Func<bool> condition, string because)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.IsTrue(clock.Elapsed < Patience, because);
            await Task.Delay(10);
        }
    }

    private static async Task AssertConvergedEventuallyAsync(string because, params BcrdtDocument[] documents)
    {
        var clock = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                BcrdtConvergenceHarness.AssertConverged(documents);
                return;
            }
            catch (BcrdtConvergenceException) when (clock.Elapsed < Patience)
            {
                await Task.Delay(10);
            }
        }
    }

    private static async Task<BcrdtCollaborativeDocument[]> OpenAsync(IEnumerable<BcrdtCollaborationService> tabs)
    {
        List<BcrdtCollaborativeDocument> documents = [];
        foreach (BcrdtCollaborationService tab in tabs)
        {
            BcrdtCollaborativeDocument document = await tab.OpenAsync(DocumentId);
            documents.Add(document);

            // One at a time, so each tab's handshake with the others has finished before the next one arrives.
            await AssertEventuallyAsync(() => document.CrossTabSync!.IsSynced && (document.Sync?.IsSynced ?? true), "the tab should settle");
        }
        return [.. documents];
    }

    // ---------------------------------------------------------------------------------------------
    // Online
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public async Task Online_each_change_reaches_the_server_once_and_every_tab_once(int seed)
    {
        var network = new BcrdtInMemoryNetwork();
        var traffic = new Traffic(network);
        await using Peer server = await Peer.JoinAsync(network);
        BcrdtCollaborationService[] services = [Tab(network), Tab(network), Tab(network)];
        try
        {
            BcrdtCollaborativeDocument[] tabs = await OpenAsync(services);
            traffic.Clear();

            var generator = new BcrdtOperationGenerator(seed);
            var random = new Random(seed);
            int changes = 0;
            foreach (BcrdtCollaborativeDocument tab in tabs)
            {
                tab.Changed += args =>
                {
                    if (args.Transaction.Local) Interlocked.Increment(ref changes);
                };
            }

            for (int i = 0; i < 40; i++)
            {
                generator.ApplyRandomTransaction(tabs[random.Next(tabs.Length)].Crdt);
            }

            await AssertConvergedEventuallyAsync($"seed {seed}: every tab and the server should hold every change", [server.Doc, .. tabs.Select(tab => tab.Crdt)]);

            // Each change crossed each room exactly once, from the tab that made it: the tabs that received it -
            // from the server, and from the tab - passed it on to neither. Nothing needed repairing either.
            Assert.IsGreaterThan(0, changes, $"seed {seed}: the generator changed nothing");
            Assert.AreEqual(changes, traffic.Count(ServerRoom, BcrdtSyncMessageKind.Update), $"seed {seed}: updates through the server's room");
            Assert.AreEqual(changes, traffic.Count(TabsRoom, BcrdtSyncMessageKind.Update), $"seed {seed}: updates through the tabs' room");
            Assert.AreEqual(2 * changes, traffic.Count(ServerRoom) + traffic.Count(TabsRoom), $"seed {seed}: nothing but updates should have crossed");
        }
        finally
        {
            foreach (BcrdtCollaborationService service in services) await service.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Without_the_forwarding_filter_every_tab_would_hand_the_server_each_change_again()
    {
        // What the collaboration service avoids: two providers per document, both with the default of passing on
        // everything they did not receive themselves.
        var network = new BcrdtInMemoryNetwork();
        var traffic = new Traffic(network);
        await using Peer server = await Peer.JoinAsync(network);

        List<(BcrdtDocument Doc, BcrdtSyncProvider Network, BcrdtSyncProvider Tabs)> tabs = [];
        try
        {
            for (int i = 0; i < 3; i++)
            {
                var doc = new BcrdtDocument();
                BcrdtSyncProvider toServer = await Task.Run(() => new BcrdtSyncProvider(doc, network.CreateTransport(ServerRoom), null, SyncOptions()));
                BcrdtSyncProvider toTabs = await Task.Run(() => new BcrdtSyncProvider(doc, network.CreateTransport(TabsRoom), null, SyncOptions()));
                tabs.Add((doc, toServer, toTabs));
                await toServer.ConnectAsync();
                await toTabs.ConnectAsync();
                await AssertEventuallyAsync(() => toServer.IsSynced && toTabs.IsSynced, "the tab should settle");
            }

            traffic.Clear();
            tabs[0].Doc.GetText("body").Insert(0, "one change");
            await AssertConvergedEventuallyAsync("everyone should hold the change", [server.Doc, .. tabs.Select(tab => tab.Doc)]);

            // Every tab that heard the change from another tab before it heard it from the server passed it on to the
            // server again; which ones did depends only on which copy arrived first.
            Assert.IsGreaterThan(1, traffic.Count(ServerRoom, BcrdtSyncMessageKind.Update), "the author's update, and other tabs' copies of it");
        }
        finally
        {
            foreach ((BcrdtDocument doc, BcrdtSyncProvider toServer, BcrdtSyncProvider toTabs) in tabs)
            {
                await toServer.DisposeAsync();
                await toTabs.DisposeAsync();
                doc.Dispose();
            }
        }
    }

    [TestMethod]
    public async Task A_provider_sends_on_only_the_changes_its_filter_lets_through()
    {
        var network = new BcrdtInMemoryNetwork();
        var traffic = new Traffic(network);
        await using Peer server = await Peer.JoinAsync(network);

        using var doc = new BcrdtDocument();
        object quiet = new();
        await using BcrdtSyncProvider provider = await Task.Run(() => new BcrdtSyncProvider(doc, network.CreateTransport(ServerRoom), null, new BcrdtSyncProviderOptions
        {
            HeartbeatInterval = TimeSpan.Zero,
            HandshakeTimeout = TimeSpan.FromMilliseconds(200),
            ShouldForward = args => args.Origin != quiet,
        }));
        await provider.ConnectAsync();
        await AssertEventuallyAsync(() => provider.IsSynced, "the provider should settle");
        traffic.Clear();

        // What a second provider of the same document received - from another tab, say - applied under its origin.
        using (var elsewhere = new BcrdtDocument())
        {
            elsewhere.GetText("quiet").Insert(0, "kept here");
            doc.ApplyUpdate(elsewhere.EncodeStateAsUpdate(), quiet);
        }
        doc.GetText("body").Insert(0, "sent");

        await AssertEventuallyAsync(() => server.Doc.GetText("body").ToString() == "sent", "the change the filter let through is sent");
        Assert.AreEqual(1, traffic.Count(ServerRoom, BcrdtSyncMessageKind.Update), "and only that one");
        Assert.AreEqual(string.Empty, server.Doc.GetText("quiet").ToString());

        // The handshake is not filtered: it reconciles everything.
        await provider.ResyncAsync();
        await AssertEventuallyAsync(() => server.Doc.GetText("quiet").ToString() == "kept here", "the next handshake brings the rest");
    }

    // ---------------------------------------------------------------------------------------------
    // Offline
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public async Task Tabs_that_are_all_offline_keep_each_other_up_to_date_and_the_server_catches_up_on_reconnection(int seed)
    {
        var network = new BcrdtInMemoryNetwork();
        var traffic = new Traffic(network);
        await using Peer server = await Peer.JoinAsync(network);
        BcrdtCollaborationService[] services = [Tab(network), Tab(network), Tab(network)];
        try
        {
            BcrdtCollaborativeDocument[] tabs = await OpenAsync(services);

            var generator = new BcrdtOperationGenerator(seed);
            var random = new Random(seed);
            for (int i = 0; i < 15; i++) generator.ApplyRandomTransaction(tabs[random.Next(tabs.Length)].Crdt);
            await AssertConvergedEventuallyAsync($"seed {seed}: the shared start", [server.Doc, .. tabs.Select(tab => tab.Crdt)]);
            string before = BcrdtConvergenceHarness.Describe(server.Doc);

            // The browser goes offline: every tab loses the server, and none loses the others.
            foreach (BcrdtCollaborativeDocument tab in tabs) await tab.DisconnectAsync();
            await AssertEventuallyAsync(() => network.MemberCount(ServerRoom) == 1, "only the server should be left in its room");
            traffic.Clear();

            for (int i = 0; i < 30; i++) generator.ApplyRandomTransaction(tabs[random.Next(tabs.Length)].Crdt);

            // Deletions too - they leave no trace in a state vector.
            BcrdtText body = tabs[random.Next(tabs.Length)].Text("offline");
            body.Insert(0, "written and then partly deleted offline");
            body.Delete("written and ".Length, "then ".Length);

            await AssertConvergedEventuallyAsync($"seed {seed}: the tabs should follow each other offline", [.. tabs.Select(tab => tab.Crdt)]);
            Assert.AreEqual("written and partly deleted offline", tabs[0].Text("offline").ToString());
            Assert.AreEqual(0, traffic.Count(ServerRoom), $"seed {seed}: nothing should reach the server while the tabs are offline");
            Assert.AreEqual(before, BcrdtConvergenceHarness.Describe(server.Doc), $"seed {seed}: the server should not have changed");

            foreach (BcrdtCollaborativeDocument tab in tabs) await tab.ConnectAsync();

            await AssertConvergedEventuallyAsync($"seed {seed}: reconnecting should bring the server everything", [server.Doc, .. tabs.Select(tab => tab.Crdt)]);
            await AssertEventuallyAsync(() => tabs.All(tab => tab.IsSynced), $"seed {seed}: every tab should be synced with the server again");
        }
        finally
        {
            foreach (BcrdtCollaborationService service in services) await service.DisposeAsync();
        }
    }

    [TestMethod]
    [DynamicData(nameof(Seeds))]
    public async Task A_tab_offline_among_online_ones_shares_its_changes_with_them_and_the_server_gets_them_when_it_reconnects(int seed)
    {
        var network = new BcrdtInMemoryNetwork();
        var traffic = new Traffic(network);
        await using Peer server = await Peer.JoinAsync(network);
        await using Peer elsewhere = await Peer.JoinAsync(network);   // somebody else's browser
        BcrdtCollaborationService[] services = [Tab(network), Tab(network), Tab(network)];
        try
        {
            BcrdtCollaborativeDocument[] tabs = await OpenAsync(services);
            BcrdtCollaborativeDocument offline = tabs[0];
            BcrdtCollaborativeDocument[] online = tabs[1..];

            online[0].Text("notes").Insert(0, "shared start, some of it deleted later");
            await AssertConvergedEventuallyAsync("the shared start", server.Doc, elsewhere.Doc, offline.Crdt, online[0].Crdt, online[1].Crdt);

            await offline.DisconnectAsync();
            traffic.Clear();

            // The offline tab's changes - a deletion among them - reach the other tabs, and not the server: the tabs
            // that received them leave that to the tab that made them.
            var generator = new BcrdtOperationGenerator(seed);
            for (int i = 0; i < 10; i++) generator.ApplyRandomTransaction(offline.Crdt);
            offline.Text("notes").Delete("shared start, ".Length, "some of it ".Length);
            await AssertConvergedEventuallyAsync($"seed {seed}: the online tabs should follow the offline one", offline.Crdt, online[0].Crdt, online[1].Crdt);
            Assert.AreEqual("shared start, deleted later", online[1].Text("notes").ToString());
            Assert.AreEqual(0, traffic.Count(ServerRoom, BcrdtSyncMessageKind.Update), $"seed {seed}: nobody should send the server the offline tab's changes");
            Assert.AreEqual("shared start, some of it deleted later", server.Doc.GetText("notes").ToString());

            // An online tab's change reaches the offline tab through the others' channel; one from elsewhere does
            // not, because the tabs that received it from the server pass it on to nobody.
            online[0].Map.Set("online", "from a tab");
            elsewhere.Doc.GetMap("").Set("elsewhere", "from another browser");
            await AssertEventuallyAsync(() => (string?)offline.Map.Get("online") == "from a tab", "the online tab's change should reach the offline one");
            await AssertEventuallyAsync(() => (string?)online[1].Map.Get("elsewhere") == "from another browser", "the change from elsewhere should reach the online tabs");
            Assert.IsNull(offline.Map.Get("elsewhere"), "the offline tab should get the change from elsewhere when it reconnects, not before");

            await offline.ConnectAsync();

            await AssertConvergedEventuallyAsync($"seed {seed}: reconnecting should reconcile everyone", server.Doc, elsewhere.Doc, offline.Crdt, online[0].Crdt, online[1].Crdt);
            Assert.AreEqual("from another browser", offline.Map.Get("elsewhere"));
            Assert.AreEqual("shared start, deleted later", server.Doc.GetText("notes").ToString());
        }
        finally
        {
            foreach (BcrdtCollaborationService service in services) await service.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task Over_a_bare_relay_a_change_building_on_an_offline_tabs_waits_until_that_tab_reconnects()
    {
        // The price of passing on only a tab's own changes: an online tab that builds on what an offline tab wrote
        // sends a change its peers cannot place until the offline tab brings them what it builds on.
        var network = new BcrdtInMemoryNetwork();
        await using Peer elsewhere = await Peer.JoinAsync(network);
        await using BcrdtCollaborationService first = Tab(network);
        await using BcrdtCollaborationService second = Tab(network);
        BcrdtCollaborativeDocument[] tabs = await OpenAsync([first, second]);
        (BcrdtCollaborativeDocument online, BcrdtCollaborativeDocument offline) = (tabs[0], tabs[1]);

        await offline.DisconnectAsync();
        offline.Text("notes").Insert(0, "Written offline.");
        await AssertEventuallyAsync(() => online.Text("notes").ToString() == "Written offline.", "the online tab should follow the offline one");
        online.Text("notes").Insert("Written offline.".Length, " Continued online.");

        await AssertEventuallyAsync(() => elsewhere.Doc.HasPendingUpdates, "the continuation should arrive, and wait");
        Assert.AreEqual(string.Empty, elsewhere.Doc.GetText("notes").ToString());

        await offline.ConnectAsync();
        await AssertConvergedEventuallyAsync("the offline tab's handshake should bring what the continuation builds on", elsewhere.Doc, online.Crdt, offline.Crdt);
        Assert.AreEqual("Written offline. Continued online.", elsewhere.Doc.GetText("notes").ToString());
    }

    [TestMethod]
    public async Task With_a_document_host_a_change_building_on_an_offline_tabs_reaches_everyone_at_once()
    {
        // The host asks the sender of a change it cannot place for what the change builds on - and the online tab
        // has it, from the offline one.
        await using LoopbackHost host = await LoopbackHost.StartAsync(
            app => app.MapBitBcrdtWebSocket("/ws/{documentId}", options: new BcrdtWebSocketEndpointOptions
            {
                DocumentHost = app.Services.GetRequiredService<BcrdtDocumentHost>(),
            }),
            builder => builder.Services.AddBitBcrdtDocumentHost());

        var network = new BcrdtInMemoryNetwork();
        var relay = new BcrdtWebSocketTransportFactory(host.WebSocketBase + "/ws/" + BcrdtWebSocketTransportFactory.DocumentIdPlaceholder);
        var options = new BcrdtCollaborationOptions { Sync = SyncOptions(), CreateUndoManager = false };
        await using var first = new BcrdtCollaborationService(options, relay, null, new Rooms(network, "tabs:"));
        await using var second = new BcrdtCollaborationService(options, relay, null, new Rooms(network, "tabs:"));

        using var elsewhere = new BcrdtDocument();
        await using BcrdtSyncProvider elsewhereProvider = await Task.Run(() => new BcrdtSyncProvider(elsewhere, relay.Create(DocumentId), null, SyncOptions()));
        await elsewhereProvider.ConnectAsync();
        await AssertEventuallyAsync(() => elsewhereProvider.IsSynced, "somebody else's browser should be in the room");

        BcrdtCollaborativeDocument[] tabs = await OpenAsync([first, second]);
        (BcrdtCollaborativeDocument online, BcrdtCollaborativeDocument offline) = (tabs[0], tabs[1]);

        await offline.DisconnectAsync();
        offline.Text("notes").Insert(0, "Written offline.");
        await AssertEventuallyAsync(() => online.Text("notes").ToString() == "Written offline.", "the online tab should follow the offline one");
        online.Text("notes").Insert("Written offline.".Length, " Continued online.");

        await AssertEventuallyAsync(() => elsewhere.GetText("notes").ToString() == "Written offline. Continued online.", "the host should ask the online tab for the rest");
        Assert.AreEqual(BcrdtConnectionStatus.Disconnected, offline.ConnectionStatus, "with the offline tab still offline");
    }

    [TestMethod]
    public async Task A_tab_opened_while_offline_catches_up_with_the_tabs_already_open()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService first = Tab(network, withServer: false);
        await using BcrdtCollaborationService second = Tab(network, withServer: false);

        BcrdtCollaborativeDocument a = (await OpenAsync([first]))[0];
        a.Text("body").Insert(0, "typed before the second tab opened");
        a.Text("body").Delete(0, 6);

        BcrdtCollaborativeDocument b = (await OpenAsync([second]))[0];

        Assert.AreEqual("before the second tab opened", b.Text("body").ToString());
        Assert.IsNull(b.Sync, "there is no network");
        Assert.AreEqual(BcrdtConnectionStatus.Disconnected, b.ConnectionStatus, "the status is the network's");
    }

    [TestMethod]
    public async Task A_tab_alone_counts_as_synced_with_the_others_once_its_handshake_times_out()
    {
        var network = new BcrdtInMemoryNetwork();
        await using var tab = new BcrdtCollaborationService(
            new BcrdtCollaborationOptions { Sync = SyncOptions(TimeSpan.FromMilliseconds(300)), CreateUndoManager = false },
            transports: null,
            store: null,
            new Rooms(network, "tabs:"));

        var clock = Stopwatch.StartNew();
        BcrdtCollaborativeDocument document = await tab.OpenAsync(DocumentId);
        await AssertEventuallyAsync(() => document.CrossTabSync!.IsSynced, "nobody is there to answer");

        Assert.IsGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(250), clock.Elapsed, "synced by the timeout, not by an answer");
        Assert.AreEqual(1, network.MemberCount(TabsRoom));
    }

    [TestMethod]
    public async Task The_tabs_stay_connected_when_the_document_is_taken_offline_and_disconnect_when_it_closes()
    {
        var network = new BcrdtInMemoryNetwork();
        await using Peer server = await Peer.JoinAsync(network);
        await using BcrdtCollaborationService tab = Tab(network);
        BcrdtCollaborativeDocument document = (await OpenAsync([tab]))[0];

        await document.DisconnectAsync();
        Assert.AreEqual(BcrdtConnectionStatus.Disconnected, document.ConnectionStatus);
        Assert.IsTrue(document.CrossTabSync!.IsSynced);
        Assert.AreEqual(1, network.MemberCount(TabsRoom));

        await tab.CloseAsync(DocumentId);
        Assert.AreEqual(0, network.MemberCount(TabsRoom));
        Assert.AreEqual(1, network.MemberCount(ServerRoom), "the server alone");
    }

    // ---------------------------------------------------------------------------------------------
    // Presence
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Presence_crosses_only_the_network()
    {
        var network = new BcrdtInMemoryNetwork();
        var traffic = new Traffic(network);
        await using Peer server = await Peer.JoinAsync(network);
        await using BcrdtCollaborationService first = Tab(network);
        await using BcrdtCollaborationService second = Tab(network);

        // Opened offline: the tabs reach each other, and not the server.
        var ada = new BcrdtCollaboratorInfo("ada", "Ada", "#ff0000");
        BcrdtCollaborativeDocument a = await first.OpenAsync(DocumentId, new BcrdtOpenDocumentOptions { User = ada, Connect = false });
        BcrdtCollaborativeDocument b = await second.OpenAsync(DocumentId, new BcrdtOpenDocumentOptions { User = ada, Connect = false });
        await AssertEventuallyAsync(() => a.CrossTabSync!.IsSynced && b.CrossTabSync!.IsSynced, "the tabs should settle");

        a.Text("body").Insert(0, "shared");
        await AssertEventuallyAsync(() => b.Text("body").ToString() == "shared", "the edit should cross");
        Assert.IsEmpty(a.Presence.Others, "presence does not cross between tabs");
        Assert.IsEmpty(b.Presence.Others, "presence does not cross between tabs");

        // Online, each lists the other - once, through the server.
        await a.ConnectAsync();
        await b.ConnectAsync();
        await AssertEventuallyAsync(() => a.Presence.Others.Count == 1 && b.Presence.Others.Count == 1, "each tab should list the other through the server");
        Assert.AreEqual(b.ClientId, a.Presence.Others.Single().ClientId);
        Assert.AreEqual(0, traffic.Count(TabsRoom, BcrdtSyncMessageKind.AwarenessUpdate, BcrdtSyncMessageKind.AwarenessQuery));
    }

    // ---------------------------------------------------------------------------------------------
    // Registration
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Off_the_browser_UseCrossTabSync_is_a_harmless_no_op()
    {
        // No IJSRuntime either: nothing may ask for one.
        await using ServiceProvider services = new ServiceCollection()
            .AddBitBcrdtServices(collaboration => collaboration.UseCrossTabSync())
            .BuildServiceProvider();

        Assert.IsNull(services.GetService<BcrdtCrossTabTransports>());

        await using AsyncServiceScope scope = services.CreateAsyncScope();
        BcrdtCollaborativeDocument document = await scope.ServiceProvider.GetRequiredService<IBcrdtCollaborationService>().OpenAsync(DocumentId);
        Assert.IsNull(document.CrossTabSync);
        Assert.IsNull(document.Sync);
    }

    [TestMethod]
    public async Task The_registration_UseCrossTabSync_makes_in_the_browser_gives_every_document_its_provider()
    {
        // What UseCrossTabSync registers on WebAssembly, with the in-memory network standing in for the channel.
        var network = new BcrdtInMemoryNetwork();
        IServiceCollection collection = new ServiceCollection().AddBitBcrdtServices(collaboration => collaboration
            .Configure(options => options.Sync = SyncOptions()));
        collection.AddScoped(_ => new BcrdtCrossTabTransports(new Rooms(network, "tabs:")));
        await using ServiceProvider services = collection.BuildServiceProvider();

        await using AsyncServiceScope first = services.CreateAsyncScope();
        await using AsyncServiceScope second = services.CreateAsyncScope();
        BcrdtCollaborativeDocument[] tabs = await OpenAsync([
            (BcrdtCollaborationService)first.ServiceProvider.GetRequiredService<IBcrdtCollaborationService>(),
            (BcrdtCollaborationService)second.ServiceProvider.GetRequiredService<IBcrdtCollaborationService>()]);

        tabs[0].Text("body").Insert(0, "across");
        await AssertEventuallyAsync(() => tabs[1].Text("body").ToString() == "across", "the edit should cross");
    }

    // ---------------------------------------------------------------------------------------------
    // The BroadcastChannel transport
    // ---------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Tabs_meet_on_a_channel_named_after_the_document_and_leave_it_when_they_close_it()
    {
        var browser = new FakeBroadcastChannels();
        var options = new BcrdtCollaborationOptions { Sync = SyncOptions(), CreateUndoManager = false };
        await using var first = new BcrdtCollaborationService(options, null, null, new BcrdtBroadcastChannelTransportFactory(browser));
        await using var second = new BcrdtCollaborationService(options, null, null, new BcrdtBroadcastChannelTransportFactory(browser));
        await using var otherApp = new BcrdtCollaborationService(options, null, null, new BcrdtBroadcastChannelTransportFactory(browser, "other-app"));

        BcrdtCollaborativeDocument[] tabs = await OpenAsync([first, second, otherApp]);
        CollectionAssert.AreEquivalent(new[] { "bit-bcrdt/doc", "bit-bcrdt/doc", "other-app/doc" }, browser.OpenChannels());

        tabs[0].Text("body").Insert(0, "hello, other tab");
        tabs[1].Text("body").Insert(0, ">> ");
        await AssertConvergedEventuallyAsync("the two tabs of one app should meet", tabs[0].Crdt, tabs[1].Crdt);
        Assert.AreEqual(">> hello, other tab", tabs[0].Text("body").ToString());
        Assert.AreEqual(string.Empty, tabs[2].Text("body").ToString(), "another prefix is another channel");

        await first.CloseAsync(DocumentId);
        CollectionAssert.AreEquivalent(new[] { "bit-bcrdt/doc", "other-app/doc" }, browser.OpenChannels());
        Assert.AreEqual(0, browser.LiveReceivers(), "a closed channel's receiver should be released, and the others' are .NET's to keep");

        tabs[1].Text("body").Insert(0, "after: ");
        Assert.AreEqual(">> hello, other tab", tabs[0].Text("body").ToString(), "a closed tab hears nothing more");
    }

    [TestMethod]
    public async Task A_message_that_arrives_after_the_channel_closed_is_dropped()
    {
        var browser = new FakeBroadcastChannels();
        await using var transport = new BcrdtBroadcastChannelTransport(browser, "bit-bcrdt/doc");
        Assert.AreEqual("broadcast-channel:bit-bcrdt/doc", transport.Description);
        int received = 0;
        transport.MessageReceived += _ => received++;

        await transport.ConnectAsync();
        BcrdtBroadcastChannelTransport.Receiver receiver = browser.Receivers().Single().Value;
        receiver.OnChannelMessage([1, 2, 3]);
        Assert.AreEqual(1, received);

        // One already on its way through interop when the channel closed.
        await transport.DisconnectAsync();
        Assert.AreEqual(BcrdtTransportState.Disconnected, transport.State);
        receiver.OnChannelMessage([1, 2, 3]);
        Assert.AreEqual(1, received);

        // A channel opened again reports to a receiver of its own.
        await transport.ConnectAsync();
        Assert.AreNotSame(receiver, browser.Receivers().Single().Value);
        await transport.DisconnectAsync();

        // Sending on a closed channel is dropped, not thrown, as the transport contract asks.
        await transport.SendAsync(new byte[] { 4 });
        Assert.IsEmpty(browser.Posted);
    }

    /// <summary>
    /// Stands in for the browser and the module's <c>openChannel</c>: channels by name, each message delivered to every
    /// other open channel of the same name and never to the one that posted it.
    /// </summary>
    private sealed class FakeBroadcastChannels : IJSRuntime, IJSObjectReference
    {
        private readonly List<Channel> _channels = [];

        public List<(string Name, byte[] Data)> Posted { get; } = [];

        public string[] OpenChannels()
        {
            lock (_channels) return [.. _channels.Where(channel => channel.Open).Select(channel => channel.Name)];
        }

        public DotNetObjectReference<BcrdtBroadcastChannelTransport.Receiver>[] Receivers()
        {
            lock (_channels) return [.. _channels.Where(channel => channel.Open).Select(channel => channel.Receiver)];
        }

        public int LiveReceivers()
        {
            lock (_channels) return _channels.Count(channel => !channel.Open && IsLive(channel.Receiver));
        }

        private static bool IsLive(DotNetObjectReference<BcrdtBroadcastChannelTransport.Receiver> receiver)
        {
            try
            {
                _ = receiver.Value;
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            args ??= [];
            object? result = identifier switch
            {
                "import" => this,
                "openChannel" => Open((string)args[0]!, (DotNetObjectReference<BcrdtBroadcastChannelTransport.Receiver>)args[1]!),
                _ => throw new InvalidOperationException($"Unexpected call to '{identifier}'."),
            };
            return ValueTask.FromResult((TValue)result!);
        }

        private Channel Open(string name, DotNetObjectReference<BcrdtBroadcastChannelTransport.Receiver> receiver)
        {
            var channel = new Channel(this, name, receiver);
            lock (_channels) _channels.Add(channel);
            return channel;
        }

        private void Post(Channel sender, byte[] data)
        {
            Channel[] others;
            lock (_channels)
            {
                Posted.Add((sender.Name, data));
                others = [.. _channels.Where(channel => channel.Open && channel.Name == sender.Name && channel != sender)];
            }

            // Structured clone: every tab gets its own copy.
            foreach (Channel other in others) other.Receiver.Value.OnChannelMessage([.. data]);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class Channel(FakeBroadcastChannels browser, string name, DotNetObjectReference<BcrdtBroadcastChannelTransport.Receiver> receiver) : IJSObjectReference
        {
            public string Name => name;

            public DotNetObjectReference<BcrdtBroadcastChannelTransport.Receiver> Receiver => receiver;

            public bool Open { get; private set; } = true;

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
                InvokeAsync<TValue>(identifier, CancellationToken.None, args);

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            {
                switch (identifier)
                {
                    case "post":
                        if (Open) browser.Post(this, (byte[])args![0]!);
                        break;
                    case "close":
                        Open = false;
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected call to '{identifier}'.");
                }
                return ValueTask.FromResult(default(TValue)!);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
