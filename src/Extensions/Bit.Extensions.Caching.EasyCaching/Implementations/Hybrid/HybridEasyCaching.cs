using EasyCaching.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Extensions.Caching.EasyCaching.Implementations.Hybrid
{
    public class HybridEasyCaching : EasyCachingBase, IDistributedCache
    {
        private readonly IHybridCachingProvider _hybridCachingProvider;
        private readonly ConcurrentDictionary<string, TimeSpan> _expirations = new ConcurrentDictionary<string, TimeSpan>();

        public HybridEasyCaching(IHybridCachingProvider hybridCachingProvider, IOptions<HybridEasyCachingOptions> options)
            : base(options?.Value ?? throw new ArgumentNullException(nameof(options)))
        {
            _hybridCachingProvider = hybridCachingProvider;
        }

        public byte[]? Get(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var result = _hybridCachingProvider.Get<byte[]>(key);

            return (!result.HasValue || result.IsNull) ? null : result.Value;
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var result = await _hybridCachingProvider.GetAsync<byte[]>(key).ConfigureAwait(false);

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

            _hybridCachingProvider.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _expirations.TryRemove(key, out TimeSpan _);

            return _hybridCachingProvider.RemoveAsync(key);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var expiration = GetEasyCacheExpiration(options);

            _expirations.TryAdd(key, expiration);

            _hybridCachingProvider.Set(key, value, expiration);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var expiration = GetEasyCacheExpiration(options);

            _expirations.TryAdd(key, expiration);

            return _hybridCachingProvider.SetAsync(key, value, expiration);
        }
    }
}
