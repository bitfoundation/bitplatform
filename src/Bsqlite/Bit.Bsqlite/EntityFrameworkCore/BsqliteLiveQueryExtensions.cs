using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Bit.Bsqlite.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.EntityFrameworkCore;

/// <summary>EF entry point for table-level live queries.</summary>
/// <remarks>
/// <para>
/// Every run of the query - the first <c>RefreshAsync</c> and each refresh a write sets off - happens
/// in a context of its own, built like the one the query came from and disposed once the rows are
/// read. Two things depend on that. A refresh arrives whenever another tab writes, so on the
/// application's context it would collide with whatever the application starts there next - EF
/// allows one operation per context at a time. And a tracking query re-run on a context that already
/// tracks the rows hands back the instances it holds without their new values, so a live view would
/// pick up inserts and deletes but never an update.
/// </para>
/// <para>
/// The rows a live query returns are therefore current, and are not tracked by the application's
/// context. To change one, load it - or attach it - in the context that saves it.
/// </para>
/// </remarks>
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

        // The write notification arrives while SaveChanges is still consuming its result, inside the
        // transaction it wrote in. The gate defers the re-read until the save has finished, so it
        // never reads rows that may yet roll back. Null only for a query whose provider hides its
        // context, in which case the query also runs as given - there is no context to copy.
        var context = DbContextOf(query);
        var gate = context is null ? null : new BsqliteSaveChangesGate(context);
        var contexts = context is null ? null : new RefreshContexts(context);

        return new BsqliteLiveQuery<IReadOnlyList<T>>(
            connection,
            async ct =>
            {
                if (gate is not null)
                {
                    await gate.WaitForIdleAsync(ct).ConfigureAwait(false);
                }

                await using var own = contexts?.Create();
                if (own is null)
                {
                    return await query.ToListAsync(ct).ConfigureAwait(false);
                }

                // Query roots name an entity type, not a context, so the same expression runs on the
                // fresh context's provider - which is how a compiled query runs on any context.
                return await own.GetService<IAsyncQueryProvider>()
                    .CreateQuery<T>(query.Expression)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
            },
            tables,
            onDispose: gate is null ? null : gate.Dispose);
    }

    /// <summary>
    /// Builds contexts of the same type and options as the one a live query came from, for its
    /// runs to happen in. See the class remarks.
    /// </summary>
    /// <remarks>
    /// Everything is captured up front, so a refresh never touches the application's context, which
    /// may be disposed by then. A context type that cannot be constructed from its options - one
    /// configured only in <c>OnConfiguring</c>, with no constructor taking them - is tried without
    /// them; failing that, the query runs on the application's context as given.
    /// </remarks>
    private sealed class RefreshContexts(DbContext context)
    {
        private readonly Type _contextType = context.GetType();
        private readonly IDbContextOptions _options = context.GetService<IDbContextOptions>();
        private readonly IServiceProvider _services =
            context.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider
            ?? NoServices.Instance;

        private Func<DbContext>? _create;
        private bool _unavailable;

        public DbContext? Create()
        {
            if (_create is not null)
            {
                return _create();
            }

            if (_unavailable)
            {
                return null;
            }

            foreach (var candidate in new Func<DbContext>[] { WithOptions, WithoutOptions })
            {
                try
                {
                    var created = candidate();
                    _create = candidate;
                    return created;
                }
                catch (InvalidOperationException)
                {
                    // No constructor this way round; try the next.
                }
            }

            _unavailable = true;
            return null;
        }

        // The context type is the application's, and its constructors survive trimming because the
        // application builds it through them - directly, or through a factory or AddDbContext, all
        // of which demand public constructors be kept.
        [UnconditionalSuppressMessage("Trimming", "IL2077",
            Justification = "The context type's public constructors are kept by whatever built the context this one copies.")]
        private DbContext WithOptions()
            => (DbContext)ActivatorUtilities.CreateInstance(_services, _contextType, _options);

        [UnconditionalSuppressMessage("Trimming", "IL2077",
            Justification = "The context type's public constructors are kept by whatever built the context this one copies.")]
        private DbContext WithoutOptions()
            => (DbContext)ActivatorUtilities.CreateInstance(_services, _contextType);
    }

    private sealed class NoServices : IServiceProvider
    {
        public static readonly NoServices Instance = new();

        public object? GetService(Type serviceType) => null;
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
