namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in web (blazor web assembly & blazor server)
        services.AddCoreServices();

        return services;
    }
}
