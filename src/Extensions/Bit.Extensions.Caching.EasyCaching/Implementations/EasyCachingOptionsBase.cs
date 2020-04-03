using System;

namespace Bit.Extensions.Caching.EasyCaching.Implementations
{
    public abstract class EasyCachingOptionsBase
    {
        public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);
    }
}
