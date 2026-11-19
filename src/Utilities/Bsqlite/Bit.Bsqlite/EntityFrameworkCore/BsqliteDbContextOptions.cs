using Microsoft.EntityFrameworkCore;

namespace Bit.Bsqlite;

/// <summary>Options for <see cref="IBsqliteDbContextFactory{TContext}"/>.</summary>
public sealed class BsqliteDbContextOptions<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// The registered database (<c>AddDatabase</c>) the contexts are bound to. Required when more
    /// than one is registered; with exactly one, that one is used.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>What to do to the schema the first time a context is created per session.</summary>
    public BsqliteSchemaStrategy Schema { get; set; } = BsqliteSchemaStrategy.Migrate;

    /// <summary>Further configuration of the options every context is built with.</summary>
    public Action<IServiceProvider, DbContextOptionsBuilder<TContext>>? Configure { get; set; }

    /// <summary>
    /// Runs after the schema step, once per session, with a context of its own: the place to seed
    /// demo data or run integrity checks.
    /// </summary>
    public Func<TContext, CancellationToken, Task>? OnFirstUse { get; set; }
}
