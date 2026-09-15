using Microsoft.JSInterop;

namespace Bit.BlazorUI;

/// <summary>
/// Moves DOM focus onto a grid cell by id. The date grids use a roving tabindex - one cell in the
/// tab order at a time - so an arrow key has to carry the focus to the newly tabbable cell after the
/// re-render. Every failure is swallowed: focus is a convenience, and the circuit may be tearing
/// down or the target may have scrolled out of the rendered range.
/// </summary>
internal static class BitFcFocusInterop
{
    public static async ValueTask<bool> TryFocusAsync(
        IJSRuntime js,
        string elementId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await js.InvokeAsync<bool>("BitBlazorUI.FullCalendar.focusElement", cancellationToken, elementId);
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
