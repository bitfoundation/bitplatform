using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Element-scoped DOM event subscriptions. Returns an <see cref="IAsyncDisposable"/> handle
/// (<see cref="ButilSubscription"/>) so callers can <c>await using</c> for the lifetime of a
/// component without hand-rolling Add/Remove pairs.
/// </summary>
/// <remarks>
/// Internally this routes through the same per-element JS plumbing used by Document and Window,
/// so all the typed event-arg classes (<see cref="ButilPointerEventArgs"/>, <see cref="ButilWheelEventArgs"/>, etc.)
/// are available with no extra wiring.
/// </remarks>
public static class ElementReferenceEventExtensions
{
    /// <summary>
    /// Subscribes to a DOM event on the given element. The returned handle detaches the listener on dispose.
    /// </summary>
    public static async Task<ButilSubscription> SubscribeEvent<T>(
        this ElementReference element,
        IJSRuntime js,
        string domEvent,
        Action<T> listener,
        bool useCapture = false,
        bool preventDefault = false,
        bool stopPropagation = false)
    {
        var argType = typeof(T);
        var eventType = DomEventArgs.TypeOf(domEvent);
        if (argType != eventType)
            throw new InvalidOperationException($"Invalid listener type ({argType}) for this dom event type ({eventType})");

        // Each element gets a generated id so the JS side can target it directly.
        var elementId = Guid.NewGuid().ToString("N");
        var members = ResolveMembers(argType);
        var methodName = ResolveMethodName(argType);
        var listenerId = RegisterListener(argType, listener, elementId, useCapture);

        var options = useCapture;

        await js.InvokeVoid("BitButil.element.subscribeEvent",
            element,
            elementId,
            domEvent,
            methodName,
            listenerId,
            members,
            options,
            preventDefault,
            stopPropagation);

        return new ButilSubscription(listenerId, async () =>
        {
            UnregisterListener(argType, listenerId);
            if (OperatingSystem.IsBrowser() is false) return;
            await js.InvokeVoid("BitButil.element.unsubscribeEvent", elementId, domEvent, listenerId, options);
        });
    }

    private static string[] ResolveMembers(Type argType)
    {
        if (argType == typeof(ButilKeyboardEventArgs)) return ButilKeyboardEventArgs.EventArgsMembers;
        if (argType == typeof(ButilMouseEventArgs)) return ButilMouseEventArgs.EventArgsMembers;
        if (argType == typeof(ButilPointerEventArgs)) return ButilPointerEventArgs.EventArgsMembers;
        if (argType == typeof(ButilWheelEventArgs)) return ButilWheelEventArgs.EventArgsMembers;
        if (argType == typeof(ButilTouchEventArgs)) return ButilTouchEventArgs.EventArgsMembers;
        if (argType == typeof(ButilFocusEventArgs)) return ButilFocusEventArgs.EventArgsMembers;
        if (argType == typeof(ButilInputEventArgs)) return ButilInputEventArgs.EventArgsMembers;
        if (argType == typeof(ButilDragEventArgs)) return ButilDragEventArgs.EventArgsMembers;
        if (argType == typeof(ButilClipboardEventArgs)) return ButilClipboardEventArgs.EventArgsMembers;
        if (argType == typeof(ButilCompositionEventArgs)) return ButilCompositionEventArgs.EventArgsMembers;
        return [];
    }

    private static string ResolveMethodName(Type argType)
    {
        if (argType == typeof(ButilKeyboardEventArgs)) return DomKeyboardEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilMouseEventArgs)) return DomMouseEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilPointerEventArgs)) return DomPointerEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilWheelEventArgs)) return DomWheelEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilTouchEventArgs)) return DomTouchEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilFocusEventArgs)) return DomFocusEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilInputEventArgs)) return DomInputEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilDragEventArgs)) return DomDragEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilClipboardEventArgs)) return DomClipboardEventListenersManager.InvokeMethodName;
        if (argType == typeof(ButilCompositionEventArgs)) return DomCompositionEventListenersManager.InvokeMethodName;
        return DomEventListenersManager.InvokeMethodName;
    }

    private static Guid RegisterListener<T>(Type argType, Action<T> listener, string elementId, bool useCapture)
    {
        // The existing element-scoped store key is the elementId — we reuse the same managers.
        object options = useCapture;
        if (argType == typeof(ButilKeyboardEventArgs))
            return DomKeyboardEventListenersManager.SetListener((listener as Action<ButilKeyboardEventArgs>)!, elementId, options);
        if (argType == typeof(ButilMouseEventArgs))
            return DomMouseEventListenersManager.SetListener((listener as Action<ButilMouseEventArgs>)!, elementId, options);
        if (argType == typeof(ButilPointerEventArgs))
            return DomPointerEventListenersManager.SetListener((listener as Action<ButilPointerEventArgs>)!, elementId, options);
        if (argType == typeof(ButilWheelEventArgs))
            return DomWheelEventListenersManager.SetListener((listener as Action<ButilWheelEventArgs>)!, elementId, options);
        if (argType == typeof(ButilTouchEventArgs))
            return DomTouchEventListenersManager.SetListener((listener as Action<ButilTouchEventArgs>)!, elementId, options);
        if (argType == typeof(ButilFocusEventArgs))
            return DomFocusEventListenersManager.SetListener((listener as Action<ButilFocusEventArgs>)!, elementId, options);
        if (argType == typeof(ButilInputEventArgs))
            return DomInputEventListenersManager.SetListener((listener as Action<ButilInputEventArgs>)!, elementId, options);
        if (argType == typeof(ButilDragEventArgs))
            return DomDragEventListenersManager.SetListener((listener as Action<ButilDragEventArgs>)!, elementId, options);
        if (argType == typeof(ButilClipboardEventArgs))
            return DomClipboardEventListenersManager.SetListener((listener as Action<ButilClipboardEventArgs>)!, elementId, options);
        if (argType == typeof(ButilCompositionEventArgs))
            return DomCompositionEventListenersManager.SetListener((listener as Action<ButilCompositionEventArgs>)!, elementId, options);
        return DomEventListenersManager.SetListener((listener as Action<object>)!, elementId, options);
    }

    private static void UnregisterListener(Type argType, Guid id)
    {
        if (argType == typeof(ButilKeyboardEventArgs)) { DomKeyboardEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilMouseEventArgs)) { DomMouseEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilPointerEventArgs)) { DomPointerEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilWheelEventArgs)) { DomWheelEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilTouchEventArgs)) { DomTouchEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilFocusEventArgs)) { DomFocusEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilInputEventArgs)) { DomInputEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilDragEventArgs)) { DomDragEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilClipboardEventArgs)) { DomClipboardEventListenersManager.RemoveById(id); return; }
        if (argType == typeof(ButilCompositionEventArgs)) { DomCompositionEventListenersManager.RemoveById(id); return; }
        DomEventListenersManager.RemoveById(id);
    }
}
