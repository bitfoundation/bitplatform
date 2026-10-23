using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            BlexLoggingMiddleware? logging = null;
            foreach (var entry in opts.MiddlewareEntries)
            {
                switch (entry)
                {
                    case Type type:
                        resolved.Add((IBlexMiddleware)sp.GetRequiredService(type));
                        break;

                    case BlexLoggingRegistration registration:
                        if (logging is null && sp.GetService<ILoggerFactory>() is { } loggers)
                        {
                            logging = new BlexLoggingMiddleware(loggers.CreateLogger(BlexDiagnostics.SourceName), registration.ActionLevel);
                            resolved.Add(logging);
                        }

                        break;

                    default:
                        resolved.Add((IBlexMiddleware)entry);
                        break;
                }
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
                TimeProvider = opts.TimeProvider ?? sp.GetService<TimeProvider>() ?? TimeProvider.System,
                ReportOffDispatcherChanges = opts.ReportOffDispatcherChanges,
                DetectInPlaceMutations = opts.DetectInPlaceMutations,
            };

            if (opts.OnActionError is { } onActionError)
                manager.ActionFailed += onActionError;

            // Isolated failures reach the log rather than stderr, and still reach the app's own sink.
            if (logging is not null)
            {
                var onError = opts.OnError;
                manager.OnError = error =>
                {
                    logging.LogIsolatedFailure(error);
                    onError?.Invoke(error);
                };
            }

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
    /// <see cref="IBlexStorage"/> to be registered (for Blazor use <see cref="Blazor.BitBlexBlazor.AddBitBlexLocalStoragePersistence"/>
    /// or <see cref="Blazor.BitBlexBlazor.AddBitBlexSessionStoragePersistence"/>).
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
    /// Registers the server-state cache (<see cref="Query.BlexQueryClient"/>): queries, mutations,
    /// invalidation and the prerender handoff of fetched data.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Sets the defaults every query starts from.</param>
    /// <param name="lifetime">
    /// Defaults to <see cref="ServiceLifetime.Scoped"/> -- one cache per Blazor Server circuit, so one user's
    /// data never reaches another; must match the lifetime passed to <see cref="AddBitBlexServices"/>.
    /// </param>
    public static IServiceCollection AddBitBlexQueries(
        this IServiceCollection services,
        Action<Query.BlexQueryClientOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        var options = new Query.BlexQueryClientOptions();
        configure?.Invoke(options);
        services.Add(new ServiceDescriptor(
            typeof(Query.BlexQueryClient),
            sp => new Query.BlexQueryClient(sp.GetService<BlexManager>(), options),
            lifetime));
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

    /// <summary>
    /// Registers in-app undo/redo (<see cref="BlexHistory"/>) over only the stores
    /// <paramref name="scope"/> admits, so an undo never reverts a store outside it -- the session, the
    /// theme, a cart. <c>&lt;BlexProvider&gt;</c> starts recording automatically.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="scope">Decides which stores the history covers (<c>s =&gt; s is not SessionStore</c>).</param>
    /// <param name="maxEntries">Maximum number of undo entries retained.</param>
    /// <param name="lifetime">
    /// Defaults to <see cref="ServiceLifetime.Scoped"/>; must match the lifetime passed to
    /// <see cref="AddBitBlexServices"/>.
    /// </param>
    /// <param name="filter">Optional predicate deciding which covered actions become their own undo step.</param>
    /// <param name="groupBy">Optional key selector that merges consecutive covered actions into a single undo step.</param>
    public static IServiceCollection AddBitBlexHistory(
        this IServiceCollection services,
        Func<IBlexStore, bool> scope,
        int maxEntries = 100,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<BlexActionContext, bool>? filter = null,
        Func<BlexActionContext, string?>? groupBy = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(scope);
        services.Add(new ServiceDescriptor(
            typeof(BlexHistory),
            sp => new BlexHistory(sp.GetRequiredService<BlexManager>(), scope, maxEntries, filter, groupBy),
            lifetime));
        return services;
    }

    /// <summary>
    /// Registers undo/redo for one store type only (<see cref="BlexHistory{TStore}"/>), alongside or
    /// instead of the application-wide <see cref="BlexHistory"/>. Inject
    /// <c>BlexHistory&lt;TStore&gt;</c>; it starts recording when it is first resolved.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="maxEntries">Maximum number of undo entries retained.</param>
    /// <param name="lifetime">
    /// Defaults to <see cref="ServiceLifetime.Scoped"/>; must match the lifetime passed to
    /// <see cref="AddBitBlexServices"/>.
    /// </param>
    /// <param name="filter">Optional predicate deciding which actions become their own undo step.</param>
    /// <param name="groupBy">Optional key selector that merges consecutive actions into a single undo step.</param>
    /// <remarks>
    /// It starts on resolution rather than waiting for <c>&lt;BlexProvider&gt;</c>, because a component
    /// typically resolves it after the stores exist. If that happens before persistence has hydrated
    /// the store, hydration is a restore, and the history moves its baseline to the hydrated state.
    /// </remarks>
    public static IServiceCollection AddBitBlexHistory<TStore>(
        this IServiceCollection services,
        int maxEntries = 100,
        ServiceLifetime lifetime = ServiceLifetime.Scoped,
        Func<BlexActionContext, bool>? filter = null,
        Func<BlexActionContext, string?>? groupBy = null)
        where TStore : class, IBlexStore
    {
        ArgumentNullException.ThrowIfNull(services);
        services.Add(new ServiceDescriptor(
            typeof(BlexHistory<TStore>),
            sp =>
            {
                var history = new BlexHistory<TStore>(sp.GetRequiredService<BlexManager>(), maxEntries, filter, groupBy);
                history.Start();
                return history;
            },
            lifetime));
        return services;
    }
}
