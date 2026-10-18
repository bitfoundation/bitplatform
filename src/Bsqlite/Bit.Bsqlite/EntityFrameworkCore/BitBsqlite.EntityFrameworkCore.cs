using System.Diagnostics.CodeAnalysis;
using Bit.Bsqlite.EntityFrameworkCore;
using Bit.Bsqlite.Interop;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers EF Core contexts on top of <c>AddBitBsqliteServices</c>.</summary>
public static partial class BitBsqlite
{
    /// <summary>
    /// Registers <see cref="IBsqliteDbContextFactory{TContext}"/> (and
    /// <see cref="IDbContextFactory{TContext}"/>) for a context bound to a registered
    /// <see cref="BsqliteDatabase"/> - the only one, or the one
    /// <see cref="BsqliteDbContextOptions{TContext}.DatabaseName"/> names. Migrations are applied
    /// once per session unless <see cref="BsqliteDbContextOptions{TContext}.Schema"/> says otherwise.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddBitBsqliteServices(sqlite => sqlite
    ///     .AddStorageProvider&lt;BsqliteOpfsStorageProvider&gt;()
    ///     .UseStorage(s => s.Prefer("opfs"))
    ///     .AddDatabase("app.db"));
    ///
    /// builder.Services.AddBitBsqliteDbContext&lt;AppDbContext&gt;(o =>
    ///     o.OnFirstUse = (db, ct) => DemoData.SeedIfEmptyAsync(db, ct));
    ///
    /// // In a component:
    /// await using var db = await Factory.CreateDbContextAsync();
    /// </code>
    /// </example>
    public static IServiceCollection AddBitBsqliteDbContext<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] TContext>(
        this IServiceCollection services,
        Action<BsqliteDbContextOptions<TContext>>? configure = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new BsqliteDbContextOptions<TContext>();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        // Scoped like the database it is bound to.
        services.TryAddScoped<IBsqliteDbContextFactory<TContext>>(sp =>
        {
            var contextOptions = sp.GetRequiredService<BsqliteDbContextOptions<TContext>>();
            return new BsqliteDbContextFactory<TContext>(
                sp,
                DatabaseFor(sp, typeof(TContext), contextOptions.DatabaseName),
                contextOptions);
        });
        services.TryAddScoped<IDbContextFactory<TContext>>(sp =>
            sp.GetRequiredService<IBsqliteDbContextFactory<TContext>>());

        return services;
    }

    private static BsqliteDatabase DatabaseFor(IServiceProvider services, Type contextType, string? databaseName)
    {
        if (databaseName is not null)
        {
            return services.GetKeyedService<BsqliteDatabase>(databaseName)
                ?? throw new InvalidOperationException(
                    $"{contextType.Name} is bound to the database '{databaseName}', which is not "
                    + $"registered. Add it with AddDatabase(\"{databaseName}\").");
        }

        // Falling back to whichever was registered last would bind the context - and run its
        // migrations - against a database that is not its own.
        var registered = services.GetServices<BsqliteDatabaseRegistration>().ToArray();
        if (registered.Length > 1)
        {
            throw new InvalidOperationException(
                $"Several databases are registered ({string.Join(", ", registered.Select(r => $"'{r.DatabaseName}'"))}), "
                + $"so {contextType.Name} cannot tell which one it belongs to. Name it in "
                + $"AddBitBsqliteDbContext<{contextType.Name}>(o => o.DatabaseName = \"...\").");
        }

        return services.GetRequiredService<BsqliteDatabase>();
    }
}
