using TodoTemplate.Shared.Dtos.Account;
using TodoTemplate.Shared.Enums;

namespace TodoTemplate.App.Pages;

public partial class EditProfile
{
    public UserDto? UserDto { get; set; } = new();

    public string? SelectedGender { get; set; }

    public string? ProfilePhotoUploadUrl { get; set; }
    public string? ProfilePhotoRemoveUrl { get; set; }
    public string? UserProfilePhotoUrl { get; set; }

    public bool HasMessageBar { get; set; }
    public bool IsSuccessMessageBar { get; set; }
    public string? MessageBarText { get; set; }

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

#if BlazorServer || BlazorHybrid
    [Inject]
    public IConfiguration Configuration { get; set; } = default!;
#endif

    protected override async Task OnInitializedAsync()
    {
        ProfilePhotoUploadUrl = "api/Attachment/UploadProfilePhoto/";
        ProfilePhotoRemoveUrl = "api/Attachment/RemoveProfilePhoto/";
        UserProfilePhotoUrl = "api/Attachment/GetProfilePhoto/";

#if BlazorServer || BlazorHybrid
        var serverUrl = Configuration.GetValue<string>("ApiServerAddress");
        ProfilePhotoUploadUrl = $"{serverUrl}{ProfilePhotoUploadUrl}";
        ProfilePhotoRemoveUrl = $"{serverUrl}{ProfilePhotoRemoveUrl}";
        UserProfilePhotoUrl = $"{serverUrl}{UserProfilePhotoUrl}";
#endif

        var userResponse = await HttpClient.GetAsync($"User/GetCurrentUser");
        UserDto = await userResponse.Content.ReadFromJsonAsync<UserDto>();
        SelectedGender = UserDto?.Gender.ToString();

        await base.OnInitializedAsync();
    }

    private async Task OnClickSave()
    {
        try
        {
            if (SelectedGender == Gender.Male.ToString())
            {
                UserDto!.Gender = Gender.Male;
            }
            else if (SelectedGender == Gender.Female.ToString())
            {
                UserDto!.Gender = Gender.Female;
            }
            else if (SelectedGender == Gender.Custom.ToString())
            {
                UserDto!.Gender = Gender.Custom;
            }

            await HttpClient.PutAsJsonAsync("User", UserDto);

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

