using Bit.Bcrdt.BlazeDb;
using Bit.BlazeDb;
using Bit.BlazeDb.Storage;
using Microsoft.Extensions.DependencyInjection;
using static Bit.Bcrdt.Tests.HandWrittenUpdates;

namespace Bit.Bcrdt.Tests.Integrations;

/// <summary>
/// The store contract, held against a store that keeps documents in a BlazeDb database.
/// </summary>
/// <remarks>
/// Crashes are simulated the way BlazeDb's own tests simulate them: the in-memory backend's files are copied as
/// they are at the moment of the crash, and a database is opened over the copy - so nothing the original instance
/// does afterwards, its shutdown flush included, can reach the bytes being recovered.
/// </remarks>
[TestClass]
public class BlazeDbDocumentStoreTests
{
    private static ValueTask<BlazeDbDatabase> OpenAsync(IBlazeDbStorage? storage = null, bool readOnly = false, TimeSpan? flushInterval = null) =>
        BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            ReadOnly = readOnly,
            FlushInterval = flushInterval ?? TimeSpan.FromMilliseconds(100),
        }.AddTable(BcrdtBlazeDbDocumentStore.Table));

    private static byte[] CreateUpdate(string content, ulong clientId = 1)
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
        doc.GetText("body").Insert(0, content);
        return doc.EncodeStateAsUpdate();
    }

    private static string Text(byte[] update)
    {
        using var doc = new BcrdtDocument();
        doc.ApplyUpdate(update);
        return doc.GetText("body").ToString();
    }

    private static List<BcrdtBlazeDbUpdateRecord> Rows(BlazeDbDatabase database, string documentId) =>
        [.. database.GetTable(BcrdtBlazeDbUpdateRecord.Table).Lookup(BcrdtBlazeDbUpdateRecord.Indexes.DocumentId, documentId)];

    /// <summary>A copy of the storage with the last <paramref name="bytes"/> of its log cut off, as a crash mid-write leaves it.</summary>
    private static BlazeDbInMemoryStorage TearLog(BlazeDbInMemoryStorage storage, int bytes)
    {
        BlazeDbInMemoryStorage torn = storage.Clone();
        string log = torn.FileNames.Single(name => name.StartsWith("wal", StringComparison.Ordinal));
        byte[] contents = torn.GetFile(log)!;
        torn.SetFile(log, contents[..^bytes]);
        return torn;
    }

    [TestMethod]
    public async Task Appended_updates_load_back_as_one_merged_update_with_their_record_count()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var store = new BcrdtBlazeDbDocumentStore(database);

        Assert.IsTrue((await store.LoadAsync("doc")).IsEmpty);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        doc.Updated += args => store.AppendUpdateAsync("doc", args.Update).AsTask().GetAwaiter().GetResult();
        doc.GetText("body").Insert(0, "hello");
        doc.GetText("body").Insert(5, " world");
        doc.GetText("body").Delete(0, 1);

        BcrdtStoredDocument stored = await store.LoadAsync("doc");

        Assert.AreEqual(3, stored.RecordCount);
        Assert.AreEqual("ello world", Text(stored.Update!));
        Assert.HasCount(3, Rows(database, "doc"));
    }

    [TestMethod]
    public async Task Compaction_replaces_every_record_with_one_merged_update()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var store = new BcrdtBlazeDbDocumentStore(database);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        doc.Updated += args => store.AppendUpdateAsync("doc", args.Update).AsTask().GetAwaiter().GetResult();
        for (int i = 0; i < 20; i++) doc.GetText("body").Insert(i, ((char)('a' + i)).ToString());

        await store.CompactAsync("doc");
        BcrdtStoredDocument stored = await store.LoadAsync("doc");

        Assert.AreEqual(1, stored.RecordCount);
        Assert.AreEqual(doc.GetText("body").ToString(), Text(stored.Update!));
        Assert.HasCount(1, Rows(database, "doc"));

        // A single record has nothing to merge with.
        await store.CompactAsync("doc");
        Assert.AreEqual(1, (await store.LoadAsync("doc")).RecordCount);
    }

    [TestMethod]
    public async Task A_crash_during_compaction_leaves_either_every_record_or_the_merged_one()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using BlazeDbDatabase database = await OpenAsync(storage);
        var store = new BcrdtBlazeDbDocumentStore(database);

        string[] words = ["one", "two", "three", "four", "five"];
        for (int i = 0; i < words.Length; i++) await store.AppendUpdateAsync("doc", CreateUpdate(words[i], clientId: (ulong)(i + 1)));
        string expected = Text((await store.LoadAsync("doc")).Update!);

        BlazeDbInMemoryStorage before = storage.Clone();
        await store.CompactAsync("doc");
        BlazeDbInMemoryStorage after = storage.Clone();

        // The merged record and the deletions are one commit, so cutting into it takes all of it back.
        foreach ((BlazeDbInMemoryStorage crashed, int records) in new[] { (before, 5), (after, 1), (TearLog(after, 1), 5), (TearLog(after, 9), 5) })
        {
            await using BlazeDbDatabase recovered = await OpenAsync(crashed);
            BcrdtStoredDocument stored = await new BcrdtBlazeDbDocumentStore(recovered).LoadAsync("doc");

            Assert.AreEqual(records, stored.RecordCount);
            Assert.AreEqual(expected, Text(stored.Update!));
        }
    }

    [TestMethod]
    public async Task An_append_survives_a_crash_once_it_completes_and_a_torn_one_costs_only_itself()
    {
        var storage = new BlazeDbInMemoryStorage();

        // The background flusher is held off, so only the store's own wait can have made the appends durable.
        await using BlazeDbDatabase database = await OpenAsync(storage, flushInterval: TimeSpan.FromHours(1));
        var store = new BcrdtBlazeDbDocumentStore(database);

        await store.AppendUpdateAsync("doc", CreateUpdate("first", clientId: 1));
        await store.AppendUpdateAsync("doc", CreateUpdate("second", clientId: 2));

        await using (BlazeDbDatabase recovered = await OpenAsync(storage.Clone()))
        {
            Assert.AreEqual(2, (await new BcrdtBlazeDbDocumentStore(recovered).LoadAsync("doc")).RecordCount);
        }

        await using (BlazeDbDatabase recovered = await OpenAsync(TearLog(storage, 3)))
        {
            BcrdtStoredDocument stored = await new BcrdtBlazeDbDocumentStore(recovered).LoadAsync("doc");
            Assert.AreEqual(1, stored.RecordCount);
            Assert.AreEqual("first", Text(stored.Update!));
        }
    }

    [TestMethod]
    public async Task Without_waiting_for_durability_an_append_is_durable_once_the_database_flushes()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using BlazeDbDatabase database = await OpenAsync(storage, flushInterval: TimeSpan.FromHours(1));
        var store = new BcrdtBlazeDbDocumentStore(database, new BcrdtBlazeDbDocumentStoreOptions { WaitForDurability = false });

        await store.AppendUpdateAsync("doc", CreateUpdate("buffered"));

        await using (BlazeDbDatabase recovered = await OpenAsync(storage.Clone()))
        {
            Assert.IsTrue((await new BcrdtBlazeDbDocumentStore(recovered).LoadAsync("doc")).IsEmpty);
        }

        await database.FlushAsync();

        await using (BlazeDbDatabase recovered = await OpenAsync(storage.Clone()))
        {
            Assert.AreEqual("buffered", Text((await new BcrdtBlazeDbDocumentStore(recovered).LoadAsync("doc")).Update!));
        }
    }

    [TestMethod]
    public async Task Documents_are_listed_and_kept_apart_and_deleting_one_leaves_the_others()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var store = new BcrdtBlazeDbDocumentStore(database);

        await store.AppendUpdateAsync("first", CreateUpdate("one", clientId: 1));
        await store.AppendUpdateAsync("second", CreateUpdate("two", clientId: 2));
        await store.AppendUpdateAsync("first", CreateUpdate("three", clientId: 3));

        CollectionAssert.AreEquivalent(new[] { "first", "second" }, (await store.ListAsync()).ToArray());
        Assert.AreEqual(2, (await store.LoadAsync("first")).RecordCount);
        Assert.AreEqual("two", Text((await store.LoadAsync("second")).Update!));

        await store.CompactAsync("first");
        Assert.AreEqual(1, (await store.LoadAsync("second")).RecordCount);

        await store.DeleteAsync("first");
        await store.DeleteAsync("never-stored");

        CollectionAssert.AreEquivalent(new[] { "second" }, (await store.ListAsync()).ToArray());
        Assert.IsTrue((await store.LoadAsync("first")).IsEmpty);
        Assert.AreEqual("two", Text((await store.LoadAsync("second")).Update!));
    }

    [TestMethod]
    public async Task Untrusted_document_identifiers_are_stored_exactly_as_given_and_never_collide()
    {
        var storage = new BlazeDbInMemoryStorage();
        string[] ids =
        [
            "../../etc/passwd", "..\\..\\windows\\system32", "a/b", "A", "a", " ", "con", "nul:", "\0", "é", "é",
            "😀", new string('x', 10_000), "%2e%2e%2f", "*?<>|\"",
        ];

        await using (BlazeDbDatabase database = await OpenAsync(storage))
        {
            var store = new BcrdtBlazeDbDocumentStore(database);
            for (int i = 0; i < ids.Length; i++) await store.AppendUpdateAsync(ids[i], CreateUpdate($"doc-{i}"));
        }

        await using BlazeDbDatabase reopened = await OpenAsync(storage);
        var reloaded = new BcrdtBlazeDbDocumentStore(reopened);

        CollectionAssert.AreEquivalent(ids, (await reloaded.ListAsync()).ToArray());
        for (int i = 0; i < ids.Length; i++)
        {
            BcrdtStoredDocument stored = await reloaded.LoadAsync(ids[i]);
            Assert.AreEqual(1, stored.RecordCount);
            Assert.AreEqual($"doc-{i}", Text(stored.Update!));
        }

        // A lone surrogate would be written as a replacement character and read back as a different identifier.
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await reloaded.AppendUpdateAsync("\ud800", CreateUpdate("lost")));
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () => await reloaded.LoadAsync(""));
    }

    [TestMethod]
    public async Task A_row_that_is_not_an_update_is_skipped_reported_and_never_removed_by_compaction()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var store = new BcrdtBlazeDbDocumentStore(database);
        var skipped = new List<string>();
        store.UnreadableRecordSkipped += skipped.Add;

        await store.AppendUpdateAsync("doc", CreateUpdate("readable", clientId: 1));

        // Written by something else, under the very number the store would have used next.
        byte[] garbage = [0xFF, 0xFF, 0xFF, 0xFF];
        database.GetTable(BcrdtBlazeDbUpdateRecord.Table).Insert(new BcrdtBlazeDbUpdateRecord { Id = 2, DocumentId = "doc", Update = garbage });
        await store.AppendUpdateAsync("doc", CreateUpdate("also readable", clientId: 2));

        CollectionAssert.AreEquivalent(new long[] { 1, 2, 3 }, Rows(database, "doc").Select(row => row.Id).ToArray());

        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        Assert.AreEqual(2, stored.RecordCount);
        StringAssert.Contains(Text(stored.Update!), "readable");
        CollectionAssert.AreEqual(new[] { "doc" }, skipped);

        await store.CompactAsync("doc");

        // The merged record, in the first one's place, and the row it could not merge, untouched.
        List<BcrdtBlazeDbUpdateRecord> rows = Rows(database, "doc");
        Assert.HasCount(2, rows);
        Assert.IsTrue(rows.Any(row => row.Id == 2 && row.Update.SequenceEqual(garbage)));
        Assert.IsTrue(rows.Any(row => row.Id == 1));
        Assert.AreEqual(1, (await store.LoadAsync("doc")).RecordCount);
    }

    [TestMethod]
    public async Task A_record_too_far_past_the_ones_before_it_is_skipped_and_the_rest_load()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var store = new BcrdtBlazeDbDocumentStore(database);
        var skipped = new List<string>();
        store.UnreadableRecordSkipped += skipped.Add;

        // Each reads on its own, but the second leaves a hole in its author's clock range wider than any replica
        // writes, so no merge that includes it succeeds.
        await store.AppendUpdateAsync("doc", CollectedRange(7, 0));
        await store.AppendUpdateAsync("doc", CollectedRange(7, 2 + (ulong)BcrdtUpdateCodec.MaxClockGap));
        await store.AppendUpdateAsync("doc", CreateUpdate("kept"));

        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        using var reloaded = new BcrdtDocument();
        reloaded.ApplyUpdate(stored.Update!);

        Assert.AreEqual(2, stored.RecordCount);
        Assert.AreEqual("kept", reloaded.GetText("body").ToString());
        Assert.AreEqual(1L, reloaded.GetStateVector().Get(7));
        Assert.HasCount(1, skipped);

        await store.CompactAsync("doc");
        Assert.HasCount(2, Rows(database, "doc"));
        Assert.AreEqual("kept", Text((await store.LoadAsync("doc")).Update!));
    }

    [TestMethod]
    public async Task A_write_never_joins_a_transaction_the_application_has_open()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var store = new BcrdtBlazeDbDocumentStore(database);
        await store.AppendUpdateAsync("doc", CreateUpdate("before", clientId: 1));
        await store.AppendUpdateAsync("doc", CreateUpdate("and before", clientId: 4));

        using (BlazeDbTransaction transaction = database.BeginTransaction())
        {
            // Joining it would let the rollback below take back an update the store had reported stored.
            await Assert.ThrowsExactlyAsync<IOException>(async () => await store.AppendUpdateAsync("doc", CreateUpdate("during", clientId: 2)));
            await Assert.ThrowsExactlyAsync<IOException>(async () => await store.CompactAsync("doc"));
            transaction.Rollback();
        }

        await store.AppendUpdateAsync("doc", CreateUpdate("after", clientId: 3));
        Assert.AreEqual(3, (await store.LoadAsync("doc")).RecordCount);
    }

    [TestMethod]
    public async Task Persistence_retries_an_update_refused_while_the_application_held_a_transaction()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var store = new BcrdtBlazeDbDocumentStore(database);
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(
            doc, store, "doc", new BcrdtDocumentPersistenceOptions { RetryDelay = TimeSpan.FromMilliseconds(10) });

        var failures = new List<Exception>();
        persistence.WriteFailed += failures.Add;

        BlazeDbTransaction transaction = database.BeginTransaction();
        doc.GetText("body").Insert(0, "kept for later");
        await WaitUntilAsync(() => failures.Count > 0);
        transaction.Dispose();

        await persistence.FlushAsync();
        Assert.AreEqual("kept for later", Text((await store.LoadAsync("doc")).Update!));
        Assert.IsInstanceOfType<IOException>(failures[0]);
    }

    [TestMethod]
    public async Task A_read_only_replica_loads_documents_and_refuses_to_change_them()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (BlazeDbDatabase writer = await OpenAsync(storage))
        {
            await new BcrdtBlazeDbDocumentStore(writer).AppendUpdateAsync("doc", CreateUpdate("from the writer"));
        }

        await using BlazeDbDatabase replica = await OpenAsync(storage, readOnly: true);
        var store = new BcrdtBlazeDbDocumentStore(replica);

        Assert.AreEqual("from the writer", Text((await store.LoadAsync("doc")).Update!));
        CollectionAssert.AreEqual(new[] { "doc" }, (await store.ListAsync()).ToArray());

        // Refused outright, so the persistence layer reports it once instead of retrying forever.
        await Assert.ThrowsExactlyAsync<NotSupportedException>(async () => await store.AppendUpdateAsync("doc", CreateUpdate("from a reader", clientId: 2)));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(async () => await store.DeleteAsync("doc"));
    }

    [TestMethod]
    public async Task A_database_without_the_table_is_reported_with_how_to_register_it()
    {
        await using BlazeDbDatabase database = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions());
        var store = new BcrdtBlazeDbDocumentStore(database);

        InvalidOperationException error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await store.LoadAsync("doc"));
        StringAssert.Contains(error.Message, "AddTable(BcrdtBlazeDbDocumentStore.Table)");
    }

    [TestMethod]
    public async Task A_throwing_application_change_handler_does_not_fail_a_write_that_committed()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var store = new BcrdtBlazeDbDocumentStore(database);
        var reported = new List<BlazeDbChangeHandlerException>();
        store.ChangeHandlerFailed += reported.Add;
        database.Changed += (_, _) => throw new InvalidOperationException("an application bug");

        await store.AppendUpdateAsync("doc", CreateUpdate("stored once"));

        Assert.HasCount(1, Rows(database, "doc"));
        Assert.HasCount(1, reported);
        Assert.AreEqual("stored once", Text((await store.LoadAsync("doc")).Update!));
    }

    [TestMethod]
    public async Task A_failing_flush_is_retried_without_committing_a_copy_on_every_attempt()
    {
        var storage = new FailingStorage();
        await using BlazeDbDatabase database = await OpenAsync(storage, flushInterval: TimeSpan.FromHours(1));
        var store = new BcrdtBlazeDbDocumentStore(database);
        await store.AppendUpdateAsync("doc", CreateUpdate("stored"));

        storage.Failing = true;
        byte[] update = CreateUpdate("retried", clientId: 2);
        await Assert.ThrowsExactlyAsync<IOException>(async () => await store.AppendUpdateAsync("doc", update));
        await Assert.ThrowsExactlyAsync<IOException>(async () => await store.AppendUpdateAsync("doc", update));
        await Assert.ThrowsExactlyAsync<IOException>(async () => await store.AppendUpdateAsync("doc", update));

        // The first attempt committed; the retries waited for its flush instead of committing again.
        Assert.HasCount(2, Rows(database, "doc"));

        storage.Failing = false;
        await store.AppendUpdateAsync("doc", update);

        Assert.HasCount(3, Rows(database, "doc"));
        await using BlazeDbDatabase recovered = await OpenAsync(storage.Inner.Clone());
        StringAssert.Contains(Text((await new BcrdtBlazeDbDocumentStore(recovered).LoadAsync("doc")).Update!), "retried");
    }

    [TestMethod]
    public async Task The_application_lock_serializes_the_store_with_the_application()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        using var gate = new SemaphoreSlim(1, 1);
        var store = new BcrdtBlazeDbDocumentStore(database, new BcrdtBlazeDbDocumentStoreOptions { Gate = gate });

        await gate.WaitAsync();
        Task append = store.AppendUpdateAsync("doc", CreateUpdate("waits")).AsTask();
        await Task.Delay(50);

        Assert.IsFalse(append.IsCompleted);
        Assert.IsEmpty(Rows(database, "doc"));

        gate.Release();
        await append;
        Assert.HasCount(1, Rows(database, "doc"));
    }

    [TestMethod]
    public async Task Stores_sharing_a_database_never_reuse_a_record_number()
    {
        await using BlazeDbDatabase database = await OpenAsync();
        var first = new BcrdtBlazeDbDocumentStore(database);
        var second = new BcrdtBlazeDbDocumentStore(_ => ValueTask.FromResult(database));

        await Task.WhenAll(Enumerable.Range(0, 40).Select(i =>
            (i % 2 == 0 ? first : second).AppendUpdateAsync($"doc-{i % 4}", CreateUpdate($"{i}", clientId: (ulong)(i + 1))).AsTask()));

        Assert.AreEqual(40, database.GetTable(BcrdtBlazeDbUpdateRecord.Table).Count);
        Assert.AreEqual(10, (await second.LoadAsync("doc-3")).RecordCount);
    }

    [TestMethod]
    public async Task A_document_round_trips_through_persistence_and_compacts_at_the_threshold()
    {
        var storage = new BlazeDbInMemoryStorage();
        string expected;

        await using (BlazeDbDatabase database = await OpenAsync(storage))
        {
            var store = new BcrdtBlazeDbDocumentStore(database);
            using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(
                doc, store, "notes", new BcrdtDocumentPersistenceOptions { CompactionThreshold = 5 });

            for (int i = 0; i < 23; i++)
            {
                doc.GetText("body").Insert(doc.GetText("body").Length, $"{i};");
                await persistence.FlushAsync();
            }
            doc.GetMap("meta").Set("title", "Notes");
            await persistence.FlushAsync();

            Assert.IsLessThan(5, persistence.RecordCount);
            Assert.IsLessThan(5, Rows(database, "notes").Count);
            expected = BcrdtConvergenceHarness.Describe(doc);
        }

        await using BlazeDbDatabase reopened = await OpenAsync(storage);
        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using BcrdtDocumentPersistence reattached = await BcrdtDocumentPersistence.AttachAsync(reloaded, new BcrdtBlazeDbDocumentStore(reopened), "notes");

        Assert.AreEqual(expected, BcrdtConvergenceHarness.Describe(reloaded));
    }

    [TestMethod]
    public async Task A_replica_kept_in_BlazeDb_reopens_offline_and_converges_with_a_second_replica()
    {
        var network = new BcrdtInMemoryNetwork();
        var storage = new BlazeDbInMemoryStorage();
        BlazeDbDatabase database = await OpenAsync(storage);

        await using BcrdtCollaborationService other = new(Options(connect: true), new RoomTransportFactory(network));
        BcrdtCollaborativeDocument b = await other.OpenAsync("shared");

        await using (ServiceProvider services = Services(network, database, connect: true))
        await using (AsyncServiceScope scope = services.CreateAsyncScope())
        {
            Assert.IsInstanceOfType<BcrdtBlazeDbDocumentStore>(scope.ServiceProvider.GetRequiredService<IBcrdtDocumentStore>());

            BcrdtCollaborativeDocument a = await scope.ServiceProvider.GetRequiredService<IBcrdtCollaborationService>().OpenAsync("shared");
            a.Text("body").Insert(0, "written by A");
            await WaitUntilAsync(() => b.Text("body").ToString() == "written by A");
        }

        // The application restarts: the database is closed, and opened again from what reached its storage.
        await database.DisposeAsync();
        database = await OpenAsync(storage);

        await using (ServiceProvider services = Services(network, database, connect: false))
        await using (AsyncServiceScope scope = services.CreateAsyncScope())
        {
            BcrdtCollaborativeDocument a = await scope.ServiceProvider.GetRequiredService<IBcrdtCollaborationService>().OpenAsync("shared");
            Assert.AreEqual("written by A", a.Text("body").ToString());

            // Both sides change the document while A is offline; reconnecting reconciles both ways.
            a.Array("items").Push("offline in A");
            b.Array("items").Push("online in B");
            await a.ConnectAsync();
            await WaitUntilAsync(() => a.Array("items").Count == 2 && b.Array("items").Count == 2);

            BcrdtConvergenceHarness.AssertConverged(a.Crdt, b.Crdt);
        }

        await database.DisposeAsync();
    }

    private static ServiceProvider Services(BcrdtInMemoryNetwork network, BlazeDbDatabase database, bool connect)
    {
        var services = new ServiceCollection();
        services.AddSingleton(database);
        services.AddBitBcrdtServices(collaboration => collaboration
            .Configure(options =>
            {
                options.ConnectOnOpen = connect;
                options.CreateUndoManager = false;
                options.Sync = new BcrdtSyncProviderOptions { HeartbeatInterval = TimeSpan.Zero };
            })
            .UseTransport(new RoomTransportFactory(network))
            .UseBlazeDbStore((provider, _) => new(Task.FromResult(provider.GetRequiredService<BlazeDbDatabase>()))));
        return services.BuildServiceProvider();
    }

    private static BcrdtCollaborationOptions Options(bool connect) => new()
    {
        ConnectOnOpen = connect,
        CreateUndoManager = false,
        Sync = new BcrdtSyncProviderOptions { HeartbeatInterval = TimeSpan.Zero },
    };

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int i = 0; i < 500 && !condition(); i++) await Task.Delay(10);
        Assert.IsTrue(condition(), "The condition was not met in time.");
    }

    private sealed class RoomTransportFactory(BcrdtInMemoryNetwork network) : IBcrdtTransportFactory
    {
        public IBcrdtTransport Create(string documentId) => network.CreateTransport(documentId);
    }

    /// <summary>A backend whose appends fail while <see cref="Failing"/> is set, as a full quota makes them.</summary>
    private sealed class FailingStorage : IBlazeDbStorage
    {
        public BlazeDbInMemoryStorage Inner { get; } = new();

        public bool Failing { get; set; }

        public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) => Inner.ReadAsync(name, cancellationToken);

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            Failing ? throw new IOException("The disk is full.") : Inner.WriteAtomicAsync(name, data, cancellationToken);

        public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            Failing ? throw new IOException("The disk is full.") : Inner.AppendAsync(name, data, cancellationToken);

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) => Inner.DeleteAsync(name, cancellationToken);
    }
}
