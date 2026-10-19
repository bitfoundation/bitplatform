using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bdnd;

/// <summary>
/// Thin wrapper around the <c>bit-bdnd.js</c> module. Registered as a scoped
/// service by <c>AddBitBdndServices()</c> and used by the drag/drop components.
/// </summary>
public sealed class BdndInterop : IAsyncDisposable
{
    /// <summary>
    /// Prefix of the <c>DataTransfer</c> type that marks drags started by a
    /// <see cref="BdndDraggable{TItem}"/>; the draggable's handler id follows it.
    /// </summary>
    internal const string SourceTypePrefix = "application/x-bit-bdnd-";

    private const string ModulePath = "./_content/Bit.Bdnd/bit-bdnd.js";

    private readonly IJSRuntime _js;
    private Task<IJSObjectReference>? _module;

    public BdndInterop(IJSRuntime js) => _js = js;

    internal static string SourceType(string handlerId) => SourceTypePrefix + handlerId;

    /// <summary>
    /// Reads an element's bounding client rectangle, e.g. to compare the pointer against a hovered
    /// item's midpoint in a sortable list. Returns null when the element is not rendered.
    /// </summary>
    public async ValueTask<BdndDomRect?> GetBoundingClientRectAsync(ElementReference element)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<BdndDomRect?>("getBoundingClientRect", element);
    }

    internal async ValueTask<IJSObjectReference?> RegisterDragSourceAsync(
        ElementReference element, object dotNetRef, BdndDragSourceOptions options)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<IJSObjectReference?>(
            "registerDragSource", element, dotNetRef, options);
    }

    internal async ValueTask<IJSObjectReference?> RegisterDropTargetAsync(
        ElementReference element, object dotNetRef, BdndDropTargetOptions options)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<IJSObjectReference?>(
            "registerDropTarget", element, dotNetRef, options);
    }

    // A failed import is retried by the next call instead of being cached for the whole scope.
    private Task<IJSObjectReference> GetModuleAsync()
    {
        if (_module is null || _module.IsFaulted || _module.IsCanceled)
            _module = _js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        return _module;
    }

    public async ValueTask DisposeAsync()
    {
        var module = _module;
        _module = null;
        if (module is null) return;
        try
        {
            await (await module).DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException
                                       or OperationCanceledException or InvalidOperationException) { }
    }
}
