namespace Bit.BlazorUI;

/// <summary>
/// The set of parameters a <see cref="BitMessageBox"/> shown through <see cref="BitMessageBoxService.Prompt(BitMessageBoxPromptParameters)"/>
/// is customized with: everything a <see cref="BitMessageBoxParameters"/> sets, plus the text field the answer is typed into.
/// </summary>
/// <remarks>
/// The <see cref="BitMessageBoxParameters.Body"/> (or <see cref="BitMessageBoxParameters.BodyTemplate"/>) is the question,
/// set above the field, and it is also what names the field when no <see cref="Label"/> is given. The field takes the focus
/// when the box opens, with its <see cref="Value"/> selected, so <see cref="BitMessageBoxParameters.AutoFocus"/> is not used.
/// </remarks>
public class BitMessageBoxPromptParameters : BitMessageBoxParameters
{
    /// <summary>
    /// Checks the answer against something only reachable asynchronously - a server that knows whether a name is taken -
    /// after <see cref="Required"/> and <see cref="Validator"/> have accepted it: return an error message to refuse it and
    /// show the message under the field, or <c>null</c> to accept it.
    /// </summary>
    /// <remarks>
    /// It runs only when the box is answered (the affirmative button, or Enter in the field), with the field showing that it
    /// is busy, and the value it accepted is the one handed back - an edit made while it runs is not. An edit after it refused
    /// a value takes its message away, since the message is about a value that is no longer there.
    /// <br/>
    /// The value is never <c>null</c>: an empty field is checked as the empty string it is handed back as. The token is
    /// cancelled when the check is given up on - the Cancel or the close button pressed while it runs, the Escape key, the
    /// token of the showing - so a server call made with it stops with the box. An exception it throws (other than the
    /// cancellation of that token) closes the box and is rethrown to the caller of the Prompt.
    /// </remarks>
    public Func<string, CancellationToken, Task<string?>>? AsyncValidator { get; set; }

    /// <summary>
    /// The value of the autocomplete attribute of the text field, such as <c>off</c>, <c>username</c> or
    /// <c>current-password</c>, which is what a browser or a password manager fills it in by.
    /// </summary>
    public string? AutoComplete { get; set; }

    /// <summary>
    /// Adds a button to a <see cref="BitInputType.Password"/> field that shows what was typed.
    /// </summary>
    public bool? CanRevealPassword { get; set; }

    /// <summary>
    /// The help text shown under the text field, which the field is also described by.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of the text field, such as <see cref="BitInputType.Password"/> or <see cref="BitInputType.Email"/>.
    /// The default is <see cref="BitInputType.Text"/>.
    /// </summary>
    public BitInputType? InputType { get; set; }

    /// <summary>
    /// The visible label of the text field. Without one the question in the body names the field.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The maximum number of characters the text field accepts.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Renders a multi-line text field, where Enter starts a new line and Ctrl+Enter answers the box.
    /// </summary>
    public bool? Multiline { get; set; }

    /// <summary>
    /// The placeholder of the text field.
    /// </summary>
    public string? Placeholder { get; set; }

    /// <summary>
    /// Refuses an empty or white-space answer, showing <see cref="RequiredMessage"/> under the field.
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// The error message a <see cref="Required"/> field shows when it is left empty. The default is "A value is required.".
    /// </summary>
    public string? RequiredMessage { get; set; }

    /// <summary>
    /// Checks the answer before it is accepted: return an error message to refuse it and show the message under the field,
    /// or <c>null</c> to accept it.
    /// </summary>
    /// <remarks>
    /// It runs when the affirmative button is pressed (or Enter in the field), and - once an answer has been refused - on
    /// every edit after that, so the message goes away as soon as the value is fixed. Dismissing the box is never refused.
    /// The value is never <c>null</c>: an empty field is checked as the empty string it is handed back as. An exception it
    /// throws closes the box and is rethrown to the caller of the Prompt.
    /// </remarks>
    public Func<string, string?>? Validator { get; set; }

    /// <summary>
    /// The initial value of the text field.
    /// </summary>
    public string? Value { get; set; }
}
