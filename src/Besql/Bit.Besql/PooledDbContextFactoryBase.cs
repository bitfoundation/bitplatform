using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Bit.Besql;

public class PooledDbContextFactoryBase<TDbContext>(DbContextOptions<TDbContext> options,
        Func<IServiceProvider, TDbContext, Task> dbContextInitializer) : PooledDbContextFactory<TDbContext>(options)
    where TDbContext : DbContext
{
    private TaskCompletionSource? dbContextInitializerTcs;

    public override async Task<TDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (dbContextInitializerTcs is null)
        {
            await StartRunningDbContextInitializer();
        }

        await dbContextInitializerTcs!.Task.ConfigureAwait(false);

        return await base.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
    }

    public override TDbContext CreateDbContext()
    {
        return CreateDbContextAsync().GetAwaiter().GetResult();
    }

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
            await using var dbContext = await base.CreateDbContextAsync().ConfigureAwait(false);
            await dbContextInitializer(dbContext.GetService<IServiceProvider>(), dbContext).ConfigureAwait(false);
        }
    }
}
