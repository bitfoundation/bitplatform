//+:cnd:noEmit
namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>The authorization policies of the Workbench's own pages, built on the app's roles.</summary>
public static class WorkbenchPolicies
{
    public const string ManageBuildings = nameof(ManageBuildings);

    /// <summary>The roles that may manage buildings.</summary>
    public static string[] AdminRoles { get; } =
    [
        AppRoles.GlobalAdmin,
        //#if (multitenant == true)
        AppRoles.TenantAdmin,
        //#endif
    ];
}
