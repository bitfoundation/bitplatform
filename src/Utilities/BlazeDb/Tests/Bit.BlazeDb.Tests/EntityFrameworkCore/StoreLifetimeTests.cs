using System.ComponentModel.DataAnnotations;
using Bit.BlazeDb.EntityFrameworkCore.Infrastructure;
using Bit.BlazeDb.Storage;
using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests.EntityFrameworkCore;

/// <summary>
/// The provider's engines over their lifetime: deleted while another context wants the store, closed by the
/// application, disposed behind the provider's back - and the concurrency tokens a save is checked against.
/// </summary>
[TestClass]
public sealed class StoreLifetimeTests
{
    [TestMethod]
    public async Task A_Context_Asking_For_The_Store_While_EnsureDeleted_Runs_Waits_And_Keeps_What_It_Writes()
    {
        var storage = new GatedStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using (var seed = new PeopleContext(options))
        {
            seed.People.Add(new Person { Id = 1, Name = "Deleted", City = "c" });
            seed.SaveChanges();
        }

        // The next write - the closing engine's final flush, or a tick of its flusher that the final flush then
        // waits for - stops until released, which holds EnsureDeleted between closing the engine and deleting.
        storage.HoldNextWrite();
        using var deleter = new PeopleContext(options);
        var deleting = deleter.Database.EnsureDeletedAsync();
        await storage.WriteHeld.WaitAsync(TimeSpan.FromSeconds(10));

        var writing = Task.Run(async () =>
        {
            using var writer = new PeopleContext(options);
            writer.People.Add(new Person { Id = 2, Name = "Kept", City = "c" });
            await writer.SaveChangesAsync();
        });

        // Before the store was deleted under the gate, the writer opened a second engine on the storage right
        // here, saved, and then lost the row to the delete.
        var savedWhileDeleting = await Task.WhenAny(writing, Task.Delay(500)) == writing;
        storage.Release();
        Assert.IsTrue(await deleting);
        await writing;
        Assert.IsFalse(savedWhileDeleting, "The writer opened the store while EnsureDeleted was still closing and deleting it.");

        using (var closer = new PeopleContext(options))
        {
            Assert.IsTrue(await closer.Database.CloseBlazeDbAsync());
        }
        using var reopened = new PeopleContext(options);
        CollectionAssert.AreEqual(new[] { 2 }, reopened.People.Select(p => p.Id).ToArray());
        await reopened.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task CloseBlazeDbAsync_Flushes_And_Closes_Without_Deleting_And_The_Store_Opens_Again()
    {
        var storage = new BlazeDbInMemoryStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using var context = new PeopleContext(options);
        context.People.Add(new Person { Id = 1, Name = "Saved", City = "c" });
        context.SaveChanges();
        var engine = context.Database.GetBlazeDb();

        Assert.IsTrue(await context.Database.CloseBlazeDbAsync());
        Assert.IsFalse(await context.Database.CloseBlazeDbAsync(), "Nothing is open any more.");
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await engine.FlushAsync());

        // The same context goes on working, on an engine opened again from what the close flushed.
        Assert.AreEqual("Saved", context.People.Single().Name);
        Assert.AreNotSame(engine, context.Database.GetBlazeDb());
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task CloseBlazeDbAsync_Leaves_An_Engine_The_Application_Passed_In_Alone()
    {
        await using var database = await BlazeDbDatabase.OpenAsync(new BlazeDbDatabaseOptions());
        using var context = new PeopleContext(new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(database).Options);
        context.People.Add(new Person { Id = 1, City = "c" });
        context.SaveChanges();

        Assert.IsFalse(await context.Database.CloseBlazeDbAsync());
        Assert.AreEqual(1, context.People.Count());
    }

    [TestMethod]
    public async Task An_Engine_The_Application_Disposed_Is_Treated_As_Closed_And_Opened_Again()
    {
        var storage = new BlazeDbInMemoryStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using (var context = new PeopleContext(options))
        {
            context.People.Add(new Person { Id = 1, Name = "Saved", City = "c" });
            context.SaveChanges();
            await context.Database.GetBlazeDb().DisposeAsync();
        }

        // It used to stay in the provider's cache, and every later context failed with ObjectDisposedException.
        using var next = new PeopleContext(options);
        Assert.AreEqual("Saved", next.People.Single().Name);
        await next.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task Two_Releases_Racing_Close_The_Engine_Opened_Between_Them_Rather_Than_Orphan_It()
    {
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(new BlazeDbInMemoryStorage()).Options;
        var extension = options.FindExtension<BlazeDbOptionsExtension>()!;
        using var context = new PeopleContext(options);
        var model = context.Model;
        var cache = new BlazeDbEngineCache();
        var key = extension.StoreKey;
        cache.GetOrCreate(extension, model, out _);

        // The first release holds the store's gate until let go; an opener and then a second release queue on it,
        // in that order.
        var holdFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = cache.ReleaseAsync(key, async () => await holdFirst.Task).AsTask();
        var opening = cache.GetOrCreateAsync(extension, model, CancellationToken.None).AsTask();
        var holdSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = cache.ReleaseAsync(key, async () => await holdSecond.Task).AsTask();

        // The opener opens the store again once the first release is done; the second release finishes only
        // after that, when it can.
        holdFirst.SetResult();
        await first;
        await Task.WhenAny(opening, Task.Delay(TimeSpan.FromSeconds(2)));
        holdSecond.SetResult();
        await second;
        var (opened, _) = await opening;

        // Whichever order they ran in, one engine is open on the storage: the opener's, still in the cache, or a
        // fresh one once the second release has closed the opener's. The second release used to skip the
        // opener's engine - leaving it open and its flusher running - and drop it from the cache, so the next
        // access opened another engine on the same storage.
        var next = cache.GetOrCreate(extension, model, out _);
        Assert.IsTrue(ReferenceEquals(next, opened) || opened.IsDisposed,
            "The engine opened between the two releases was left open outside the cache.");
        await cache.ReleaseAsync(key);
        if (!opened.IsDisposed)
        {
            await opened.DisposeAsync();
        }
    }

    [TestMethod]
    public async Task A_Context_That_Cached_An_Engine_The_Application_Disposed_Opens_The_Store_Again()
    {
        var storage = new BlazeDbInMemoryStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using var context = new PeopleContext(options);
        context.People.Add(new Person { Id = 1, Name = "Saved", City = "c" });
        context.SaveChanges();
        var engine = context.Database.GetBlazeDb();

        await engine.DisposeAsync();

        // The context had cached the engine, and disposing it changes no version: it used to keep handing out
        // the disposed one, and every query and save failed with ObjectDisposedException.
        Assert.AreEqual("Saved", context.People.Single().Name);
        Assert.AreNotSame(engine, context.Database.GetBlazeDb());
        context.People.Add(new Person { Id = 2, Name = "After", City = "c" });
        Assert.AreEqual(1, context.SaveChanges());
        await context.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task CloseBlazeDbAsync_Leaves_An_In_Memory_Store_Open_With_Its_Rows()
    {
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb("close-" + Guid.NewGuid().ToString("N")).Options;
        using (var context = new PeopleContext(options))
        {
            context.People.Add(new Person { Id = 1, Name = "Kept", City = "c" });
            context.SaveChanges();
            Assert.IsFalse(await context.Database.CloseBlazeDbAsync(), "An in-memory store has nothing to flush and is left open.");
        }

        using var next = new PeopleContext(options);
        Assert.AreEqual("Kept", next.People.Single().Name);
        await next.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task A_Close_That_Leaves_The_Store_Open_Keeps_The_Contexts_Unsaved_Changes_Visible_To_Indexed_Queries()
    {
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb("close-" + Guid.NewGuid().ToString("N")).Options;
        using (var seed = new PeopleContext(options))
        {
            seed.People.Add(new Person { Id = 1, Name = "Ada", City = "London" });
            seed.SaveChanges();
        }

        using var changing = new PeopleContext(options);
        var ada = changing.People.Single();
        ada.City = "Paris"; // Not saved: the City index still says London.
        Assert.IsFalse(await changing.Database.CloseBlazeDbAsync(), "An in-memory store is left open.");

        // The close used to take the context out of the engine's registry although the engine stayed open, so
        // another context trusted the City index and answered from London.
        using var reading = new PeopleContext(options);
        Assert.AreSame(ada, reading.People.Single(p => p.City == "Paris"));
        Assert.IsEmpty(reading.People.Where(p => p.City == "London").ToList());
        await reading.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public async Task A_Synchronous_Query_That_Cannot_Wait_For_A_Close_Says_The_Store_Is_Being_Closed()
    {
        var storage = new GatedStorage();
        var options = new DbContextOptionsBuilder<PeopleContext>().UseBlazeDb(storage).Options;
        using var context = new PeopleContext(options);
        context.People.Add(new Person { Id = 1, Name = "Saved", City = "c" });
        context.SaveChanges();

        storage.HoldNextWrite();
        using var closer = new PeopleContext(options);
        var closing = closer.Database.CloseBlazeDbAsync();
        await storage.WriteHeld.WaitAsync(TimeSpan.FromSeconds(10));

        // A synchronization context stands in for the browser's one thread: this thread cannot wait for the close.
        var previous = SynchronizationContext.Current;
        InvalidOperationException error;
        SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
        try
        {
            error = Assert.ThrowsExactly<InvalidOperationException>(() => context.People.ToList());
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
        storage.Release();
        Assert.IsTrue(await closing);

        // It used to say the store was being opened, and to await EnsureCreatedAsync.
        StringAssert.Contains(error.Message, "being closed");
        await context.Database.EnsureDeletedAsync();
    }

    // ---- Concurrency tokens ----

    [TestMethod]
    public void A_Save_Over_A_Token_Another_Context_Saved_Fails_Even_When_No_Indexed_Value_Changed()
    {
        var options = TokenStore();
        using var first = new TicketContext(options);
        using var second = new TicketContext(options);
        var mine = first.Tickets.Single();
        var theirs = second.Tickets.Single();

        mine.Title = "First";
        mine.Version++;
        first.SaveChanges();

        theirs.Notes = "Second";
        Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => second.SaveChanges());
    }

    [TestMethod]
    public void A_ConcurrencyCheck_Property_Changed_By_Another_Context_Fails_The_Second_Save()
    {
        var options = TokenStore();
        using var first = new TicketContext(options);
        using var second = new TicketContext(options);
        var mine = first.Tickets.Single();
        var theirs = second.Tickets.Single();
        mine.Title = "First";
        first.SaveChanges();

        // Title is the token; Notes is neither a token nor indexed, which used to make this a silent overwrite.
        theirs.Notes = "Second";
        var error = Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => second.SaveChanges());
        StringAssert.Contains(error.Message, "'Title'");
    }

    [TestMethod]
    public void A_Detached_Entity_With_A_Stale_Row_Version_Is_Refused_And_A_Current_One_Is_Saved()
    {
        var options = TokenStore();
        using (var context = new TicketContext(options))
        {
            var ticket = context.Tickets.Single();
            ticket.Version++;
            context.SaveChanges();
        }

        using (var stale = new TicketContext(options))
        {
            stale.Update(new Ticket { Id = 1, Title = "Stale", Version = 1 });
            Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => stale.SaveChanges());
        }

        using (var current = new TicketContext(options))
        {
            current.Update(new Ticket { Id = 1, Title = "Ticket", Notes = "Current", Version = 2 });
            Assert.AreEqual(1, current.SaveChanges());
        }

        using var check = new TicketContext(options);
        Assert.AreEqual("Current", check.Tickets.Single().Notes);
    }

    [TestMethod]
    public void Deleting_Over_A_Token_Another_Context_Saved_Fails()
    {
        var options = TokenStore();
        using var first = new TicketContext(options);
        using var second = new TicketContext(options);
        var theirs = second.Tickets.Single();
        first.Tickets.Single().Version++;
        first.SaveChanges();

        second.Remove(theirs);
        Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => second.SaveChanges());
    }

    [TestMethod]
    public void A_Context_Changing_Its_Own_Token_Saves_And_Then_Saves_Again()
    {
        var options = TokenStore();
        using var context = new TicketContext(options);
        var ticket = context.Tickets.Single();
        for (var i = 0; i < 3; i++)
        {
            ticket.Version++;
            ticket.Notes = $"Edit {i}";
            Assert.AreEqual(1, context.SaveChanges());
        }
        Assert.AreEqual(4, ticket.Version);
    }

    [TestMethod]
    public void A_Token_Written_Through_The_Engine_After_A_Save_Is_The_One_The_Next_Save_Starts_From()
    {
        var options = TokenStore();
        using (var context = new TicketContext(options))
        {
            context.Tickets.Single().Version++;
            context.SaveChanges();

            // The engine's own API moves the token on, on the very instance the save recorded.
            var table = context.Database.GetBlazeDb().GetTable(Ticket.Table);
            var ticket = table.Get(1)!;
            var previous = new Ticket { Id = ticket.Id, Title = ticket.Title, Notes = ticket.Notes, Version = ticket.Version };
            ticket.Version = 3;
            table.UpdateInPlace(ticket, previous);
        }

        // The record of the EF save used to outlive that write, and every later save failed against it.
        using (var next = new TicketContext(options))
        {
            var ticket = next.Tickets.Single();
            Assert.AreEqual(3, ticket.Version);
            ticket.Notes = "After the engine's write";
            Assert.AreEqual(1, next.SaveChanges());
        }

        // And a detached entity still carrying the version the EF save left is stale.
        using var stale = new TicketContext(options);
        stale.Update(new Ticket { Id = 1, Title = "Ticket", Version = 2 });
        Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => stale.SaveChanges());
    }

    [TestMethod]
    public void A_Save_Rolled_Back_With_Its_Transaction_Leaves_The_Token_As_It_Was()
    {
        var options = TokenStore();
        using (var context = new TicketContext(options))
        {
            using var transaction = context.Database.BeginTransaction();
            var ticket = context.Tickets.Single();
            ticket.Version++;
            ticket.Notes = "Rolled back";
            context.SaveChanges();
            transaction.Rollback();
        }

        // The save recorded version 2 as committed although its transaction never was; what a later save is
        // checked against is version 1, the one the database still holds.
        using (var current = new TicketContext(options))
        {
            current.Update(new Ticket { Id = 1, Title = "Ticket", Notes = "Current", Version = 1 });
            Assert.AreEqual(1, current.SaveChanges());
        }
        using var check = new TicketContext(options);
        Assert.AreEqual("Current", check.Tickets.Single().Notes);
    }

    [TestMethod]
    public void A_Token_The_Engine_Wrote_On_The_Tracked_Instance_Fails_That_Contexts_Save()
    {
        var options = TokenStore();
        using var context = new TicketContext(options);
        var ticket = context.Tickets.Single();

        // The engine's own API moves the token on, on the very instance the context tracks with version 1.
        var table = context.Database.GetBlazeDb().GetTable(Ticket.Table);
        var previous = new Ticket { Id = ticket.Id, Title = ticket.Title, Notes = ticket.Notes, Version = ticket.Version };
        ticket.Version = 3;
        table.UpdateInPlace(ticket, previous);

        // The row being the entity itself used to skip the check, so the save went through over version 3.
        ticket.Notes = "Over the engine's write";
        Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => context.SaveChanges());
    }

    [TestMethod]
    public async Task A_Row_Read_From_Storage_Is_Checked_Against_What_Was_Stored_Not_What_A_Context_Changed_In_Memory()
    {
        var storage = new BlazeDbInMemoryStorage();
        var options = new DbContextOptionsBuilder<TicketContext>().UseBlazeDb(storage).Options;
        using (var seed = new TicketContext(options))
        {
            seed.Tickets.Add(new Ticket { Id = 1, Title = "Ticket", Version = 1 });
            seed.SaveChanges();
            Assert.IsTrue(await seed.Database.CloseBlazeDbAsync());
        }

        // Read back from storage, so no save of this engine has written the row.
        using var changing = new TicketContext(options);
        changing.Tickets.Single().Version = 5; // Not saved.

        // The live row's version 5 used to be taken for the committed one, and this save went through.
        using var stale = new TicketContext(options);
        stale.Update(new Ticket { Id = 1, Title = "Ticket", Notes = "Stale", Version = 5 });
        Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => stale.SaveChanges());

        await stale.Database.EnsureDeletedAsync();
    }

    [TestMethod]
    public void A_Model_With_Other_Tokens_On_The_Same_Store_Reads_The_Record_By_Name()
    {
        var name = "tokens-" + Guid.NewGuid().ToString("N");
        var titleOnly = new DbContextOptionsBuilder<TitleTokenContext>().UseBlazeDb(name).Options;
        var both = new DbContextOptionsBuilder<TicketContext>().UseBlazeDb(name).Options;

        // The title-only model binds the table first, so the records it keeps carry Title alone.
        using (var seed = new TitleTokenContext(titleOnly))
        {
            seed.Tickets.Add(new Ticket { Id = 1, Title = "Ticket", Version = 1 });
            seed.SaveChanges();
        }

        // A model checking Title and Version used to read the second value of a one-value record.
        using (var context = new TicketContext(both))
        {
            context.Tickets.Single().Notes = "Saved";
            Assert.AreEqual(1, context.SaveChanges());
        }

        // Title, which both models check, is still checked through the other model's record.
        using var first = new TitleTokenContext(titleOnly);
        using var second = new TicketContext(both);
        var theirs = second.Tickets.Single();
        first.Tickets.Single().Title = "First";
        first.SaveChanges();
        theirs.Notes = "Second";
        var error = Assert.ThrowsExactly<DbUpdateConcurrencyException>(() => second.SaveChanges());
        StringAssert.Contains(error.Message, "'Title'");
    }

    [TestMethod]
    public void A_Shadow_Concurrency_Token_Is_Refused_With_The_Model()
    {
        var options = new DbContextOptionsBuilder<ShadowTokenContext>().UseBlazeDb("shadow-" + Guid.NewGuid().ToString("N")).Options;
        using var context = new ShadowTokenContext(options);

        var error = Assert.ThrowsExactly<NotSupportedException>(() => context.Model);

        StringAssert.Contains(error.Message, "'Stamp'");
    }

    private static DbContextOptions<TicketContext> TokenStore()
    {
        var options = new DbContextOptionsBuilder<TicketContext>().UseBlazeDb("tokens-" + Guid.NewGuid().ToString("N")).Options;
        using var seed = new TicketContext(options);
        seed.Tickets.Add(new Ticket { Id = 1, Title = "Ticket", Version = 1 });
        seed.SaveChanges();
        return options;
    }

    /// <summary>
    /// Holds the first write after <see cref="HoldNextWrite"/> until <see cref="Release"/>: an in-memory storage
    /// whose writes otherwise complete at once, leaving no window to race in.
    /// </summary>
    private sealed class GatedStorage : IBlazeDbEnumerableStorage
    {
        private readonly BlazeDbInMemoryStorage _inner = new();
        private readonly TaskCompletionSource _released = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _armed;

        public Task WriteHeld => _held.Task;

        public void HoldNextWrite() => Volatile.Write(ref _armed, 1);

        public void Release() => _released.TrySetResult();

        public ValueTask<byte[]?> ReadAsync(string name, CancellationToken cancellationToken = default) =>
            _inner.ReadAsync(name, cancellationToken);

        public async ValueTask WriteAtomicAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            await HoldAsync();
            await _inner.WriteAtomicAsync(name, data, cancellationToken);
        }

        public async ValueTask AppendAsync(string name, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            await HoldAsync();
            await _inner.AppendAsync(name, data, cancellationToken);
        }

        public ValueTask DeleteAsync(string name, CancellationToken cancellationToken = default) =>
            _inner.DeleteAsync(name, cancellationToken);

        public ValueTask<IReadOnlyCollection<string>> ListAsync(CancellationToken cancellationToken = default) =>
            _inner.ListAsync(cancellationToken);

        private Task HoldAsync()
        {
            if (Interlocked.Exchange(ref _armed, 0) == 0)
            {
                return Task.CompletedTask;
            }
            _held.TrySetResult();
            return _released.Task;
        }
    }
}

[BlazeDbTable("tickets")]
public partial class Ticket
{
    [BlazeDbKey]
    public int Id { get; set; }

    [ConcurrencyCheck]
    public string Title { get; set; } = "";

    public string? Notes { get; set; }

    public int Version { get; set; }
}

public sealed class TicketContext(DbContextOptions<TicketContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Ticket>().Property(t => t.Version).IsConcurrencyToken();
}

/// <summary>The same table with only the <c>[ConcurrencyCheck]</c> token, for a model sharing the store with <see cref="TicketContext"/>.</summary>
public sealed class TitleTokenContext(DbContextOptions<TitleTokenContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();
}

public sealed class ShadowTokenContext(DbContextOptions<ShadowTokenContext> options) : DbContext(options)
{
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Ticket>().Property<long>("Stamp").IsConcurrencyToken();
}
