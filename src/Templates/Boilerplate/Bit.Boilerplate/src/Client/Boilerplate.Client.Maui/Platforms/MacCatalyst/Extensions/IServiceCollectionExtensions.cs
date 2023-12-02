namespace Microsoft.Extensions.DependencyInjection;

public static class IMacServiceCollectionExtensions
{
    public static IServiceCollection AddClientMacServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in macOS.

        return services;
    }
}
