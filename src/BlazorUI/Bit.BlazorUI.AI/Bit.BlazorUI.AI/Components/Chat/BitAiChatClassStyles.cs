namespace Bit.BlazorUI;

public class BitAiChatClassStyles
{
    /// <summary>
    /// Custom CSS classes/styles for the root element of the BitAiChat.
    /// </summary>
    public string? Root { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiMessageList (the transcript) of the BitAiChat.
    /// </summary>
    public BitAiMessageListClassStyles? MessageList { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiEmptyState of the BitAiChat.
    /// </summary>
    public BitAiEmptyStateClassStyles? EmptyState { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the footer of the BitAiChat, which holds the composer.
    /// </summary>
    public string? Footer { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the column inside the footer of the BitAiChat, which lines the composer up with the
    /// messages above it.
    /// </summary>
    public string? FooterColumn { get; set; }

    /// <summary>
    /// Custom CSS classes/styles for the BitAiScrollToLatest of the BitAiChat.
    /// </summary>
    public BitAiScrollToLatestClassStyles? ScrollToLatest { get; set; }
}
