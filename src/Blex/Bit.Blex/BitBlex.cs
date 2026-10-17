using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex;

/// <summary>DI helpers for registering Blex and its stores.</summary>
/// <remarks>
/// Every registration defaults to <see cref="ServiceLifetime.Scoped"/>, which is what Blazor wants:
/// a Blazor Server circuit is a scope, so each user gets their own manager and stores. Hosts with
/// no scopes -- .NET MAUI, console apps, workers -- should pass
/// <see cref="ServiceLifetime.Singleton"/> instead (<c>Bit.Blex.Maui</c>'s <c>UseBitBlex()</c> does this
/// for you). Resolving a scoped service from the root provider works today but fails as soon as the
/// container is built with <c>ValidateScopes</c> enabled.
/// </remarks>
public static class BitBlex
{
    /// <summary>
    /// Registers the Blex manager and middleware. Call <see cref="AddBitBlexStore{TStore}"/> for each store.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures <see cref="BlexOptions"/>.</param>
    /// <param name="lifetime">
    /// Lifetime of the manager and DI-resolved middleware. Defaults to
    /// <see cref="ServiceLifetime.Scoped"/> (Blazor); use <see cref="ServiceLifetime.Singleton"/>
    /// in hosts without scopes. Must match the lifetime used for the stores.
    /// </param>
    [UnconditionalSuppressMessage("Trimming", "IL2072",
        Justification = "Every Type in BlexOptions.MiddlewareTypes was added by UseMiddleware<TMiddleware>, whose type argument is annotated with PublicConstructors.")]
    public static IServiceCollection AddBitBlexServices(
        this IServiceCollection services,
        Action<BlexOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new BlexOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        foreach (var type in options.MiddlewareTypes)
        {
            // Registered concretely, with the IBlexMiddleware registration forwarding to it, so
            // resolving either way inside one scope yields the same instance.
            services.Add(new ServiceDescriptor(type, type, lifetime));
            services.Add(new ServiceDescriptor(typeof(IBlexMiddleware), sp => sp.GetRequiredService(type), lifetime));
        }

        services.Add(new ServiceDescriptor(typeof(BlexManager), sp =>
        {
            var opts = sp.GetRequiredService<BlexOptions>();

            // Registration order, exactly as written: order decides which veto is consulted first
            // and what a logger sees.
            var resolved = new List<IBlexMiddleware>(opts.MiddlewareEntries.Count);
            foreach (var entry in opts.MiddlewareEntries)
            {
                resolved.Add(entry is Type type
                    ? (IBlexMiddleware)sp.GetRequiredService(type)
                    : (IBlexMiddleware)entry);
            }

            // Middleware registered straight into the service collection (not through the options)
            // still participates -- appended, and de-duplicated against what is already resolved,
            // since the IBlexMiddleware registrations above forward to those same instances.
            foreach (var extra in sp.GetServices<IBlexMiddleware>())
            {
                if (!resolved.Contains(extra))
                    resolved.Add(extra);
            }

            var manager = new BlexManager(resolved)
            {
                DevToolsStateSanitizer = opts.DevToolsStateSanitizer,
                DevToolsActionSanitizer = opts.DevToolsActionSanitizer,
                OnError = opts.OnError,
            };

            if (opts.OnActionError is { } onActionError)
                manager.ActionFailed += onActionError;

            return manager;
        }, lifetime));

        return services;
    }

    /// <summary>
    /// Registers a store exposed both as its concrete type and as <see cref="IBlexStore"/>.
    /// The store is attached to the <see cref="BlexManager"/> on first resolution, so actions are
    /// observed even in hosts without a <c>&lt;BlexProvider&gt;</c> (console apps, workers, tests).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="lifetime">
    /// Defaults to <see cref="ServiceLifetime.Scoped"/> (Blazor); use
    /// <see cref="ServiceLifetime.Singleton"/> in hosts without scopes. Must match the lifetime
    /// passed to <see cref="AddBitBlexServices"/>.
    /// </param>
    public static IServiceCollection AddBitBlexStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(
        this IServiceCollection services,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TStore : BlexStoreBase
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Add(new ServiceDescriptor(typeof(TStore), sp =>
        {
            var store = ActivatorUtilities.CreateInstance<TStore>(sp);
            sp.GetRequiredService<BlexManager>().Register(store);
            return store;
        }, lifetime));

        services.Add(new ServiceDescriptor(typeof(IBlexStore), sp => sp.GetRequiredService<TStore>(), lifetime));
        return services;
    }

    /// <summary>
    /// Enables automatic persistence for stores marked <c>[BlexStoreAttribute(Persist = true)]</c>. Requires an
    /// <see cref="IBlexStorage"/> to be registered (for Blazor use <c>AddBitBlexLocalStoragePersistence</c>
    /// or <c>AddBitBlexSessionStoragePersistence</c> from <c>Bit.Blex.Blazor</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures <see cref="BlexPersistenceOptions"/>.</param>
    /// <param name="lifetime">
    /// Defaults to <see cref="ServiceLifetime.Scoped"/>; must match the lifetime passed to
    /// <see cref="AddBitBlexServices"/>.
    /// </param>
    public static IServiceCollection AddBitBlexPersistence(
        this IServiceCollection services,
        Action<BlexPersistenceOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new BlexPersistenceOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);
        services.Add(new ServiceDescriptor(typeof(BlexStatePersistor), sp => new BlexStatePersistor(
            sp.GetRequiredService<BlexManager>(),
            sp.GetRequiredService<IBlexStorage>(),
            sp.GetRequiredService<BlexPersistenceOptions>()), lifetime));
        return services;
    }

    /// <summary>
    /// Registers in-app undo/redo (<see cref="BlexHistory"/>). Resolve <see cref="BlexHistory"/>
    /// to call <c>Undo()</c>/<c>Redo()</c>; <c>&lt;BlexProvider&gt;</c> starts recording automatically.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="maxEntries">Maximum number of undo entries retained.</param>
    /// <param name="lifetime">
    /// Defaults to <see cref="ServiceLifetime.Scoped"/>; must match the lifetime passed to
    /// <see cref="AddBitBlexServices"/>.
    /// </param>
    /// <param name="filter">
    /// Optional predicate deciding which actions become their own undo step. See
    /// <see cref="BlexHistory(BlexManager, int, Func{BlexActionContext, bool}, Func{BlexActionContext, string})"/>.
    /// </param>
    /// <param name="groupBy">
    /// Optional key selector that merges consecutive actions into a single undo step (redux-undo's
    /// <c>groupBy</c>).
    /// </param>
    public static IServiceCollection AddBitBlexHistory(
        this IServiceCollection services,
        int maxEntries = 100,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<BlexActionContext, bool>? filter = null,
        Func<BlexActionContext, string?>? groupBy = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Add(new ServiceDescriptor(
            typeof(BlexHistory),
            sp => new BlexHistory(sp.GetRequiredService<BlexManager>(), maxEntries, filter, groupBy),
            lifetime));
        return services;
    }
}
