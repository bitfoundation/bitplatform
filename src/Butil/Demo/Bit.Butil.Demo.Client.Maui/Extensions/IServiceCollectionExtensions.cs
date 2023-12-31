namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in web (blazor web assembly & blazor server)
        services.AddClientSharedServices();

        return services;
    }
}
