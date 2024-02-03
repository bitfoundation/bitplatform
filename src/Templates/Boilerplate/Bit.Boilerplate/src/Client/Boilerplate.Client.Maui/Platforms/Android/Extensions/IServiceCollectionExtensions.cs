namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientMauiProjectAndroidServices(this IServiceCollection services)
    {
        // Services being registered here can get injected in Maui/Android.

        return services;
    }
}
