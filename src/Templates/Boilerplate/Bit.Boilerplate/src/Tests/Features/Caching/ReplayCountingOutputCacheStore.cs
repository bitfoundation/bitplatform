using Microsoft.AspNetCore.OutputCaching;

namespace Boilerplate.Tests.Features.Caching;

/// <summary>
/// Wraps whichever <see cref="IOutputCacheStore"/> is registered and counts the reads it answered, so a test can tell
/// a response replayed from the cache apart from one the app simply produced again.
/// </summary>
public sealed class ReplayCountingOutputCacheStore(IOutputCacheStore inner, ReplayCountingOutputCacheStore.Counter counter) : IOutputCacheStore
{
    public sealed class Counter
    {
        private int count;

        public int Count => Volatile.Read(ref count);

        public void Increment() => Interlocked.Increment(ref count);

        public void Reset() => Volatile.Write(ref count, 0);
    }

    public async ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var value = await inner.GetAsync(key, cancellationToken);

        if (value is not null)
            counter.Increment();

        return value;
    }

    public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
        => inner.SetAsync(key, value, tags, validFor, cancellationToken);

    public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        => inner.EvictByTagAsync(tag, cancellationToken);
}

public static class ReplayCountingOutputCacheStoreExtensions
{
    /// <summary>
    /// Decorates the registered store rather than replacing it, so the real memory store still stores and evicts.
    /// </summary>
    public static IServiceCollection CountOutputCacheReplays(this IServiceCollection services, ReplayCountingOutputCacheStore.Counter counter)
    {
        var registered = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IOutputCacheStore))
            ?? throw new InvalidOperationException($"No {nameof(IOutputCacheStore)} is registered to decorate.");

        services.Remove(registered);

        services.AddSingleton<IOutputCacheStore>(sp => new ReplayCountingOutputCacheStore(Resolve(sp, registered), counter));

        return services;
    }

    private static IOutputCacheStore Resolve(IServiceProvider sp, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IOutputCacheStore instance)
            return instance;

        if (descriptor.ImplementationFactory is not null)
            return (IOutputCacheStore)descriptor.ImplementationFactory(sp);

        return (IOutputCacheStore)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!);
    }
}
