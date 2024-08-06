using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

public partial class UserDataSection
{
    private bool isSaving;
    private bool isRemoving;
    private bool isUploading;
    private string? profileImageUrl;
    private string? profileImageError;
    private string? profileImageUploadUrl;
    private string? removeProfileImageHttpUrl;

    private UserDto user = default!;
    private readonly EditUserDto editUserDto = new();

    private string? message;
    private BitColor messageColor;
    private ElementReference messageRef = default!;


    [AutoInject] private IUserController userController = default!;


    [Parameter] public bool Loading { get; set; }

    [Parameter]
    public UserDto User
    {
        get => user;
        set
        {
            user = value;
            user?.Patch(editUserDto);
        }
    }


    protected override async Task OnInitAsync()
    {
        var access_token = await PrerenderStateService.GetValue(AuthTokenProvider.GetAccessTokenAsync);

        removeProfileImageHttpUrl = $"api/Attachment/RemoveProfileImage?access_token={access_token}";

        var serverAddress = Configuration.GetServerAddress();
        profileImageUrl = new Uri(serverAddress, $"api/Attachment/GetProfileImage?access_token={access_token}").ToString();
        profileImageUploadUrl = new Uri(serverAddress, $"api/Attachment/UploadProfileImage?access_token={access_token}").ToString();

        await base.OnInitAsync();
    }

    private async Task SaveProfile()
    {
        if (isSaving) return;

        isSaving = true;
        message = null;

        try
        {
            editUserDto.Patch(user);

            (await userController.Update(editUserDto, CurrentCancellationToken)).Patch(user);

            PublishUserDataUpdated();

            messageColor = BitColor.Success;
            message = Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)];
            await messageRef.ScrollIntoView();
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageColor = BitColor.Error;
            await messageRef.ScrollIntoView();
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

            user.ProfileImageName = null;

            PublishUserDataUpdated();
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageColor = BitColor.Error;
            await messageRef.ScrollIntoView();
        }
        finally
        {
            isRemoving = false;
        }
    }

    private async Task HandleOnUploadComplete()
    {
        try
        {
            var updatedUser = await userController.GetCurrentUser(CurrentCancellationToken);

            user.ProfileImageName = updatedUser.ProfileImageName;

            PublishUserDataUpdated();
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageColor = BitColor.Error;
            await messageRef.ScrollIntoView();
        }
        finally
        {
            isUploading = false;
        }
    }

    private void PublishUserDataUpdated()
    {
        PubSubService.Publish(PubSubMessages.USER_DATA_UPDATED, user);
    }
}
