using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

internal static class DomKeyboardEventHandler
{
    internal static async Task AddListener<T>(IJSRuntime js, string elementName, string domEvent, Action<T> listener, object options)
    {
        var action = (listener as Action<DomKeyboardEventArgs>)!;

        var id = DomKeyboardEvent.SetListener(action, elementName, options);

        await js.AddEventListener(elementName, domEvent, DomKeyboardEvent.InvokeMethodName, id, DomKeyboardEventArgs.SelectedMembers, options);
    }


    internal static async Task RemoveListener<T>(IJSRuntime js, string elementName, string domEvent, Action<T> listener, object options)
    {
        var action = (listener as Action<DomKeyboardEventArgs>)!;

        var ids = DomKeyboardEvent.RemoveListener(action, elementName, options);

        await js.RemoveEventListener(elementName, domEvent, ids, options);
    }
}
