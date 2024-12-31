using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Boilerplate.Client.Core.Services.Contracts;

namespace Boilerplate.Server.Web.Services;

/// <summary>
/// In standalone API mode, this code only runs during Blazor pre-rendering or Blazor Server.
/// Since the `AppSecureJWTFormat` in the Server.Api project strictly validates access tokens using the provided PFX file, 
/// strict validation isn't necessary here. Instead, we simply parse the token, similar to how it's handled on the client side (Blazor WASM and Blazor Hybrid).
/// </summary>
public partial class SimpleJwtSecureDataFormat : ISecureDataFormat<AuthenticationTicket>
{
    public AuthenticationTicket? Unprotect(string? protectedText) => Unprotect(protectedText, null);

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        try
        {
            if (string.IsNullOrEmpty(protectedText))
            {
                return NotSignedIn();
            }

            var claimsPrincipal = IAuthTokenProvider.ParseAccessToken(protectedText, validateExpiry: true);
            var properties = new AuthenticationProperties() { ExpiresUtc = DateTimeOffset.FromUnixTimeSeconds(long.Parse(claimsPrincipal.FindFirstValue("exp")!)) };
            var data = new AuthenticationTicket(claimsPrincipal, properties: properties, IdentityConstants.BearerScheme);

            return data;
        }
        catch (Exception ex)
        {
            if (AppEnvironment.IsDev())
            {
                Console.WriteLine(ex); // since we do not have access to any logger at this point!
            }

            return NotSignedIn();
        }
    }

    private static AuthenticationTicket NotSignedIn()
    {
        return new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity()), string.Empty);
    }

    public string Protect(AuthenticationTicket data) => Protect(data, null);

    public string Protect(AuthenticationTicket data, string? purpose)
    {
        throw new NotImplementedException(); // In standalone API mode, Blazor Server is not expected to issue JWT tokens.
    }
}
