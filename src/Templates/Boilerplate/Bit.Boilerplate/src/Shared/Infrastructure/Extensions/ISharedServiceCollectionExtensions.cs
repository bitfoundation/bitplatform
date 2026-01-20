using System.Text;
using Boilerplate.Shared.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ISharedServiceCollectionExtensions
{
    public static IServiceCollection AddSharedProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Services being registered here can get injected everywhere.

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

        services.AddOptions<SharedSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.ConfigureAuthorizationCore();

        services.AddLocalization();

        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Define authorization policies here to seamlessly integrate them across various components,
    /// including web api actions and razor pages using Authorize attribute, AuthorizeView in razor pages,
    /// and programmatically in C# by injecting <see cref="IAuthorizationService"/> for enhanced security and access control.
    /// </summary>
    public static void ConfigureAuthorizationCore(this IServiceCollection services)
    {
        StringBuilder duplicateFeaturesReportString = new();

        foreach (var g in AppFeatures.GetAll().GroupBy(p => p.Value).Where(g => g.Count() > 1))
        {
            duplicateFeaturesReportString.Append(string.Join(Environment.NewLine, g.Select(p => $"{p.Group.Name}-{p.Name}-{p.Value}")));
        }

        if (duplicateFeaturesReportString.Length > 0)
            throw new Exception($"Duplicate feature values found. Please ensure all feature values are unique{duplicateFeaturesReportString}");

        services.AddSingleton<IAuthorizationHandler, FeatureRequirementHandler>();

        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(AuthPolicies.PRIVILEGED_ACCESS, x => x.RequireClaim(AppClaimTypes.PRIVILEGED_SESSION, "true"));
            options.AddPolicy(AuthPolicies.ELEVATED_ACCESS, x => x.RequireClaim(AppClaimTypes.ELEVATED_SESSION, "true"));

            foreach (var feat in AppFeatures.GetAll())
            {
                options.AddPolicy(feat.Value, policy => policy.AddRequirements(new AppFeatureRequirement(FeatureName: $"{feat.Group.Name}.{feat.Name}", FeatureValue: feat.Value)));
            }
        });
    }
}
