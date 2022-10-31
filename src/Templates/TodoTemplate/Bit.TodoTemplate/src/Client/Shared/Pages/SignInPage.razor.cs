using Microsoft.AspNetCore.Components.Forms;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.Client.Shared.Pages;

public partial class SignInPage
{
    public SignInRequestDto SignInModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType SignInMessageType { get; set; }

    public string? SignInMessage { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? RedirectUrl { get; set; }

    private async Task DoSignIn()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        SignInMessage = null;

        try
        {
            await AuthenticationService.SignIn(SignInModel);

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (KnownException e)
        {
            SignInMessageType = BitMessageBarType.Error;

            SignInMessage = e.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool IsSubmitButtonEnabled => IsLoading is false;

    protected async override Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (await AuthenticationStateProvider.IsUserAuthenticated())
        {
            NavigationManager.NavigateTo("/");
        }
    }
}

