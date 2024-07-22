//+:cnd:noEmit
namespace Boilerplate.Shared;

public static class Urls
{
    public const string HomePage = "/";

    public const string NotAuthorizedPage = "/not-authorized";

    public const string NotFoundPage = "/not-found";

    public const string TermsPage = "/terms";

    public const string ProfilePage = "/profile";

    public const string ConfirmPage = "/confirm";

    public const string ForgotPasswordPage = "/forgot-password";

    public const string ResetPasswordPage = "/reset-password";

    public const string SignInPage = "/sign-in";

    public const string SignUpPage = "/sign-up";

    //#if (sample == "Admin")
    public const string AddOrEditCategoryPage = "/add-edit-category";

    public const string CategoriesPage = "/categories";

    public const string DashboardPage = "/dashboard";

    public const string ProductsPage = "/products";

    //#elif (sample == "Todo")
    public const string TodoPage = "/todo";
    //#endif

    //#if (offlineDb == true)
    public const string OfflineEditProfilePage = "/offline-edit-profile";
    //#endif

    public const string AboutPage = "/about";
}
