namespace Bit.BlazorUI;

internal static class ElementReferenceExtensions
{
    /// <summary>
    /// Moves the focus to the element the way a component does it on its own behalf (after a key press, a click
    /// or a render), where failing to move it only leaves the focus where it already is.
    /// </summary>
    internal static async ValueTask FocusSafelyAsync(this ElementReference element, bool preventScroll = false)
    {
        try
        {
            await element.FocusAsync(preventScroll);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, nothing to focus
        catch (JSException) { } // the element is no longer in the document, failing to focus it is not fatal
        catch (InvalidOperationException) { } // the element has not been rendered yet, so there is nothing to focus
        catch (OperationCanceledException) { } // the interop call timed out or was cancelled
    }
}
