using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

public partial class UserSessionsSection
{
    private bool isLoading;
    private bool isWaiting;
    private string? message;
    private List<UserSessionDto> userSessions = [];
    private BitSeverity severity = BitSeverity.Error;


    [AutoInject] private IUserController userController = default!;


    protected override async Task OnInitAsync()
    {
        await LoadSessions();

        await base.OnInitAsync();
    }

    private async Task LoadSessions()
    {
        isLoading = true;

        try
        {
            userSessions = await userController.GetUserSessions(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task RevokeSession(UserSessionDto session)
    {
        if (isWaiting || session.SessionUniqueId.ToString() == UserClaims?.SessionId) return;

        isWaiting = true;
        message = null;

        try
        {
            await userController.RevokeSession(session.SessionUniqueId, CurrentCancellationToken);

            await LoadSessions();
        }
        catch (KnownException e)
        {
            message = e.Message;
            severity = BitSeverity.Error;
        }
        finally
        {
            isWaiting = false;
            message = Localizer[nameof(AppStrings.RemoveSessionSuccessMessage)];
            severity = BitSeverity.Success;
        }
    }
}
