using System;

namespace Bit.Butil;

internal class DomEventArgs
{
    internal static Type TypeOf(string domEvent)
    {
        return domEvent switch
        {
            ButilEvents.KeyDown => typeof(ButilKeyboardEventArgs),
            ButilEvents.KeyUp => typeof(ButilKeyboardEventArgs),
            ButilEvents.KeyPress => typeof(ButilKeyboardEventArgs),
            _ => throw new NotSupportedException(domEvent),
        };
    }
}
