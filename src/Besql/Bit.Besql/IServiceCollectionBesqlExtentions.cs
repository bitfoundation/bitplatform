using Bit.Besql;
using Microsoft.EntityFrameworkCore;
#if NET9_0_OR_GREATER
using Microsoft.EntityFrameworkCore.Migrations;
#endif
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionBesqlExtentions
{
    public static IServiceCollection AddBesqlDbContextFactory<TDbContext>(this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = null,
        Func<IServiceProvider, TDbContext, Task>? dbContextInitializer = null)
        where TDbContext : DbContext
    {
        optionsAction ??= (_, _) => { };
        dbContextInitializer ??= async (_, _) => { };

        services.AddSingleton(dbContextInitializer);

        if (OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser())
        {
            AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);
        }

        if (OperatingSystem.IsBrowser())
        {
            services.AddSingleton<BesqlDbContextInterceptor>();
            services.TryAddSingleton<IBesqlStorage, BrowserCacheBesqlStorage>();
            // To make optimized db context work in blazor wasm: https://github.com/dotnet/efcore/issues/31751
            // https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-models
            services.AddDbContextFactory<TDbContext, BesqlPooledDbContextFactory<TDbContext>>((serviceProvider, options) =>
            {
                options.AddInterceptors(serviceProvider.GetRequiredService<BesqlDbContextInterceptor>());
#if NET9_0_OR_GREATER
                options.ReplaceService<IHistoryRepository, BesqlHistoryRepository>();
#endif
                optionsAction.Invoke(serviceProvider, options);
            });
        }
        else
        {
            services.TryAddSingleton<IBesqlStorage, NoopBesqlStorage>();
            services.AddDbContextFactory<TDbContext, PooledDbContextFactoryBase<TDbContext>>(optionsAction);
        }

        return services;
    }

    public static IServiceCollection AddBesqlDbContextFactory<TDbContext>(this IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        Func<TDbContext, Task>? dbContextInitializer = null)
        where TDbContext : DbContext
    {
        optionsAction ??= _ => { };
        dbContextInitializer ??= async _ => { };

        return services.AddBesqlDbContextFactory<TDbContext>((serviceProvider, options) => optionsAction.Invoke(options), (serviceProvider, dbContext) => dbContextInitializer.Invoke(dbContext));
    }
}
