using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bit.Besql;

public class BesqlPooledDbContextFactory<TDbContext> : PooledDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly IBesqlStorage _storage;
    private TaskCompletionSource _initTcs = new();

    public BesqlPooledDbContextFactory(
        IBesqlStorage storage,
        DbContextOptions<TDbContext> options)
        : base(options)
    {
        _storage = storage;
        _ = InitAsync();
    }

    public override async Task<TDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        await _initTcs.Task;

        var ctx = await base.CreateDbContextAsync(cancellationToken);

        return ctx;
    }

    private async Task InitAsync()
    {
        try
        {
            var fileName = "Offline-Client.db"; // TODO: Needs to be read from connection string
            await _storage.Init(fileName);
            await using var connection = new SqliteConnection($"Data Source={fileName}");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA synchronous = FULL;";
            await command.ExecuteNonQueryAsync();
            _initTcs.SetResult();
        }
        catch (Exception exp)
        {
            _initTcs.SetException(exp);
        }
    }
}
