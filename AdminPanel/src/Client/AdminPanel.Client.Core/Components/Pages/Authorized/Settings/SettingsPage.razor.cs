using AdminPanel.Shared.Dtos.Identity;
using AdminPanel.Shared.Controllers.Identity;

namespace AdminPanel.Client.Core.Components.Pages.Authorized.Settings;

public partial class SettingsPage
{
    protected override string? Title => Localizer[nameof(AppStrings.Settings)];
    protected override string? Subtitle => string.Empty;

    private UserDto? user;
    private bool isLoading;
    private int openedAccordion = 1;
    private string? profileImageUrl;

    [AutoInject] private IUserController userController = default!;


    protected override async Task OnInitAsync()
    {
        isLoading = true;

        try
        {
            user = await userController.GetCurrentUser(CurrentCancellationToken);

            var serverAddress = Configuration.GetServerAddress();
            var access_token = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessToken);
            profileImageUrl = $"{serverAddress}/api/Attachment/GetProfileImage?access_token={access_token}";
        }
        finally
        {
            isLoading = false;
        }

        await base.OnInitAsync();
    }
}
