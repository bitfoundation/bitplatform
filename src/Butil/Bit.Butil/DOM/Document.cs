using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

public class Document(IJSRuntime js)
{
    private const string ElementName = "document";

    public async Task AddEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        await DomEventDispatcher.AddEventListener(js, ElementName, domEvent, listener, useCapture);
    }

    public async Task RemoveEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        await DomEventDispatcher.RemoveEventListener(js, ElementName, domEvent, listener, useCapture);
    }
}
