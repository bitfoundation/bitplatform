namespace Microsoft.Extensions.DependencyInjection;

public static class IMacServiceCollectionExtensions
{
    public static IServiceCollection AddClientMacServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Mac.

        return services;
    }
}
