using Microsoft.Extensions.Caching.Distributed;
using System;

namespace Bit.Extensions.Caching.EasyCaching.Implementations
{
    public class EasyCachingBase
    {
        private readonly EasyCachingOptionsBase _options;

        public EasyCachingBase(EasyCachingOptionsBase options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            _options = options;
        }

        protected virtual TimeSpan GetEasyCacheExpiration(DistributedCacheEntryOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.AbsoluteExpiration.HasValue ? (DateTimeOffset.UtcNow - options.AbsoluteExpiration.Value) :
                (options.AbsoluteExpirationRelativeToNow ?? options.AbsoluteExpirationRelativeToNow ?? _options.DefaultExpiration);
        }
    }
}
