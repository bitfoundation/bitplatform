using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>The Try* writes, optimistic concurrency checks and the atomic bulk writes.</summary>
[TestClass]
public class BulkAndConditionalWriteTests
{
    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<int, Account> Accounts)> OpenAccountsAsync(IBlazeDbStorage? storage = null)
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(Account.Table));
        return (db, db.GetTable(Account.Table));
    }

    private static Account NewAccount(int id, string? username = null, int tenant = 0, int? slot = null) =>
        new() { Id = id, Username = username ?? $"user{id}", TenantId = tenant, Email = $"{id}@example.com", Slot = slot };

    [TestMethod]
    public async Task TryInsert_Reports_An_Existing_Key_Without_Writing()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        Assert.IsTrue(people.TryInsert(new Person(1, "Ada", 36)));
        var version = people.Version;
        Assert.IsFalse(people.TryInsert(new Person(1, "Impostor", 0)));

        Assert.AreEqual(new Person(1, "Ada", 36), people.Get(1));
        Assert.AreEqual(version, people.Version);
    }

    [TestMethod]
    public async Task TryInsert_Still_Enforces_Unique_Indexes()
    {
        var (db, accounts) = await OpenAccountsAsync();
        await using var _ = db;
        accounts.Insert(NewAccount(1, "ada"));

        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.TryInsert(NewAccount(2, "ada")));
        Assert.AreEqual(1, accounts.Count);
    }

    [TestMethod]
    public async Task TryUpdate_Reports_A_Missing_Row_Without_Writing()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        Assert.IsFalse(people.TryUpdate(new Person(1, "Ghost", 0)));
        Assert.AreEqual(0, people.Count);

        people.Insert(new Person(1, "Ada", 36));
        Assert.IsTrue(people.TryUpdate(new Person(1, "Ada Lovelace", 37)));
        Assert.AreEqual("Ada Lovelace", people.Get(1)!.Name);
    }

    [TestMethod]
    public async Task TryUpdate_With_A_Comparison_Detects_A_Concurrent_Change_To_A_Record()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        var read = people.Get(1)!;                          // a form opens the row
        people.Update(new Person(1, "Ada", 37));            // something else writes it meanwhile
        Assert.IsFalse(people.TryUpdate(read with { Name = "Ada Lovelace" }, read));
        Assert.AreEqual(new Person(1, "Ada", 37), people.Get(1));

        var fresh = people.Get(1)!;                         // re-read and retry
        Assert.IsTrue(people.TryUpdate(fresh with { Name = "Ada Lovelace" }, fresh));
        Assert.AreEqual(new Person(1, "Ada Lovelace", 37), people.Get(1));
    }

    [TestMethod]
    public async Task TryUpdate_With_A_Comparison_Compares_Class_Rows_By_Identity()
    {
        var (db, accounts) = await OpenAccountsAsync();
        await using var _ = db;
        accounts.Insert(NewAccount(1, "ada"));

        var read = accounts.Get(1)!;
        var identicalCopy = NewAccount(1, "ada");
        Assert.IsFalse(accounts.TryUpdate(NewAccount(1, "ada2"), identicalCopy), "Equal values are not the same row object.");
        Assert.IsTrue(accounts.TryUpdate(NewAccount(1, "ada2"), read));
        Assert.IsFalse(accounts.TryUpdate(NewAccount(1, "ada3"), read), "The row was replaced, so the old object no longer matches.");
    }

    [TestMethod]
    public async Task TryDelete_With_A_Comparison_Removes_Only_The_Row_That_Was_Read()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));
        var read = people.Get(1)!;
        people.Update(new Person(1, "Ada", 37));

        Assert.IsFalse(people.TryDelete(1, read));
        Assert.IsTrue(people.Contains(1));
        Assert.IsTrue(people.TryDelete(1, people.Get(1)!));
        Assert.IsFalse(people.Contains(1));
        Assert.IsFalse(people.TryDelete(1, read));
    }

    [TestMethod]
    public async Task InsertMany_Is_All_Or_Nothing()
    {
        var (db, accounts) = await OpenAccountsAsync();
        await using var _ = db;
        accounts.Insert(NewAccount(1, "taken"));

        var batch = new[] { NewAccount(2), NewAccount(3), NewAccount(4, "taken"), NewAccount(5) };
        Assert.ThrowsExactly<BlazeDbUniqueConstraintViolationException>(() => accounts.InsertMany(batch));

        Assert.AreEqual(1, accounts.Count);
        Assert.AreEqual(0, db.VerifyIntegrity().Count);

        accounts.InsertMany(batch.Where(a => a.Username != "taken"));
        Assert.AreEqual(4, accounts.Count);
    }

    [TestMethod]
    public async Task InsertMany_Inside_A_Transaction_Fails_Without_Ending_It()
    {
        var (db, accounts) = await OpenAccountsAsync();
        await using var _ = db;

        using var txn = db.BeginTransaction();
        accounts.Insert(NewAccount(1));
        Assert.ThrowsExactly<BlazeDbDuplicateKeyException>(() => accounts.InsertMany([NewAccount(2), NewAccount(1)]));
        Assert.IsFalse(accounts.Contains(2));
        txn.Commit();
        Assert.AreEqual(1, accounts.Count);
    }

    [TestMethod]
    public async Task UpsertMany_Lets_The_Last_Row_For_A_Key_Win()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        people.UpsertMany([new Person(1, "Ada Lovelace", 37), new Person(2, "Grace", 45), new Person(2, "Grace Hopper", 46)]);

        Assert.AreEqual(new Person(1, "Ada Lovelace", 37), people.Get(1));
        Assert.AreEqual(new Person(2, "Grace Hopper", 46), people.Get(2));
    }

    [TestMethod]
    public async Task DeleteMany_Takes_Keys_Straight_From_A_Query_Over_The_Same_Table()
    {
        var (db, accounts) = await OpenAccountsAsync();
        await using var _ = db;
        accounts.InsertMany(Enumerable.Range(1, 20).Select(i => NewAccount(i, slot: i)));

        // The range enumerates the ordered index the deletes modify; the keys are buffered first.
        var deleted = accounts.DeleteMany(accounts.Range(Account.Indexes.Slot, 5, 9).Select(a => a.Id));

        Assert.AreEqual(5, deleted);
        Assert.AreEqual(15, accounts.Count);
        Assert.AreEqual(0, accounts.Range(Account.Indexes.Slot, 5, 9).Count());
        Assert.AreEqual(0, db.VerifyIntegrity().Count);
    }

    [TestMethod]
    public async Task DeleteMany_Counts_Only_Rows_That_Existed()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.InsertMany([new Person(1, "A", 1), new Person(2, "B", 2)]);

        Assert.AreEqual(1, people.DeleteMany([2, 3, 2]));
        Assert.AreEqual(1, people.Count);
    }

    [TestMethod]
    public async Task DeleteWhere_And_DeleteAll_Survive_A_Reopen()
    {
        var storage = new BlazeDbInMemoryStorage();
        var (db, accounts) = await OpenAccountsAsync(storage);
        await using (db)
        {
            accounts.InsertMany(Enumerable.Range(1, 10).Select(i => NewAccount(i, tenant: i % 2)));
            Assert.AreEqual(5, accounts.DeleteWhere(a => a.TenantId == 1));
            Assert.AreEqual(5, accounts.Count);
        }

        var (reopened, recovered) = await OpenAccountsAsync(storage);
        await using (reopened)
        {
            Assert.AreEqual(5, recovered.Count);
            Assert.IsTrue(recovered.Scan().All(a => a.TenantId == 0));
            Assert.AreEqual(5, recovered.DeleteAll());
            Assert.AreEqual(0, recovered.Count);
            Assert.AreEqual(0, recovered.CountBy(Account.Indexes.TenantId, 0));
        }

        var (again, empty) = await OpenAccountsAsync(storage);
        await using (again)
        {
            Assert.AreEqual(0, empty.Count);
        }
    }

    [TestMethod]
    public async Task Bulk_Writes_Reject_Null_Arguments()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        Assert.ThrowsExactly<ArgumentNullException>(() => people.InsertMany(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => people.UpsertMany(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => people.DeleteMany(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => people.DeleteWhere(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => people.InsertMany([new Person(1, "A", 1), null!]));
        Assert.AreEqual(0, people.Count);
    }
}
