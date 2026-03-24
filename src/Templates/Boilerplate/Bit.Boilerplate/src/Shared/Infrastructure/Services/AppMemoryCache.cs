using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace Boilerplate.Shared.Infrastructure.Services;

/// <summary>
/// Total capacity of the in memory cache has been set in Shared/appsettings.json
/// Unit is arbitrary; we treat it as 1 unit per average entry
/// This would prevent the cache from growing indefinitely and consuming too much memory, which could lead to performance degradation or out-of-memory exceptions.
/// While `FusionCache` would set Size to 1 if not set (Check out src\Server\Boilerplate.Server.Shared\Infrastructure\Extensions\WebApplicationBuilderExtensions.cs)
/// some 3rd party libraries may not set Size when adding entries to the cache, so we set it to 1 by default in CreateEntry method to ensure that all entries are counted towards the cache size limit, preventing runtime errors.
/// https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/issues/1190
/// </summary>
public class AppMemoryCache(IOptions<MemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory) : IMemoryCache
{
    private readonly MemoryCache implementation = new(optionsAccessor, loggerFactory);

    public ICacheEntry CreateEntry(object key)
    {
        var entry = implementation.CreateEntry(key);
        entry.Size ??= 1;
        return entry;
    }

    public void Dispose()
    {
        implementation.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Remove(object key)
    {
        implementation.Remove(key);
    }

    public bool TryGetValue(object key, out object? value)
    {
        return implementation.TryGetValue(key, out value);
    }
}
