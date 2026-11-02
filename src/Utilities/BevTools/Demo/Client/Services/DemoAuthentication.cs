using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Bit.BevTools.Demo.Client.Services;

/// <summary>
/// A sign-in that needs no identity provider: enough for BevTools' Authentication state to have something to show,
/// including a claim named like a token, which it redacts.
/// </summary>
public sealed class DemoAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private ClaimsPrincipal _user = Anonymous;

    public bool IsSignedIn => _user.Identity?.IsAuthenticated == true;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(_user));

    public void SignIn()
    {
        _user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "ada.lovelace"),
            new Claim(ClaimTypes.Email, "ada@example.com"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("plan", "Pro"),
            new Claim("access_token", "demo-access-token-must-be-redacted"),
        ], authenticationType: "Demo"));
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public void SignOut()
    {
        _user = Anonymous;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
