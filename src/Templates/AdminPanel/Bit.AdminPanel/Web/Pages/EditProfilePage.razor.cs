//-:cnd:noEmit
using AdminPanel.Shared.Dtos.Account;

namespace AdminPanel.App.Pages;

[Authorize]
public partial class EditProfilePage
{
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

            var access_token = await StateService.GetValue($"{nameof(EditProfilePage)}-access_token", async () =>
                await AuthTokenProvider.GetAcccessToken());

            ProfileImageUploadUrl = $"{GetBaseUrl()}Attachment/UploadProfileImage?access_token={access_token}";
            ProfileImageUrl = $"{GetBaseUrl()}Attachment/GetProfileImage?access_token={access_token}";

        }
        finally
        {
            IsLoadingData = false;
        }

        await base.OnInitAsync();
    }

    string GetBaseUrl()
    {
#if BlazorWebAssembly
        return "/api/";
#else
        return Configuration.GetValue<string>("ApiServerAddress");
#endif
    }

    private async Task LoadEditProfileData()
    {
        User = (await StateService.GetValue($"{nameof(EditProfilePage)}-{nameof(User)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", AppJsonContext.Default.UserDto))) ?? new();

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

    private async Task GoBack()
    {
        await JsRuntime.GoBack();
    }

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

            await HttpClient.PutAsJsonAsync("User/Update", User, AppJsonContext.Default.EditUserDto);

            EditProfileMessageType = BitMessageBarType.Success;

            EditProfileMessage = Localizer[nameof(AppStrings.ProfileUpdatedSuccessfullyMessage)];
        }
        catch (KnownException e)
        {
            EditProfileMessageType = BitMessageBarType.Error;

            EditProfileMessage = e.Message;
        }
        finally
        {
            IsSavingData = false;
        }
    }
}

