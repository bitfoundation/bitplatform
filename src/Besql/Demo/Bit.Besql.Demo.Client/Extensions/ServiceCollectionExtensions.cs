using Bit.Besql.Demo.Client.Data;
using Microsoft.EntityFrameworkCore;
using Bit.Besql.Demo.Client.Data.CompiledModel;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddBesqlDbContextFactory<OfflineDbContext>((sp, optionsBuilder) =>
        {
            optionsBuilder
                .UseModel(OfflineDbContextModel.Instance) // use generated compiled model in order to make db context optimized
                .UseSqlite($"Data Source=Offline-Client.db");
        }, dbContextInitializer: async (sp, dbContext) => await dbContext.Database.MigrateAsync());

        return services;
    }
}
