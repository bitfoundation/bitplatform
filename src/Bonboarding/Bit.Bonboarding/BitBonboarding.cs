using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bonboarding;

/// <summary>
/// Extension methods to register the Bit.Bonboarding services in the DI container, plus small
/// helpers that make the common cases read well in markup.
/// </summary>
public static class BitBonboarding
{
    /// <summary>
    /// Registers everything the library needs. All services are scoped, which is per-circuit on
    /// Blazor Server and per-application on WebAssembly. In a Blazor Web App, call it in both the
    /// server and the client project:
    /// <code>builder.Services.AddBitBonboardingServices();</code>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional global defaults.</param>
    public static IServiceCollection AddBitBonboardingServices(this IServiceCollection services, Action<BonboardingOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<BonboardingOptions>();
        if (configure is not null) services.Configure(configure);

        // The interop DTOs cross the JS boundary through reflection-based System.Text.Json, which the
        // trimmer cannot see, so their members are rooted explicitly.
        PreserveInteropContracts();

        services.TryAddScoped<BonboardingHostRegistry>();
        services.TryAddScoped<IBonboardingInterop, BonboardingInterop>();
        services.TryAddScoped<IBonboardingStore, BonboardingBrowserStore>();
        services.TryAddScoped<IBonboardingLocalizer, BonboardingDefaultLocalizer>();
        services.TryAddScoped<IBonboardingService, BonboardingService>();

        return services;
    }

    /// <summary>
    /// Replaces the persistence layer, for example to store progress against the user's profile on
    /// the server instead of in the browser.
    /// </summary>
    public static IServiceCollection AddBitBonboardingStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(this IServiceCollection services)
        where TStore : class, IBonboardingStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IBonboardingStore>();
        services.AddScoped<IBonboardingStore, TStore>();
        return services;
    }

    /// <summary>Keeps onboarding progress in memory only, so nothing is written to the browser.</summary>
    public static IServiceCollection AddBitBonboardingInMemoryStore(this IServiceCollection services)
        => services.AddBitBonboardingStore<BonboardingInMemoryStore>();

    /// <summary>Replaces the label source, for example with an <c>IStringLocalizer</c> adapter.</summary>
    public static IServiceCollection AddBitBonboardingLocalizer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TLocalizer>(this IServiceCollection services)
        where TLocalizer : class, IBonboardingLocalizer
    {
        ArgumentNullException.ThrowIfNull(services);

        services.RemoveAll<IBonboardingLocalizer>();
        services.AddScoped<IBonboardingLocalizer, TLocalizer>();
        return services;
    }

    /// <summary>
    /// Adds an analytics sink. Several observers may be registered; each receives every event and
    /// a throwing observer can never break a tour.
    /// </summary>
    public static IServiceCollection AddBitBonboardingObserver<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TObserver>(this IServiceCollection services)
        where TObserver : class, IBonboardingObserver
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IBonboardingObserver, TObserver>();
        return services;
    }

    /// <summary>
    /// Splattable attributes that mark an element as a named anchor:
    /// <c>&lt;button @attributes="BitBonboarding.Anchor("create")"&gt;</c>. Equivalent to writing
    /// <c>data-bonboarding-anchor="create"</c> by hand, which is also perfectly fine.
    /// </summary>
    public static IReadOnlyDictionary<string, object> Anchor(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Dictionary<string, object>(1) { [BonboardingStepTarget.AnchorAttribute] = name };
    }

    // Roots the public properties and constructors of every type (de)serialized across JS interop,
    // so the trimmer keeps them. The attributes take effect because this method is reachable from
    // AddBitBonboardingServices.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingStepActivation))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingTargetDescriptor))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingAdvanceTriggerDescriptor))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingStepGeometry))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingBrowserEnvironment))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingRect))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingSize))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingInterop.KeyPayload))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors, typeof(BonboardingInterop.InitializeOptions))]
    private static void PreserveInteropContracts() { }
}
