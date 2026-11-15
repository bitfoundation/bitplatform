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
    public static BitAiToolCallDisplay Display(BitAiToolCallPart call) =>
        AiChatProtocol.Tools.Hidden.Contains(call.ToolName) ? BitAiToolCallDisplay.Hidden :
        AiChatProtocol.Tools.Cards.Contains(call.ToolName) ? BitAiToolCallDisplay.Card :
        BitAiToolCallDisplay.Status;

    /// <summary>The title, icon and risk a call is shown with. Null leaves the tool's own name, for a tool added without a line here.</summary>
    public static BitAiToolDefinition? Describe(string toolName, IStringLocalizer<AppStrings> localizer)
    {
        // The title is the one the server gives the call as a step, too (See AppChatbot.AnswerSteps)
        if (AiChatProtocol.Tools.TitleKey(toolName) is not { } title) return null;

        return new(toolName)
        {
            Title = localizer[title],
            Icon = toolName switch
            {
                AiChatProtocol.Tools.NavigateToPage => BitIconName.NavigateForward,
                AiChatProtocol.Tools.SetApplicationTheme => BitIconName.Color,
                AiChatProtocol.Tools.SetApplicationCulture => BitIconName.LocaleLanguage,
                AiChatProtocol.Tools.ShowSignInModal => BitIconName.Signin,
                AiChatProtocol.Tools.CheckLastError => BitIconName.Bug,
                AiChatProtocol.Tools.ClearAppFiles => BitIconName.Broom,
                AiChatProtocol.Tools.RequestHumanFollowUp => BitIconName.Contact,
                //#if (module == "Sales")
                //#if (database == "PostgreSQL" || database == "SqlServer")
                AiChatProtocol.Tools.GetProductRecommendations => BitIconName.Search,
                AiChatProtocol.Tools.ShowProducts => BitIconName.Car,
                //#endif
                //#endif
                _ => null
            },
            // Destructive: the approval's allow button takes the error color, so it doesn't read as the safe answer
            Risk = toolName is AiChatProtocol.Tools.ClearAppFiles ? BitAiToolRisk.Destructive : BitAiToolRisk.ReadOnly
        };
    }
}
