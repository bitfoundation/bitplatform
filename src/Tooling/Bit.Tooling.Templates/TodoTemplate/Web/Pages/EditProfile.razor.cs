using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class EditProfile
{
    public UserDto? User { get; set; } = new();
    public UserDto UserToEdit { get; set; } = new();

    public string? ProfilePhotoUploadUrl { get; set; }
    public string? ProfilePhotoRemoveUrl { get; set; }
    public string? UserProfilePhotoUrl { get; set; }

    public bool IsSaveButtonEnabled { get; set; }
    public bool IsLoading { get; set; }

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
        User = await StateService.GetValue(nameof(User), async () =>
            await HttpClient.GetFromJsonAsync("User/GetCurrentUser", ToDoTemplateJsonContext.Default.UserDto));

        UserToEdit.FullName = User.FullName;
        UserToEdit.BirthDate = User.BirthDate;
        UserToEdit.Gender = User.Gender;

        var access_token = await StateService.GetValue("access_token", async () =>
            await TokenProvider.GetAcccessToken());

        ProfilePhotoUploadUrl = $"api/Attachment/UploadProfilePhoto?access_token={access_token}";
        ProfilePhotoRemoveUrl = $"api/Attachment/RemoveProfilePhoto?access_token={access_token}";
        UserProfilePhotoUrl = $"api/Attachment/GetProfilePhoto?access_token={access_token}";

#if BlazorServer || BlazorHybrid
        var serverUrl = Configuration.GetValue<string>("ApiServerAddress");
        ProfilePhotoUploadUrl = $"{serverUrl}{ProfilePhotoUploadUrl}";
        ProfilePhotoRemoveUrl = $"{serverUrl}{ProfilePhotoRemoveUrl}";
        UserProfilePhotoUrl = $"{serverUrl}{UserProfilePhotoUrl}";
#endif

        await base.OnInitAsync();
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
        IsLoading = true;

        try
        {
            User.FullName = UserToEdit.FullName;
            User.BirthDate = UserToEdit.BirthDate;
            User.Gender = UserToEdit.Gender;

            await HttpClient.PutAsJsonAsync("User", User, ToDoTemplateJsonContext.Default.UserDto);

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
            IsLoading = false;
        }
    }
}

