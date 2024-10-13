﻿//+:cnd:noEmit
namespace Boilerplate.Shared;

public static partial class Urls
{
    public const string HomePage = "/";

    public const string NotAuthorizedPage = "/not-authorized";

    public const string NotFoundPage = "/not-found";

    public const string TermsPage = "/terms";

    public const string SettingsPage = "/settings";

    public const string ConfirmPage = "/confirm";

    public const string ForgotPasswordPage = "/forgot-password";

    public const string ResetPasswordPage = "/reset-password";

    public const string SignInPage = "/sign-in";

    public const string SignUpPage = "/sign-up";

    public const string AboutPage = "/about";

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

    public static readonly string[] All =
    [
        HomePage, NotAuthorizedPage, NotFoundPage, TermsPage, SettingsPage, ConfirmPage, ForgotPasswordPage, ResetPasswordPage, SignInPage, SignUpPage, AboutPage,
        //#if (sample == "Admin")
        AddOrEditCategoryPage, CategoriesPage, DashboardPage, ProductsPage, 
        //#elif (sample == "Todo")
        TodoPage, 
        //#endif
        //#if (offlineDb == true)
        OfflineEditProfilePage,
        //#endif
    ];

    public static readonly string[] AnonymousPages = [HomePage, TermsPage];
}
