using EasyCaching.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Extensions.Caching.EasyCaching.Implementations.Distributed
{
    public class DistributedEasyCaching : EasyCachingBase, IDistributedCache
    {
        private readonly IEasyCachingProvider _easyCachingProvider;
        private readonly ConcurrentDictionary<string, TimeSpan> _expirations = new ConcurrentDictionary<string, TimeSpan>();

        public DistributedEasyCaching(IEasyCachingProviderFactory easyCachingProviderFactory, IOptions<DistributedEasyCachingOptions> options)
            : base(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
            if (easyCachingProviderFactory == null)
                throw new ArgumentNullException(nameof(easyCachingProviderFactory));

            _easyCachingProvider = easyCachingProviderFactory.GetCachingProvider(options.Value.ProviderName);
        }

        public byte[]? Get(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var result = _easyCachingProvider.Get<byte[]>(key);

            return (!result.HasValue || result.IsNull) ? null : result.Value;
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var result = await _easyCachingProvider.GetAsync<byte[]>(key).ConfigureAwait(false);

            return (!result.HasValue || result.IsNull) ? null : result.Value;
        }

        public void Refresh(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var value = Get(key);

            if (value != null && _expirations.TryGetValue(key, out TimeSpan expiration))
            {
                Set(key, value, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration });
            }
        }

        public async Task RefreshAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var value = await GetAsync(key).ConfigureAwait(false);

            if (value != null && _expirations.TryGetValue(key, out TimeSpan expiration))
            {
                await SetAsync(key, value, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration }).ConfigureAwait(false);
            }
        }

        public void Remove(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _expirations.TryRemove(key, out TimeSpan _);

            _easyCachingProvider.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _expirations.TryRemove(key, out TimeSpan _);

            return _easyCachingProvider.RemoveAsync(key);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var expiration = GetEasyCacheExpiration(options);

            _expirations.TryAdd(key, expiration);

            _easyCachingProvider.Set(key, value, expiration);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var expiration = GetEasyCacheExpiration(options);

            _expirations.TryAdd(key, expiration);

            return _easyCachingProvider.SetAsync(key, value, expiration);
        }
    }
}
