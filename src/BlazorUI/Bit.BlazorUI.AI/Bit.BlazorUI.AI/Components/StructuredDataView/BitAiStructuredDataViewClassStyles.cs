namespace Bit.BlazorUI;

public class BitAiStructuredDataViewClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiStructuredDataView.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the list of labelled values an object is shown as by the BitAiStructuredDataView.
    /// </summary>
    public string? List { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each entry - a label and its value - of the list of the BitAiStructuredDataView.
    /// </summary>
    public string? Entry { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label of each entry of the list of the BitAiStructuredDataView.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the value of each entry of the list of the BitAiStructuredDataView.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the dash a missing value is shown as in the list of the BitAiStructuredDataView.
    /// </summary>
    public string? Empty { get; set; }
}
