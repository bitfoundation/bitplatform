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
    /// <see cref="IDbContextFactory{TContext}"/>) for a context bound to the registered
    /// <see cref="BsqliteDatabase"/>. Migrations are applied once per session unless
    /// <see cref="BsqliteDbContextOptions{TContext}.Schema"/> says otherwise.
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
            new BsqliteDbContextFactory<TContext>(
                sp,
                sp.GetRequiredService<BsqliteDatabase>(),
                sp.GetRequiredService<BsqliteDbContextOptions<TContext>>()));
        services.TryAddScoped<IDbContextFactory<TContext>>(sp =>
            sp.GetRequiredService<IBsqliteDbContextFactory<TContext>>());

        return services;
    }
}
