using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using TodoTemplate.Shared.Dtos.Account;
using TodoTemplate.Shared.Enums;

namespace TodoTemplate.App.Pages;

public partial class EditProfile
{
    public UserDto? UserDto { get; set; } = new();

    public string? SelectedGender { get; set; }

    public string? ProfilePhotoUploadUrl { get; set; }
    public string? ProfilePhotoRemoveUrl { get; set; }
    public string? UserProfilePhoto { get; set; }

    public bool HasMessageBar { get; set; }
    public bool IsSuccessMessageBar { get; set; }
    public string? MessageBarText { get; set; }

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [CascadingParameter]
    public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationStateTask;
        var userName = authState.User.Claims.ToList().FirstOrDefault(claim => claim.Type == "name")?.Value;

        var userResponse = await HttpClient.GetAsync($"User/{userName}");
        UserDto = await userResponse.Content.ReadFromJsonAsync<UserDto>();
        SelectedGender = UserDto?.Gender.ToString();

        var profilePhotoResponse = await HttpClient.GetAsync($"Attachment/GetProfilePhoto/{userName}");
        var profilePhotoResult = profilePhotoResponse.Content.ReadAsByteArrayAsync().Result;
        if (profilePhotoResult.Length is  not 0) // => convert byte to <img> readable format
            UserProfilePhoto = $"data:image;base64,{Convert.ToBase64String(profilePhotoResult)}";

        // we sent the user name to the upload path because we need to set the user photo name equal to the user name
        ProfilePhotoUploadUrl = $"https://localhost:5001/api/Attachment/UploadProfilePhoto/{userName}";
        ProfilePhotoRemoveUrl = $"https://localhost:5001/api/Attachment/RemoveProfilePhoto/{userName}";

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

            await HttpClient.PutAsync("User", JsonContent.Create(UserDto));

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

