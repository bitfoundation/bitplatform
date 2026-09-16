namespace Bit.BlazorUI;

public class BitMessageBoxClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitMessageBox.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the BitMessageBox.
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the header of the BitMessageBox.
    /// </summary>
    public string? Header { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon container of the BitMessageBox.
    /// </summary>
    public string? IconContainer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of the BitMessageBox.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title of the BitMessageBox.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitSpacer of the BitMessageBox.
    /// </summary>
    public string? Spacer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the CloseButton of the BitMessageBox.
    /// </summary>
    public BitButtonClassStyles? CloseButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body of the BitMessageBox.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footer of the BitMessageBox.
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for every action button of the BitMessageBox, applied under the
    /// classes/styles of the specific button beside it.
    /// </summary>
    public BitButtonClassStyles? ActionButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the OkButton of the BitMessageBox.
    /// </summary>
    public BitButtonClassStyles? OkButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the CancelButton of the BitMessageBox.
    /// </summary>
    public BitButtonClassStyles? CancelButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the YesButton of the BitMessageBox.
    /// </summary>
    public BitButtonClassStyles? YesButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the NoButton of the BitMessageBox.
    /// </summary>
    public BitButtonClassStyles? NoButton { get; set; }
}
