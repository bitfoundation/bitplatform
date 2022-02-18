using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class EditProfile
{
    public UserDto? User { get; set; } = new();

    public string? FullName { get; set; }
    public DateTimeOffset? BirthDate { get; set; }
    public string? SelectedGender { get; set; }

    public string? ProfilePhotoUploadUrl { get; set; }
    public string? ProfilePhotoRemoveUrl { get; set; }
    public string? UserProfilePhotoUrl { get; set; }

    public bool IsEnableSaveButton { get; set; }
    public bool IsLoading { get; set; }

    public bool IsSuccessEditProfile { get; set; }
    public string? EditProfileMessage { get; set; }

    [Inject]
    public ITokenProvider TokenProvider { get; set; } = default!;

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject]
    public IStateService StateService { get; set; } = default!;

#if BlazorServer || BlazorHybrid
    [Inject]
    public IConfiguration Configuration { get; set; } = default!;
#endif

    protected override async Task OnInitAsync()
    {
        User = await StateService.GetValue(nameof(User), async () => await HttpClient.GetFromJsonAsync($"User/GetCurrentUser", ToDoTemplateJsonContext.Default.UserDto));

        FullName = User?.FullName;
        BirthDate = User?.BirthDate;
        SelectedGender = User?.Gender.ToString();

        var access_token = await StateService.GetValue("access_token", async () => await TokenProvider.GetAcccessToken());

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

    private void CheckSaveButtonEnable()
    {
        if (User?.FullName == FullName && User?.BirthDate == BirthDate && User?.Gender.ToString() == SelectedGender)
        {
            IsEnableSaveButton = false;
            return;
        }
       
        IsEnableSaveButton = true;
    }

    private async Task Save()
    {
        IsLoading = true;

        try
        {
            User!.FullName = FullName;

            User!.BirthDate = BirthDate;

            if (SelectedGender == Gender.Male.ToString())
            {
                User!.Gender = Gender.Male;
            }
            else if (SelectedGender == Gender.Female.ToString())
            {
                User!.Gender = Gender.Female;
            }
            else if (SelectedGender == Gender.Custom.ToString())
            {
                User!.Gender = Gender.Custom;
            }

            await HttpClient.PutAsJsonAsync("User", User, ToDoTemplateJsonContext.Default.UserDto);

            IsSuccessEditProfile = true;

            EditProfileMessage = "Edit successfully";
        }
        catch (Exception e)
        {
            IsSuccessEditProfile = false;

            EditProfileMessage = e is KnownException 
                ? ErrorStrings.ResourceManager.GetString(e.Message) 
                : ErrorStrings.UnknownException;
        }

        IsLoading = false;
    }
}

