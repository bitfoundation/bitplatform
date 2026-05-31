using System;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class DomEventDispatcher
{
    private static readonly object FalseUseCapture = false;
    private static readonly object TrueUseCapture = true;

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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomMouseEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomKeyboardEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomPointerEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomWheelEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomTouchEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomFocusEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomInputEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomDragEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomClipboardEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomCompositionEventListenersManager))]
    internal static async Task<Guid> AddEventListener<T>(IJSRuntime js,
        string elementName,
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

        string[] args = [];
        var methodName = "";
        var id = Guid.NewGuid();
        var options = useCapture ? TrueUseCapture : FalseUseCapture;

        if (argType == typeof(ButilKeyboardEventArgs))
        {
            args = ButilKeyboardEventArgs.EventArgsMembers;
            methodName = DomKeyboardEventListenersManager.InvokeMethodName;
            id = DomKeyboardEventListenersManager.SetListener((listener as Action<ButilKeyboardEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilMouseEventArgs))
        {
            args = ButilMouseEventArgs.EventArgsMembers;
            methodName = DomMouseEventListenersManager.InvokeMethodName;
            id = DomMouseEventListenersManager.SetListener((listener as Action<ButilMouseEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilPointerEventArgs))
        {
            args = ButilPointerEventArgs.EventArgsMembers;
            methodName = DomPointerEventListenersManager.InvokeMethodName;
            id = DomPointerEventListenersManager.SetListener((listener as Action<ButilPointerEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilWheelEventArgs))
        {
            args = ButilWheelEventArgs.EventArgsMembers;
            methodName = DomWheelEventListenersManager.InvokeMethodName;
            id = DomWheelEventListenersManager.SetListener((listener as Action<ButilWheelEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilTouchEventArgs))
        {
            args = ButilTouchEventArgs.EventArgsMembers;
            methodName = DomTouchEventListenersManager.InvokeMethodName;
            id = DomTouchEventListenersManager.SetListener((listener as Action<ButilTouchEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilFocusEventArgs))
        {
            args = ButilFocusEventArgs.EventArgsMembers;
            methodName = DomFocusEventListenersManager.InvokeMethodName;
            id = DomFocusEventListenersManager.SetListener((listener as Action<ButilFocusEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilInputEventArgs))
        {
            args = ButilInputEventArgs.EventArgsMembers;
            methodName = DomInputEventListenersManager.InvokeMethodName;
            id = DomInputEventListenersManager.SetListener((listener as Action<ButilInputEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilDragEventArgs))
        {
            args = ButilDragEventArgs.EventArgsMembers;
            methodName = DomDragEventListenersManager.InvokeMethodName;
            id = DomDragEventListenersManager.SetListener((listener as Action<ButilDragEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilClipboardEventArgs))
        {
            args = ButilClipboardEventArgs.EventArgsMembers;
            methodName = DomClipboardEventListenersManager.InvokeMethodName;
            id = DomClipboardEventListenersManager.SetListener((listener as Action<ButilClipboardEventArgs>)!, elementName, options);
        }
        else if (argType == typeof(ButilCompositionEventArgs))
        {
            args = ButilCompositionEventArgs.EventArgsMembers;
            methodName = DomCompositionEventListenersManager.InvokeMethodName;
            id = DomCompositionEventListenersManager.SetListener((listener as Action<ButilCompositionEventArgs>)!, elementName, options);
        }
        else
        {
            methodName = DomEventListenersManager.InvokeMethodName;
            var action = listener as Action<object>;
            id = DomEventListenersManager.SetListener(action!, elementName, options);
        }

        await js.AddEventListener(elementName, domEvent, methodName, id, args, options, preventDefault, stopPropagation);

        return id;
    }

    internal static async Task<Guid[]> RemoveEventListener<T>(IJSRuntime js,
        string elementName,
        string domEvent,
        Action<T> listener,
        bool useCapture = false)
    {
        var argType = typeof(T);
        var eventType = DomEventArgs.TypeOf(domEvent);

        if (argType != eventType)
            throw new InvalidOperationException($"Invalid listener type ({argType}) for this dom event type ({eventType})");

        Guid[] ids;
        var options = useCapture ? TrueUseCapture : FalseUseCapture;

        if (argType == typeof(ButilKeyboardEventArgs))
            ids = DomKeyboardEventListenersManager.RemoveListener((listener as Action<ButilKeyboardEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilMouseEventArgs))
            ids = DomMouseEventListenersManager.RemoveListener((listener as Action<ButilMouseEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilPointerEventArgs))
            ids = DomPointerEventListenersManager.RemoveListener((listener as Action<ButilPointerEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilWheelEventArgs))
            ids = DomWheelEventListenersManager.RemoveListener((listener as Action<ButilWheelEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilTouchEventArgs))
            ids = DomTouchEventListenersManager.RemoveListener((listener as Action<ButilTouchEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilFocusEventArgs))
            ids = DomFocusEventListenersManager.RemoveListener((listener as Action<ButilFocusEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilInputEventArgs))
            ids = DomInputEventListenersManager.RemoveListener((listener as Action<ButilInputEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilDragEventArgs))
            ids = DomDragEventListenersManager.RemoveListener((listener as Action<ButilDragEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilClipboardEventArgs))
            ids = DomClipboardEventListenersManager.RemoveListener((listener as Action<ButilClipboardEventArgs>)!, elementName, options);
        else if (argType == typeof(ButilCompositionEventArgs))
            ids = DomCompositionEventListenersManager.RemoveListener((listener as Action<ButilCompositionEventArgs>)!, elementName, options);
        else
            ids = DomEventListenersManager.RemoveListener((listener as Action<object>)!, elementName, options);

        await js.RemoveEventListener(elementName, domEvent, ids, options);

        return ids;
    }

    /// <summary>
    /// Detaches a single listener by id, regardless of which typed manager owns it.
    /// Used by <see cref="ButilSubscription"/> when the original delegate isn't available.
    /// </summary>
    internal static async Task RemoveEventListenerById(IJSRuntime js, string elementName, string domEvent, Guid id, bool useCapture = false)
    {
        // Try every typed store; the one that owns the id will succeed.
        DomKeyboardEventListenersManager.RemoveById(id);
        DomMouseEventListenersManager.RemoveById(id);
        DomPointerEventListenersManager.RemoveById(id);
        DomWheelEventListenersManager.RemoveById(id);
        DomTouchEventListenersManager.RemoveById(id);
        DomFocusEventListenersManager.RemoveById(id);
        DomInputEventListenersManager.RemoveById(id);
        DomDragEventListenersManager.RemoveById(id);
        DomClipboardEventListenersManager.RemoveById(id);
        DomCompositionEventListenersManager.RemoveById(id);
        DomEventListenersManager.RemoveById(id);

        var options = useCapture ? TrueUseCapture : FalseUseCapture;
        await js.RemoveEventListener(elementName, domEvent, [id], options);
    }
}
