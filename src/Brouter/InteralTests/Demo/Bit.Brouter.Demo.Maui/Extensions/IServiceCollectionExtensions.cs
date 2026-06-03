namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddMauiServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Services registered in this class can be injected in Android, iOS, Windows, and macOS.
        services.AddCoreServices();

        return services;
    }
}
