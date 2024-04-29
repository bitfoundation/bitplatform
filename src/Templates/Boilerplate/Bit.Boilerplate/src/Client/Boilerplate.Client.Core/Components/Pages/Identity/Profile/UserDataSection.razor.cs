using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

public partial class UserDataSection
{
    private UserDto? user;

    [AutoInject] private IUserController userController = default!;

    private bool isSaving;
    private bool isRemoving;
    private string? profileImageUrl;
    private string? profileImageError;
    private string? profileImageUploadUrl;
    private string? removeProfileImageHttpUrl;

    private UserDto userDto = default!;
    private readonly EditUserDto editUserDto = new();

    private string? message;
    private BitMessageBarType messageType;

    [Parameter] public bool Loading { get; set; }

    [Parameter]
    public UserDto? User
    {
        get => user;
        set
        {
            if (value is null || user == value) return;

            user = value;

            userDto = user;
            userDto.Patch(editUserDto);
        }
    }


    protected override async Task OnInitAsync()
    {
        var access_token = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessTokenAsync);

        removeProfileImageHttpUrl = $"api/Attachment/RemoveProfileImage?access_token={access_token}";

        var apiUri = new Uri(Configuration.GetApiServerAddress());
        profileImageUrl = new Uri(apiUri, $"api/Attachment/GetProfileImage?access_token={access_token}").AbsolutePath;
        profileImageUploadUrl = new Uri(apiUri, $"api/Attachment/UploadProfileImage?access_token={access_token}").AbsolutePath;

        await base.OnInitAsync();
    }


    private async Task SaveProfile()
    {
        if (isSaving) return;

        isSaving = true;
        message = null;

        try
        {
            editUserDto.Patch(userDto);

            (await userController.Update(editUserDto, CurrentCancellationToken)).Patch(userDto);

            PubSubService.Publish(PubSubMessages.USER_DATA_UPDATED, userDto);

            messageType = BitMessageBarType.Success;
            message = Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)];
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageType = BitMessageBarType.Error;
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
            await HttpClient.DeleteAsync(removeProfileImageHttpUrl, CurrentCancellationToken);

            userDto.ProfileImageName = null;
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageType = BitMessageBarType.Error;
        }
        finally
        {
            isRemoving = false;
        }
    }

    private void HandleOnUploadProfileImageComplete()
    {
        PubSubService.Publish(PubSubMessages.USER_DATA_UPDATED, userDto);
    }
}
