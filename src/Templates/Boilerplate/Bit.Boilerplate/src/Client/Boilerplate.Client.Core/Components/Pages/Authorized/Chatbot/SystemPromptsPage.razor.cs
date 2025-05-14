
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Chatbot;

public partial class SystemPromptsPage
{
    [AutoInject] private IChatbotController chatbotController = default!;

    private SystemPromptDto? systemPrompt;

    private bool isLoading = true;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        try
        {
            systemPrompt = await chatbotController.GetSystemPromptMarkdown(PromptKind.Support, CurrentCancellationToken);
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
            (await chatbotController.Update(systemPrompt!, CurrentCancellationToken)).Patch(systemPrompt);
        }
    }
}
