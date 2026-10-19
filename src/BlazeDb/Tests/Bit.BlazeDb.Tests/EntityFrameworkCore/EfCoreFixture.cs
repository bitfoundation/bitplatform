using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Tests.EntityFrameworkCore;

/// <summary>
/// A private in-memory store and contexts over it. Tables come from <see cref="PeopleContext"/>'s
/// model; nothing is registered with <c>AddTable</c>.
/// </summary>
internal sealed class TestDatabase : IDisposable, IAsyncDisposable
{
    private TestDatabase(DbContextOptions<PeopleContext> options, BlazeDbDatabase database)
    {
        Options = options;
        Database = database;
    }

    public DbContextOptions<PeopleContext> Options { get; }

    public BlazeDbDatabase Database { get; }

    public static TestDatabase Open()
    {
        var options = new DbContextOptionsBuilder<PeopleContext>()
            .UseBlazeDb("test-" + Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new PeopleContext(options);
        return new TestDatabase(options, context.Database.GetBlazeDb());
    }

    public PeopleContext CreateContext() => new(Options);

    public BlazeDbTable<int, Person> People => Database.GetTable(Person.Table);

    public BlazeDbTable<int, Order> Orders => Database.GetTable(Order.Table);

    public BlazeDbTable<string, Tag> Tags => Database.GetTable(Tag.Table);

    public void Dispose()
    {
        using var context = CreateContext();
        context.Database.EnsureDeleted();
    }

    public async ValueTask DisposeAsync()
    {
        using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}
