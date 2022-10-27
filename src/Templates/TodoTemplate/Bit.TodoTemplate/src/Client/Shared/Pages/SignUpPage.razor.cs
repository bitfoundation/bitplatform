using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Client.Shared.Pages;

public partial class SignUpPage
{
    public SignUpRequestDto SignUpModel { get; set; } = new();

    public bool IsSignedUp { get; set; }
    public bool IsLoading { get; set; }

    public BitMessageBarType SignUpMessageType { get; set; }
    public string? SignUpMessage { get; set; }

    private bool IsSubmitButtonEnabled =>
        string.IsNullOrWhiteSpace(SignUpModel.UserName) is false &&
        string.IsNullOrWhiteSpace(SignUpModel.Password) is false &&
        SignUpModel.IsAcceptPrivacy &&
        IsLoading is false;

    private async Task DoSignUp()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        SignUpMessage = null;

        try
        {
            SignUpModel.Email = SignUpModel.UserName;

            await HttpClient.PostAsJsonAsync("Auth/SignUp", SignUpModel, AppJsonContext.Default.SignUpRequestDto);

            IsSignedUp = true;
        }
        catch (ResourceValidationException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Errors)
                .Select(e => e.Message));
        }
        catch (KnownException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = e.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResendLink()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        SignUpMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendConfirmationEmail", new()
            {
                Email = SignUpModel.Email
            }, AppJsonContext.Default.SendConfirmationEmailRequestDto);

            SignUpMessageType = BitMessageBarType.Success;
            SignUpMessage = Localizer[nameof(AppStrings.ResendConfirmationLinkMessage)];
        }
        catch (KnownException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = e.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected async override Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (await AuthenticationStateProvider.IsUserAuthenticated())
        {
            NavigationManager.NavigateTo("/");
        }
    }
}
