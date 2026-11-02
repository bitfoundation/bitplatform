//+:cnd:noEmit
namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// The sample accounts a view can be signed in as (a global admin, a tenant admin and a demo user) and the sample tenants.
/// </summary>
public static class WorkbenchDemoUsers
{
    public static IReadOnlyList<WorkbenchDemoUser> All { get; } =
    [
        new(nameof(WorkbenchStrings.DemoUserYas), "yas@bitplatform.dev", AppRoles.GlobalAdmin),
        new(nameof(WorkbenchStrings.DemoUserSaleh), "saleh@bitplatform.dev", StoreManagerRole),
        new(nameof(WorkbenchStrings.DemoUserZahra), "zahra@bitplatform.dev", AppRoles.Demo),
    ];

    public static IReadOnlyList<string> TenantKeys { get; } =
    [
        nameof(WorkbenchStrings.DemoTenantStore),
        nameof(WorkbenchStrings.DemoTenantHeadOffice),
    ];

    /// <summary>The user at this index, clamped to the list.</summary>
    public static WorkbenchDemoUser Find(int index) => All[Math.Clamp(index, 0, All.Count - 1)];

    // The store manager is a tenant admin, or a second global admin when the app has no multitenancy.
    //#if (multitenant == true)
    private const string StoreManagerRole = AppRoles.TenantAdmin;
    //#else
    //#if (IsInsideProjectTemplate == true)
    /*
    //#endif
    private const string StoreManagerRole = AppRoles.GlobalAdmin;
    //#if (IsInsideProjectTemplate == true)
    */
    //#endif
    //#endif
}
