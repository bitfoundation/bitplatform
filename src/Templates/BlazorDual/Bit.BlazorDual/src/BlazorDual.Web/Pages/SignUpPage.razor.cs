using BlazorDual.Shared.Dtos.Account;

namespace BlazorDual.Web.Pages;

public partial class SignUpPage
{
    private bool _isLoading;
    private bool _isSignedUp;
    private string? _signUpMessage;
    private BitMessageBarType _signUpMessageType;
    private SignUpRequestDto _signUpModel = new();


    protected async override Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (await AuthenticationStateProvider.IsUserAuthenticatedAsync())
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private async Task DoSignUp()
    {
        if (_isLoading) return;

        _isLoading = true;
        _signUpMessage = null;

        try
        {
            _signUpModel.Email = _signUpModel.UserName;

            await HttpClient.PostAsJsonAsync("Auth/SignUp", _signUpModel, AppJsonContext.Default.SignUpRequestDto);

            _isSignedUp = true;
        }
        catch (ResourceValidationException e)
        {
            _signUpMessageType = BitMessageBarType.Error;
            _signUpMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Errors).Select(e => e.Message));
        }
        catch (KnownException e)
        {
            _signUpMessage = e.Message;
            _signUpMessageType = BitMessageBarType.Error;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task DoResendLink()
    {
        if (_isLoading) return;

        _isLoading = true;
        _signUpMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendConfirmationEmail", new() { Email = _signUpModel.Email }, AppJsonContext.Default.SendConfirmationEmailRequestDto);

            _signUpMessageType = BitMessageBarType.Success;
            _signUpMessage = Localizer[nameof(AppStrings.ResendConfirmationLinkMessage)];
        }
        catch (KnownException e)
        {
            _signUpMessage = e.Message;
            _signUpMessageType = BitMessageBarType.Error;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
