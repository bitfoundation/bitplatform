using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Keyboard(IJSRuntime js) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(KeyboardListenersManager))]
    public async Task<Guid> Add(string code, Action handler, ButilModifiers modifiers = ButilModifiers.None, bool preventDefault = true, bool stopPropagation = true, bool repeat = false)
    {
        var listenerId = KeyboardListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.FastInvokeVoidAsync("BitButil.keyboard.add",
            KeyboardListenersManager.InvokeMethodName,
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

    public async ValueTask<Guid[]> Remove(Action handler)
    {
        var ids = KeyboardListenersManager.RemoveListener(handler);

        await Remove(ids);

        return ids;
    }

    public async ValueTask Remove(Guid id)
    {
        KeyboardListenersManager.RemoveListeners([id]);

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

    public async ValueTask RemoveAll()
    {
        if (_handlers.Count == 0) return;

        var ids = _handlers.Select(h => h.Key).ToArray();

        _handlers.Clear();

        KeyboardListenersManager.RemoveListeners(ids);

        await RemoveFromJs(ids);
    }

    private async ValueTask RemoveFromJs(Guid[] ids)
    {
        if (OperatingSystem.IsBrowser() is false) return;

        await js.FastInvokeVoidAsync("BitButil.keyboard.remove", ids);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);

        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await RemoveAll();
        }
    }
}
