namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSharedProjectServices(this IServiceCollection services)
    {
        // Services being registered here can get injected everywhere (Api, Web, Android, iOS, Windows and macOS)

        services.TryAddTransient<IDateTimeProvider, DateTimeProvider>();
        services.TryAddTransient<CultureInfoManager>();

        // Define authorization policies here to seamlessly integrate them across various components,
        // including web api actions and razor pages using Authorize attribute, AuthorizeView in razor pages,
        // and programmatically in C# by injecting IAuthorizationService for enhanced security and access control.
        services.AddAuthorizationCore(options => options.AddPolicy("AdminsOnly", authPolicyBuilder => authPolicyBuilder.RequireRole("Admin")));

        services.AddLocalization();

        return services;
    }
}
