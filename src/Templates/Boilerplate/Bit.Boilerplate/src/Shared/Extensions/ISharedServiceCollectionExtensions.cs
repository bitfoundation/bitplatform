using Boilerplate.Shared;
using Microsoft.AspNetCore.Authorization;
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

        services.AddSingleton(sp =>
        {
            SharedSettings settings = new();
            configuration.Bind(settings);
            return settings;
        });
        services.TryAddSingleton(sp =>
        {
            JsonSerializerOptions options = new JsonSerializerOptions(AppJsonContext.Default.Options);

            options.TypeInfoResolverChain.Add(IdentityJsonContext.Default);

            return options;
        });
        // Creates async service scope from the `root` service scope.
        services.AddSingleton(sp => new RootServiceScopeProvider(() => sp.CreateAsyncScope()));

        services.AddOptions<SharedSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.ConfigureAuthorizationCore();

        services.AddLocalization();

        return services;
    }

    /// <summary>
    /// Define authorization policies here to seamlessly integrate them across various components,
    /// including web api actions and razor pages using Authorize attribute, AuthorizeView in razor pages,
    /// and programmatically in C# by injecting <see cref="IAuthorizationService"/> for enhanced security and access control.
    /// </summary>
    public static void ConfigureAuthorizationCore(this IServiceCollection services)
    {
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(AuthPolicies.TFA_ENABLED, x => x.RequireClaim("amr", "mfa"));
            options.AddPolicy(AuthPolicies.PRIVILEGED_ACCESS, x => x.RequireClaim(AppClaimTypes.PRIVILEGED_SESSION, "true"));
            options.AddPolicy(AuthPolicies.ELEVATED_ACCESS, x => x.RequireClaim(AppClaimTypes.ELEVATED_SESSION, "true"));
        });
    }
}
