using System.Text.RegularExpressions;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignUp
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool IsAcceptPrivacy { get; set; }

    public string? EmailError { get; set; }
    public string? PasswordError { get; set; }

    public bool IsValidProperty { get; set; }
    public bool IsLoading { get; set; }

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

    private void ValidateSignUp()
    {
        if (string.IsNullOrEmpty(Email)) { IsValidProperty = false; return;}
        if (string.IsNullOrEmpty(Password)) { IsValidProperty = false; return; }
        if (!IsAcceptPrivacy) { IsValidProperty = false; return; }
        IsValidProperty = true;
    }

    private async Task DoSignUp()
    {
        IsLoading = true;

        EmailError = !new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$").Match(Email!).Success 
            ? "The email address format is incorrect." 
            : null;

        PasswordError = Password!.Length < 6
            ? "The password must have at least 6 characters."
            : !new Regex(@"[^a-zA-Z0-9\n\r\t ]").Match(Password!).Success
                ? "The password must have at least one non alphanumeric character." 
                : null;

        if (!string.IsNullOrEmpty(EmailError) || !string.IsNullOrEmpty(PasswordError))
        {
            IsLoading = false;
            return;
        }

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

            NavigationManager.NavigateTo("/");
        }
        catch (ResourceValidationException e)
        {
            MessageBarText = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Messages));
        }

        HasMessageBar = true;

        IsLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated())
                NavigationManager.NavigateTo("/");
        }
    }
}

