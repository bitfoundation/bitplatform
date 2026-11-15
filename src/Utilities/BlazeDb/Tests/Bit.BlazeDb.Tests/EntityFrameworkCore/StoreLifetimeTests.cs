using System.ComponentModel.DataAnnotations;
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
