using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class TableCrudTests
{
    [TestMethod]
    public async Task Insert_And_Get_Roundtrips()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        people.Insert(new Person(1, "Ada", 36));

        Assert.AreEqual(1, people.Count);
        Assert.IsTrue(people.Contains(1));
        Assert.AreEqual(new Person(1, "Ada", 36), people.Get(1));
        Assert.IsTrue(people.TryGet(1, out var row));
        Assert.AreEqual("Ada", row.Name);
    }

    [TestMethod]
    public async Task Get_Missing_Returns_Default()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        Assert.IsNull(people.Get(42));
        Assert.IsFalse(people.TryGet(42, out _));
        Assert.IsFalse(people.Contains(42));
    }

    [TestMethod]
    public async Task Insert_Duplicate_Key_Throws()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        var ex = Assert.ThrowsExactly<BlazeDbDuplicateKeyException>(() => people.Insert(new Person(1, "Bob", 40)));
        Assert.AreEqual("people", ex.TableName);
        Assert.AreEqual(new Person(1, "Ada", 36), people.Get(1));
    }

    [TestMethod]
    public async Task Update_Replaces_Existing_Row()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        people.Update(new Person(1, "Ada Lovelace", 37));

        Assert.AreEqual(1, people.Count);
        Assert.AreEqual("Ada Lovelace", people.Get(1)!.Name);
    }

    [TestMethod]
    public async Task Update_Missing_Throws()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        Assert.ThrowsExactly<KeyNotFoundException>(() => people.Update(new Person(9, "Nobody", 0)));
    }

    [TestMethod]
    public async Task Upsert_Inserts_Then_Replaces()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        people.Upsert(new Person(1, "Ada", 36));
        Assert.AreEqual("Ada", people.Get(1)!.Name);

        people.Upsert(new Person(1, "Grace", 45));
        Assert.AreEqual(1, people.Count);
        Assert.AreEqual("Grace", people.Get(1)!.Name);
    }

    [TestMethod]
    public async Task Delete_Removes_Row_And_Reports_Missing()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        Assert.IsTrue(people.Delete(1));
        Assert.AreEqual(0, people.Count);
        Assert.IsFalse(people.Delete(1));
    }

    [TestMethod]
    public async Task Scan_Enumerates_All_Rows()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));
        people.Insert(new Person(2, "Grace", 45));
        people.Insert(new Person(3, "Alan", 41));

        var names = people.Scan().Select(p => p.Name).OrderBy(n => n).ToList();

        Assert.AreSequenceEqual(["Ada", "Alan", "Grace"], names);
    }

    [TestMethod]
    public async Task GetTable_Unregistered_Descriptor_Throws()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions());

        Assert.ThrowsExactly<BlazeDbException>(() => db.GetTable(PersonTable.Descriptor));
    }

    [TestMethod]
    public async Task Invalid_Options_Are_Rejected_Before_Opening()
    {
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { FlushInterval = TimeSpan.Zero }));
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { CheckpointWalSize = 0 }));
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
            await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { QuotaReserveBytes = -1 }));
        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
            await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { ReadOnly = true }));
    }

    [TestMethod]
    public async Task A_Disposed_Database_Refuses_Further_Use()
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = new BlazeDbInMemoryStorage() }.AddTable(PersonTable.Descriptor));
        await db.DisposeAsync();

        Assert.ThrowsExactly<ObjectDisposedException>(() => db.BeginTransaction());
        Assert.ThrowsExactly<ObjectDisposedException>(() => db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "a", 1)));
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await db.FlushAsync());
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await db.CheckpointAsync());
        await db.DisposeAsync(); // idempotent
    }
}
