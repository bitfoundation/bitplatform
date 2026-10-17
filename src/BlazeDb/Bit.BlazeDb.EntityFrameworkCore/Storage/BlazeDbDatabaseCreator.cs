using Bit.BlazeDb.EntityFrameworkCore.Infrastructure;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Bit.BlazeDb.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bit.BlazeDb.EntityFrameworkCore.Storage;

/// <summary>
/// Opens the engine from the context model (EnsureCreated) and drops the shared store
/// (EnsureDeleted), matching the contract other providers implement for a file or in-memory database.
/// </summary>
internal sealed class BlazeDbDatabaseCreator : IDatabaseCreator
{
    private readonly IDbContextOptions _options;
    private readonly IModel _model;
    private readonly BlazeDbEngineCache _cache;
    private readonly IBlazeDbTableCache _tables;

    public BlazeDbDatabaseCreator(
        IDbContextOptions options, IModel model, BlazeDbEngineCache cache, IBlazeDbTableCache tables)
    {
        _options = options;
        _model = model;
        _cache = cache;
        _tables = tables;
    }

    /// <summary>
    /// Opens the store if no context has yet, and reports whether this call did the opening. That is
    /// the contract callers rely on for <c>if (EnsureCreated()) Seed();</c>: true the first time a
    /// store is brought up in this process, false for every context after that.
    /// </summary>
    public bool EnsureCreated()
    {
        _cache.GetOrCreate(Extension(), _model, out var created);
        _ = _tables.Database;
        return created;
    }

    public async Task<bool> EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        var (_, created) = await _cache.GetOrCreateAsync(Extension(), _model, cancellationToken).ConfigureAwait(false);
        return created;
    }

    public bool CanConnect() => _tables.Database is not null;

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<bool>(cancellationToken)
            : Task.FromResult(CanConnect());

    public bool EnsureDeleted() => EnsureDeletedAsync().GetAwaiter().GetResult();

    public async Task<bool> EnsureDeletedAsync(CancellationToken cancellationToken = default)
    {
        var extension = Extension();
        if (!extension.OwnsEngine)
        {
            return false;
        }

        var storage = extension.Storage;
        await _cache.ReleaseAsync(extension.StoreKey).ConfigureAwait(false);
        _tables.Reset();
        if (storage is not null)
        {
            // The engine knows what it wrote; deleting only the manifest would leave a log the next
            // open replays, bringing the deleted rows straight back.
            await BlazeDbDatabase.DeleteAsync(storage, cancellationToken).ConfigureAwait(false);
        }
        return true;
    }

    private BlazeDbOptionsExtension Extension() =>
        _options.FindExtension<BlazeDbOptionsExtension>()
        ?? throw new InvalidOperationException("No BlazeDb store was configured. Call optionsBuilder.UseBlazeDb().");
}
