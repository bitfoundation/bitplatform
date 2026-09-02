using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wraps the <see href="https://developer.mozilla.org/en-US/docs/Web/API/EditContext_API">EditContext API</see>:
/// text input, IME composition and the thing on screen become three separate concerns instead of one
/// <c>contenteditable</c> element that is all of them at once.
/// </summary>
/// <remarks>
/// <b>Named <c>TextEditContext</c> on purpose.</b> Blazor's own
/// <c>Microsoft.AspNetCore.Components.Forms.EditContext</c> is in every component's implicit usings,
/// so a Butil type called <c>EditContext</c> would make the name ambiguous in every razor file that
/// touches forms. The browser type it wraps is still <c>EditContext</c>.
/// <br/>
/// <b>What it is for:</b> a code editor, a rich-text surface or a canvas-drawn document that needs
/// real text input - IME composition, dictation, autocorrect - without giving the browser control of
/// the DOM it edits. The element you attach to stops being edited directly; you receive
/// <c>textupdate</c> events, keep your own model, and render whatever you like.
/// <br/>
/// <b>What you take on:</b> everything the browser was doing for you. Caret drawing, selection
/// painting, and the IME's own underlines (see <see cref="TextEditFormat"/>) become your job, as does
/// telling the platform where the surface is (<see cref="TextEditContextHandle.UpdateControlBounds"/>)
/// so the candidate window lands in the right place. This is not a drop-in replacement for
/// <c>contenteditable</c> - it is the API you reach for when <c>contenteditable</c> has already
/// failed you.
/// <br/>
/// Chromium only. Where <see cref="IsSupported"/> is false, <see cref="Attach"/> returns null.
/// </remarks>
[ButilService(typeof(TextEditContext))]
public class TextEditContext(IJSRuntime js) : IAsyncDisposable
{
    internal const string TextMethodName = nameof(InvokeTextUpdate);
    internal const string CompositionMethodName = nameof(InvokeComposition);
    internal const string FormatMethodName = nameof(InvokeTextFormat);

    private readonly ConcurrentDictionary<Guid, Handlers> _handlers = new();

    // Per-instance callback reference (see Keyboard): contexts are isolated per circuit / WASM app
    // and released on disposal - no static state, no cross-circuit leak.
    private DotNetObjectReference<TextEditContext>? _dotNetRef;
    private DotNetObjectReference<TextEditContext> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>True when the runtime exposes <c>EditContext</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.editContext.isSupported");

    /// <summary>
    /// Invoked from JS on each text update. Public + <see cref="JSInvokableAttribute"/> so it can be
    /// dispatched through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(TextMethodName)]
    public void InvokeTextUpdate(Guid id, TextEditContextUpdate update)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnTextUpdate.Invoke(update ?? new TextEditContextUpdate());
    }

    /// <summary>
    /// Invoked from JS when an IME composition starts (true) or ends (false). Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(CompositionMethodName)]
    public void InvokeComposition(Guid id, bool composing)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnComposition?.Invoke(composing);
    }

    /// <summary>
    /// Invoked from JS with the ranges the input method wants decorated. Public +
    /// <see cref="JSInvokableAttribute"/> so it can be dispatched through the per-instance
    /// <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(FormatMethodName)]
    public void InvokeTextFormat(Guid id, TextEditFormat[] formats)
    {
        if (_handlers.TryGetValue(id, out var handlers)) handlers.OnFormatUpdate?.Invoke(formats ?? []);
    }

    /// <summary>
    /// Attaches an edit context to an element, taking over its text input.
    /// </summary>
    /// <param name="element">
    /// The editing surface. It does not need to be <c>contenteditable</c> - it does need to be
    /// focusable, so give it a <c>tabindex</c>.
    /// </param>
    /// <param name="onTextUpdate">
    /// Called for every text change the input method makes - typing, IME composition, dictation,
    /// autocorrect. Apply it to your own model and re-render. Called on the interop dispatch, so a
    /// Blazor component has to <c>StateHasChanged</c> itself.
    /// </param>
    /// <param name="options">The text the surface already shows and where the caret is. Optional.</param>
    /// <param name="onComposition">Optional. True when an IME composition starts, false when it ends.</param>
    /// <param name="onFormatUpdate">
    /// Optional, but expected of a real editor: the underlines the IME wants drawn under the text
    /// being composed. See <see cref="TextEditFormat"/>.
    /// </param>
    /// <returns>
    /// The handle, or null when the runtime has no <c>EditContext</c>. <b>Dispose it</b> when the
    /// editor goes away.
    /// </returns>
    [DynamicDependency(nameof(InvokeTextUpdate), typeof(TextEditContext))]
    [DynamicDependency(nameof(InvokeComposition), typeof(TextEditContext))]
    [DynamicDependency(nameof(InvokeTextFormat), typeof(TextEditContext))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TextEditContextOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TextEditContextUpdate))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TextEditFormat))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(TextEditSelection))]
    public async ValueTask<TextEditContextHandle?> Attach(
        ElementReference element,
        Action<TextEditContextUpdate> onTextUpdate,
        TextEditContextOptions? options = null,
        Action<bool>? onComposition = null,
        Action<TextEditFormat[]>? onFormatUpdate = null)
    {
        ArgumentNullException.ThrowIfNull(onTextUpdate);

        var id = Guid.NewGuid();
        _handlers[id] = new Handlers(onTextUpdate, onComposition, onFormatUpdate);

        var attached = await js.Invoke<bool>("BitButil.editContext.attach",
            element, id, options ?? new TextEditContextOptions(), DotNetRef,
            TextMethodName, CompositionMethodName, FormatMethodName);

        if (attached is false)
        {
            _handlers.TryRemove(id, out _);
            return null;
        }

        return new TextEditContextHandle(js, id, async () =>
        {
            _handlers.TryRemove(id, out _);
            await js.InvokeVoid("BitButil.editContext.detach", id);
        });
    }

    /// <summary>Detaches every context attached through this instance and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            var ids = _handlers.Keys.ToArray();
            _handlers.Clear();
            foreach (var id in ids) await js.InvokeVoid("BitButil.editContext.detach", id);
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
        GC.SuppressFinalize(this);
    }

    private sealed record Handlers(
        Action<TextEditContextUpdate> OnTextUpdate,
        Action<bool>? OnComposition,
        Action<TextEditFormat[]>? OnFormatUpdate);
}
