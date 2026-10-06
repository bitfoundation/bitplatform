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

    /// <inheritdoc cref="FocusSafelyAsync(ElementReference, bool)"/>
    /// <returns>What became of the focus call, for a caller that falls back to another element when this
    /// one could not take the focus, or has to undo what it set up for a focus move that never happened.</returns>
    internal static ValueTask<FocusAttempt> TryFocusAsync(this ElementReference element, bool preventScroll = false)
    {
        if (element.Context is null) return ValueTask.FromResult(FocusAttempt.Missed);

        return FocusSafely.TryAsync(() => element.FocusAsync(preventScroll));
    }
}

/// <summary>
/// What became of a focus move made through <see cref="FocusSafely"/>.
/// </summary>
internal enum FocusAttempt
{
    /// <summary>The focus call reached the element.</summary>
    Focused,

    /// <summary>This element could not take the focus, another one still can.</summary>
    Missed,

    /// <summary>The circuit is gone, so there is nothing left to focus at all.</summary>
    Disconnected
}

/// <summary>
/// The same tolerance as <see cref="ElementReferenceExtensions.FocusSafelyAsync(ElementReference, bool)"/>
/// for a focus move that goes through a component (its own FocusAsync) rather than an element reference.
/// </summary>
internal static class FocusSafely
{
    internal static async ValueTask RunAsync(Func<ValueTask> focus)
    {
        await TryAsync(focus);
    }

    internal static async ValueTask<FocusAttempt> TryAsync(Func<ValueTask> focus)
    {
        try
        {
            await focus();

            return FocusAttempt.Focused;
        }
        catch (JSDisconnectedException) // the circuit is gone, so is the element
        {
            return FocusAttempt.Disconnected;
        }
        catch (InvalidOperationException) { } // the element reference is not attached to a renderer
        catch (JSException) { } // the element is no longer in the document

        return FocusAttempt.Missed;
    }
}
