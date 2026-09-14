using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// An edit context attached to an element, returned by <see cref="TextEditContext.Attach"/>.
/// </summary>
/// <remarks>
/// <b>Always dispose.</b> While a context is attached the element no longer edits itself, so a
/// context that outlives its editor leaves the element swallowing every keystroke.
/// </remarks>
public sealed class TextEditContextHandle : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Func<ValueTask> _release;
    private bool _disposed;

    internal TextEditContextHandle(IJSRuntime js, Guid id, Func<ValueTask> release)
    {
        _js = js;
        Id = id;
        _release = release;
    }

    /// <summary>The internal context id.</summary>
    public Guid Id { get; }

    /// <summary>The context's current text, or null once detached.</summary>
    public ValueTask<string?> GetText() => _js.Invoke<string?>("BitButil.editContext.getText", Id);

    /// <summary>Where the caret or selection is, or null once detached.</summary>
    public ValueTask<TextEditSelection?> GetSelection() => _js.Invoke<TextEditSelection?>("BitButil.editContext.getSelection", Id);

    /// <summary>
    /// Tells the input method about a text change your own code made - a paste, an undo, a
    /// programmatic edit.
    /// </summary>
    /// <param name="rangeStart">Start of the range being replaced, in the context's text.</param>
    /// <param name="rangeEnd">End of the range being replaced, exclusive.</param>
    /// <param name="text">What replaces it.</param>
    /// <remarks>
    /// Skipping this is the classic edit-context bug: the IME keeps composing against a buffer that
    /// no longer matches what the user sees.
    /// </remarks>
    public ValueTask UpdateText(int rangeStart, int rangeEnd, string text)
        => _js.InvokeVoid("BitButil.editContext.updateText", Id, rangeStart, rangeEnd, text);

    /// <summary>Tells the input method where the caret or selection moved to.</summary>
    public ValueTask UpdateSelection(int start, int end)
        => _js.InvokeVoid("BitButil.editContext.updateSelection", Id, start, end);

    /// <summary>
    /// Tells the platform where the whole editing surface is on screen, in CSS pixels relative to
    /// the viewport.
    /// </summary>
    /// <remarks>
    /// Used to place the IME's candidate window. Without it the platform guesses, and the candidate
    /// list can land nowhere near the text being typed.
    /// </remarks>
    public ValueTask UpdateControlBounds(double x, double y, double width, double height)
        => _js.InvokeVoid("BitButil.editContext.updateControlBounds", Id, x, y, width, height);

    /// <summary>
    /// Tells the platform where the selection is on screen, in CSS pixels relative to the viewport -
    /// the finer-grained companion to <see cref="UpdateControlBounds"/>.
    /// </summary>
    public ValueTask UpdateSelectionBounds(double x, double y, double width, double height)
        => _js.InvokeVoid("BitButil.editContext.updateSelectionBounds", Id, x, y, width, height);

    /// <summary>
    /// Detaches the context, handing the element back to the DOM's own editing. Calling it again
    /// does nothing.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try { await _release(); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed

        GC.SuppressFinalize(this);
    }
}
