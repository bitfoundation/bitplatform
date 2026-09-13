namespace Bit.BlazorUI;

/// <summary>
/// The custom CSS classes/styles for the different parts of the <see cref="BitLabel"/> component.
/// </summary>
public class BitLabelClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the label.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the required indicator of the label.
    /// </summary>
    /// <remarks>
    /// The indicator is the element the asterisk - or whatever <see cref="BitLabel.RequiredText"/> or
    /// <see cref="BitLabel.RequiredTemplate"/> puts in its place - is rendered into, and it only exists while
    /// <see cref="BitLabel.Required"/> is set.
    /// </remarks>
    public string? RequiredIndicator { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the optional indicator of the label.
    /// </summary>
    /// <remarks>
    /// The indicator is the element the "(optional)" text - or whatever <see cref="BitLabel.OptionalText"/> or
    /// <see cref="BitLabel.OptionalTemplate"/> puts in its place - is rendered into, and it only exists while
    /// <see cref="BitLabel.Optional"/> is set and <see cref="BitLabel.Required"/> is not.
    /// </remarks>
    public string? OptionalIndicator { get; set; }
}
