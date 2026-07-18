//+:cnd:noEmit
using System.Text;
using Boilerplate.Shared.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Caching.Memory;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class ISharedServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSharedProjectServices(IConfiguration configuration)
        {
            // Services being registered here can get injected everywhere.

            services.AddScoped<HtmlRenderer>();
            services.AddScoped<CultureInfoManager>();

            services.AddSingleton(TimeProvider.System);

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
            services.AddSingleton<IMemoryCache, AppMemoryCache>(); // Extends services.AddMemoryCache()
            services.Configure<MemoryCacheOptions>(options =>
            {
                configuration.GetRequiredSection("MemoryCache").DynamicBind(options);
            });

            return services;
        }

        /// <summary>
        /// Define authorization policies here to seamlessly integrate them across various components,
        /// including web api actions and razor pages using Authorize attribute, AuthorizeView in razor pages,
        /// and programmatically in C# by injecting <see cref="IAuthorizationService"/> for enhanced security and access control.
        /// </summary>
        public void ConfigureAuthorizationCore()
        {
            StringBuilder duplicateFeaturesReportString = new();

            foreach (var g in AppFeatures.GetGlobalAdminFeatures().GroupBy(p => p.Value).Where(g => g.Count() > 1))
            {
                duplicateFeaturesReportString.Append(string.Join(Environment.NewLine, g.Select(p => $"{p.Group.Name}-{p.Name}-{p.Value}")));
            }

            if (duplicateFeaturesReportString.Length > 0)
                throw new Exception($"Duplicate feature values found. Please ensure all feature values are unique{duplicateFeaturesReportString}");

            services.AddSingleton<IAuthorizationHandler, FeatureRequirementHandler>();

            services.AddAuthorizationCore(options =>
            {
                options.AddPolicy(AuthPolicies.PRIVILEGED_ACCESS, x => x.RequireClaim(AppClaimTypes.PRIVILEGED_SESSION, "true"));
                options.AddPolicy(AuthPolicies.ELEVATED_ACCESS, x => x.RequireAssertion(ctx => ctx.User.GetElevatedSessionExpiresOn() > TimeProvider.GetUtcNow()));
                //#if (multitenant == true)
                options.AddPolicy(AuthPolicies.TENANT_SELECTED, x => x.RequireAssertion(ctx => ctx.User.GetTenantId() is not null));
                //#endif

                foreach (var feat in AppFeatures.GetGlobalAdminFeatures())
                {
                    options.AddPolicy(feat.Value, policy => policy.AddRequirements(new AppFeatureRequirement(FeatureName: $"{feat.Group.Name}.{feat.Name}", FeatureValue: feat.Value)));
                }
            });
        }
    }
}
