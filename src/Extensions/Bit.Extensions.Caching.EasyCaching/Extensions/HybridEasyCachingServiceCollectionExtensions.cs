using Bit.Extensions.Caching.EasyCaching.Implementations;
using Bit.Extensions.Caching.EasyCaching.Implementations.Hybrid;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HybridEasyCachingServiceCollectionExtensions
    {
        public static IServiceCollection AddHybridEasyCaching(this IServiceCollection services, Action<HybridEasyCachingOptions> setupAction)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (setupAction == null)
                throw new ArgumentNullException(nameof(setupAction));

            services.AddOptions();
            services.Configure(setupAction);
            services.AddSingleton<IDistributedCache, HybridEasyCaching>();

            return services;
        }
    }

    public sealed class HybridEasyCachingOptions : EasyCachingOptionsBase, IOptions<HybridEasyCachingOptions>
    {
        HybridEasyCachingOptions IOptions<HybridEasyCachingOptions>.Value => this;
    }
}
