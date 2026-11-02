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
    /// stays listed for minutes. To withdraw them as soon as the connection drops, also call
    /// <c>AddBitBcrdtCircuitPresence</c> from <c>Bit.Bcrdt.Server</c>, whose circuit handler calls
    /// <see cref="BcrdtPresenceService.Suspend"/> on every open document when the connection goes down
    /// and <see cref="BcrdtPresenceService.Resume"/> when it comes back. It is not registered here
    /// because this package also runs in WebAssembly, where circuits do not exist.
    /// </para>
    /// <para>
    /// A WebAssembly app that lazy loads Bit.Bcrdt leaves it out of the project that runs in the browser, and
    /// configures the service through <see cref="GetBcrdtCollaborationService"/> instead. Everywhere else it is required:
    /// the collaborative components fail when they are created without it.
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

    /// <summary>
    /// The <see cref="IBcrdtCollaborationService"/> the collaborative components of this circuit or WebAssembly runtime
    /// use: the one <see cref="AddBitBcrdtServices"/> registered or, in a WebAssembly app that lazy loads Bit.Bcrdt and
    /// so cannot register it, the one this configures. Inject <see cref="IBcrdtCollaborationService"/> instead wherever
    /// the services are registered; an app that lazy loads Bit.Bcrdt configures it like this, before anything uses it:
    /// <code>
    /// @inject IServiceProvider Services
    /// @inject NavigationManager Navigation
    ///
    /// protected override void OnInitialized() => Services.GetBcrdtCollaborationService(collaboration => collaboration
    ///     .UseTransport(new BcrdtWebSocketTransportFactory(Navigation.BaseUri + "ws/bcrdt/{documentId}"))
    ///     .UseIndexedDbStore());
    /// </code>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <paramref name="configure"/> configures the service Bcrdt creates - transport, store and options - exactly as it
    /// configures <see cref="AddBitBcrdtServices"/>, except that the builder's container holds nothing of the
    /// application's but its <c>IJSRuntime</c>: hand the transport and the store over as instances or delegates. Where
    /// the services are registered it is not called, so code that has to work both ways - the page of a Blazor Web App,
    /// prerendered on a server that registers them and run in a browser that lazy loads them - can always call this.
    /// </para>
    /// <para>
    /// Only the first configuration creates the service, so a page visited again, or a second page that configures it
    /// too, gets the same one back. Without a configuration nothing is created: a collaborative component - or a call
    /// without <paramref name="configure"/> - that asks first throws. Configure it in <c>OnInitialized</c> of the page
    /// or layout that holds the collaborative components, which runs before theirs, and not after an <c>await</c>; pass
    /// <c>_ => { }</c> for a purely local service, with no transport and no store.
    /// </para>
    /// <para>
    /// Outside WebAssembly Bit.Bcrdt is loaded with the application, so there the services have to be registered and
    /// this throws when they are not, configuration or not.
    /// </para>
    /// </remarks>
    /// <param name="services">The services of the caller, as injected through <see cref="IServiceProvider"/>.</param>
    /// <param name="configure">Configures the service Bcrdt creates, as for <see cref="AddBitBcrdtServices"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// The services are not registered, and either <paramref name="services"/> are not WebAssembly's or
    /// <paramref name="configure"/> is null and nothing configured the service before.
    /// </exception>
    public static IBcrdtCollaborationService GetBcrdtCollaborationService(
        this IServiceProvider services,
        Action<BcrdtCollaborationBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // A registered service wins even when it is not a BcrdtCollaborationService - a test double, or a decorator.
        return services.GetService<IBcrdtCollaborationService>() ?? BcrdtOwnCollaboration.Of(services, configure);
    }
}
