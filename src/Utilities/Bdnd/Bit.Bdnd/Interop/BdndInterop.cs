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

    /// <summary>
    /// Moves a keyboard drag of the source behind <paramref name="registration"/> to another place among the
    /// <paramref name="candidates"/> - a target, or an edge of a target that has edges - in document order from where
    /// it is (<paramref name="currentId"/> and <paramref name="currentEdge"/>): <paramref name="command"/> is
    /// <c>next</c>, <c>previous</c>, <c>first</c>, <c>last</c> or <c>stay</c>. <paramref name="sourceTargets"/> are the
    /// targets around the source, by which the place the item already has is told apart. The target landed on is
    /// scrolled into view. Null when no candidate is in the document.
    /// </summary>
    internal async ValueTask<BdndKeyboardTarget?> KeyboardNavigateAsync(IJSObjectReference registration,
        BdndKeyboardCandidate[] candidates, string[] sourceTargets, string? currentId, string? currentEdge, string command)
        => await registration.InvokeAsync<BdndKeyboardTarget?>("keyboardNavigate", candidates, sourceTargets, currentId, currentEdge, command);

    /// <summary>
    /// A stream of the contents of file <paramref name="index"/> of native drop <paramref name="dropId"/>. Takes no
    /// cancellation token: a reference handed out after a cancellation would be lost, with the file it holds.
    /// </summary>
    internal async ValueTask<IJSStreamReference> ReadNativeFileAsync(string dropId, int index)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<IJSStreamReference>("readNativeFile", dropId, index);
    }

    /// <summary>The descriptions of the files of native drop <paramref name="dropId"/> from <paramref name="offset"/> on, one page.</summary>
    internal async ValueTask<BdndNativeFile[]> GetNativeDropFilesAsync(string dropId, int offset, int count)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<BdndNativeFile[]>("nativeDropFiles", dropId, offset, count);
    }

    /// <summary>Has screen readers read <paramref name="text"/> out, through the module's live region.</summary>
    internal async ValueTask AnnounceAsync(string text)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("announce", text);
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
