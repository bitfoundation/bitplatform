//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Identity;

namespace AdminPanel.Client.Core.Pages.Identity;

[Authorize]
public partial class EditProfilePage
{
    private bool _isLoading;
    private bool _isRemoving;
    private bool _isLoadingData;
    private string? _profileImageUrl;
    private string? _profileImageError;
    private string? _editProfileMessage;
    private string? _profileImageUploadUrl;
    private string? _profileImageRemoveUrl;
    private BitMessageBarType _editProfileMessageType;
    private UserDto _user = new();
    private readonly EditUserDto _userToEdit = new();
    private bool _isDeleteAccountConfirmModalOpen;

    protected override async Task OnInitAsync()
    {
        _isLoadingData = true;

        try
        {
            await LoadEditProfileData();

            var access_token = await PrerenderStateService.GetValue($"{nameof(EditProfilePage)}-access_token", AuthTokenProvider.GetAccessTokenAsync);

            _profileImageUploadUrl = $"{Configuration.GetApiServerAddress()}Attachment/UploadProfileImage?access_token={access_token}";
            _profileImageUrl = $"{Configuration.GetApiServerAddress()}Attachment/GetProfileImage?access_token={access_token}";
            _profileImageRemoveUrl = $"Attachment/RemoveProfileImage?access_token={access_token}";
        }
        finally
        {
            _isLoadingData = false;
        }
    }

    private async Task LoadEditProfileData()
    {
        _user = await PrerenderStateService.GetValue($"{nameof(EditProfilePage)}-{nameof(_user)}", GetCurrentUser) ?? new();

        UpdateEditProfileData();
    }

    private async Task RefreshProfileData()
    {
        _user = await GetCurrentUser() ?? new();

        UpdateEditProfileData();

        PubSubService.Publish(PubSubMessages.PROFILE_UPDATED, _user);
    }

    private void UpdateEditProfileData()
    {
        _userToEdit.FullName = _user.FullName;
        _userToEdit.BirthDate = _user.BirthDate;
        _userToEdit.Gender = _user.Gender;
    }

    private Task<UserDto?> GetCurrentUser() => HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto);

    private async Task GoBack() => await JsRuntime.GoBack();

    private async Task DoSave()
    {
        if (_isLoading) return;

        _isLoading = true;
        _editProfileMessage = null;

        try
        {
            _user.FullName = _userToEdit.FullName;
            _user.BirthDate = _userToEdit.BirthDate;
            _user.Gender = _userToEdit.Gender;

            (await (await HttpClient.PutAsJsonAsync("User/Update", _userToEdit, AppJsonContext.Default.EditUserDto))
                .Content.ReadFromJsonAsync(AppJsonContext.Default.UserDto))!.Patch(_user);

            PubSubService.Publish(PubSubMessages.PROFILE_UPDATED, _user);

            _editProfileMessageType = BitMessageBarType.Success;

            _editProfileMessage = Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)];
        }
        catch (KnownException e)
        {
            _editProfileMessage = e.Message;
            _editProfileMessageType = BitMessageBarType.Error;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task RemoveProfileImage()
    {
        if (_isRemoving) return;

        _isRemoving = true;

        try
        {
            await HttpClient.DeleteAsync(_profileImageRemoveUrl);

            await RefreshProfileData();
        }
        catch (KnownException e)
        {
            _editProfileMessage = e.Message;
            _editProfileMessageType = BitMessageBarType.Error;
        }
        finally
        {
            _isRemoving = false;
        }
    }
}
