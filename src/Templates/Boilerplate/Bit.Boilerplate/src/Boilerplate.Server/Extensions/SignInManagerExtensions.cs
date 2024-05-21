using Boilerplate.Server.Models.Identity;

namespace Microsoft.AspNetCore.Identity;

public static class SignInManagerExtensions
{
    public static async Task<SignInResult> OtpSignInAsync(this SignInManager<User> signInManager, User user, string otp)
    {
        var userManager = signInManager.UserManager;

        if (await userManager.IsLockedOutAsync(user)) return SignInResult.LockedOut;

        bool tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, $"Otp,Date:{user.OtpRequestedOn}", otp!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            return SignInResult.Failed;
        }

        await userManager.UpdateSecurityStampAsync(user);
        await signInManager.SignInWithClaimsAsync(user!, isPersistent: false, [new Claim("amr", "otp")]);
        return user.TwoFactorEnabled ? SignInResult.TwoFactorRequired : SignInResult.Success;
    }
}
