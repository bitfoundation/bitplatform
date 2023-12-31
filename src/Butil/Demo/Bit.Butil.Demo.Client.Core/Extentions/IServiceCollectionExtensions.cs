using Bit.Butil;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientSharedServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in client side (Web, Android, iOS, Windows, macOS)
        services.AddBitButilServices();

        return services;
    }
}
