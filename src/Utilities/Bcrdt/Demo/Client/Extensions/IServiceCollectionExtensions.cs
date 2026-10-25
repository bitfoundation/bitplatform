using Bit.Bcrdt;
using Bit.Bcrdt.Demo.Client.Collaboration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The services shared by the WebAssembly client and the server host. Both containers have to
/// register the same set: every page is prerendered on the server before it runs in the browser,
/// so anything a page injects must resolve in both.
/// </summary>
public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddDemoServices(this IServiceCollection services)
    {
        // The library's own registration. It gives the application a scoped IBcrdtCollaborationService wired to
        // a transport and a store, which is all a normal single-replica application needs - and it is what
        // BcrdtComponentBase and BcrdtCollaborationScope resolve. Only the live page opens a document through
        // it: the others build one service per replica by hand so that several replicas can appear side
        // by side, and those services take their timing (presence timeout, heartbeat) from
        // DemoReplica.OpenAsync rather than from here. See DemoWorkspace.
        // Its transport is the live page's relay through the demo server (see LiveRelay), registered ahead of the
        // library so AddBitBcrdtServices picks it up. Resolving it is harmless while prerendering: a transport is
        // only created when a document is opened, and the live page opens its document only once it is interactive.
        // Its store is the browser's IndexedDB, so a live document - and what was typed into it offline - survives a
        // reload or a closed tab. Like the transport, it is touched only once a document is opened, and the live
        // page opens its document only when it runs in the browser.
        services.AddScoped<LiveRelay>();
        services.AddScoped<IBcrdtTransportFactory>(sp => sp.GetRequiredService<LiveRelay>());
        services.AddBitBcrdtServices(collaboration => collaboration.UseIndexedDbStore("bit-bcrdt-demo"));

        // One workspace for the whole session. It owns each demo replica's private document store, so
        // navigating away from a page and back finds the document where it was left rather than empty.
        // Nothing on the server ever asks it for a replica: the replica pages open theirs only once they
        // run interactively in the browser (see ReplicaPageBase), never while they are prerendered.
        services.AddSingleton(_ => new DemoWorkspace(BcrdtInMemoryNetwork.Shared));

        return services;
    }
}
