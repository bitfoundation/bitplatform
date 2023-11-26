using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Bit.Butil;

public class Keyboard(Document document) : IDisposable
{
    private bool _isInitialized;
    private readonly ConcurrentDictionary<Guid, Listener> _listeners = new();


    private async Task Init()
    {
        if (_isInitialized) return;

        await document.AddEventListener<ButilKeyboardEventArgs>(ButilEvents.KeyUp, RunHandlers, false, true, true);

        _isInitialized = true;
    }

    private void RunHandlers(ButilKeyboardEventArgs args)
    {
        foreach (var (_, listener) in _listeners)
        {
            var key = listener.Key;
            var handler = listener.Handler;
            var modifiers = listener.Modifiers;

            if (handler is null) continue;

            if (string.Equals(key, args.Key, StringComparison.OrdinalIgnoreCase) is false) continue;

            if (modifiers.HasFlag(ButilModifiers.Alt) && args.AltKey is false) continue;
            if (modifiers.HasFlag(ButilModifiers.Ctrl) && args.CtrlKey is false) continue;
            if (modifiers.HasFlag(ButilModifiers.Meta) && args.MetaKey is false) continue;
            if (modifiers.HasFlag(ButilModifiers.Shift) && args.ShiftKey is false) continue;

            try { Task.Run(handler); } catch { }
        }
    }

    public async Task<Guid> Add(string key, Action handler, ButilModifiers modifiers = ButilModifiers.None)
    {
        await Init();

        var id = Guid.NewGuid();

        var listener = new Listener { Key = key, Handler = handler, Modifiers = modifiers };

        _listeners.TryAdd(id, listener);

        return id;
    }

    public async Task<bool> Remove(Guid id)
    {
        return _listeners.TryRemove(id, out _);
    }

    public void Dispose()
    {
        _ = document.RemoveEventListener<ButilKeyboardEventArgs>(ButilEvents.KeyDown, RunHandlers);
    }

    private class Listener
    {
        public string? Key { get; set; }
        public Action? Handler { get; set; }
        public ButilModifiers Modifiers { get; set; }
    }
}
