using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Client.Core.Components.Layout.AiChat;

public partial class AiChatApprovalDetails
{
    /// <summary>The tool asking (See <see cref="AiChatProtocol.Tools"/>).</summary>
    [Parameter] public string? ToolName { get; set; }

    /// <summary>A voice call ends too, which the user should know before they agree.</summary>
    [Parameter] public bool IsInVoiceCall { get; set; }

    private string? description;
    private (string IconName, string Text)[] consequences = [];

    protected override async Task OnParamsSetAsync()
    {
        if (ToolName is AiChatProtocol.Tools.ClearAppFiles)
        {
            description = Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesDescription)];
            consequences =
            [
                (BitIconName.SignOut, Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesSignOut)]),
                (BitIconName.Delete, Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesDeleteConversation)]),
                (BitIconName.Refresh, Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesRestart)]),
                .. IsInVoiceCall ? [(BitIconName.DeclineCall, (string)Localizer[nameof(AppStrings.AiChatApprovalClearAppFilesEndVoiceCall)])] : Array.Empty<(string, string)>()
            ];
        }
        else
        {
            description = null;
            consequences = [];
        }

        await base.OnParamsSetAsync();
    }
}
