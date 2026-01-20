//+:cnd:noEmit
using Boilerplate.Server.Api.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;

namespace Boilerplate.Server.Api.Features.Identity;

public partial class IdentityController
{
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;
    [AutoInject] private IAuthenticationSchemeProvider authenticationSchemeProvider = default!;

    [HttpGet]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)]
    public async Task<string> GetExternalSignInUri(string provider, string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default)
    {
        var uri = Url.Action(nameof(ExternalSignIn), new { provider, returnUrl, localHttpPort, origin = Request.GetWebAppUrl() })!;
        return new Uri(Request.GetBaseUrl(), uri).ToString();
    }

    [HttpGet]
    public async Task<ActionResult> ExternalSignIn(string provider,
        string? returnUrl = null, /* Specifies the relative page address to navigate to after completion. */
        int? localHttpPort = null, /* Defines the local HTTP server port awaiting the external sign-in result on Windows/macOS/iOS versions of the app. */
        [FromQuery] string? origin = null /* Indicates the base address URL for redirection after the process completes. */ )
    {
        var redirectUrl = Url.Action(nameof(ExternalSignInCallback), "Identity", new { returnUrl, localHttpPort, origin });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    [HttpGet]
    public async Task<ActionResult> ExternalSignInCallback(string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default)
    {
        string? signInPageUri;
        ExternalLoginInfo? info = null;

        try
        {
            info = await signInManager.GetExternalLoginInfoAsync() ?? throw new BadRequestException();
            var email = info.Principal.GetEmail();
            var phoneNumber = phoneService.NormalizePhoneNumber(info.Principal.Claims.FirstOrDefault(c => c.Type is ClaimTypes.HomePhone or ClaimTypes.MobilePhone or ClaimTypes.OtherPhone)?.Value);

            var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            if (user is null && (string.IsNullOrEmpty(email) is false || string.IsNullOrEmpty(phoneNumber) is false))
            {
                user = await userManager.FindUser(new() { Email = email, PhoneNumber = phoneNumber });
            }

            if (user is null)
            {
                var name = info.Principal.FindFirstValue("preferred_username") ?? info.Principal.FindFirstValue(ClaimTypes.Name) ?? info.Principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? info.Principal.FindFirstValue("name");
                // Instead of automatically creating a user here, you can navigate to the sign-up page and pass the email and phone number in the query string.

                user = new()
                {
                    FullName = name,
                    LockoutEnabled = true
                };

                await userStore.SetUserNameAsync(user, Guid.NewGuid().ToString(), cancellationToken);

                if (string.IsNullOrEmpty(email) is false)
                {
                    await userEmailStore.SetEmailAsync(user, email, cancellationToken);
                }

                if (string.IsNullOrEmpty(phoneNumber) is false)
                {
                    await userPhoneNumberStore.SetPhoneNumberAsync(user, phoneNumber!, cancellationToken);
                }

                if (info.LoginProvider is "Keycloak")
                {
                    // Roles are supposed to be managed through the key cloack provider.
                    // The same decision can be made for other providers as well (e.g. AzureAD), if they support roles/claims management.
                    await userManager.CreateAsync(user);
                }
                else
                {
                    // External sign-in providers like Google, Facebook, Twitter etc. are typically used for consumer sign-ups.
                    // Therefore, we assign a role to these users by default.
                    await userManager.CreateUserWithDemoRole(user);
                }

                await userManager.AddLoginAsync(user, info);
            }

            if (string.IsNullOrEmpty(email) is false && email == user.Email && await userManager.IsEmailConfirmedAsync(user) is false)
            {
                await userEmailStore.SetEmailConfirmedAsync(user, true, cancellationToken);
                await userManager.UpdateAsync(user);
            }

            if (string.IsNullOrEmpty(phoneNumber) is false && phoneNumber == user.PhoneNumber && await userManager.IsPhoneNumberConfirmedAsync(user) is false)
            {
                await userPhoneNumberStore.SetPhoneNumberConfirmedAsync(user, true, cancellationToken);
                await userManager.UpdateAsync(user);
            }

            // Using these tokens (if provided by external provider) for further API calls to the provider on behalf of the user.
            // Example: Getting more profile data from the provider, posting on behalf of the user or getting user claims updates, etc.
            var idToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "id_token")?.Value;
            var refreshToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "refresh_token")?.Value;
            var accessToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "access_token")?.Value;
            var expiresAt = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "expires_at")?.Value;
            if (string.IsNullOrEmpty(idToken) is false)
            {
                await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "id_token", idToken);
            }
            if (string.IsNullOrEmpty(refreshToken) is false)
            {
                await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "refresh_token", refreshToken);
            }
            if (string.IsNullOrEmpty(accessToken) is false)
            {
                await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "access_token", accessToken);
            }
            if (string.IsNullOrEmpty(expiresAt) is false)
            {
                await userManager.SetAuthenticationTokenAsync(user, info.LoginProvider, "expires_at", expiresAt);
            }

            (_, signInPageUri) = await GenerateAutomaticSignInLink(user, returnUrl, originalAuthenticationMethod: "External"); // Sign in with a magic link, and 2FA will be prompted if already enabled.
        }
        catch (Exception exp)
        {
            serverExceptionHandler.Handle(exp, new() { { "LoginProvider", info?.LoginProvider }, { "Principal", info?.Principal?.GetDisplayName() } });
            signInPageUri = $"{PageUrls.SignIn}?error={Uri.EscapeDataString(exp is KnownException ? Localizer[exp.Message] : Localizer[nameof(AppStrings.UnknownException)])}";
        }
        finally
        {
            await Request.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // We'll handle sign-in with the following redirects, so no external identity cookie is needed.
        }

        var redirectRelativeUrl = $"{PageUrls.WebInteropApp}?actionName=ExternalSignInCallback&url={Uri.EscapeDataString(signInPageUri!)}&localHttpPort={localHttpPort}";

        if (localHttpPort is not null)
            return Redirect(new Uri(new Uri($"http://localhost:{localHttpPort}"), redirectRelativeUrl).ToString()); // Check out Client.web/wwwroot/web-interop-app.html's comments.

        return Redirect(new Uri(Request.HttpContext.Request.GetWebAppUrl(), redirectRelativeUrl).ToString());
    }

    [HttpGet]
    [AppResponseCache(SharedMaxAge = 3600 * 24 * 7, MaxAge = 60 * 5)]
    public async Task<string[]> GetSupportedExternalAuthSchemes(CancellationToken cancellationToken = default)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        var providers = schemes
            .Where(s => string.IsNullOrEmpty(s.DisplayName) is false && s.Name != IdentityConstants.ExternalScheme)
            .Select(s => s.Name)
            .ToArray();

        return providers;
    }
}
