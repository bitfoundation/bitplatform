using Bit.Besql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionBesqlExtentions
{
    public static IServiceCollection AddBesqlDbContextFactory<TContext>(
       this IServiceCollection services,
       Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction)
        where TContext : DbContext
    {
        if (OperatingSystem.IsBrowser())
        {
            services.TryAddScoped<IBesqlStorage, BrowserCacheBesqlStorage>();
            services.AddDbContextFactory<TContext, BesqlDbContextFactory<TContext>>(
                optionsAction ?? ((s, p) => { }), ServiceLifetime.Scoped);
        }
        else
        {
            services.AddDbContextFactory<TContext>(
                optionsAction ?? ((s, p) => { }), ServiceLifetime.Scoped);
        }

        return services;
    }

    public static IServiceCollection AddBesqlDbContextFactory<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction)
    where TContext : DbContext
    {
        return services.AddBesqlDbContextFactory<TContext>((s, p) => optionsAction?.Invoke(p));
    }

    public static IServiceCollection AddBesqlDbContextFactory<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        return services.AddBesqlDbContextFactory<TContext>(options => { });
    }
}
