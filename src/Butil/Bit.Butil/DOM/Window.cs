using System;

namespace Bit.Butil;

public static class Window
{
    private const string ElementName = "window";

    public static void AddEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        DomEventDispatcher.AddEventListener<T>(ElementName, domEvent, listener, useCapture);
    }

    public static void RemoveEventListener<T>(string domEvent, Action<T> listener, bool useCapture = false)
    {
        DomEventDispatcher.RemoveEventListener<T>(ElementName, domEvent, listener, useCapture);
    }
}
