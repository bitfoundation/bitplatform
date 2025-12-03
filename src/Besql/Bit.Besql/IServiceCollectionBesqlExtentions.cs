using Bit.Besql;
using Microsoft.EntityFrameworkCore;
#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;
#endif
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionBesqlExtentions
{
#if NET9_0_OR_GREATER
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BesqlHistoryRepository))]
#endif
    public static IServiceCollection AddBesqlDbContextFactory<TDbContext>(this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction = null,
        Func<IServiceProvider, TDbContext, Task>? dbContextInitializer = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TDbContext : DbContext
    {
        optionsAction ??= (_, _) => { };
        dbContextInitializer ??= async (_, _) => { };

        services.TryAddSingleton(async (IServiceProvider sp, TDbContext dbContext) =>
        {
            await dbContext.Database.ConfigureSqliteJournalMode();
            await dbContextInitializer(sp, dbContext);
        });

        AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);

        if (OperatingSystem.IsBrowser())
        {
            services.TryAddScoped<LazyAssemblyLoader>();
            services.TryAddSingleton<BesqlDbContextInterceptor>();
            services.TryAddSingleton<IBitBesqlStorage, BitBesqlBrowserCacheStorage>();
            // To make optimized db context work in blazor wasm: https://github.com/dotnet/efcore/issues/31751
            // https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-models
            services.AddDbContextFactory<TDbContext, BesqlDbContextFactory<TDbContext>>((serviceProvider, options) =>
            {
                options.AddInterceptors(serviceProvider.GetRequiredService<BesqlDbContextInterceptor>());
#if NET9_0_OR_GREATER
                options.ReplaceService<IHistoryRepository, BesqlHistoryRepository>();
#endif
                optionsAction.Invoke(serviceProvider, options);
            }, lifetime: lifetime);
        }
        else
        {
            services.TryAddSingleton<IBitBesqlStorage, BitBesqlNoopStoage>();
            services.AddDbContextFactory<TDbContext, DbContextFactoryBase<TDbContext>>((serviceProvider, options) =>
            {
#if NET9_0_OR_GREATER
                options.ReplaceService<IHistoryRepository, BesqlHistoryRepository>();
#endif
                optionsAction.Invoke(serviceProvider, options);
            }, lifetime: lifetime);
        }

        return services;
    }

    public static IServiceCollection AddBesqlDbContextFactory<TDbContext>(this IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction = null,
        Func<TDbContext, Task>? dbContextInitializer = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
        where TDbContext : DbContext
    {
        optionsAction ??= _ => { };
        dbContextInitializer ??= async _ => { };

        return services.AddBesqlDbContextFactory<TDbContext>((serviceProvider, options) => optionsAction.Invoke(options), (serviceProvider, dbContext) => dbContextInitializer.Invoke(dbContext), lifetime);
    }
}
