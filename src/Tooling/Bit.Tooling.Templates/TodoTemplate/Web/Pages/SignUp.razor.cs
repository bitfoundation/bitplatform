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

    public bool IsSignUpButtonEnabled { get; set; }
    public bool IsLoading { get; set; }

    public BitMessageBarType SignUpMessageType { get; set; }
    public string? SignUpMessage { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Inject] public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    [Inject] public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

    private void CheckSignUpButtonEnable()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || IsAcceptPrivacy is false)
        {
            IsSignUpButtonEnabled = false;
            return;
        }

        IsSignUpButtonEnabled = true;
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
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        IsSignUpButtonEnabled = false;
        SignUpMessage = null;

        if (ValidateSignUp() is false)
        {
            IsLoading = false;
            IsSignUpButtonEnabled = true;
            return;
        }

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SignUp", new()
            {
                UserName = Email,
                Email = Email,
                Password = Password
            }, ToDoTemplateJsonContext.Default.SignUpRequestDto);

            SignUpMessageType = BitMessageBarType.Success;
            SignUpMessage = "Confirmation link has sent to your email. Please follow the link.";
        }
        catch (ResourceValidationException e)
        {
            SignUpMessageType = BitMessageBarType.Error;

            SignUpMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Messages).Select(e => ErrorStrings.ResourceManager.Translate(e, Email!)));
        }
        catch (KnownException e)
        {
            SignUpMessageType = BitMessageBarType.Error;

            SignUpMessage = ErrorStrings.ResourceManager.Translate(e.Message);
        }
        finally
        {
            IsLoading = false;
            IsSignUpButtonEnabled = true;
        }
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

