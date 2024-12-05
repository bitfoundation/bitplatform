using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bit.Besql;

public class BesqlPooledDbContextFactory<TDbContext> : PooledDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private readonly string _fileName;
    private readonly IBesqlStorage _storage;
    private readonly string _connectionString;
    private readonly TaskCompletionSource _initTcs = new();

    public BesqlPooledDbContextFactory(
        IBesqlStorage storage,
        DbContextOptions<TDbContext> options)
        : base(options)
    {
        _connectionString = options.Extensions
                .OfType<RelationalOptionsExtension>()
                .First(r => string.IsNullOrEmpty(r.ConnectionString) is false).ConnectionString!;

        _fileName = new DbConnectionStringBuilder()
        {
            ConnectionString = _connectionString
        }["Data Source"].ToString()!;

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
            await _storage.Init(_fileName);
            await using var connection = new SqliteConnection(_connectionString);
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
