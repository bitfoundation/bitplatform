using Bit.BlazeDb;

namespace Bit.BlazeDb.Tests;

[TestClass]
public class TransactionTests
{
    [TestMethod]
    public async Task Commit_Keeps_All_Writes()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using (var txn = db.BeginTransaction())
        {
            people.Insert(new Person(1, "Ada", 36));
            people.Insert(new Person(2, "Grace", 45));
            txn.Commit();
        }

        Assert.AreEqual(2, people.Count);
    }

    [TestMethod]
    public async Task Writes_Are_Visible_Inside_Transaction()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using var txn = db.BeginTransaction();
        people.Insert(new Person(1, "Ada", 36));

        Assert.AreEqual("Ada", people.Get(1)!.Name);
        txn.Commit();
    }

    [TestMethod]
    public async Task Rollback_Reverts_Insert_Update_And_Delete()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));
        people.Insert(new Person(2, "Grace", 45));

        using (var txn = db.BeginTransaction())
        {
            people.Insert(new Person(3, "Alan", 41));
            people.Update(new Person(1, "Ada Lovelace", 37));
            people.Delete(2);
            txn.Rollback();
        }

        Assert.AreEqual(2, people.Count);
        Assert.AreEqual(new Person(1, "Ada", 36), people.Get(1));
        Assert.AreEqual(new Person(2, "Grace", 45), people.Get(2));
        Assert.IsFalse(people.Contains(3));
    }

    [TestMethod]
    public async Task Rollback_Restores_Multiple_Writes_To_Same_Key()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Original", 10));

        using (var txn = db.BeginTransaction())
        {
            people.Update(new Person(1, "First", 11));
            people.Update(new Person(1, "Second", 12));
            people.Delete(1);
            people.Insert(new Person(1, "Third", 13));
            txn.Rollback();
        }

        Assert.AreEqual(new Person(1, "Original", 10), people.Get(1));
    }

    [TestMethod]
    public async Task Dispose_Without_Commit_Rolls_Back()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using (db.BeginTransaction())
        {
            people.Insert(new Person(1, "Ada", 36));
        }

        Assert.AreEqual(0, people.Count);
    }

    [TestMethod]
    public async Task Nested_Transactions_Are_Rejected()
    {
        await using var db = await TestDb.OpenInMemoryAsync();

        using var txn = db.BeginTransaction();
        Assert.ThrowsExactly<InvalidOperationException>(() => db.BeginTransaction());
        txn.Commit();
    }

    [TestMethod]
    public async Task Ambient_Writes_Join_The_Active_Transaction()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using (db.BeginTransaction())
        {
            // Write does not reference the transaction object, but must still be reverted.
            people.Insert(new Person(1, "Ada", 36));
        }

        Assert.IsFalse(people.Contains(1));
    }

    [TestMethod]
    public async Task Completed_Transaction_Cannot_Be_Reused()
    {
        await using var db = await TestDb.OpenInMemoryAsync();

        var txn = db.BeginTransaction();
        txn.Commit();

        Assert.ThrowsExactly<InvalidOperationException>(txn.Commit);
        Assert.ThrowsExactly<InvalidOperationException>(txn.Rollback);
    }

    [TestMethod]
    public async Task New_Transaction_Can_Start_After_Previous_Completes()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using (var txn = db.BeginTransaction())
        {
            people.Insert(new Person(1, "Ada", 36));
            txn.Commit();
        }

        using (var txn = db.BeginTransaction())
        {
            people.Insert(new Person(2, "Grace", 45));
            txn.Rollback();
        }

        Assert.IsTrue(people.Contains(1));
        Assert.IsFalse(people.Contains(2));
    }

    [TestMethod]
    public async Task Failed_Operation_Inside_Transaction_Leaves_Prior_Writes_Applied_Until_Rollback()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Ada", 36));

        using (var txn = db.BeginTransaction())
        {
            people.Insert(new Person(2, "Grace", 45));
            Assert.ThrowsExactly<BlazeDbDuplicateKeyException>(() => people.Insert(new Person(1, "Dup", 0)));
            txn.Rollback();
        }

        Assert.AreEqual(1, people.Count);
        Assert.IsTrue(people.Contains(1));
    }
}
