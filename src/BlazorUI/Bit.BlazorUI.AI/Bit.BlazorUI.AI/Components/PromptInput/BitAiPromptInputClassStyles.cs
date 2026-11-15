namespace Bit.BlazorUI;

public class BitAiPromptInputClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiPromptInput.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the overlay of the BitAiPromptInput shown while files are dragged over it.
    /// </summary>
    public string? DropOverlay { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the list of the staged attachments of the BitAiPromptInput.
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each BitAiAttachmentChip of the staged attachments of the BitAiPromptInput.
    /// </summary>
    public BitAiAttachmentChipClassStyles? AttachmentChip { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitMessage of the BitAiPromptInput that names the files it turned away.
    /// </summary>
    public BitMessageClassStyles? Notice { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the menu of the slash commands and the mentions of the BitAiPromptInput.
    /// </summary>
    public string? Menu { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each option of the menu of the BitAiPromptInput.
    /// </summary>
    public string? MenuItem { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the icon of each option of the menu of the BitAiPromptInput.
    /// </summary>
    public string? MenuItemIcon { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the label of each option of the menu of the BitAiPromptInput.
    /// </summary>
    public string? MenuItemLabel { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the description of each option of the menu of the BitAiPromptInput.
    /// </summary>
    public string? MenuItemHint { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the row of the BitAiPromptInput that holds the prefix, the text and the tools.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the PrefixTemplate of the BitAiPromptInput.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the textarea of the BitAiPromptInput.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the container of the tools of the BitAiPromptInput: the attach button, the dictation,
    /// the SuffixTemplate and the send button.
    /// </summary>
    public string? Tools { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitButton of the BitAiPromptInput that opens the file picker.
    /// </summary>
    public BitButtonClassStyles? AttachButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiVoiceInput of the BitAiPromptInput.
    /// </summary>
    public BitAiVoiceInputClassStyles? VoiceInput { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitButton of the BitAiPromptInput that sends the prompt, or stops the turn in flight.
    /// </summary>
    public BitButtonClassStyles? SendButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the provisional text of the dictation of the BitAiPromptInput.
    /// </summary>
    public string? Interim { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footer of the BitAiPromptInput that holds the hint and the character counter.
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the hint of the BitAiPromptInput.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the character counter of the BitAiPromptInput.
    /// </summary>
    public string? Counter { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the visually hidden live region of the BitAiPromptInput that announces the characters
    /// left once the limit is close.
    /// </summary>
    public string? LiveRegion { get; set; }
}
