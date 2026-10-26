using Bit.BlazeDb;
using Bit.BlazeDb.Querying;
using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Invariants of the core write path: what a refused write leaves behind, how the engine treats
/// rows it cannot store, how disposal ends an open transaction, and the integrity check that
/// proves the rows and indexes agree.
/// </summary>
[TestClass]
public class CoreCorrectnessTests
{
    /// <summary>An ordering that refuses to compare the age 13, standing in for any comparer or selector that throws.</summary>
    private sealed class FragileComparer : IComparer<int>
    {
        public int Compare(int x, int y) =>
            x == 13 || y == 13 ? throw new InvalidOperationException("The comparer refuses 13.") : x.CompareTo(y);
    }

    private static readonly BlazeDbHashIndexDefinition<Person, string> ByName = new("Name", static p => p.Name);
    private static readonly BlazeDbOrderedIndexDefinition<Person, int> ByAge = new("Age", static p => p.Age, new FragileComparer());

    // The hash index comes first, so a failure in the ordered one happens after another index has
    // already been updated - the case that needs unwinding.
    private static readonly BlazeDbTableDescriptor<int, Person> Fragile = new(
        "fragile",
        static p => p.Id,
        PersonTable.Descriptor.RowWriter,
        PersonTable.Descriptor.RowReader,
        PersonTable.Descriptor.KeyWriter,
        PersonTable.Descriptor.KeyReader,
        [ByName, ByAge]);

    private static async Task<(BlazeDbDatabase Db, BlazeDbTable<int, Person> Table)> OpenFragileAsync(IBlazeDbStorage? storage = null)
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions
        {
            Storage = storage,
            FlushInterval = TimeSpan.FromHours(1),
        }.AddTable(Fragile));
        var table = db.GetTable(Fragile);
        table.Insert(new Person(1, "Ada", 36));
        table.Insert(new Person(2, "Grace", 45));
        return (db, table);
    }

    private static void AssertHealthy(BlazeDbDatabase db) =>
        Assert.AreEqual(0, db.VerifyIntegrity().Count, string.Join(Environment.NewLine, db.VerifyIntegrity()));

    [TestMethod]
    public async Task An_Insert_An_Index_Refuses_Leaves_No_Trace()
    {
        var (db, table) = await OpenFragileAsync();
        await using var _ = db;

        Assert.ThrowsExactly<InvalidOperationException>(() => table.Insert(new Person(3, "Alan", 13)));

        Assert.AreEqual(2, table.Count);
        Assert.IsFalse(table.Contains(3));
        Assert.AreEqual(0, table.CountBy(ByName, "Alan"));
        CollectionAssert.AreEqual(new[] { 1, 2 }, table.OrderBy(ByAge).Select(p => p.Id).ToArray());
        AssertHealthy(db);
    }

    [TestMethod]
    public async Task An_Update_An_Index_Refuses_Keeps_The_Old_Row_Fully_Indexed()
    {
        var (db, table) = await OpenFragileAsync();
        await using var _ = db;
        var before = table.Get(2);

        Assert.ThrowsExactly<InvalidOperationException>(() => table.Update(new Person(2, "Grace Hopper", 13)));

        Assert.AreSame(before, table.Get(2));
        Assert.AreEqual(1, table.CountBy(ByName, "Grace"));
        Assert.AreEqual(0, table.CountBy(ByName, "Grace Hopper"));
        CollectionAssert.AreEqual(new[] { 2 }, table.Range(ByAge, 40, 50).Select(p => p.Id).ToArray());
        AssertHealthy(db);
    }

    [TestMethod]
    public async Task A_Refused_Write_Inside_A_Transaction_Leaves_The_Earlier_Writes_Committable()
    {
        var storage = new BlazeDbInMemoryStorage();
        var (db, table) = await OpenFragileAsync(storage);
        await using (db)
        {
            using (var txn = db.BeginTransaction())
            {
                table.Insert(new Person(3, "Alan", 41));
                Assert.ThrowsExactly<InvalidOperationException>(() => table.Upsert(new Person(1, "Ada", 13)));
                txn.Commit();
            }
            AssertHealthy(db);
        }

        await using var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(Fragile));
        var recovered = reopened.GetTable(Fragile);
        Assert.AreEqual(new Person(1, "Ada", 36), recovered.Get(1));
        Assert.AreEqual(new Person(3, "Alan", 41), recovered.Get(3));
        AssertHealthy(reopened);
    }

    [TestMethod]
    public async Task A_Delete_An_Index_Refuses_Keeps_The_Row()
    {
        var descriptor = new BlazeDbTableDescriptor<int, Person>(
            "fragile-delete",
            static p => p.Id,
            PersonTable.Descriptor.RowWriter,
            PersonTable.Descriptor.RowReader,
            PersonTable.Descriptor.KeyWriter,
            PersonTable.Descriptor.KeyReader,
            [ByName, new BlazeDbHashIndexDefinition<Person, int>("Selector", static p => p.Name == "Doomed" ? throw new InvalidOperationException() : p.Age)]);
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(descriptor));
        var table = db.GetTable(descriptor);
        table.Insert(new Person(1, "Ada", 36));

        Assert.ThrowsExactly<InvalidOperationException>(() => table.Insert(new Person(2, "Doomed", 1)));
        Assert.IsFalse(table.Contains(2));
        Assert.AreEqual(0, table.CountBy(ByName, "Doomed"));
        AssertHealthy(db);
    }

    [TestMethod]
    public async Task A_Null_Row_Is_Refused_With_The_Table_Named()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);

        var error = Assert.ThrowsExactly<ArgumentNullException>(() => people.Insert(null!));
        StringAssert.Contains(error.Message, "people");
        Assert.ThrowsExactly<ArgumentNullException>(() => people.Upsert(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => people.Update(null!));
        Assert.AreEqual(0, people.Count);
    }

    [TestMethod]
    public async Task A_Null_Key_Is_Refused_With_The_Key_Property_Named()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(Setting.Table));
        var settings = db.GetTable(Setting.Table);

        var error = Assert.ThrowsExactly<ArgumentException>(() => settings.Insert(new Setting { Name = null!, Value = "x" }));
        StringAssert.Contains(error.Message, "Name");
        Assert.AreEqual(0, settings.Count);
    }

    [TestMethod]
    public async Task VerifyIntegrity_Reports_An_Indexed_Value_Changed_In_Place()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(Account.Table));
        var accounts = db.GetTable(Account.Table);
        accounts.Insert(new Account { Id = 1, Username = "ada", TenantId = 1, Email = "ada@example.com" });
        AssertHealthy(db);

        accounts.Get(1)!.Username = "countess";

        var issues = db.VerifyIntegrity();
        Assert.IsTrue(issues.Any(i => i.Table == "accounts" && i.Index == "Username"), string.Join(Environment.NewLine, issues));
    }

    [TestMethod]
    public async Task VerifyIntegrity_Reports_A_Key_Changed_In_Place()
    {
        await using var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions().AddTable(Account.Table));
        var accounts = db.GetTable(Account.Table);
        accounts.Insert(new Account { Id = 1, Username = "ada", Email = "ada@example.com" });

        accounts.Get(1)!.Id = 99;

        var issue = db.VerifyIntegrity().Single();
        Assert.IsNull(issue.Index);
        StringAssert.Contains(issue.Description, "99");
    }

    [TestMethod]
    public async Task Disposing_With_A_Transaction_Open_Rolls_It_Back()
    {
        var storage = new BlazeDbInMemoryStorage();
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(PersonTable.Descriptor));
        var people = db.GetTable(PersonTable.Descriptor);
        people.Insert(new Person(1, "Committed", 1));

        var txn = db.BeginTransaction();
        people.Insert(new Person(2, "Uncommitted", 2));
        people.Update(new Person(1, "Overwritten", 3));
        await db.DisposeAsync();

        // Memory describes what storage holds, and the abandoned transaction can no longer commit.
        Assert.AreEqual(new Person(1, "Committed", 1), people.Get(1));
        Assert.IsFalse(people.Contains(2));
        Assert.ThrowsExactly<ObjectDisposedException>(txn.Commit);
        txn.Dispose(); // the usual `using` pattern must not throw

        await using var reopened = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }.AddTable(PersonTable.Descriptor));
        var recovered = reopened.GetTable(PersonTable.Descriptor);
        Assert.AreEqual(1, recovered.Count);
        Assert.AreEqual(new Person(1, "Committed", 1), recovered.Get(1));
    }

    [TestMethod]
    public async Task Disposing_Twice_At_Once_Is_Safe()
    {
        var db = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = new BlazeDbInMemoryStorage() }.AddTable(PersonTable.Descriptor));
        db.GetTable(PersonTable.Descriptor).Insert(new Person(1, "Ada", 36));

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(async () => await db.DisposeAsync())));

        Assert.ThrowsExactly<ObjectDisposedException>(() => db.BeginTransaction());
    }

    [TestMethod]
    public async Task Version_Moves_On_Every_Change_And_Only_Then()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        var versions = new List<long> { people.Version };

        people.Insert(new Person(1, "Ada", 36));
        versions.Add(people.Version);
        _ = people.Get(1);
        _ = people.Scan().ToList();
        Assert.AreEqual(versions[^1], people.Version, "Reads must not move the version.");

        Assert.IsFalse(people.Delete(42));
        Assert.AreEqual(versions[^1], people.Version, "A delete that finds nothing changes nothing.");

        using (var txn = db.BeginTransaction())
        {
            people.Update(new Person(1, "Ada Lovelace", 37));
            versions.Add(people.Version);
            txn.Rollback();
        }
        versions.Add(people.Version);

        for (var i = 1; i < versions.Count; i++)
        {
            Assert.IsTrue(versions[i] > versions[i - 1], $"Version did not move at step {i}.");
        }
    }

    [TestMethod]
    public void A_Stream_Interrupted_By_A_Foreign_Write_Throws_Instead_Of_Returning_A_Mixed_Result()
    {
        // WebAssembly's single thread, modelled as a queue: the write is posted from the loop body, so
        // it runs while the stream is suspended at its next yield - as another event handler would.
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => SingleThreadedContext.Run(async () =>
        {
            await using var db = await TestDb.OpenInMemoryAsync();
            var people = db.GetTable(PersonTable.Descriptor);
            for (var i = 0; i < 10; i++)
            {
                people.Insert(new Person(i, $"P{i}", i));
            }

            var seen = 0;
            await foreach (var _ in BlazeDbQuery<int, Person>.From(people).ExecuteAsync(batchSize: 2))
            {
                if (++seen == 2)
                {
                    SynchronizationContext.Current!.Post(_ => people.Update(new Person(5, "Changed", 5)), null);
                }
            }
        }));
        StringAssert.Contains(error.Message, "people");
    }

    /// <summary>A synchronization context that runs everything posted to it, in order, on the calling thread.</summary>
    private sealed class SingleThreadedContext : SynchronizationContext
    {
        private readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = [];

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public static void Run(Func<Task> body)
        {
            var previous = Current;
            var context = new SingleThreadedContext();
            SetSynchronizationContext(context);
            try
            {
                var task = body();
                task.ContinueWith(_ => context._queue.CompleteAdding(), TaskScheduler.Default);
                foreach (var (callback, state) in context._queue.GetConsumingEnumerable())
                {
                    callback(state);
                }
                task.GetAwaiter().GetResult();
            }
            finally
            {
                SetSynchronizationContext(previous);
            }
        }
    }

    [TestMethod]
    public async Task A_Stream_Whose_Own_Loop_Writes_Runs_To_The_End()
    {
        await using var db = await TestDb.OpenInMemoryAsync();
        var people = db.GetTable(PersonTable.Descriptor);
        for (var i = 0; i < 10; i++)
        {
            people.Insert(new Person(i, $"P{i}", i));
        }

        var seen = 0;
        await foreach (var person in BlazeDbQuery<int, Person>.From(people).ExecuteAsync(batchSize: 3))
        {
            people.Update(person with { Age = person.Age + 100 });
            seen++;
        }

        Assert.AreEqual(10, seen);
        Assert.IsTrue(people.Scan().All(p => p.Age >= 100));
    }
}
