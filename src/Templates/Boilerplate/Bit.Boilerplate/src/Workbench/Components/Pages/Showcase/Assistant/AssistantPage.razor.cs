namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant;

public partial class AssistantPage
{
    private List<AssistantConversation> conversations = [];
    private ElementReference messagesElement;
    private CancellationTokenSource? streamCancellation;
    private bool isLoading = true;
    private bool isFailed;

    [AutoInject] private AssistantStore store = default!;
    [AutoInject] private AssistantService assistantService = default!;

    private AssistantConversation? CurrentConversation => conversations.FirstOrDefault(c => c.Id == store.CurrentId);

    private string? Prompt => store.Prompt;

    private bool CanSend => string.IsNullOrWhiteSpace(Prompt) is false && streamCancellation is null && isLoading is false && isFailed is false;

    protected override async Task OnInitializedAsync()
    {
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
            var seeded = await assistantService.GetConversations(CurrentCancellationToken);
            var kept = store.Conversations.Select(Settle).ToList();

            conversations = [.. kept.Concat(seeded.Where(s => kept.Any(k => k.Id == s.Id) is false)).OrderByDescending(c => c.UpdatedAt)];
            isLoading = false;
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

    private void NewChat()
    {
        Stop();
        store.Open(null);
        store.ShowHistory(false);
    }

    private void Select(AssistantConversation conversation)
    {
        Stop();
        store.Open(conversation.Id);
        store.ShowHistory(false);
    }

    private Task Send() => CanSend ? Ask(Prompt!.Trim()) : Task.CompletedTask;

    private async Task Ask(string text)
    {
        if (streamCancellation is not null)
            return;

        store.Write(null);

        var conversation = CurrentConversation;

        if (conversation is null)
        {
            conversation = new() { Id = Guid.NewGuid().ToString("N"), Title = text.Length > 48 ? $"{text[..48]}…" : text };
            conversations.Insert(0, conversation);
        }

        conversation.UpdatedAt = DateTimeOffset.Now;
        conversation.Messages.Add(new() { Role = AssistantRole.User, At = DateTimeOffset.Now, Text = text });

        var reply = new AssistantMessage { Role = AssistantRole.Assistant, At = DateTimeOffset.Now, Topic = assistantService.Match(text) };
        conversation.Messages.Add(reply);

        store.Keep(conversation);

        await Stream(reply);
    }

    private Task Retry(AssistantMessage message)
    {
        message.IsFailed = false;
        message.Text = string.Empty;

        return Stream(message);
    }

    private void Rate(AssistantMessage message, bool? isHelpful)
    {
        message.IsHelpful = message.IsHelpful == isHelpful ? null : isHelpful;

        if (CurrentConversation is { } conversation)
        {
            store.Keep(conversation);
        }
    }

    private static AssistantConversation Settle(AssistantConversation conversation)
    {
        foreach (var message in conversation.Messages.Where(m => m.IsStreaming))
        {
            message.IsStreaming = false;
            message.IsFailed = string.IsNullOrEmpty(message.Text);
        }

        return conversation;
    }

    private async Task Stream(AssistantMessage reply)
    {
        var topic = reply.Topic ?? AssistantService.Fallback;
        var lastRender = DateTimeOffset.UtcNow;

        streamCancellation = CancellationTokenSource.CreateLinkedTokenSource(CurrentCancellationToken);
        reply.IsStreaming = true;
        reply.Card = AssistantCardKind.None;

        StateHasChanged();
        await ScrollToEnd();

        try
        {
            await foreach (var chunk in assistantService.StreamAnswer(topic, streamCancellation.Token))
            {
                reply.Text += chunk;

                if (DateTimeOffset.UtcNow - lastRender > TimeSpan.FromMilliseconds(60))
                {
                    lastRender = DateTimeOffset.UtcNow;
                    StateHasChanged();
                    await ScrollToEnd();
                }
            }

            reply.Card = topic.Card;
        }
        catch (OperationCanceledException)
        {
            if (string.IsNullOrEmpty(reply.Text))
            {
                reply.Text = $"*{Localizer[nameof(WorkbenchStrings.AssistantStopped)]}*";
            }
        }
        finally
        {
            reply.IsStreaming = false;
            streamCancellation.Dispose();
            streamCancellation = null;
        }

        if (CurrentCancellationToken.IsCancellationRequested)
            return;

        if (conversations.FirstOrDefault(c => c.Messages.Contains(reply)) is { } conversation)
        {
            store.Keep(conversation);
        }

        StateHasChanged();
        await ScrollToEnd();
    }

    private void Stop() => streamCancellation?.Cancel();

    private async Task Copy(AssistantMessage message)
    {
        await JSRuntime.InvokeVoidAsync("workbench.copy", message.Text);
    }

    private async Task ScrollToEnd()
    {
        if (CurrentCancellationToken.IsCancellationRequested)
            return;

        await JSRuntime.InvokeVoidAsync("workbench.scrollToEnd", messagesElement);
    }

    public override async ValueTask DisposeAsync()
    {
        streamCancellation?.Cancel();

        await base.DisposeAsync();
    }
}
