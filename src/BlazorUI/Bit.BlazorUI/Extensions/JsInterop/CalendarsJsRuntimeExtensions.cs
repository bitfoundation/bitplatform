namespace Bit.BlazorUI;

internal static class CalendarsJsRuntimeExtensions
{
    internal static ValueTask BitCalendarsSetup(this IJSRuntime jsRuntime, string id, string? componentId = null)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.setup", id, componentId);
    }

    internal static ValueTask BitCalendarsDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.dispose", id);
    }

    internal static ValueTask BitCalendarsFocusCell(this IJSRuntime jsRuntime, string cellId, bool preventScroll = false)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.Calendars.focusCell", cellId, preventScroll);
    }
}
