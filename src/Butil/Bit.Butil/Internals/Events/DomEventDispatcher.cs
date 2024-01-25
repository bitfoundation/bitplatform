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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomMouseEventListenersManager))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DomKeyboardEventListenersManager))]
    internal static async Task AddEventListener<T>(IJSRuntime js,
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
            var action = listener as Action<ButilKeyboardEventArgs>;
            id = DomKeyboardEventListenersManager.SetListener(action!, elementName, options);
        }
        else if (argType == typeof(ButilMouseEventArgs))
        {
            args = ButilMouseEventArgs.EventArgsMembers;
            methodName = DomMouseEventListenersManager.InvokeMethodName;
            var action = listener as Action<ButilMouseEventArgs>;
            id = DomMouseEventListenersManager.SetListener(action!, elementName, options);
        }
        else
        {
            methodName = DomEventListenersManager.InvokeMethodName;
            var action = listener as Action<object>;
            id = DomEventListenersManager.SetListener(action!, elementName, options);
        }

        await js.AddEventListener(elementName, domEvent, methodName, id, args, options, preventDefault, stopPropagation);
    }

    internal static async Task RemoveEventListener<T>(IJSRuntime js,
        string elementName,
        string domEvent,
        Action<T> listener,
        bool useCapture = false)
    {
        var argType = typeof(T);
        var eventType = DomEventArgs.TypeOf(domEvent);

        if (argType != eventType)
            throw new InvalidOperationException($"Invalid listener type ({argType}) for this dom event type ({eventType})");

        Guid[] ids = [];
        var options = useCapture ? TrueUseCapture : FalseUseCapture;

        if (argType == typeof(ButilKeyboardEventArgs))
        {
            var action = listener as Action<ButilKeyboardEventArgs>;
            ids = DomKeyboardEventListenersManager.RemoveListener(action!, elementName, options);
        }
        else if (argType == typeof(ButilMouseEventArgs))
        {
            var action = listener as Action<ButilMouseEventArgs>;
            ids = DomMouseEventListenersManager.RemoveListener(action!, elementName, options);
        }
        else
        {
            var action = listener as Action<object>;
            ids = DomEventListenersManager.RemoveListener(action!, elementName, options);
        }

        await js.RemoveEventListener(elementName, domEvent, ids, options);
    }
}
