using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Maui.Hosting;

namespace Bit.Blex.Maui;

/// <summary>DI helpers for wiring Blex into a .NET MAUI application.</summary>
/// <remarks>
/// <para>
/// A native MAUI app has no DI scopes: everything is resolved from the application's root provider
/// for the process lifetime. The <see cref="MauiAppBuilder"/> helpers here therefore register Blex
/// as <see cref="ServiceLifetime.Singleton"/> rather than the <see cref="ServiceLifetime.Scoped"/>
/// default that Blazor wants. Besides being the honest lifetime, it keeps the registrations valid
/// when the container is built with <c>ValidateScopes</c> enabled, which rejects resolving a scoped
/// service from the root provider -- something the startup initializer has to do.
/// </para>
/// <para>
/// Blazor Hybrid is the other way round: the <c>BlazorWebView</c> creates a scope, so those apps use
/// the core <c>AddBitBlexServices</c>/<c>AddBitBlexStore</c> and the <see cref="IServiceCollection"/> overload of
/// <see cref="AddBitBlexPreferencesPersistence(IServiceCollection, Action{BlexPersistenceOptions}, ServiceLifetime)"/>,
/// all of which stay scoped by default.
/// </para>
/// </remarks>
public static class BitBlexMaui
{
    /// <summary>
    /// Registers the Blex manager and middleware (like <c>AddBitBlexServices</c>, but as singletons) plus the
    /// MAUI startup initializer that registers stores, rehydrates persisted state and starts
    /// history when the app is built. Register each store with
    /// <see cref="AddBitBlexStore{TStore}(MauiAppBuilder)"/> afterwards.
    /// </summary>
    /// <example><code>
    /// var builder = MauiApp.CreateBuilder();
    /// builder.UseBitBlex(options => options.DevToolsName = "My App");
    /// builder.AddBitBlexStore&lt;CounterStore&gt;();
    /// builder.AddBitBlexPreferencesPersistence();
    /// </code></example>
    public static MauiAppBuilder UseBitBlex(this MauiAppBuilder builder, Action<BlexOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddBitBlexServices(configure, ServiceLifetime.Singleton);
        builder.Services.AddBitBlexMauiInitializer();
        return builder;
    }

    /// <summary>
    /// Registers a store as a singleton -- the lifetime a scope-less MAUI host needs. Pairs with
    /// <see cref="UseBitBlex"/>.
    /// </summary>
    public static MauiAppBuilder AddBitBlexStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(this MauiAppBuilder builder)
        where TStore : BlexStoreBase
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddBitBlexStore<TStore>(ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>Registers in-app undo/redo as a singleton. Pairs with <see cref="UseBitBlex"/>.</summary>
    /// <param name="builder">The MAUI app builder.</param>
    /// <param name="maxEntries">Maximum number of undo entries retained.</param>
    /// <param name="filter">
    /// Optional predicate deciding which actions become their own undo step. See
    /// <see cref="BlexHistory(BlexManager, int, Func{BlexActionContext, bool}, Func{BlexActionContext, string})"/>.
    /// </param>
    /// <param name="groupBy">
    /// Optional key selector that merges consecutive actions into a single undo step (redux-undo's
    /// <c>groupBy</c>).
    /// </param>
    public static MauiAppBuilder AddBitBlexHistory(
        this MauiAppBuilder builder,
        int maxEntries = 100,
        Func<BlexActionContext, bool>? filter = null,
        Func<BlexActionContext, string?>? groupBy = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddBitBlexHistory(maxEntries, ServiceLifetime.Singleton, filter, groupBy);
        return builder;
    }

    /// <summary>
    /// Persists stores marked <c>[BlexStoreAttribute(Persist = true)]</c> to MAUI <c>Preferences</c> as a
    /// singleton. Pairs with <see cref="UseBitBlex"/>.
    /// </summary>
    public static MauiAppBuilder AddBitBlexPreferencesPersistence(
        this MauiAppBuilder builder,
        Action<BlexPersistenceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddBitBlexPreferencesPersistence(configure, ServiceLifetime.Singleton);
        return builder;
    }

    /// <summary>
    /// Persists stores marked <c>[BlexStoreAttribute(Persist = true)]</c> to .NET MAUI
    /// <c>Preferences</c> (OS-native app settings). State is rehydrated automatically when
    /// <c>MauiApp.Build()</c> runs. Call after <c>UseBitBlex</c>/<c>AddBitBlexServices</c> and the store
    /// registrations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures <see cref="BlexPersistenceOptions"/>.</param>
    /// <param name="lifetime">
    /// Lifetime of the persistor and the <c>Preferences</c> storage. Defaults to
    /// <see cref="ServiceLifetime.Scoped"/> so Blazor Hybrid keeps matching the rest of its
    /// (scoped) Blex registrations; the <see cref="MauiAppBuilder"/> overload passes
    /// <see cref="ServiceLifetime.Singleton"/> for native MAUI. Register your own
    /// <see cref="IBlexStorage"/> beforehand to override the storage -- the first registration
    /// wins, so give it the same lifetime.
    /// </param>
    public static IServiceCollection AddBitBlexPreferencesPersistence(
        this IServiceCollection services,
        Action<BlexPersistenceOptions>? configure = null,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAdd(new ServiceDescriptor(typeof(IBlexStorage), typeof(BlexPreferencesStorage), lifetime));
        services.AddBitBlexPersistence(configure, lifetime);
        services.AddBitBlexMauiInitializer();
        return services;
    }

    /// <summary>
    /// Registers the startup initializer on its own -- useful when Blex was registered with the
    /// core <c>AddBitBlexServices</c> (rather than <see cref="UseBitBlex"/>) but store registration, hydration
    /// and history recording should still happen automatically at <c>MauiApp.Build()</c> time.
    /// Idempotent: calling it multiple times registers a single initializer.
    /// </summary>
    public static IServiceCollection AddBitBlexMauiInitializer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMauiInitializeService, BlexMauiInitializer>());
        return services;
    }
}
