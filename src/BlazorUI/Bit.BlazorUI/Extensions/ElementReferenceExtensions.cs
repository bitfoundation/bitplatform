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

    /// <summary>
    /// Moves the focus to the element a click has just landed on - or to the part of the very field it landed on
    /// that takes the focus - without scrolling to it. The click is what says it is in view, and a focus() that
    /// scrolls anyway scrolls the container of a trigger in a sticky header back to where the header sits in the
    /// flow: a jump of the page the click never asked for. A focus asked for any other way - an open through a
    /// method or a parameter, a focus handed back after a close - may well be for an element out of view, which
    /// the user has to be brought to, and goes through <see cref="FocusSafelyAsync"/> instead.
    /// </summary>
    internal static ValueTask FocusClickedSafelyAsync(this ElementReference element)
    {
        return element.FocusSafelyAsync(preventScroll: true);
    }
}
