namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant;

/// <summary>The assistant's conversation list with its loading and empty states, beside the chat or in its side panel.</summary>
public partial class AssistantHistory
{
    [Parameter] public IReadOnlyList<BitAiConversationSummary> Conversations { get; set; } = [];

    [Parameter] public string? SelectedId { get; set; }

    [Parameter] public bool IsLoading { get; set; }

    [Parameter] public bool IsFailed { get; set; }

    [Parameter] public EventCallback<BitAiConversationSummary> OnSelect { get; set; }

    [Parameter] public EventCallback OnNewChat { get; set; }

    [Parameter] public EventCallback<BitAiConversationSummary> OnDelete { get; set; }
}
