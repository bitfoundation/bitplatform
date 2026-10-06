namespace Bit.BlazorUI;

internal static class ElementReferenceExtensions
{
    /// <summary>
    /// Moves the focus to the element the way a component does it on its own behalf - after a key press,
    /// a click or a render - where failing to move it only leaves the focus where it already is.
    /// <br />
    /// So none of the reasons there is nothing to focus is an error: the element has not been rendered
    /// yet (no context), it has already left the document by the time the call reaches the browser
    /// (<see cref="JSException"/>: "Unable to focus an invalid element"), or the circuit is gone
    /// (<see cref="JSDisconnectedException"/>).
    /// <br />
    /// The public FocusAsync methods of the components do not use this: a caller asking for the focus
    /// is told when it could not be moved.
    /// </summary>
    internal static async ValueTask FocusSafelyAsync(this ElementReference element, bool preventScroll = false)
    {
        await element.TryFocusAsync(preventScroll);
    }

    /// <inheritdoc cref="FocusSafelyAsync"/>
    /// <returns>Whether the focus call reached the element, for a caller that has to undo what it set up
    /// for a focus move that never happened.</returns>
    internal static async ValueTask<bool> TryFocusAsync(this ElementReference element, bool preventScroll = false)
    {
        if (element.Context is null) return false;

        try
        {
            await element.FocusAsync(preventScroll);

            return true;
        }
        catch (JSDisconnectedException) { } // the circuit is gone, so is the element
        catch (InvalidOperationException) { } // the element reference is not attached to a renderer
        catch (JSException) { } // the element is no longer in the document

        return false;
    }
}
