using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Keyboard(IJSRuntime js) : IDisposable
{
    private readonly ConcurrentDictionary<Guid, Action> _handlers = new();

    public async Task<Guid> Add(string key, Action handler, ButilModifiers modifiers = ButilModifiers.None, bool preventDefault = true, bool stopPropagation = true, bool repeat = false)
    {
        var listenerId = KeyboardListenersManager.AddListener(handler);
        _handlers.TryAdd(listenerId, handler);

        await js.AddKeyboard(KeyboardListenersManager.InvokeMethodName, listenerId, key,
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

        _ = js.RemoveKeyboard(ids);
    }

    public void Dispose()
    {
        var ids = _handlers.Select(h => h.Key).ToArray();

        KeyboardListenersManager.RemoveListeners(ids);

        _ = js.RemoveKeyboard(ids);
    }

    private class Listener
    {
        public string? Key { get; set; }
        public Action? Handler { get; set; }
        public ButilModifiers Modifiers { get; set; }
    }
}
