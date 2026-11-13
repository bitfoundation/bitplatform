//+:cnd:noEmit
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout.AiChat;

/// <summary>
/// How the AI chat panel shows the chatbot's tool calls (See AppChatbot.Tools.cs). The server runs them and sends no
/// more than their names, so every word and icon of them is the panel's, in the user's language.
/// </summary>
internal static class AppAiChatTools
{
    /// <summary>
    /// The calls the user sees: as a line saying what the assistant did, a card of their own (products, a form, an
    /// approval), or nothing at all - the ones that only feed the model, and the suggestions, which show under the answer.
    /// </summary>
    public static BitAiToolCallDisplay Display(BitAiToolCallPart call) => call.ToolName switch
    {
        AiChatProtocol.Tools.GetCurrentDateTime or AiChatProtocol.Tools.GetAppPages or AiChatProtocol.Tools.ShowFollowUpSuggestions => BitAiToolCallDisplay.Hidden,
        AiChatProtocol.Tools.RequestHumanFollowUp or AiChatProtocol.Tools.ClearAppFiles => BitAiToolCallDisplay.Card,
        //#if (module == "Sales")
        //#if (database == "PostgreSQL" || database == "SqlServer")
        AiChatProtocol.Tools.ShowProducts => BitAiToolCallDisplay.Card,
        //#endif
        //#endif
        _ => BitAiToolCallDisplay.Status
    };

    /// <summary>The title, icon and risk a call is shown with. Null leaves the tool's own name, for a tool added without a line here.</summary>
    public static BitAiToolDefinition? Describe(string toolName, IStringLocalizer<AppStrings> localizer) => toolName switch
    {
        AiChatProtocol.Tools.NavigateToPage => new(toolName) { Title = localizer[nameof(AppStrings.AiChatToolNavigation)], Icon = BitIconName.NavigateForward },
        AiChatProtocol.Tools.SetApplicationTheme => new(toolName) { Title = localizer[nameof(AppStrings.AiChatToolThemeChange)], Icon = BitIconName.Color },
        AiChatProtocol.Tools.SetApplicationCulture => new(toolName) { Title = localizer[nameof(AppStrings.AiChatToolLanguageChange)], Icon = BitIconName.LocaleLanguage },
        AiChatProtocol.Tools.ShowSignInModal => new(toolName) { Title = localizer[nameof(AppStrings.AiChatToolSignIn)], Icon = BitIconName.Signin },
        AiChatProtocol.Tools.CheckLastError => new(toolName) { Title = localizer[nameof(AppStrings.AiChatToolErrorCheck)], Icon = BitIconName.Bug },
        // Destructive: the approval's allow button takes the error color, so it doesn't read as the safe answer
        AiChatProtocol.Tools.ClearAppFiles => new(toolName) { Title = localizer[nameof(AppStrings.AiChatApprovalClearAppFilesApprove)], Icon = BitIconName.Broom, Risk = BitAiToolRisk.Destructive },
        AiChatProtocol.Tools.RequestHumanFollowUp => new(toolName) { Title = localizer[nameof(AppStrings.AiChatHumanFollowUpPrompt)], Icon = BitIconName.Contact },
        //#if (module == "Sales")
        //#if (database == "PostgreSQL" || database == "SqlServer")
        AiChatProtocol.Tools.GetProductRecommendations => new(toolName) { Title = localizer[nameof(AppStrings.AiChatToolProductSearch)], Icon = BitIconName.Search },
        AiChatProtocol.Tools.ShowProducts => new(toolName) { Title = localizer[nameof(AppStrings.AiChatToolProductSearch)], Icon = BitIconName.Car },
        //#endif
        //#endif
        _ => null
    };
}
