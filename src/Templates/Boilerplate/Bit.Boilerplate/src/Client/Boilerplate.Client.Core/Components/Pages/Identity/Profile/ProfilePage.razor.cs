//-:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

[Authorize]
public partial class ProfilePage
{
    [AutoInject] private IUserController userController = default!;

    private bool isLoading;
    private bool isDeleteAccountConfirmModalOpen;

    private UserDto? user;


    protected override async Task OnInitAsync()
    {
        isLoading = true;

        try
        {
            user = await userController.GetCurrentUser(CurrentCancellationToken);
        }
        finally
        {
            isLoading = false;
        }

        await base.OnInitAsync();
    }
}
