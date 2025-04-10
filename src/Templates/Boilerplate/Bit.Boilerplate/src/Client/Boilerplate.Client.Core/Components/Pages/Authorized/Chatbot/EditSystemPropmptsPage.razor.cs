
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Chatbot;

public partial class EditSystemPropmptsPage
{
    [AutoInject] private IChatbotController chatbotController = default!;

    private string? systemPromptMarkdown;
    private string? summarizeConversationContextPrompt;

    private bool isLoading = true;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        try
        {
            (systemPromptMarkdown, summarizeConversationContextPrompt) = await (chatbotController.GetSystemPromptMarkdown(PromptKind.Support, CurrentCancellationToken),
            chatbotController.GetSystemPromptMarkdown(PromptKind.SummarizeConversationContext, CurrentCancellationToken));
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task SaveChanges()
    {
        await Task.WhenAll(chatbotController.UpdateSystemPrompt(new() { Kind = PromptKind.Support, Markdown = systemPromptMarkdown }, CurrentCancellationToken),
            chatbotController.UpdateSystemPrompt(new() { Kind = PromptKind.SummarizeConversationContext, Markdown = summarizeConversationContextPrompt }, CurrentCancellationToken));
    }
}
