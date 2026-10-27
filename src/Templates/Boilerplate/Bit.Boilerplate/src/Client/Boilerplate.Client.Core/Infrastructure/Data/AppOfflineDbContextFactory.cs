namespace Boilerplate.Client.Core.Infrastructure.Data;

/// <summary>
/// Creates <see cref="AppOfflineDbContext"/> where the offline database is a plain SQLite file on the server - Blazor Server,
/// pre-rendering and the design-time tools - bringing the database up to date before handing out the first context.
/// In the browser and in Blazor Hybrid, Bsqlite's factory does the same (See IClientCoreServiceCollectionExtensions).
/// </summary>
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
