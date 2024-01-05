using Bit.Besql.Sample.Client.Data;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddBesqlDbContextFactory<OfflineDbContext>();

        return services;
    }
}
