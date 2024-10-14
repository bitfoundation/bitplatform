//+:cnd:noEmit
using System.Reflection;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Models.Identity;

namespace Microsoft.AspNetCore.Identity;

public static partial class SignInManagerExtensions
{
    /// <summary>
    /// The app calls <see cref="OtpSignInAsync"/> in following scenarios:
    /// 1- When user wants to sign-in with 6 digit number provided through sms.
    /// 2- When user wants to sign-in with 6 digit number provided through email containing magic link.
    /// 3- Successful email confirmation after sign-up in order to auto sign in the confirmed user to improve UX.
    /// 4- Successful phone number confirmation after sign-up in order to auto sign-in the confirmed the user to improve UX.
    /// 5- Browser redirects to magic link created after social sign-in to auto sign-in the user.
    /// 6- When user wants to sign-in with 6 digit number provided through SignalR (If configured).
    /// 7- When user wants to sign-in with 6 digit number provided through push notification (If configured).
    /// We need to clarify the authentication method that can be either Social, Sms, Email, SignalR or push notification,
    /// in order not to send 2nd step to the same communication channel, so user has to use different authentication method to successfully complete 2 step authentication.
    /// </summary>
    public static async Task<(SignInResult signInResult, string? authenticationMethod)> OtpSignInAsync(this SignInManager<User> signInManager, User user, string otp)
    {
        var appSettings = signInManager.Context.RequestServices.GetRequiredService<AppSettings>();

        var expired = (DateTimeOffset.Now - user.OtpRequestedOn) > appSettings.Identity.OtpTokenLifetime;

        if (expired)
            throw new BadRequestException(nameof(AppStrings.ExpiredToken));

        var userManager = signInManager.UserManager;

        if (await signInManager.CanSignInAsync(user) is false) return (SignInResult.NotAllowed, null);

        if (await userManager.IsLockedOutAsync(user)) return (SignInResult.LockedOut, null);

        bool tokenIsValid = false;
        string? authenticationMethod = null;
        string[] authenticationMethods = ["Email",
            "Sms",
            //#if (signalr == true)
            "SignalR",
            //#endif
            //#if (notification == true)
            "Push",
            //#endif
            "Social"];

        foreach (var authMethod in authenticationMethods)
        {
            tokenIsValid = await userManager.VerifyUserTokenAsync(user!, TokenOptions.DefaultPhoneProvider, FormattableString.Invariant($"Otp_{authMethod},{user.OtpRequestedOn?.ToUniversalTime()}"), otp!);
            if (tokenIsValid)
            {
                authenticationMethod = authMethod;
                break;
            }
        }

        if (tokenIsValid is false)
        {
            await userManager.AccessFailedAsync(user);
            return (SignInResult.Failed, null);
        }

        return (await SignInOrTwoFactorAsync(signInManager, user), authenticationMethod); // See SignInManager.SignInOrTwoFactorAsync in aspnetcore repo
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
