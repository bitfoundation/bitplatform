namespace Bit.BlazorUI;

public class BitAiConversationListClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiConversationList.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the new-conversation button of the BitAiConversationList.
    /// </summary>
    public BitActionButtonClassStyles? NewButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the heading of each group of the BitAiConversationList.
    /// </summary>
    public string? GroupHeader { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the list of each group of the BitAiConversationList.
    /// </summary>
    public string? List { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for each item of the BitAiConversationList.
    /// </summary>
    public string? Item { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button each conversation of the BitAiConversationList is chosen with.
    /// </summary>
    public string? ItemButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the button of the conversation currently open in the BitAiConversationList.
    /// </summary>
    public string? SelectedItemButton { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the title of each conversation of the BitAiConversationList.
    /// </summary>
    public string? ItemTitle { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the delete button of each conversation of the BitAiConversationList.
    /// </summary>
    public BitButtonClassStyles? DeleteButton { get; set; }
}
