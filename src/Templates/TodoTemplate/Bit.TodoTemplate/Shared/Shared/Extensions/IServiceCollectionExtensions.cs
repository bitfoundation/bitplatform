using TodoTemplate.Shared.Services.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddLocalization();

        services.AddAuthorizationCore();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    }
}
