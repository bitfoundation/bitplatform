using Bit.BlazeDb;
using Bit.BlazeDb.Serialization;
using Bit.BlazeDb.Storage;
using Bit.BlazeDb.Tests.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Change notifications and live queries: what a commit reports, when, and that a subscriber only ever
/// sees committed states.
/// </summary>
[TestClass]
public class ChangeNotificationTests
{
    private sealed class Recorder<TKey, TRow>
        where TKey : notnull
    {
        public List<BlazeDbTableChangedEventArgs<TKey, TRow>> Events { get; } = [];

        public Recorder(BlazeDbTable<TKey, TRow> table) => table.Changed += (_, e) => Events.Add(e);

        public BlazeDbRowChange<TKey, TRow> Single() => Events.Single().Changes.Single();
    }

    [TestMethod]
    public async Task Each_Autocommit_Write_Reports_What_It_Did()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        var recorder = new Recorder<int, Person>(people);

        people.Insert(new Person(1, "Ada", 36));
        Assert.AreEqual(new BlazeDbRowChange<int, Person>(BlazeDbChangeKind.Inserted, 1, null, new Person(1, "Ada", 36)), recorder.Single());

        recorder.Events.Clear();
        people.Update(new Person(1, "Ada Lovelace", 37));
        Assert.AreEqual(new BlazeDbRowChange<int, Person>(BlazeDbChangeKind.Updated, 1, new Person(1, "Ada", 36), new Person(1, "Ada Lovelace", 37)), recorder.Single());

        recorder.Events.Clear();
        Assert.IsTrue(people.Delete(1));
        Assert.AreEqual(new BlazeDbRowChange<int, Person>(BlazeDbChangeKind.Deleted, 1, new Person(1, "Ada Lovelace", 37), null), recorder.Single());

        recorder.Events.Clear();
        Assert.IsFalse(people.Delete(1));
        Assert.IsFalse(people.TryUpdate(new Person(1, "Ghost", 0)));
        Assert.AreEqual(0, recorder.Events.Count, "A write that changes nothing reports nothing.");
    }

    [TestMethod]
    public async Task A_Transaction_Reports_Once_With_The_Net_Change_Per_Row()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.InsertMany([new Person(1, "Ada", 36), new Person(2, "Grace", 45), new Person(3, "Alan", 41)]);
        var recorder = new Recorder<int, Person>(people);

        using (var txn = db.BeginTransaction())
        {
            people.Update(new Person(1, "Ada L", 37));
            people.Update(new Person(1, "Ada Lovelace", 38)); // two updates: one change, first old to last new
            people.Insert(new Person(4, "Temp", 0));
            people.Delete(4);                                  // inserted and gone again: nothing
            people.Delete(2);
            people.Insert(new Person(2, "Grace Hopper", 46));  // deleted and back: an update
            people.Delete(3);
            Assert.AreEqual(0, recorder.Events.Count, "Nothing is reported before the commit.");
            txn.Commit();
        }

        var changes = recorder.Events.Single().Changes;
        CollectionAssert.AreEqual(new[]
        {
            new BlazeDbRowChange<int, Person>(BlazeDbChangeKind.Updated, 1, new Person(1, "Ada", 36), new Person(1, "Ada Lovelace", 38)),
            new BlazeDbRowChange<int, Person>(BlazeDbChangeKind.Updated, 2, new Person(2, "Grace", 45), new Person(2, "Grace Hopper", 46)),
            new BlazeDbRowChange<int, Person>(BlazeDbChangeKind.Deleted, 3, new Person(3, "Alan", 41), null),
        }, changes.ToArray());
    }

    [TestMethod]
    public async Task Rolled_Back_Writes_Are_Never_Reported()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        var recorder = new Recorder<int, Person>(people);

        using (db.BeginTransaction())
        {
            people.Insert(new Person(1, "Ada", 36));
        }
        Assert.ThrowsExactly<InvalidOperationException>(() => db.RunInTransaction(() =>
        {
            people.Insert(new Person(2, "Grace", 45));
            throw new InvalidOperationException();
        }));
        using (var txn = db.BeginTransaction())
        {
            people.Insert(new Person(3, "Kept", 1));
            var savepoint = txn.CreateSavepoint();
            people.Insert(new Person(4, "Undone", 1));
            txn.RollbackTo(savepoint);
            txn.Commit();
        }

        Assert.AreEqual(1, recorder.Events.Count);
        Assert.AreEqual(3, recorder.Single().Key);
    }

    [TestMethod]
    public async Task A_Commit_The_Log_Refuses_Is_Not_Reported()
    {
        // A serializer that refuses one row: the commit is undone, so there is nothing to report.
        var fragile = new BlazeDbTableDescriptor<int, Person>("fragile", static p => p.Id,
            static (w, p) =>
            {
                if (p.Name == "boom")
                {
                    throw new InvalidOperationException();
                }
                PersonTable.Descriptor.RowWriter(w, p);
            },
            PersonTable.Descriptor.RowReader, PersonTable.Descriptor.KeyWriter, PersonTable.Descriptor.KeyReader);
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = new BlazeDbInMemoryStorage() }.AddTable(fragile));
        var table = db.GetTable(fragile);
        var recorder = new Recorder<int, Person>(table);

        Assert.ThrowsExactly<InvalidOperationException>(() => table.Insert(new Person(1, "boom", 0)));
        Assert.ThrowsExactly<InvalidOperationException>(() => table.InsertMany([new Person(2, "fine", 0), new Person(3, "boom", 0)]));

        Assert.AreEqual(0, recorder.Events.Count);
    }

    [TestMethod]
    public async Task Bulk_And_Composed_Writes_Report_Once_At_The_Outermost_Commit()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        var recorder = new Recorder<int, Person>(people);

        people.InsertMany(Enumerable.Range(1, 5).Select(i => new Person(i, $"P{i}", i)));
        Assert.AreEqual(1, recorder.Events.Count);
        Assert.AreEqual(5, recorder.Events[0].Changes.Count);

        db.RunInTransaction(() =>
        {
            people.DeleteMany([1, 2]);
            db.RunInTransaction(() => people.Upsert(new Person(3, "Changed", 3)));
            Assert.AreEqual(1, recorder.Events.Count, "Nested units report with the outermost commit.");
        });
        Assert.AreEqual(2, recorder.Events.Count);
        Assert.AreEqual(3, recorder.Events[1].Changes.Count);
    }

    [TestMethod]
    public async Task The_Database_Event_Follows_Every_Table_After_Their_Own()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(PersonTable.Descriptor).AddTable(Setting.Table));
        var people = db.GetTable(PersonTable.Descriptor);
        var settings = db.GetTable(Setting.Table);
        var order = new List<string>();
        people.Changed += (_, _) => order.Add("people");
        settings.Changed += (_, _) => order.Add("settings");
        var all = new List<BlazeDbChangedEventArgs>();
        db.Changed += (_, e) => { order.Add("database"); all.Add(e); };

        db.RunInTransaction(() =>
        {
            settings.Insert(new Setting { Name = "theme", Value = "dark" });
            people.Insert(new Person(1, "Ada", 36));
        });

        CollectionAssert.AreEqual(new[] { "settings", "people", "database" }, order);
        CollectionAssert.AreEqual(new[] { "Setting", "people" }, all.Single().Changes.Select(c => c.Table).ToArray());
        Assert.AreEqual(1, all[0].Changes[1].Key);
        Assert.AreEqual(BlazeDbChangeKind.Inserted, all[0].Changes[1].Kind);
        Assert.IsNull(all[0].Changes[1].OldRow);
    }

    [TestMethod]
    public async Task A_Handler_Sees_The_Committed_State_And_May_Write()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        var seen = new List<(int Key, int Count)>();
        people.Changed += (_, e) =>
        {
            seen.Add((e.Changes[0].Key, people.Count));
            // A write from inside a handler is reported after this notification has finished.
            if (e.Changes[0].Key == 1)
            {
                people.Insert(new Person(2, "Follow-up", 0));
                Assert.AreEqual(1, seen.Count);
            }
        };

        people.Insert(new Person(1, "Ada", 36));

        CollectionAssert.AreEqual(new[] { (1, 1), (2, 2) }, seen);
    }

    [TestMethod]
    public async Task A_Throwing_Handler_Does_Not_Undo_The_Write_Or_Silence_The_Others()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        var reached = 0;
        people.Changed += (_, _) => throw new InvalidOperationException("handler bug");
        people.Changed += (_, _) => reached++;
        db.Changed += (_, _) => reached++;

        var error = Assert.ThrowsExactly<BlazeDbChangeHandlerException>(() => people.Insert(new Person(1, "Ada", 36)));

        StringAssert.Contains(error.Message, "committed");
        Assert.IsInstanceOfType<InvalidOperationException>(error.InnerException);
        Assert.IsTrue(people.Contains(1));
        Assert.AreEqual(2, reached, "The other table handler and the database handler both heard of the commit.");
    }

    [TestMethod]
    public async Task A_Replica_Reports_A_Reload_As_A_Reset()
    {
        var storage = new BlazeDbInMemoryStorage();
        await using var writer = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(PersonTable.Descriptor));
        await using var replica = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage, ReadOnly = true }.AddTable(PersonTable.Descriptor));
        var replicaPeople = replica.GetTable(PersonTable.Descriptor);
        var recorder = new Recorder<int, Person>(replicaPeople);
        var databaseResets = 0;
        replica.Changed += (_, e) => databaseResets += e.IsReset ? 1 : 0;
        using var count = replicaPeople.Watch(t => t.Count);

        writer.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
        await writer.FlushAsync();
        await replica.ReloadAsync();

        Assert.IsTrue(recorder.Events.Single().IsReset);
        Assert.AreEqual(0, recorder.Events[0].Changes.Count);
        Assert.AreEqual(1, databaseResets);
        Assert.AreEqual(1, count.Result);
    }

    [TestMethod]
    public async Task A_Live_Query_Refreshes_Only_For_Relevant_Commits()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(TodoItem.Table));
        var todos = db.GetTable(TodoItem.Table);
        var work = Guid.NewGuid();
        todos.Insert(new TodoItem { Id = work, Title = "Write", Category = "work" });

        using var live = todos.Watch(t => t.Lookup(TodoItem.Indexes.Category, "work").Select(x => x.Title).ToList(), relevantTo: t => t.Category == "work");
        var raised = 0;
        live.Changed += (_, _) => raised++;
        CollectionAssert.AreEqual(new[] { "Write" }, live.Result);

        todos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = "Shop", Category = "home" });
        Assert.AreEqual(0, live.Version, "A row the query cannot see does not re-run it.");

        todos.Update(new TodoItem { Id = work, Title = "Write more", Category = "work" });
        CollectionAssert.AreEqual(new[] { "Write more" }, live.Result);

        todos.Update(new TodoItem { Id = work, Title = "Write more", Category = "home" }); // leaves the result: the old row was relevant
        Assert.AreEqual(0, live.Result.Count);
        Assert.AreEqual(2, raised);

        live.Dispose();
        todos.Insert(new TodoItem { Id = Guid.NewGuid(), Title = "Ignored", Category = "work" });
        Assert.AreEqual(2, live.Version);
        Assert.ThrowsExactly<ObjectDisposedException>(live.Refresh);
    }

    [TestMethod]
    public void SaveChanges_Reports_One_Commit()
    {
        using var test = TestDatabase.Open();
        var recorder = new Recorder<int, EntityFrameworkCore.Person>(test.People);
        using var context = test.CreateContext();

        context.People.Add(new EntityFrameworkCore.Person { Id = 1, Name = "Ada", City = "London", Age = 36 });
        context.People.Add(new EntityFrameworkCore.Person { Id = 2, Name = "Alan", City = "Wilmslow", Age = 41 });
        context.SaveChanges();
        var ada = context.People.Single(p => p.Id == 1);
        ada.Age = 37;
        context.SaveChanges();

        Assert.AreEqual(2, recorder.Events.Count);
        Assert.AreEqual(2, recorder.Events[0].Changes.Count);
        var update = recorder.Events[1].Changes.Single();
        Assert.AreEqual(BlazeDbChangeKind.Updated, update.Kind);
        Assert.AreEqual(36, update.OldRow!.Age);
        Assert.AreEqual(37, update.NewRow!.Age);
    }

    [TestMethod]
    public async Task Concurrent_Writers_Lose_No_Notification()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        var reported = 0;
        people.Changed += (_, e) => Interlocked.Add(ref reported, e.Changes.Count);

        await Task.WhenAll(Enumerable.Range(0, 8).Select(writer => Task.Run(() =>
        {
            for (var i = 0; i < 250; i++)
            {
                people.Insert(new Person(writer * 1000 + i, "x", i));
            }
        })));

        Assert.AreEqual(2000, people.Count);
        Assert.AreEqual(2000, reported);
    }
}
