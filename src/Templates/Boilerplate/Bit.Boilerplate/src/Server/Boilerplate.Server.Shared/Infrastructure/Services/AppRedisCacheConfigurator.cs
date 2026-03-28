using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed.Redis;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;

namespace Boilerplate.Server.Shared.Infrastructure.Services;

/// <summary>
/// Configures FusionCache L2, Distributed Locking, and Backplane to share a single Redis connection enrhiched with logging and retry policies.
/// </summary>
public class AppRedisCacheConfigurator([FromKeyedServices("redis-cache")] IConnectionMultiplexer connectionMultiplexer) : IPostConfigureOptions<RedisCacheOptions>,
    IPostConfigureOptions<RedisDistributedLockerOptions>,
    IPostConfigureOptions<RedisBackplaneOptions>
{
    public void PostConfigure(string? name, RedisCacheOptions options)
    {
        options.ConnectionMultiplexerFactory = async () => connectionMultiplexer;
    }

    public void PostConfigure(string? name, RedisBackplaneOptions options)
    {
        options.ConnectionMultiplexerFactory = async () => connectionMultiplexer;
    }

    public void PostConfigure(string? name, RedisDistributedLockerOptions options)
    {
        options.ConnectionMultiplexerFactory = async () => connectionMultiplexer;
    }
}

public static class AppRedisConfiguratorExtensions
{
    public static IServiceCollection ConfigureRedisOptions(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RedisCacheOptions>, AppRedisCacheConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RedisBackplaneOptions>, AppRedisCacheConfigurator>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RedisDistributedLockerOptions>, AppRedisCacheConfigurator>());

        return services;
    }
}
