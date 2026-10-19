namespace Bit.Borm;

/// <summary>
/// Which DOM event writes a field's value back to the model. This is the "when does the form know what
/// I typed" question every form library has to answer, and the two useful answers are "when I leave the
/// field" and "as I type".
/// </summary>
public enum BormUpdateTrigger
{
    /// <summary>
    /// The browser's <c>change</c> event. For a text box that means blur, which keeps the model quiet
    /// while the user is mid-word and is the right default for anything with an expensive rule.
    /// </summary>
    Change,

    /// <summary>
    /// The browser's <c>input</c> event: every keystroke. Pair it with
    /// <see cref="BormFieldDefinition.DebounceMilliseconds"/> when validation or a computed value
    /// is doing real work on each change.
    /// </summary>
    Input
}
