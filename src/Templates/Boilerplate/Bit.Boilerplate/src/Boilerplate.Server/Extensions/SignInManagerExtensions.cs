using Microsoft.AspNetCore.Authentication;
using Boilerplate.Server.Models.Identity;

namespace Microsoft.AspNetCore.Identity;

public static class SignInManagerExtensions
{
    public static async Task<SignInResult> OtpSignInAsync(this SignInManager<User> signInManager, User user, string otp)
    {
        var userManager = signInManager.UserManager;

        if (await signInManager.CanSignInAsync(user) is false) return SignInResult.NotAllowed;

        if (await userManager.IsLockedOutAsync(user)) return SignInResult.LockedOut;

        bool tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, $"Otp,{user.OtpRequestedOn}", otp!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            return SignInResult.Failed;
        }

        if (user.TwoFactorEnabled)
        {
            await signInManager.Context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, StoreTwoFactorInfo(user.Id));
            return SignInResult.TwoFactorRequired;
        }

        await signInManager.SignInWithClaimsAsync(user!, isPersistent: false, [new Claim("amr", "otp")]);

        return SignInResult.Success;
    }

    private static ClaimsPrincipal StoreTwoFactorInfo(int userId)
    {
        var identity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);

        identity.AddClaim(new Claim(ClaimTypes.Name, userId.ToString()));

        return new ClaimsPrincipal(identity);
    }
}
