//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.App.Pages;

public partial class EditProfilePage
{
    [AutoInject] private IAuthTokenProvider authTokenProvider = default!;

    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private IStateService stateService = default!;

#if BlazorServer || BlazorHybrid
    [AutoInject] private IConfiguration configuration = default!;
#endif

    public UserDto User { get; set; } = new();
    public UserDto UserToEdit { get; set; } = new();

    public string? ProfileImageUploadUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? ProfileImageError { get; set; }

    public bool IsSavingData { get; set; }
    public bool IsLoadingData { get; set; }

    public BitMessageBarType EditProfileMessageType { get; set; }
    public string? EditProfileMessage { get; set; }

    protected override async Task OnInitAsync()
    {
        IsLoadingData = true;

        try
        {
            await LoadEditProfileData();

            var access_token = await stateService.GetValue($"{nameof(EditProfilePage)}-access_token", async () =>
                await authTokenProvider.GetAcccessToken());

            ProfileImageUploadUrl = $"Attachment/UploadProfileImage?access_token={access_token}";
            ProfileImageUrl = $"Attachment/GetProfileImage?access_token={access_token}";

#if BlazorServer || BlazorHybrid
            var serverUrl = configuration.GetValue<string>("ApiServerAddress");
            ProfileImageUploadUrl = $"{serverUrl}{ProfileImageUploadUrl}";
            ProfileImageUrl = $"{serverUrl}{ProfileImageUrl}";
#endif

        }
        finally
        {
            IsLoadingData = false;
        }

        await base.OnInitAsync();
    }

    private async Task LoadEditProfileData()
    {
        User = (await stateService.GetValue($"{nameof(EditProfilePage)}-{nameof(User)}", async () =>
            await httpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto))) ?? new();

        UserToEdit.ProfileImageName = User.ProfileImageName;
        UserToEdit.FullName = User.FullName;
        UserToEdit.BirthDate = User.BirthDate;
        UserToEdit.Gender = User.Gender;
    }

    private bool IsSubmitButtonEnabled =>
            ((User.FullName ?? string.Empty) != (UserToEdit.FullName ?? string.Empty)
            || User.BirthDate != UserToEdit.BirthDate
            || User.Gender != UserToEdit.Gender)
            && IsSavingData is false;

    private async Task Save()
    {
        if (IsSavingData)
        {
            return;
        }

        IsSavingData = true;
        EditProfileMessage = null;

        try
        {
            User.FullName = UserToEdit.FullName;
            User.BirthDate = UserToEdit.BirthDate;
            User.Gender = UserToEdit.Gender;

            await httpClient.PutAsJsonAsync("User/Update", User, AppJsonContext.Default.EditUserDto);

            EditProfileMessageType = BitMessageBarType.Success;

            EditProfileMessage = AppStrings.ProfileUpdatedSuccessfullyMessage;
        }
        catch (KnownException e)
        {
            EditProfileMessageType = BitMessageBarType.Error;

            EditProfileMessage = ErrorStrings.ResourceManager.Translate(e.Message);
        }
        finally
        {
            IsSavingData = false;
        }
    }
}

