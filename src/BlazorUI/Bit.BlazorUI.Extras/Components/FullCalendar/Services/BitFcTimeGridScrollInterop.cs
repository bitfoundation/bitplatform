using Microsoft.JSInterop;

namespace Bit.BlazorUI;

internal static class BitFcTimeGridScrollInterop
{
    public static async ValueTask<bool> TryScrollToStartOfDayAsync(
        IJSRuntime js,
        string elementId,
        int startOfDayHour,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync(
                "BitBlazorUI.FullCalendar.scrollToHour",
                cancellationToken,
                elementId,
                startOfDayHour,
                BitFullCalendarHelpers.HourHeightPx);
            return true;
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (JSException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}

