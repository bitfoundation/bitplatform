namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchDemoUsers
{
    public static IReadOnlyList<WorkbenchDemoUser> All { get; } =
    [
        new(nameof(WorkbenchStrings.DemoUserSarah), "sarah@example.com"),
        new(nameof(WorkbenchStrings.DemoUserDaan), "daan@example.com"),
        new(nameof(WorkbenchStrings.DemoUserNiloofar), "niloofar@example.com"),
    ];

    public static IReadOnlyList<string> TenantKeys { get; } =
    [
        nameof(WorkbenchStrings.DemoTenantStore),
        nameof(WorkbenchStrings.DemoTenantHeadOffice),
    ];
}
