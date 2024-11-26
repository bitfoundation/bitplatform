//+:cnd:noEmit
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR.Client;
//#endif
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Settings;

public partial class ProfileSection
{
    [Parameter] public bool Loading { get; set; }
    [Parameter] public UserDto? User { get; set; }


    //#if (signalR == true)
    [AutoInject] HubConnection hub = default!;
    //#endif
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

        profileImageUrl = new Uri(AbsoluteServerAddress, $"/api/Attachment/GetProfileImage?access_token={accessToken}").ToString();
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
            //#if (signalR == true)
            if (await hub.IsUserSessionUnique(CurrentCancellationToken))
            {
                throw new ForbiddenException(Localizer[nameof(AppStrings.ConcurrentUserSessionOnTheSameDevice)]);
            }
            //#endif

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

        try
        {
            await HttpClient.DeleteAsync(removeProfileImageHttpUrl, CurrentCancellationToken);

            User.ProfileImageName = null;

            PublishUserDataUpdated();
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
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
