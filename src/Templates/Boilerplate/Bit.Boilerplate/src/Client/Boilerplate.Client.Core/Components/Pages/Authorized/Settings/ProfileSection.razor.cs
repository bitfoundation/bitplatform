//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class ProfileSection
{
    [Parameter] public bool Loading { get; set; }
    [Parameter] public UserDto? User { get; set; }

    [AutoInject] private IUserController userController = default!;


    private bool isSaving;
    private bool isUploading;
    private string? profileImageUrl;
    private string? profileImageUploadUrl;
    private string? removeProfileImageHttpUrl;
    private BitFileUpload fileUploadRef = default!;
    private readonly EditUserDto editUserDto = new();

    protected override async Task OnInitAsync()
    {
        var accessToken = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessToken);

        removeProfileImageHttpUrl = $"api/Attachment/RemoveProfileImage?access_token={accessToken}";

        profileImageUploadUrl = new Uri(AbsoluteServerAddress, $"/api/Attachment/UploadProfileImage?access_token={accessToken}").ToString();

        await base.OnInitAsync();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (User is not null)
        {
            User.Patch(editUserDto);
            profileImageUrl = new Uri(AbsoluteServerAddress, $"/api/Attachment/GetProfileImage/{User.Id}?v={User.ConcurrencyStamp}").ToString();
        }
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
            await HttpClient.DeleteAsync(removeProfileImageHttpUrl, CurrentCancellationToken);

            User.ProfileImageName = profileImageUrl = null;

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

            profileImageUrl = new Uri(AbsoluteServerAddress, $"/api/Attachment/GetProfileImage/{User.Id}?v={User.ConcurrencyStamp}").ToString();

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
