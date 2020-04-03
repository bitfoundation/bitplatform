using Bit.Extensions.Caching.EasyCaching.Implementations;
using Bit.Extensions.Caching.EasyCaching.Implementations.Distributed;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DistributedEasyCachingServiceCollectionExtensions
    {
        public static IServiceCollection AddDistributedEasyCaching(this IServiceCollection services, Action<DistributedEasyCachingOptions> setupAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            services.AddOptions();
            services.Configure(setupAction);
            services.AddSingleton<IDistributedCache, DistributedEasyCaching>();

            return services;
        }
    }

    public sealed class DistributedEasyCachingOptions : EasyCachingOptionsBase, IOptions<DistributedEasyCachingOptions>
    {
        public string ProviderName { get; set; } = "DefaultInMemory";

        DistributedEasyCachingOptions IOptions<DistributedEasyCachingOptions>.Value => this;
    }
}
