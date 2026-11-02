using System.Collections.Concurrent;
using Bit.BlazeDb.Querying;
using Bit.BlazeDb.Storage;
using Bit.BlazeDb.Tests.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using static Bit.BlazeDb.Tests.ReviewRegressionTests;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Regressions for what a second review of the hardening pass found: change notifications delivered on
/// another thread than the commit's (and able to deadlock it), bulk writes fed by a query over their own
/// table, a pooled EF Core context losing a handler's error, an undo failure hiding the exception that made
/// the rollback happen, a live query missing a commit made while it started, a database subscriber handed
/// part of a commit, a streaming query reporting a bare enumerator error, an export of a database whose undo
/// failed, an in-memory database locked for good by one failed undo, and an import reading its file from the
/// wrong place once it stopped copying it. Then what a fresh reviewer found in those fixes: a transaction
/// committing what a failed unit could not take back, and a rollback stopping at the first write it could
/// not undo.
/// </summary>
[TestClass]
public class ReviewFollowUpTests
{
    private static readonly BlazeDbTableDescriptor<int, Person> Others = new(
        "others",
        static p => p.Id,
        PersonTable.Descriptor.RowWriter,
        PersonTable.Descriptor.RowReader,
        PersonTable.Descriptor.KeyWriter,
        PersonTable.Descriptor.KeyReader);

    private sealed class UnitFailure() : Exception("the unit's own failure");

    [TestMethod]
    public async Task Each_Thread_Delivers_Its_Own_Commits_Without_Waiting_For_Another_Threads_Handlers()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        var deliveredOn = new ConcurrentDictionary<int, int>();
        Task? second = null;
        var secondFinished = false;
        people.Changed += (_, e) =>
        {
            var key = e.Changes[0].Key;
            deliveredOn[key] = Environment.CurrentManagedThreadId;
            if (key == 2)
            {
                throw new InvalidOperationException("the second writer's handler");
            }
            // A handler waiting on a write made on another thread: that write must neither wait for this
            // handler to finish nor have its notification - and its handler's failure - delivered here.
            var task = Task.Run(() => people.Insert(new Person(2, "Second", 2)));
            second = task;
            secondFinished = SpinWait.SpinUntil(() => task.IsCompleted, TimeSpan.FromSeconds(10));
        };

        people.Insert(new Person(1, "First", 1));

        Assert.IsTrue(secondFinished, "The second write completed while the first one's handler was still running.");
        var error = await Assert.ThrowsExactlyAsync<BlazeDbChangeHandlerException>(() => second!);
        Assert.AreEqual("the second writer's handler", error.InnerException!.Message);
        Assert.AreNotEqual(deliveredOn[1], deliveredOn[2], "Each commit was delivered on the thread that made it.");
    }

    [TestMethod]
    public async Task Bulk_Writes_Can_Be_Fed_By_A_Query_Over_Their_Own_Table()
    {
        var descriptor = Switchable(new SwitchableComparer());
        var byAge = (BlazeDbOrderedIndexDefinition<Person, int>)descriptor.Indexes[0];
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(descriptor));
        var people = db.GetTable(descriptor);
        people.InsertMany(Enumerable.Range(1, 10).Select(i => new Person(i, $"P{i}", i * 10)));

        people.UpsertMany(people.Range(byAge, 0, 50).Select(p => p with { Age = p.Age + 1 }));
        people.InsertMany(people.Scan().Select(p => p with { Id = p.Id + 1000 }));

        Assert.AreEqual(20, people.Count);
        CollectionAssert.AreEqual(new[] { 11, 21, 31, 41, 51, 60, 70, 80, 90, 100 },
            people.OrderBy(byAge).Where(p => p.Id <= 10).Select(p => p.Age).ToArray());
        Assert.IsEmpty(db.VerifyIntegrity());
    }

    [TestMethod]
    public void A_Pooled_Context_Reports_Every_Save_Whose_Handler_Failed()
    {
        using var test = TestDatabase.Open();
        var factory = new PooledDbContextFactory<PeopleContext>(test.Options);
        var failing = true;
        test.Database.Changed += (_, _) =>
        {
            if (failing)
            {
                throw new InvalidOperationException("handler bug");
            }
        };

        for (var id = 1; id <= 2; id++)
        {
            // The second lease is the same context, its events cleared when it went back to the pool.
            using var context = factory.CreateDbContext();
            context.People.Add(new EntityFrameworkCore.Person { Id = id, Name = "P", City = "London", Age = 30 });
            Assert.ThrowsExactly<BlazeDbChangeHandlerException>(() => context.SaveChanges(), $"save {id}");
        }

        failing = false;
        using (var context = factory.CreateDbContext())
        {
            context.People.Add(new EntityFrameworkCore.Person { Id = 3, Name = "P", City = "London", Age = 30 });
            Assert.AreEqual(1, context.SaveChanges(), "No earlier save's error surfaces from this one.");
        }
        Assert.AreEqual(3, test.People.Count);
    }

    [TestMethod]
    public async Task A_Failed_Unit_Reports_Its_Own_Exception_When_Undoing_It_Fails_Too()
    {
        var comparer = new SwitchableComparer();
        var descriptor = Switchable(comparer);
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = new BlazeDbInMemoryStorage(), FlushInterval = TimeSpan.FromHours(1) }.AddTable(descriptor));
        var table = db.GetTable(descriptor);
        table.Insert(new Person(1, "Committed", 10));

        Assert.ThrowsExactly<UnitFailure>(() => db.RunInTransaction(() =>
        {
            table.Update(new Person(1, "Uncommitted", 11));
            comparer.Refuse = true;
            throw new UnitFailure();
        }));
        comparer.Refuse = false;

        // The undo failure is not lost: it is why the database now refuses writes, and exports.
        var refused = Assert.ThrowsExactly<BlazeDbException>(() => table.Insert(new Person(2, "Refused", 20)));
        StringAssert.Contains(refused.InnerException!.Message, "comparer refuses");
        var export = await Assert.ThrowsExactlyAsync<BlazeDbException>(async () => await db.ExportAsync(new MemoryStream()));
        StringAssert.Contains(export.Message, "Reopen");
        await db.DisposeAsync();
    }

    [TestMethod]
    public async Task Without_Storage_A_Failed_Undo_Is_Reported_But_Does_Not_Stop_The_Database()
    {
        var comparer = new SwitchableComparer();
        var descriptor = Switchable(comparer);
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(descriptor));
        var table = db.GetTable(descriptor);
        table.Insert(new Person(1, "Committed", 10));

        using (db.BeginTransaction())
        {
            // A unit joined to an open transaction, whose undo fails: its own exception still propagates.
            Assert.ThrowsExactly<UnitFailure>(() => db.RunInTransaction(() =>
            {
                table.Update(new Person(1, "Uncommitted", 11));
                comparer.Refuse = true;
                throw new UnitFailure();
            }));
            comparer.Refuse = false;
        } // The transaction still lists the write the unit could not take back, and rolling it back now works.

        Assert.AreEqual(new Person(1, "Committed", 10), table.Get(1));
        table.Insert(new Person(2, "Later", 20));
        Assert.AreEqual(2, table.Count, "With nothing to reopen from, the database goes on taking writes.");
        var issue = db.VerifyIntegrity().Single();
        Assert.AreEqual("switchable", issue.Table);
        StringAssert.Contains(issue.Description, "comparer refuses");
    }

    [TestMethod]
    public async Task A_Transaction_A_Failed_Unit_Could_Not_Be_Taken_Out_Of_Refuses_To_Commit()
    {
        var comparer = new SwitchableComparer();
        var descriptor = Switchable(comparer);
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(descriptor));
        var table = db.GetTable(descriptor);
        table.Insert(new Person(1, "Committed", 10));
        var reported = 0;
        table.Changed += (_, _) => reported++;

        var txn = db.BeginTransaction();
        try
        {
            db.RunInTransaction(() =>
            {
                table.Update(new Person(1, "Uncommitted", 11));
                comparer.Refuse = true;
                throw new UnitFailure();
            });
        }
        catch (UnitFailure)
        {
            // What a caller does with a unit it can do without: carry on with the rest of the transaction.
        }
        comparer.Refuse = false;

        var refused = Assert.ThrowsExactly<BlazeDbException>(txn.Commit);
        StringAssert.Contains(refused.InnerException!.Message, "comparer refuses");
        Assert.AreEqual(new Person(1, "Committed", 10), table.Get(1), "The failed unit's write was rolled back, not committed.");
        Assert.AreEqual(0, reported);
        txn.Dispose(); // over already: does nothing
    }

    [TestMethod]
    public async Task A_Rollback_Undoes_Every_Write_It_Can_When_One_Undo_Fails()
    {
        var comparer = new SwitchableComparer();
        var descriptor = Switchable(comparer);
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(PersonTable.Descriptor).AddTable(descriptor));
        var people = db.GetTable(PersonTable.Descriptor);
        var switchable = db.GetTable(descriptor);
        switchable.Insert(new Person(1, "Committed", 10));

        Assert.ThrowsExactly<UnitFailure>(() => db.RunInTransaction(() =>
        {
            people.Insert(new Person(7, "Earlier write", 70));
            switchable.Update(new Person(1, "Uncommitted", 11));
            comparer.Refuse = true;
            throw new UnitFailure();
        }));
        comparer.Refuse = false;

        Assert.IsFalse(people.Contains(7), "The write made before the one whose undo failed was undone too.");
    }

    [TestMethod]
    public async Task A_Live_Query_Does_Not_Miss_A_Commit_Made_While_It_Starts()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "First", 1));

        Task? writer = null;
        using var live = people.Watch(table =>
        {
            var count = table.Count;
            // Another thread commits while the first run is under way. Whether its write lands before the
            // live query listens or after it, the result has to end up counting it.
            writer ??= Task.Run(() => people.Insert(new Person(2, "Second", 2)));
            writer.Wait(TimeSpan.FromMilliseconds(200));
            return count;
        });
        await writer!;

        Assert.AreEqual(2, live.Result);
    }

    [TestMethod]
    public async Task A_Database_Subscriber_Is_Never_Handed_Part_Of_A_Commit()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(PersonTable.Descriptor).AddTable(Others));
        var people = db.GetTable(PersonTable.Descriptor);
        var others = db.GetTable(Others);
        var received = new List<BlazeDbChangedEventArgs>();
        people.Changed += (_, e) =>
        {
            if (e.Changes[0].Key == 1)
            {
                // A commit made here waits to be delivered until this one has been. It is collected while only
                // the people table is watched, so the database subscriber that arrives in between must not be
                // handed its people row alone.
                db.RunInTransaction(() =>
                {
                    people.Insert(new Person(2, "P", 2));
                    others.Insert(new Person(2, "O", 2));
                });
                db.Changed += (_, args) => received.Add(args);
            }
        };

        people.Insert(new Person(1, "P", 1));
        Assert.IsEmpty(received);

        db.RunInTransaction(() =>
        {
            people.Insert(new Person(3, "P", 3));
            others.Insert(new Person(3, "O", 3));
        });
        Assert.HasCount(2, received.Single().Changes, "A commit made once it listens reaches it whole.");
    }

    [TestMethod]
    public async Task A_Stream_Whose_Source_Cannot_Go_On_Past_A_Write_Names_The_Table()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        for (var i = 0; i < 10; i++)
        {
            people.Insert(new Person(i, $"P{i}", i));
        }

        var error = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await foreach (var person in BlazeDbQuery<int, Person>.From(people).ExecuteAsync(batchSize: 100))
            {
                // What other code may do while the body awaits: an insert, which a scan cannot go on past.
                await Task.Yield();
                people.Insert(new Person(100 + person.Id, "Added", 0));
            }
        });

        StringAssert.Contains(error.Message, "people");
        Assert.IsNotNull(error.InnerException);
    }

    [TestMethod]
    public async Task An_Import_Reads_A_Memory_Stream_From_Where_It_Stands()
    {
        var export = new MemoryStream();
        await using (var source = await TestDb.OpenInMemoryAsync())
        {
            source.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));
            await source.ExportAsync(export);
        }
        var bytes = export.ToArray();

        // From the stream's position, and from a stream over part of a larger array.
        var prefixed = new MemoryStream();
        prefixed.Write([1, 2, 3]);
        prefixed.Write(bytes);
        prefixed.Position = 3;
        var framed = new byte[bytes.Length + 10];
        bytes.CopyTo(framed, 5);

        foreach (var file in new[] { prefixed, new MemoryStream(framed, 5, bytes.Length, writable: false, publiclyVisible: true) })
        {
            var storage = new BlazeDbInMemoryStorage();
            await BlazeDbDatabase.ImportAsync(file, storage);
            await using var restored = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(PersonTable.Descriptor));
            Assert.AreEqual(new Person(1, "Ada", 36), restored.GetTable(PersonTable.Descriptor).Get(1));
        }
    }
}
