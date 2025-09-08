
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class SystemPromptsPage
{
    [AutoInject] private IChatbotController chatbotController = default!;

    private List<SystemPromptDto>? systemPrompts;

    private bool isLoading = true;

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        try
        {
            systemPrompts = await chatbotController
                .WithQuery($"$orderby={nameof(SystemPromptDto.PromptKind)}")
                .GetSystemPrompts(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task SaveChanges(SystemPromptDto systemPrompt)
    {
        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken))
        {
            (await chatbotController.UpdateSystemPrompt(systemPrompt!, CurrentCancellationToken)).Patch(systemPrompt);
        }
    }
}
