namespace Boilerplate.Client.Core.Shared;

public partial class NotAuthorizedComponent
{
    private ClaimsPrincipal _user = default!;

    [CascadingParameter] public Task<AuthenticationState> AuthenticationState { get; set; } = default!;

    protected override async Task OnParamsSetAsync()
    {
        _user = (await AuthenticationState).User;

        await base.OnParamsSetAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        string? refresh_token = await JSRuntime.GetLocalStorage("refresh_token");

        // Let's update the access token by refreshing it when a refresh token is available.
        // Following this procedure, the newly acquired access token may now include the necessary roles or claims.
        // To prevent infinitie redirect loop, let's append refresh_token=false to the url, so we only redirect in case no refresh_token=false is present

        if (string.IsNullOrEmpty(refresh_token) is false && RedirectUrl?.Contains("refresh_token=false", StringComparison.InvariantCulture) is null or false)
        {
            await AuthenticationStateProvider.RaiseAuthenticationStateHasChanged();

            if ((await AuthenticationStateTask).User.IsAuthenticated())
            {
                if (RedirectUrl is not null)
                {
                    var @char = RedirectUrl.Contains('?') ? '&' : '?'; // The RedirectUrl may already include a query string.
                    NavigationManager.NavigateTo($"{RedirectUrl}{@char}refresh_token=false");
                }
            }
        }

        if ((await AuthenticationStateTask).User.IsAuthenticated() is false)
        {
            // If neither the refresh_token nor the access_token is present, proceed to the sign-in page.
            await SignIn();
        }

        await base.OnAfterFirstRenderAsync();
    }

    private async Task SignIn()
    {
        await JSRuntime.RemoveToken();

        await AuthenticationStateProvider.RaiseAuthenticationStateHasChanged();

        RedirectToSignInPage();
    }

    private void RedirectToSignInPage()
    {
        NavigationManager.NavigateTo($"/sign-in?redirect-url={RedirectUrl ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}");
    }
}
