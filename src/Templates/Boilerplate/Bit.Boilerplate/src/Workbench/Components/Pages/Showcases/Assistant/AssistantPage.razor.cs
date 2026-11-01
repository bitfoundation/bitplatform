namespace Boilerplate.Workbench.Components.Pages.Showcases.Assistant;

/// <summary>
/// A scripted AI chat built on bit BlazorUI's AI components. The sample history loads through the scenario state;
/// the user's own chats, the open chat, the model and the unsent prompt live in <c>AssistantStore</c>.
/// </summary>
public partial class AssistantPage
{
    // The sample conversations of the last load; the store's copies and deletions take precedence.
    private List<BitAiConversationSnapshot> seeded = [];
    private BitAiChatSession session = default!;
    private AssistantAiStrings strings = default!;
    private BitAiUiOptions uiOptions = default!;
    private BitAiError loadError = default!;
    private string[] suggestions = [];
    private bool isLoading = true;
    private bool isFailed;
    private AssistantChat[]? readChats;
    private List<BitAiConversationSnapshot> keptChats = [];

    [AutoInject] private AssistantStore store = default!;
    [AutoInject] private AssistantService assistantService = default!;

    private bool IsBlocked => isLoading || isFailed;

    private BitAiModel Model => assistantService.FindModel(store.ModelId);

    /// <summary>The store's chats as snapshots, read from their JSON again only when the store's list changes.</summary>
    private List<BitAiConversationSnapshot> KeptChats
    {
        get
        {
            if (ReferenceEquals(readChats, store.Chats) is false)
            {
                readChats = store.Chats;
                keptChats = [.. store.Chats.Select(chat => BitAiConversationSerializer.SnapshotFromJson(chat.Json))];
            }

            return keptChats;
        }
    }

    /// <summary>The user's kept chats, then the sample ones they have neither deleted nor kept a copy of.</summary>
    private IEnumerable<BitAiConversationSnapshot> Conversations =>
        KeptChats.Concat(seeded.Where(s => store.DeletedIds.Contains(s.ConversationId) is false && store.Chats.Any(c => c.ConversationId == s.ConversationId) is false));

    /// <summary>The history's rows, newest first; none while the history loads or after it failed.</summary>
    private IReadOnlyList<BitAiConversationSummary> Summaries => IsBlocked
        ? []
        : [.. Conversations.OrderByDescending(c => c.UpdatedAt).Select(c => new BitAiConversationSummary(c.ConversationId, c.Title, c.UpdatedAt, MessageCount: c.Messages.Count))];

    protected override async Task OnInitializedAsync()
    {
        strings = new(Localizer);
        uiOptions = BitAiUiOptions.Default with { Strings = strings, ShowUsage = true };
        loadError = new(Localizer[nameof(WorkbenchStrings.AssistantHistoryFailed)]) { IsRetryable = true };
        suggestions = [.. AssistantService.Topics.Select(topic => Localizer[topic.PromptKey].Value)];

        // A blank chat first, so the page renders while prerendering and while the history loads.
        Open(new BitAiConversation());
        Subscribe(store);

        if (IsPrerendering)
            return;

        await WhenStateRestored();
        await LoadConversations();
    }

    /// <summary>Loads the sample history through the scenario state, then reopens the chat the store had open.</summary>
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

    /// <summary>Opens a kept or sample conversation, or a new one when none has that id any more.</summary>
    private void Open(string? conversationId)
    {
        var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);

        Open(conversation is null ? new BitAiConversation() : BitAiConversationSerializer.ToConversation(conversation));
    }

    /// <summary>Replaces the session with one that drives the conversation, after closing the current one.</summary>
    private void Open(BitAiConversation conversation)
    {
        Close();

        session = assistantService.CreateSession(conversation, Model.ModelId, strings);
        session.TurnCompleted += OnTurnCompleted;
        conversation.MessageAdded += OnMessageAdded;
    }

    /// <summary>Lets go of the current session; a reply still streaming is stopped and kept as far as it got.</summary>
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
            Keep(session.Conversation);
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

    /// <summary>Starts a new chat, keeping the current session when it has no messages yet.</summary>
    private void NewChat()
    {
        store.ShowHistory(false);

        if (session.Conversation.Messages.Count > 0)
        {
            Open(new BitAiConversation());
        }

        store.Open(null);
    }

    /// <summary>Deletes a conversation; the store remembers its id, so a sample one stays hidden after a reload.</summary>
    private void Delete(BitAiConversationSummary summary)
    {
        if (summary.ConversationId == session.Conversation.ConversationId)
        {
            Open(new BitAiConversation());
        }

        store.Delete(summary.ConversationId);
    }

    /// <summary>Keeps the chosen model and uses it for the current session's next requests.</summary>
    private void ChooseModel(string modelId)
    {
        store.ChooseModel(modelId);
        session.Options = session.Options with { RequestOptions = new() { ModelId = modelId } };
    }

    private async Task Ask(string prompt) => await session.SubmitAsync(prompt);

    /// <summary>Keeps the chat as soon as the user sends a message, so a reload during the reply still shows the question.</summary>
    private void OnMessageAdded(BitAiConversation conversation, BitAiMessage message)
    {
        if (message.Role.IsUser)
        {
            _ = InvokeAsync(async () =>
            {
                // After the session has finished adding the message, such as giving a new chat its title.
                await Task.Yield();
                KeepCurrent();
            });
        }
    }

    /// <summary>Keeps the chat when a reply ends; a reply stopped before it wrote anything gets a note instead of staying empty.</summary>
    private void OnTurnCompleted(BitAiMessage reply) => _ = InvokeAsync(() =>
    {
        // Another conversation was opened while this waited for the renderer.
        if (session.Conversation.FindMessage(reply.MessageId) is null)
            return;

        if (reply.Status is BitAiMessageStatus.Cancelled && reply.Parts.Count is 0)
        {
            reply.AddPart(new BitAiTextPart($"*{Localizer[nameof(WorkbenchStrings.AssistantStopped)]}*"));
        }

        Settle(session.Conversation);
        KeepCurrent();
    });

    /// <summary>Completes the artifacts a stopped reply left streaming, so they don't show as still being written.</summary>
    private static void Settle(BitAiConversation conversation)
    {
        foreach (var artifact in conversation.Artifacts.Values.Where(a => a.IsStreaming))
        {
            artifact.Complete();
        }
    }

    /// <summary>Saves the open conversation into the store as the one to reopen.</summary>
    private void KeepCurrent()
    {
        Keep(session.Conversation);
        store.Open(session.Conversation.ConversationId);
    }

    /// <summary>Keeps the conversation in the store as the AI components' compact JSON.</summary>
    private void Keep(BitAiConversation conversation) => store.Keep(new(conversation.ConversationId, BitAiConversationSerializer.ToJson(conversation)));

    public override async ValueTask DisposeAsync()
    {
        Close();

        await base.DisposeAsync();
    }
}
