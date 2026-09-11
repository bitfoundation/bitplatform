using System;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// The three connection callbacks an <see cref="IndexedDbHandle"/> can be opened with, and the object
/// JavaScript dispatches them through.
/// </summary>
/// <remarks>
/// <see cref="DotNetObjectReference.Create{TValue}(TValue)"/> annotates its argument with
/// <c>PublicMethods</c>, so handing JavaScript the handle itself preserved every public method the handle
/// has - and with them every <c>"BitButil.indexedDb*.&lt;function&gt;"</c> literal in it. A trimmed app that
/// only reads a record would still download the index queries, the cursor walks, the metadata reads and
/// the transaction batcher, because the identifiers naming those modules survived on methods it never
/// calls. Handing over this relay keeps that preservation to the three callbacks and nothing else.
/// <br/>
/// The <c>[JSInvokable]</c> identifiers are the names <c>indexedDb.ts</c> dispatches by, so they stay
/// what they were when these moved off the handle.
/// </remarks>
internal sealed class IndexedDbCallbacksInterop(Action? onVersionChange, Action? onClose, Action? onBlocked) : IDisposable
{
    internal const string VersionChangeMethodName = "InvokeIndexedDbVersionChange";
    internal const string CloseMethodName = "InvokeIndexedDbClose";
    internal const string BlockedMethodName = "InvokeIndexedDbBlocked";

    private DotNetObjectReference<IndexedDbCallbacksInterop>? _dotNetRef;

    internal DotNetObjectReference<IndexedDbCallbacksInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>Invoked from JS when another tab requests a version change.</summary>
    [JSInvokable(VersionChangeMethodName)]
    public void InvokeIndexedDbVersionChange(Guid id) => onVersionChange?.Invoke();

    /// <summary>Invoked from JS when the connection closes unexpectedly.</summary>
    [JSInvokable(CloseMethodName)]
    public void InvokeIndexedDbClose(Guid id) => onClose?.Invoke();

    /// <summary>Invoked from JS when an open is blocked by another connection.</summary>
    [JSInvokable(BlockedMethodName)]
    public void InvokeIndexedDbBlocked(Guid id) => onBlocked?.Invoke();

    public void Dispose()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
