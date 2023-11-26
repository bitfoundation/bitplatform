using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class DomKeyboardEventHandler
{
    internal static async Task AddListener<T>(IJSRuntime js,
        string elementName,
        string domEvent,
        Action<T> listener,
        object options,
        bool preventDefault = false,
        bool stopPropagation = false)
    {
        var action = (listener as Action<ButilKeyboardEventArgs>)!;

        var id = DomKeyboardEventListenersManager.SetListener(action, elementName, options);

        await js.AddEventListener(elementName, domEvent, DomKeyboardEventListenersManager.InvokeMethodName, id, ButilKeyboardEventArgs.EventArgsMembers, options, preventDefault, stopPropagation);
    }


    internal static async Task RemoveListener<T>(IJSRuntime js, string elementName, string domEvent, Action<T> listener, object options)
    {
        var action = (listener as Action<ButilKeyboardEventArgs>)!;

        var ids = DomKeyboardEventListenersManager.RemoveListener(action, elementName, options);

        await js.RemoveEventListener(elementName, domEvent, ids, options);
    }
}
