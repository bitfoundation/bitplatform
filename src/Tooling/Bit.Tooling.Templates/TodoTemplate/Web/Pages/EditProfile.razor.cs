using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class EditProfile
{
    public UserDto? User { get; set; } = new();
    public UserDto UserToEdit { get; set; } = new();

    public string? ProfileImageUploadUrl { get; set; }
    public string? ProfileImageRemoveUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? ProfileImageError { get; set; }

    public bool IsSaveButtonEnabled { get; set; }
    public bool IsLoadingSaveButton { get; set; }
    public bool IsLoadingPage { get; set; }

    public BitMessageBarType EditProfileMessageType { get; set; }
    public string? EditProfileMessage { get; set; }

    [Inject] public ITokenProvider TokenProvider { get; set; } = default!;

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public IStateService StateService { get; set; } = default!;

#if BlazorServer || BlazorHybrid
    [Inject] public IConfiguration Configuration { get; set; } = default!;
#endif

    protected override async Task OnInitAsync()
    {
        IsLoadingPage = true;

        await InitEditProfileData();

         var access_token = await StateService.GetValue("access_token", async () =>
            await TokenProvider.GetAcccessToken());

        ProfileImageUploadUrl = $"api/Attachment/UploadProfileImage?access_token={access_token}";
        ProfileImageRemoveUrl = $"api/Attachment/RemoveProfileImage?access_token={access_token}";
        ProfileImageUrl = $"api/Attachment/GetProfileImage?access_token={access_token}";

#if BlazorServer || BlazorHybrid
        var serverUrl = Configuration.GetValue<string>("ApiServerAddress");
        ProfileImageUploadUrl = $"{serverUrl}{ProfileImageUploadUrl}";
        ProfileImageRemoveUrl = $"{serverUrl}{ProfileImageRemoveUrl}";
        ProfileImageUrl = $"{serverUrl}{ProfileImageUrl}";
#endif

        IsLoadingPage = false;

        await base.OnInitAsync();
    }

    private async Task InitEditProfileData()
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
        IsLoadingSaveButton = true;

        try
        {
            User.FullName = UserToEdit.FullName;
            User.BirthDate = UserToEdit.BirthDate;
            User.Gender = UserToEdit.Gender;

            await HttpClient.PutAsJsonAsync("User", User, ToDoTemplateJsonContext.Default.UserDto);

            IsSaveButtonEnabled = false;

            EditProfileMessageType = BitMessageBarType.Success;

            EditProfileMessage = "Edit successfully";
        }
        catch (Exception e)
        {
            EditProfileMessageType = BitMessageBarType.Error;

            EditProfileMessage = e is KnownException
                ? ErrorStrings.ResourceManager.GetString(e.Message)
                : ErrorStrings.UnknownException;
        }
        finally
        {
            IsLoadingSaveButton = false;
        }
    }
}

