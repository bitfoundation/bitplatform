using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Live handle to the always-on-top window opened by
/// <see cref="DocumentPictureInPicture.RequestWindow"/>. Dispose it to close the window and put
/// everything that was moved into it back where it came from.
/// </summary>
/// <remarks>
/// The window is a real, separate document. Elements moved into it with <see cref="MoveElement"/>
/// keep their identity - so a Blazor component moved out of the page carries on being the same
/// component, with its state and its event handlers - but they lose the page's CSS unless the
/// stylesheets were copied along (see <see cref="DocumentPictureInPictureOptions.CopyStyleSheets"/>).
/// </remarks>
public sealed class DocumentPictureInPictureWindowHandle : IAsyncDisposable
{
    internal const string CloseMethodName = nameof(InvokeDocumentPictureInPictureClose);

    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private readonly Action? _onClose;
    private DotNetObjectReference<DocumentPictureInPictureWindowHandle>? _dotNetRef;
    private bool _disposed;

    internal DocumentPictureInPictureWindowHandle(IJSRuntime js, Guid id, Action? onClose)
    {
        _js = js;
        _id = id;
        _onClose = onClose;
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    internal DotNetObjectReference<DocumentPictureInPictureWindowHandle>? CallbackRef => _dotNetRef;

    internal void Initialize(DocumentPictureInPictureSize? size)
    {
        if (size is null) return;   // prerender/SSR: no JS runtime ran, so there's nothing to record

        InitialWidth = size.Width;
        InitialHeight = size.Height;
    }

    /// <summary>The internal window id.</summary>
    public Guid Id => _id;

    /// <summary>The window's inner width when it opened. Read <see cref="GetSize"/> for the current one.</summary>
    public int InitialWidth { get; private set; }

    /// <summary>The window's inner height when it opened.</summary>
    public int InitialHeight { get; private set; }

    /// <summary>
    /// Moves an element out of the page and into the floating window.
    /// </summary>
    /// <param name="element">The element to move. Its original position is remembered so it can be put back.</param>
    /// <returns>False when the window is gone, or the element could not be moved.</returns>
    /// <remarks>
    /// Moving is not copying: the element leaves the page, so leave a placeholder where it was if the
    /// layout would otherwise collapse. It comes back on <see cref="RestoreElements"/>, on disposal,
    /// and when the user closes the window themselves.
    /// <br/>
    /// Blazor keeps updating a moved element, since the renderer holds the same node - but a
    /// re-render that replaces the element's <em>parent</em> markup can leave the moved node behind.
    /// Move a stable container rather than something a parent component re-renders around.
    /// </remarks>
    public ValueTask<bool> MoveElement(ElementReference element)
        => _js.Invoke<bool>("BitButil.documentPictureInPicture.moveElement", _id, element);

    /// <summary>
    /// Puts everything that was moved back where it came from, without closing the window.
    /// </summary>
    /// <remarks>
    /// Happens automatically on disposal and when the user closes the window, so this is only needed
    /// to hand content back to the page while keeping the window open.
    /// </remarks>
    public ValueTask RestoreElements() => _js.InvokeVoid("BitButil.documentPictureInPicture.restoreElements", _id);

    /// <summary>
    /// Adds a stylesheet to the floating window's document.
    /// </summary>
    /// <param name="css">The CSS text to add.</param>
    /// <returns>False when the window is gone.</returns>
    /// <remarks>
    /// For the styles that only apply in the floating window - a compact layout, a hidden chrome -
    /// on top of whatever <see cref="DocumentPictureInPictureOptions.CopyStyleSheets"/> brought over.
    /// </remarks>
    public ValueTask<bool> AddStyleSheet(string css)
        => _js.Invoke<bool>("BitButil.documentPictureInPicture.addStyleSheet", _id, css);

    /// <summary>The window's current inner size, or <c>null</c> when it is gone.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DocumentPictureInPictureSize))]
    public ValueTask<DocumentPictureInPictureSize?> GetSize()
        => _js.Invoke<DocumentPictureInPictureSize?>("BitButil.documentPictureInPicture.size", _id);

    /// <summary>True while the floating window is still open.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsOpen() => _js.Invoke<bool>("BitButil.documentPictureInPicture.isOpen", _id);

    /// <summary>Brings the floating window to the front.</summary>
    /// <returns>False when the window is gone.</returns>
    public ValueTask<bool> Focus() => _js.Invoke<bool>("BitButil.documentPictureInPicture.focus", _id);

    /// <summary>
    /// Invoked from JS when the window closes, however that happened. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(CloseMethodName)]
    public void InvokeDocumentPictureInPictureClose(Guid id)
    {
        if (id != _id) return;

        try
        {
            _onClose?.Invoke();
        }
        finally
        {
            // The window is gone and JS has dropped its entry, so nothing will dispatch here again -
            // and a handle the user never disposes would otherwise hold the reference for the
            // lifetime of the circuit.
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }

    /// <summary>
    /// Restores everything that was moved and closes the window. Calling it again does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.documentPictureInPicture.close", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }
}
