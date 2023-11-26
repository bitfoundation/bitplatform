using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class DomKeyboardEventHandler
{
    internal static async Task AddListener(IJSRuntime js,
        string elementName,
        string domEvent,
        Action<ButilKeyboardEventArgs> listener,
        object options,
        bool preventDefault = false,
        bool stopPropagation = false)
    {
        var id = DomKeyboardEventListenersManager.SetListener(listener, elementName, options);

        await js.AddEventListener(elementName, domEvent, DomKeyboardEventListenersManager.InvokeMethodName, id, ButilKeyboardEventArgs.EventArgsMembers, options, preventDefault, stopPropagation);
    }


    internal static async Task RemoveListener(IJSRuntime js, string elementName, string domEvent, Action<ButilKeyboardEventArgs> listener, object options)
    {
        var ids = DomKeyboardEventListenersManager.RemoveListener(listener, elementName, options);

        await js.RemoveEventListener(elementName, domEvent, ids, options);
    }
}
