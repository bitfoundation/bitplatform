using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bit.Besql;

public class BesqlPooledDbContextFactory<TContext> : PooledDbContextFactory<TContext>
    where TContext : DbContext
{
    private static string Filename => "Offline-ClientDb.db";

    private readonly IBesqlStorage _storage;
    private TaskCompletionSource _initTcs = new();

    public BesqlPooledDbContextFactory(
        IBesqlStorage storage,
        DbContextOptions<TContext> options)
        : base(options)
    {
        _storage = storage;
        _ = InitAsync();
    }

    public override async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        await _initTcs.Task;

        var ctx = await base.CreateDbContextAsync(cancellationToken);

        return ctx;
    }

    private async Task InitAsync()
    {
        try
        {
            await _storage.Init(Filename);
            await using var connection = new SqliteConnection($"Data Source={Filename}");
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
