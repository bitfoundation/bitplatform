using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class EditProfile
{
    public UserDto? User { get; set; } = new();

    public string? SelectedGender { get; set; }

    public string? ProfilePhotoUploadUrl { get; set; }
    public string? ProfilePhotoRemoveUrl { get; set; }
    public string? UserProfilePhotoUrl { get; set; }

    public bool HasMessageBar { get; set; }
    public bool IsSuccessMessageBar { get; set; }
    public string? MessageBarText { get; set; }

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

    protected override async Task OnInitializedAsync()
    {
        ProfilePhotoUploadUrl = await StateService.GetValue(nameof(ProfilePhotoUploadUrl), async () => $"api/Attachment/UploadProfilePhoto?access_token={await TokenProvider.GetAcccessToken()}");
        ProfilePhotoRemoveUrl = await StateService.GetValue(nameof(ProfilePhotoRemoveUrl), async () => $"api/Attachment/RemoveProfilePhoto?access_token={await TokenProvider.GetAcccessToken()}");
        UserProfilePhotoUrl = await StateService.GetValue(nameof(UserProfilePhotoUrl), async () => $"api/Attachment/GetProfilePhoto?access_token={await TokenProvider.GetAcccessToken()}");

#if BlazorServer || BlazorHybrid
        var serverUrl = Configuration.GetValue<string>("ApiServerAddress");
        ProfilePhotoUploadUrl = $"{serverUrl}{ProfilePhotoUploadUrl}";
        ProfilePhotoRemoveUrl = $"{serverUrl}{ProfilePhotoRemoveUrl}";
        UserProfilePhotoUrl = $"{serverUrl}{UserProfilePhotoUrl}";
#endif
        User = await StateService.GetValue(nameof(User), async () => await HttpClient.GetFromJsonAsync<UserDto>($"User/GetCurrentUser"));

        SelectedGender = User.Gender.ToString();

        await base.OnInitializedAsync();
    }

    private async Task OnClickSave()
    {
        try
        {
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

            await HttpClient.PutAsJsonAsync("User", User);

            IsSuccessMessageBar = true;

            MessageBarText = "Edit successfully";
        }
        catch (Exception e)
        {
            IsSuccessMessageBar = false;

            MessageBarText = e.Message;
        }

        HasMessageBar = true;
    }
}

