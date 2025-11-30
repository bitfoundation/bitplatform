using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bit.Besql;

public class DbContextFactoryBase<TDbContext>(IServiceProvider serviceProvider,
        DbContextOptions<TDbContext> options,
        IDbContextFactorySource<TDbContext> factorySource,
        Func<IServiceProvider, TDbContext, Task> dbContextInitializer) : IDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    private TaskCompletionSource? dbContextInitializerTcs;

    [RequiresUnreferencedCode("Calls StartRunningDbContextInitializer()")]
    public virtual async Task<TDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (dbContextInitializerTcs is null)
        {
            await StartRunningDbContextInitializer();
        }

        await dbContextInitializerTcs!.Task.ConfigureAwait(false);

        var dbContext = factorySource.Factory(serviceProvider, options);

        return dbContext;
    }

    public virtual TDbContext CreateDbContext()
    {
        return factorySource.Factory(serviceProvider, options);
    }

    [RequiresUnreferencedCode("Calls Bit.Besql.PooledDbContextFactoryBase<TDbContext>.InitializeDbContext()")]
    private async Task StartRunningDbContextInitializer()
    {
        if (dbContextInitializerTcs is not null)
            return;

        dbContextInitializerTcs = new();

        try
        {
            await InitializeDbContext().ConfigureAwait(false);
            dbContextInitializerTcs.SetResult();
        }
        catch (Exception ex)
        {
            dbContextInitializerTcs.SetException(ex);
        }
    }

    [RequiresUnreferencedCode("Types and members the loaded assemblies depend on might be removed")]
    protected virtual async Task InitializeDbContext()
    {
        if (dbContextInitializer is not null)
        {
            await using var dbContext = factorySource.Factory(serviceProvider, options);
            await dbContextInitializer(dbContext.GetService<IServiceProvider>(), dbContext).ConfigureAwait(false);
        }
    }
}
