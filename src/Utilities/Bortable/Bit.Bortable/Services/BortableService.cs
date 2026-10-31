using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Bit.Bortable;

/// <summary>
/// The per-scope coordinator that holds the active drag, so that several <see cref="Bortable{TItem}"/>
/// lists in the same group can exchange items. Scoped, so every Blazor Server circuit has a drag state
/// of its own. Registered by <see cref="BitBortable.AddBitBortableServices(IServiceCollection)"/>; where it
/// is not - an app that lazy loads Bit.Bortable on WebAssembly - the lists create one per WebAssembly
/// runtime (see <see cref="Of"/>).
/// </summary>
internal sealed partial class BortableService(IJSRuntime js, IEnumerable<BortableConfiguration> configurations) : IAsyncDisposable, IDisposable
{
    private const string ModulePath = "./_content/Bit.Bortable/bit-bortable.js";

    // One coordinator per WebAssembly runtime for applications that lazy load Bit.Bortable and so never call
    // AddBitBortableServices. Keyed weakly by the runtime's IJSRuntime - on WebAssembly there is one for the life of the
    // tab - so that every runtime in a process, as in the tests, has its own. Never disposed, and nothing needs it to be:
    // it lives as long as the tab, like the runtime's one scope, and the one thing disposal would add, releasing the
    // module reference held in the browser, has no page left to do it in by then.
    private static readonly ConditionalWeakTable<IJSRuntime, BortableService> OwnCoordinators = new();

    /// <summary>
    /// The coordinator of the circuit or WebAssembly runtime that <paramref name="services"/> belong to: the one
    /// <see cref="BitBortable.AddBitBortableServices(IServiceCollection)"/> registered, otherwise - on WebAssembly only -
    /// one the lists create themselves, once per runtime, with no app-wide defaults.
    /// </summary>
    /// <remarks>
    /// The second case is what lets a WebAssembly app lazy load Bit.Bortable: the assembly is not there yet when
    /// <c>Program.cs</c> runs, so nothing can be registered. Only WebAssembly can lazy load it - everywhere else it is
    /// loaded with the application - so everywhere else a missing registration is a mistake, reported where it happens.
    /// </remarks>
    /// <param name="services">The services of the list asking, as injected through <see cref="IServiceProvider"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// The services are not registered, and <paramref name="services"/> do not belong to a WebAssembly runtime.
    /// </exception>
    internal static BortableService Of(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.GetService<BortableService>() is { } registered) return registered;

        // The JS runtime tells WebAssembly apart: only there is it in-process; Blazor Server's and Blazor Hybrid's reach
        // the browser over a connection, and a prerendering server has none that works.
        if (services.GetService<IJSRuntime>() is not IJSInProcessRuntime jsRuntime) throw new InvalidOperationException(
            "Bit.Bortable's services are not registered: call builder.Services.AddBitBortableServices() in Program.cs - in a Blazor Web App, in both the server and the client project. " +
            "Only a WebAssembly app that lazy loads Bit.Bortable goes without.");

        if (OwnCoordinators.TryGetValue(jsRuntime, out var own)) return own;

        // Two lists that get here together end up with the same coordinator - the first one added - and only that one
        // warns: once per runtime, which on WebAssembly is once per app.
        var created = new BortableService(jsRuntime, []);
        own = OwnCoordinators.GetOrAdd(jsRuntime, created);
        if (ReferenceEquals(own, created) && services.GetService<ILoggerFactory>() is { } loggers)
            LogServicesNotRegistered(loggers.CreateLogger(typeof(BitBortable).FullName!));
        return own;
    }

    [LoggerMessage(1, LogLevel.Warning, "Bit.Bortable's services are not registered, so the lists create their own and take none of the app-wide defaults (BortableOptions). Call builder.Services.AddBitBortableServices() in Program.cs - in a Blazor Web App, in both the server and the client project. An app that lazy loads Bit.Bortable on WebAssembly cannot, and can ignore this warning.")]
    private static partial void LogServicesNotRegistered(ILogger logger);

    private BortableOptions? _options;

    /// <summary>The app-wide defaults (<see cref="BitBortable.AddBitBortableServices(IServiceCollection, Action{BortableOptions})"/>), built once per scope.</summary>
    internal BortableOptions Options => _options ??= Build(configurations);

    private static BortableOptions Build(IEnumerable<BortableConfiguration> configurations)
    {
        var options = new BortableOptions();
        foreach (var configuration in configurations) configuration.Apply(options);
        return options;
    }

    // The import of bit-bortable.js, shared by every list of the scope: the module keeps no per-list state.
    private Task<IJSObjectReference>? _import;

    private long _lastKey;

    internal BortableDragContext? Context { get; set; }

    /// <summary>
    /// How many finished drags are waiting for a destination's <c>ConfirmDrop</c>. Until they are answered no other
    /// drag starts: one that changed the same lists would be undone along with the refused one.
    /// </summary>
    internal int Confirming { get; set; }

    /// <summary>
    /// A key for an item, unique across every list of the scope: the browser layer names an item by it, so it never
    /// depends on an index the page may have moved on from.
    /// </summary>
    internal string NextKey() => _keyPrefix + (++_lastKey).ToString(System.Globalization.CultureInfo.InvariantCulture);

    // Tells this scope's keys from another's on the same page (a Server island next to a WebAssembly one): both count
    // from 1, and the browser layer looks an item up by its key across every list it knows.
    private readonly string _keyPrefix = Guid.NewGuid().ToString("N")[..8] + "-";

    private int _lastDrag;

    /// <summary>The id of the next drag (see <see cref="BortableDragContext.Id"/>); never 0, which means "the drag in progress".</summary>
    internal int NextDragId() => ++_lastDrag;

    /// <summary>
    /// The drag in progress, provided it is the one <paramref name="drag"/> names (0: whichever is in progress): a call
    /// about a drag that has since ended finds none.
    /// </summary>
    internal BortableDragContext? ContextOf(int drag)
        => Context is { Finished: false } ctx && (drag == 0 || ctx.Id == drag) ? ctx : null;

    /// <summary>
    /// Serializes the drag handlers of all lists, so a drop position, the end of a drag and a list leaving the
    /// page mid-drag never act on each other's half-made changes. Never held while the app's own callbacks run.
    /// </summary>
    internal SemaphoreSlim Gate { get; } = new(1, 1);

    /// <summary>
    /// Every list of the scope that is on the page. When a drag starts, each of them is asked whether it takes the
    /// drag, and the browser layer routes the pointer to those that do.
    /// </summary>
    internal HashSet<IBortableZone> Zones { get; } = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// The <c>bit-bortable.js</c> module, imported by the first list that asks for it. <c>null</c> when the
    /// browser is gone (a disconnected circuit), in which case a later list tries again.
    /// </summary>
    internal async Task<IJSObjectReference?> GetModuleAsync()
    {
        var import = _import ??= js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        try
        {
            return await import;
        }
        catch (Exception e) when (e is JSDisconnectedException or TaskCanceledException)
        {
            if (ReferenceEquals(_import, import)) _import = null;
            return null;
        }
    }

    /// <summary>Releases the shared module.</summary>
    public async ValueTask DisposeAsync()
    {
        var import = _import;
        _import = null;
        if (import is not { IsCompletedSuccessfully: true }) return;
        try { await import.Result.DisposeAsync(); }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (TaskCanceledException) { }
    }

    /// <summary>
    /// For a container disposed synchronously, which cannot wait for the browser: the module is only let go
    /// of, and the browser releases it with the page.
    /// </summary>
    public void Dispose() => _import = null;
}
