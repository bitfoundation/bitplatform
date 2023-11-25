using System;

namespace Bit.Butil;

internal class DomEventArgs
{
    public static Type TypeOf(string domEvent)
    {
        return domEvent switch
        {
            DomEvents.KeyDown => typeof(DomKeyboardEventArgs),
            DomEvents.KeyUp => typeof(DomKeyboardEventArgs),
            DomEvents.KeyPress => typeof(DomKeyboardEventArgs),
            _ => throw new NotSupportedException(domEvent),
        };
    }
}
