namespace Bit.BlazorUI.Events;

/// <summary>
/// Registers the CSS animation and transition events Blazor itself has no directives for, so that an element can
/// handle them with @onanimationend, @ontransitionend and the rest of them. A .razor file reaches them by importing
/// this namespace (<c>@using Bit.BlazorUI.Events</c>, normally in an _Imports.razor).
/// </summary>
/// <remarks>
/// The Razor compiler only reads these registrations off a type named EventHandlers, and only where its namespace is
/// imported. They are kept out of the Bit.BlazorUI namespace every app imports because two registrations of the
/// same name in scope fail the build (RZ9990): an app that registers one of these events itself keeps compiling, and
/// imports this namespace only once its own registration is gone.
/// <br />
/// The arguments of the events (<see cref="BitAnimationEventArgs"/> and <see cref="BitTransitionEventArgs"/>) are
/// read off the browser's event by the library's script (bit.blazorui.js), which registers them with Blazor as it
/// loads. The events bubble, so each directive can be paired with :stopPropagation. None of them can be canceled, so
/// a :preventDefault on one of them does nothing; it is enabled all the same because the Razor compiler only binds
/// :stopPropagation for a registration that enables both.
/// <br />
/// A duration of zero never fires an end: an animation or a transition that is cut to nothing - under
/// prefers-reduced-motion, say - is cut to 0.01ms rather than to 0ms for the handler waiting on it to still run.
/// </remarks>
[EventHandler("onanimationstart", typeof(BitAnimationEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onanimationend", typeof(BitAnimationEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onanimationiteration", typeof(BitAnimationEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("onanimationcancel", typeof(BitAnimationEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontransitionrun", typeof(BitTransitionEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontransitionstart", typeof(BitTransitionEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontransitionend", typeof(BitTransitionEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
[EventHandler("ontransitioncancel", typeof(BitTransitionEventArgs), enableStopPropagation: true, enablePreventDefault: true)]
public static class EventHandlers
{
}
