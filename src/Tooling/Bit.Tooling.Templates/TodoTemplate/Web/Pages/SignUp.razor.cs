using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignUp
{
    public string? Email { get; set; }

    public string? EmailError { get; set; }

    public string? Password { get; set; }

    public string? PasswordError { get; set; }

    public bool IsAcceptPrivacy { get; set; }

    public bool HasMessageBar { get; set; }

    public bool IsSuccessMessageBar { get; set; }

    public string? MessageBarText { get; set; }

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    [Inject]
    public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

    private async Task DoSignUp()
    {
        EmailError = string.IsNullOrEmpty(Email) ? "Please enter your email" : null;

        PasswordError = string.IsNullOrEmpty(Password) ? "Please enter your password" : null;

        if (EmailError is not null || PasswordError is not null) return;

        try
        {
            await HttpClient.PostAsJsonAsync("User/SignUp", new UserDto
            {
                UserName = Email,
                Email = Email,
                Password = Password
            }, ToDoTemplateJsonContext.Default.UserDto);

            await TodoTemplateAuthenticationService.SignIn(new SignInRequestDto
            {
                UserName = Email,
                Password = Password
            });

            IsSuccessMessageBar = true;
            MessageBarText = "Sign-up successfully";
        }
        catch (ResourceValidationException e)
        {
            MessageBarText = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Messages));
        }

        HasMessageBar = true;
    }

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated())
                NavigationManager.NavigateTo("/");
        }
    }
}

