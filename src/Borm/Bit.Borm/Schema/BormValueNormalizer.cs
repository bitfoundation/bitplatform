namespace Bit.Borm;

/// <summary>
/// Tidies a value the user has just finished entering — trimming the stray spaces around a pasted
/// email, folding a reference number to upper case, stripping the punctuation out of a card number.
/// Returns the value to store, which may be the one it was given.
/// </summary>
/// <remarks>
/// <para>
/// This is the piece that is otherwise written four times in every application, always slightly
/// differently: a validator that rejects <c>" a@b.com "</c> is technically right and useless, and
/// trimming inside the rule fixes the message without fixing the data that is about to be saved.
/// React Hook Form spells the same idea <c>setValueAs</c>.
/// </para>
/// <para>
/// It runs when the user <em>leaves</em> the field, and again over every field on submit — never on
/// each keystroke, which would eat the space between two words as it is typed and put the caret
/// somewhere the user did not leave it.
/// </para>
/// </remarks>
public delegate object? BormValueNormalizer(object? value);
