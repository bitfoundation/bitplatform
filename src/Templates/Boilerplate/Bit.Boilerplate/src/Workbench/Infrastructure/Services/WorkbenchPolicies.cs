//+:cnd:noEmit
namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchPolicies
{
    public const string ManageBuildings = nameof(ManageBuildings);

    public static string[] AdminRoles { get; } =
    [
        AppRoles.GlobalAdmin,
        //#if (multitenant == true)
        AppRoles.TenantAdmin,
        //#endif
    ];
}
