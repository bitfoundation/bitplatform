namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant;

public partial class AssistantPage
{
    private List<BitAiConversationSnapshot> seeded = [];
    private BitAiChatSession session = default!;
    private AssistantAiStrings strings = default!;
    private BitAiUiOptions uiOptions = default!;
    private BitAiError loadError = default!;
    private string[] suggestions = [];
    private bool isLoading = true;
    private bool isFailed;

    [AutoInject] private AssistantStore store = default!;
    [AutoInject] private AssistantService assistantService = default!;

    private bool IsBlocked => isLoading || isFailed;

    private BitAiModel Model => assistantService.FindModel(store.ModelId);

    private IEnumerable<BitAiConversationSnapshot> Conversations =>
        store.Conversations.Concat(seeded.Where(s => store.DeletedIds.Contains(s.ConversationId) is false && store.Conversations.Any(c => c.ConversationId == s.ConversationId) is false));

    private IReadOnlyList<BitAiConversationSummary> Summaries => IsBlocked
        ? []
        : [.. Conversations.OrderByDescending(c => c.UpdatedAt).Select(c => new BitAiConversationSummary(c.ConversationId, c.Title, c.UpdatedAt, MessageCount: c.Messages.Count))];

    protected override async Task OnInitializedAsync()
    {
        strings = new(Localizer);
        uiOptions = BitAiUiOptions.Default with { Strings = strings, ShowUsage = true };
        loadError = new(Localizer[nameof(WorkbenchStrings.AssistantHistoryFailed)]) { IsRetryable = true };
        suggestions = [.. AssistantService.Topics.Select(topic => Localizer[topic.PromptKey].Value)];

        Open(new BitAiConversation());
        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();
        await LoadConversations();
    }

    private async Task LoadConversations()
    {
        isLoading = true;
        isFailed = false;

        try
        {
            seeded = await assistantService.GetConversations(CurrentCancellationToken);
            isLoading = false;

            Open(store.CurrentId);
        }
        catch (WorkbenchSimulatedException)
        {
            isFailed = true;
            isLoading = false;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Open(string? conversationId)
    {
        var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);

        Open(conversation is null ? new BitAiConversation() : BitAiConversationSerializer.ToConversation(conversation));
    }

    private void Open(BitAiConversation conversation)
    {
        Close();

        session = assistantService.CreateSession(conversation, Model.ModelId, strings);
        session.TurnCompleted += OnTurnCompleted;
        conversation.MessageAdded += OnMessageAdded;
    }

    private void Close()
    {
        if (session is null)
            return;

        session.TurnCompleted -= OnTurnCompleted;
        session.Conversation.MessageAdded -= OnMessageAdded;

        if (session.IsGenerating)
        {
            session.Stop();
            Settle(session.Conversation);
            store.Keep(BitAiConversationSerializer.ToSnapshot(session.Conversation));
        }

        _ = session.DisposeAsync().AsTask();
    }

    private void Select(BitAiConversationSummary summary)
    {
        store.ShowHistory(false);

        if (summary.ConversationId == session.Conversation.ConversationId)
            return;

        Open(summary.ConversationId);
        store.Open(summary.ConversationId);
    }

    private void NewChat()
    {
        store.ShowHistory(false);

        if (session.Conversation.Messages.Count > 0)
        {
            Open(new BitAiConversation());
        }

        store.Open(null);
    }

    private void Delete(BitAiConversationSummary summary)
    {
        if (summary.ConversationId == session.Conversation.ConversationId)
        {
            Open(new BitAiConversation());
        }

        store.Delete(summary.ConversationId);
    }

    private void ChooseModel(string modelId)
    {
        store.ChooseModel(modelId);
        session.Options = session.Options with { RequestOptions = new() { ModelId = modelId } };
    }

    private async Task Ask(string prompt) => await session.SubmitAsync(prompt);

    private void OnMessageAdded(BitAiConversation conversation, BitAiMessage message)
    {
        if (message.Role.IsUser)
        {
            _ = InvokeAsync(async () =>
            {
                await Task.Yield();
                KeepCurrent();
            });
        }
    }

    private void OnTurnCompleted(BitAiMessage reply) => _ = InvokeAsync(() =>
    {
        if (session.Conversation.FindMessage(reply.MessageId) is null)
            return;

        if (reply.Status is BitAiMessageStatus.Cancelled && reply.Parts.Count is 0)
        {
            reply.AddPart(new BitAiTextPart($"*{Localizer[nameof(WorkbenchStrings.AssistantStopped)]}*"));
        }

        Settle(session.Conversation);
        KeepCurrent();
    });

    private static void Settle(BitAiConversation conversation)
    {
        foreach (var artifact in conversation.Artifacts.Values.Where(a => a.IsStreaming))
        {
            artifact.Complete();
        }
    }

    private void KeepCurrent()
    {
        store.Keep(BitAiConversationSerializer.ToSnapshot(session.Conversation));
        store.Open(session.Conversation.ConversationId);
    }

    public override async ValueTask DisposeAsync()
    {
        Close();

        await base.DisposeAsync();
    }
}
