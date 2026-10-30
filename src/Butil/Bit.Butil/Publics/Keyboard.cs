using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Registers global or element-scoped keyboard shortcuts.
/// </summary>
/// <remarks>
/// All shortcuts are matched against the <c>keydown</c> event only. Key-up (<c>keyup</c>) and
/// key-press (<c>keypress</c>) are not observed, so a handler fires when the key combination goes
/// down, not when it is released. Matching is by the physical key (<c>KeyboardEvent.code</c>),
/// not the produced character.
/// <br/>
/// <c>preventDefault</c> and <c>stopPropagation</c> default to <see langword="true"/>, which suits a
/// shortcut that replaces the browser's own action (Ctrl+S, F5). A shortcut without Ctrl, Alt or Meta
/// stands aside, by default, when its key goes to a control that acts on that key itself - Delete in a
/// text field, an arrow on a slider, Space on a checkbox - so registering one does not take the key away
/// from every control on the page. <see cref="KeyboardShortcutOptions.IgnoreWhenEditable"/> changes that.
/// </remarks>
[ButilService(typeof(Keyboard))]
public class Keyboard(IJSRuntime js) : IAsyncDisposable
{
    internal const string InvokeMethodName = nameof(InvokeKeyboard);

    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    // One DotNetObjectReference per service instance. Because the listeners live on this (scoped)
    // instance instead of in static state, they are isolated per Blazor circuit / WASM app and are
    // released when the instance is disposed - no cross-circuit bleed and no leak when a circuit
    // drops without an explicit Remove. Created lazily so prerender/SSR (which never adds listeners)
    // doesn't allocate one.
    private DotNetObjectReference<Keyboard>? _dotNetRef;
    private DotNetObjectReference<Keyboard> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    /// <summary>
    /// Invoked from JS when a registered shortcut fires. Public + <see cref="JSInvokableAttribute"/>
    /// so it can be called through the per-instance <see cref="DotNetObjectReference{T}"/>.
    /// </summary>
    [JSInvokable(InvokeMethodName)]
    public void InvokeKeyboard(Guid id)
    {
        if (_handlers.TryGetValue(id, out var handler)) handler.Invoke();
    }

    /// <summary>
    /// Registers an app-wide shortcut and returns the id that removes it again. The match is on
    /// <c>KeyboardEvent.code</c> - a physical key position, so the shortcut lands under the same
    /// finger on every keyboard layout.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/code">KeyboardEvent.code</see>
    /// </summary>
    /// <param name="code">The code to match, e.g. <see cref="ButilKeyCodes.KeyS"/>.</param>
    /// <param name="handler">Called when the combination is pressed.</param>
    /// <param name="modifiers">The modifiers that must be down. Matched exactly, so extra modifiers do not fire it.</param>
    /// <param name="preventDefault">Suppress the browser's own action for the combination.</param>
    /// <param name="stopPropagation">Stop the keydown travelling any further.</param>
    /// <param name="repeat">Keep firing while the key is held, instead of once per press.</param>
    public Task<Guid> Add(string code, Action handler, ButilModifiers modifiers = ButilModifiers.None, bool preventDefault = true, bool stopPropagation = true, bool repeat = false)
        => Add(code, handler, Options(modifiers, preventDefault, stopPropagation, repeat));

    /// <summary>
    /// Registers an app-wide shortcut with the full set of <see cref="KeyboardShortcutOptions"/> and returns the id
    /// that removes it again.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/KeyboardEvent/code">KeyboardEvent.code</see>
    /// </summary>
    /// <param name="code">The code to match, e.g. <see cref="ButilKeyCodes.KeyS"/>.</param>
    /// <param name="handler">Called when the combination is pressed.</param>
    /// <param name="options">The modifiers to match and what the shortcut does to the key.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsKeyboardShortcut))]
    public async Task<Guid> Add(string code, Action handler, KeyboardShortcutOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.keyboard.add", DotNetRef, listenerId, JsKeyboardShortcut.From(code, options));

        return listenerId;
    }

    /// <summary>
    /// Same as <see cref="Add(string, Action, ButilModifiers, bool, bool, bool)"/> but returns an
    /// <see cref="IAsyncDisposable"/> handle that detaches the shortcut when disposed.
    /// </summary>
    public Task<ButilSubscription> Subscribe(string code, Action handler,
        ButilModifiers modifiers = ButilModifiers.None,
        bool preventDefault = true,
        bool stopPropagation = true,
        bool repeat = false)
        => Subscribe(code, handler, Options(modifiers, preventDefault, stopPropagation, repeat));

    /// <summary>
    /// Same as <see cref="Add(string, Action, KeyboardShortcutOptions)"/> but returns an
    /// <see cref="IAsyncDisposable"/> handle that detaches the shortcut when disposed.
    /// </summary>
    public async Task<ButilSubscription> Subscribe(string code, Action handler, KeyboardShortcutOptions options)
    {
        var id = await Add(code, handler, options);
        return new ButilSubscription(id, () => Remove(id));
    }

    /// <summary>
    /// Element-scoped variant of <see cref="Subscribe(string, Action, ButilModifiers, bool, bool, bool)"/>: the
    /// shortcut only fires while the given element (or one of its descendants) has focus or receives the keyboard event.
    /// </summary>
    public Task<ButilSubscription> SubscribeOn(Microsoft.AspNetCore.Components.ElementReference element,
        string code, Action handler,
        ButilModifiers modifiers = ButilModifiers.None,
        bool preventDefault = true,
        bool stopPropagation = true,
        bool repeat = false)
        => SubscribeOn(element, code, handler, Options(modifiers, preventDefault, stopPropagation, repeat));

    /// <summary>
    /// Element-scoped variant of <see cref="Subscribe(string, Action, KeyboardShortcutOptions)"/>: the shortcut only
    /// fires while the given element (or one of its descendants) has focus or receives the keyboard event. It never
    /// stands aside for the element itself under <see cref="KeyboardShortcutOptions.IgnoreWhenEditable"/>, only for
    /// the controls inside it, so a shortcut scoped to a text field fires while the user types there.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsKeyboardShortcut))]
    public async Task<ButilSubscription> SubscribeOn(Microsoft.AspNetCore.Components.ElementReference element,
        string code, Action handler, KeyboardShortcutOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.keyboard.addOn", DotNetRef, listenerId, element, JsKeyboardShortcut.From(code, options));

        return new ButilSubscription(listenerId, () => Remove(listenerId));
    }

    private static KeyboardShortcutOptions Options(ButilModifiers modifiers, bool preventDefault, bool stopPropagation, bool repeat)
        => new() { Modifiers = modifiers, PreventDefault = preventDefault, StopPropagation = stopPropagation, Repeat = repeat };

    /// <summary>
    /// Removes a previously added keyboard shortcut by its handler.
    /// </summary>
    /// <remarks>
    /// Listeners are matched by delegate identity, so you must pass the very same
    /// <paramref name="handler"/> instance that was registered. A newly-created lambda will not
    /// match and the returned array will be empty. To avoid this, keep the <see cref="Guid"/>
    /// returned by <c>Add</c> and call <see cref="Remove(Guid)"/>, or use <c>Subscribe</c>
    /// which returns a disposable <see cref="ButilSubscription"/>.
    /// </remarks>
    public async ValueTask<Guid[]> Remove(Action handler)
    {
        var ids = _handlers.Where(h => Equals(h.Value, handler)).Select(h => h.Key).ToArray();

        await Remove(ids);

        return ids;
    }

    /// <summary>Removes one shortcut, by the id <c>Add</c> returned.</summary>
    public async ValueTask Remove(Guid id)
    {
        await Remove([id]);
    }

    private async ValueTask Remove(Guid[] ids)
    {
        if (ids.Length == 0) return;

        foreach (var id in ids)
        {
            _handlers.TryRemove(id, out _);
        }

        await RemoveFromJs(ids);
    }

    /// <summary>Removes every shortcut registered through this instance.</summary>
    public async ValueTask RemoveAll()
    {
        if (_handlers.Count == 0) return;

        var ids = _handlers.Select(h => h.Key).ToArray();

        _handlers.Clear();

        await RemoveFromJs(ids);
    }

    private async ValueTask RemoveFromJs(Guid[] ids)
    {
        await js.InvokeVoid("BitButil.keyboard.remove", ids);
    }

    /// <summary>Removes every shortcut this instance registered and releases its interop reference.</summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The disposal body. <paramref name="disposing"/> is false only on a finalizer path, where
    /// reaching back into JavaScript is not safe, so nothing is torn down then.
    /// </summary>
    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing is false) return;

        try
        {
            await RemoveAll();
        }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }
}
