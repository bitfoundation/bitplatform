using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Keyboard(IJSRuntime js) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    public async Task<Guid> Add(string code, Action handler, ButilModifiers modifiers = ButilModifiers.None, bool preventDefault = true, bool stopPropagation = true, bool repeat = false)
    {
        var listenerId = KeyboardListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.InvokeVoidAsync("BitButil.keyboard.add",
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

    public Guid[] Remove(Action handler)
    {
        var ids = KeyboardListenersManager.RemoveListener(handler);

        Remove(ids);

        return ids;
    }

    public void Remove(Guid id)
    {
        KeyboardListenersManager.RemoveListeners([id]);

        Remove([id]);
    }

    private void Remove(Guid[] ids)
    {
        foreach (var id in ids)
        {
            _handlers.TryRemove(id, out _);
        }

        _ = js.InvokeVoidAsync("BitButil.keyboard.remove", ids);
    }

    public async Task RemoveAll()
    {
        var ids = _handlers.Select(h => h.Key).ToArray();

        _handlers.Clear();

        KeyboardListenersManager.RemoveListeners(ids);

        await js.InvokeVoidAsync("BitButil.keyboard.remove", ids);
    }

    public void Dispose()
    {
        _ = RemoveAll();
    }
}
