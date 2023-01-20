//-:cnd:noEmit
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Client.Shared.Pages;

[Authorize]
public partial class EditProfilePage
{
    private bool _isSaving;
    private bool _isRemoving;
    private bool _isLoading;
    private string? _profileImageUrl;
    private string? _profileImageError;
    private string? _editProfileMessage;
    private string? _profileImageUploadUrl;
    private string? _profileImageRemoveUrl;
    private BitMessageBarType _editProfileMessageType;
    private UserDto _user = new();
    private readonly UserDto _userToEdit = new();

    protected override async Task OnInitAsync()
    {
        _isLoading = true;

        try
        {
            await LoadEditProfileData();

            var access_token = await StateService.GetValue($"{nameof(EditProfilePage)}-access_token", AuthTokenProvider.GetAcccessTokenAsync);

            _profileImageUploadUrl = $"{GetBaseUrl()}Attachment/UploadProfileImage?access_token={access_token}";
            _profileImageUrl = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}";
            _profileImageRemoveUrl = $"Attachment/RemoveProfileImage?access_token={access_token}";
        }
        finally
        {
            _isLoading = false;
        }

        await base.OnInitAsync();
    }

    private string? GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return Configuration.GetValue<string>("ApiServerAddress");
#endif
    }

    private async Task LoadEditProfileData()
    {
        _user = await StateService.GetValue($"{nameof(EditProfilePage)}-{nameof(_user)}", GetCurrentUser) ?? new();

        UpdateEditProfileData();
    }

    private async Task RefreshProfileData()
    {
        _user = await GetCurrentUser() ?? new();

        UpdateEditProfileData();

        PubSubService.Pub(PubSubMessages.PROFILE_UPDATED, _user);
    }

    private void UpdateEditProfileData()
    {
        _userToEdit.Gender = _user.Gender;
        _userToEdit.FullName = _user.FullName;
        _userToEdit.BirthDate = _user.BirthDate;
        _userToEdit.ProfileImageName = _user.ProfileImageName;
    }

    private Task<UserDto?> GetCurrentUser() => HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto);


    private async Task DoSave()
    {
        if (_isSaving) return;

        _isSaving = true;
        _editProfileMessage = null;

        try
        {
            _user.FullName = _userToEdit.FullName;
            _user.BirthDate = _userToEdit.BirthDate;
            _user.Gender = _userToEdit.Gender;

            await HttpClient.PutAsJsonAsync("User/Update", _user, AppJsonContext.Default.EditUserDto);

            PubSubService.Pub(PubSubMessages.PROFILE_UPDATED, _user);

            _editProfileMessageType = BitMessageBarType.Success;
            _editProfileMessage = Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)];
        }
        catch (KnownException e)
        {
            _editProfileMessageType = BitMessageBarType.Error;

            _editProfileMessage = e.Message;
        }
        finally
        {
            _isSaving = false;
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

