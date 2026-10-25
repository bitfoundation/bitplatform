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
/// read off the browser's event by the library's script (bit.blazorui.js), which registers them with Blazor under the
/// events' own names as soon as Blazor is on the page, whichever of the two scripts loads first. Blazor takes one
/// registration per name, so an app's own Blazor.registerCustomEventType of one of these names, made after the
/// script's, is taken in rather than thrown on: its createEventArgs runs beside the library's, its fields winning a
/// clash, and its handlers get its arguments while the library's get theirs. One made before the script's keeps the
/// name to itself, and the library's handlers then get only what the app's arguments carry.
/// <br />
/// The events bubble, so each directive can be paired with :stopPropagation. None of them can be canceled, so
/// a :preventDefault on one of them does nothing; it is enabled all the same because the Razor compiler only binds
/// :stopPropagation for a registration that enables both.
/// <br />
/// A transition whose duration and delay add up to zero is not run at all, so it fires none of its events; an
/// animation of no duration still starts and ends, firing animationstart and animationend at once. A transition cut to
/// nothing - under prefers-reduced-motion, say - is cut to 0.01ms rather than to 0ms for the handler waiting on its
/// end to still run.
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
