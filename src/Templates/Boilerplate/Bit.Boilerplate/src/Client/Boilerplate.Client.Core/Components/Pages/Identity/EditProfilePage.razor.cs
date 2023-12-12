//-:cnd:noEmit
using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

[Authorize]
public partial class EditProfilePage
{
    [AutoInject] IUserController userController = default!;

    private bool isSaving;
    private bool isRemoving;
    private bool isLoading;
    private string? profileImageUrl;
    private string? profileImageError;
    private string? editProfileMessage;
    private string? profileImageUploadUrl;
    private string? profileImageRemoveUrl;
    private BitMessageBarType editProfileMessageType;
    private UserDto user = new();
    private readonly EditUserDto userToEdit = new();
    private bool isDeleteAccountConfirmModalOpen;

    protected override async Task OnInitAsync()
    {
        isLoading = true;

        try
        {
            await LoadEditProfileData();

            var access_token = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessTokenAsync);

            profileImageUploadUrl = $"{Configuration.GetApiServerAddress()}api/Attachment/UploadProfileImage?access_token={access_token}";
            profileImageUrl = $"{Configuration.GetApiServerAddress()}api/Attachment/GetProfileImage?access_token={access_token}";
            profileImageRemoveUrl = $"api/Attachment/RemoveProfileImage?access_token={access_token}";
        }
        finally
        {
            isLoading = false;
        }

        await base.OnInitAsync();
    }

    private async Task LoadEditProfileData()
    {
        user = await GetCurrentUser() ?? new();

        user.Patch(userToEdit);
    }

    private async Task RefreshProfileData()
    {
        await LoadEditProfileData();

        PubSubService.Publish(PubSubMessages.PROFILE_UPDATED, user);
    }

    private Task<UserDto> GetCurrentUser() => userController.GetCurrentUser(CurrentCancellationToken);

    private async Task DoSave()
    {
        if (isSaving) return;

        isSaving = true;
        editProfileMessage = null;

        try
        {
            userToEdit.Patch(user);

            (await userController.Update(userToEdit, CurrentCancellationToken)).Patch(user);

            PubSubService.Publish(PubSubMessages.PROFILE_UPDATED, user);

            editProfileMessageType = BitMessageBarType.Success;
            editProfileMessage = Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)];
        }
        catch (KnownException e)
        {
            editProfileMessageType = BitMessageBarType.Error;

            editProfileMessage = e.Message;
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task RemoveProfileImage()
    {
        if (isRemoving) return;

        isRemoving = true;

        try
        {
            await HttpClient.DeleteAsync(profileImageRemoveUrl, CurrentCancellationToken);

            await RefreshProfileData();
        }
        catch (KnownException e)
        {
            editProfileMessage = e.Message;
            editProfileMessageType = BitMessageBarType.Error;
        }
        finally
        {
            isRemoving = false;
        }
    }
}
