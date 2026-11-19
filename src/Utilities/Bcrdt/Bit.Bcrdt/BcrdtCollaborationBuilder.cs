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

    /// <summary>
    /// Keeps the tabs of one browser that have the same document open in sync with each other directly, over a
    /// <c>BroadcastChannel</c> - while the browser is offline too. A no-op anywhere but WebAssembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every tab is a replica of its own. Without this, two tabs of one browser see each other's edits only through
    /// the server - not at all while the browser is offline - and with <see cref="UseIndexedDbStore"/> only after a
    /// reload. With it, each document a tab opens gets a second sync provider to the other tabs
    /// (<see cref="BcrdtCollaborativeDocument.CrossTabSync"/>, over a <see cref="BcrdtBroadcastChannelTransport"/>),
    /// next to the one to the server (<see cref="BcrdtCollaborativeDocument.Sync"/>). A tab opening the document
    /// catches up with the others by the same handshake as with the server; alone, it counts as synced once the
    /// handshake times out.
    /// </para>
    /// <para>
    /// Each of the two providers sends on only the changes made in its own tab. A change the server sent is not
    /// passed on to the other tabs, which are in the server's room too, and a change another tab made is not passed
    /// on to the server, which that tab told itself - passed on, every tab would hand the server each change again.
    /// So while the tabs are online the server hears each change once, however many tabs are open. When a tab's
    /// connection is down, the other tabs still hear its changes, and it theirs; what the server sent it meanwhile,
    /// and what it changed meanwhile, cross with the handshake it runs when it reconnects - the one every reconnection
    /// runs.
    /// </para>
    /// <para>
    /// Presence crosses only the network. Each tab is already a participant of its own, listed in every other tab
    /// through the server; publishing it over the channel as well would send every caret move and heartbeat twice,
    /// and a tab taken offline - which withdraws from the room - would linger in the other tabs' lists. Offline, a
    /// tab sees the other tabs' edits but not their carets.
    /// </para>
    /// <para>
    /// Only WebAssembly runs the replica in the browser. In Blazor Server the replicas are in the server's circuits,
    /// which already meet through the relay, and Hybrid and console apps have no other tabs - so everywhere else,
    /// the server's prerendering included, this does nothing, and the same registration can be shared by every
    /// project of an app. Lazy loading works as with the store: the builder's container holds the
    /// <see cref="Microsoft.JSInterop.IJSRuntime"/> it needs.
    /// </para>
    /// </remarks>
    /// <param name="channelPrefix">
    /// What the name of every document's channel begins with. Applications on the same origin that must not see
    /// each other's documents use different prefixes, as they use different IndexedDB databases.
    /// </param>
    public BcrdtCollaborationBuilder UseCrossTabSync(string channelPrefix = BcrdtBroadcastChannelTransport.DefaultChannelPrefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(channelPrefix);
        if (!OperatingSystem.IsBrowser()) return this;

        Services.TryAddScoped(provider => new BcrdtCrossTabTransports(
            new BcrdtBroadcastChannelTransportFactory(provider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>(), channelPrefix)));
        return this;
    }

    private sealed class DelegateTransportFactory(Func<string, IBcrdtTransport> factory) : IBcrdtTransportFactory
    {
        public IBcrdtTransport Create(string documentId) => factory(documentId);
    }
}
