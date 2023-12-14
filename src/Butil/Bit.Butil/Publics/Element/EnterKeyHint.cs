namespace Bit.Butil;

public enum EnterKeyHint
{
    /// <summary>
    /// The enterKeyHint value is not set.
    /// </summary>
    NotSet,

    /// <summary>
    /// Typically indicating inserting a new line.
    /// </summary>
    Enter,

    /// <summary>
    /// Typically meaning there is nothing more to input and the input method editor (IME) will be closed.
    /// </summary>
    Done,

    /// <summary>
    /// Typically meaning to take the user to the target of the text they typed.
    /// </summary>
    Go,

    /// <summary>
    /// Typically taking the user to the next field that will accept text.
    /// </summary>
    Next,

    /// <summary>
    /// Typically taking the user to the previous field that will accept text.
    /// </summary>
    Previous,

    /// <summary>
    /// Typically taking the user to the results of searching for the text they have typed.
    /// </summary>
    Search,

    /// <summary>
    /// Typically delivering the text to its target.
    /// </summary>
    Send
}
