namespace Boilerplate.Workbench.Components.Pages.Showcase.Assistant;

public partial class AssistantPage
{
    private List<AssistantConversation> conversations = [];
    private AssistantConversation? current;
    private ElementReference messagesElement;
    private CancellationTokenSource? streamCancellation;
    private bool isLoading = true;
    private bool isFailed;
    private bool isHistoryOpen;
    private string? prompt;

    [AutoInject] private AssistantService assistantService = default!;

    private bool CanSend => string.IsNullOrWhiteSpace(prompt) is false && streamCancellation is null && isLoading is false && isFailed is false;

    protected override Task OnInitializedAsync() => LoadConversations();

    private async Task LoadConversations()
    {
        isLoading = true;
        isFailed = false;

        try
        {
            conversations = await assistantService.GetConversations(CurrentCancellationToken);
            current = conversations.FirstOrDefault();
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
        current = null;
        isHistoryOpen = false;
    }

    private void Select(AssistantConversation conversation)
    {
        Stop();
        current = conversation;
        isHistoryOpen = false;
    }

    private Task Send() => CanSend ? Ask(prompt!.Trim()) : Task.CompletedTask;

    private async Task Ask(string text)
    {
        if (streamCancellation is not null)
            return;

        prompt = null;

        if (current is null)
        {
            current = new() { Id = Guid.NewGuid().ToString("N"), Title = text.Length > 48 ? $"{text[..48]}…" : text };
            conversations.Insert(0, current);
        }

        current.UpdatedAt = DateTimeOffset.Now;
        current.Messages.Add(new() { Role = AssistantRole.User, At = DateTimeOffset.Now, Text = text });

        var reply = new AssistantMessage { Role = AssistantRole.Assistant, At = DateTimeOffset.Now, Topic = assistantService.Match(text) };
        current.Messages.Add(reply);

        await Stream(reply);
    }

    private Task Retry(AssistantMessage message)
    {
        message.IsFailed = false;
        message.Text = string.Empty;

        return Stream(message);
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
