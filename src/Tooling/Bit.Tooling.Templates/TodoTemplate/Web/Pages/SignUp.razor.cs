using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class SignUp
{
    public SignUpRequestDto SignUpModel { get; set; } = new();

    public bool IsSignedUp { get; set; }
    public bool IsLoading { get; set; }

    public BitMessageBarType SignUpMessageType { get; set; }
    public string? SignUpMessage { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Inject] public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

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

            await HttpClient.PostAsJsonAsync("Auth/SignUp", SignUpModel, TodoTemplateJsonContext.Default.SignUpRequestDto);

            IsSignedUp = true;

            SignUpMessageType = BitMessageBarType.Success;
            SignUpMessage = "A confirmation link has been sent to your email.";
        }
        catch (ResourceValidationException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Messages)
                .Select(e => ErrorStrings.ResourceManager.Translate(e, SignUpModel.UserName!)));
        }
        catch (KnownException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = ErrorStrings.ResourceManager.Translate(e.Message);
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
            }, TodoTemplateJsonContext.Default.SendConfirmationEmailRequestDto);

            SignUpMessageType = BitMessageBarType.Success;
            SignUpMessage = "The confirmation link has been re-sent.";
        }
        catch (KnownException e)
        {
            SignUpMessageType = BitMessageBarType.Error;
            SignUpMessage = ErrorStrings.ResourceManager.Translate(e.Message, SignUpModel.Email);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated())
            { 
                NavigationManager.NavigateTo("/");
            }
        }
    }
}

