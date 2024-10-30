namespace AdminPanel.Shared;

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

    public const string AddOrEditCategoryPage = "/add-edit-category";

    public const string CategoriesPage = "/categories";

    public const string DashboardPage = "/dashboard";

    public const string ProductsPage = "/products";



    public static readonly string[] All = typeof(Urls).GetFields()
                                                      .Where(f => f.FieldType == typeof(string))
                                                      .Select(f => f.GetValue(null)!.ToString()!)
                                                      .ToArray();
}
