using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace Boilerplate.Shared.Infrastructure.Services;

/// <summary>
/// Total capacity of the in memory cache has been set in Shared/appsettings.json in the "MemoryCache" section, and it
/// prevents the cache from growing indefinitely and consuming too much memory, which could lead to performance
/// degradation or out-of-memory exceptions.
///
/// The unit is bytes. It has to be, because the ASP.NET Core output cache stores its response bodies in this very same
/// cache and charges each one its exact length (See FusionOutputCacheStore, wired up by AddFusionOutputCache). Counting
/// entries instead would let a single cached page or attachment consume the entire budget.
///
/// Entries that don't know their own size - 3rd party libraries mostly, some of which don't set Size at all
/// (https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers/issues/1190) - are charged
/// <see cref="EstimatedEntrySizeInBytes"/> instead. That's deliberately an over-estimate: charging an entry too much
/// only means fewer of them fit, while charging too little lets the cache outgrow the limit it exists to enforce.
/// </summary>
public class AppMemoryCache(IOptions<MemoryCacheOptions> optionsAccessor, ILoggerFactory loggerFactory) : IMemoryCache
{
    /// <summary>
    /// What an entry that doesn't report a size of its own is charged against the "MemoryCache:SizeLimit" budget.
    /// </summary>
    public const int EstimatedEntrySizeInBytes = 4 * 1024;

    private readonly MemoryCache implementation = new(optionsAccessor, loggerFactory);

    public ICacheEntry CreateEntry(object key)
    {
        var entry = implementation.CreateEntry(key);
        entry.Size ??= EstimatedEntrySizeInBytes;
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
