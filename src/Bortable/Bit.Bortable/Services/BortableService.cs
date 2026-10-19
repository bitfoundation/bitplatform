using Microsoft.JSInterop;

namespace Bit.Bortable;

/// <summary>
/// The per-scope coordinator that holds the active drag, so that several <see cref="Bortable{TItem}"/>
/// lists in the same group can exchange items. Scoped, so every Blazor Server circuit has a drag state
/// of its own. Registered by <see cref="BitBortable.AddBitBortableServices"/>.
/// </summary>
internal sealed class BortableService(IJSRuntime js) : IAsyncDisposable, IDisposable
{
    private const string ModulePath = "./_content/Bit.Bortable/bit-bortable.js";

    // The import of bit-bortable.js, shared by every list of the scope: the module keeps no per-list state.
    private Task<IJSObjectReference>? _import;

    internal BortableDragContext? Context { get; set; }

    /// <summary>
    /// Serializes the drag handlers of all lists. Each handler awaits JS interop, so without it a
    /// <c>dragover</c> could interleave with another one (or with the <c>drop</c>/<c>dragend</c> that
    /// finishes the drag) and act on stale indices. Never held while the app's own callbacks run.
    /// </summary>
    internal SemaphoreSlim Gate { get; } = new(1, 1);

    /// <summary>
    /// Every list of the scope that is on the page. A drag that starts or ends changes which of them take
    /// its <c>dragover</c> events, so each of them is told (see <see cref="IBortableZone.SyncDragState"/>).
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
