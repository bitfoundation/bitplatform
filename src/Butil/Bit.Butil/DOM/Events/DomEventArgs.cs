using System;

namespace Bit.Butil;

internal class DomEventArgs
{
    public static Type TypeOf(string domEvent)
    {
        return domEvent switch
        {
            DomEvents.KeyDown => typeof(DomKeyboardEventArgs),
            _ => throw new NotSupportedException(domEvent),
        };
    }
}
