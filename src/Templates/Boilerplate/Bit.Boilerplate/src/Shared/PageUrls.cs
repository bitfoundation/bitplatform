//+:cnd:noEmit
namespace Boilerplate.Shared;

public static partial class PageUrls
{
    public const string Home = "/";

    public const string NotFound = "/not-found";

    public const string Terms = "/terms";

    public const string Settings = "/settings";

    public const string About = "/about";

    //#if (module == "Admin")

    public const string Categories = "/categories";

    public const string Dashboard = "/dashboard";

    public const string Products = "/products";

    public const string AddOrEditProduct = "/add-edit-product";

    //#endif
    //#if (sample == true)
    public const string Todo = "/todo";
    //#endif
    //#if (module == "Sales")
    public const string Product = "/product";
    //#endif

    //#if (signalR == true)
    public const string SystemPrompts = "/system-prompts";
    //#endif

    public const string Authorize = "/authorize";

    public const string Roles = "/user-groups";

    public const string Users = "/users";

    //#if (offlineDb == true)
    public const string OfflineDatabaseDemo = "/offline-database-demo";
    //#endif
}
