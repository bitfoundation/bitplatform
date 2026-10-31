namespace Boilerplate.Workbench.Infrastructure.Models;

public sealed record WorkbenchDemoUser(string NameKey, string Email, string Role)
{
    public string RoleKey => Role switch
    {
        AppRoles.GlobalAdmin => nameof(WorkbenchStrings.RoleGlobalAdmin),
        AppRoles.Demo => nameof(WorkbenchStrings.RoleDemo),
        _ => nameof(WorkbenchStrings.RoleTenantAdmin)
    };
}
