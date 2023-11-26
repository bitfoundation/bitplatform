using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class DomMouseEventHandler
{
    internal static async Task AddListener(IJSRuntime js,
        string elementName,
        string domEvent,
        Action<ButilMouseEventArgs> listener,
        object options,
        bool preventDefault = false,
        bool stopPropagation = false)
    {
        var id = DomMouseEventListenersManager.SetListener(listener, elementName, options);

        await js.AddEventListener(elementName, domEvent, DomMouseEventListenersManager.InvokeMethodName, id, ButilMouseEventArgs.EventArgsMembers, options, preventDefault, stopPropagation);
    }


    internal static async Task RemoveListener(IJSRuntime js, string elementName, string domEvent, Action<ButilMouseEventArgs> listener, object options)
    {
        var ids = DomMouseEventListenersManager.RemoveListener(listener, elementName, options);

        await js.RemoveEventListener(elementName, domEvent, ids, options);
    }
}
