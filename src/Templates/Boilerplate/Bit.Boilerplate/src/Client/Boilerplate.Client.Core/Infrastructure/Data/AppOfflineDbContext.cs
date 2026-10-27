using Boilerplate.Shared.Features.Todo;
using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Boilerplate.Client.Core.Infrastructure.Data;

public partial class AppOfflineDbContext(DbContextOptions<AppOfflineDbContext> options) : OfflineDbContext(options)
{
    public virtual DbSet<TodoItemDto> TodoItems { get; set; }

    /// <summary>
    /// Brings the database up to date with the model. Runs once, before the first context is handed out
    /// (See IClientCoreServiceCollectionExtensions and <see cref="AppOfflineDbContextFactory"/>).
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (AppEnvironment.IsDevelopment() is false && Model.GetType() == typeof(RuntimeModel))
            throw new InvalidOperationException("AppOfflineDbContext has not been optimized. Run 'dnx dotnet-ef@10.0.14 -- dbcontext optimize --context AppOfflineDbContext' before publishing, and re-run it after every model or migration change. See Boilerplate.Client.Core/Infrastructure/Data/README.md.");

        await Database.MigrateAsync(cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type:
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();

        configurationBuilder.Properties<decimal>().HavePrecision(18, 3);
        configurationBuilder.Properties<decimal?>().HavePrecision(18, 3);

        base.ConfigureConventions(configurationBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                entry.CurrentValues["UpdatedAt"] = this.GetService<TimeProvider>().GetUtcNow();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            // Last writer wins.
            .UseDefaultConflictResolver(new ClientWinsConflictResolver())
            .UseHttpClient(this.GetService<HttpClient>())
            .Entity<TodoItemDto>(options =>
            {
                options.Endpoint = new Uri("/api/v1/TodoItemTable", UriKind.Relative);
            });
    }
}
