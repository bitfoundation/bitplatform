using Bit.Besql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteDbContextFactory<TContext>(
       this IServiceCollection services,
       Action<IServiceProvider, DbContextOptionsBuilder>? optionsAction)
        where TContext : DbContext
    {
        if (OperatingSystem.IsBrowser())
        {
            services.TryAddScoped<IStorage, BrowserCacheStorage>();
            services.AddDbContextFactory<TContext, SqliteWasmDbContextFactory<TContext>>(
                optionsAction ?? ((s, p) => { }), ServiceLifetime.Scoped);
        }
        else
        {
            services.AddDbContextFactory<TContext>(
                optionsAction ?? ((s, p) => { }), ServiceLifetime.Scoped);
        }

        return services;
    }

    public static IServiceCollection AddSqliteDbContextFactory<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder>? optionsAction)
    where TContext : DbContext
    {
        return services.AddSqliteDbContextFactory<TContext>((s, p) => optionsAction?.Invoke(p));
    }

    public static IServiceCollection AddSqliteDbContextFactory<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
    {
        return services.AddSqliteDbContextFactory<TContext>(options => { });
    }
}
