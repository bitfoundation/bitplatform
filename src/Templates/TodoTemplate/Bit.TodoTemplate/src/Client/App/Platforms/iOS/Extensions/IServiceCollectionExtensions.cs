namespace Microsoft.Extensions.DependencyInjection;

public static class IiOSServiceCollectionExtensions
{
    public static IServiceCollection AddClientiOSServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in iOS.

        return services;
    }
}
