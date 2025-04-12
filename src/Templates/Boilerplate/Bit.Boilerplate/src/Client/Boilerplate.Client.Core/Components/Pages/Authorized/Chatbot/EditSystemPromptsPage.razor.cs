
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Chatbot;

public partial class EditSystemPromptsPage
{
    [AutoInject] private IChatbotController chatbotController = default!;

    private string? systemPromptMarkdown;

    private bool isLoading = true;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        try
        {
            systemPromptMarkdown = await chatbotController.GetSystemPromptMarkdown(PromptKind.Support, CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task SaveChanges()
    {
        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken))
        {
            await chatbotController.UpdateSystemPrompt(new() { Kind = PromptKind.Support, Markdown = systemPromptMarkdown }, CurrentCancellationToken);
        }
    }
}
