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
    /// </remarks>
    public Func<string?, string?>? Validator { get; set; }

    /// <summary>
    /// The initial value of the text field.
    /// </summary>
    public string? Value { get; set; }
}
