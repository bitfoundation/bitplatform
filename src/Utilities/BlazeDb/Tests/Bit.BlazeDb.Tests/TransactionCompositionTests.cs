using Bit.BlazeDb;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>Savepoints, <see cref="BlazeDbDatabase.RunInTransaction(Action)"/> and how the two nest.</summary>
[TestClass]
public class TransactionCompositionTests
{
    private static BlazeDbDatabaseOptions Persistent(IBlazeDbStorage storage) =>
        new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(PersonTable.Descriptor);

    [TestMethod]
    public async Task RollbackTo_Undoes_Only_What_Came_After_The_Savepoint()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        using var txn = db.BeginTransaction();
        people.Insert(new Person(2, "Grace", 45));
        var savepoint = txn.CreateSavepoint();
        people.Update(new Person(1, "Ada Lovelace", 37));
        people.Delete(2);
        people.Insert(new Person(3, "Alan", 41));

        txn.RollbackTo(savepoint);

        Assert.AreEqual(new Person(1, "Ada", 36), people.Get(1));
        Assert.AreEqual(new Person(2, "Grace", 45), people.Get(2));
        Assert.IsFalse(people.Contains(3));
        txn.Commit();
        Assert.AreEqual(2, people.Count);
        Assert.AreEqual(0, db.VerifyIntegrity().Count);
    }

    [TestMethod]
    public async Task A_Savepoint_Can_Be_Rolled_Back_To_Repeatedly()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using var txn = db.BeginTransaction();
        var savepoint = txn.CreateSavepoint();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            people.Insert(new Person(1, $"Attempt {attempt}", attempt));
            txn.RollbackTo(savepoint);
            Assert.AreEqual(0, people.Count);
        }
        txn.Commit();
    }

    [TestMethod]
    public async Task Rolling_Back_Past_A_Savepoint_Invalidates_It()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using var txn = db.BeginTransaction();
        var outer = txn.CreateSavepoint();
        people.Insert(new Person(1, "Ada", 36));
        var inner = txn.CreateSavepoint();
        people.Insert(new Person(2, "Grace", 45));

        txn.RollbackTo(outer);
        people.Insert(new Person(3, "Alan", 41));
        people.Insert(new Person(4, "Edsger", 72));

        // inner marked a point the earlier rollback undid; reusing it would truncate the wrong writes.
        Assert.ThrowsExactly<InvalidOperationException>(() => txn.RollbackTo(inner));
        CollectionAssert.AreEquivalent(new[] { 3, 4 }, people.Scan().Select(p => p.Id).ToArray());
    }

    [TestMethod]
    public async Task A_Savepoint_From_Another_Transaction_Is_Refused()
    {
        await using var db = await TestDb.OpenInMemoryAsync();

        BlazeDbSavepoint foreign;
        using (var first = db.BeginTransaction())
        {
            foreign = first.CreateSavepoint();
            first.Commit();
        }

        using var second = db.BeginTransaction();
        Assert.ThrowsExactly<InvalidOperationException>(() => second.RollbackTo(foreign));
        Assert.ThrowsExactly<InvalidOperationException>(() => second.RollbackTo(default));
    }

    [TestMethod]
    public async Task RunInTransaction_Commits_Everything_As_One_Record()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using (var db = await BlazeDbDatabase.OpenAsync(Persistent(storage)))
        {
            var people = db.GetTable(PersonTable.Descriptor);
            var count = db.RunInTransaction(() =>
            {
                people.Insert(new Person(1, "Ada", 36));
                people.Insert(new Person(2, "Grace", 45));
                return people.Count;
            });
            Assert.AreEqual(2, count);
            Assert.IsNull(db.ActiveTransaction);
        }

        // One commit record: both rows replay together from one checksummed record.
        var wal = storage.GetFile(storage.Wal())!;
        Assert.AreEqual(1, CountRecords(wal));
        await using var reopened = await BlazeDbDatabase.OpenAsync(Persistent(storage));
        Assert.AreEqual(2, reopened.GetTable(PersonTable.Descriptor).Count);
    }

    private static int CountRecords(byte[] wal)
    {
        var count = 0;
        for (var i = 0; i + 4 <= wal.Length; i++)
        {
            if (wal.AsSpan(i, 4).SequenceEqual("BLZR"u8))
            {
                count++;
            }
        }
        return count;
    }

    [TestMethod]
    public async Task RunInTransaction_Rolls_Back_Everything_When_The_Action_Throws()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        Assert.ThrowsExactly<BlazeDbDuplicateKeyException>(() => db.RunInTransaction(() =>
        {
            people.Update(new Person(1, "Ada Lovelace", 37));
            people.Insert(new Person(2, "Grace", 45));
            people.Insert(new Person(1, "Duplicate", 0));
        }));

        Assert.AreEqual(new Person(1, "Ada", 36), people.Get(1));
        Assert.IsFalse(people.Contains(2));
        Assert.IsNull(db.ActiveTransaction);
    }

    [TestMethod]
    public async Task RunInTransaction_Inside_A_Transaction_Fails_Without_Ending_It()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using var txn = db.BeginTransaction();
        people.Insert(new Person(1, "Kept", 1));
        Assert.ThrowsExactly<InvalidOperationException>(() => db.RunInTransaction(() =>
        {
            people.Insert(new Person(2, "Undone", 2));
            throw new InvalidOperationException("the unit fails");
        }));

        Assert.AreSame(txn, db.ActiveTransaction);
        Assert.IsTrue(people.Contains(1));
        Assert.IsFalse(people.Contains(2));
        txn.Commit();
        Assert.AreEqual(1, people.Count);
    }

    [TestMethod]
    public async Task Nested_RunInTransaction_Units_Fail_Independently()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        db.RunInTransaction(() =>
        {
            people.Insert(new Person(1, "Outer", 1));
            try
            {
                db.RunInTransaction(() =>
                {
                    people.Insert(new Person(2, "Inner", 2));
                    throw new InvalidOperationException();
                });
            }
            catch (InvalidOperationException)
            {
                // The inner unit is gone; the outer one carries on.
            }
            people.Insert(new Person(3, "Outer again", 3));
        });

        CollectionAssert.AreEquivalent(new[] { 1, 3 }, people.Scan().Select(p => p.Id).ToArray());
    }

    [TestMethod]
    public async Task RunInTransaction_On_A_Replica_Is_Refused()
    {
        var storage = new BlazeDbInMemoryStorage();
        await (await BlazeDbDatabase.OpenAsync(Persistent(storage))).DisposeAsync();
        await using var replica = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage, ReadOnly = true }.AddTable(PersonTable.Descriptor));

        Assert.ThrowsExactly<InvalidOperationException>(() => replica.RunInTransaction(() => { }));
    }

    [TestMethod]
    public async Task Concurrent_Writers_Each_Get_An_Atomic_Unit()
    {
        // Writers on different threads are serialized; the reads here happen only after they finish,
        // because the engine does not support reads concurrent with writes.
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = new BlazeDbInMemoryStorage(),
            FlushInterval = TimeSpan.FromMilliseconds(5),
        }.AddTable(Account.Table));
        var accounts = db.GetTable(Account.Table);
        const int Writers = 8;
        const int UnitsPerWriter = 200;

        await Task.WhenAll(Enumerable.Range(0, Writers).Select(writer => Task.Run(() =>
        {
            for (var unit = 0; unit < UnitsPerWriter; unit++)
            {
                var id = writer * UnitsPerWriter + unit;
                try
                {
                    db.RunInTransaction(() =>
                    {
                        accounts.Insert(new Account { Id = id, Username = $"user{id}", TenantId = writer, Email = $"{id}@x" });
                        // Every fifth unit collides with itself on the unique username and must vanish whole.
                        if (unit % 5 == 0)
                        {
                            accounts.Insert(new Account { Id = -id - 1, Username = $"user{id}", TenantId = writer });
                        }
                    });
                }
                catch (BlazeDbUniqueConstraintViolationException)
                {
                }
            }
        })));

        Assert.AreEqual(Writers * UnitsPerWriter * 4 / 5, accounts.Count);
        Assert.IsFalse(accounts.Scan().Any(a => a.Id < 0));
        for (var writer = 0; writer < Writers; writer++)
        {
            Assert.AreEqual(UnitsPerWriter * 4 / 5, accounts.CountBy(Account.Indexes.TenantId, writer));
        }
        Assert.AreEqual(0, db.VerifyIntegrity().Count);
        await db.FlushAsync();
    }
}
