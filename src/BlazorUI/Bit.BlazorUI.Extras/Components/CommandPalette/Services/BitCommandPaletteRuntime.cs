using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

/// <summary>
/// The services a palette and its trigger work with: the ones <see cref="BitCommandPaletteServiceCollectionExtensions.AddBitCommandPaletteServices"/> registered,
/// otherwise ones BitCommandPalette creates itself, once per browser connection.
/// </summary>
/// <remarks>
/// The second case is what lets a WebAssembly app lazy load Bit.BlazorUI.Extras: the assembly is not there yet when
/// <c>Program.cs</c> runs, so nothing can be registered. The choice is made once for every service rather than per
/// service, so a partial registration never mixes the application's services with BitCommandPalette's defaults; a registered
/// <see cref="BitCommandPaletteOptions"/> and the application's logging are still honoured. The application's own services - the
/// ones a handler or a source reaches through its context, and the sources registered with
/// <see cref="BitCommandPaletteServiceCollectionExtensions.AddBitCommandPaletteSource"/> - still come from the application's container.
/// </remarks>
internal sealed class BitCommandPaletteRuntime
{
    // Keyed weakly by the connection's IJSRuntime, so a closed Blazor Server circuit takes its services with it. Plain
    // objects rather than a service provider: nothing in them is disposable, and nothing outlives the connection.
    private static readonly ConditionalWeakTable<IJSRuntime, BitCommandPaletteRuntime> _own = new();

    // The logger factories already told that the services are not registered: one warning per application rather than
    // one per circuit.
    private static readonly ConditionalWeakTable<ILoggerFactory, object?> _warned = new();

    private BitCommandPaletteRuntime(BitCommandPaletteService service, BitCommandPaletteOptions options, BitCommandPaletteRanker ranker, BitCommandPaletteHistory history)
    {
        Service = service;
        Options = options;
        Ranker = ranker;
        History = history;
    }

    public BitCommandPaletteService Service { get; }

    public BitCommandPaletteOptions Options { get; }

    public BitCommandPaletteRanker Ranker { get; }

    public BitCommandPaletteHistory History { get; }

    /// <summary>The services of the circuit or WebAssembly runtime that <paramref name="services"/> belongs to.</summary>
    /// <param name="services">The services of the component asking, as injected through <see cref="IServiceProvider"/>.</param>
    public static BitCommandPaletteRuntime Of(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.GetService<BitCommandPaletteService>() is { } service)
        {
            return new(service,
                       services.GetRequiredService<BitCommandPaletteOptions>(),
                       services.GetRequiredService<BitCommandPaletteRanker>(),
                       services.GetRequiredService<BitCommandPaletteHistory>());
        }

        var js = services.GetService<IJSRuntime>() ?? throw new InvalidOperationException(
            "The command palette's services are not registered: call builder.Services.AddBitCommandPaletteServices() in Program.cs. " +
            "Without them BitCommandPalette creates its own for each browser connection, which needs the IJSRuntime of a Blazor app, and this container has none.");

        return _own.TryGetValue(js, out var own) ? own : Create(services, js);
    }

    private static BitCommandPaletteRuntime Create(IServiceProvider services, IJSRuntime js)
    {
        if (services.GetService<ILoggerFactory>() is { } loggers && _warned.TryAdd(loggers, null))
        {
            BitCommandPaletteLog.ServicesNotRegistered(loggers.CreateLogger(typeof(BitCommandPaletteServiceCollectionExtensions).FullName!));
        }

        var options = services.GetService<BitCommandPaletteOptions>() ?? new BitCommandPaletteOptions();
        var history = new BitCommandPaletteHistory(new BitCommandPaletteBrowserStorageHistoryStore(js, options), options, TimeProvider.System, services.GetService<ILogger<BitCommandPaletteHistory>>());
        var created = new BitCommandPaletteRuntime(new BitCommandPaletteService(options, services.GetService<ILogger<BitCommandPaletteService>>()), options, new BitCommandPaletteRanker(options), history);

        // Two components of one connection that get here together end up with the same services: the first one added.
        return _own.GetOrAdd(js, created);
    }
}
