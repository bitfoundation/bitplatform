//+:cnd:noEmit
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Web;
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Controllers.Identity;

public partial class IdentityController
{
    [AutoInject] private HtmlRenderer htmlRenderer = default!;

    [HttpGet]
    public async Task<string> GetSocialSignInUri(string provider, string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default)
    {
        var uri = Url.Action(nameof(SocialSignIn), new { provider, returnUrl, localHttpPort })!;
        return new Uri(Request.GetBaseUrl(), uri).ToString();
    }

    [HttpGet]
    public async Task<ActionResult> SocialSignIn(string provider, string? returnUrl = null, int? localHttpPort = null)
    {
        var redirectUrl = Url.Action(nameof(SocialSignInCallback), "Identity", new { returnUrl, localHttpPort });
        var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    [HttpGet]
    public async Task<ActionResult> SocialSignInCallback(string? returnUrl = null, int? localHttpPort = null, CancellationToken cancellationToken = default)
    {
        string? url;

        var info = await signInManager.GetExternalLoginInfoAsync() ?? throw new BadRequestException();

        try
        {
            var email = info.Principal.GetEmail();
            var phoneNumber = info.Principal.Claims.FirstOrDefault(c => c.Type is ClaimTypes.HomePhone or ClaimTypes.MobilePhone or ClaimTypes.OtherPhone)?.Value;

            var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);

            if (user is null && (string.IsNullOrEmpty(email) is false || string.IsNullOrEmpty(phoneNumber) is false))
            {
                user = await userManager.FindUserAsync(new() { Email = email, PhoneNumber = phoneNumber });
            }

            if (user is null)
            {
                // Instead of automatically creating a user here, you can navigate to the sign-up page and pass the email and phone number in the query string.

                user = new() { LockoutEnabled = true };

                await userStore.SetUserNameAsync(user, Guid.NewGuid().ToString(), cancellationToken);

                if (string.IsNullOrEmpty(email) is false)
                {
                    await userEmailStore.SetEmailAsync(user, email, cancellationToken);
                }

                if (string.IsNullOrEmpty(phoneNumber) is false)
                {
                    await userPhoneNumberStore.SetPhoneNumberAsync(user, phoneNumber!, cancellationToken);
                }

                var result = await userManager.CreateAsync(user, password: Guid.NewGuid().ToString("N") /* Users can reset their password later. */);

                if (result.Succeeded is false)
                {
                    throw new BadRequestException(string.Join(", ", result.Errors.Select(e => new LocalizedString(e.Code, e.Description))));
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

            (_, url) = await GenerateOtpTokenData(user, returnUrl); // Sign in with a magic link, and 2FA will be prompted if already enabled.
        }
        catch (Exception exp)
        {
            LogSocialSignInCallbackFailed(logger, exp, info.LoginProvider, info.Principal.GetDisplayName());
            url = $"{Urls.SignInPage}?error={Uri.EscapeDataString(exp is KnownException ? Localizer[exp.Message] : Localizer[nameof(AppStrings.UnknownException)])}";
        }
        finally
        {
            await Request.HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // We'll handle sign-in with the following redirects, so no external identity cookie is needed.
        }

        if (localHttpPort is not null) return Redirect(new Uri(new Uri($"http://localhost:{localHttpPort}"), url).ToString());
        var webClientUrl = Configuration.GetValue<string?>("WebClientUrl");
        if (string.IsNullOrEmpty(webClientUrl) is false) return Redirect(new Uri(new Uri(webClientUrl), url).ToString());
        return LocalRedirect($"~{url}");
    }
}
