using Bit.BlazeDb;
using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Storage;
using Bit.BlazeDb.Tests.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Regressions for the defects an independent review of the hardening pass found: a committed EF Core save
/// reported as failed when a change handler threw, an atomic unit that kept writes after rolling back past
/// its own start, the engine's metadata table leaking into change notifications, a constructor-bound required
/// member, and a failed undo leaving unjournaled writes for a checkpoint to persist.
/// </summary>
[TestClass]
public class ReviewRegressionTests
{
    [TestMethod]
    public void A_Throwing_Handler_Does_Not_Make_A_Committed_Save_Look_Failed()
    {
        using var test = TestDatabase.Open();
        test.Database.Changed += (_, _) => throw new InvalidOperationException("handler bug");
        using var context = test.CreateContext();
        var ada = new EntityFrameworkCore.Person { Id = 1, Name = "Ada", City = "London", Age = 36 };
        context.People.Add(ada);

        var error = Assert.ThrowsExactly<BlazeDbChangeHandlerException>(() => context.SaveChanges());

        Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
        Assert.IsTrue(test.People.Contains(1), "The save committed.");
        Assert.AreEqual(EntityState.Unchanged, context.Entry(ada).State, "EF accepted the save it made.");
        Assert.AreEqual(0, context.SaveChanges(), "Nothing is left to save, so no duplicate is attempted.");
    }

    [TestMethod]
    public void A_Throwing_Handler_Does_Not_Make_A_Committed_Transaction_Forget_Its_Entities()
    {
        using var test = TestDatabase.Open();
        using var context = test.CreateContext();
        var ada = new EntityFrameworkCore.Person { Id = 1, Name = "Ada", City = "London", Age = 36 };

        using (var transaction = context.Database.BeginTransaction())
        {
            context.People.Add(ada);
            context.SaveChanges();
            test.Database.Changed += (_, _) => throw new InvalidOperationException("handler bug");
            Assert.ThrowsExactly<BlazeDbChangeHandlerException>(transaction.Commit);
        }

        Assert.IsTrue(test.People.Contains(1));
        Assert.AreEqual(EntityState.Unchanged, context.Entry(ada).State, "A committed transaction is not treated as rolled back.");
    }

    [TestMethod]
    public async Task A_Unit_That_Rolls_Back_Past_Its_Start_Still_Fails_Atomically()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using var txn = db.BeginTransaction();
        var savepoint = txn.CreateSavepoint();
        people.Insert(new Person(1, "Before the unit", 1));
        Assert.ThrowsExactly<InvalidOperationException>(() => db.RunInTransaction(() =>
        {
            people.Insert(new Person(2, "Unit", 2));
            txn.RollbackTo(savepoint);          // reaches below where the unit began
            people.Insert(new Person(3, "Unit, after", 3));
            people.Insert(new Person(4, "Unit, after", 4));
            throw new InvalidOperationException("the unit fails");
        }));

        Assert.AreEqual(0, people.Count, "Every write the failed unit made is gone, and the rollback it asked for stands.");
        txn.Commit();
        Assert.AreEqual(0, people.Count);
    }

    [TestMethod]
    public async Task A_Unit_That_Rolls_Back_Past_Its_Start_Reports_Its_Own_Exception()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        using var txn = db.BeginTransaction();
        var savepoint = txn.CreateSavepoint();
        people.Insert(new Person(1, "Before the unit", 1));
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => db.RunInTransaction(() =>
        {
            txn.RollbackTo(savepoint);
            throw new InvalidOperationException("the unit's own failure");
        }));

        Assert.AreEqual("the unit's own failure", error.Message);
    }

    [TestMethod]
    public async Task The_Engine_Table_Is_Not_Reported_To_Database_Subscribers()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions()
            .AddTable(PersonTable.Descriptor)
            .AddMigration(1, d => d.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Migrated", 1))));
        var tables = new List<string>();
        db.Changed += (_, e) => tables.AddRange(e.Changes.Select(c => c.Table));

        db.SetUserVersion(2);
        db.RunInTransaction(() =>
        {
            db.GetTable(PersonTable.Descriptor).Insert(new Person(2, "Ada", 36));
            db.SetUserVersion(3);
        });

        CollectionAssert.AreEqual(new[] { "people" }, tables);
    }

    [TestMethod]
    public void A_Required_Member_Taken_By_The_Constructor_Roundtrips()
    {
        var gadget = new RequiredGadget(7, "lamp") { Name = "lamp" };
        var writer = new BlazeDbBufferWriter();
        RequiredGadget.Table.RowWriter(writer, gadget);
        var reader = new BlazeDbBufferReader(writer.WrittenSpan);

        var decoded = RequiredGadget.Table.RowReader(ref reader);

        Assert.AreEqual(7, decoded.Id);
        Assert.AreEqual("lamp", decoded.Name);
    }

    /// <summary>An ordering that can be told to refuse every comparison, so an undo can be made to fail.</summary>
    internal sealed class SwitchableComparer : IComparer<int>
    {
        public bool Refuse { get; set; }

        public int Compare(int x, int y) => Refuse ? throw new InvalidOperationException("comparer refuses") : x.CompareTo(y);
    }

    internal static BlazeDbTableDescriptor<int, Person> Switchable(SwitchableComparer comparer) => new(
        "switchable",
        static p => p.Id,
        PersonTable.Descriptor.RowWriter,
        PersonTable.Descriptor.RowReader,
        PersonTable.Descriptor.KeyWriter,
        PersonTable.Descriptor.KeyReader,
        [new BlazeDbOrderedIndexDefinition<Person, int>("Age", static p => p.Age, comparer)]);

    [TestMethod]
    public async Task A_Rollback_That_Cannot_Undo_Stops_Writes_And_Checkpoints_Until_Reopened()
    {
        var comparer = new SwitchableComparer();
        var descriptor = Switchable(comparer);
        var storage = new BlazeDbInMemoryStorage();
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(descriptor));
        var table = db.GetTable(descriptor);
        table.Insert(new Person(1, "Committed", 10));
        table.Insert(new Person(2, "Committed", 20));

        var txn = db.BeginTransaction();
        table.Update(new Person(1, "Uncommitted", 11));
        comparer.Refuse = true;
        Assert.ThrowsExactly<InvalidOperationException>(txn.Rollback);
        comparer.Refuse = false;

        var refused = Assert.ThrowsExactly<BlazeDbException>(() => table.Insert(new Person(3, "Refused", 30)));
        StringAssert.Contains(refused.Message, "reopen");
        await Assert.ThrowsExactlyAsync<BlazeDbException>(async () => await db.CheckpointAsync());
        await db.DisposeAsync(); // still flushes what did commit

        await using var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(descriptor));
        var recovered = reopened.GetTable(descriptor);
        Assert.AreEqual(new Person(1, "Committed", 10), recovered.Get(1), "The write the undo could not take back never reached storage.");
        Assert.AreEqual(2, recovered.Count);
    }

    [TestMethod]
    public async Task Disposing_With_A_Transaction_Whose_Undo_Fails_Still_Closes_The_Log()
    {
        var comparer = new SwitchableComparer();
        var descriptor = Switchable(comparer);
        var storage = new BlazeDbInMemoryStorage();
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage, FlushInterval = TimeSpan.FromHours(1) }.AddTable(descriptor));
        var table = db.GetTable(descriptor);
        table.Insert(new Person(1, "Committed", 10));   // buffered, not yet flushed

        _ = db.BeginTransaction();
        table.Update(new Person(1, "Uncommitted", 11));
        comparer.Refuse = true;

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await db.DisposeAsync());
        comparer.Refuse = false;

        await using var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(descriptor));
        Assert.AreEqual(new Person(1, "Committed", 10), reopened.GetTable(descriptor).Get(1), "The final flush ran despite the failed undo.");
    }
}
