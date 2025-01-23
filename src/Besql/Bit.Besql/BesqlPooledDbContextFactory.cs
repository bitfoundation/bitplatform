using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bit.Besql;

public class BesqlPooledDbContextFactory<TDbContext> : PooledDbContextFactoryBase<TDbContext>
    where TDbContext : DbContext
{
    private readonly string _fileName;
    private readonly IBesqlStorage _storage;
    private readonly string _connectionString;

    public BesqlPooledDbContextFactory(
        IBesqlStorage storage,
        DbContextOptions<TDbContext> options,
        Func<IServiceProvider, TDbContext, Task> dbContextInitializer)
        : base(options, dbContextInitializer)
    {
        _connectionString = options.Extensions
                .OfType<RelationalOptionsExtension>()
                .First(r => string.IsNullOrEmpty(r.ConnectionString) is false).ConnectionString!;

        _fileName = new DbConnectionStringBuilder()
        {
            ConnectionString = _connectionString
        }["Data Source"].ToString()!.Trim('/');

        _storage = storage;
    }

    protected override async Task InitializeDbContext()
    {
        await _storage.Init(_fileName).ConfigureAwait(false);
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA synchronous = FULL;";
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);

        await base.InitializeDbContext().ConfigureAwait(false);
    }
}
