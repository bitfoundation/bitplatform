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

    private static byte[] CreateUpdate(string content)
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        doc.GetText("body").Insert(0, content);
        return doc.EncodeStateAsUpdate();
    }
}
