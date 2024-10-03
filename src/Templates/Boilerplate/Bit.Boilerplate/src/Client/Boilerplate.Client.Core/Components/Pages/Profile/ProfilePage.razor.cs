//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Profile;

[Authorize]
public partial class ProfilePage
{
    protected override string? Title => Localizer[nameof(AppStrings.ProfileTitle)];
    protected override string? Subtitle => string.Empty;

    private UserDto? user;
    private bool isLoading;

    [AutoInject] private IUserController userController = default!;


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
