namespace Bit.Bcrdt.Tests;

/// <summary>
/// The whole stack assembled: service, transport, provider, presence and storage.
/// </summary>
/// <remarks>
/// The layers below are tested in isolation elsewhere. What matters here is that they compose -
/// that opening a document actually connects it, that an edit in one client reaches another, and
/// that a disconnection followed by offline editing reconciles on reconnect.
/// </remarks>
[TestClass]
public class CollaborationStackTests
{
    private sealed class RoomTransportFactory(BcrdtInMemoryNetwork network) : IBcrdtTransportFactory
    {
        public IBcrdtTransport Create(string documentId) => network.CreateTransport(documentId);
    }

    private static BcrdtCollaborationService CreateClient(BcrdtInMemoryNetwork network, IBcrdtDocumentStore? store = null) =>
        new(
            new BcrdtCollaborationOptions
            {
                Sync = new BcrdtSyncProviderOptions { HeartbeatInterval = TimeSpan.Zero },
                CreateUndoManager = false,
            },
            new RoomTransportFactory(network),
            store);

    private static async Task SettleAsync() => await Task.Delay(50);

    [TestMethod]
    public async Task An_edit_in_one_client_reaches_another()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService clientA = CreateClient(network);
        await using BcrdtCollaborationService clientB = CreateClient(network);

        BcrdtCollaborativeDocument a = await clientA.OpenAsync("shared");
        BcrdtCollaborativeDocument b = await clientB.OpenAsync("shared");
        await SettleAsync();

        a.Text("body").Insert(0, "hello from A");
        await SettleAsync();

        Assert.AreEqual("hello from A", b.Text("body").ToString());
    }

    [TestMethod]
    public async Task A_client_joining_late_receives_the_existing_document()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService clientA = CreateClient(network);

        BcrdtCollaborativeDocument a = await clientA.OpenAsync("shared");
        a.Text("body").Insert(0, "already here");
        await SettleAsync();

        await using BcrdtCollaborationService clientB = CreateClient(network);
        BcrdtCollaborativeDocument b = await clientB.OpenAsync("shared");
        await SettleAsync();

        // The handshake, not a replay of past updates, is what brings a late joiner up to date.
        Assert.AreEqual("already here", b.Text("body").ToString());
        Assert.IsTrue(b.IsSynced);
    }

    [TestMethod]
    public async Task Offline_edits_on_both_sides_merge_on_reconnect()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService clientA = CreateClient(network);
        await using BcrdtCollaborationService clientB = CreateClient(network);

        BcrdtCollaborativeDocument a = await clientA.OpenAsync("shared");
        BcrdtCollaborativeDocument b = await clientB.OpenAsync("shared");
        await SettleAsync();

        a.Array("items").Push("shared-start");
        await SettleAsync();

        await a.DisconnectAsync();

        // Both sides keep working. Neither can see the other.
        for (int i = 0; i < 20; i++) a.Array("items").Push($"offline-a-{i}");
        for (int i = 0; i < 20; i++) b.Array("items").Push($"offline-b-{i}");
        await SettleAsync();

        Assert.AreEqual(21, a.Array("items").Count);
        Assert.AreEqual(21, b.Array("items").Count);

        await a.ConnectAsync();
        await SettleAsync();

        // Reconciliation has to go both ways. The returning client needs what it missed, and the
        // client that stayed connected needs the work done while the other was away.
        Assert.AreEqual(41, a.Array("items").Count);
        Assert.AreEqual(41, b.Array("items").Count);
        BcrdtConvergenceHarness.AssertConverged(a.Crdt, b.Crdt);
    }

    [TestMethod]
    public async Task Presence_propagates_between_clients()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService clientA = CreateClient(network);
        await using BcrdtCollaborationService clientB = CreateClient(network);

        BcrdtCollaborativeDocument a = await clientA.OpenAsync(
            "shared", new BcrdtOpenDocumentOptions { User = new BcrdtCollaboratorInfo("ada", "Ada", "#ff0000") });
        BcrdtCollaborativeDocument b = await clientB.OpenAsync(
            "shared", new BcrdtOpenDocumentOptions { User = new BcrdtCollaboratorInfo("grace", "Grace", "#00ff00") });

        await SettleAsync();

        Assert.Contains(other => other.User.Name == "Ada", b.Presence.Others);
        Assert.Contains(other => other.User.Name == "Grace", a.Presence.Others);
    }

    [TestMethod]
    public async Task Presence_is_not_written_to_storage()
    {
        var network = new BcrdtInMemoryNetwork();
        var store = new BcrdtInMemoryDocumentStore();
        await using BcrdtCollaborationService client = CreateClient(network, store);

        BcrdtCollaborativeDocument doc = await client.OpenAsync(
            "shared", new BcrdtOpenDocumentOptions { User = new BcrdtCollaboratorInfo("ada", "Ada", "#ff0000") });

        doc.Text("body").Insert(0, "document content");
        await doc.FlushAsync();

        // A reloaded document must know its content and know nothing about who was looking at it.
        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 99 });
        BcrdtStoredDocument stored = await store.LoadAsync("shared");
        reloaded.ApplyUpdate(stored.Update!);

        Assert.AreEqual("document content", reloaded.GetText("body").ToString());
        Assert.DoesNotContain("Ada", BcrdtConvergenceHarness.Describe(reloaded));
    }

    [TestMethod]
    public async Task Opening_the_same_document_twice_returns_one_replica()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService client = CreateClient(network);

        BcrdtCollaborativeDocument first = await client.OpenAsync("shared");
        BcrdtCollaborativeDocument second = await client.OpenAsync("shared");

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public async Task A_transaction_batches_changes_into_a_single_update()
    {
        var network = new BcrdtInMemoryNetwork();
        await using BcrdtCollaborationService client = CreateClient(network);
        BcrdtCollaborativeDocument doc = await client.OpenAsync("shared");

        int updates = 0;
        doc.Changed += _ => updates++;

        using (BcrdtCollaborativeTransaction transaction = doc.BeginTransaction())
        {
            doc.Map.Set("title", "Hello");
            doc.Array("items").Push(1, 2, 3);
            doc.Text("body").Insert(0, "batched");
        }

        Assert.AreEqual(1, updates);
        Assert.AreEqual("Hello", doc.Map.Get("title"));
        Assert.AreEqual(3, doc.Array("items").Count);
    }

    [TestMethod]
    public async Task A_dropped_connection_is_reported_and_reconnection_restores_sync()
    {
        var network = new BcrdtInMemoryNetwork();
        var transport = network.CreateTransport("shared");

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using var provider = new BcrdtSyncProvider(doc, transport, null, new BcrdtSyncProviderOptions
        {
            HeartbeatInterval = TimeSpan.Zero,
            Reconnect = new BcrdtReconnectPolicy { BaseDelay = TimeSpan.FromMilliseconds(10) },
        });

        List<BcrdtConnectionStatus> statuses = [];
        provider.StatusChanged += status => statuses.Add(status);

        await provider.ConnectAsync();
        await SettleAsync();

        transport.SimulateDrop();
        await Task.Delay(200);

        Assert.Contains(BcrdtConnectionStatus.Reconnecting, statuses);
        Assert.AreEqual(BcrdtTransportState.Connected, transport.State);
    }

    [TestMethod]
    public void Reconnect_backoff_grows_and_stays_within_its_ceiling()
    {
        var policy = new BcrdtReconnectPolicy
        {
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(5),
        };

        for (int attempt = 1; attempt <= 40; attempt++)
        {
            TimeSpan delay = policy.NextDelay(attempt);
            Assert.IsInRange(TimeSpan.Zero, policy.MaxDelay, delay);
        }

        // Full jitter means the delay is random, so the property to check is spread rather than
        // any particular value: a fixed schedule would make every client retry in lockstep.
        var samples = Enumerable.Range(0, 50).Select(_ => policy.NextDelay(10)).Distinct().Count();
        Assert.IsTrue(samples > 20, $"Only {samples} distinct delays; the backoff is not jittering.");
    }

    [TestMethod]
    public void The_text_synchronizer_recovers_the_edit_from_a_whole_new_value()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "the quick brown fox");

        // What an input element reports: the entire new value, not the change.
        BcrdtTextSynchronizer.Apply(text, "the quick red fox");

        Assert.AreEqual("the quick red fox", text.ToString());
    }

    [TestMethod]
    public void The_text_synchronizer_produces_a_minimal_edit_that_preserves_concurrent_work()
    {
        using var a = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var b = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        a.GetText("body").Insert(0, "hello world");
        b.ApplyUpdate(a.EncodeStateAsUpdate(), "sync");

        // A edits the end through an input control while B concurrently edits the start.
        BcrdtTextSynchronizer.Apply(a.GetText("body"), "hello world!");
        b.GetText("body").Insert(0, ">> ");

        a.ApplyUpdate(b.EncodeStateAsUpdate(a.GetStateVector()), "sync");
        b.ApplyUpdate(a.EncodeStateAsUpdate(b.GetStateVector()), "sync");

        // A whole-value write would have destroyed B's prefix.
        Assert.AreEqual(">> hello world!", a.GetText("body").ToString());
        BcrdtConvergenceHarness.AssertConverged(a, b);
    }

    [TestMethod]
    [DataRow("abc", "abc", 0, 0, "")]
    [DataRow("abc", "abXc", 2, 0, "X")]
    [DataRow("abc", "ac", 1, 1, "")]
    [DataRow("", "hello", 0, 0, "hello")]
    [DataRow("hello", "", 0, 5, "")]
    [DataRow("aaa", "aaaa", 3, 0, "a")]
    public void The_text_diff_finds_the_narrowest_change(
        string current, string desired, int index, int deleted, string inserted)
    {
        BcrdtTextEdit edit = BcrdtTextSynchronizer.Diff(current, desired);

        Assert.AreEqual(index, edit.Index);
        Assert.AreEqual(deleted, edit.DeletedLength);
        Assert.AreEqual(inserted, edit.Inserted);
    }

    [TestMethod]
    public void A_caret_is_carried_across_a_change_made_earlier_in_the_document()
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        BcrdtText text = doc.GetText("body");
        text.Insert(0, "hello world");

        List<BcrdtDeltaOperation> observed = [];
        text.Observe((e, _) => observed.AddRange(((BcrdtTextEvent)e).Delta));

        text.Insert(0, ">>>> ");

        // A caret at 6 should still be on the same character, five positions further along.
        Assert.AreEqual(11, BcrdtTextSynchronizer.MapCaretThroughDelta(6, observed));
    }
}
