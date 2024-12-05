using Bit.Besql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionBesqlExtentions
{
    public static IServiceCollection AddBesqlDbContextFactory<TContext>(this IServiceCollection services, Action<IServiceProvider, DbContextOptionsBuilder> optionsAction)
        where TContext : DbContext
    {
        if (OperatingSystem.IsBrowser())
        {
            services.AddSingleton<BesqlDbContextInterceptor>();
            services.TryAddSingleton<IBesqlStorage, BrowserCacheBesqlStorage>();
            // To make optimized db context work in blazor wasm: https://github.com/dotnet/efcore/issues/31751
            // https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics?tabs=with-di%2Cexpression-api-with-constant#compiled-models
            AppContext.SetSwitch("Microsoft.EntityFrameworkCore.Issue31751", true);
            services.AddDbContextFactory<TContext, BesqlPooledDbContextFactory<TContext>>((serviceProvider, options) =>
            {
                options.AddInterceptors(serviceProvider.GetRequiredService<BesqlDbContextInterceptor>());
                optionsAction.Invoke(serviceProvider, options);
            });
        }
        else
        {
            services.TryAddSingleton<IBesqlStorage, NoopBesqlStorage>();
            services.AddPooledDbContextFactory<TContext>(optionsAction);
        }

        return services;
    }

    public static IServiceCollection AddBesqlDbContextFactory<TContext>(this IServiceCollection services, Action<DbContextOptionsBuilder>? optionsAction)
        where TContext : DbContext
    {
        return services.AddBesqlDbContextFactory<TContext>((serviceProvider, options) => optionsAction?.Invoke(options));
    }
}
