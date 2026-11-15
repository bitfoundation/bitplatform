using Bit.BlazeDb;
using Bit.BlazeDb.Blex;
using Bit.Blex;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers BlazeDb as the place Bit.Blex persists its stores.</summary>
public static class BlazeDbBlexServiceCollectionExtensions
{
    /// <summary>
    /// Persists the stores marked <c>[BlexStore(Persist = true)]</c> in a BlazeDb database, in place of browser
    /// <c>localStorage</c>. <paramref name="database"/> opens (or returns) the application's database on first use; it must
    /// register the state table with <see cref="BlazeDbBlexExtensions.AddBlexState"/>. Call after <c>AddBitBlexServices</c> and the store
    /// registrations, as for Blex's own persistence.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="database">Returns the database, opening it if it is not open yet - typically the application's own database service.</param>
    /// <param name="configure">Configures Blex's persistence, as <c>AddBitBlexPersistence</c> does.</param>
    /// <param name="flushOnSave">See <see cref="BlazeDbBlexStorage.FlushOnSave"/>.</param>
    /// <param name="lifetime">Must match the lifetime passed to <c>AddBitBlexServices</c>.</param>
    public static IServiceCollection AddBitBlexBlazeDbPersistence(
        this IServiceCollection services,
        Func<IServiceProvider, CancellationToken, ValueTask<BlazeDbDatabase>> database,
        Action<BlexPersistenceOptions>? configure = null,
        bool flushOnSave = true,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(database);
        services.Add(new ServiceDescriptor(
            typeof(IBlexStorage),
            sp => new BlazeDbBlexStorage(ct => database(sp, ct)) { FlushOnSave = flushOnSave },
            lifetime));
        services.AddBitBlexPersistence(configure, lifetime);
        return services;
    }
}
