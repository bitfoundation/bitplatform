namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The AI section of the Theme page: every bit BlazorUI AI component in its states, in the order of the demo site's AI
/// category, on the static samples of <see cref="AiSamples"/>. The two chats run on <see cref="AiSampleTransport"/>,
/// which only answers what is sent with a note, so nothing here calls a model.
/// </summary>
public partial class AiSection
{
    private AiSamples samples = default!;
    private BitAiUiOptions uiOptions = default!;
    // The options of the samples drawn open, or with line numbers, where the section's own options keep them closed.
    private BitAiUiOptions openToolCallsOptions = default!;
    private BitAiUiOptions openReasoningOptions = default!;
    private BitAiUiOptions lineNumbersOptions = default!;
    private BitAiChatSession emptyChatSession = default!;
    private BitAiChatSession chatSession = default!;
    // What the samples that can be used keep: the open conversation and what is left of the list, and the chosen model.
    private string? selectedConversationId = "weekly";
    private List<BitAiConversationSummary> conversations = [];
    private string? modelId = "thorough";

    private string PromptPlaceholder => Localizer[nameof(WorkbenchStrings.ThemeAiAskTheStoreAssistant)];

    private string CopyText => Localizer[nameof(WorkbenchStrings.ThemeAiOrderPO1042IsBooked)];

    private string CollapsibleTitle => Localizer[nameof(WorkbenchStrings.ThemeAiWhatTheAssistantCanSee)];

    private string CollapsibleText => Localizer[nameof(WorkbenchStrings.ThemeAiSalesStockAndTheTeam)];

    private string CitedSentence => Localizer[nameof(WorkbenchStrings.ThemeAiRotterdamGrewMostThisWeek3)];

    protected override void OnInitialized()
    {
        // The AI components' own texts in the view's language, as the assistant page has them, with the usage of each reply shown.
        var strings = new AssistantAiStrings(Localizer);

        uiOptions = BitAiUiOptions.Default with { Strings = strings, ShowUsage = true };
        openToolCallsOptions = uiOptions with { CollapseToolCalls = false };
        openReasoningOptions = uiOptions with { CollapseReasoning = false };
        lineNumbersOptions = uiOptions with { ShowLineNumbers = true };

        samples = new(Localizer);
        conversations = [.. samples.Conversations];

        var transport = new AiSampleTransport(Localizer[nameof(WorkbenchStrings.ThemeAiThisChatIsASample)]);
        var sessionOptions = new BitAiChatSessionOptions { Strings = strings, AutoTitle = false };

        emptyChatSession = new(transport, new BitAiConversation("empty-chat"), sessionOptions);
        chatSession = new(transport, samples.ChatConversation, sessionOptions);
    }

    private void SelectConversation(BitAiConversationSummary summary) => selectedConversationId = summary.ConversationId;

    /// <summary>Takes the conversation off the list; deleting the open one opens the first that is left.</summary>
    private void DeleteConversation(BitAiConversationSummary summary)
    {
        conversations = [.. conversations.Where(conversation => conversation.ConversationId != summary.ConversationId)];

        if (summary.ConversationId == selectedConversationId)
        {
            selectedConversationId = conversations.FirstOrDefault()?.ConversationId;
        }
    }

    /// <summary>Nothing to try again: the page calls no model, so the error stays as it is.</summary>
    private void Retry()
    {
    }

    /// <summary>Keeps the chip: the sample shows a chip that can be removed, and stays on the page to show it.</summary>
    private void KeepAttachment(BitAiAttachment attachment)
    {
    }

    public override async ValueTask DisposeAsync()
    {
        if (emptyChatSession is not null)
        {
            await emptyChatSession.DisposeAsync();
        }

        if (chatSession is not null)
        {
            await chatSession.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}
