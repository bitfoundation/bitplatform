using System.Text.RegularExpressions;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignUp
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public bool IsAcceptPrivacy { get; set; }

    public string? EmailErrorMessage { get; set; }
    public string? PasswordErrorMessage { get; set; }

    public bool IsEnableSignUpButton { get; set; }
    public bool IsLoading { get; set; }

    public bool IsSuccessSignUp { get; set; }
    public string? SignUpMessage { get; set; }

    [Inject]
    public HttpClient HttpClient { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    [Inject]
    public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

    private void CheckSignUpButtonEnable()
    {
        if (string.IsNullOrEmpty(Email))
        {
            IsEnableSignUpButton = false;
            return;
        }
        if (string.IsNullOrEmpty(Password))
        {
            IsEnableSignUpButton = false;
            return;
        }
        if (IsAcceptPrivacy is false)
        {
            IsEnableSignUpButton = false;
            return;
        }
        IsEnableSignUpButton = true;
    }

    private bool ValidateSignUp()
    {
        EmailErrorMessage = string.IsNullOrEmpty(Email)
            ? "Please enter your email."
            : null;

        PasswordErrorMessage = string.IsNullOrEmpty(Password)
            ? "Please enter your password."
            : null;

        if (string.IsNullOrEmpty(EmailErrorMessage) is false || string.IsNullOrEmpty(PasswordErrorMessage) is false)
        {
            return false;
        }

        var isCorrectEmailFormat = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");

        EmailErrorMessage = isCorrectEmailFormat.Match(Email).Success is false
            ? "The email address format is incorrect."
            : null;

        PasswordErrorMessage = Password.Length < 6
            ? "The password must have at least 6 characters."
            : null;

        if (string.IsNullOrEmpty(EmailErrorMessage) is false || string.IsNullOrEmpty(PasswordErrorMessage) is false)
        {
            return false;
        }

        return true;
    }

    private async Task DoSignUp()
    {
        IsLoading = true;

        if (ValidateSignUp() is false)
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

            NavigationManager.NavigateTo("/");
        }
        catch (ResourceValidationException e)
        {
            SignUpMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Messages));
        }

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

