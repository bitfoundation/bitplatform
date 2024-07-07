using System.Reflection;
using Boilerplate.Api.Models.Identity;

namespace Microsoft.AspNetCore.Identity;

public static class SignInManagerExtensions
{
    public static async Task<SignInResult> OtpSignInAsync(this SignInManager<User> signInManager, User user, string otp)
    {
        var userManager = signInManager.UserManager;

        if (await signInManager.CanSignInAsync(user) is false) return SignInResult.NotAllowed;

        if (await userManager.IsLockedOutAsync(user)) return SignInResult.LockedOut;

        bool tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp,{user.OtpRequestedOn}"), otp!);

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            return SignInResult.Failed;
        }

        return await SignInOrTwoFactorAsync(signInManager, user); // See SignInManager.SignInOrTwoFactorAsync in aspnetcore repo
    }

    private static async Task<SignInResult> SignInOrTwoFactorAsync(SignInManager<User> signInManager, User user)
    {
        if (user.TwoFactorEnabled)
        {
            // Allow the two-factor flow to continue later within the same request
            var twoFactorAuthenticationInfo = Activator.CreateInstance(_TwoFactorAuthenticationInfoType);
            _TwoFactorAuthenticationInfoTypeUserProperty.SetValue(twoFactorAuthenticationInfo, user);
            _TwoFactorInfoField.SetValue(signInManager, twoFactorAuthenticationInfo);

            return SignInResult.TwoFactorRequired;
        }

        await signInManager.SignInWithClaimsAsync(user!, isPersistent: false, [new Claim("amr", "otp")]);

        return SignInResult.Success;
    }

    private static readonly Type _TwoFactorAuthenticationInfoType = typeof(SignInManager<User>)
        .GetTypeInfo()
        .DeclaredNestedTypes.Single(t => t.Name is "TwoFactorAuthenticationInfo")
        .MakeGenericType(typeof(User));

    private static readonly PropertyInfo _TwoFactorAuthenticationInfoTypeUserProperty = _TwoFactorAuthenticationInfoType!
        .GetProperty("User")!;

    private static readonly FieldInfo _TwoFactorInfoField = typeof(SignInManager<User>)
        .GetField("_twoFactorInfo", BindingFlags.Instance | BindingFlags.NonPublic)!;
}
