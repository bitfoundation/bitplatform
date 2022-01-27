using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignUp
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public bool IsAcceptPrivacy { get; set; }

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    private async Task OnClickSignUp()
    {
        var response = await HttpClient.PostAsync("User/Create",JsonContent.Create(new UserDto
        {
            UserName = Email,
            Email = Email,
            Password = Password
        }));

        var user = await response.Content.ReadFromJsonAsync<RoleDto>();
    }
}

