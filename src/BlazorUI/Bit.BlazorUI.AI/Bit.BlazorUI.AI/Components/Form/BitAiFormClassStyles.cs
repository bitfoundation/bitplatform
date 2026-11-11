namespace Bit.BlazorUI;

public class BitAiFormClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiForm.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title of the BitAiForm.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the description under the title of the BitAiForm.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the wrapper of each single field (one input) of the BitAiForm.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each group of fields (the fieldset of a nested object or a list) of the BitAiForm.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the legend of each group of fields of the BitAiForm.
    /// </summary>
    public string? Legend { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the help text of each group of fields of the BitAiForm.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each entry of a list of the BitAiForm.
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the fields of each entry of a list of the BitAiForm.
    /// </summary>
    public string? ItemBody { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button removing an entry of a list of the BitAiForm.
    /// </summary>
    public BitButtonClassStyles? RemoveButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button adding an entry to a list of the BitAiForm.
    /// </summary>
    public BitButtonClassStyles? AddButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the error messages the BitAiForm renders itself: the one of a list, and the one of
    /// a checkbox.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the actions of the BitAiForm.
    /// </summary>
    public string? Actions { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the cancel button of the BitAiForm.
    /// </summary>
    public BitButtonClassStyles? CancelButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the submit button of the BitAiForm.
    /// </summary>
    public BitButtonClassStyles? SubmitButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the confirmation shown in place of the actions once the BitAiForm is submitted.
    /// </summary>
    public string? Submitted { get; set; }
}
