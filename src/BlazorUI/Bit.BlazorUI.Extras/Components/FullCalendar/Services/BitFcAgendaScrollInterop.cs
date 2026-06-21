using System.Globalization;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

internal static class BitFcAgendaScrollInterop
{
    public static async ValueTask<bool> TryScrollToDateAsync(
        IJSRuntime js,
        string scrollContainerId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await js.InvokeVoidAsync(
                "BitBlazorUI.FullCalendar.scrollAgendaToDate",
                cancellationToken,
                scrollContainerId,
                date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
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
