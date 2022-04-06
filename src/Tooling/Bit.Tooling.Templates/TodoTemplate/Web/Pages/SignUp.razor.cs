using System.Text.RegularExpressions;
using TodoTemplate.App.Models;

namespace TodoTemplate.App.Pages;

public partial class SignUp
{
    public SignUpModel SignUpModel { get; set; } = new();

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
            await HttpClient.PostAsJsonAsync("Auth/SignUp", new()
            {
                UserName = SignUpModel.Email,
                Email = SignUpModel.Email,
                Password = SignUpModel.Password
            }, TodoTemplateJsonContext.Default.SignUpRequestDto);

            SignUpMessageType = BitMessageBarType.Success;
            SignUpMessage = "Confirmation link has sent to your email. Please follow the link.";
        }
        catch (ResourceValidationException e)
        {
            SignUpMessageType = BitMessageBarType.Error;

            SignUpMessage = string.Join(Environment.NewLine, e.Details.SelectMany(d => d.Messages)
                .Select(e => ErrorStrings.ResourceManager.Translate(e, SignUpModel.Email!)));
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

