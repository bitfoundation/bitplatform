using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Bit.Besql;

public class BesqlDbContextFactory<TDbContext> : DbContextFactoryBase<TDbContext>
    where TDbContext : DbContext
{
    private readonly string _fileName;
    private readonly string _connectionString;
    private readonly IBitBesqlStorage _storage;
    private readonly IServiceProvider _serviceProvider;

    public BesqlDbContextFactory(
        IServiceProvider serviceProvider,
        DbContextOptions<TDbContext> options,
        IDbContextFactorySource<TDbContext> factorySource,
        Func<IServiceProvider, TDbContext, Task> dbContextInitializer,
        IBitBesqlStorage storage) : base(serviceProvider, options, factorySource, dbContextInitializer)
    {
        _connectionString = options.Extensions
                .OfType<RelationalOptionsExtension>()
                .First(r => string.IsNullOrEmpty(r.ConnectionString) is false).ConnectionString!;

        _fileName = new DbConnectionStringBuilder()
        {
            ConnectionString = _connectionString
        }["Data Source"].ToString()!.Trim('/');

        _storage = storage;

        _serviceProvider = serviceProvider;
    }

    [RequiresUnreferencedCode("Types and members the loaded assemblies depend on might be removed")]
    protected override async Task InitializeDbContext()
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<LazyAssemblyLoader>().LoadAssembliesAsync(["System.Private.Xml.wasm"]);
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
