//+:cnd:noEmit
using System.Reflection;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Models.Identity;

namespace Microsoft.AspNetCore.Identity;

public static partial class SignInManagerExtensions
{
    /// <summary>
    /// The app invokes <see cref="OtpSignInAsync"/> in the following scenarios:
    /// 
    /// 1. When the user opts to sign in using a 6-digit code received via SMS.
    /// 2. When the user chooses to sign in using a 6-digit code sent via email, typically within a magic link.
    /// 3. After a successful email confirmation after sign-up, to automatically sign in the confirmed user for an improved user experience.
    /// 4. After a successful phone number confirmation after sign-up, to automatically sign in the confirmed user for a smoother user experience.
    /// 5. When the browser is redirected to a magic link created after a social sign-in, to automatically authenticate the user.
    /// 6. When the user opts to sign in using a 6-digit code delivered via native push notification, web push or SignalR message (if configured).
    /// 
    /// It's important to clarify the authentication method (e.g., Social, SMS, Email, or Push) 
    /// to avoid sending a second step to the same communication channel: For successful two-step authentication, the user must use a different method for the second step.
    /// </summary>

    public static async Task<(SignInResult signInResult, string? authenticationMethod)> OtpSignInAsync(this SignInManager<User> signInManager, User user, string otp)
    {
        var appSettings = signInManager.Context.RequestServices.GetRequiredService<ServerApiSettings>();

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
            //#if (notification == true || signalR == true)
            "Push", // => Native push notification, web push or SignalR message.
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
