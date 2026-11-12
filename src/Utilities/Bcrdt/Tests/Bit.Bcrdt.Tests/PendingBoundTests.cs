using System.Collections.Concurrent;
using System.Diagnostics;
using Bit.Bcrdt.Server;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// <see cref="BcrdtDocumentOptions.MaxPendingSize"/>: changes waiting for their dependencies are bounded,
/// and discarding them never costs convergence.
/// </summary>
[TestClass]
public class PendingBoundTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(15);

    /// <summary>Two consecutive updates from one author: the second builds on the first.</summary>
    private static (BcrdtDocument Author, byte[] First, byte[] Second) TwoUpdates(ulong client, int length = 64)
    {
        var author = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = client });
        List<byte[]> updates = [];
        author.Updated += e => updates.Add(e.Update);

        author.GetText("body").Insert(0, new string('a', length));
        author.GetText("body").Insert(length, new string('b', length));
        return (author, updates[0], updates[1]);
    }

    [TestMethod]
    public void Waiting_changes_beyond_the_bound_are_discarded_and_reported()
    {
        using var reader = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1, MaxPendingSize = 1024 });
        int reported = 0;
        reader.PendingUpdatesDiscarded += _ => reported++;

        (BcrdtDocument author, _, byte[] second) = TwoUpdates(2, length: 4000);
        using (author)
        {
            // It builds on a change the reader never got, so it can only wait - and it is too big to.
            reader.ApplyUpdate(second);

            Assert.IsFalse(reader.HasPendingUpdates);
            Assert.AreEqual(1, reader.PendingDiscards);
            Assert.AreEqual(1, reported);
            Assert.AreEqual(0, reader.GetStateVector().Get(2), "nothing of the discarded change may count as received");

            // The next exchange brings everything back.
            reader.ApplyUpdate(author.EncodeStateAsUpdate(reader.GetStateVector()));
            Assert.AreEqual(author.GetText("body").ToString(), reader.GetText("body").ToString());
        }
    }

    [TestMethod]
    public void Waiting_changes_within_the_bound_are_kept_and_applied_when_their_dependency_arrives()
    {
        using var reader = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        (BcrdtDocument author, byte[] first, byte[] second) = TwoUpdates(2);
        using (author)
        {
            reader.ApplyUpdate(second);
            Assert.IsTrue(reader.HasPendingUpdates);

            reader.ApplyUpdate(first);
            Assert.IsFalse(reader.HasPendingUpdates);
            Assert.AreEqual(0, reader.PendingDiscards);
            Assert.AreEqual(author.GetText("body").ToString(), reader.GetText("body").ToString());
        }
    }

    [TestMethod]
    public void A_bound_of_zero_keeps_everything()
    {
        using var reader = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1, MaxPendingSize = 0 });
        (BcrdtDocument author, _, byte[] second) = TwoUpdates(2, length: 100_000);
        using (author)
        {
            reader.ApplyUpdate(second);
            Assert.IsTrue(reader.HasPendingUpdates);
            Assert.AreEqual(0, reader.PendingDiscards);
        }
    }

    [TestMethod]
    public void Many_small_waiting_changes_are_bounded_together()
    {
        using var reader = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1, MaxPendingSize = 4096 });

        // Each is small, but they pile up: a peer sending changes that build on nothing it will ever send.
        using var author = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        List<byte[]> updates = [];
        author.Updated += e => updates.Add(e.Update);
        for (int i = 0; i < 200; i++) author.GetArray("items").Push(new string('x', 100));

        foreach (byte[] update in updates.Skip(1)) reader.ApplyUpdate(update);

        Assert.IsGreaterThanOrEqualTo(1, reader.PendingDiscards);
        Assert.IsLessThanOrEqualTo(4096 + 512, reader.Store.PendingUpdate?.Length ?? 0);

        reader.ApplyUpdate(author.EncodeStateAsUpdate(reader.GetStateVector()));
        Assert.AreSequenceEqual(author.GetArray("items").ToArray(), reader.GetArray("items").ToArray());
    }

    [TestMethod]
    public void A_discarded_deletion_is_restored_by_the_next_exchange()
    {
        // The insertion and its deletion reach the reader by different routes, deletion first.
        using var inserter = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        using var deleter = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 });
        using var reader = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1, MaxPendingSize = 1 });

        byte[]? insertion = null;
        inserter.Updated += e => insertion = e.Update;
        inserter.GetText("body").Insert(0, "gone soon");

        deleter.ApplyUpdate(insertion!);
        byte[]? deletion = null;
        deleter.Updated += e => deletion = e.Update;
        deleter.GetText("body").Delete(0, 9);

        reader.ApplyUpdate(deletion!);
        Assert.AreEqual(1, reader.PendingDiscards, "a deletion of something not received yet waits, and the bound is one byte");

        reader.ApplyUpdate(insertion!);
        Assert.AreEqual("gone soon", reader.GetText("body").ToString(), "the deletion was discarded, so for now the text shows");

        // Any exchange carries the complete delete set, which is what repairs it.
        reader.ApplyUpdate(deleter.EncodeStateAsUpdate(reader.GetStateVector()));
        Assert.AreEqual("", reader.GetText("body").ToString());
    }

    [TestMethod]
    public async Task A_provider_asks_for_what_its_document_discarded()
    {
        var network = new BcrdtInMemoryNetwork();
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1, MaxPendingSize = 1024 });

        BcrdtInMemoryTransport rawTransport = network.CreateTransport("room");
        ConcurrentQueue<BcrdtSyncMessage> heard = new();
        rawTransport.MessageReceived += message => heard.Enqueue(BcrdtSyncProtocol.Decode(message.ToArray()));
        await rawTransport.ConnectAsync(TestContext.CancellationToken);

        await using BcrdtSyncProvider provider = await Task.Run(() => new BcrdtSyncProvider(doc, network.CreateTransport("room"), null, new BcrdtSyncProviderOptions
        {
            HeartbeatInterval = TimeSpan.Zero,
            HandshakeTimeout = TimeSpan.FromMilliseconds(50),
            SyncAwareness = false,
        }));
        await provider.ConnectAsync(TestContext.CancellationToken);
        Assert.IsTrue(await EventuallyAsync(() => provider.Status == BcrdtConnectionStatus.Synced), "nobody answers, so it settles alone");
        heard.Clear();

        (BcrdtDocument author, _, byte[] second) = TwoUpdates(2, length: 4000);
        using (author)
        {
            await rawTransport.SendAsync(BcrdtSyncProtocol.EncodeUpdate(second), TestContext.CancellationToken);

            Assert.IsTrue(
                await EventuallyAsync(() => heard.Any(message => message.Kind == BcrdtSyncMessageKind.SyncStep1)),
                "a document that discarded waiting changes must ask the room for them again");
            Assert.AreEqual(1, doc.PendingDiscards);
        }
    }

    [TestMethod]
    public async Task A_document_host_bounds_what_waits_and_asks_the_sender_for_the_rest()
    {
        await using var host = new BcrdtDocumentHost(new BcrdtDocumentHostOptions { MaxPendingSize = 1024 });
        var connection = new RecordingConnection();
        using BcrdtHostSession session = await host.JoinAsync("bounded", connection, TestContext.CancellationToken);

        (BcrdtDocument author, byte[] first, byte[] second) = TwoUpdates(2, length: 4000);
        using (author)
        {
            Assert.IsTrue(await session.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(second)));

            Assert.IsTrue(await host.ReadAsync("bounded", doc => doc.PendingDiscards == 1 && !doc.HasPendingUpdates, TestContext.CancellationToken));
            Assert.Contains(
                message => message.Kind == BcrdtSyncMessageKind.SyncStep1, connection.Sent.Select(message => BcrdtSyncProtocol.Decode(message)),
                "the sender holds what the change builds on, so it is asked");

            Assert.IsTrue(await session.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(first)));
            Assert.IsTrue(await session.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(second)));
            Assert.AreEqual(author.GetText("body").ToString(), await host.ReadAsync("bounded", doc => doc.GetText("body").ToString(), TestContext.CancellationToken));
        }
    }

    [TestMethod]
    public async Task A_document_host_that_discards_what_waits_asks_every_connection_for_it()
    {
        await using var host = new BcrdtDocumentHost(new BcrdtDocumentHostOptions { MaxPendingSize = 1024 });
        var flooder = new RecordingConnection("flooder");
        var bystander = new RecordingConnection("bystander");
        using BcrdtHostSession flooding = await host.JoinAsync("bounded", flooder, TestContext.CancellationToken);
        using BcrdtHostSession waiting = await host.JoinAsync("bounded", bystander, TestContext.CancellationToken);
        bystander.Sent.Clear();

        (BcrdtDocument author, _, byte[] second) = TwoUpdates(2, length: 4000);
        using (author)
        {
            Assert.IsTrue(await flooding.ReceiveAsync(BcrdtSyncProtocol.EncodeUpdate(second)));

            Assert.IsTrue(await host.ReadAsync("bounded", doc => doc.PendingDiscards == 1, TestContext.CancellationToken));
            Assert.Contains(
                message => message.Kind == BcrdtSyncMessageKind.SyncStep1, bystander.Sent.Select(message => BcrdtSyncProtocol.Decode(message)),
                "what was discarded may have come from any connection, so every one is asked");
        }
    }

    private static async Task<bool> EventuallyAsync(Func<bool> condition)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            if (clock.Elapsed > Patience) return condition();
            await Task.Delay(10);
        }
        return true;
    }

    private sealed class RecordingConnection(string id = "recording") : IBcrdtHostConnection
    {
        public ConcurrentQueue<byte[]> Sent { get; } = new();

        public string Id => id;

        public BcrdtDocumentAccess Access => BcrdtDocumentAccess.ReadWrite;

        public string? UserId => null;

        public string Relay => "test";

        public void Send(byte[] message) => Sent.Enqueue(message);

        public void Refuse(string reason) { }
    }

    public TestContext TestContext { get; set; }
}
