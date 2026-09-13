using System;
using System.Collections.Concurrent;
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
    public async Task<Guid> Add(string code, Action handler, ButilModifiers modifiers = ButilModifiers.None, bool preventDefault = true, bool stopPropagation = true, bool repeat = false)
    {
        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.keyboard.add",
            DotNetRef,
            listenerId,
            code,
            modifiers.HasFlag(ButilModifiers.Alt),
            modifiers.HasFlag(ButilModifiers.Ctrl),
            modifiers.HasFlag(ButilModifiers.Meta),
            modifiers.HasFlag(ButilModifiers.Shift),
            preventDefault,
            stopPropagation,
            repeat);

        return listenerId;
    }

    /// <summary>
    /// Same as <see cref="Add"/> but returns an <see cref="IAsyncDisposable"/> handle that
    /// detaches the shortcut when disposed.
    /// </summary>
    public async Task<ButilSubscription> Subscribe(string code, Action handler,
        ButilModifiers modifiers = ButilModifiers.None,
        bool preventDefault = true,
        bool stopPropagation = true,
        bool repeat = false)
    {
        var id = await Add(code, handler, modifiers, preventDefault, stopPropagation, repeat);
        return new ButilSubscription(id, () => Remove(id));
    }

    /// <summary>
    /// Element-scoped variant of <see cref="Subscribe"/>: the shortcut only fires while the given
    /// element (or one of its descendants) has focus or receives the keyboard event.
    /// </summary>
    public async Task<ButilSubscription> SubscribeOn(Microsoft.AspNetCore.Components.ElementReference element,
        string code, Action handler,
        ButilModifiers modifiers = ButilModifiers.None,
        bool preventDefault = true,
        bool stopPropagation = true,
        bool repeat = false)
    {
        var listenerId = Guid.NewGuid();
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoid("BitButil.keyboard.addOn",
            DotNetRef,
            listenerId,
            element,
            code,
            modifiers.HasFlag(ButilModifiers.Alt),
            modifiers.HasFlag(ButilModifiers.Ctrl),
            modifiers.HasFlag(ButilModifiers.Meta),
            modifiers.HasFlag(ButilModifiers.Shift),
            preventDefault,
            stopPropagation,
            repeat);

        return new ButilSubscription(listenerId, () => Remove(listenerId));
    }

    /// <summary>
    /// Removes a previously added keyboard shortcut by its handler.
    /// </summary>
    /// <remarks>
    /// Listeners are matched by delegate identity, so you must pass the very same
    /// <paramref name="handler"/> instance that was registered. A newly-created lambda will not
    /// match and the returned array will be empty. To avoid this, keep the <see cref="Guid"/>
    /// returned by <see cref="Add"/> and call <see cref="Remove(Guid)"/>, or use <see cref="Subscribe"/>
    /// which returns a disposable <see cref="ButilSubscription"/>.
    /// </remarks>
    public async ValueTask<Guid[]> Remove(Action handler)
    {
        var ids = _handlers.Where(h => Equals(h.Value, handler)).Select(h => h.Key).ToArray();

        await Remove(ids);

        return ids;
    }

    /// <summary>Removes one shortcut, by the id <see cref="Add"/> returned.</summary>
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
