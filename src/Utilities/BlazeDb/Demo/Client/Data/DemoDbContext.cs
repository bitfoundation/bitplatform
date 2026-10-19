using Microsoft.EntityFrameworkCore;

namespace Bit.BlazeDb.Demo.Client.Data;

/// <summary>
/// The demo's EF Core surface over the same tables every other page uses. Configuration is
/// ordinary <c>UseBlazeDb</c>; the entity types are the <c>[BlazeDbTable]</c> classes the generator
/// already emitted descriptors for.
/// </summary>
public sealed class DemoDbContext(DbContextOptions<DemoDbContext> options) : DbContext(options)
{
    public DbSet<TodoEntry> Todos => Set<TodoEntry>();

    public DbSet<Setting> Settings => Set<Setting>();
}
