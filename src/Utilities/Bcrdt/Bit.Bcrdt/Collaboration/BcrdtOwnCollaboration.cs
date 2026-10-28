using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Bcrdt;

/// <summary>
/// The collaboration service Bcrdt creates itself, once per browser connection, where
/// <see cref="BitBcrdt.AddBitBcrdtServices"/> registered none.
/// </summary>
/// <remarks>
/// <para>
/// This is what lets a WebAssembly app lazy load Bit.Bcrdt: the assembly is not there yet when <c>Program.cs</c> runs,
/// so nothing can be registered. The choice is made once for every service rather than per service: without a
/// registered <see cref="IBcrdtCollaborationService"/>, a registered transport, store or options are not used either,
/// so a partial registration never mixes the application's services with the ones configured here.
/// </para>
/// <para>
/// Nothing disposes a service created here. On WebAssembly that is no different from the registered one, which lives
/// as long as the application. On Blazor Server the documents a <see cref="Blazor.BcrdtCollaborationScope"/> opened are
/// closed with it when the circuit ends, but a document the application opened itself stays open until it closes it.
/// </para>
/// </remarks>
internal static partial class BcrdtOwnCollaboration
{
    // Keyed weakly by the connection's IJSRuntime, so a closed Blazor Server circuit takes its service with it.
    private static readonly ConditionalWeakTable<IJSRuntime, Own> _own = new();

    // The logger factories already told that the services are not registered: one warning per application rather than
    // one per circuit.
    private static readonly ConditionalWeakTable<ILoggerFactory, object?> _warned = new();

    /// <summary>The service of the browser connection that <paramref name="services"/> belongs to.</summary>
    /// <param name="services">The services of the caller, which do not include an <see cref="IBcrdtCollaborationService"/>.</param>
    /// <param name="configure">Configures the service if this call creates it; null for a purely local one.</param>
    public static IBcrdtCollaborationService Of(IServiceProvider services, Action<BcrdtCollaborationBuilder>? configure)
    {
        var js = services.GetService<IJSRuntime>() ?? throw new InvalidOperationException(
            "Bit.Bcrdt's services are not registered: call builder.Services.AddBitBcrdtServices() in Program.cs. " +
            "Without them Bcrdt creates its own collaboration service for each browser connection, which needs the IJSRuntime of a Blazor app, and this container has none.");

        if (_own.TryGetValue(js, out Own? own))
        {
            // A configuration that comes too late would be dropped without a trace, and documents opened since would
            // never sync or be saved - a failure nobody could explain from what the page shows.
            if (configure is not null && own.Configured is false) throw new InvalidOperationException(
                "The collaboration service of this browser connection was already created without a configuration - by a collaborative component, " +
                "or by a call to GetBcrdtCollaborationService without one - so it has no transport and no store. Configure it before anything uses it: " +
                "in OnInitialized of the page or layout that holds the collaborative components, which runs before theirs.");

            return own.Service;
        }

        // An application that configures the service lazy loads Bcrdt on purpose; one that does not most likely forgot
        // the registration, such as the client project of a Blazor Web App, and is told why nothing syncs.
        if (configure is null && services.GetService<ILoggerFactory>() is { } loggers && _warned.TryAdd(loggers, null))
        {
            ServicesNotRegistered(loggers.CreateLogger(typeof(BitBcrdt).FullName!));
        }

        // A container rather than the service alone, so the builder works exactly as it does for AddBitBcrdtServices -
        // UseIndexedDbStore included, which takes the connection's IJSRuntime from it.
        ServiceCollection collection = new();
        collection.AddSingleton(js);
        collection.AddBitBcrdtServices(configure);
        var created = new Own(collection.BuildServiceProvider().GetRequiredService<IBcrdtCollaborationService>(), configure is not null);

        // Two callers of one connection that get here together end up with the same service: the first one added.
        return _own.GetOrAdd(js, created).Service;
    }

    [LoggerMessage(1, LogLevel.Warning, "Bit.Bcrdt's services are not registered, so the collaborative components create their own collaboration service, with no transport and no store: documents are neither synchronized nor saved. Call builder.Services.AddBitBcrdtServices(...) in Program.cs - in a Blazor Web App, in both the server and the client project. An app that lazy loads Bit.Bcrdt on WebAssembly cannot, and configures the service with IServiceProvider.GetBcrdtCollaborationService(collaboration => ...) before the first collaborative component renders.")]
    private static partial void ServicesNotRegistered(ILogger logger);

    private sealed record Own(IBcrdtCollaborationService Service, bool Configured);
}
