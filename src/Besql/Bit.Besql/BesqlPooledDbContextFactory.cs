using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bit.Besql;

public class BesqlPooledDbContextFactory<TDbContext> : PooledDbContextFactoryBase<TDbContext>
    where TDbContext : DbContext
{
    private readonly string _fileName;
    private readonly IBitBesqlStorage _storage;
    private readonly string _connectionString;

    public BesqlPooledDbContextFactory(
        IBitBesqlStorage storage,
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
        if (File.Exists(_fileName) is false)
        {
            await _storage.Load(_fileName).ConfigureAwait(false);
        }

        await base.InitializeDbContext().ConfigureAwait(false);
    }
}
