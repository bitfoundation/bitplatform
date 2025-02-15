//+:cnd:noEmit
using Boilerplate.Shared.Controllers;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class ProfileSection
{
    [Parameter] public bool Loading { get; set; }
    [Parameter] public UserDto? User { get; set; }

    [AutoInject] private IUserController userController = default!;
    [AutoInject] private IAttachmentController attachmentController = default!;


    private bool isSaving;
    private bool isUploading;
    private string? profileImageUploadUrl;
    private BitFileUpload fileUploadRef = default!;
    private readonly EditUserDto editUserDto = new();

    
    private string? ProfileImageUrl => User?.GetProfileImageUrl(AbsoluteServerAddress);


    protected override async Task OnInitAsync()
    {
        var accessToken = await AuthTokenProvider.GetAccessToken();

        profileImageUploadUrl = new Uri(AbsoluteServerAddress, $"/api/Attachment/UploadProfileImage?access_token={accessToken}").ToString();

        await base.OnInitAsync();
    }

    protected override void OnParametersSet()
    {
        User?.Patch(editUserDto);

        base.OnParametersSet();
    }


    private async Task SaveProfile()
    {
        if (isSaving || User is null) return;

        isSaving = true;

        try
        {
            editUserDto.Patch(User);

            (await userController.Update(editUserDto, CurrentCancellationToken)).Patch(User);

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
        if (isSaving || User is null) return;
        isSaving = true;

        try
        {
            await attachmentController.RemoveProfileImage(CurrentCancellationToken);

            User.ProfileImageName = null;

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
        if (User is null) return;

        try
        {
            var updatedUser = await userController.GetCurrentUser(CurrentCancellationToken);

            updatedUser.Patch(User);

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
        PubSubService.Publish(ClientPubSubMessages.PROFILE_UPDATED, User);
    }
}
