namespace BlazorWeb.Client.Pages;

public partial class NotAuthorizedPage
{
    private ClaimsPrincipal _user { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "redirect_url"), Parameter] public string? RedirectUrl { get; set; }

    protected override async Task OnParamsSetAsync()
    {
        _user = (await AuthenticationStateTask).User;

        await base.OnParamsSetAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        string? refresh_token = await JSRuntime.GetLocalStorage("refresh_token");

        // Let's update the access token by refreshing it when a refresh token is available.
        // Following this procedure, the newly acquired access token may now include the necessary roles or claims.
        // To prevent infinitie redirect loop, let's append refreshToken=false to the url, so we only redirect in case no refreshToken=false is present

        if (string.IsNullOrEmpty(refresh_token) is false && RedirectUrl?.Contains("refreshToken=false", StringComparison.InvariantCulture) is null or false)
        {
            try
            {
                var refreshTokenResponse = await (await HttpClient.PostAsJsonAsync("Identity/Refresh", new() { RefreshToken = refresh_token }, AppJsonContext.Default.RefreshRequestDto))
                    .Content.ReadFromJsonAsync(AppJsonContext.Default.TokenResponseDto);

                await JSRuntime.StoreToken(refreshTokenResponse!, true);
                await AuthenticationStateProvider.RaiseAuthenticationStateHasChanged();
            }
            catch (ResourceValidationException) /* refresh_token is expired */
            {
                if (string.IsNullOrEmpty(RedirectUrl) is false)
                {
                    var @char = RedirectUrl.Contains('?') ? '&' : '?'; // The RedirectUrl may already include a query string.
                    NavigationManager.NavigateTo($"{RedirectUrl}{@char}refreshToken=false");
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
        NavigationManager.NavigateTo($"/sign-in?redirect_url={RedirectUrl ?? NavigationManager.ToBaseRelativePath(NavigationManager.Uri)}");
    }
}
