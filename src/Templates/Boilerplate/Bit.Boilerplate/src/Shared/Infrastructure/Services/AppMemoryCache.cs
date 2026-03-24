using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace Boilerplate.Shared.Infrastructure.Services;

/// <summary>
/// Total capacity of the in memory cache has been set in Shared/appsettings.json in the "MemoryCacheOptions" section. The SizeLimit property is set to 100000.
/// Unit is arbitrary; we treat it as 1 unit per average cache entry
/// This would prevent the cache from growing indefinitely and consuming too much memory, which could lead to performance degradation or out-of-memory exceptions.
///
/// Some 3rd party libraries may not set Size when adding entries to the cache, so we set it to 1 by default in CreateEntry method to prevent potential runtime errors.
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
