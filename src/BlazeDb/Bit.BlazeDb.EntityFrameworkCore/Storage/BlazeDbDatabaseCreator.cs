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

    /// <summary>
    /// Deletes synchronously where that is possible. Closing a persistent store stops its flusher,
    /// flushes and deletes files, all of it asynchronous I/O; where the thread cannot wait for that
    /// (see <see cref="BlazeDbEngineCache.CanBlock"/>) the call is refused before anything is
    /// touched, rather than failing part-way with the engine already released.
    /// </summary>
    public bool EnsureDeleted()
    {
        if (!BlazeDbEngineCache.CanBlock && Extension() is { OwnsEngine: true, Storage: not null })
        {
            throw new InvalidOperationException(
                "Deleting a persistent BlazeDb store needs asynchronous I/O, which this thread cannot " +
                "wait for. Call await context.Database.EnsureDeletedAsync() instead.");
        }

        var deleting = EnsureDeletedAsync();
        if (!deleting.IsCompleted && !BlazeDbEngineCache.CanBlock)
        {
            // An in-memory store closes synchronously unless an open is in flight on it; this
            // delete then completes on its own once that open has finished.
            throw new InvalidOperationException(
                "This BlazeDb store is still being opened, so it cannot be deleted synchronously on this " +
                "thread. Call await context.Database.EnsureDeletedAsync() instead.");
        }
        return deleting.GetAwaiter().GetResult();
    }

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
