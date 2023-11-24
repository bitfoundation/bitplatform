using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Client.Pages.Identity;

public partial class SignInPage
{
    private bool isLoading;
    private string? signInMessage;
    private BitMessageBarType signInMessageType;
    private SignInRequestDto signInModel = new();

    [SupplyParameterFromQuery(Name = "redirect-url"), Parameter] public string? RedirectUrl { get; set; }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if ((await AuthenticationStateTask).User.IsAuthenticated())
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private async Task DoSignIn()
    {
        if (isLoading) return;

        isLoading = true;
        signInMessage = null;

        try
        {
            await AuthenticationManager.SignIn(signInModel);

            NavigationManager.NavigateTo(RedirectUrl ?? "/");
        }
        catch (KnownException e)
        {
            signInMessageType = BitMessageBarType.Error;

            signInMessage = e.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}

