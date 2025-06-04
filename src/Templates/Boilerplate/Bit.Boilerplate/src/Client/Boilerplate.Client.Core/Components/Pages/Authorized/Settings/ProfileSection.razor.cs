//+:cnd:noEmit
using Boilerplate.Shared.Controllers;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class ProfileSection
{
    [Parameter] public bool Loading { get; set; }
    [CascadingParameter(Name = Parameters.CurrentUser)]
    public UserDto? CurrentUser { get; set; }

    [AutoInject] private IUserController userController = default!;
    [AutoInject] private IAttachmentController attachmentController = default!;


    private bool isSaving;
    private bool isUploading;
    private BitFileUpload fileUploadRef = default!;
    private readonly EditUserRequestDto editUserDto = new();

    private string? ProfileImageUrl => CurrentUser?.GetProfileImageUrl(AbsoluteServerAddress);

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        CurrentUser?.Patch(editUserDto);
    }


    private async Task SaveProfile()
    {
        if (isSaving || CurrentUser is null) return;

        isSaving = true;

        try
        {
            editUserDto.Patch(CurrentUser);

            (await userController.Update(editUserDto, CurrentCancellationToken)).Patch(CurrentUser);

            PublishUserDataUpdated();

            SnackBarService.Success(Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)]);
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task RemoveProfileImage()
    {
        if (isSaving || CurrentUser is null) return;
        isSaving = true;

        try
        {
            await attachmentController.DeleteUserProfilePicture(CurrentCancellationToken);

            CurrentUser.HasProfilePicture = false;

            PublishUserDataUpdated();
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task HandleOnUploadComplete()
    {
        if (CurrentUser is null) return;

        try
        {
            var updatedUser = await userController.GetCurrentUser(CurrentCancellationToken);

            updatedUser.Patch(CurrentUser);

            PublishUserDataUpdated();
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isUploading = false;
        }
    }

    private async Task HandleOnUploadFailed()
    {
        isUploading = false;
        SnackBarService.Error(Localizer[nameof(AppStrings.FileUploadFailed)]);
    }

    private void PublishUserDataUpdated()
    {
        PubSubService.Publish(ClientPubSubMessages.PROFILE_UPDATED, CurrentUser);
    }

    private async Task<string> GetUploadUrl()
    {
        return new Uri(AbsoluteServerAddress, $"/api/Attachment/UploadUserProfilePicture").ToString();
    }

    private async Task<Dictionary<string, string>> GetUploadRequestHeaders()
    {
        var accessToken = await AuthManager.GetFreshAccessToken(requestedBy: nameof(BitFileUpload));

        return new() { { "Authorization", $"Bearer {accessToken}" } };
    }
}
