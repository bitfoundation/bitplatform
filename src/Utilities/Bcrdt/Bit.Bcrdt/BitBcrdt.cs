using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bit.Bcrdt;

/// <summary>
/// Configures collaboration for an application.
/// </summary>
/// <remarks>
/// Each of the three seams - transport, storage and options - is independently optional. An
/// application that configures none of them still gets a working, purely local collaborative
/// document, which is the right starting point: the model is the same offline and online, so a
/// feature can be built and tested without a server and then connected to one without changing how
/// it reads or writes state.
/// </remarks>
public sealed class BcrdtCollaborationBuilder
{
    internal BcrdtCollaborationBuilder(IServiceCollection services) => Services = services;

    /// <summary>The service collection being configured.</summary>
    public IServiceCollection Services { get; }

    /// <summary>The options instance the service will be constructed with.</summary>
    public BcrdtCollaborationOptions Options { get; } = new();

    /// <summary>Adjusts the application-wide options.</summary>
    public BcrdtCollaborationBuilder Configure(Action<BcrdtCollaborationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(Options);
        return this;
    }

    /// <summary>Registers where connections come from.</summary>
    public BcrdtCollaborationBuilder UseTransport<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFactory>() where TFactory : class, IBcrdtTransportFactory
    {
        Services.TryAddSingleton<IBcrdtTransportFactory, TFactory>();
        return this;
    }

    /// <summary>Registers a transport factory instance.</summary>
    public BcrdtCollaborationBuilder UseTransport(IBcrdtTransportFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.TryAddSingleton(factory);
        return this;
    }

    /// <summary>Registers a transport built from a delegate.</summary>
    public BcrdtCollaborationBuilder UseTransport(Func<string, IBcrdtTransport> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Services.TryAddSingleton<IBcrdtTransportFactory>(new DelegateTransportFactory(factory));
        return this;
    }

    /// <summary>Registers where documents are stored.</summary>
    public BcrdtCollaborationBuilder UseStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>() where TStore : class, IBcrdtDocumentStore
    {
        Services.TryAddSingleton<IBcrdtDocumentStore, TStore>();
        return this;
    }

    /// <summary>Registers a store instance.</summary>
    public BcrdtCollaborationBuilder UseStore(IBcrdtDocumentStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        Services.TryAddSingleton(store);
        return this;
    }

    /// <summary>Stores documents in process memory, for tests and ephemeral sessions.</summary>
    public BcrdtCollaborationBuilder UseInMemoryStore() => UseStore(new BcrdtInMemoryDocumentStore());

    /// <summary>
    /// Stores documents in the browser's IndexedDB, so they - and the edits made offline - survive a
    /// reload, a closed tab and a restarted browser. See <see cref="BcrdtIndexedDbDocumentStore"/>.
    /// </summary>
    /// <remarks>
    /// Registered per scope, because it talks to the browser through the scope's
    /// <see cref="Microsoft.JSInterop.IJSRuntime"/>: in WebAssembly that is the page's, in Blazor Server the
    /// circuit's - which keeps each user's documents in their own browser.
    /// </remarks>
    /// <param name="databaseName">The IndexedDB database to use.</param>
    public BcrdtCollaborationBuilder UseIndexedDbStore(string databaseName = BcrdtIndexedDbDocumentStore.DefaultDatabaseName)
    {
        ArgumentException.ThrowIfNullOrEmpty(databaseName);
        Services.TryAddScoped<IBcrdtDocumentStore>(provider =>
            new BcrdtIndexedDbDocumentStore(provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), databaseName));
        return this;
    }

    private sealed class DelegateTransportFactory(Func<string, IBcrdtTransport> factory) : IBcrdtTransportFactory
    {
        public IBcrdtTransport Create(string documentId) => factory(documentId);
    }
}

/// <summary>Registers bit Bcrdt with the dependency injection container.</summary>
public static class BitBcrdt
{
    /// <summary>
    /// Adds collaboration services.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The collaboration service is registered as a <b>scoped</b> service, and that is important
    /// rather than incidental. In Blazor Server a scope is one user's circuit, so scoped registration
    /// gives every connected user their own replica and their own client identity; registering it as
    /// a singleton would make every visitor share one replica and one identity, and their edits
    /// would be indistinguishable to the engine. In WebAssembly there is exactly one scope, so the
    /// same registration behaves as a singleton - which is what a single-user client wants.
    /// </para>
    /// <para>
    /// The same registration therefore works unchanged in WebAssembly, Server and Hybrid, which is
    /// the point.
    /// </para>
    /// <para>
    /// One Server-specific consequence: a circuit outlives its browser tab by the circuit retention
    /// period, and its documents keep publishing presence until then, so a person who closed the tab
    /// stays listed for minutes. To withdraw them as soon as the connection drops, register a
    /// <c>CircuitHandler</c> that calls <see cref="BcrdtPresenceService.Suspend"/> on every open
    /// document in <c>OnConnectionDownAsync</c> and <see cref="BcrdtPresenceService.Resume"/> in
    /// <c>OnConnectionUpAsync</c>. It is not registered here because this package also runs in
    /// WebAssembly, where circuits do not exist.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddBitBcrdtServices(
        this IServiceCollection services,
        Action<BcrdtCollaborationBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var builder = new BcrdtCollaborationBuilder(services);
        configure?.Invoke(builder);

        services.TryAddSingleton(builder.Options);
        services.TryAddScoped<IBcrdtCollaborationService>(provider => new BcrdtCollaborationService(
            provider.GetRequiredService<BcrdtCollaborationOptions>(),
            provider.GetService<IBcrdtTransportFactory>(),
            provider.GetService<IBcrdtDocumentStore>()));

        return services;
    }
}
