using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Bit.Besql;

public class BesqlPooledDbContextFactory<TDbContext> : PooledDbContextFactoryBase<TDbContext>
    where TDbContext : DbContext
{
    private readonly string _fileName;
    private readonly string _connectionString;
    private readonly IBitBesqlStorage _storage;
    private readonly LazyAssemblyLoader _lazyAssemblyLoader;

    public BesqlPooledDbContextFactory(
        IBitBesqlStorage storage,
        DbContextOptions<TDbContext> options,
        Func<IServiceProvider, TDbContext, Task> dbContextInitializer,
        LazyAssemblyLoader lazyAssemblyLoader)
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

        _lazyAssemblyLoader = lazyAssemblyLoader;
    }

    protected override async Task InitializeDbContext()
    {
        try
        {
            await _lazyAssemblyLoader.LoadAssembliesAsync(["System.Private.Xml.wasm"]);
        }
        catch { }

        if (File.Exists(_fileName) is false)
        {
            await _storage.Load(_fileName).ConfigureAwait(false);
        }

        await base.InitializeDbContext().ConfigureAwait(false);
    }

    public override TDbContext CreateDbContext()
    {
        throw new NotSupportedException($"{nameof(CreateDbContext)} is not supported in bit Besql, use {nameof(CreateDbContextAsync)} instead.");
    }
}
