//+:cnd:noEmit
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using ZiggyCreatures.Caching.Fusion.Locking.Distributed.Redis;
using ZiggyCreatures.Caching.Fusion.Backplane.StackExchangeRedis;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR.StackExchangeRedis;
//#endif

namespace Boilerplate.Server.Shared.Infrastructure.Services;

/// <summary>
/// Configures FusionCache L2, Distributed Locking and Backplanes to share a single Redis connection enriched with logging and retry policies.
/// </summary>
public class AppRedisCacheConfigurator(
    [FromKeyedServices("redis-cache")] IConnectionMultiplexer redisCacheConnectionMultiplexer) :
        IPostConfigureOptions<RedisCacheOptions>,
        IPostConfigureOptions<RedisDistributedLockerOptions>,
        IPostConfigureOptions<RedisBackplaneOptions>
        //#if (signalR == true)
        , IPostConfigureOptions<RedisOptions>
//#endif
{
    public void PostConfigure(string? name, RedisCacheOptions options)
    {
        options.ConnectionMultiplexerFactory = async () => redisCacheConnectionMultiplexer;
    }

    public void PostConfigure(string? name, RedisBackplaneOptions options)
    {
        // Redis backplane for FusionCache, used for synchronizing cache invalidation across multiple server instances in a distributed environment.
        options.ConnectionMultiplexerFactory = async () => redisCacheConnectionMultiplexer;
    }

    public void PostConfigure(string? name, RedisDistributedLockerOptions options)
    {
        // This refers to FusionCache's internal L2 distributed locking mechanism, specifically designed to prevent Distributed Cache Stampede.
        // Note: This is distinct from the general-purpose Distributed Locker (Medallion.Threading.Redis) we use for application-level business logic.
        options.ConnectionMultiplexerFactory = async () => redisCacheConnectionMultiplexer;
    }

    //#if (signalR == true)
    public void PostConfigure(string? name, RedisOptions options)
    {
        // Redis backplane for SignalR, used for scaling out SignalR across multiple server instances.
        options.ConnectionFactory = async _ => redisCacheConnectionMultiplexer;
    }
    //#endif
}

public static class AppRedisConfiguratorExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureRedisOptions()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RedisCacheOptions>, AppRedisCacheConfigurator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RedisBackplaneOptions>, AppRedisCacheConfigurator>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RedisDistributedLockerOptions>, AppRedisCacheConfigurator>());
            //#if (signalR == true)
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RedisOptions>, AppRedisCacheConfigurator>());
            //#endif

            return services;
        }
    }
}
