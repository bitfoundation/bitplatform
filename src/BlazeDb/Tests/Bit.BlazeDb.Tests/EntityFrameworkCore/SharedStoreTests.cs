using Bit.BlazeDb.Storage;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests.EntityFrameworkCore;

/// <summary>
/// What follows from queries handing back the table's own objects when more than one context, or
/// more than one save, is involved. Each of these was a way to corrupt an index or the change
/// tracker silently; now each is either correct or a clear error.
/// </summary>
[TestClass]
public class SharedStoreTests
{
    private static DbContextOptions<PeopleContext> IsolatedOptions() =>
        new DbContextOptionsBuilder<PeopleContext>()
            .UseBlazeDb("shared-" + Guid.NewGuid().ToString("N"))
            .Options;

    private static Person Ada() => new() { Id = 1, Name = "Ada", City = "London", Age = 36 };

    [TestMethod]
    public void A_Second_Context_Saving_Stale_Originals_Gets_A_Concurrency_Error_Not_A_Duplicate_Index_Entry()
    {
        var options = IsolatedOptions();
        using var seed = new PeopleContext(options);
        seed.People.Add(Ada());
        seed.SaveChanges();

        using var a = new PeopleContext(options);
        using var b = new PeopleContext(options);
        var fromA = a.People.Single();
        var fromB = b.People.Single();
        Assert.AreSame(fromA, fromB); // The same live row, tracked twice with separate originals.

        fromA.City = "Paris";
        a.SaveChanges();

        // B's originals still say London. Applying them would retract nothing and add Paris again.
        fromB.Name = "Ada Lovelace";
        Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => b.SaveChanges());

        using var check = new PeopleContext(options);
        Assert.ContainsSingle(check.People.Where(p => p.City == "Paris").ToList());
        Assert.IsEmpty(check.People.Where(p => p.City == "London").ToList());
        Assert.ContainsSingle(check.Database.GetBlazeDb().GetTable(Person.Table).Lookup(Person.Indexes.City, "Paris"));
    }

    [TestMethod]
    public void Another_Contexts_Unsaved_Change_Is_Seen_By_Indexed_Queries_Too()
    {
        var options = IsolatedOptions();
        using var seed = new PeopleContext(options);
        seed.People.Add(Ada());
        seed.SaveChanges();

        using var a = new PeopleContext(options);
        using var b = new PeopleContext(options);
        var ada = a.People.Single();
        ada.City = "Paris"; // Changed in memory by A; the City index still says London.

        // B has nothing modified itself, so on its own it would trust the index and answer from
        // London - returning an object that says Paris, and finding nothing under Paris.
        Assert.IsEmpty(b.People.Where(p => p.City == "London").ToList());
        Assert.AreSame(ada, b.People.Single(p => p.City == "Paris"));
        Assert.AreSame(ada, b.People.Single(p => p.City == "Paris" && p.Age == 36));
        Assert.AreSequenceEqual(["Paris"], b.People.Where(p => p.City == "Paris").Select(p => p.City).ToList());

        a.SaveChanges();
        Assert.AreSame(ada, b.People.Single(p => p.City == "Paris"));
        Assert.IsEmpty(b.People.Where(p => p.City == "London").ToList());
    }

    [TestMethod]
    public void A_Disposed_Context_No_Longer_Counts_As_Having_Pending_Changes()
    {
        var options = IsolatedOptions();
        using var seed = new PeopleContext(options);
        seed.People.Add(Ada());
        seed.SaveChanges();

        using var b = new PeopleContext(options);
        using (var a = new PeopleContext(options))
        {
            a.People.Single().Name = "Ada Lovelace"; // Not an indexed property, and never saved.
        }
        // Whether or not the index is consulted, the answer is the same; this only has to not fail.
        Assert.ContainsSingle(b.People.Where(p => p.City == "London").ToList());
    }

    [TestMethod]
    public void Constraint_Violations_Surface_As_DbUpdateException()
    {
        var options = IsolatedOptions();
        using var seed = new PeopleContext(options);
        seed.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36, Email = "ada@x" });
        seed.SaveChanges();

        using (var duplicateKey = new PeopleContext(options))
        {
            duplicateKey.People.Add(new Person { Id = 1, Name = "Again", City = "Paris", Age = 1 });
            var ex = Assert.ThrowsExactly<DbUpdateException>(() => duplicateKey.SaveChanges());
            Assert.IsExactInstanceOfType<BlazeDbDuplicateKeyException>(ex.InnerException);
            Assert.ContainsSingle(ex.Entries);
        }

        using (var duplicateEmail = new PeopleContext(options))
        {
            duplicateEmail.People.Add(new Person { Id = 2, Name = "Grace", City = "NYC", Age = 45, Email = "ada@x" });
            var ex = Assert.ThrowsExactly<DbUpdateException>(() => duplicateEmail.SaveChanges());
            Assert.IsExactInstanceOfType<BlazeDbUniqueConstraintViolationException>(ex.InnerException);
        }

        using var check = new PeopleContext(options);
        Assert.ContainsSingle(check.People.ToList());
    }

    [TestMethod]
    public void Updating_A_Row_Another_Context_Deleted_Is_A_Concurrency_Error()
    {
        var options = IsolatedOptions();
        using var seed = new PeopleContext(options);
        seed.People.Add(Ada());
        seed.SaveChanges();

        using var a = new PeopleContext(options);
        using var b = new PeopleContext(options);
        var fromA = a.People.Single();
        b.People.Remove(b.People.Single());
        b.SaveChanges();

        fromA.Name = "Ada Lovelace";
        var ex = Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => a.SaveChanges());
        Assert.IsExactInstanceOfType<KeyNotFoundException>(ex.InnerException);
    }

    [TestMethod]
    public async Task A_Context_That_Knows_Fewer_Tables_Can_Open_A_Store_Written_By_One_That_Knows_More()
    {
        // The rows of the table it does not know are carried, not dropped, and become visible the
        // moment a context that does know it shares the engine.
        var storage = new BlazeDbInMemoryStorage();
        await using (var engine = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions { Storage = storage }
                         .AddTable(Person.Table).AddTable(Order.Table)))
        {
            engine.GetTable(Person.Table).Insert(Ada());
            engine.GetTable(Order.Table).Insert(new Order { Id = 10, PersonId = 1, Total = 9.5m });
        }

        var narrow = new DbContextOptionsBuilder<PeopleOnlyContext>().UseBlazeDb(storage).Options;
        var wide = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;

        using var peopleOnly = new PeopleOnlyContext(narrow);
        Assert.AreEqual("Ada", peopleOnly.People.Single().Name);
        Assert.AreSequenceEqual(["orders"], peopleOnly.Database.GetBlazeDb().UnregisteredTables);

        using var full = new PeopleContext(wide);
        Assert.AreSame(peopleOnly.Database.GetBlazeDb(), full.Database.GetBlazeDb());
        Assert.AreEqual(9.5m, full.Orders.Single(o => o.PersonId == 1).Total);
        Assert.IsEmpty(full.Database.GetBlazeDb().UnregisteredTables);

        await full.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public void Indexed_Queries_Agree_With_The_Live_Object_Before_SaveChanges()
    {
        using var context = new PeopleContext(IsolatedOptions());
        context.People.Add(Ada());
        context.SaveChanges();

        var ada = context.People.Single();
        ada.City = "Paris";
        ada.Active = true;

        // The row has changed in memory; the index entries move on SaveChanges. Until then, a query
        // through the index has to give the same answer as one that reads the object.
        Assert.AreSame(ada, context.People.Single(p => p.City == "Paris"));
        Assert.IsEmpty(context.People.Where(p => p.City == "London").ToList());
        Assert.AreSame(ada, context.People.Single(p => p.Active));
        Assert.AreSame(ada, context.People.Single(p => p.City == "Paris" && p.Age == 36));
        Assert.AreSequenceEqual([1], context.People.OrderBy(p => p.Age).Select(p => p.Id).ToList());

        context.SaveChanges();
        Assert.AreSame(ada, context.People.Single(p => p.City == "Paris"));
    }

    [TestMethod]
    public void A_Rolled_Back_Update_Leaves_The_Context_Usable_And_The_Data_Restored()
    {
        using var context = new PeopleContext(IsolatedOptions());
        context.People.Add(Ada());
        context.SaveChanges();

        var ada = context.People.Single();
        using (var transaction = context.Database.BeginTransaction())
        {
            ada.City = "Paris";
            context.SaveChanges();
            transaction.Rollback();
        }

        // The tracker no longer vouches for an object whose save was undone; the next query loads
        // the row as the database has it instead of tripping over a second entity with the same key.
        var reloaded = context.People.Single(p => p.Id == 1);
        Assert.AreEqual("London", reloaded.City);
        Assert.AreSame(reloaded, context.People.Single(p => p.City == "London"));
        Assert.ContainsSingle(context.ChangeTracker.Entries());

        reloaded.City = "Berlin";
        Assert.AreEqual(1, context.SaveChanges());
        Assert.AreEqual("Berlin", context.Database.GetBlazeDb().GetTable(Person.Table).Get(1)!.City);
    }

    [TestMethod]
    public void A_Rollback_Keeps_Entities_That_Were_Only_Added_And_Never_Saved()
    {
        using var context = new PeopleContext(IsolatedOptions());
        using (var transaction = context.Database.BeginTransaction())
        {
            context.People.Add(Ada());
            transaction.Rollback();
        }

        Assert.AreEqual(EntityState.Added, context.ChangeTracker.Entries().Single().State);
        Assert.AreEqual(1, context.SaveChanges());
    }

    [TestMethod]
    public void Single_With_A_Predicate_Tracks_Only_The_Entity_It_Returns()
    {
        using var context = new PeopleContext(IsolatedOptions());
        for (var i = 1; i <= 5; i++)
        {
            context.People.Add(new Person { Id = i, Name = "P" + i, City = "C", Age = 20 + i });
        }
        context.SaveChanges();
        context.ChangeTracker.Clear();

        var two = context.People.Single(p => p.Name == "P2");
        Assert.AreSame(two, context.ChangeTracker.Entries().Single().Entity);

        context.ChangeTracker.Clear();
        var last = context.People.OrderBy(p => p.Name).Last();
        Assert.AreSame(last, context.ChangeTracker.Entries().Single().Entity);

        context.ChangeTracker.Clear();
        var first = context.People.First(p => p.Age > 22);
        Assert.AreSame(first, context.ChangeTracker.Entries().Single().Entity);

        context.ChangeTracker.Clear();
        var filtered = context.People.Where(p => p.Age > 22).Skip(1).Take(5).ToList();
        Assert.AreEqual(filtered.Count, context.ChangeTracker.Entries().Count());
    }

    [TestMethod]
    public void EnsureCreated_Reports_Whether_It_Opened_The_Store()
    {
        var options = IsolatedOptions();
        using var first = new PeopleContext(options);
        Assert.IsTrue(first.Database.EnsureCreated());
        Assert.IsFalse(first.Database.EnsureCreated());

        using var second = new PeopleContext(options);
        Assert.IsFalse(second.Database.EnsureCreated());
    }

    [TestMethod]
    public async Task EnsureCreatedAsync_Reports_Whether_It_Opened_The_Store()
    {
        var options = IsolatedOptions();
        using var first = new PeopleContext(options);
        Assert.IsTrue(await first.Database.EnsureCreatedAsync());
        using var second = new PeopleContext(options);
        Assert.IsFalse(await second.Database.EnsureCreatedAsync());
    }

    [TestMethod]
    public void A_Context_Survives_Another_Context_Deleting_The_Store()
    {
        var options = IsolatedOptions();
        using var a = new PeopleContext(options);
        a.People.Add(Ada());
        a.SaveChanges();
        var engine = a.Database.GetBlazeDb();

        using var b = new PeopleContext(options);
        Assert.IsTrue(b.Database.EnsureDeleted());

        // A's cached engine was disposed underneath it; it reopens rather than failing.
        Assert.IsEmpty(a.People.ToList());
        Assert.AreNotSame(engine, a.Database.GetBlazeDb());
    }

    [TestMethod]
    public void A_Save_Cannot_Join_Another_Contexts_Transaction()
    {
        var options = IsolatedOptions();
        using var a = new PeopleContext(options);
        using var b = new PeopleContext(options);

        using var transaction = a.Database.BeginTransaction();
        b.People.Add(Ada());
        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => b.SaveChanges());
        Assert.Contains("Another DbContext", ex.Message);

        transaction.Rollback();
        Assert.AreEqual(1, b.SaveChanges());
    }

    [TestMethod]
    public async Task EnsureDeleted_On_Persistent_Storage_Removes_The_Log_Too()
    {
        // Deleting only the manifest is not a delete: the next open would treat the store as fresh
        // and replay wal-1.blz, bringing the rows back.
        var storage = new BlazeDbInMemoryStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;

        using (var context = new PeopleContext(options))
        {
            context.People.Add(Ada());
            context.SaveChanges();
            await context.Database.GetBlazeDb().FlushAsync();
            Assert.IsTrue(await context.Database.EnsureDeletedAsync());
        }
        Assert.IsEmpty(storage.FileNames);

        using (var context = new PeopleContext(options))
        {
            Assert.IsEmpty(context.People.ToList());
            context.People.Add(new Person { Id = 2, Name = "Grace", City = "NYC", Age = 45 });
            context.SaveChanges();
            await context.Database.GetBlazeDb().FlushAsync();
            await context.Database.EnsureDeletedAsync();
        }

        using var again = new PeopleContext(options);
        Assert.IsEmpty(again.People.ToList());
        await again.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task Overlapping_Async_And_Sync_Opens_Share_One_Engine()
    {
        var storage = new SlowOpenStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using var a = new PeopleContext(options);
        using var b = new PeopleContext(options);

        var opening = a.Database.EnsureCreatedAsync();
        await Task.Delay(50);
        Assert.IsFalse(opening.IsCompleted);

        // Reads block until the first open is through; nothing recovers the same storage twice.
        var sync = Task.Run(() => b.Database.EnsureCreated());
        storage.Release();
        Assert.IsTrue(await opening);
        Assert.IsFalse(await sync);
        Assert.AreEqual(1, storage.ManifestReads);
        Assert.AreSame(a.Database.GetBlazeDb(), b.Database.GetBlazeDb());

        await a.Database.EnsureDeletedAsync();
    }

    /// <summary>Holds the first manifest read open so an open can be caught in flight.</summary>
    private sealed class SlowOpenStorage : IBlazeDbStorage
    {
        private readonly BlazeDbInMemoryStorage _inner = new();
        private readonly TaskCompletionSource _gate = new();

        public int ManifestReads;

        public void Release() => _gate.TrySetResult();

        public async ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default)
        {
            if (name == "manifest.blz")
            {
                Interlocked.Increment(ref ManifestReads);
                await _gate.Task;
            }
            return await _inner.ReadAsync(name, cancellationToken);
        }

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            _inner.WriteAtomicAsync(name, data, cancellationToken);

        public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            _inner.AppendAsync(name, data, cancellationToken);

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(name, cancellationToken);
    }


    [TestMethod]
    public void A_Query_After_A_Failed_Save_Returns_The_Tracked_Entity_And_The_Retry_Succeeds()
    {
        // The failed save was rolled back, which put the earlier values back into the table as a
        // separate instance. The tracker still holds the entity the application changed; a query
        // must hand that one back - as every provider does for a key it already tracks - rather than
        // fail over a second object with the same key, and the corrected retry must then land.
        using var context = new PeopleContext(IsolatedOptions());
        context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36, Email = "ada@x" });
        context.People.Add(new Person { Id = 2, Name = "Grace", City = "NYC", Age = 45, Email = "grace@x" });
        context.SaveChanges();

        var ada = context.People.Single(p => p.Id == 1);
        var grace = context.People.Single(p => p.Id == 2);
        ada.City = "Paris";
        grace.Email = "ada@x"; // Collides with Ada's unique email.
        Assert.ThrowsExactly<DbUpdateException>(() => context.SaveChanges());

        // The database holds London again; the context holds Paris. The query answers for the context.
        Assert.AreEqual("London", context.Database.GetBlazeDb().GetTable(Person.Table).Get(1)!.City);
        Assert.AreSame(ada, context.People.Single(p => p.Id == 1));
        Assert.AreSame(ada, context.People.OrderBy(p => p.Id).First());
        Assert.AreEqual(2, context.People.ToList().Count);
        Assert.AreEqual(2, context.ChangeTracker.Entries().Count());

        grace.Email = "grace@y";
        Assert.AreEqual(2, context.SaveChanges());
        Assert.AreSame(ada, context.Database.GetBlazeDb().GetTable(Person.Table).Get(1));
        Assert.AreEqual("Paris", context.People.Single(p => p.Id == 1).City);
        Assert.ContainsSingle(context.Database.GetBlazeDb().GetTable(Person.Table).Lookup(Person.Indexes.City, "Paris"));
        Assert.IsEmpty(context.Database.GetBlazeDb().GetTable(Person.Table).Lookup(Person.Indexes.City, "London"));
    }

    [TestMethod]
    public void Disposing_A_Context_With_An_Open_Transaction_Rolls_It_Back()
    {
        // The engine allows one transaction at a time, so one left open by a context that went away
        // would silently pull every later write on the store into a batch nobody can commit.
        var options = IsolatedOptions();
        var engine = new PeopleContext(options).Database.GetBlazeDb();

        using (var abandoned = new PeopleContext(options))
        {
            abandoned.Database.BeginTransaction();
            abandoned.People.Add(Ada());
            abandoned.SaveChanges();
            Assert.IsTrue(engine.HasActiveTransaction);
        }

        Assert.IsFalse(engine.HasActiveTransaction);
        using var next = new PeopleContext(options);
        Assert.IsEmpty(next.People.ToList());
        next.People.Add(Ada());
        Assert.AreEqual(1, next.SaveChanges());
        Assert.AreEqual("Ada", engine.GetTable(Person.Table).Get(1)!.Name);
    }

    [TestMethod]
    public void A_Detached_Entity_Updates_Its_Row_Even_When_An_Indexed_Value_Changed()
    {
        // The disconnected-update pattern: the entity is a new object, so its original values are
        // its own current ones and say nothing about what the City index holds.
        var options = IsolatedOptions();
        using (var seed = new PeopleContext(options))
        {
            seed.People.Add(Ada());
            seed.SaveChanges();
        }

        using (var context = new PeopleContext(options))
        {
            context.Update(new Person { Id = 1, Name = "Ada", City = "Paris", Age = 36 });
            Assert.AreEqual(1, context.SaveChanges());
        }

        using (var context = new PeopleContext(options))
        {
            var attached = new Person { Id = 1, Name = "Ada", City = "Paris", Age = 36 };
            context.Attach(attached);
            attached.City = "Berlin";
            Assert.AreEqual(1, context.SaveChanges());
        }

        using (var check = new PeopleContext(options))
        {
            Assert.AreEqual("Berlin", check.People.Single(p => p.City == "Berlin").City);
            Assert.IsEmpty(check.People.Where(p => p.City == "London" || p.City == "Paris").ToList());
            var table = check.Database.GetBlazeDb().GetTable(Person.Table);
            Assert.IsEmpty(table.Lookup(Person.Indexes.City, "London"));
            Assert.IsEmpty(table.Lookup(Person.Indexes.City, "Paris"));
            Assert.ContainsSingle(table.Lookup(Person.Indexes.City, "Berlin"));
        }

        using (var context = new PeopleContext(options))
        {
            context.People.Remove(new Person { Id = 1 });
            Assert.AreEqual(1, context.SaveChanges());
        }

        using var empty = new PeopleContext(options);
        Assert.IsEmpty(empty.People.ToList());
        Assert.IsEmpty(empty.Database.GetBlazeDb().GetTable(Person.Table).Lookup(Person.Indexes.City, "Berlin"));
    }

    [TestMethod]
    public void Deleting_A_Row_Another_Context_Deleted_Is_A_Concurrency_Error()
    {
        var options = IsolatedOptions();
        using var seed = new PeopleContext(options);
        seed.People.Add(Ada());
        seed.SaveChanges();

        using var a = new PeopleContext(options);
        using var b = new PeopleContext(options);
        var fromA = a.People.Single();
        b.People.Remove(b.People.Single());
        b.SaveChanges();

        a.People.Remove(fromA);
        var ex = Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => a.SaveChanges());
        Assert.IsExactInstanceOfType<KeyNotFoundException>(ex.InnerException);
    }

    [TestMethod]
    public void A_Save_That_Fails_Inside_A_Transaction_Leaves_None_Of_Its_Writes_In_It()
    {
        using var context = new PeopleContext(IsolatedOptions());
        context.People.Add(new Person { Id = 1, Name = "Ada", City = "London", Age = 36, Email = "ada@x" });
        context.SaveChanges();
        var table = context.Database.GetBlazeDb().GetTable(Person.Table);

        using (var transaction = context.Database.BeginTransaction())
        {
            context.People.Add(new Person { Id = 2, Name = "Grace", City = "NYC", Age = 45 });
            var clash = new Person { Id = 3, Name = "Alan", City = "London", Age = 41, Email = "ada@x" };
            context.People.Add(clash);
            Assert.ThrowsExactly<DbUpdateException>(() => context.SaveChanges());

            // The transaction is as it was before the save, and still open.
            Assert.IsNull(table.Get(2));
            Assert.IsNull(table.Get(3));

            clash.Email = "alan@x";
            Assert.AreEqual(2, context.SaveChanges());
            transaction.Commit();
        }

        Assert.AreSequenceEqual([1, 2, 3], table.Scan().Select(p => p.Id).Order());
    }

    [TestMethod]
    public async Task A_Synchronous_Open_That_Cannot_Wait_Is_Finished_By_EnsureCreatedAsync_Not_Repeated()
    {
        var storage = new SlowOpenStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using var context = new PeopleContext(options);

        // A thread with a synchronization context cannot wait for asynchronous storage.
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        try
        {
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => context.People.ToList());
            Assert.Contains("EnsureCreatedAsync", ex.Message);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        storage.Release();
        Assert.IsTrue(await context.Database.EnsureCreatedAsync());
        Assert.AreEqual(1, storage.ManifestReads); // One open, one engine, one flusher on the storage.
        Assert.IsEmpty(context.People.ToList());

        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task EnsureDeleted_On_A_Thread_That_Cannot_Wait_Refuses_Before_Touching_The_Store()
    {
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(new BlazeDbInMemoryStorage()).Options;
        using var context = new PeopleContext(options);
        context.Database.EnsureCreated();
        var engine = context.Database.GetBlazeDb();

        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        try
        {
            var ex = Assert.ThrowsExactly<InvalidOperationException>(() => context.Database.EnsureDeleted());
            Assert.Contains("EnsureDeletedAsync", ex.Message);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.AreSame(engine, context.Database.GetBlazeDb());
        Assert.IsTrue(await context.Database.EnsureDeletedAsync());
    }

    [TestMethod]
    public void A_Context_Whose_Reopen_Failed_Does_Not_Fall_Back_To_The_Released_Engine()
    {
        var storage = new FailingReadStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using var a = new PeopleContext(options);
        a.People.Add(Ada());
        a.SaveChanges();

        using (var b = new PeopleContext(options))
        {
            Assert.IsTrue(b.Database.EnsureDeleted());
        }

        storage.FailReads = true;
        Assert.ThrowsExactly<IOException>(() => a.People.ToList());

        // Not the engine B released, still holding Ada: the store was deleted, so it opens empty.
        storage.FailReads = false;
        Assert.IsEmpty(a.People.ToList());
        a.Database.EnsureDeleted();
    }

    /// <summary>A backend whose reads can be made to fail, to make an open throw.</summary>
    private sealed class FailingReadStorage : IBlazeDbStorage
    {
        private readonly BlazeDbInMemoryStorage _inner = new();

        public bool FailReads { get; set; }

        public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
            FailReads ? throw new IOException("The storage is unavailable.") : _inner.ReadAsync(name, cancellationToken);

        public ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            _inner.WriteAtomicAsync(name, data, cancellationToken);

        public ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default) =>
            _inner.AppendAsync(name, data, cancellationToken);

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(name, cancellationToken);
    }
}
