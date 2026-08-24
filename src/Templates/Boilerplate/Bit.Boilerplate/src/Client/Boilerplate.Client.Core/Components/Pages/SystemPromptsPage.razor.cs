
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Pages;

public partial class SystemPromptsPage
{
    [AutoInject] private IChatbotController chatbotController = default!;

    private List<SystemPromptDto>? systemPrompts;

    private bool isLoading = true;

    /// <summary>
    /// The tab header, localized rather than the raw enum member name - a tab that reads "AnalyzeProductImage" is
    /// neither spaced nor translatable. A new PromptKind falls back to its name until it gets its own key.
    /// </summary>
    private string GetPromptKindText(PromptKind promptKind) => promptKind switch
    {
        PromptKind.Support => Localizer[nameof(AppStrings.PromptKindSupport)],
        PromptKind.AnalyzeProductImage => Localizer[nameof(AppStrings.PromptKindAnalyzeProductImage)],
        _ => promptKind.ToString()
    };

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
