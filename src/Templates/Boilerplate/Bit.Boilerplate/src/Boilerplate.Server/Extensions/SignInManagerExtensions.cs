using Boilerplate.Server.Models.Identity;

namespace Microsoft.AspNetCore.Identity;

public static class SignInManagerExtensions
{
    public static async Task<SignInResult> OtpSignInAsync(this SignInManager<User> signInManager, User user, string otp)
    {
        SignInResult result;
        var userManager = signInManager.UserManager;

        if (await userManager.IsLockedOutAsync(user))
        {
            result = SignInResult.LockedOut;
        }
        else
        {
            bool tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, $"Otp,Date:{user.OtpRequestedOn}", otp!);

            if (tokenIsValid is false)
            {
                result = SignInResult.Failed;
                await userManager.AccessFailedAsync(user);
            }
            else
            {
                await userManager.UpdateSecurityStampAsync(user);
                await signInManager.SignInWithClaimsAsync(user!, isPersistent: false, [new Claim("amr", "otp")]);
                result = user.TwoFactorEnabled ? SignInResult.TwoFactorRequired : SignInResult.Success;
            }
        }

        return result;
    }
}
