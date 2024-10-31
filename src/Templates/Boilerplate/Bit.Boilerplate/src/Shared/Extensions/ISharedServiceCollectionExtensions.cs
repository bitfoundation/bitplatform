using Boilerplate.Shared;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ISharedServiceCollectionExtensions
{
    public static IServiceCollection AddSharedProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected everywhere (Api, Web, Android, iOS, Windows and macOS)

        services.AddScoped<HtmlRenderer>();
        services.AddScoped<CultureInfoManager>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        services.AddSingleton(sp => configuration.Get<SharedSettings>()!);
        services.AddSingleton(sp =>
        {
            var options = new JsonSerializerOptions(AppJsonContext.Default.Options)
            {
                TypeInfoResolver = new CompositeJsonTypeInfoResolver(AppJsonContext.Default, IdentityJsonContext.Default)
            };

            return options;
        });

        services.AddOptions<SharedSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Define authorization policies here to seamlessly integrate them across various components,
        // including web api actions and razor pages using Authorize attribute, AuthorizeView in razor pages,
        // and programmatically in C# by injecting IAuthorizationService for enhanced security and access control.
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy("AdminsOnly", authPolicyBuilder => authPolicyBuilder.RequireRole("Admin"));
            options.AddPolicy("TwoFactorEnabled", x => x.RequireClaim("amr", "mfa")); // For those who have two-factor authentication enabled.
        });

        services.AddLocalization();

        return services;
    }
}
