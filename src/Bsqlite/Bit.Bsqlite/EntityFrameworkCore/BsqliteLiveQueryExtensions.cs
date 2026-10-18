using System.Runtime.CompilerServices;
using Bit.Bsqlite.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore;

/// <summary>EF entry point for table-level live queries.</summary>
public static class BsqliteLiveQueryExtensions
{
    /// <summary>
    /// Re-executes <paramref name="query"/> whenever a table it reads is written.
    /// </summary>
    public static IBsqliteLiveQuery<IReadOnlyList<T>> AsLiveQuery<T>(this IQueryable<T> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return AsLiveQuery(query, ConnectionOf(query));
    }

    /// <summary>
    /// Re-executes <paramref name="query"/> whenever a table it reads is written.
    /// </summary>
    public static IBsqliteLiveQuery<IReadOnlyList<T>> AsLiveQuery<T>(
        this IQueryable<T> query,
        BsqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(connection);

        var tables = BsqliteTableNames.Extract(query.ToQueryString());

        // The write notification arrives while SaveChanges is still consuming its result. Refreshing
        // then, on a host where the refresh runs on another thread, races the change tracker; the
        // gate defers the re-read until the save has finished. Null only for a query whose provider
        // hides its context, in which case the refresh runs ungated as before.
        var context = DbContextOf(query);
        var gate = context is null ? null : new BsqliteSaveChangesGate(context);

        return new BsqliteLiveQuery<IReadOnlyList<T>>(
            connection,
            async ct =>
            {
                if (gate is not null)
                {
                    await gate.WaitForIdleAsync(ct).ConfigureAwait(false);
                }

                return await query.ToListAsync(ct).ConfigureAwait(false);
            },
            tables,
            onDispose: gate is null ? null : gate.Dispose);
    }

    /// <summary>
    /// <see cref="DbSet{TEntity}"/> implements <c>IInfrastructure&lt;IServiceProvider&gt;</c>.
    /// <c>Where</c>/<c>OrderBy</c> replace it with <c>EntityQueryable</c>, whose provider holds
    /// the compiler that still knows the context.
    /// </summary>
    private static BsqliteConnection ConnectionOf<T>(IQueryable<T> query)
    {
        var context = DbContextOf(query)
            ?? throw new InvalidOperationException(
                "AsLiveQuery() needs an EF query whose provider exposes the current DbContext.");

        if (context.Database.GetDbConnection() is BsqliteConnection connection)
        {
            return connection;
        }

        throw new InvalidOperationException(
            "AsLiveQuery() requires the context to be using a BsqliteConnection.");
    }

    private static DbContext? DbContextOf(IQueryable query)
        => ContextFromInfrastructure(query as IInfrastructure<IServiceProvider>)
            ?? ContextFromInfrastructure(query.Provider as IInfrastructure<IServiceProvider>)
            ?? ContextFromQueryProvider(query.Provider);

    private static DbContext? ContextFromInfrastructure(IInfrastructure<IServiceProvider>? accessor)
        => accessor?.GetService<ICurrentDbContext>()?.Context;

    // The last resort: reach through EF's query provider for the context. EF1001 is suppressed here
    // and nowhere else, so the rest of the assembly still fails the build if it touches an internal.
#pragma warning disable EF1001
    private static DbContext? ContextFromQueryProvider(IQueryProvider provider)
    {
        if (provider is not EntityQueryProvider entityProvider)
        {
            return null;
        }

        var compiler = GetQueryCompiler(entityProvider);
        if (compiler is QueryCompiler queryCompiler)
        {
            return GetQueryContextFactory(queryCompiler).Create().Context;
        }

        return null;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_queryCompiler")]
    private static extern ref IQueryCompiler GetQueryCompiler(EntityQueryProvider provider);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_queryContextFactory")]
    private static extern ref IQueryContextFactory GetQueryContextFactory(QueryCompiler compiler);
#pragma warning restore EF1001
}
