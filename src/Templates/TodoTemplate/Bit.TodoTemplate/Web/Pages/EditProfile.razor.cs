//-:cnd:noEmit
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class EditProfile
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

    [AutoInject] private IAuthTokenProvider AuthTokenProvider { get; set; } = default!;

    [AutoInject] private HttpClient HttpClient { get; set; } = default!;

    [AutoInject] private IStateService StateService { get; set; } = default!;

#if BlazorServer || BlazorHybrid
    [AutoInject] private IConfiguration Configuration { get; set; } = default!;
#endif

    protected override async Task OnInitAsync()
    {
        IsLoadingData = true;

        try
        {
            await LoadEditProfileData();

            var access_token = await StateService.GetValue($"{nameof(EditProfile)}-access_token", async () =>
                await AuthTokenProvider.GetAcccessToken());

            ProfileImageUploadUrl = $"api/Attachment/UploadProfileImage?access_token={access_token}";
            ProfileImageUrl = $"api/Attachment/GetProfileImage?access_token={access_token}";

#if BlazorServer || BlazorHybrid
            var serverUrl = Configuration.GetValue<string>("ApiServerAddress");
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
        User = (await StateService.GetValue($"{nameof(EditProfile)}-{nameof(User)}", async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", TodoTemplateJsonContext.Default.UserDto))) ?? new();

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

            await HttpClient.PutAsJsonAsync("User", User, TodoTemplateJsonContext.Default.EditUserDto);

            EditProfileMessageType = BitMessageBarType.Success;

            EditProfileMessage = "Edit successfully";
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

