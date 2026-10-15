using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.BlazorUI;

/// <summary>
/// Focus management for the calendar's modal dialogs: moving focus into the dialog and trapping
/// Tab navigation within it while it is open, then restoring focus to the previously focused
/// element when it closes. All calls swallow the expected interop teardown exceptions so a
/// dialog disposing during circuit shutdown never surfaces an error.
/// <para>
/// A dialog is set up and torn down by a key of its own (any id unique to the instance) rather than by its element:
/// the teardown runs as the dialog is disposed, when its element has already left the DOM and an element reference
/// no longer resolves in the browser, so a teardown by element never found the dialog it was meant to close.
/// </para>
/// </summary>
internal static class BitFcDialogInterop
{
    public static async ValueTask SetupAsync(IJSRuntime js, ElementReference container, string key)
    {
        // Only suppress the cases that legitimately occur when the circuit is going away
        // (disconnect / cancellation). A JSException or InvalidOperationException here means the
        // focus-management interop is genuinely broken, so let it surface instead of silently
        // leaving the dialog without focus trapping.
        try
        {
            await js.InvokeVoidAsync("BitBlazorUI.FullCalendar.setupDialog", container, key);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
    }

    public static async ValueTask TeardownAsync(IJSRuntime js, string key)
    {
        try
        {
            await js.InvokeVoidAsync("BitBlazorUI.FullCalendar.teardownDialog", key);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
        catch (InvalidOperationException) { }
    }
}
