//+:cnd:noEmit
using System.ComponentModel;

namespace Boilerplate.Shared;

public static partial class PageUrls
{
    public const string NotAuthorized = "/not-authorized";

    [Description("Verify your email address or phone number after sign-up (usually reached automatically).")]
    public const string Confirm = "/confirm";

    [Description("Start the password reset flow by requesting a reset code.")]
    public const string ForgotPassword = "/forgot-password";

    [Description("Set a new password using a reset code.")]
    public const string ResetPassword = "/reset-password";

    [Description("Sign in to your account.")]
    public const string SignIn = "/sign-in";

    [Description("Create a new account.")]
    public const string SignUp = "/sign-up";
}
