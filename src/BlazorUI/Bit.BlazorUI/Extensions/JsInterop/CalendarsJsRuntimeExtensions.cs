namespace Bit.BlazorUI;

internal static class CalendarsJsRuntimeExtensions
{
    internal static ValueTask BitCalendarsSetup(this IJSRuntime jsRuntime, string id, bool trapFocus = false, string? componentId = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.setup", id, trapFocus, componentId);
    }

    internal static ValueTask BitCalendarsDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.dispose", id);
    }

    internal static ValueTask BitCalendarsFocusCell(this IJSRuntime jsRuntime, string cellId)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.focusCell", cellId);
    }
}
