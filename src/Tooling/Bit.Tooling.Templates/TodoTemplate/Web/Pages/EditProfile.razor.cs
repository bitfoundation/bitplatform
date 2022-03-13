using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class EditProfile
{
    public UserDto? User { get; set; } = new();
    public UserDto UserToEdit { get; set; } = new();

    public string? ProfileImageUploadUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? ProfileImageError { get; set; }

    public bool IsSaveButtonEnabled { get; set; }
    public bool IsLoadingSaveButton { get; set; }
    public bool IsLoadingPage { get; set; }

    public BitMessageBarType EditProfileMessageType { get; set; }
    public string? EditProfileMessage { get; set; }

    [Inject] public IAuthTokenProvider AuthTokenProvider { get; set; } = default!;

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public IStateService StateService { get; set; } = default!;

#if BlazorServer || BlazorHybrid
    [Inject] public IConfiguration Configuration { get; set; } = default!;
#endif

    protected override async Task OnInitAsync()
    {
        IsLoadingPage = true;

        try
        {
            await LoadEditProfileData();

            var access_token = await StateService.GetValue("access_token", async () =>
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
            IsLoadingPage = false;
        }

        await base.OnInitAsync();
    }

    private async Task LoadEditProfileData()
    {
        User = await StateService.GetValue(nameof(User), async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", ToDoTemplateJsonContext.Default.UserDto));

        UserToEdit.FullName = User?.FullName;
        UserToEdit.BirthDate = User?.BirthDate;
        UserToEdit.Gender = User?.Gender;
    }

    private void CheckSaveButtonEnabled()
    {
        if (User?.FullName == UserToEdit.FullName &&
            User?.BirthDate == UserToEdit.BirthDate &&
            User?.Gender == UserToEdit.Gender)
        {
            IsSaveButtonEnabled = false;
            return;
        }

        IsSaveButtonEnabled = true;
    }

    private async Task Save()
    {
        if (IsLoadingSaveButton)
        {
            return;
        }

        IsLoadingSaveButton = true;
        IsSaveButtonEnabled = false;
        EditProfileMessage = null;

        try
        {
            User.FullName = UserToEdit.FullName;
            User.BirthDate = UserToEdit.BirthDate;
            User.Gender = UserToEdit.Gender;

            await HttpClient.PutAsJsonAsync("User", User, ToDoTemplateJsonContext.Default.EditUserDto);

            IsSaveButtonEnabled = false;

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
            IsLoadingSaveButton = false;
            IsSaveButtonEnabled = true;
        }
    }
}

