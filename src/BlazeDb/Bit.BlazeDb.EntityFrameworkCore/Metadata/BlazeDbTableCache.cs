using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Bit.BlazeDb.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb.EntityFrameworkCore.Metadata;

internal sealed class BlazeDbTableCache : IBlazeDbTableCache, IDisposable
{
    // Bindings are reflection-built and immutable, and a store outlives any one context, so
    // they are cached against the engine rather than rebuilt for every context instance.
    private static readonly ConditionalWeakTable<BlazeDbDatabase, ConcurrentDictionary<IEntityType, IBlazeDbTableBinding>>
        Bindings = new();

    private readonly IDbContextOptions _options;
    private readonly IModel _model;
    private readonly BlazeDbEngineCache _cache;
    private readonly ICurrentDbContext _currentContext;
    private BlazeDbDatabase? _database;
    private int _version;
    private ConcurrentDictionary<IEntityType, IBlazeDbTableBinding>? _bindings;

    public BlazeDbTableCache(IDbContextOptions options, IModel model, BlazeDbEngineCache cache, ICurrentDbContext currentContext)
    {
        _options = options;
        _model = model;
        _cache = cache;
        _currentContext = currentContext;
    }

    public BlazeDbDatabase Database
    {
        get
        {
            // The engine is looked up again once any store has been released: another context's
            // EnsureDeleted disposes the engine this one cached, and queries against a disposed
            // engine fail in ways that say nothing about why. A changed version costs a dictionary
            // lookup; an unchanged one costs a compare.
            if (_database is not null && _version == _cache.Version)
            {
                return _database;
            }

            var extension = Extension();
            _version = _cache.Version;
            _database = _cache.GetOrCreate(extension, _model, out _);
            _bindings = Bindings.GetOrCreateValue(_database);
            // So indexed queries from other contexts on this engine can see this one's pending
            // modifications; see BlazeDbContextRegistry.
            BlazeDbContextRegistry.For(_database).Register(_currentContext.Context);
            return _database;
        }
    }

    public bool OtherContextsHaveModified(Type clrType) =>
        _database is { } database && BlazeDbContextRegistry.For(database).OthersHaveModified(clrType, _currentContext.Context);

    public IBlazeDbTableBinding GetBinding(IEntityType entityType)
    {
        // Through the property, so the bindings are the current engine's and not those of one
        // another context has since released.
        var database = Database;
        return _bindings!.GetOrAdd(
            entityType,
            static (type, database) => BlazeDbTableResolver.CreateBinding(database, type),
            database);
    }

    public void Reset()
    {
        if (_database is { } database)
        {
            BlazeDbContextRegistry.For(database).Unregister(_currentContext.Context);
        }
        _database = null;
        _bindings = null;
    }

    /// <summary>The context's service scope is disposed with the context; leave the registry then.</summary>
    public void Dispose() => Reset();

    private BlazeDbOptionsExtension Extension() =>
        _options.FindExtension<BlazeDbOptionsExtension>()
        ?? throw new InvalidOperationException(
            "No BlazeDb store was configured. Call optionsBuilder.UseBlazeDb().");
}
