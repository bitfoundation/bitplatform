namespace Boilerplate.Client.Core.Infrastructure.Data;

public partial class AppOfflineDbContextFactory(DbContextOptions<AppOfflineDbContext> options) : IDbContextFactory<AppOfflineDbContext>
{
    private readonly SemaphoreSlim initializationLock = new(1, 1);
    private bool isInitialized;

    public AppOfflineDbContext CreateDbContext() => new(options);

    public async Task<AppOfflineDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        if (isInitialized is false)
        {
            await initializationLock.WaitAsync(cancellationToken);
            try
            {
                if (isInitialized is false)
                {
                    await using var dbContext = new AppOfflineDbContext(options);
                    await dbContext.InitializeAsync(cancellationToken);
                    isInitialized = true;
                }
            }
            finally
            {
                initializationLock.Release();
            }
        }

        return new(options);
    }
}
