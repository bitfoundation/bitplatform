using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Bheadless;

/// <summary>
/// The DOM events Bit.Bheadless registers with Blazor (from its JS initializer): <c>@onbitbhkeydown</c>
/// and <c>@onbitbhclick</c> fire with every <c>keydown</c> / <c>click</c> and say which host element owns it;
/// <c>@onbitbhhover</c> is sent by the runtime to a list option (<c>data-bit-bh-hover</c>) the pointer reaches while
/// another option is highlighted - once, not with every move, and not when the keyboard scrolls it under a resting pointer.
/// </summary>
[EventHandler("onbitbhkeydown", typeof(BitBhKeyboardEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onbitbhclick", typeof(BitBhMouseEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onbitbhhover", typeof(EventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class BitBhEventHandlers
{
    /// <summary>The attribute that marks an element as the owner of the events inside it.</summary>
    public const string OwnerAttribute = "data-bit-bh-event-owner";

    /// <summary>The name of the owned <c>keydown</c> event in a render tree.</summary>
    public const string KeyDown = "onbitbhkeydown";

    /// <summary>The name of the owned <c>click</c> event in a render tree.</summary>
    public const string Click = "onbitbhclick";

    /// <summary>The name of the option hover event in a render tree.</summary>
    public const string Hover = "onbitbhhover";

    /// <summary>The attribute that marks an element as a list option the runtime sends <see cref="Hover"/> to.</summary>
    public const string HoverAttribute = "data-bit-bh-hover";
}
