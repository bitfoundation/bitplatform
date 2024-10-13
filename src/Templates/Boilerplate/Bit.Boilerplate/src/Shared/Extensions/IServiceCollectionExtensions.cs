using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class IServiceCollectionExtensions
{
    public static IServiceCollection AddSharedProjectServices(this IServiceCollection services)
    {
        // Services being registered here can get injected everywhere (Api, Web, Android, iOS, Windows and macOS)

        services.AddTransient<IDateTimeProvider, DateTimeProvider>();
        services.AddTransient<CultureInfoManager>();

        // Define authorization policies here to seamlessly integrate them across various components,
        // including web api actions and razor pages using Authorize attribute, AuthorizeView in razor pages,
        // and programmatically in C# by injecting IAuthorizationService for enhanced security and access control.
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy("AdminsOnly", authPolicyBuilder => authPolicyBuilder.RequireRole("Admin"));
            options.AddPolicy("TwoFactorEnabled", x => x.RequireClaim("amr", "mfa")); // For those who have two-factor authentication enabled.
        });

        services.AddLocalization();

        services.AddTransient(typeof(Lazy<>), typeof(Lazy<>)); // add support for lazy injection
        services.AddTransient<HtmlRenderer>();
        services.AddTransient(sp => AppJsonContext.Default.Options);

        return services;
    }
}
