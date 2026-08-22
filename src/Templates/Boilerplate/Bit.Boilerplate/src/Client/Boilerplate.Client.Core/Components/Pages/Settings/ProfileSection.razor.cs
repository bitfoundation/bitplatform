//+:cnd:noEmit
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class ProfileSection
{
    [CascadingParameter] public UserDto? CurrentUser { get; set; }


    [AutoInject] private IUserController userController = default!;
    [AutoInject] private IAttachmentController attachmentController = default!;


    private bool isSaving;
    private bool isUploading;
    private Guid? seededUserId;
    private BitFileUpload fileUploadRef = default!;
    private readonly EditUserRequestDto editUserDto = new();

    private string? ProfileImageUrl => CurrentUser?.GetProfileImageUrl(AbsoluteServerAddress);


    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (CurrentUser is null || seededUserId == CurrentUser.Id) return;

        seededUserId = CurrentUser.Id;
        CurrentUser.Patch(editUserDto);
    }


    private async Task SaveProfile()
    {
        if (isSaving || CurrentUser is null) return;

        isSaving = true;

        try
        {
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
        if (CurrentUser is null)
        {
            isUploading = false;
            return;
        }

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

    private void HandleOnInvalid(BitFileInfo[] files)
    {
        SnackBarService.Error(files[0].Message ?? Localizer[nameof(AppStrings.FileUploadFailed)]);
    }

    private async Task HandleOnUploadFailed(BitFileInfo fileInfo)
    {
        isUploading = false;
        SnackBarService.Error(string.IsNullOrEmpty(fileInfo.Message) ? Localizer[nameof(AppStrings.FileUploadFailed)] : fileInfo.Message);
    }

    private void PublishUserDataUpdated()
    {
        PubSubService.Publish(ClientAppMessages.PROFILE_UPDATED, CurrentUser);
    }

    private async Task<string> GetUploadUrl()
    {
        var uploadUrl = new Uri(AbsoluteServerAddress, $"/api/v1/Attachment/UploadUserProfilePicture").ToString();

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            uploadUrl += $"?culture={CultureInfo.CurrentUICulture.Name}"; // To have localized error messages from the server (if any).
        }

        return uploadUrl;
    }

    private async Task<Dictionary<string, string>> GetUploadRequestHeaders()
    {
        var accessToken = await AuthManager.GetFreshAccessToken(requestedBy: nameof(BitFileUpload));

        return new() { { "Authorization", $"Bearer {accessToken}" } };
    }
}
