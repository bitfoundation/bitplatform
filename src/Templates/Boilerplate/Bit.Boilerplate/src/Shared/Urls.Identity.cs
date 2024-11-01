//+:cnd:noEmit
namespace Boilerplate.Shared;

public static partial class Urls
{
    public const string NotAuthorizedPage = "/not-authorized";

    public const string ConfirmPage = "/confirm";

    public const string ForgotPasswordPage = "/forgot-password";

    public const string ResetPasswordPage = "/reset-password";

    public const string SignInPage = "/sign-in";

    public const string SignUpPage = "/sign-up";

    //#if (offlineDb == true)
    public const string OfflineEditProfilePage = "/offline-edit-profile";
    //#endif
}
