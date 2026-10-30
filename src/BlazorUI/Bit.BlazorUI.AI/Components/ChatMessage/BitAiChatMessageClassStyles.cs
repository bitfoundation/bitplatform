namespace Bit.BlazorUI;

public class BitAiChatMessageClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiChatMessage.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the pill a system message is shown in by the BitAiChatMessage.
    /// </summary>
    public string? System { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the attachments of a user message of the BitAiChatMessage.
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the bubble a user message is shown in by the BitAiChatMessage.
    /// </summary>
    public string? Bubble { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the actions of a user message (copy and edit) of the BitAiChatMessage.
    /// </summary>
    public string? Actions { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the edit button of a user message of the BitAiChatMessage.
    /// </summary>
    public BitButtonClassStyles? EditButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the editor a user message is edited in by the BitAiChatMessage.
    /// </summary>
    public string? Editor { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the row of buttons under the editor of the BitAiChatMessage.
    /// </summary>
    public string? EditorActions { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the cancel button of the editor of the BitAiChatMessage.
    /// </summary>
    public BitButtonClassStyles? CancelButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the save button of the editor of the BitAiChatMessage.
    /// </summary>
    public BitButtonClassStyles? SaveButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the body of an assistant message of the BitAiChatMessage.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiMessageActions under an assistant message of the BitAiChatMessage.
    /// </summary>
    public BitAiMessageActionsClassStyles? MessageActions { get; set; }
}
