namespace Bit.BlazorUI;

internal static class CalendarsJsRuntimeExtensions
{
    internal static ValueTask BitCalendarsSetup(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.setup", id);
    }

    internal static ValueTask BitCalendarsDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.dispose", id);
    }

    internal static ValueTask BitCalendarsFocusDay(this IJSRuntime jsRuntime, string dayId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.focusDay", dayId);
    }
}
