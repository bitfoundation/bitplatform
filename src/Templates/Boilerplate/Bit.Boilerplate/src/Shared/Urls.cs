//+:cnd:noEmit
namespace Boilerplate.Shared;

public static partial class Urls
{
    public const string HomePage = "/";

    public const string NotFoundPage = "/not-found";

    public const string TermsPage = "/terms";

    public const string SettingsPage = "/settings";

    public const string AboutPage = "/about";

    //#if (module == "Admin")
    public const string AddOrEditCategoryPage = "/add-edit-category";

    public const string CategoriesPage = "/categories";

    public const string DashboardPage = "/dashboard";

    public const string ProductsPage = "/products";

    //#endif
    //#if (sample == true)
    public const string TodoPage = "/todo";
    //#endif
    //#if (module == "Sales")
    public const string ProductPage = "/product";
    //#endif

    public static readonly string[] All = typeof(Urls).GetFields()
                                                      .Where(f => f.FieldType == typeof(string))
                                                      .Select(f => f.GetValue(null)!.ToString()!)
                                                      .ToArray();
}
