using Microsoft.Extensions.Time.Testing;
using static Bit.Bcrdt.Tests.HandWrittenUpdates;

namespace Bit.Bcrdt.Tests;

/// <summary>
/// Storing a document, losing power halfway through, and getting it back.
/// </summary>
/// <remarks>
/// The store's promises are narrow on purpose: appends are cheap and crash-safe, compaction is
/// lossless, and a torn trailing record costs at most the last update. These tests hold it to
/// exactly those promises, including the crash case, which is the one that never gets exercised in
/// normal use and therefore never gets found in normal testing.
/// </remarks>
[TestClass]
public class PersistenceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "bit-bcrdt-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    public static IEnumerable<object[]> StoreKinds => [[true], [false]];

    private IBcrdtDocumentStore CreateStore(bool useFileSystem) =>
        useFileSystem ? new BcrdtFileSystemDocumentStore(_root) : new BcrdtInMemoryDocumentStore();

    [TestMethod]
    [DynamicData(nameof(StoreKinds))]
    public async Task A_document_survives_being_closed_and_reopened(bool useFileSystem)
    {
        IBcrdtDocumentStore store = CreateStore(useFileSystem);

        using (var doc = new BcrdtDocument(new BcrdtDocumentOptions { Id = "doc", ClientId = 1 }))
        {
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");

            doc.GetText("body").Insert(0, "persisted text");
            doc.GetMap("meta").Set("title", "Example");

            await persistence.FlushAsync();
        }

        using var reopened = new BcrdtDocument(new BcrdtDocumentOptions { Id = "doc", ClientId = 2 });
        await using BcrdtDocumentPersistence reloaded = await BcrdtDocumentPersistence.AttachAsync(reopened, store, "doc");

        Assert.AreEqual("persisted text", reopened.GetText("body").ToString());
        Assert.AreEqual("Example", reopened.GetMap("meta").Get("title"));
    }

    [TestMethod]
    [DynamicData(nameof(StoreKinds))]
    public async Task Edits_made_before_loading_are_merged_rather_than_overwritten(bool useFileSystem)
    {
        IBcrdtDocumentStore store = CreateStore(useFileSystem);

        using (var seed = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        {
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(seed, store, "doc");
            seed.GetArray("items").Push("stored");
            await persistence.FlushAsync();
        }

        // The user started editing offline before storage had finished loading. Neither side may win
        // outright; the two have to merge.
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        doc.GetArray("items").Push("unsaved");

        await using BcrdtDocumentPersistence reloaded = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");

        List<object?> items = doc.GetArray("items").ToArray();
        Assert.Contains("stored", items);
        Assert.Contains("unsaved", items);
    }

    [TestMethod]
    [DynamicData(nameof(StoreKinds))]
    public async Task Compaction_preserves_the_document_exactly(bool useFileSystem)
    {
        IBcrdtDocumentStore store = CreateStore(useFileSystem);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using (BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc"))
        {
            var generator = new BcrdtOperationGenerator(7);
            for (int i = 0; i < 60; i++)
            {
                generator.ApplyRandomTransaction(doc);
                await persistence.FlushAsync();
            }
            await persistence.FlushAsync();
        }

        string before = BcrdtConvergenceHarness.Describe(doc);
        await store.CompactAsync("doc");

        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        reloaded.ApplyUpdate(stored.Update!);

        Assert.AreEqual(1, stored.RecordCount);
        Assert.AreEqual(before, BcrdtConvergenceHarness.Describe(reloaded));
    }

    [TestMethod]
    public async Task A_torn_trailing_record_is_discarded_and_the_rest_still_loads()
    {
        var store = new BcrdtFileSystemDocumentStore(_root);

        using (var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        {
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");
            doc.GetText("body").Insert(0, "first");
            await persistence.FlushAsync();
            doc.GetText("body").Insert(5, " second");
            await persistence.FlushAsync();
        }

        // Simulate losing power partway through an append.
        string logPath = Directory.EnumerateFiles(_root, "*.bcl").Single();
        byte[] contents = await File.ReadAllBytesAsync(logPath);
        await File.WriteAllBytesAsync(logPath, [.. contents, 0x40, 0x00, 0x00, 0x00, 0xDE, 0xAD, 0xBE]);

        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        await using BcrdtDocumentPersistence recovery = await BcrdtDocumentPersistence.AttachAsync(reloaded, store, "doc");

        Assert.AreEqual("first second", reloaded.GetText("body").ToString());
    }

    [TestMethod]
    public async Task A_record_whose_checksum_does_not_match_is_discarded()
    {
        var store = new BcrdtFileSystemDocumentStore(_root);

        using (var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        {
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");
            doc.GetText("body").Insert(0, "good");
            await persistence.FlushAsync();
        }

        string logPath = Directory.EnumerateFiles(_root, "*.bcl").Single();
        byte[] contents = await File.ReadAllBytesAsync(logPath);
        contents[^1] ^= 0xFF;
        await File.WriteAllBytesAsync(logPath, contents);

        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        Assert.IsTrue(stored.IsEmpty, "A record with a broken checksum must not be handed to the decoder.");
    }

    [TestMethod]
    public async Task A_document_identifier_containing_path_separators_cannot_escape_the_store()
    {
        var store = new BcrdtFileSystemDocumentStore(_root);

        // A document identifier is caller-supplied and often comes from a URL.
        await store.AppendUpdateAsync("../../escaped", new byte[] { 1, 2, 3 });

        foreach (string path in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            Assert.StartsWith(_root, Path.GetFullPath(path), StringComparison.Ordinal);
        }
    }

    [TestMethod]
    [DynamicData(nameof(StoreKinds))]
    public async Task Documents_can_be_listed_and_deleted(bool useFileSystem)
    {
        IBcrdtDocumentStore store = CreateStore(useFileSystem);

        await store.AppendUpdateAsync("alpha", CreateUpdate("a"));
        await store.AppendUpdateAsync("beta", CreateUpdate("b"));

        IReadOnlyList<string> ids = await store.ListAsync();
        Assert.Contains("alpha", ids);
        Assert.Contains("beta", ids);

        await store.DeleteAsync("alpha");
        BcrdtStoredDocument gone = await store.LoadAsync("alpha");
        Assert.IsTrue(gone.IsEmpty);
    }

    [TestMethod]
    public async Task Two_replicas_persisted_separately_converge_when_their_stores_are_combined()
    {
        var storeA = new BcrdtInMemoryDocumentStore();
        var storeB = new BcrdtInMemoryDocumentStore();

        using var a = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        using var b = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });

        await using (BcrdtDocumentPersistence pa = await BcrdtDocumentPersistence.AttachAsync(a, storeA, "doc"))
        await using (BcrdtDocumentPersistence pb = await BcrdtDocumentPersistence.AttachAsync(b, storeB, "doc"))
        {
            a.GetText("body").Insert(0, "from A");
            b.GetText("body").Insert(0, "from B");

            await pa.FlushAsync();
            await pb.FlushAsync();
        }

        // Two devices that were both offline. Loading each store into one document is the same
        // operation as syncing them over a network.
        using var combined = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 });
        combined.ApplyUpdate((await storeA.LoadAsync("doc")).Update!);
        combined.ApplyUpdate((await storeB.LoadAsync("doc")).Update!);

        string text = combined.GetText("body").ToString();
        Assert.Contains("from A", text);
        Assert.Contains("from B", text);
    }

    [TestMethod]
    public async Task Damage_in_the_middle_of_a_log_costs_only_the_damaged_record()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        var corruptions = new List<BcrdtStoreCorruption>();
        store.CorruptionDetected += corruptions.Add;

        byte[] first = CreateUpdate("first", clientId: 1);
        byte[] second = CreateUpdate("second", clientId: 2);
        byte[] third = CreateUpdate("third", clientId: 3);
        await store.AppendUpdateAsync("doc", first);
        await store.AppendUpdateAsync("doc", second);
        await store.AppendUpdateAsync("doc", third);

        // A flipped byte inside the second record: bit rot, not an interrupted append.
        string logPath = Directory.EnumerateFiles(_root, "*.bcl").Single();
        byte[] contents = await File.ReadAllBytesAsync(logPath);
        contents[8 + first.Length + 8 + 2] ^= 0xFF;
        await File.WriteAllBytesAsync(logPath, contents);

        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 9 });
        reloaded.ApplyUpdate(stored.Update!);

        // Everything after the damage survives; once the log was cut there, it never came back.
        Assert.AreEqual(2, stored.RecordCount);
        Assert.IsTrue(reloaded.GetStateVector().Get(1) > 0 && reloaded.GetStateVector().Get(3) > 0);
        Assert.AreEqual(0L, reloaded.GetStateVector().Get(2));

        // The damaged log is kept, untouched, and the store says so.
        BcrdtStoreCorruption corruption = corruptions.Single();
        Assert.AreEqual("doc", corruption.DocumentId);
        Assert.AreEqual(2, corruption.RecordsKept);
        CollectionAssert.AreEqual(contents, await File.ReadAllBytesAsync(corruption.QuarantinePath));

        // The repaired log reads cleanly from then on, and takes appends after its good records.
        await store.AppendUpdateAsync("doc", CreateUpdate("fourth", clientId: 4));
        Assert.AreEqual(3, (await new BcrdtFileSystemDocumentStore(_root).LoadAsync("doc")).RecordCount);
        Assert.HasCount(1, corruptions);
    }

    [TestMethod]
    public async Task A_record_that_is_intact_but_unreadable_does_not_lock_the_document_away()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        var corruptions = new List<BcrdtStoreCorruption>();
        store.CorruptionDetected += corruptions.Add;

        await store.AppendUpdateAsync("doc", CreateUpdate("readable", clientId: 1));
        await store.AppendUpdateAsync("doc", new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });

        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        using var reloaded = new BcrdtDocument();
        reloaded.ApplyUpdate(stored.Update!);

        Assert.AreEqual("readable", reloaded.GetText("body").ToString());
        Assert.HasCount(1, corruptions);
        await store.CompactAsync("doc");
        Assert.AreEqual("readable", Text((await store.LoadAsync("doc")).Update!));
    }

    [TestMethod]
    public async Task A_record_too_far_past_the_ones_before_it_is_set_aside_and_the_rest_load()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        var corruptions = new List<BcrdtStoreCorruption>();
        store.CorruptionDetected += corruptions.Add;

        // Every record reads on its own, but the second leaves a hole in its author's clock range wider
        // than any replica writes. A document refuses such a block, so a log merged with it in could
        // never be opened again.
        byte[] far = CollectedRange(7, 2 + (ulong)BcrdtUpdateCodec.MaxClockGap);
        await store.AppendUpdateAsync("doc", CollectedRange(7, 0));
        await store.AppendUpdateAsync("doc", far);
        await store.AppendUpdateAsync("doc", CreateUpdate("kept"));

        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        using var reloaded = new BcrdtDocument();
        reloaded.ApplyUpdate(stored.Update!);

        Assert.AreEqual(2, stored.RecordCount);
        Assert.AreEqual("kept", reloaded.GetText("body").ToString());
        Assert.AreEqual(1L, reloaded.GetStateVector().Get(7));

        // Set aside like any other record the document cannot use - copied away untouched, and reported
        // once - so every later load and compaction finds a log it can read.
        BcrdtStoreCorruption corruption = corruptions.Single();
        Assert.AreEqual(2, corruption.RecordsKept);
        Assert.AreEqual(8L + far.Length, corruption.BytesDiscarded);
        Assert.AreEqual(2, (await store.LoadAsync("doc")).RecordCount);
        await store.CompactAsync("doc");
        Assert.AreEqual("kept", Text((await store.LoadAsync("doc")).Update!));
        Assert.HasCount(1, corruptions);
    }

    [TestMethod]
    public async Task Records_starting_too_far_past_clock_zero_are_set_aside_even_when_they_merge()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        var corruptions = new List<BcrdtStoreCorruption>();
        store.CorruptionDetected += corruptions.Add;

        // A document measures a block from what it already holds - nothing, while it is being loaded - so
        // content that starts further out than any replica writes is refused, whether it is one record
        // or two that sit side by side and merge without a gap between them.
        await store.AppendUpdateAsync("lone", CollectedRange(7, (ulong)BcrdtUpdateCodec.MaxClockGap + 1));
        await store.AppendUpdateAsync("pair", CollectedRange(7, 1UL << 40));
        await store.AppendUpdateAsync("pair", CollectedRange(7, (1UL << 40) + 1));
        await store.AppendUpdateAsync("pair", CreateUpdate("kept"));

        Assert.IsTrue((await store.LoadAsync("lone")).IsEmpty);

        BcrdtStoredDocument pair = await store.LoadAsync("pair");
        using var reloaded = new BcrdtDocument();
        reloaded.ApplyUpdate(pair.Update!);
        Assert.AreEqual(1, pair.RecordCount);
        Assert.AreEqual("kept", reloaded.GetText("body").ToString());

        Assert.HasCount(2, corruptions);
        Assert.AreEqual(0, corruptions.Single(corruption => corruption.DocumentId == "lone").RecordsKept);
        Assert.AreEqual(1, corruptions.Single(corruption => corruption.DocumentId == "pair").RecordsKept);
    }

    [TestMethod]
    public async Task Damage_and_a_record_too_far_out_in_one_log_are_set_aside_together()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        var corruptions = new List<BcrdtStoreCorruption>();
        store.CorruptionDetected += corruptions.Add;

        byte[] unreadable = [0xFF, 0xFF, 0xFF, 0xFF];
        byte[] far = CollectedRange(7, (ulong)BcrdtUpdateCodec.MaxClockGap + 1);
        await store.AppendUpdateAsync("doc", CreateUpdate("kept"));
        await store.AppendUpdateAsync("doc", unreadable);
        await store.AppendUpdateAsync("doc", far);

        // One repair, reported once, after which the document opens - not a repair reported for a
        // document that still fails to load.
        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        Assert.AreEqual(1, stored.RecordCount);
        Assert.AreEqual("kept", Text(stored.Update!));

        BcrdtStoreCorruption corruption = corruptions.Single();
        Assert.AreEqual(1, corruption.RecordsKept);
        Assert.AreEqual(8L + unreadable.Length + 8L + far.Length, corruption.BytesDiscarded);
        Assert.AreEqual("kept", Text((await store.LoadAsync("doc")).Update!));
        Assert.HasCount(1, corruptions);
    }

    [TestMethod]
    public async Task Two_store_instances_on_one_directory_never_lose_an_append_to_a_compaction()
    {
        var appender = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        var compactor = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        var updates = new List<byte[]>();
        doc.Updated += args => updates.Add(args.Update);
        for (int i = 0; i < 200; i++) doc.GetArray("items").Push((long)i);

        Task appending = Task.Run(async () =>
        {
            foreach (byte[] update in updates) await appender.AppendUpdateAsync("doc", update);
        });
        Task compacting = Task.Run(async () =>
        {
            while (!appending.IsCompleted) await compactor.CompactAsync("doc");
        });
        await Task.WhenAll(appending, compacting);

        using var reloaded = new BcrdtDocument();
        reloaded.ApplyUpdate((await appender.LoadAsync("doc")).Update!);
        Assert.AreEqual(200, reloaded.GetArray("items").Count);
    }

    [TestMethod]
    public async Task A_flush_completes_while_the_document_keeps_changing()
    {
        var store = new SlowStore(TimeSpan.FromMilliseconds(30));
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");

        using var editing = new CancellationTokenSource();
        Task typing = Task.Run(async () =>
        {
            while (!editing.IsCancellationRequested)
            {
                lock (doc) doc.GetText("body").Insert(0, "x");
                await Task.Delay(5);
            }
        });

        await Task.Delay(50);
        Task flush;
        lock (doc) flush = persistence.FlushAsync();

        // The edits made after the call are not the flush's concern.
        Assert.AreSame(flush, await Task.WhenAny(flush, Task.Delay(TimeSpan.FromSeconds(10))), "the flush should complete while edits continue");
        await editing.CancelAsync();
        await typing;
    }

    [TestMethod]
    public async Task A_failing_compaction_is_not_retried_after_every_write()
    {
        var store = new FailingCompactionStore();
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(
            doc, store, "doc", new BcrdtDocumentPersistenceOptions { CompactionThreshold = 5 });

        for (int i = 0; i < 30; i++)
        {
            doc.GetText("body").Insert(0, "x");
            await persistence.FlushAsync();
        }

        // Thresholds at 5, 10, 15, 20, 25 and 30 records - not one attempt for every write past the first.
        Assert.IsLessThanOrEqualTo(6, store.CompactionAttempts, $"{store.CompactionAttempts} compaction attempts");
        Assert.IsGreaterThanOrEqualTo(1, store.CompactionAttempts);
    }

    [TestMethod]
    public async Task A_torn_tail_is_cut_off_in_one_pass_however_large_the_torn_record()
    {
        var store = new BcrdtFileSystemDocumentStore(_root, flushToDisk: false);
        var corruptions = new List<BcrdtStoreCorruption>();
        store.CorruptionDetected += corruptions.Add;

        byte[] first = CreateUpdate("kept");
        await store.AppendUpdateAsync("doc", first);

        // Power lost while a multi-megabyte record was being written: its header promises 4 MB, and 2 MB
        // of zero-rich binary payload made it to disk. Nearly every other offset in it reads as a header
        // whose length fits in what is left, which made a checksum at each of them take minutes.
        string logPath = Directory.EnumerateFiles(_root, "*.bcl").Single();
        var random = new Random(1);
        byte[] torn = new byte[8 + 2 * 1024 * 1024];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(torn, 4 * 1024 * 1024);
        for (int i = 8; i + 4 <= torn.Length; i += 4)
        {
            torn[i] = (byte)random.Next(256);
            torn[i + 1] = (byte)random.Next(256);
        }
        await File.WriteAllBytesAsync(logPath, [.. await File.ReadAllBytesAsync(logPath), .. torn]);

        var watch = System.Diagnostics.Stopwatch.StartNew();
        BcrdtStoredDocument stored = await store.LoadAsync("doc");
        watch.Stop();

        Assert.IsLessThan(TimeSpan.FromSeconds(5), watch.Elapsed, $"loading took {watch.Elapsed}");
        Assert.AreEqual("kept", Text(stored.Update!));
        Assert.AreEqual(8 + first.Length, new FileInfo(logPath).Length);
        Assert.IsEmpty(corruptions, "an interrupted append is not corruption");
    }

    [TestMethod]
    public async Task A_bounded_in_memory_store_never_forgets_an_attached_document_however_long_it_sits_idle()
    {
        var time = new FakeTimeProvider();
        var store = new BcrdtInMemoryDocumentStore(maxDocuments: 1, time);

        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using (BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "open"))
        {
            doc.GetText("body").Insert(0, "base");
            await persistence.FlushAsync();

            // Open but idle for long past the grace period while other documents fill the store.
            time.Advance(TimeSpan.FromMinutes(10));
            await store.AppendUpdateAsync("other-1", CreateUpdate("x", clientId: 2));
            time.Advance(TimeSpan.FromMinutes(10));
            await store.AppendUpdateAsync("other-2", CreateUpdate("y", clientId: 3));

            // The next change builds on what the store already holds; that must still be there.
            doc.GetText("body").Insert(4, " and more");
            await persistence.FlushAsync();
        }

        Assert.AreEqual("base and more", Text((await store.LoadAsync("open")).Update!));
    }

    [TestMethod]
    public async Task A_bounded_in_memory_store_forgets_an_idle_detached_document()
    {
        var time = new FakeTimeProvider();
        var store = new BcrdtInMemoryDocumentStore(maxDocuments: 1, time);

        using (var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 }))
        {
            await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "closed");
            doc.GetText("body").Insert(0, "gone");
            await persistence.FlushAsync();
        }

        time.Advance(TimeSpan.FromMinutes(2));
        await store.AppendUpdateAsync("new", CreateUpdate("here", clientId: 2));

        Assert.AreEqual(1, store.Count);
        Assert.IsTrue((await store.LoadAsync("closed")).IsEmpty);
    }

    [TestMethod]
    public async Task A_flush_after_a_change_was_refused_outright_says_so_once()
    {
        var store = new RefusingStore();
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using BcrdtDocumentPersistence persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");

        var refused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        persistence.WriteFailed += _ => refused.TrySetResult();
        store.RefuseNext = true;
        doc.GetText("body").Insert(0, "lost");
        await refused.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Settled by now - given up on - but never reported to anyone: the flush is the one to say so.
        await Task.Delay(50);
        await Assert.ThrowsExactlyAsync<IOException>(() => persistence.FlushAsync());

        // Said once; a flush called after that reports only what happens from then on.
        await persistence.FlushAsync();
        doc.GetText("body").Insert(0, "more ");
        await persistence.FlushAsync();
        Assert.AreEqual(1, persistence.RecordCount);
    }

    private static string Text(byte[] update)
    {
        using var doc = new BcrdtDocument();
        doc.ApplyUpdate(update);
        return doc.GetText("body").ToString();
    }

    private static byte[] CreateUpdate(string content, ulong clientId = 1)
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
        doc.GetText("body").Insert(0, content);
        return doc.EncodeStateAsUpdate();
    }

    /// <summary>An in-memory store whose appends take a while.</summary>
    private sealed class SlowStore(TimeSpan latency) : IBcrdtDocumentStore
    {
        private readonly BcrdtInMemoryDocumentStore _inner = new();

        public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.LoadAsync(documentId, cancellationToken);

        public async ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default)
        {
            await Task.Delay(latency, cancellationToken);
            await _inner.AppendUpdateAsync(documentId, update, cancellationToken);
        }

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.CompactAsync(documentId, cancellationToken);

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            _inner.ListAsync(cancellationToken);
    }

    /// <summary>An in-memory store that refuses the next append outright, as one over its size limit.</summary>
    private sealed class RefusingStore : IBcrdtDocumentStore
    {
        private readonly BcrdtInMemoryDocumentStore _inner = new();

        public bool RefuseNext { get; set; }

        public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.LoadAsync(documentId, cancellationToken);

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default)
        {
            if (RefuseNext)
            {
                RefuseNext = false;
                throw new ArgumentException("The record is too large.");
            }
            return _inner.AppendUpdateAsync(documentId, update, cancellationToken);
        }

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.CompactAsync(documentId, cancellationToken);

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            _inner.ListAsync(cancellationToken);
    }

    /// <summary>An in-memory store that cannot compact - a file another process holds open, say.</summary>
    private sealed class FailingCompactionStore : IBcrdtDocumentStore
    {
        private readonly BcrdtInMemoryDocumentStore _inner = new();
        private int _compactions;

        public int CompactionAttempts => Volatile.Read(ref _compactions);

        public ValueTask<BcrdtStoredDocument> LoadAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.LoadAsync(documentId, cancellationToken);

        public ValueTask AppendUpdateAsync(string documentId, ReadOnlyMemory<byte> update, CancellationToken cancellationToken = default) =>
            _inner.AppendUpdateAsync(documentId, update, cancellationToken);

        public ValueTask CompactAsync(string documentId, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _compactions);
            throw new IOException("The file is in use.");
        }

        public ValueTask DeleteAsync(string documentId, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(documentId, cancellationToken);

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default) =>
            _inner.ListAsync(cancellationToken);
    }
}
