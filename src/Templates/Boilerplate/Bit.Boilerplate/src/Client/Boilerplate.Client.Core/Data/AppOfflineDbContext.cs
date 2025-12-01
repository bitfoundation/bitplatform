using Boilerplate.Shared.Dtos.Todo;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Datasync.Client.Http;
using CommunityToolkit.Datasync.Client.Offline;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Boilerplate.Client.Core.Services.HttpMessageHandlers;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Boilerplate.Client.Core.Data;

public partial class AppOfflineDbContext(DbContextOptions<AppOfflineDbContext> options) : OfflineDbContext(options)
{
    public virtual DbSet<TodoItemDto> TodoItems { get; set; }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses. Convert the values to a supported type:
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();

        base.ConfigureConventions(configurationBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                entry.CurrentValues["UpdatedAt"] = DateTimeOffset.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnDatasyncInitialization(DatasyncOfflineOptionsBuilder optionsBuilder)
    {
        var absoluteServerAddressProvider = this.GetService<AbsoluteServerAddressProvider>();
        var httpMessageHandlersChainFactory = this.GetService<HttpMessageHandlersChainFactory>();

        HttpClientOptions clientOptions = new()
        {
            Endpoint = absoluteServerAddressProvider.GetAddress(),
            HttpPipeline = [httpMessageHandlersChainFactory.Invoke()]
        };

        optionsBuilder
            .UseHttpClientOptions(clientOptions)
            .Entity<TodoItemDto>(options =>
            {
                options.Endpoint = new Uri("/api/TodoItemTable", UriKind.Relative);
            });
    }
}
