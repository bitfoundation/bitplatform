using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Balette;

/// <summary>
/// The services a palette and its trigger work with: the ones <see cref="BitBalette.AddBitBaletteServices"/> registered,
/// otherwise ones Balette creates itself, once per browser connection.
/// </summary>
/// <remarks>
/// The second case is what lets a WebAssembly app lazy load Bit.Balette: the assembly is not there yet when
/// <c>Program.cs</c> runs, so nothing can be registered. The choice is made once for every service rather than per
/// service, so a partial registration never mixes the application's services with Balette's defaults; a registered
/// <see cref="BaletteOptions"/> and the application's logging are still honoured. The application's own services - the
/// ones a handler or a source reaches through its context, and the sources registered with
/// <see cref="BitBalette.AddBitBaletteSource"/> - still come from the application's container.
/// </remarks>
internal sealed class BaletteRuntime
{
    // Keyed weakly by the connection's IJSRuntime, so a closed Blazor Server circuit takes its services with it. Plain
    // objects rather than a service provider: nothing in them is disposable, and nothing outlives the connection.
    private static readonly ConditionalWeakTable<IJSRuntime, BaletteRuntime> _own = new();

    // The logger factories already told that the services are not registered: one warning per application rather than
    // one per circuit.
    private static readonly ConditionalWeakTable<ILoggerFactory, object?> _warned = new();

    private BaletteRuntime(BaletteService service, BaletteOptions options, BaletteRanker ranker, BaletteHistory history)
    {
        Service = service;
        Options = options;
        Ranker = ranker;
        History = history;
    }

    public BaletteService Service { get; }

    public BaletteOptions Options { get; }

    public BaletteRanker Ranker { get; }

    public BaletteHistory History { get; }

    /// <summary>The services of the circuit or WebAssembly runtime that <paramref name="services"/> belongs to.</summary>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    public static BaletteRuntime Of(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.GetService<BaletteService>() is { } service)
        {
            return new(service,
                       services.GetRequiredService<BaletteOptions>(),
                       services.GetRequiredService<BaletteRanker>(),
                       services.GetRequiredService<BaletteHistory>());
        }

        var js = services.GetService<IJSRuntime>() ?? throw new InvalidOperationException(
            "Bit.Balette's services are not registered: call builder.Services.AddBitBaletteServices() in Program.cs. " +
            "Without them Balette creates its own for each browser connection, which needs the IJSRuntime of a Blazor app, and this container has none.");

        return _own.TryGetValue(js, out var own) ? own : Create(services, js);
    }

    private static BaletteRuntime Create(IServiceProvider services, IJSRuntime js)
    {
        if (services.GetService<ILoggerFactory>() is { } loggers && _warned.TryAdd(loggers, null))
        {
            BaletteLog.ServicesNotRegistered(loggers.CreateLogger(typeof(BitBalette).FullName!));
        }

        var options = services.GetService<BaletteOptions>() ?? new BaletteOptions();
        var history = new BaletteHistory(new BaletteBrowserStorageHistoryStore(js, options), options, TimeProvider.System, services.GetService<ILogger<BaletteHistory>>());
        var created = new BaletteRuntime(new BaletteService(options, services.GetService<ILogger<BaletteService>>()), options, new BaletteRanker(options), history);

        // Two components of one connection that get here together end up with the same services: the first one added.
        return _own.GetOrAdd(js, created);
    }
}
