using BlazorWeb.Shared.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        // Services being registered here can get injected everywhere (Api, Web)

        services.AddTransient<IDateTimeProvider, DateTimeProvider>();

        // Define authorization policies here to seamlessly integrate them across various components,
        // including web api actions and razor pages using Authorize attribute, AuthorizeView in razor pages, and programmatically in C# for enhanced security and access control.
        services.AddAuthorizationCore(options => options.AddPolicy("AdminOnly", authPolicyBuilder => authPolicyBuilder.RequireRole("Admin")));

        services.AddLocalization();

        return services;
    }
}
