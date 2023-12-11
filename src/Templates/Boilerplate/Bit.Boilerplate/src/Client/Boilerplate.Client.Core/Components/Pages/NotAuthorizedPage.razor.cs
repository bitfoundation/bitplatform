namespace Boilerplate.Client.Core.Components.Pages;

public partial class NotAuthorizedPage
{
    private ClaimsPrincipal user = default!;

    [SupplyParameterFromQuery(Name = "redirect-url"), Parameter] public string? RedirectUrl { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        user = (await AuthenticationStateTask).User;

        await base.OnParamsSetAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        string? refresh_token = await StorageService.GetItem("refresh_token");

        // Let's update the access token by refreshing it when a refresh token is available.
        // Following this procedure, the newly acquired access token may now include the necessary roles or claims.
        // To prevent infinitie redirect loop, let's append refresh_token=false to the url, so we only redirect in case no refresh_token=false is present

        if (string.IsNullOrEmpty(refresh_token) is false && RedirectUrl?.Contains("try_refreshing_token=false", StringComparison.InvariantCulture) is null or false)
        {
            await AuthenticationManager.RefreshToken();

            if ((await AuthenticationStateTask).User.IsAuthenticated())
            {
                if (RedirectUrl is not null)
                {
                    var @char = RedirectUrl.Contains('?') ? '&' : '?'; // The RedirectUrl may already include a query string.
                    NavigationManager.NavigateTo($"{RedirectUrl}{@char}try_refreshing_token=false");
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
        await AuthenticationManager.SignOut();

        RedirectToSignInPage();
    }

    private void RedirectToSignInPage()
    {
        var redirectUrl = RedirectUrl ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.NavigateTo($"/sign-in{(string.IsNullOrEmpty(redirectUrl) ? "" : $"?redirect-url={redirectUrl}")}");
    }
}
