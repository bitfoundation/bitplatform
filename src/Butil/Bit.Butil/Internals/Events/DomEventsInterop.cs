using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Per-instance DOM event dispatcher. Replaces the old static <c>Dom*EventListenersManager</c> set:
/// all listener state lives on this instance and the typed <see cref="JSInvokableAttribute"/> callbacks
/// are reached through a per-instance <see cref="DotNetObjectReference{T}"/>. That keeps listeners
/// isolated per Blazor circuit / WASM app and releases them (no leak) when the owner is disposed.
/// </summary>
internal sealed class DomEventsInterop : IDisposable
{
    private readonly ConcurrentDictionary<Guid, Entry> _listeners = new();

    private DotNetObjectReference<DomEventsInterop>? _dotNetRef;
    private DotNetObjectReference<DomEventsInterop> DotNetRef => DotNetObjectReferenceHelper.GetOrCreate(ref _dotNetRef, this);

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilMouseEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilKeyboardEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilPointerEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilWheelEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilTouchEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilTouchPoint))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilFocusEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilInputEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilDragEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilClipboardEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ButilCompositionEventArgs))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(JsAddEventListenerOptions))]
    internal async Task<Guid> AddEventListener<T>(IJSRuntime js,
        string elementName,
        string domEvent,
        Action<T> listener,
        bool useCapture = false,
        bool preventDefault = false,
        bool stopPropagation = false,
        bool passive = false,
        bool once = false)
    {
        var argType = typeof(T);
        var eventType = DomEventArgs.TypeOf(domEvent);

        if (argType != eventType)
            throw new InvalidOperationException($"Invalid listener type ({argType}) for this dom event type ({eventType})");

        var (methodName, members) = Resolve(argType);
        // Pass a bare boolean for the common capture-only case (keeps the wire payload minimal and
        // backward-compatible); upgrade to the full options object only when passive/once are set.
        var options = (passive || once)
            ? (object)new JsAddEventListenerOptions { Capture = useCapture, Passive = passive, Once = once }
            : useCapture;
        var id = Guid.NewGuid();
        _listeners.TryAdd(id, new Entry { Action = listener, ArgType = argType, Element = elementName, Event = domEvent, UseCapture = useCapture });

        await js.AddEventListener(elementName, domEvent, methodName, DotNetRef, id, members, options, preventDefault, stopPropagation);

        return id;
    }

    internal async Task<Guid[]> RemoveEventListener<T>(IJSRuntime js,
        string elementName,
        string domEvent,
        Action<T> listener,
        bool useCapture = false)
    {
        var argType = typeof(T);
        var eventType = DomEventArgs.TypeOf(domEvent);

        if (argType != eventType)
            throw new InvalidOperationException($"Invalid listener type ({argType}) for this dom event type ({eventType})");

        var ids = _listeners
            .Where(l => Equals(l.Value.Action, listener) && l.Value.Element == elementName && l.Value.Event == domEvent && l.Value.UseCapture == useCapture)
            .Select(l => l.Key)
            .ToArray();

        if (ids.Length == 0) return ids; // nothing matched - skip the interop round-trip

        foreach (var id in ids) _listeners.TryRemove(id, out _);

        await js.RemoveEventListener(elementName, domEvent, ids, (object)useCapture);

        return ids;
    }

    /// <summary>Detaches a single listener by id (used by subscription handles).</summary>
    internal async Task RemoveEventListenerById(IJSRuntime js, string elementName, string domEvent, Guid id, bool useCapture = false)
    {
        _listeners.TryRemove(id, out _);
        await js.RemoveEventListener(elementName, domEvent, [id], (object)useCapture);
    }

    /// <summary>
    /// Registers a listener without performing the window/document JS wiring. Used by the
    /// element-scoped path, which drives its own JS subscription but still needs the typed
    /// callback routing and the per-instance reference.
    /// </summary>
    internal (Guid Id, string MethodName, string[] Members, DotNetObjectReference<DomEventsInterop> Ref) Register<T>(
        Action<T> listener, string element, string domEvent, bool useCapture)
    {
        var (methodName, members) = Resolve(typeof(T));
        var id = Guid.NewGuid();
        _listeners.TryAdd(id, new Entry { Action = listener, ArgType = typeof(T), Element = element, Event = domEvent, UseCapture = useCapture });
        return (id, methodName, members, DotNetRef);
    }

    internal void Unregister(Guid id) => _listeners.TryRemove(id, out _);

    private void Dispatch<T>(Guid id, T args)
    {
        if (_listeners.TryGetValue(id, out var entry) && entry.Action is Action<T> action)
            action.Invoke(args);
    }

    [JSInvokable("InvokeMouseEvent")] public void InvokeMouseEvent(Guid id, ButilMouseEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeKeyboardEvent")] public void InvokeKeyboardEvent(Guid id, ButilKeyboardEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokePointerEvent")] public void InvokePointerEvent(Guid id, ButilPointerEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeWheelEvent")] public void InvokeWheelEvent(Guid id, ButilWheelEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeTouchEvent")] public void InvokeTouchEvent(Guid id, ButilTouchEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeFocusEvent")] public void InvokeFocusEvent(Guid id, ButilFocusEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeInputEvent")] public void InvokeInputEvent(Guid id, ButilInputEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeDragEvent")] public void InvokeDragEvent(Guid id, ButilDragEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeClipboardEvent")] public void InvokeClipboardEvent(Guid id, ButilClipboardEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeCompositionEvent")] public void InvokeCompositionEvent(Guid id, ButilCompositionEventArgs args) => Dispatch(id, args);
    [JSInvokable("InvokeDomEvent")] public void InvokeDomEvent(Guid id, object args) => Dispatch(id, args);

    private static (string MethodName, string[] Members) Resolve(Type argType)
    {
        if (argType == typeof(ButilKeyboardEventArgs)) return ("InvokeKeyboardEvent", ButilKeyboardEventArgs.EventArgsMembers);
        if (argType == typeof(ButilMouseEventArgs)) return ("InvokeMouseEvent", ButilMouseEventArgs.EventArgsMembers);
        if (argType == typeof(ButilPointerEventArgs)) return ("InvokePointerEvent", ButilPointerEventArgs.EventArgsMembers);
        if (argType == typeof(ButilWheelEventArgs)) return ("InvokeWheelEvent", ButilWheelEventArgs.EventArgsMembers);
        if (argType == typeof(ButilTouchEventArgs)) return ("InvokeTouchEvent", ButilTouchEventArgs.EventArgsMembers);
        if (argType == typeof(ButilFocusEventArgs)) return ("InvokeFocusEvent", ButilFocusEventArgs.EventArgsMembers);
        if (argType == typeof(ButilInputEventArgs)) return ("InvokeInputEvent", ButilInputEventArgs.EventArgsMembers);
        if (argType == typeof(ButilDragEventArgs)) return ("InvokeDragEvent", ButilDragEventArgs.EventArgsMembers);
        if (argType == typeof(ButilClipboardEventArgs)) return ("InvokeClipboardEvent", ButilClipboardEventArgs.EventArgsMembers);
        if (argType == typeof(ButilCompositionEventArgs)) return ("InvokeCompositionEvent", ButilCompositionEventArgs.EventArgsMembers);
        return ("InvokeDomEvent", []);
    }

    public void Dispose()
    {
        _listeners.Clear();
        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }

    private sealed class Entry
    {
        public object Action { get; set; } = default!;
        public Type ArgType { get; set; } = default!;
        public string Element { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public bool UseCapture { get; set; }
    }
}
