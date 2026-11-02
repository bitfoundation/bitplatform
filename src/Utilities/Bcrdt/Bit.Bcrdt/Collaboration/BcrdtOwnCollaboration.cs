using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bcrdt;

/// <summary>
/// The collaboration service Bcrdt creates itself, once per WebAssembly runtime, from the configuration given to
/// <see cref="BitBcrdt.GetBcrdtCollaborationService"/> where <see cref="BitBcrdt.AddBitBcrdtServices"/> registered none.
/// </summary>
/// <remarks>
/// <para>
/// This is what lets a WebAssembly app lazy load Bit.Bcrdt: the assembly is not there yet when <c>Program.cs</c> runs,
/// so nothing can be registered. That only happens on WebAssembly - everywhere else Bit.Bcrdt is loaded with the
/// application - so everywhere else a missing registration is a mistake, and fails as it always did. The choice is made
/// once for every service rather than per service: without a registered <see cref="IBcrdtCollaborationService"/>, a
/// registered transport, store or options are not used either, so a partial registration never mixes the
/// application's services with the ones configured here.
/// </para>
/// <para>
/// Nothing is created without a configuration. A component that asks before anything configured the service fails,
/// naming both ways to provide one, rather than collaborating on a purely local document that is neither synchronized
/// nor saved - a failure nobody could explain from what the page shows. Asking again once a configuration has been
/// given succeeds, so the mistake is never stuck for the rest of the session.
/// </para>
/// <para>
/// Nothing disposes a service created here, which is no different from the registered one: a WebAssembly runtime, and
/// so its one scope, lives as long as the tab.
/// </para>
/// </remarks>
internal static class BcrdtOwnCollaboration
{
    // Keyed weakly by the runtime's IJSRuntime - on WebAssembly there is one for the life of the tab - so that every
    // runtime in a process, as in the tests, has its own service.
    private static readonly ConditionalWeakTable<IJSRuntime, IBcrdtCollaborationService> _own = new();

    // Creating under a lock, rather than adding whichever of two racing callers finishes first, is what applies exactly
    // one configuration: the loser's would otherwise be dropped, and the transport and store it built with it leaked.
    private static readonly object _gate = new();

    /// <summary>
    /// Whether <paramref name="services"/> belong to a WebAssembly runtime: the one hosting model Bit.Bcrdt can be lazy
    /// loaded in, and so the one where its services may be configured rather than registered.
    /// </summary>
    /// <remarks>
    /// Told by the JavaScript runtime, which is in-process only there: Blazor Server's and Blazor Hybrid's reach the
    /// browser over a connection, and a prerendering server has none that works.
    /// </remarks>
    public static bool IsWebAssembly(IServiceProvider services) => services.GetService<IJSRuntime>() is IJSInProcessRuntime;

    /// <summary>The service of the WebAssembly runtime that <paramref name="services"/> belong to.</summary>
    /// <param name="services">The services of the caller, which do not include an <see cref="IBcrdtCollaborationService"/>.</param>
    /// <param name="configure">Configures the service if this call creates it; null to get the one already created.</param>
    public static IBcrdtCollaborationService Of(IServiceProvider services, Action<BcrdtCollaborationBuilder>? configure)
    {
        if (services.GetService<IJSRuntime>() is not IJSInProcessRuntime js) throw new InvalidOperationException(
            "Bit.Bcrdt's services are not registered: call builder.Services.AddBitBcrdtServices(...) in Program.cs - in a Blazor Web App, in both the server and the client project. " +
            "Only a WebAssembly app that lazy loads Bit.Bcrdt goes without, and configures the collaboration service with IServiceProvider.GetBcrdtCollaborationService(collaboration => ...) instead.");

        lock (_gate)
        {
            if (_own.TryGetValue(js, out IBcrdtCollaborationService? own)) return own;

            if (configure is null) throw new InvalidOperationException(
                "Bit.Bcrdt's collaboration service is neither registered nor configured. Call builder.Services.AddBitBcrdtServices(...) in Program.cs - in a Blazor Web App, in both the server and the client project. " +
                "An app that lazy loads Bit.Bcrdt cannot, and configures the service with IServiceProvider.GetBcrdtCollaborationService(collaboration => ...) before anything uses it: " +
                "in OnInitialized of the page or layout that holds the collaborative components, which runs before theirs - not after an await. " +
                "GetBcrdtCollaborationService(_ => { }) configures a purely local one, with no transport and no store.");

            // A container rather than the service alone, so the builder works exactly as it does for AddBitBcrdtServices -
            // UseIndexedDbStore included, which takes the runtime's IJSRuntime from it. One per tab, so its cost is paid once.
            ServiceCollection collection = new();
            collection.AddSingleton<IJSRuntime>(js);
            collection.AddBitBcrdtServices(configure);
            own = collection.BuildServiceProvider().GetRequiredService<IBcrdtCollaborationService>();

            _own.Add(js, own);
            return own;
        }
    }
}
