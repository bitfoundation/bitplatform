using System.Reflection;
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
            // Allow the two-factor flow to continue later within the same request
            var twoFactorAuthenticationInfo = Activator.CreateInstance(TwoFactorAuthenticationInfoType);
            TwoFactorAuthenticationInfoTypeUserProperty.SetValue(twoFactorAuthenticationInfo, user);
            TwoFactorInfoField.SetValue(signInManager, twoFactorAuthenticationInfo);

            return SignInResult.TwoFactorRequired;
        }

        await signInManager.SignInWithClaimsAsync(user!, isPersistent: false, [new Claim("amr", "otp")]);

        return SignInResult.Success;
    }

    private static readonly Type TwoFactorAuthenticationInfoType = typeof(SignInManager<User>)
        .GetTypeInfo()
        .DeclaredNestedTypes.Single(t => t.Name is "TwoFactorAuthenticationInfo")
        .MakeGenericType(typeof(User));

    private static readonly PropertyInfo TwoFactorAuthenticationInfoTypeUserProperty = TwoFactorAuthenticationInfoType!
        .GetProperty("User")!;

    private static readonly FieldInfo TwoFactorInfoField = typeof(SignInManager<User>)
        .GetField("_twoFactorInfo", BindingFlags.Instance | BindingFlags.NonPublic)!;
}
