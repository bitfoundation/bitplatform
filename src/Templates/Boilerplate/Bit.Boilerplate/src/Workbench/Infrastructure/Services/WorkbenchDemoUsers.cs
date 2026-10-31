//+:cnd:noEmit
namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchDemoUsers
{
    public static IReadOnlyList<WorkbenchDemoUser> All { get; } =
    [
        new(nameof(WorkbenchStrings.DemoUserSarah), "sarah@hq.example.com", AppRoles.GlobalAdmin),
        new(nameof(WorkbenchStrings.DemoUserDaan), "daan@amsterdam.example.com", StoreManagerRole),
        new(nameof(WorkbenchStrings.DemoUserNiloofar), "niloofar@visitor.example.com", AppRoles.Demo),
    ];

    public static IReadOnlyList<string> TenantKeys { get; } =
    [
        nameof(WorkbenchStrings.DemoTenantStore),
        nameof(WorkbenchStrings.DemoTenantHeadOffice),
    ];

    public static WorkbenchDemoUser Find(int index) => All[Math.Clamp(index, 0, All.Count - 1)];

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
