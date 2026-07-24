using Microsoft.JSInterop;

namespace Bit.BlazorUI;

internal static class BitFcTimelineScrollInterop
{
    public static async ValueTask<bool> TryScrollToTargetAsync(
        IJSRuntime js,
        string scrollContainerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<bool>(
                "BitBlazorUI.FullCalendar.scrollTimelineToTarget",
                cancellationToken,
                scrollContainerId);
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
