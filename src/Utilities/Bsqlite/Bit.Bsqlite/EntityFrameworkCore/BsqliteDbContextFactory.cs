using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bsqlite;

internal sealed class BsqliteDbContextFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext> : IBsqliteDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// The cross-tab lock the first-use step runs under. Not the migrator's own lock, which
    /// <c>MigrateAsync</c> takes inside it: a Web Lock is not re-entrant.
    /// </summary>
    internal const string FirstUseLockName = "ef-first-use";

    private readonly IServiceProvider _services;
    private readonly BsqliteDbContextOptions<TContext> _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private BsqliteSession? _preparedFor;

    public BsqliteDbContextFactory(
        IServiceProvider services,
        BsqliteDatabase database,
        BsqliteDbContextOptions<TContext> options)
    {
        _services = services;
        Database = database;
        _options = options;
        database.Closing += OnClosingAsync;
    }

    public BsqliteDatabase Database { get; }

    private bool NeedsPreparation
        => _options.Schema != BsqliteSchemaStrategy.None || _options.OnFirstUse is not null;

    public TContext CreateDbContext()
    {
        var session = Database.Session;

        // A session opened some other way - BsqliteDatabase.OpenAsync, say - has not had its schema
        // migrated or its first-use step run, and a context on it would fail with "no such table" or
        // query a schema one deploy out of date.
        if (session is null || (!ReferenceEquals(_preparedFor, session) && NeedsPreparation))
        {
            throw new InvalidOperationException(
                $"The database '{Database.Name}' is not ready for {typeof(TContext).Name}. Await "
                + "CreateDbContextAsync() at least once first: opening the database and applying "
                + "migrations are asynchronous in the browser, and the synchronous overload cannot "
                + "wait for them.");
        }

        return Build(session);
    }

    public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var session = await Database.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (!ReferenceEquals(_preparedFor, session))
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!ReferenceEquals(_preparedFor, session))
                {
                    await PrepareAsync(session, cancellationToken).ConfigureAwait(false);
                    _preparedFor = session;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        return Build(session);
    }

    Task<TContext> IDbContextFactory<TContext>.CreateDbContextAsync(CancellationToken cancellationToken)
        => CreateDbContextAsync(cancellationToken);

    // EF Core marks migrations RequiresDynamicCode for NativeAOT, which builds the design-time model it cannot
    // emit code for. Blazor WebAssembly - the host this factory opens databases in - is not NativeAOT: even an
    // AOT-compiled WebAssembly app keeps the interpreter for code generated at run time, and migrations run there.
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "Runs on Blazor WebAssembly, whose AOT keeps the interpreter for dynamic code; NativeAOT is not a host of this library.")]
    private async Task PrepareAsync(BsqliteSession session, CancellationToken cancellationToken)
    {
        if (!NeedsPreparation)
        {
            return;
        }

        // Every tab prepares its own session over the same file. Without this, two tabs opening at
        // once would both seed an empty database.
        await using var exclusive = await session.Connection
            .TryAcquireLockAsync(FirstUseLockName, cancellationToken)
            .ConfigureAwait(false);

        await using var context = Build(session);

        switch (_options.Schema)
        {
            case BsqliteSchemaStrategy.Migrate:
                await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
                break;

            case BsqliteSchemaStrategy.EnsureCreated:
                await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
                break;
        }

        if (_options.OnFirstUse is { } firstUse)
        {
            await firstUse(context, cancellationToken).ConfigureAwait(false);
        }
    }

    private TContext Build(BsqliteSession session)
    {
        var builder = new DbContextOptionsBuilder<TContext>()
            .UseApplicationServiceProvider(_services)
            .UseBitBsqlite(session.Connection);

        _options.Configure?.Invoke(_services, builder);

        return ActivatorUtilities.CreateInstance<TContext>(_services, builder.Options);
    }

    private Task OnClosingAsync()
    {
        _preparedFor = null;
        return Task.CompletedTask;
    }
}
