namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectMacCatalystServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Maui/macOS.

        return services;
    }
}
