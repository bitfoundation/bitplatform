namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddClientWindowsServices(this IServiceCollection services)
    {
        // Services registered in this class can be injected in Windows.

        return services;
    }
}
