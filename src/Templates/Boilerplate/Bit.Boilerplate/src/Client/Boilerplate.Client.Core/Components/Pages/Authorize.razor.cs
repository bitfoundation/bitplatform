namespace Boilerplate.Client.Core.Components.Pages;

/// <summary>
/// If you need an authentication process similar to SSO/OAuth, navigate to /authorize with `client_id`, 
/// an appropriately encoded `redirect_uri`, and `state`.
/// 
/// The user can sign in (or sign up if necessary) using various authentication methods provided by project template, 
/// such as social sign-in, 2FA, magic link, and OTP. After authentication, the system redirects to the specified app 
/// with an access token and other relevant authentication details.
/// 
/// Example Usage:
/// Opening:
///     http://localhost:5030/authorize?client_id=SampleClient&redirect_uri=https://sampleclient.azurewebsites.net/signInCallback&state=/carts
///     http://localhost:5030/authorize?client_id=NopClient&redirect_uri=https%3A%2F%2Fsampleclient.azurewebsites.net%2FsignInCallback&state=%2Fcarts
/// 
/// Redirects to:
///     https://sampleclient.azurewebsites.net/signInCallback?access_token=di1d98cxh913fh29ufhnfunxw9&token_type=Bearer&expires_in=3600&state=/carts
///
/// Note:
/// This route is **disabled by default** for security reasons.  
/// To enable it, **uncomment the route definition in the corresponding Razor page** (`@attribute [Route(Urls.Authorize)]`)  
/// </summary>
public partial class Authorize
{
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private IAuthTokenProvider authTokenProvider = default!;

    [Parameter, SupplyParameterFromQuery(Name = "client_id")] public string? ClientId { get; set; }
    [Parameter, SupplyParameterFromQuery(Name = "redirect_uri")] public string? RedirectUri { get; set; }
    [Parameter, SupplyParameterFromQuery(Name = "state")] public string? State { get; set; }

    private Dictionary<string, string[]> clients = new(StringComparer.OrdinalIgnoreCase) // Client configurations; can also be fetched from a server API.
    {
        {
            "SampleClient",
            [
                "https://sampleclient.azurewebsites.net/signInCallback"
            ]
        }
    };

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (clients.TryGetValue(ClientId!, out var clientAllowedRedirectUrls) is false)
        {
            NavigationManager.NavigateTo($"{RedirectUri}#error=invalid_missing_client_id&state={Uri.EscapeDataString(State ?? "")}");
            return;
        }

        if (clientAllowedRedirectUrls.Any(clientUrl => string.Equals(clientUrl, RedirectUri, StringComparison.InvariantCultureIgnoreCase)) is false)
        {
            NavigationManager.NavigateTo($"{RedirectUri}#error=invalid_redirect_uri&state={Uri.EscapeDataString(State ?? "")}");
            return;
        }

        // Attempt to refresh the user's authentication token.
        var accessToken = await authManager.RefreshToken(requestedBy: "AuthorizePage");

        if (string.IsNullOrEmpty(accessToken))
            return; // If the token is expired, the session is deleted, or the user is logged out, redirecting back to sign-in will occur.

        // Parse the access token and calculate its expiration duration.
        var token = IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false);
        var expiresIn = long.Parse(token.FindFirst("exp")!.Value) - long.Parse(token.FindFirst("iat")!.Value);

        NavigationManager.NavigateTo($"{RedirectUri}?access_token={accessToken}&token_type=Bearer&expires_in={expiresIn}&state={Uri.EscapeDataString(State ?? "")}");
    }
}
