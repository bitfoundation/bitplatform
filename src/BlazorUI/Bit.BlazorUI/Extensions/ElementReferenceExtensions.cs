namespace Bit.BlazorUI;

internal static class ElementReferenceExtensions
{
    /// <summary>
    /// Moves the focus to the element the way a component does it on its own behalf - after a key press,
    /// a click or a render - where failing to move it only leaves the focus where it already is.
    /// <br />
    /// So none of the reasons there is nothing to focus is an error: the element has not been rendered
    /// yet (no context), it has already left the document by the time the call reaches the browser
    /// (<see cref="JSException"/>: "Unable to focus an invalid element"), the call timed out or was
    /// cancelled (<see cref="OperationCanceledException"/>), or the circuit is gone
    /// (<see cref="JSDisconnectedException"/>).
    /// <br />
    /// The public FocusAsync methods of the components do not use this: a caller asking for the focus
    /// is told when it could not be moved.
    /// </summary>
    /// <remarks>
    /// It sits on the keyboard paths (every digit typed into an OTP input, every arrow key of a roving
    /// tab stop), so it awaits the focus call directly rather than through a delegate.
    /// </remarks>
    internal static async ValueTask FocusSafelyAsync(this ElementReference element, bool preventScroll = false)
    {
        if (element.Context is null) return;

        try
        {
            await element.FocusAsync(preventScroll);
        }
        catch (Exception ex) when (FocusSafely.IsTolerated(ex)) { }
    }

    /// <inheritdoc cref="FocusSafelyAsync(ElementReference, bool)"/>
    /// <returns>What became of the focus call, for a caller that falls back to another element when this
    /// one could not take the focus, or has to undo what it set up for a focus move that never happened.</returns>
    internal static async ValueTask<FocusAttempt> TryFocusAsync(this ElementReference element, bool preventScroll = false)
    {
        if (element.Context is null) return FocusAttempt.Missed;

        try
        {
            await element.FocusAsync(preventScroll);

            return FocusAttempt.Focused;
        }
        catch (Exception ex) when (FocusSafely.IsTolerated(ex))
        {
            return FocusSafely.ToAttempt(ex);
        }
    }

    /// <summary>
    /// Moves the focus the way a public FocusAsync does it: a caller asking for the focus is told when an element
    /// that is there could not take it, but a control that was never rendered (hidden by a parameter, or a first
    /// render that has not happened yet) leaves an empty reference behind, which has nothing to focus rather than
    /// something that failed.
    /// </summary>
    internal static ValueTask FocusIfRenderedAsync(this ElementReference element, bool preventScroll = false)
    {
        return element.Context is null ? ValueTask.CompletedTask : element.FocusAsync(preventScroll);
    }

    /// <summary>
    /// <see cref="TryFocusAsync(ElementReference, bool)"/> for a caller that acts on the focus having actually
    /// landed (a focus ring put back on, a focus event it expects to be consumed): an element that refuses the
    /// focus (inert, hidden by a collapsed container, disabled) does not make the focus call throw, it only
    /// leaves the focus where it was, so the browser is asked whether the element holds it afterwards.
    /// </summary>
    internal static async ValueTask<FocusAttempt> TryFocusConfirmedAsync(this ElementReference element, IJSRuntime js, bool preventScroll = false)
    {
        var attempt = await element.TryFocusAsync(preventScroll);
        if (attempt is not FocusAttempt.Focused) return attempt;

        try
        {
            return await js.BitUtilsIsActiveElement(element) ? FocusAttempt.Focused : FocusAttempt.Missed;
        }
        catch (Exception ex) when (FocusSafely.IsTolerated(ex))
        {
            return FocusSafely.ToAttempt(ex);
        }
    }
}

/// <summary>
/// What became of a focus move made through <see cref="FocusSafely"/>.
/// </summary>
internal enum FocusAttempt
{
    /// <summary>
    /// The focus call reached the element. Only <see cref="ElementReferenceExtensions.TryFocusConfirmedAsync"/>
    /// also checks that the element took it: one that refuses the focus without throwing reports this too.
    /// </summary>
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
        try
        {
            await focus();
        }
        catch (Exception ex) when (IsTolerated(ex)) { }
    }

    internal static async ValueTask<FocusAttempt> TryAsync(Func<ValueTask> focus)
    {
        try
        {
            await focus();

            return FocusAttempt.Focused;
        }
        catch (Exception ex) when (IsTolerated(ex))
        {
            return ToAttempt(ex);
        }
    }

    // The failures a focus move tolerates; only the circuit going away also ends every move a caller would fall
    // back to, which is what ToAttempt tells apart.
    internal static bool IsTolerated(Exception ex) => ex is JSDisconnectedException // the circuit is gone, so is the element
                                                          or InvalidOperationException // the element reference is not attached to a renderer (ObjectDisposedException included)
                                                          or OperationCanceledException // the interop call timed out or was cancelled
                                                          or JSException; // the element is no longer in the document

    internal static FocusAttempt ToAttempt(Exception ex) => ex is JSDisconnectedException ? FocusAttempt.Disconnected : FocusAttempt.Missed;
}
