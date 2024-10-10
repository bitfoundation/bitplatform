using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Settings;

public partial class ProfileSection
{
    private bool isSaving;
    private bool isUploading;
    private string? profileImageUrl;
    private string? profileImageUploadUrl;
    private string? removeProfileImageHttpUrl;

    private readonly EditUserDto editUserDto = new();

    private BitSnackBar snackbarRef = default!;


    [AutoInject] private IUserController userController = default!;

    [Parameter]
    public UserDto? User { get; set; }


    protected override async Task OnInitAsync()
    {
        var access_token = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessTokenAsync);

        removeProfileImageHttpUrl = $"api/Attachment/RemoveProfileImage?access_token={access_token}";

        var serverAddress = Configuration.GetServerAddress();
        profileImageUrl = $"{serverAddress}/api/Attachment/GetProfileImage?access_token={access_token}";
        profileImageUploadUrl = $"{serverAddress}/api/Attachment/UploadProfileImage?access_token={access_token}";

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

            await snackbarRef.Success(Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)]);
        }
        catch (KnownException e)
        {
            await snackbarRef.Error(e.Message);
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task RemoveProfileImage()
    {
        if (isSaving || User is null) return;

        try
        {
            await HttpClient.DeleteAsync(removeProfileImageHttpUrl, CurrentCancellationToken);

            User.ProfileImageName = null;

            PublishUserDataUpdated();
        }
        catch (KnownException e)
        {
            await snackbarRef.Error(e.Message);
        }
    }

    private async Task HandleOnUploadComplete()
    {
        if (User is null) return;

        try
        {
            var updatedUser = await userController.GetCurrentUser(CurrentCancellationToken);

            User.ProfileImageName = updatedUser.ProfileImageName;

            PublishUserDataUpdated();
        }
        catch (KnownException e)
        {
            await snackbarRef.Error(e.Message);
        }
        finally
        {
            isUploading = false;
        }
    }

    private async Task HandleOnUploadFailed()
    {
        isUploading = false;
        await snackbarRef.Error(Localizer[nameof(AppStrings.FileUploadFailed)]);
    }

    private void PublishUserDataUpdated()
    {
        PubSubService.Publish(PubSubMessages.PROFILE_UPDATED, User);
    }
}
