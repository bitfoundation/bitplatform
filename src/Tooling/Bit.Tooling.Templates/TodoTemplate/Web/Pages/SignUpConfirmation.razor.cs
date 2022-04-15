using System.Text.RegularExpressions;
using TodoTemplate.App.Models;

namespace TodoTemplate.App.Pages;

public partial class SignUpConfirmation
{
    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    public bool IsLoading { get; set; }

    public BitMessageBarType SignUpConfirmationMessageType { get; set; }

    public string? SignUpConfirmationMessage { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Inject] public TodoTemplateAuthenticationStateProvider TodoTemplateAuthenticationStateProvider { get; set; } = default!;

    protected override Task OnInitAsync()
    {
        SignUpConfirmationMessageType = BitMessageBarType.Success;
        SignUpConfirmationMessage = "A confirmation link has been sent to your email.";

        return base.OnInitAsync();
    }

    private async Task ResendLink()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        SignUpConfirmationMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendConfirmationEmail", new()
            {
                Email = Email
            }, TodoTemplateJsonContext.Default.SendConfirmationEmailRequestDto);

            SignUpConfirmationMessageType = BitMessageBarType.Success;

            SignUpConfirmationMessage = "The confirmation link has been re-sent.";
        }
        catch (KnownException e)
        {
            SignUpConfirmationMessageType = BitMessageBarType.Error;

            SignUpConfirmationMessage = ErrorStrings.ResourceManager.Translate(e.Message, Email!);
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
            if (string.IsNullOrEmpty(Email) || await TodoTemplateAuthenticationStateProvider.IsUserAuthenticated())
                NavigationManager.NavigateTo("/");
        }
    }
}

