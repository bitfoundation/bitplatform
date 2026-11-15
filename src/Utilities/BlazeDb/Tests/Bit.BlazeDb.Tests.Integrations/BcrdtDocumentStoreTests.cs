using Bit.BlazeDb.Bcrdt;
using Bit.BlazeDb.Storage;
using Bit.Bcrdt;
using Bit.Bcrdt.Testing;

namespace Bit.BlazeDb.Tests.Integrations;

[TestClass]
public class BcrdtDocumentStoreTests
{
    private static BlazeDbDatabaseOptions Options(BlazeDbInMemoryStorage storage) =>
        new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddBcrdtDocuments();

    private static byte[] CreateUpdate(string text, ulong clientId = 1)
    {
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = clientId });
        doc.GetText("body").Insert(0, text);
        return doc.EncodeStateAsUpdate();
    }

    [TestMethod]
    public async Task A_Document_Survives_Being_Closed_And_The_Database_Reopened()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await BlazeDbDatabase.OpenAsync(Options(storage)))
        {
            var store = new BlazeDbBcrdtDocumentStore(db);
            using var doc = new BcrdtDocument(new BcrdtDocumentOptions { Id = "doc", ClientId = 1 });
            await using var persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc");
            doc.GetText("body").Insert(0, "persisted text");
            doc.GetMap("meta").Set("title", "Example");
            await persistence.FlushAsync();
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(Options(storage));
        using var again = new BcrdtDocument(new BcrdtDocumentOptions { Id = "doc", ClientId = 2 });
        await using var reloaded = await BcrdtDocumentPersistence.AttachAsync(again, new BlazeDbBcrdtDocumentStore(reopened), "doc");
        Assert.AreEqual("persisted text", again.GetText("body").ToString());
        Assert.AreEqual("Example", again.GetMap("meta").Get("title"));
    }

    [TestMethod]
    public async Task Compaction_Replaces_The_Records_With_One_That_Holds_The_Same_Document()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddBcrdtDocuments());
        var store = new BlazeDbBcrdtDocumentStore(db);
        using var doc = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1 });
        await using (var persistence = await BcrdtDocumentPersistence.AttachAsync(doc, store, "doc"))
        {
            var generator = new BcrdtOperationGenerator(7);
            for (var i = 0; i < 40; i++)
            {
                generator.ApplyRandomTransaction(doc);
                await persistence.FlushAsync();
            }
        }
        var before = BcrdtConvergenceHarness.Describe(doc);
        Assert.IsTrue((await store.LoadAsync("doc")).RecordCount > 1);

        await store.CompactAsync("doc");

        var stored = await store.LoadAsync("doc");
        Assert.AreEqual(1, stored.RecordCount);
        Assert.AreEqual(1, db.GetTable(BlazeDbBcrdtRecord.Table).Count);
        using var reloaded = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 2 });
        reloaded.ApplyUpdate(stored.Update!);
        Assert.AreEqual(before, BcrdtConvergenceHarness.Describe(reloaded));
    }

    [TestMethod]
    public async Task Records_Are_Kept_Per_Document_In_Order_And_Listed_And_Deleted()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddBcrdtDocuments());
        var store = new BlazeDbBcrdtDocumentStore(db);
        Assert.IsTrue((await store.LoadAsync("alpha")).IsEmpty);

        await store.AppendUpdateAsync("alpha", CreateUpdate("a1"));
        await store.AppendUpdateAsync("beta", CreateUpdate("b"));
        await store.AppendUpdateAsync("alpha", CreateUpdate("a2", clientId: 2));

        CollectionAssert.AreEquivalent(new[] { "alpha", "beta" }, (await store.ListAsync()).ToArray());
        Assert.AreEqual(2, (await store.LoadAsync("alpha")).RecordCount);
        var sequences = db.GetTable(BlazeDbBcrdtRecord.Table).Scan()
            .Where(record => record.DocumentId == "alpha")
            .Select(record => record.Sequence)
            .OrderBy(sequence => sequence)
            .ToArray();
        CollectionAssert.AreEqual(new long[] { 1, 2 }, sequences);

        await store.DeleteAsync("alpha");
        Assert.IsTrue((await store.LoadAsync("alpha")).IsEmpty);
        CollectionAssert.AreEqual(new[] { "beta" }, (await store.ListAsync()).ToArray());
    }

    [TestMethod]
    public async Task A_Loaded_Update_Is_A_Copy_The_Caller_Can_Change()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddBcrdtDocuments());
        var store = new BlazeDbBcrdtDocumentStore(db);
        var update = CreateUpdate("x");
        await store.AppendUpdateAsync("doc", update);

        var loaded = await store.LoadAsync("doc");
        Array.Clear(loaded.Update!);

        CollectionAssert.AreEqual(update, (await store.LoadAsync("doc")).Update);
    }

    [TestMethod]
    public async Task Appends_From_Several_Threads_Are_All_Kept_And_Numbered_Once()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddBcrdtDocuments());
        var store = new BlazeDbBcrdtDocumentStore(db);
        var update = CreateUpdate("x");

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 50; i++)
            {
                await store.AppendUpdateAsync("doc", update);
            }
        })));

        var sequences = db.GetTable(BlazeDbBcrdtRecord.Table).Scan().Select(record => record.Sequence).ToList();
        Assert.AreEqual(400, sequences.Count);
        Assert.AreEqual(400, sequences.Distinct().Count());
    }

    [TestMethod]
    public async Task A_Compaction_Is_One_Transaction()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var db = await BlazeDbDatabase.OpenAsync(Options(storage));
        var store = new BlazeDbBcrdtDocumentStore(db);
        await store.AppendUpdateAsync("doc", CreateUpdate("a"));
        await store.AppendUpdateAsync("doc", CreateUpdate("b", clientId: 2));
        var commits = new List<int>();
        db.GetTable(BlazeDbBcrdtRecord.Table).Changed += (_, args) => commits.Add(args.Changes.Count);

        await store.CompactAsync("doc");

        // Two records deleted and the merged one inserted, reported - and journaled - as one commit.
        CollectionAssert.AreEqual(new[] { 3 }, commits);
    }

    [TestMethod]
    public async Task A_Database_Without_The_Table_Is_Reported_With_The_Fix()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions());
        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await new BlazeDbBcrdtDocumentStore(db).ListAsync());
        StringAssert.Contains(ex.Message, "AddBcrdtDocuments()");
    }
}
